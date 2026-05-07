import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAppConfig } from '../AppConfigContext'
import { useAuth } from '../AuthContext'
import { login } from '../api/auth'

export default function Login() {
  const { gardenName } = useAppConfig()
  const { refresh } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const next = new URLSearchParams(location.search).get('next')

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await login(email, password)
      const me = await refresh()
      if (!me) {
        setError('Sign-in failed.')
        return
      }
      const destination = chooseDestination(me.isAdmin, next)
      navigate(destination, { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sign-in failed')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="admin-login">
      <form className="card admin-login-card" onSubmit={handleSubmit} noValidate>
        <h1 className="section-title">Sign in</h1>
        <p className="card-note">Sign in to {gardenName}.</p>

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

        {error && <div className="form-error" role="alert">{error}</div>}

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

function chooseDestination(isAdmin: boolean, next: string | null): string {
  if (next && (isAdmin || !next.startsWith('/admin'))) return next
  return isAdmin ? '/admin' : '/profile'
}
