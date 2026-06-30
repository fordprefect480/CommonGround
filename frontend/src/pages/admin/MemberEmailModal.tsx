import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'
import type { Member } from '../../api/auth'
import type { SendNewsletterResult } from '../../api/email'
import { ComposeForm, memberLabel, pluralize, useEmailTemplate } from './emailComposer'

interface MemberEmailModalProps {
  /** The members selected in the table. Those without an email are skipped. */
  members: Member[]
  onClose: () => void
  onSent: (result: SendNewsletterResult) => void
}

// Starter greeting the admin writes the rest of their message after.
const BODY_PLACEHOLDER = '<p>Hi,</p><p></p>'

function hasEmail(m: Member): boolean {
  return !!(m.email && m.email.trim().length > 0)
}

/**
 * Modal composer for emailing the members selected on the Members page. The
 * admin can send anything; the body starts with a greeting they continue from.
 * Rendered only while open (the parent conditionally mounts it).
 */
export default function MemberEmailModal({
  members,
  onClose,
  onSent,
}: MemberEmailModalProps) {
  const [isNewsletter, setIsNewsletter] = useState(true)
  const template = useEmailTemplate(isNewsletter)

  const eligible = members.filter(hasEmail)
  const skipped = members.length - eligible.length

  const [recipientIds, setRecipientIds] = useState<Set<string>>(() => new Set(eligible.map((m) => m.id)))
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState(BODY_PLACEHOLDER)
  const [sending, setSending] = useState(false)

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !sending) onClose()
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [onClose, sending])

  const recipients = eligible.filter((m) => recipientIds.has(m.id))

  const removeRecipient = (id: string) => {
    setRecipientIds((prev) => {
      if (!prev.has(id)) return prev
      const next = new Set(prev)
      next.delete(id)
      return next
    })
  }

  return createPortal(
    <div role="presentation" onClick={() => { if (!sending) onClose() }} style={overlayStyle}>
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Email selected members"
        className="card admin-form"
        onClick={(e) => e.stopPropagation()}
        style={{ width: '100%', maxWidth: 680, margin: 0, maxHeight: '90vh', overflowY: 'auto' }}
      >
        <h2 className="section-title">Email selected members</h2>

        <div className="field">
          <span className="field-label">To</span>
          {recipients.length === 0 ? (
            <p className="card-note" style={{ margin: 0 }}>No recipients selected.</p>
          ) : (
            <div className="member-combobox-input" style={{ cursor: 'default' }}>
              {recipients.map((m) => (
                <span key={m.id} className="member-chip">
                  <span className="member-chip-label">{memberLabel(m)}</span>
                  <button
                    type="button"
                    className="member-chip-remove"
                    aria-label={`Remove ${memberLabel(m)}`}
                    onClick={() => removeRecipient(m.id)}
                    disabled={sending}
                  >
                    ×
                  </button>
                </span>
              ))}
            </div>
          )}
          <span className="card-note">
            {pluralize(recipients.length, 'recipient')}
            {skipped > 0 && <> &middot; {pluralize(skipped, 'selected member')} skipped (no email address)</>}
          </span>
        </div>

        <ComposeForm
          subject={subject}
          onSubjectChange={setSubject}
          body={body}
          onBodyChange={setBody}
          isNewsletter={isNewsletter}
          onIsNewsletterChange={setIsNewsletter}
          template={template}
          recipientCount={recipients.length}
          buildRecipients={() => ({ mode: 'specific_members', memberIds: Array.from(recipientIds) })}
          onSent={onSent}
          onSendingChange={setSending}
          secondaryAction={
            <button type="button" className="footer-link" onClick={onClose} disabled={sending}>
              Cancel
            </button>
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
