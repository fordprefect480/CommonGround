import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { fetchSentEmail, type SentEmailRecipient } from '../../api/email'

interface RecipientsPopoverProps {
  emailId: number
  /** Bounding rect of the trigger, in viewport coordinates. */
  anchor: DOMRect
  onClose: () => void
}

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; recipients: SentEmailRecipient[] }

const WIDTH = 300
const GAP = 4
const MARGIN = 8

// Floating panel listing every recipient of a sent email. Anchored below the
// "+N more" trigger and lazy-loads the addresses so the list endpoint stays light.
export default function RecipientsPopover({ emailId, anchor, onClose }: RecipientsPopoverProps) {
  const [state, setState] = useState<State>({ status: 'loading' })
  const ref = useRef<HTMLDivElement>(null)
  const [pos, setPos] = useState({ top: anchor.bottom + GAP, left: anchor.left })

  useEffect(() => {
    let cancelled = false
    fetchSentEmail(emailId)
      .then((detail) => { if (!cancelled) setState({ status: 'ready', recipients: detail.recipients }) })
      .catch((err: unknown) => {
        if (!cancelled) setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load' })
      })
    return () => { cancelled = true }
  }, [emailId])

  // Clamp into the viewport once the panel has a measured height.
  useLayoutEffect(() => {
    const el = ref.current
    if (!el) return
    const { height } = el.getBoundingClientRect()
    const left = Math.max(MARGIN, Math.min(anchor.left, window.innerWidth - WIDTH - MARGIN))
    let top = anchor.bottom + GAP
    if (top + height > window.innerHeight - MARGIN) {
      top = Math.max(MARGIN, anchor.top - GAP - height)
    }
    setPos({ top, left })
  }, [anchor, state])

  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [onClose])

  return createPortal(
    <div role="presentation" onClick={onClose} style={overlayStyle}>
      <div
        ref={ref}
        role="dialog"
        aria-modal="true"
        aria-label="Recipients"
        className="card recipients-popover"
        onClick={(e) => e.stopPropagation()}
        style={{ top: pos.top, left: pos.left, width: WIDTH }}
      >
        {state.status === 'loading' && <p className="admin-loading">Loading&hellip;</p>}
        {state.status === 'error' && <div className="form-error" role="alert">{state.message}</div>}
        {state.status === 'ready' && (
          <>
            <p className="recipients-popover-count">
              {state.recipients.length} {state.recipients.length === 1 ? 'recipient' : 'recipients'}
            </p>
            <ul className="recipients-popover-list">
              {state.recipients.map((r) => (
                <li key={r.id} className={r.status === 'failed' ? 'recipients-popover-failed' : undefined}>
                  <span>{r.email}</span>
                  {r.status === 'failed' && <span className="pill pill-warn">Failed</span>}
                </li>
              ))}
            </ul>
          </>
        )}
      </div>
    </div>,
    document.body,
  )
}

const overlayStyle: React.CSSProperties = {
  position: 'fixed',
  inset: 0,
  zIndex: 1000,
}
