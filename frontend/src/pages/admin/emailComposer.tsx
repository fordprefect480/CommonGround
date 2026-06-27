import { useEffect, useRef, useState } from 'react'
import {
  fetchEmailTemplate,
  sendNewsletter,
  type NewsletterRecipients,
  type SendNewsletterResult,
} from '../../api/email'
import type { Member } from '../../api/auth'

// Host-agnostic pieces of the email composer, shared by the full-page composer
// (EmailCompose) and the in-modal payment-reminder composer
// (PaymentReminderModal). The recipient-selection UI is NOT here - each host
// renders its own and supplies `buildRecipients`/`recipientCount`.

interface HtmlEditorProps {
  value: string
  onChange: (html: string) => void
  disabled: boolean
}

export function HtmlEditor({ value, onChange, disabled }: HtmlEditorProps) {
  const ref = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    const el = ref.current
    if (el && el.innerHTML !== value) {
      el.innerHTML = value
    }
  }, [value])

  return (
    <div
      ref={ref}
      id="email-html-body"
      className="email-html-editor"
      contentEditable={!disabled}
      role="textbox"
      aria-multiline="true"
      aria-disabled={disabled}
      suppressContentEditableWarning
      onInput={(e) => onChange((e.currentTarget as HTMLDivElement).innerHTML)}
    />
  )
}

export type TemplateState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; header: string; footer: string }

const BODY_SLOT_ATTR = 'data-cg-body-slot'

export function splitTemplate(html: string): { header: string; footer: string } {
  const doc = new DOMParser().parseFromString(html, 'text/html')

  const variableSpan = Array.from(doc.querySelectorAll('span[data-type="variable"]'))
    .find((s) => s.textContent?.includes('{{{BODY}}}'))

  let anchor: Element | null = null
  if (variableSpan) {
    let walker: Element | null = variableSpan
    while (walker && walker.tagName !== 'TR') walker = walker.parentElement
    anchor = walker ?? variableSpan
  } else {
    const walker = doc.createTreeWalker(doc.body, NodeFilter.SHOW_TEXT)
    let node = walker.nextNode()
    while (node && !node.nodeValue?.includes('{{{BODY}}}')) node = walker.nextNode()
    if (node?.parentElement) anchor = node.parentElement
  }

  if (!anchor) return { header: html, footer: '' }

  anchor.setAttribute(BODY_SLOT_ATTR, '')

  const headerDoc = doc.cloneNode(true) as Document
  const headerAnchor = headerDoc.querySelector(`[${BODY_SLOT_ATTR}]`)
  if (headerAnchor) {
    trimFollowing(headerAnchor)
    headerAnchor.remove()
  }

  const footerDoc = doc.cloneNode(true) as Document
  const footerAnchor = footerDoc.querySelector(`[${BODY_SLOT_ATTR}]`)
  if (footerAnchor) {
    trimPreceding(footerAnchor)
    footerAnchor.remove()
  }

  return {
    header: headerDoc.body.innerHTML,
    footer: footerDoc.body.innerHTML,
  }
}

function trimFollowing(start: Element) {
  let current: Node = start
  while (current.parentNode) {
    let next = current.nextSibling
    while (next) {
      const after = next.nextSibling
      current.parentNode.removeChild(next)
      next = after
    }
    if (current.parentNode.nodeName === 'BODY') break
    current = current.parentNode
  }
}

function trimPreceding(start: Element) {
  let current: Node = start
  while (current.parentNode) {
    let prev = current.previousSibling
    while (prev) {
      const before = prev.previousSibling
      current.parentNode.removeChild(prev)
      prev = before
    }
    if (current.parentNode.nodeName === 'BODY') break
    current = current.parentNode
  }
}

const EMPTY_BODY_HTML_PATTERNS = [/^\s*$/, /^<p>\s*<\/p>$/i]

export function isBodyEmpty(html: string): boolean {
  const stripped = html.replace(/<br\s*\/?>(\s|&nbsp;)*/gi, '').trim()
  return EMPTY_BODY_HTML_PATTERNS.some((re) => re.test(stripped))
}

export function memberLabel(m: Member): string {
  return m.displayName ?? m.email ?? '(no name)'
}

export function pluralize(n: number, singular: string): string {
  return `${n} ${singular}${n === 1 ? '' : 's'}`
}

/** Loads the Resend template once and returns it split around the {{{BODY}}} slot. */
export function useEmailTemplate(): TemplateState {
  const [template, setTemplate] = useState<TemplateState>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const html = await fetchEmailTemplate()
        if (cancelled) return
        const { header, footer } = splitTemplate(html)
        setTemplate({ status: 'ready', header, footer })
      } catch (err) {
        if (cancelled) return
        setTemplate({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load template' })
      }
    })()
    return () => { cancelled = true }
  }, [])

  return template
}

type SendState =
  | { status: 'idle' }
  | { status: 'confirming' }
  | { status: 'sending' }
  | { status: 'error'; message: string }

interface ComposeFormProps {
  subject: string
  onSubjectChange: (subject: string) => void
  body: string
  onBodyChange: (html: string) => void
  template: TemplateState
  recipientCount: number
  recipientNoun?: string
  buildRecipients: () => NewsletterRecipients
  onSent: (result: SendNewsletterResult) => void
  /** Notifies the host while a send is in flight so it can lock its recipient UI. */
  onSendingChange?: (sending: boolean) => void
  /** Host-specific action shown next to the send button (e.g. Cancel / Close). */
  secondaryAction?: React.ReactNode
  /** Host-specific note shown next to the send button (e.g. "no subscribers"). */
  footerNote?: React.ReactNode
}

/**
 * Subject + template-aware body editor + the send/confirm/error state machine.
 * Sends via the existing newsletter endpoint and hands the result back through
 * `onSent` so the host decides where to go next.
 */
export function ComposeForm({
  subject,
  onSubjectChange,
  body,
  onBodyChange,
  template,
  recipientCount,
  recipientNoun = 'recipient',
  buildRecipients,
  onSent,
  onSendingChange,
  secondaryAction,
  footerNote,
}: ComposeFormProps) {
  const [send, setSend] = useState<SendState>({ status: 'idle' })

  const subjectTrimmed = subject.trim()
  const canCompose = subjectTrimmed.length > 0 && !isBodyEmpty(body)
  const canSend = canCompose && recipientCount > 0 && send.status === 'idle'
  const sending = send.status === 'sending'

  const confirmSend = () => {
    if (!canSend) return
    setSend({ status: 'confirming' })
  }

  const performSend = async () => {
    setSend({ status: 'sending' })
    onSendingChange?.(true)
    try {
      const fullHtml = template.status === 'ready'
        ? template.header + body + template.footer
        : body
      const result = await sendNewsletter(subjectTrimmed, fullHtml, buildRecipients())
      onSent(result)
    } catch (err) {
      setSend({ status: 'error', message: err instanceof Error ? err.message : 'Send failed' })
      onSendingChange?.(false)
    }
  }

  return (
    <>
      <div className="field">
        <label className="field-label" htmlFor="email-subject">Subject</label>
        <input
          id="email-subject"
          type="text"
          value={subject}
          onChange={(e) => onSubjectChange(e.target.value)}
          disabled={sending}
          maxLength={200}
          autoComplete="off"
        />
      </div>

      <div className="field">
        <label className="field-label" htmlFor="email-html-body">Body</label>
        {template.status === 'error' && (
          <div className="form-error" role="alert">Couldn't load template from Resend: {template.message}</div>
        )}
        {template.status === 'loading' ? (
          <p className="admin-loading">Loading template from Resend&hellip;</p>
        ) : (
          <div className="email-compose-preview">
            {template.status === 'ready' && (
              <div
                className="email-compose-chrome"
                aria-hidden="true"
                dangerouslySetInnerHTML={{ __html: template.header }}
              />
            )}
            <HtmlEditor value={body} onChange={onBodyChange} disabled={sending} />
            {template.status === 'ready' && (
              <div
                className="email-compose-chrome"
                aria-hidden="true"
                dangerouslySetInnerHTML={{ __html: template.footer }}
              />
            )}
          </div>
        )}
      </div>

      {send.status === 'idle' && (
        <div className="admin-actions">
          <button type="button" className="primary-button" onClick={confirmSend} disabled={!canSend}>
            {recipientCount > 0 ? `Send to ${pluralize(recipientCount, recipientNoun)}` : 'Send'}
          </button>
          {secondaryAction}
          {footerNote}
        </div>
      )}

      {send.status === 'confirming' && (
        <div className="form-error" role="alertdialog" aria-labelledby="email-confirm-heading">
          <p id="email-confirm-heading">
            <strong>Send &ldquo;{subjectTrimmed}&rdquo; to {pluralize(recipientCount, recipientNoun)}?</strong>
          </p>
          <p>This cannot be undone.</p>
          <div className="admin-actions" style={{ marginTop: '0.75rem' }}>
            <button type="button" className="primary-button" onClick={performSend}>
              Yes, send now
            </button>
            <button type="button" className="footer-link" onClick={() => setSend({ status: 'idle' })}>
              Cancel
            </button>
          </div>
        </div>
      )}

      {send.status === 'sending' && (
        <p className="admin-loading">Sending&hellip; this may take a moment.</p>
      )}

      {send.status === 'error' && (
        <>
          <div className="form-error" role="alert">{send.message}</div>
          <div className="admin-actions" style={{ marginTop: '0.75rem' }}>
            <button type="button" className="footer-link" onClick={() => setSend({ status: 'idle' })}>
              Dismiss
            </button>
          </div>
        </>
      )}
    </>
  )
}
