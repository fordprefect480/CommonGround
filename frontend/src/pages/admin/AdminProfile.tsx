import { useEffect, useState } from 'react'
import { changePassword, fetchMe, updateMe } from '../../api/auth'

type Status = 'idle' | 'loading' | 'saving' | 'saved' | 'error'
type PwStatus = 'idle' | 'saving' | 'saved' | 'error'

export default function AdminProfile() {
  const [email, setEmail] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [isSubscribedToMailingList, setIsSubscribedToMailingList] = useState(true)
  const [status, setStatus] = useState<Status>('loading')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchMe()
      .then((me) => {
        if (!me) {
          setStatus('error')
          setError('Not signed in')
          return
        }
        setEmail(me.email)
        setFirstName(me.firstName ?? '')
        setLastName(me.lastName ?? '')
        setIsSubscribedToMailingList(me.isSubscribedToMailingList)
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
      const me = await updateMe({
        firstName: firstName.trim() || null,
        lastName: lastName.trim() || null,
        isSubscribedToMailingList,
      })
      setFirstName(me.firstName ?? '')
      setLastName(me.lastName ?? '')
      setIsSubscribedToMailingList(me.isSubscribedToMailingList)
      setStatus('saved')
    } catch (err) {
      setStatus('error')
      setError(err instanceof Error ? err.message : 'Save failed')
    }
  }

  if (status === 'loading') return <p className="admin-loading">Loading&hellip;</p>

  const onNameChange = (setter: (v: string) => void) => (e: React.ChangeEvent<HTMLInputElement>) => {
    setter(e.target.value)
    if (status === 'saved') setStatus('idle')
  }

  const onSubscriptionChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setIsSubscribedToMailingList(e.target.checked)
    if (status === 'saved') setStatus('idle')
  }

  return (
    <section className="admin-page" aria-labelledby="profile-heading">
      <header className="admin-page-header">
        <h1 id="profile-heading" className="admin-page-title">Your profile</h1>
      </header>

      {error && <div className="form-error" role="alert">{error}</div>}

      <div className="profile-forms">
        <form className="card admin-form" onSubmit={save}>
        <label className="field">
          <span className="field-label">Email</span>
          <input value={email} readOnly disabled />
        </label>

        <label className="field">
          <span className="field-label">First name</span>
          <input
            value={firstName}
            onChange={onNameChange(setFirstName)}
            maxLength={100}
          />
        </label>

        <label className="field">
          <span className="field-label">Last name</span>
          <input
            value={lastName}
            onChange={onNameChange(setLastName)}
            maxLength={100}
          />
        </label>

        <label className="checkbox-field">
          <input
            type="checkbox"
            checked={isSubscribedToMailingList}
            onChange={onSubscriptionChange}
          />
          <span>Subscribe to mailing list</span>
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

        <ChangePasswordForm />
      </div>
    </section>
  )
}

function ChangePasswordForm() {
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [confirm, setConfirm] = useState('')
  const [status, setStatus] = useState<PwStatus>('idle')
  const [error, setError] = useState<string | null>(null)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    if (next !== confirm) {
      setStatus('error')
      setError('New password and confirmation do not match.')
      return
    }

    setStatus('saving')
    try {
      await changePassword(current, next)
      setCurrent('')
      setNext('')
      setConfirm('')
      setStatus('saved')
    } catch (err) {
      setStatus('error')
      setError(err instanceof Error ? err.message : 'Password change failed')
    }
  }

  const onChange = (setter: (v: string) => void) => (e: React.ChangeEvent<HTMLInputElement>) => {
    setter(e.target.value)
    if (status === 'saved' || status === 'error') {
      setStatus('idle')
      setError(null)
    }
  }

  return (
    <form className="card admin-form" onSubmit={submit}>
      <h2 className="section-title">Change password</h2>

      {error && <div className="form-error" role="alert">{error}</div>}

      <label className="field">
        <span className="field-label">Current password</span>
        <input
          type="password"
          required
          autoComplete="current-password"
          value={current}
          onChange={onChange(setCurrent)}
        />
      </label>

      <label className="field">
        <span className="field-label">New password</span>
        <input
          type="password"
          required
          autoComplete="new-password"
          value={next}
          onChange={onChange(setNext)}
        />
      </label>

      <label className="field">
        <span className="field-label">Confirm new password</span>
        <input
          type="password"
          required
          autoComplete="new-password"
          value={confirm}
          onChange={onChange(setConfirm)}
        />
      </label>

      <div className="admin-actions">
        <button
          type="submit"
          className="primary-button"
          disabled={status === 'saving'}
        >
          {status === 'saving' ? 'Updating…' : 'Update password'}
        </button>
        {status === 'saved' && <span className="admin-whoami">Password updated.</span>}
      </div>
    </form>
  )
}
