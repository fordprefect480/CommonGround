import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import Seo from '../Seo'
import { resetPassword } from '../api/auth'

export default function ResetPassword() {
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const email = params.get('email') ?? ''
  const code = params.get('code') ?? ''
  const linkValid = email.length > 0 && code.length > 0

  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [done, setDone] = useState(false)

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    if (password !== confirm) {
      setError('New password and confirmation do not match.')
      return
    }

    setSubmitting(true)
    try {
      await resetPassword(email, code, password)
      setDone(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not reset your password. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="admin-login">
      <Seo title="Reset password" noindex />
      <div className="card admin-login-card">
        <img
          src="/swcg/logo-apple.png"
          width="56"
          height="66"
          alt=""
          className="admin-login-logo"
        />
        <h1 className="section-title">Choose a new password</h1>

        {!linkValid ? (
          <>
            <div className="form-error" role="alert">
              This reset link is incomplete or invalid. Please request a new one.
            </div>
            <p className="card-note">
              <Link to="/forgot-password" className="footer-link">Request a new link</Link>
            </p>
          </>
        ) : done ? (
          <>
            <div className="form-success" role="status">
              Your password has been reset. You can now sign in with your new password.
            </div>
            <button
              type="button"
              className="primary-button"
              onClick={() => navigate('/login', { replace: true })}
            >
              Go to sign in
            </button>
          </>
        ) : (
          <form onSubmit={handleSubmit} noValidate>
            <p className="card-note">Resetting the password for {email}.</p>

            <label className="field">
              <span className="field-label">New password</span>
              <input
                type="password"
                autoComplete="new-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </label>

            <label className="field">
              <span className="field-label">Confirm new password</span>
              <input
                type="password"
                autoComplete="new-password"
                value={confirm}
                onChange={(e) => setConfirm(e.target.value)}
                required
              />
            </label>

            {error && <div className="form-error" role="alert">{error}</div>}

            <button type="submit" className="primary-button" disabled={submitting}>
              {submitting ? 'Resetting…' : 'Reset password'}
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
