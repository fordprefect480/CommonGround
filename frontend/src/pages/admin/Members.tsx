import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createMember, fetchMembers, fetchMembershipRenewalTarget, type Member } from '../../api/auth'
import { useAppConfig } from '../../AppConfigContext'
import { membershipPaidThroughFyLabel } from '../../format'
import PaymentReminderModal from './PaymentReminderModal'

const ADMIN_ROLE = 'Admin'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; members: Member[]; renewalTargetUtc: string }

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

const dateFormatter = new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })

function formatJoinedAt(iso: string): string {
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '-' : dateFormatter.format(d)
}

function pluralize(n: number, singular: string): string {
  return `${n} ${singular}${n === 1 ? '' : 's'}`
}

type MembershipStatus = 'paid' | 'notPaid'

// "Paid" means the member has paid through the upcoming renewal boundary
// (the next 1 July, with the late-join carry-over - see the server's
// MembershipPeriod, surfaced as renewalTargetUtc). For most of the year that
// boundary is the current financial year's, so this matches the obvious
// reading; within the 8-week renewal window it additionally flags members who
// are still covered this year but haven't renewed for the year ahead.
function membershipStatus(member: Member, renewalTargetMs: number): MembershipStatus {
  if (!member.membershipPaidThroughUtc) return 'notPaid'
  const paidThrough = new Date(member.membershipPaidThroughUtc).getTime()
  if (Number.isNaN(paidThrough)) return 'notPaid'
  return paidThrough >= renewalTargetMs ? 'paid' : 'notPaid'
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
      const [members, renewalTargetUtc] = await Promise.all([
        fetchMembers(),
        fetchMembershipRenewalTarget(),
      ])
      setState({ status: 'ready', members, renewalTargetUtc })
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
        <h1 id="members-heading" className="admin-page-title">Members</h1>
        {!showForm && (
          <div className="admin-actions">
            <button
              type="button"
              className="primary-button"
              onClick={() => window.location.assign('/api/admin/members/export.xlsx')}
              disabled={state.status !== 'ready' || state.members.length === 0}
            >
              Export
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
        <MembersList
          members={state.members}
          renewalTargetUtc={state.renewalTargetUtc}
          filter={filter}
          onFilterChange={setFilter}
        />
      )}
    </section>
  )
}

function MembersList({
  members,
  renewalTargetUtc,
  filter,
  onFilterChange,
}: {
  members: Member[]
  renewalTargetUtc: string
  filter: MemberFilter
  onFilterChange: (filter: MemberFilter) => void
}) {
  const navigate = useNavigate()
  const { gardenName, membershipPriceCents, paymentsEnabled } = useAppConfig()
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [reminderOpen, setReminderOpen] = useState(false)

  if (members.length === 0) {
    return <p className="admin-empty">No users registered yet.</p>
  }

  const renewalMs = new Date(renewalTargetUtc).getTime()
  const statuses = members.map((m) => membershipStatus(m, renewalMs))
  const paidCount = statuses.filter((s) => s === 'paid').length
  const notPaidCount = statuses.filter((s) => s === 'notPaid').length

  const visible = members.filter((_, i) => filter === 'all' || statuses[i] === filter)

  const toggleFilter = (value: Exclude<MemberFilter, 'all'>) =>
    onFilterChange(filter === value ? 'all' : value)

  const chips: { value: Exclude<MemberFilter, 'all'>; label: string; count: number }[] = [
    { value: 'paid', label: 'Paid', count: paidCount },
    { value: 'notPaid', label: 'Not Yet Paid', count: notPaidCount },
  ]

  const toggleOne = (id: string) =>
    setSelectedIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })

  const allVisibleSelected = visible.length > 0 && visible.every((m) => selectedIds.has(m.id))
  const someVisibleSelected = visible.some((m) => selectedIds.has(m.id))

  const toggleAllVisible = () =>
    setSelectedIds((prev) => {
      const next = new Set(prev)
      if (allVisibleSelected) visible.forEach((m) => next.delete(m.id))
      else visible.forEach((m) => next.add(m.id))
      return next
    })

  const selectedMembers = members.filter((m) => selectedIds.has(m.id))

  return (
    <>
      <div className="filter-chips" role="group" aria-label="Filter members by membership status">
        {chips.map((chip) => (
          <button
            key={chip.value}
            type="button"
            className={`filter-chip${filter === chip.value ? ' filter-chip-selected' : ''}`}
            aria-pressed={filter === chip.value}
            onClick={() => toggleFilter(chip.value)}
          >
            {chip.label} <span className="filter-chip-count">{chip.count}</span>
          </button>
        ))}
      </div>

      {selectedIds.size > 0 && (
        <div className="admin-actions" style={{ marginBottom: '1rem' }}>
          <span className="card-note">{pluralize(selectedIds.size, 'member')} selected</span>
          <button type="button" className="primary-button" onClick={() => setReminderOpen(true)}>
            Email selected
          </button>
          <button type="button" className="footer-link" onClick={() => setSelectedIds(new Set())}>
            Clear
          </button>
        </div>
      )}

      <MembersTable
        members={visible}
        renewalMs={renewalMs}
        selectedIds={selectedIds}
        onToggleOne={toggleOne}
        allVisibleSelected={allVisibleSelected}
        someVisibleSelected={someVisibleSelected}
        onToggleAllVisible={toggleAllVisible}
      />

      {reminderOpen && (
        <PaymentReminderModal
          members={selectedMembers}
          gardenName={gardenName}
          membershipPriceCents={membershipPriceCents}
          paymentsEnabled={paymentsEnabled}
          fyLabel={membershipPaidThroughFyLabel(renewalTargetUtc)}
          membershipUrl={`${window.location.origin}/membership`}
          onClose={() => setReminderOpen(false)}
          onSent={(result) => {
            setReminderOpen(false)
            setSelectedIds(new Set())
            navigate(`/admin/email/${result.id}`)
          }}
        />
      )}
    </>
  )
}

const MEMBERSHIP_PILL: Record<MembershipStatus, { className: string; label: string }> = {
  paid: { className: 'pill pill-ok', label: 'Paid' },
  notPaid: { className: 'pill pill-warn', label: 'Not Yet Paid' },
}

function MembersTable({
  members,
  renewalMs,
  selectedIds,
  onToggleOne,
  allVisibleSelected,
  someVisibleSelected,
  onToggleAllVisible,
}: {
  members: Member[]
  renewalMs: number
  selectedIds: Set<string>
  onToggleOne: (id: string) => void
  allVisibleSelected: boolean
  someVisibleSelected: boolean
  onToggleAllVisible: () => void
}) {
  const navigate = useNavigate()

  if (members.length === 0) {
    return <p className="admin-empty">No members match this filter.</p>
  }

  return (
    <div className="admin-table-wrap">
      <table className="admin-table">
        <thead>
          <tr>
            <th scope="col" className="admin-table-checkbox">
              <input
                type="checkbox"
                aria-label="Select all members shown"
                checked={allVisibleSelected}
                ref={(el) => {
                  if (el) el.indeterminate = someVisibleSelected && !allVisibleSelected
                }}
                onChange={onToggleAllVisible}
              />
            </th>
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
            const pill = MEMBERSHIP_PILL[membershipStatus(member, renewalMs)]
            const name = member.displayName ?? member.email ?? '(no name)'
            const open = () => navigate(`/admin/members/${member.id}`)
            const stop = (e: React.SyntheticEvent) => e.stopPropagation()
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
                aria-label={`View membership details for ${name}`}
              >
                <td
                  className="admin-table-checkbox"
                  data-label="Select"
                  onClick={stop}
                  onKeyDown={stop}
                >
                  <input
                    type="checkbox"
                    aria-label={`Select ${name}`}
                    checked={selectedIds.has(member.id)}
                    onChange={() => onToggleOne(member.id)}
                  />
                </td>
                <td className="admin-card-title" data-label="Name">
                  <span className="admin-table-link">{name}</span>
                  {isAdmin && <span className="pill pill-ok admin-name-badge">Admin</span>}
                </td>
                <td data-label="Email">{member.email ?? '-'}</td>
                <td data-label="Phone">{member.phoneNumber ?? '-'}</td>
                <td data-label="Member since">{formatJoinedAt(member.joinedAt)}</td>
                <td data-label="Membership">
                  <span className={pill.className}>{pill.label}</span>
                </td>
                <td data-label="Mailing list">
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
