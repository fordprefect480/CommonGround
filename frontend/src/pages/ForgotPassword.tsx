import { useState } from 'react'
import { Link } from 'react-router-dom'
import Seo from '../Seo'
import { useAppConfig } from '../AppConfigContext'
import { requestPasswordReset } from '../api/auth'

export default function ForgotPassword() {
  const { gardenName } = useAppConfig()
  const [email, setEmail] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [sent, setSent] = useState(false)

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await requestPasswordReset(email.trim())
      setSent(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="admin-login">
      <Seo title="Forgot password" noindex />
      <div className="card admin-login-card">
        <img
          src="/swcg/logo-apple.png"
          width="56"
          height="66"
          alt=""
          className="admin-login-logo"
        />
        <h1 className="section-title">Forgot your password?</h1>

        {sent ? (
          <>
            <div className="form-success" role="status">
              If an account exists for that email, we've sent a link to reset your password.
              Check your inbox (and spam folder).
            </div>
            <p className="card-note">
              <Link to="/login" className="footer-link">Back to sign in</Link>
            </p>
          </>
        ) : (
          <form onSubmit={handleSubmit} noValidate>
            <p className="card-note">
              Enter the email address for your {gardenName} account and we'll send you a link to
              reset your password.
            </p>

            <label className="field">
              <span className="field-label">Email</span>
              <input
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </label>

            {error && <div className="form-error" role="alert">{error}</div>}

            <button type="submit" className="primary-button" disabled={submitting}>
              {submitting ? 'Sending…' : 'Send reset link'}
            </button>

            <p className="card-note">
              <Link to="/login" className="footer-link">Back to sign in</Link>
            </p>
          </form>
        )}
      </div>
    </main>
  )
}
