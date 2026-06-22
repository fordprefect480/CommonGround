import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'

interface RecordPaymentModalProps {
  open: boolean
  title: string
  description?: string
  /** Pre-filled amount in cents (the standard price); the admin can change it. */
  initialAmountCents: number
  onClose: () => void
  onConfirm: (amountCents: number) => Promise<void>
}

// Admin dialog for recording a manual (cash/bank-transfer) payment. Replaces the
// old confirm() prompt so the admin can enter the amount actually received.
export default function RecordPaymentModal({
  open,
  title,
  description,
  initialAmountCents,
  onClose,
  onConfirm,
}: RecordPaymentModalProps) {
  const [dollars, setDollars] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Reset to the standard amount each time the dialog opens.
  useEffect(() => {
    if (!open) return
    setDollars((initialAmountCents / 100).toString())
    setBusy(false)
    setError(null)
  }, [open, initialAmountCents])

  // Close on Escape.
  useEffect(() => {
    if (!open) return
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [open, onClose])

  if (!open) return null

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    const value = Number(dollars)
    if (!Number.isFinite(value) || value < 0) {
      setError('Enter an amount of $0 or more.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      await onConfirm(Math.round(value * 100))
      onClose()
    } catch (err) {
      setBusy(false)
      setError(err instanceof Error ? err.message : 'Could not record the payment.')
    }
  }

  return createPortal(
    <div role="presentation" onClick={onClose} style={overlayStyle}>
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className="card admin-form"
        onClick={(e) => e.stopPropagation()}
        style={{ width: '100%', maxWidth: 420, margin: 0 }}
      >
        <h2 className="section-title">{title}</h2>
        {description && <p className="card-note" style={{ margin: 0 }}>{description}</p>}

        <form onSubmit={submit} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <label className="field">
            <span className="field-label">Amount received</span>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <span aria-hidden="true">$</span>
              <input
                type="number"
                min="0"
                step="0.01"
                inputMode="decimal"
                autoFocus
                value={dollars}
                onChange={(e) => setDollars(e.target.value)}
                disabled={busy}
                style={{ maxWidth: 140 }}
              />
            </div>
          </label>

          {error && <div className="form-error" role="alert">{error}</div>}

          <div className="admin-actions">
            <button type="submit" className="primary-button" disabled={busy}>
              {busy ? 'Recording…' : 'Record payment'}
            </button>
            <button type="button" className="secondary-button" onClick={onClose} disabled={busy}>
              Cancel
            </button>
          </div>
        </form>
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
