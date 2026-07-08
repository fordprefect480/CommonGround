import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'

interface PromptModalProps {
  open: boolean
  title: string
  description?: string
  /** Label shown above the input. */
  label: string
  /** Pre-filled value when the dialog opens. */
  initialValue?: string
  placeholder?: string
  /** Render a textarea instead of a single-line input. */
  multiline?: boolean
  maxLength?: number
  inputMode?: 'text' | 'numeric' | 'url'
  confirmLabel?: string
  onClose: () => void
  /** Return an error string to block submission, or null/undefined to allow it. */
  validate?: (value: string) => string | null | undefined
  onConfirm: (value: string) => void | Promise<void>
}

// Admin dialog for collecting a single text value. Replaces window.prompt so the
// input works consistently (native prompts are blocked in some embedded browsers)
// and matches the rest of the admin dialogs, e.g. RecordPaymentModal.
export default function PromptModal({
  open,
  title,
  description,
  label,
  initialValue = '',
  placeholder,
  multiline = false,
  maxLength,
  inputMode = 'text',
  confirmLabel = 'Save',
  onClose,
  validate,
  onConfirm,
}: PromptModalProps) {
  const [value, setValue] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement | HTMLTextAreaElement | null>(null)

  // Reset to the starting value each time the dialog opens, then focus the field.
  useEffect(() => {
    if (!open) return
    setValue(initialValue)
    setBusy(false)
    setError(null)
    const id = requestAnimationFrame(() => inputRef.current?.focus())
    return () => cancelAnimationFrame(id)
  }, [open, initialValue])

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
    const validationError = validate?.(value)
    if (validationError) {
      setError(validationError)
      return
    }
    setBusy(true)
    setError(null)
    try {
      await onConfirm(value)
      onClose()
    } catch (err) {
      setBusy(false)
      setError(err instanceof Error ? err.message : 'Something went wrong. Please try again.')
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
            <span className="field-label">{label}</span>
            {multiline ? (
              <textarea
                ref={inputRef as React.Ref<HTMLTextAreaElement>}
                rows={4}
                value={value}
                placeholder={placeholder}
                maxLength={maxLength}
                onChange={(e) => setValue(e.target.value)}
                disabled={busy}
              />
            ) : (
              <input
                ref={inputRef as React.Ref<HTMLInputElement>}
                type="text"
                inputMode={inputMode}
                value={value}
                placeholder={placeholder}
                maxLength={maxLength}
                onChange={(e) => setValue(e.target.value)}
                disabled={busy}
              />
            )}
          </label>

          {error && <div className="form-error" role="alert">{error}</div>}

          <div className="admin-actions">
            <button type="submit" className="primary-button" disabled={busy}>
              {busy ? 'Saving…' : confirmLabel}
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
