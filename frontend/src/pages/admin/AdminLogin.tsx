import { useState } from 'react'
import { Link } from 'react-router-dom'
import { login } from '../../api/auth'
import { useAppConfig } from '../../AppConfigContext'

interface AdminLoginProps {
  onLoggedIn: () => Promise<void>
}

export default function AdminLogin({ onLoggedIn }: AdminLoginProps) {
  const { gardenName } = useAppConfig()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await login(email, password)
      await onLoggedIn()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sign-in failed')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="admin-login">
      <form className="card admin-login-card" onSubmit={handleSubmit} noValidate>
        <h1 className="section-title">Admin sign-in</h1>
        <p className="card-note">Sign in to manage {gardenName}.</p>

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

        <label className="field">
          <span className="field-label">Password</span>
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </label>

        {error && (
          <div className="form-error" role="alert">{error}</div>
        )}

        <button type="submit" className="primary-button" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>

        <p className="card-note">
          <Link to="/" className="footer-link">Back to homepage</Link>
        </p>
      </form>
    </main>
  )
}
