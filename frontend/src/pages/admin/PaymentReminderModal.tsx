import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'
import type { Member } from '../../api/auth'
import type { SendNewsletterResult } from '../../api/email'
import { ComposeForm, memberLabel, pluralize, useEmailTemplate } from './emailComposer'
import { buildReminderDraft } from './paymentReminder'

interface PaymentReminderModalProps {
  /** The members selected in the table. Those without an email are skipped. */
  members: Member[]
  gardenName: string
  membershipPriceCents: number
  paymentsEnabled: boolean
  fyLabel: string
  membershipUrl: string
  onClose: () => void
  onSent: (result: SendNewsletterResult) => void
}

function hasEmail(m: Member): boolean {
  return !!(m.email && m.email.trim().length > 0)
}

/**
 * Modal composer for a membership payment reminder. Rendered only while open
 * (the parent conditionally mounts it), pre-filled with a renewal draft the
 * admin edits before sending to the selected members.
 */
export default function PaymentReminderModal({
  members,
  gardenName,
  membershipPriceCents,
  paymentsEnabled,
  fyLabel,
  membershipUrl,
  onClose,
  onSent,
}: PaymentReminderModalProps) {
  const template = useEmailTemplate()

  const eligible = members.filter(hasEmail)
  const skipped = members.length - eligible.length

  const [recipientIds, setRecipientIds] = useState<Set<string>>(() => new Set(eligible.map((m) => m.id)))
  const [draft] = useState(() =>
    buildReminderDraft({ gardenName, fyLabel, priceCents: membershipPriceCents, paymentsEnabled, membershipUrl }),
  )
  const [subject, setSubject] = useState(draft.subject)
  const [body, setBody] = useState(draft.bodyHtml)
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
        aria-label="Send payment reminder"
        className="card admin-form"
        onClick={(e) => e.stopPropagation()}
        style={{ width: '100%', maxWidth: 680, margin: 0, maxHeight: '90vh', overflowY: 'auto' }}
      >
        <h2 className="section-title">Send payment reminder</h2>

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
