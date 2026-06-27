import { useEffect, useMemo, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import AdminBackButton from './AdminBackButton'
import { fetchEmailTemplate, fetchSubscriberCount, sendNewsletter, type NewsletterRecipients } from '../../api/email'
import { fetchMembers, type Member } from '../../api/auth'

interface HtmlEditorProps {
  value: string
  onChange: (html: string) => void
  disabled: boolean
}

function HtmlEditor({ value, onChange, disabled }: HtmlEditorProps) {
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

type SubscriberCountState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; count: number }

type MembersState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; members: Member[] }

type RecipientMode = 'all_subscribers' | 'specific_members' | 'custom_emails'

type SendState =
  | { status: 'idle' }
  | { status: 'confirming' }
  | { status: 'sending' }
  | { status: 'error'; message: string }

type TemplateState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; header: string; footer: string }

const BODY_SLOT_ATTR = 'data-cg-body-slot'

function splitTemplate(html: string): { header: string; footer: string } {
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

function isBodyEmpty(html: string): boolean {
  const stripped = html.replace(/<br\s*\/?>(\s|&nbsp;)*/gi, '').trim()
  return EMPTY_BODY_HTML_PATTERNS.some((re) => re.test(stripped))
}

function parseEmails(raw: string): string[] {
  return raw
    .split(/[\s,;]+/)
    .map((part) => part.trim().replace(/^.*<([^>]+)>\s*$/, '$1'))
    .filter((part) => part.length > 0)
}

function memberLabel(m: Member): string {
  return m.displayName ?? m.email ?? '(no name)'
}

interface ComposePreset {
  memberIds?: string[]
  subject?: string
  bodyHtml?: string
  note?: string
}

export default function EmailCompose() {
  const navigate = useNavigate()
  // The Members page can deep-link here to send a payment reminder, passing the
  // recipients and a pre-filled draft through router state for the admin to edit.
  const preset = useLocation().state as ComposePreset | null
  const presetMemberIds = preset?.memberIds ?? []
  const [subject, setSubject] = useState(preset?.subject ?? '')
  const [body, setBody] = useState(preset?.bodyHtml ?? '')
  const [template, setTemplate] = useState<TemplateState>({ status: 'loading' })
  const [mode, setMode] = useState<RecipientMode>(
    presetMemberIds.length > 0 ? 'specific_members' : 'all_subscribers',
  )
  const [subscriberCount, setSubscriberCount] = useState<SubscriberCountState>({ status: 'loading' })
  const [members, setMembers] = useState<MembersState>({ status: 'idle' })
  const [selectedMemberIds, setSelectedMemberIds] = useState<Set<string>>(new Set(presetMemberIds))
  const [memberQuery, setMemberQuery] = useState('')
  const [comboOpen, setComboOpen] = useState(false)
  const [comboHighlight, setComboHighlight] = useState(0)
  const comboRef = useRef<HTMLDivElement | null>(null)
  const comboInputRef = useRef<HTMLInputElement | null>(null)
  const [customEmailsText, setCustomEmailsText] = useState('')
  const [send, setSend] = useState<SendState>({ status: 'idle' })

  useEffect(() => {
    let cancelled = false
    const load = async () => {
      try {
        const count = await fetchSubscriberCount()
        if (cancelled) return
        setSubscriberCount({ status: 'ready', count })
      } catch (err) {
        if (cancelled) return
        setSubscriberCount({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load recipient count' })
      }
    }
    load()
    return () => { cancelled = true }
  }, [])

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

  useEffect(() => {
    if (mode !== 'specific_members') return
    if (members.status !== 'idle') return
    setMembers({ status: 'loading' })
    let active = true
    ;(async () => {
      try {
        const list = await fetchMembers()
        if (active) setMembers({ status: 'ready', members: list })
      } catch (err) {
        if (active) setMembers({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load members' })
      }
    })()
    return () => { active = false }
    // members.status intentionally omitted: re-running on its change would cancel the in-flight fetch
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mode])

  const customEmails = useMemo(() => parseEmails(customEmailsText), [customEmailsText])

  const selectedMembers = useMemo(() => {
    if (members.status !== 'ready') return []
    const byId = new Map(members.members.map((m) => [m.id, m]))
    return Array.from(selectedMemberIds)
      .map((id) => byId.get(id))
      .filter((m): m is Member => m !== undefined)
  }, [members, selectedMemberIds])

  const SUGGESTION_LIMIT = 8

  const suggestions = useMemo(() => {
    if (members.status !== 'ready') return []
    const q = memberQuery.trim().toLowerCase()
    return members.members
      .filter((m) => !selectedMemberIds.has(m.id))
      .filter((m) => !!(m.email && m.email.length > 0))
      .filter((m) => {
        if (q.length === 0) return true
        return memberLabel(m).toLowerCase().includes(q) || (m.email ?? '').toLowerCase().includes(q)
      })
      .slice(0, SUGGESTION_LIMIT)
  }, [members, memberQuery, selectedMemberIds])

  useEffect(() => {
    setComboHighlight(0)
  }, [memberQuery, comboOpen])

  useEffect(() => {
    if (!comboOpen) return
    const handler = (e: MouseEvent) => {
      if (comboRef.current && !comboRef.current.contains(e.target as Node)) {
        setComboOpen(false)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [comboOpen])

  const recipientCount = (() => {
    switch (mode) {
      case 'all_subscribers':
        return subscriberCount.status === 'ready' ? subscriberCount.count : 0
      case 'specific_members':
        return selectedMemberIds.size
      case 'custom_emails':
        return customEmails.length
    }
  })()

  const subjectTrimmed = subject.trim()
  const bodyEmpty = isBodyEmpty(body)
  const canCompose = subjectTrimmed.length > 0 && !bodyEmpty
  const canSend = canCompose && recipientCount > 0 && send.status === 'idle'

  const addMember = (id: string) => {
    setSelectedMemberIds((prev) => {
      if (prev.has(id)) return prev
      const next = new Set(prev)
      next.add(id)
      return next
    })
    setMemberQuery('')
    comboInputRef.current?.focus()
  }

  const removeMember = (id: string) => {
    setSelectedMemberIds((prev) => {
      if (!prev.has(id)) return prev
      const next = new Set(prev)
      next.delete(id)
      return next
    })
  }

  const clearSelection = () => setSelectedMemberIds(new Set())

  const onComboKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setComboOpen(true)
      setComboHighlight((h) => (suggestions.length === 0 ? 0 : (h + 1) % suggestions.length))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setComboOpen(true)
      setComboHighlight((h) => (suggestions.length === 0 ? 0 : (h - 1 + suggestions.length) % suggestions.length))
    } else if (e.key === 'Enter') {
      if (suggestions.length > 0) {
        e.preventDefault()
        const pick = suggestions[Math.min(comboHighlight, suggestions.length - 1)]
        addMember(pick.id)
      }
    } else if (e.key === 'Escape') {
      if (comboOpen) {
        e.preventDefault()
        setComboOpen(false)
      }
    } else if (e.key === 'Backspace' && memberQuery.length === 0 && selectedMembers.length > 0) {
      e.preventDefault()
      removeMember(selectedMembers[selectedMembers.length - 1].id)
    }
  }

  const buildRecipients = (): NewsletterRecipients => {
    switch (mode) {
      case 'all_subscribers':
        return { mode: 'all_subscribers' }
      case 'specific_members':
        return { mode: 'specific_members', memberIds: Array.from(selectedMemberIds) }
      case 'custom_emails':
        return { mode: 'custom_emails', emails: customEmails }
    }
  }

  const confirmSend = () => {
    if (!canSend) return
    setSend({ status: 'confirming' })
  }

  const performSend = async () => {
    setSend({ status: 'sending' })
    try {
      const fullHtml = template.status === 'ready'
        ? template.header + body + template.footer
        : body
      const result = await sendNewsletter(subjectTrimmed, fullHtml, buildRecipients())
      navigate(`/admin/email/${result.id}`, { replace: true })
    } catch (err) {
      setSend({ status: 'error', message: err instanceof Error ? err.message : 'Send failed' })
    }
  }

  const recipientNoun = mode === 'custom_emails' ? 'address' : 'recipient'
  const pluralize = (n: number, singular: string) => `${n} ${singular}${n === 1 ? '' : 's'}`

  return (
    <section className="admin-page" aria-labelledby="email-heading">
      <header className="admin-page-header">
        <div className="admin-page-heading-group">
          <AdminBackButton to="/admin/email" label="Back to emails" />
          <h1 id="email-heading" className="admin-page-title">New email</h1>
        </div>
      </header>

      {subscriberCount.status === 'error' && mode === 'all_subscribers' && (
        <div className="form-error" role="alert">{subscriberCount.message}</div>
      )}

      <div className="card">
        <fieldset className="field" disabled={send.status === 'sending'}>
          <legend className="field-label">Recipients</legend>
          <div className="recipient-mode-options">
            <label className="checkbox-field">
              <input
                type="radio"
                name="recipient-mode"
                value="all_subscribers"
                checked={mode === 'all_subscribers'}
                onChange={() => setMode('all_subscribers')}
              />
              <span>
                All subscribers
                {subscriberCount.status === 'ready' && (
                  <span className="card-note" style={{ marginLeft: '0.5rem', fontStyle: 'normal' }}>
                    ({pluralize(subscriberCount.count, 'subscriber')})
                  </span>
                )}
              </span>
            </label>
            <label className="checkbox-field">
              <input
                type="radio"
                name="recipient-mode"
                value="specific_members"
                checked={mode === 'specific_members'}
                onChange={() => setMode('specific_members')}
              />
              <span>Specific members</span>
            </label>
            <label className="checkbox-field">
              <input
                type="radio"
                name="recipient-mode"
                value="custom_emails"
                checked={mode === 'custom_emails'}
                onChange={() => setMode('custom_emails')}
              />
              <span>Any email address</span>
            </label>
          </div>

          {mode === 'specific_members' && (
            <div className="recipient-mode-detail">
              {preset?.note && (
                <p className="card-note" style={{ marginTop: 0 }}>
                  Pre-selected {preset.note}. Review the list below before sending &mdash; remove anyone you don't want to email.
                </p>
              )}
              {members.status === 'loading' && <p className="admin-loading">Loading members&hellip;</p>}
              {members.status === 'error' && (
                <div className="form-error" role="alert">{members.message}</div>
              )}
              {members.status === 'ready' && (
                <div className="member-combobox" ref={comboRef}>
                <div
                  className="member-combobox-input"
                  onClick={() => {
                    comboInputRef.current?.focus()
                    setComboOpen(true)
                  }}
                  role="presentation"
                >
                  {selectedMembers.map((m) => (
                    <span key={m.id} className="member-chip">
                      <span className="member-chip-label">{memberLabel(m)}</span>
                      <button
                        type="button"
                        className="member-chip-remove"
                        aria-label={`Remove ${memberLabel(m)}`}
                        onClick={(e) => {
                          e.stopPropagation()
                          removeMember(m.id)
                        }}
                        disabled={send.status === 'sending'}
                      >
                        ×
                      </button>
                    </span>
                  ))}
                  <input
                    ref={comboInputRef}
                    type="text"
                    value={memberQuery}
                    onChange={(e) => {
                      setMemberQuery(e.target.value)
                      setComboOpen(true)
                    }}
                    onFocus={() => setComboOpen(true)}
                    onKeyDown={onComboKeyDown}
                    placeholder={selectedMembers.length === 0 ? 'Type a name or email…' : ''}
                    autoComplete="off"
                    disabled={send.status === 'sending'}
                    role="combobox"
                    aria-expanded={comboOpen}
                    aria-autocomplete="list"
                  />
                </div>
                {comboOpen && suggestions.length > 0 && (
                  <ul className="member-suggestions" role="listbox">
                    {suggestions.map((m, i) => (
                      <li
                        key={m.id}
                        role="option"
                        aria-selected={i === comboHighlight}
                        className={`member-suggestion${i === comboHighlight ? ' is-highlighted' : ''}`}
                        onMouseDown={(e) => {
                          e.preventDefault()
                          addMember(m.id)
                        }}
                        onMouseEnter={() => setComboHighlight(i)}
                      >
                        <span className="member-suggestion-name">{memberLabel(m)}</span>
                        <span className="member-suggestion-email">{m.email}</span>
                      </li>
                    ))}
                  </ul>
                )}
                {comboOpen && suggestions.length === 0 && memberQuery.trim().length > 0 && (
                  <p className="member-suggestions-empty">No matching members.</p>
                )}
                {selectedMembers.length > 0 && (
                  <div className="admin-actions" style={{ marginTop: '0.5rem' }}>
                    <span className="card-note">{pluralize(selectedMembers.length, 'member')} selected</span>
                    <button type="button" className="footer-link" onClick={clearSelection} disabled={send.status === 'sending'}>
                      Clear
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>
        )}

          {mode === 'custom_emails' && (
            <div className="recipient-mode-detail">
              <textarea
                id="email-custom-recipients"
                value={customEmailsText}
                onChange={(e) => setCustomEmailsText(e.target.value)}
                disabled={send.status === 'sending'}
                rows={3}
                placeholder="alice@example.com, bob@example.com"
                autoComplete="off"
                spellCheck={false}
              />
              <span className="card-note">
                Separate addresses with commas, spaces, or newlines. Each recipient gets their own copy.
                {customEmails.length > 0 && (
                  <> Parsed <strong>{customEmails.length}</strong> address{customEmails.length === 1 ? '' : 'es'}.</>
                )}
              </span>
            </div>
          )}
        </fieldset>

        <div className="field">
          <label className="field-label" htmlFor="email-subject">Subject</label>
          <input
            id="email-subject"
            type="text"
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
            disabled={send.status === 'sending'}
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
              <HtmlEditor
                value={body}
                onChange={setBody}
                disabled={send.status === 'sending'}
              />
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
            <button
              type="button"
              className="primary-button"
              onClick={confirmSend}
              disabled={!canSend}
            >
              {recipientCount > 0 ? `Send to ${pluralize(recipientCount, recipientNoun)}` : 'Send'}
            </button>
            <button type="button" className="footer-link" onClick={() => navigate('/admin/email')}>
              Cancel
            </button>
            {mode === 'all_subscribers' && subscriberCount.status === 'ready' && subscriberCount.count === 0 && (
              <span className="card-note">There are no subscribed members to send to.</span>
            )}
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
      </div>
    </section>
  )
}
