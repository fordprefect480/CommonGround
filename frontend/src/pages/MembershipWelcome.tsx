import { useEffect, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import Seo from '../Seo'
import { useAuth } from '../AuthContext'
import { completeCheckout } from '../api/membership'

type State = 'working' | 'done' | 'error'

export default function MembershipWelcome() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const { refresh } = useAuth()
  const [state, setState] = useState<State>('working')
  const [error, setError] = useState<string | null>(null)
  const started = useRef(false)

  useEffect(() => {
    if (started.current) return
    started.current = true

    const sessionId = searchParams.get('session_id')
    if (!sessionId) {
      setState('error')
      setError('Missing payment session.')
      return
    }

    completeCheckout(sessionId)
      .then(() => refresh())
      .then(() => {
        setState('done')
        navigate('/profile', { replace: true })
      })
      .catch((err: unknown) => {
        setState('error')
        setError(err instanceof Error ? err.message : 'Could not complete your signup.')
      })
  }, [searchParams, navigate, refresh])

  return (
    <main className="admin-login">
      <Seo title="Welcome" noindex />
      <div className="card admin-login-card">
        {state === 'error' ? (
          <>
            <h1 className="section-title">Something went wrong</h1>
            <p className="card-note">{error}</p>
            <p className="card-note">
              If you were charged, please contact us at <a href="mailto:seafordcg@gmail.com">seafordcg@gmail.com</a>.
            </p>
          </>
        ) : (
          <>
            <h1 className="section-title">Finishing your membership…</h1>
            <p className="card-note">One moment while we confirm your payment.</p>
          </>
        )}
      </div>
    </main>
  )
}
