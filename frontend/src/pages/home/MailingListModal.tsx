import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { MSButton } from './Primitives'
import { useAppConfig } from '../../AppConfigContext'
import { subscribeToMailingList } from '../../api/subscribe'
import { loadTurnstileScript } from '../../turnstile'

interface MailingListModalProps {
  open: boolean
  onClose: () => void
}

export function MailingListModal({ open, onClose }: MailingListModalProps) {
  const config = useAppConfig()
  const captchaSiteKey = config.turnstileSiteKey ?? null
  const captchaContainerRef = useRef<HTMLDivElement | null>(null)
  const captchaWidgetIdRef = useRef<string | null>(null)
  const inputRef = useRef<HTMLInputElement | null>(null)
  const [captchaToken, setCaptchaToken] = useState<string | null>(null)
  const [email, setEmail] = useState('')
  const [sent, setSent] = useState(false)
  const [sending, setSending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Reset to a clean state each time the modal is opened.
  useEffect(() => {
    if (!open) return
    setEmail('')
    setSent(false)
    setSending(false)
    setError(null)
    setCaptchaToken(null)
  }, [open])

  // Close on Escape.
  useEffect(() => {
    if (!open) return
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [open, onClose])

  // Focus the email field once the form is showing.
  useEffect(() => {
    if (open && !sent) inputRef.current?.focus()
  }, [open, sent])

  // Render the Turnstile widget while the form is visible.
  useEffect(() => {
    if (!open || !captchaSiteKey || sent) return
    let cancelled = false
    loadTurnstileScript()
      .then((api) => {
        if (cancelled || !captchaContainerRef.current) return
        captchaWidgetIdRef.current = api.render(captchaContainerRef.current, {
          sitekey: captchaSiteKey,
          callback: (token) => setCaptchaToken(token),
          'expired-callback': () => setCaptchaToken(null),
          'error-callback': () => setCaptchaToken(null),
        })
      })
      .catch(() => {
        if (!cancelled) setError('Could not load the captcha widget. Please refresh the page.')
      })
    return () => {
      cancelled = true
      const id = captchaWidgetIdRef.current
      if (id && window.turnstile) {
        try {
          window.turnstile.remove(id)
        } catch {
          // widget gone - no-op
        }
        captchaWidgetIdRef.current = null
      }
    }
  }, [open, captchaSiteKey, sent])

  if (!open) return null

  const captchaReady = !captchaSiteKey || captchaToken !== null

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (sending) return
    if (captchaSiteKey && !captchaToken) {
      setError('Please complete the captcha before submitting.')
      return
    }
    setSending(true)
    setError(null)
    try {
      await subscribeToMailingList({ email, captchaToken: captchaToken ?? undefined })
      setSent(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not subscribe. Please try again.')
      const id = captchaWidgetIdRef.current
      if (id && window.turnstile) {
        try {
          window.turnstile.reset(id)
        } catch {
          // widget gone - no-op
        }
      }
      setCaptchaToken(null)
    } finally {
      setSending(false)
    }
  }

  return createPortal(
    <div
      role="presentation"
      onClick={onClose}
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 1000,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 20,
        background: 'rgba(20, 20, 16, 0.55)',
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Join the mailing list"
        onClick={(e) => e.stopPropagation()}
        style={{
          position: 'relative',
          width: '100%',
          maxWidth: 440,
          background: 'var(--ivory)',
          border: '1px solid var(--ink-200)',
          borderRadius: 'var(--r-lg)',
          padding: 32,
        }}
      >
        <button
          type="button"
          aria-label="Close"
          onClick={onClose}
          style={{
            position: 'absolute',
            top: 14,
            right: 16,
            background: 'none',
            border: 0,
            fontSize: 24,
            lineHeight: 1,
            cursor: 'pointer',
            color: 'var(--fg-3)',
          }}
        >
          ×
        </button>

        {sent ? (
          <div style={{ textAlign: 'center', padding: '12px 0' }}>
            <h3
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 700,
                fontSize: 26,
                textTransform: 'uppercase',
                margin: '0 0 8px',
              }}
            >
              You&rsquo;re subscribed!
            </h3>
            <p style={{ fontSize: 15, color: 'var(--fg-2)', margin: '0 0 20px' }}>
              Thanks for joining our mailing list - we&rsquo;ll keep you in the loop.
            </p>
            <MSButton size="lg" onClick={onClose}>
              Done
            </MSButton>
          </div>
        ) : (
          <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            <h3
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 700,
                fontSize: 26,
                textTransform: 'uppercase',
                margin: 0,
              }}
            >
              Join our mailing list
            </h3>
            <p style={{ fontSize: 15, lineHeight: 1.5, color: 'var(--fg-2)', margin: 0 }}>
              We send out a newsletter every month or so.
            </p>
            <label>
              <div
                style={{
                  fontSize: 12,
                  fontWeight: 600,
                  marginBottom: 5,
                  color: 'var(--ink-900)',
                }}
              >
                Email
              </div>
              <input
                ref={inputRef}
                type="email"
                required
                placeholder="sam@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={sending}
                style={modalInput}
              />
            </label>
            {captchaSiteKey && <div ref={captchaContainerRef} style={{ minHeight: 65 }} />}
            {error && (
              <div
                role="alert"
                style={{
                  fontSize: 14,
                  color: '#9B1B1B',
                  background: '#FBEAEA',
                  border: '1px solid #E8B9B9',
                  borderRadius: 'var(--r-md)',
                  padding: '10px 12px',
                }}
              >
                {error}
              </div>
            )}
            <MSButton
              type="submit"
              size="lg"
              disabled={sending || !captchaReady}
              style={{ marginTop: 4, alignSelf: 'flex-start' }}
            >
              {sending ? 'Subscribing…' : 'Subscribe'}
            </MSButton>
          </form>
        )}
      </div>
    </div>,
    document.body,
  )
}

const modalInput: React.CSSProperties = {
  width: '100%',
  boxSizing: 'border-box',
  fontFamily: 'var(--font-sans)',
  fontSize: 15,
  padding: '11px 14px',
  background: 'var(--paper)',
  border: '1px solid var(--ink-200)',
  borderRadius: 'var(--r-md)',
  color: 'var(--ink-900)',
  outline: 0,
}
