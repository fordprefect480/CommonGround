import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'

interface ConfirmModalProps {
  open: boolean
  title: string
  message?: string
  confirmLabel?: string
  cancelLabel?: string
  onCancel: () => void
  onConfirm: () => void | Promise<void>
}

// Admin yes/no dialog. Replaces window.confirm so confirmations work consistently
// (native dialogs are blocked in some embedded browsers) and match the other
// admin dialogs, e.g. RecordPaymentModal and PromptModal.
export default function ConfirmModal({
  open,
  title,
  message,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  onCancel,
  onConfirm,
}: ConfirmModalProps) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setBusy(false)
    setError(null)
  }, [open])

  // Close on Escape.
  useEffect(() => {
    if (!open) return
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onCancel()
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [open, onCancel])

  if (!open) return null

  const confirm = async () => {
    setBusy(true)
    setError(null)
    try {
      await onConfirm()
      onCancel()
    } catch (err) {
      setBusy(false)
      setError(err instanceof Error ? err.message : 'Something went wrong. Please try again.')
    }
  }

  return createPortal(
    <div role="presentation" onClick={onCancel} style={overlayStyle}>
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className="card admin-form"
        onClick={(e) => e.stopPropagation()}
        style={{ width: '100%', maxWidth: 420, margin: 0 }}
      >
        <h2 className="section-title">{title}</h2>
        {message && <p className="card-note" style={{ margin: 0 }}>{message}</p>}

        {error && <div className="form-error" role="alert">{error}</div>}

        <div className="admin-actions">
          <button type="button" className="primary-button" onClick={confirm} disabled={busy} autoFocus>
            {busy ? 'Working…' : confirmLabel}
          </button>
          <button type="button" className="secondary-button" onClick={onCancel} disabled={busy}>
            {cancelLabel}
          </button>
        </div>
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
