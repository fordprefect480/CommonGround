import { useEffect } from 'react'
import { createPortal } from 'react-dom'

interface AdminModalProps {
  /** Accessible name for the dialog. */
  ariaLabel: string
  onClose: () => void
  /** When true, an overlay click or Escape won't close the dialog (e.g. mid-send). */
  closeDisabled?: boolean
  children: React.ReactNode
}

/**
 * Centered modal dialog rendered into document.body. Closes on overlay click or
 * Escape unless `closeDisabled` is set. Shared by the admin composer modals.
 */
export default function AdminModal({ ariaLabel, onClose, closeDisabled = false, children }: AdminModalProps) {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !closeDisabled) onClose()
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [onClose, closeDisabled])

  return createPortal(
    <div role="presentation" onClick={() => { if (!closeDisabled) onClose() }} style={overlayStyle}>
      <div
        role="dialog"
        aria-modal="true"
        aria-label={ariaLabel}
        className="card admin-form"
        onClick={(e) => e.stopPropagation()}
        style={{ width: '100%', maxWidth: 680, margin: 0, maxHeight: '90vh', overflowY: 'auto' }}
      >
        {children}
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
