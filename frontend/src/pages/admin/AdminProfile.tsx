import { useEffect, useState } from 'react'
import { changePassword, fetchMe, updateMe } from '../../api/auth'
import { payMembership } from '../../api/membership'
import { useAppConfig } from '../../AppConfigContext'
import PaymentHistoryTable from './PaymentHistoryTable'

type Status = 'idle' | 'loading' | 'saving' | 'saved' | 'error'
type PwStatus = 'idle' | 'saving' | 'saved' | 'error'

export default function AdminProfile() {
  const [email, setEmail] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [phone, setPhone] = useState('')
  const [address, setAddress] = useState('')
  const [secondary, setSecondary] = useState<string[]>([])
  const [paidThrough, setPaidThrough] = useState<string | null>(null)
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
        setPhone(me.phoneNumber ?? '')
        setAddress(me.address ?? '')
        setSecondary(me.secondaryMembers)
        setPaidThrough(me.membershipPaidThroughUtc)
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
        phoneNumber: phone.trim() || null,
        address: address.trim() || null,
        secondaryMembers: secondary.map((s) => s.trim()).filter(Boolean),
        isSubscribedToMailingList,
      })
      setFirstName(me.firstName ?? '')
      setLastName(me.lastName ?? '')
      setPhone(me.phoneNumber ?? '')
      setAddress(me.address ?? '')
      setSecondary(me.secondaryMembers)
      setIsSubscribedToMailingList(me.isSubscribedToMailingList)
      setStatus('saved')
    } catch (err) {
      setStatus('error')
      setError(err instanceof Error ? err.message : 'Save failed')
    }
  }

  if (status === 'loading') return <p className="admin-loading">Loading&hellip;</p>

  const onTextChange = (setter: (v: string) => void) => (e: React.ChangeEvent<HTMLInputElement>) => {
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
        <div className="field">
          <span className="field-label">Email</span>
          <span className="field-readonly">{email}</span>
        </div>

        <label className="field">
          <span className="field-label">First name</span>
          <input
            value={firstName}
            onChange={onTextChange(setFirstName)}
            maxLength={100}
          />
        </label>

        <label className="field">
          <span className="field-label">Last name</span>
          <input
            value={lastName}
            onChange={onTextChange(setLastName)}
            maxLength={100}
          />
        </label>

        <label className="field">
          <span className="field-label">Phone</span>
          <input value={phone} onChange={onTextChange(setPhone)} />
        </label>

        <label className="field">
          <span className="field-label">Address</span>
          <input value={address} onChange={onTextChange(setAddress)} maxLength={300} />
        </label>

        <fieldset className="field" style={{ border: 0, padding: 0, margin: 0 }}>
          <span className="field-label">Additional household members</span>
          {secondary.map((value, i) => (
            <div key={i} style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
              <input
                value={value}
                maxLength={200}
                onChange={(e) => {
                  setSecondary((prev) => prev.map((v, idx) => (idx === i ? e.target.value : v)))
                  if (status === 'saved') setStatus('idle')
                }}
              />
              <button
                type="button"
                className="secondary-button"
                onClick={() => {
                  setSecondary((prev) => prev.filter((_, idx) => idx !== i))
                  if (status === 'saved') setStatus('idle')
                }}
              >
                Remove
              </button>
            </div>
          ))}
          {secondary.length < 4 && (
            <button
              type="button"
              className="secondary-button"
              onClick={() => setSecondary((prev) => [...prev, ''])}
            >
              Add a member
            </button>
          )}
        </fieldset>

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

      <div className="profile-forms">
        <MembershipStatus paidThrough={paidThrough} />

        <section className="card admin-form">
          <h2 className="section-title">Payment history</h2>
          <PaymentHistoryTable />
        </section>
      </div>
    </section>
  )
}

// Membership always runs to a 1 July boundary, which is also the Australian
// financial-year boundary. A paid-through date at or beyond today therefore
// covers the current financial year (same rule as the admin members list).
function currentFinancialYearLabel(now: Date): string {
  const startYear = now.getMonth() >= 6 ? now.getFullYear() : now.getFullYear() - 1
  return `${startYear}/${String(startYear + 1).slice(-2)}`
}

function MembershipStatus({ paidThrough }: { paidThrough: string | null }) {
  const { paymentsEnabled } = useAppConfig()
  const [status, setStatus] = useState<'idle' | 'starting' | 'error'>('idle')
  const [error, setError] = useState<string | null>(null)

  const now = new Date()
  const fyLabel = currentFinancialYearLabel(now)
  const isPaid = paidThrough != null && new Date(paidThrough).getTime() >= now.getTime()

  const startPayment = async () => {
    setStatus('starting')
    setError(null)
    try {
      const { checkoutUrl } = await payMembership()
      window.location.assign(checkoutUrl)
    } catch (err) {
      setStatus('error')
      setError(err instanceof Error ? err.message : 'Could not start payment. Please try again.')
    }
  }

  return (
    <section className="card admin-form">
      <h2 className="section-title">Membership</h2>

      <p className="card-note" style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 8 }}>
        <span className={isPaid ? 'pill pill-ok' : 'pill pill-warn'} style={{ whiteSpace: 'nowrap', flexShrink: 0 }}>
          {isPaid ? 'Paid' : 'Not yet paid'}
        </span>
        <span>for the {fyLabel} financial year</span>
      </p>

      {isPaid ? (
        <p className="card-note" style={{ margin: 0 }}>
          Your membership is valid until {new Date(paidThrough!).toLocaleDateString()}.
        </p>
      ) : (
        <>
          {paidThrough && (
            <p className="card-note" style={{ margin: 0 }}>
              Your last membership ran until {new Date(paidThrough).toLocaleDateString()}.
            </p>
          )}
          {error && <div className="form-error" role="alert">{error}</div>}
          {paymentsEnabled ? (
            <div className="admin-actions">
              <button
                type="button"
                className="primary-button"
                onClick={startPayment}
                disabled={status === 'starting'}
              >
                {status === 'starting' ? 'Starting payment…' : 'Pay membership'}
              </button>
            </div>
          ) : (
            <p className="card-note" style={{ margin: 0 }}>
              Online payments are currently unavailable. Please check back soon to pay your membership.
            </p>
          )}
        </>
      )}
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
