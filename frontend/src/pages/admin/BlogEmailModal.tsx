import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'
import type { BlogPostAdminListItem } from '../../api/blog'
import { fetchSubscriberCount, type SendNewsletterResult } from '../../api/email'
import { ComposeForm, pluralize, useEmailTemplate } from './emailComposer'

interface BlogEmailModalProps {
  /** The published post the email announces. Its title and public link pre-fill the draft. */
  post: BlogPostAdminListItem
  onClose: () => void
  onSent: (result: SendNewsletterResult) => void
}

type SubscriberCountState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; count: number }

function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

/**
 * Modal composer for emailing all subscribers about a blog post. The draft is
 * pre-filled with the post title as the subject and a short intro linking to the
 * published post, which the admin edits before sending. Rendered only while open
 * (the parent conditionally mounts it).
 */
export default function BlogEmailModal({ post, onClose, onSent }: BlogEmailModalProps) {
  const [isNewsletter, setIsNewsletter] = useState(true)
  const template = useEmailTemplate(isNewsletter)
  const [subscriberCount, setSubscriberCount] = useState<SubscriberCountState>({ status: 'loading' })

  const postUrl = `${window.location.origin}/blog/${post.slug}`
  const [subject, setSubject] = useState(post.title)
  const [body, setBody] = useState(
    `<p>Hi,</p><p>Our monthly newsletter is here. Please click the link below to have a read:</p>` +
      `<p><a href="${escapeHtml(postUrl)}">${escapeHtml(post.title)}</a></p><p></p>`,
  )
  const [sending, setSending] = useState(false)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const count = await fetchSubscriberCount()
        if (!cancelled) setSubscriberCount({ status: 'ready', count })
      } catch (err) {
        if (!cancelled) {
          setSubscriberCount({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load recipient count' })
        }
      }
    })()
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !sending) onClose()
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [onClose, sending])

  const recipientCount = subscriberCount.status === 'ready' ? subscriberCount.count : 0

  return createPortal(
    <div role="presentation" onClick={() => { if (!sending) onClose() }} style={overlayStyle}>
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Email subscribers about this post"
        className="card admin-form"
        onClick={(e) => e.stopPropagation()}
        style={{ width: '100%', maxWidth: 680, margin: 0, maxHeight: '90vh', overflowY: 'auto' }}
      >
        <h2 className="section-title">Email subscribers about this post</h2>

        <div className="field">
          <span className="field-label">To</span>
          {subscriberCount.status === 'error' ? (
            <div className="form-error" role="alert">{subscriberCount.message}</div>
          ) : (
            <p className="card-note" style={{ margin: 0 }}>
              All subscribers
              {subscriberCount.status === 'ready' && <> &middot; {pluralize(subscriberCount.count, 'subscriber')}</>}
            </p>
          )}
        </div>

        <ComposeForm
          subject={subject}
          onSubjectChange={setSubject}
          body={body}
          onBodyChange={setBody}
          isNewsletter={isNewsletter}
          onIsNewsletterChange={setIsNewsletter}
          template={template}
          recipientCount={recipientCount}
          buildRecipients={() => ({ mode: 'all_subscribers' })}
          onSent={onSent}
          onSendingChange={setSending}
          secondaryAction={
            <button type="button" className="footer-link" onClick={onClose} disabled={sending}>
              Cancel
            </button>
          }
          footerNote={
            subscriberCount.status === 'ready' && subscriberCount.count === 0 ? (
              <span className="card-note">There are no subscribed members to send to.</span>
            ) : undefined
          }
        />
      </div>
    </div>,
    document.body,
  )
}

const overlayStyle: React.CSSProperties = {
  position: 'fixed',
  inset: 0,
  zIndex: 1000,
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  padding: 20,
  background: 'rgba(20, 20, 16, 0.55)',
}
