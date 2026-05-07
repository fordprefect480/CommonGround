import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { createMember, fetchMembers, type Member } from '../../api/auth'

const ADMIN_ROLE = 'Admin'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; members: Member[] }

interface FormState {
  email: string
  firstName: string
  lastName: string
  phoneNumber: string
  password: string
  isAdmin: boolean
}

const EMPTY_FORM: FormState = {
  email: '',
  firstName: '',
  lastName: '',
  phoneNumber: '',
  password: '',
  isAdmin: false,
}

const dateFormatter = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' })

function formatJoinedAt(iso: string): string {
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '—' : dateFormatter.format(d)
}

export default function Members() {
  const [state, setState] = useState<State>({ status: 'loading' })
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState<FormState>(EMPTY_FORM)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const reload = async () => {
    setState({ status: 'loading' })
    try {
      const members = await fetchMembers()
      setState({ status: 'ready', members })
    } catch (err) {
      setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load members' })
    }
  }

  useEffect(() => {
    reload()
  }, [])

  const updateForm = (patch: Partial<FormState>) => setForm((prev) => ({ ...prev, ...patch }))

  const cancelAdd = () => {
    setShowForm(false)
    setForm(EMPTY_FORM)
    setFormError(null)
  }

  const submitAdd = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setFormError(null)
    try {
      await createMember({
        email: form.email.trim(),
        firstName: form.firstName.trim() || null,
        lastName: form.lastName.trim() || null,
        phoneNumber: form.phoneNumber.trim() || null,
        password: form.password,
        isAdmin: form.isAdmin,
      })
      cancelAdd()
      await reload()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Create failed')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="admin-page" aria-labelledby="members-heading">
      <header className="admin-page-header">
        <h1 id="members-heading" className="admin-page-title">Members</h1>
        {!showForm && (
          <div className="admin-actions">
            <button
              type="button"
              className="primary-button"
              onClick={() => window.location.assign('/api/admin/members/export.xlsx')}
              disabled={state.status !== 'ready' || state.members.length === 0}
            >
              Export to XLSX
            </button>
            <button type="button" className="primary-button" onClick={() => setShowForm(true)}>
              Add user
            </button>
          </div>
        )}
      </header>

      {showForm && (
        <form className="card admin-form" onSubmit={submitAdd}>
          {formError && <div className="form-error" role="alert">{formError}</div>}

          <label className="field">
            <span className="field-label">Email</span>
            <input
              type="email"
              required
              autoComplete="off"
              value={form.email}
              onChange={(e) => updateForm({ email: e.target.value })}
            />
          </label>

          <label className="field">
            <span className="field-label">First name</span>
            <input
              value={form.firstName}
              onChange={(e) => updateForm({ firstName: e.target.value })}
              maxLength={100}
            />
          </label>

          <label className="field">
            <span className="field-label">Last name</span>
            <input
              value={form.lastName}
              onChange={(e) => updateForm({ lastName: e.target.value })}
              maxLength={100}
            />
          </label>

          <label className="field">
            <span className="field-label">Phone</span>
            <input
              type="tel"
              autoComplete="off"
              value={form.phoneNumber}
              onChange={(e) => updateForm({ phoneNumber: e.target.value })}
            />
          </label>

          <label className="field">
            <span className="field-label">Initial password</span>
            <input
              type="password"
              required
              autoComplete="new-password"
              value={form.password}
              onChange={(e) => updateForm({ password: e.target.value })}
            />
          </label>

          <label className="checkbox-field">
            <input
              type="checkbox"
              checked={form.isAdmin}
              onChange={(e) => updateForm({ isAdmin: e.target.checked })}
            />
            <span>Make administrator</span>
          </label>

          <div className="admin-actions">
            <button type="submit" className="primary-button" disabled={submitting}>
              {submitting ? 'Creating…' : 'Create user'}
            </button>
            <button type="button" className="footer-link" disabled={submitting} onClick={cancelAdd}>
              Cancel
            </button>
          </div>
        </form>
      )}

      {state.status === 'loading' && (
        <p className="admin-loading">Loading members&hellip;</p>
      )}

      {state.status === 'error' && (
        <div className="form-error" role="alert">{state.message}</div>
      )}

      {state.status === 'ready' && (
        <MembersTable members={state.members} />
      )}
    </section>
  )
}

function MembersTable({ members }: { members: Member[] }) {
  if (members.length === 0) {
    return <p className="admin-empty">No users registered yet.</p>
  }

  return (
    <div className="admin-table-wrap">
      <table className="admin-table">
        <thead>
          <tr>
            <th scope="col">Name</th>
            <th scope="col">Email</th>
            <th scope="col">Phone</th>
            <th scope="col">Member since</th>
            <th scope="col">Is admin</th>
            <th scope="col">Email confirmed</th>
          </tr>
        </thead>
        <tbody>
          {members.map((member) => {
            const isAdmin = member.roles.includes(ADMIN_ROLE)
            return (
              <tr key={member.id}>
                <td>
                  <Link to={`/admin/members/${member.id}`} className="admin-table-link">
                    {member.displayName ?? member.email ?? '(no name)'}
                  </Link>
                </td>
                <td>{member.email ?? '—'}</td>
                <td>{member.phoneNumber ?? '—'}</td>
                <td>{formatJoinedAt(member.joinedAt)}</td>
                <td>
                  <span className={isAdmin ? 'pill pill-ok' : 'pill'}>
                    {isAdmin ? 'Yes' : 'No'}
                  </span>
                </td>
                <td>
                  <span className={member.emailConfirmed ? 'pill pill-ok' : 'pill pill-warn'}>
                    {member.emailConfirmed ? 'Yes' : 'No'}
                  </span>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
