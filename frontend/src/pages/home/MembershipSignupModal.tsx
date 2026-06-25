import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { Link, useNavigate } from 'react-router-dom'
import { MSButton } from './Primitives'
import { useAppConfig } from '../../AppConfigContext'
import { formatMembershipPrice } from '../../format'
import { signup } from '../../api/membership'
import { loadTurnstileScript } from '../../turnstile'

interface MembershipSignupModalProps {
  open: boolean
  onClose: () => void
}

const MAX_SECONDARY = 4

export function MembershipSignupModal({ open, onClose }: MembershipSignupModalProps) {
  const navigate = useNavigate()
  const config = useAppConfig()
  const priceLabel = formatMembershipPrice(config.membershipPriceCents)
  const captchaSiteKey = config.turnstileSiteKey ?? null
  const captchaContainerRef = useRef<HTMLDivElement | null>(null)
  const captchaWidgetIdRef = useRef<string | null>(null)
  const [captchaToken, setCaptchaToken] = useState<string | null>(null)

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [address, setAddress] = useState('')
  const [password, setPassword] = useState('')
  const [secondary, setSecondary] = useState<string[]>([])
  const [subscribe, setSubscribe] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setFirstName('')
    setLastName('')
    setEmail('')
    setPhone('')
    setAddress('')
    setPassword('')
    setSecondary([])
    setSubscribe(true)
    setSubmitting(false)
    setError(null)
    setCaptchaToken(null)
  }, [open])

  useEffect(() => {
    if (!open) return
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [open, onClose])

  useEffect(() => {
    if (!open || !captchaSiteKey) return
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
  }, [open, captchaSiteKey])

  if (!open) return null

  const captchaReady = !captchaSiteKey || captchaToken !== null

  const updateSecondary = (index: number, value: string) => {
    setSecondary((prev) => prev.map((v, i) => (i === index ? value : v)))
  }
  const addSecondary = () => setSecondary((prev) => (prev.length < MAX_SECONDARY ? [...prev, ''] : prev))
  const removeSecondary = (index: number) => setSecondary((prev) => prev.filter((_, i) => i !== index))

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (submitting) return
    if (captchaSiteKey && !captchaToken) {
      setError('Please complete the captcha before submitting.')
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      const { checkoutUrl } = await signup({
        firstName,
        lastName,
        email,
        phoneNumber: phone.trim() || null,
        address: address.trim() || null,
        password,
        secondaryMembers: secondary.map((s) => s.trim()).filter(Boolean),
        subscribeNewsletter: subscribe,
        captchaToken: captchaToken ?? undefined,
      })
      if (checkoutUrl) {
        // Payments are on: hand off to the Stripe-hosted checkout page.
        window.location.assign(checkoutUrl)
      } else {
        // Payments are off: the account was created and we're signed in - send them to
        // their profile, where they can pay once payments are turned back on.
        navigate('/profile')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not sign up. Please try again.')
      const id = captchaWidgetIdRef.current
      if (id && window.turnstile) {
        try {
          window.turnstile.reset(id)
        } catch {
          // widget gone - no-op
        }
      }
      setCaptchaToken(null)
      setSubmitting(false)
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
        alignItems: 'flex-start',
        justifyContent: 'center',
        padding: 20,
        overflowY: 'auto',
        background: 'rgba(20, 20, 16, 0.55)',
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Become a member"
        onClick={(e) => e.stopPropagation()}
        style={{
          position: 'relative',
          width: '100%',
          maxWidth: 480,
          margin: 'auto',
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
            Become a member
          </h3>
          <p style={{ fontSize: 15, lineHeight: 1.5, color: 'var(--fg-2)', margin: 0 }}>
            Membership is {priceLabel}/year. You&rsquo;ll be taken to a secure payment page to finish.
          </p>

          <div style={{ display: 'flex', gap: 10 }}>
            <Field label="First name" style={{ flex: 1 }}>
              <input required value={firstName} onChange={(e) => setFirstName(e.target.value)} maxLength={100} style={modalInput} disabled={submitting} />
            </Field>
            <Field label="Last name" style={{ flex: 1 }}>
              <input required value={lastName} onChange={(e) => setLastName(e.target.value)} maxLength={100} style={modalInput} disabled={submitting} />
            </Field>
          </div>

          <Field label="Email">
            <input required type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="sam@example.com" style={modalInput} disabled={submitting} />
          </Field>

          <Field label="Phone">
            <input value={phone} onChange={(e) => setPhone(e.target.value)} style={modalInput} disabled={submitting} />
          </Field>

          <Field label="Address">
            <input value={address} onChange={(e) => setAddress(e.target.value)} maxLength={300} style={modalInput} disabled={submitting} />
          </Field>

          <Field label="Password">
            <input required type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} style={modalInput} disabled={submitting} />
          </Field>

          <div>
            <div style={{ fontSize: 12, fontWeight: 600, marginBottom: 5, color: 'var(--ink-900)' }}>
              Additional household members (optional)
            </div>
            {secondary.map((value, i) => (
              <div key={i} style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
                <input
                  value={value}
                  onChange={(e) => updateSecondary(i, e.target.value)}
                  placeholder="Full name"
                  maxLength={200}
                  style={modalInput}
                  disabled={submitting}
                />
                <button
                  type="button"
                  onClick={() => removeSecondary(i)}
                  aria-label="Remove member"
                  style={{ border: '1px solid var(--ink-200)', background: 'var(--paper)', borderRadius: 'var(--r-md)', cursor: 'pointer', padding: '0 12px' }}
                  disabled={submitting}
                >
                  ×
                </button>
              </div>
            ))}
            {secondary.length < MAX_SECONDARY && (
              <button
                type="button"
                onClick={addSecondary}
                style={{ border: 'none', background: 'none', color: 'var(--apple-700)', cursor: 'pointer', fontSize: 14, padding: 0 }}
                disabled={submitting}
              >
                + Add a member
              </button>
            )}
          </div>

          <label style={{ display: 'flex', gap: 8, alignItems: 'center', fontSize: 14, color: 'var(--fg-2)' }}>
            <input type="checkbox" checked={subscribe} onChange={(e) => setSubscribe(e.target.checked)} disabled={submitting} />
            <span>Subscribe me to the garden newsletter</span>
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
              {renderSignupError(error, onClose)}
            </div>
          )}

          <MSButton type="submit" size="lg" disabled={submitting || !captchaReady} style={{ marginTop: 4, alignSelf: 'flex-start' }}>
            {submitting ? 'Starting payment…' : 'Continue to payment'}
          </MSButton>
        </form>
      </div>
    </div>,
    document.body,
  )
}

// Render the signup error, turning a trailing "please sign in" into a link to the login page
// (e.g. the "You already have a membership - please sign in." conflict from the server).
function renderSignupError(message: string, onClose: () => void): React.ReactNode {
  const marker = 'please sign in'
  const idx = message.toLowerCase().indexOf(marker)
  if (idx === -1) return message
  return (
    <>
      {message.slice(0, idx)}
      <Link to="/login" onClick={onClose} style={{ color: 'inherit', fontWeight: 600, textDecoration: 'underline' }}>
        {message.slice(idx, idx + marker.length)}
      </Link>
      {message.slice(idx + marker.length)}
    </>
  )
}

function Field({ label, style, children }: { label: string; style?: React.CSSProperties; children: React.ReactNode }) {
  return (
    <label style={style}>
      <div style={{ fontSize: 12, fontWeight: 600, marginBottom: 5, color: 'var(--ink-900)' }}>{label}</div>
      {children}
    </label>
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
