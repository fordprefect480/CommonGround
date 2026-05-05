import { useEffect, useState } from 'react'
import { fetchAdminMe, updateAdminMe } from '../../api/auth'

type Status = 'idle' | 'loading' | 'saving' | 'saved' | 'error'

export default function AdminProfile() {
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [status, setStatus] = useState<Status>('loading')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchAdminMe()
      .then((me) => {
        if (!me) {
          setStatus('error')
          setError('Not signed in')
          return
        }
        setEmail(me.email)
        setDisplayName(me.displayName ?? '')
        setStatus('idle')
      })
      .catch((err: unknown) => {
        setStatus('error')
        setError(err instanceof Error ? err.message : 'Failed to load profile')
      })
  }, [])

  const save = async (e: React.FormEvent) => {
    e.preventDefault()
    setStatus('saving')
    setError(null)
    try {
      const trimmed = displayName.trim()
      const me = await updateAdminMe(trimmed.length === 0 ? null : trimmed)
      setDisplayName(me.displayName ?? '')
      setStatus('saved')
    } catch (err) {
      setStatus('error')
      setError(err instanceof Error ? err.message : 'Save failed')
    }
  }

  if (status === 'loading') return <p className="admin-loading">Loading&hellip;</p>

  return (
    <section className="admin-page" aria-labelledby="profile-heading">
      <header className="admin-page-header">
        <h1 id="profile-heading" className="admin-page-title">Your profile</h1>
      </header>

      {error && <div className="form-error" role="alert">{error}</div>}

      <form className="card admin-form" onSubmit={save}>
        <label className="field">
          <span className="field-label">Email</span>
          <input value={email} readOnly disabled />
        </label>

        <label className="field">
          <span className="field-label">Display name</span>
          <input
            value={displayName}
            onChange={(e) => {
              setDisplayName(e.target.value)
              if (status === 'saved') setStatus('idle')
            }}
            maxLength={100}
            placeholder="Shown as the author on blog posts"
          />
        </label>

        <div className="admin-actions">
          <button
            type="submit"
            className="primary-button"
            disabled={status === 'saving'}
          >
            {status === 'saving' ? 'Saving…' : 'Save'}
          </button>
          {status === 'saved' && <span className="admin-whoami">Saved.</span>}
        </div>
      </form>
    </section>
  )
}
