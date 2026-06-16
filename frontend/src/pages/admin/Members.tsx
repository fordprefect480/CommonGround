import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
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
  isSubscribedToMailingList: boolean
}

const EMPTY_FORM: FormState = {
  email: '',
  firstName: '',
  lastName: '',
  phoneNumber: '',
  password: '',
  isAdmin: false,
  isSubscribedToMailingList: true,
}

const dateFormatter = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' })

function formatJoinedAt(iso: string): string {
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '-' : dateFormatter.format(d)
}

type MembershipStatus = 'paid' | 'notPaid'

// "Paid" means the member has paid their membership fee for the current
// financial year. Membership always runs to a 1 July boundary (see the
// server's MembershipPeriod), so a paid-through date in the future
// necessarily covers the current financial year. Everything else — never
// paid, or paid only through a past financial year — is "not yet paid".
function membershipStatus(member: Member, now: number): MembershipStatus {
  if (!member.membershipPaidThroughUtc) return 'notPaid'
  const paidThrough = new Date(member.membershipPaidThroughUtc).getTime()
  if (Number.isNaN(paidThrough)) return 'notPaid'
  return paidThrough >= now ? 'paid' : 'notPaid'
}

type MemberFilter = 'all' | 'paid' | 'notPaid'

export default function Members() {
  const [state, setState] = useState<State>({ status: 'loading' })
  const [filter, setFilter] = useState<MemberFilter>('all')
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
        isSubscribedToMailingList: form.isSubscribedToMailingList,
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
        <h1 id="members-heading" className="admin-page-title">Membership</h1>
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

          <label className="checkbox-field">
            <input
              type="checkbox"
              checked={form.isSubscribedToMailingList}
              onChange={(e) => updateForm({ isSubscribedToMailingList: e.target.checked })}
            />
            <span>Subscribe to mailing list</span>
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
        <MembersList members={state.members} filter={filter} onFilterChange={setFilter} />
      )}
    </section>
  )
}

function MembersList({
  members,
  filter,
  onFilterChange,
}: {
  members: Member[]
  filter: MemberFilter
  onFilterChange: (filter: MemberFilter) => void
}) {
  if (members.length === 0) {
    return <p className="admin-empty">No users registered yet.</p>
  }

  const now = Date.now()
  const statuses = members.map((m) => membershipStatus(m, now))
  const paidCount = statuses.filter((s) => s === 'paid').length
  const notPaidCount = statuses.filter((s) => s === 'notPaid').length

  const visible = members.filter((_, i) => filter === 'all' || statuses[i] === filter)

  const toggle = (value: Exclude<MemberFilter, 'all'>) =>
    onFilterChange(filter === value ? 'all' : value)

  const chips: { value: Exclude<MemberFilter, 'all'>; label: string; count: number }[] = [
    { value: 'paid', label: 'Paid', count: paidCount },
    { value: 'notPaid', label: 'Not Yet Paid', count: notPaidCount },
  ]

  return (
    <>
      <div className="filter-chips" role="group" aria-label="Filter members by membership status">
        {chips.map((chip) => (
          <button
            key={chip.value}
            type="button"
            className={`filter-chip${filter === chip.value ? ' filter-chip-selected' : ''}`}
            aria-pressed={filter === chip.value}
            onClick={() => toggle(chip.value)}
          >
            {chip.label} <span className="filter-chip-count">{chip.count}</span>
          </button>
        ))}
      </div>

      <MembersTable members={visible} now={now} />
    </>
  )
}

const MEMBERSHIP_PILL: Record<MembershipStatus, { className: string; label: string }> = {
  paid: { className: 'pill pill-ok', label: 'Paid' },
  notPaid: { className: 'pill pill-warn', label: 'Not Yet Paid' },
}

function MembersTable({ members, now }: { members: Member[]; now: number }) {
  const navigate = useNavigate()

  if (members.length === 0) {
    return <p className="admin-empty">No members match this filter.</p>
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
            <th scope="col">Membership</th>
            <th scope="col">Mailing list</th>
          </tr>
        </thead>
        <tbody>
          {members.map((member) => {
            const isAdmin = member.roles.includes(ADMIN_ROLE)
            const pill = MEMBERSHIP_PILL[membershipStatus(member, now)]
            const open = () => navigate(`/admin/members/${member.id}`)
            return (
              <tr
                key={member.id}
                className="admin-table-row-clickable"
                onClick={open}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault()
                    open()
                  }
                }}
                tabIndex={0}
                role="link"
                aria-label={`View membership details for ${member.displayName ?? member.email ?? 'member'}`}
              >
                <td>
                  <span className="admin-table-link">
                    {member.displayName ?? member.email ?? '(no name)'}
                  </span>
                  {isAdmin && <span className="pill pill-ok admin-name-badge">Admin</span>}
                </td>
                <td>{member.email ?? '-'}</td>
                <td>{member.phoneNumber ?? '-'}</td>
                <td>{formatJoinedAt(member.joinedAt)}</td>
                <td>
                  <span className={pill.className}>{pill.label}</span>
                </td>
                <td>
                  <span className={member.isSubscribedToMailingList ? 'pill pill-ok' : 'pill'}>
                    {member.isSubscribedToMailingList ? 'Subscribed' : 'Unsubscribed'}
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
