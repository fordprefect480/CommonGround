import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createMember, fetchMembers, fetchMembershipRenewalTarget, type Member } from '../../api/auth'
import { isPaidForRenewalYear } from '../../format'
import MemberEmailModal from './MemberEmailModal'

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

type MembershipStatus = 'paid' | 'notPaid' | 'mailingListOnly'

// "Paid" means the member is paid up for the financial year the next renewal
// covers (renewalTargetUtc - the next 1 July, with the late-join carry-over,
// from the server's MembershipPeriod). For most of the year that's the current
// financial year; within the 8-week renewal window it flags members still
// covered this year but not yet renewed for the year ahead. See
// isPaidForRenewalYear for why coverage is tested against the year's start.
// A null paid-through means the user has never held a membership at all -
// they only joined the mailing list - which is a different thing from a
// member who has paid before and lapsed.
function membershipStatus(member: Member, renewalTargetUtc: string): MembershipStatus {
  if (isPaidForRenewalYear(member.membershipPaidThroughUtc, renewalTargetUtc)) return 'paid'
  return member.membershipPaidThroughUtc ? 'notPaid' : 'mailingListOnly'
}

const MEMBERSHIP_RANK: Record<MembershipStatus, number> = { paid: 0, notPaid: 1, mailingListOnly: 2 }

type MemberFilter = 'all' | MembershipStatus

type SortKey = 'name' | 'email' | 'phone' | 'joinedAt' | 'membership' | 'mailingList'
type SortDir = 'asc' | 'desc'

const collator = new Intl.Collator('en', { sensitivity: 'base' })

function memberName(member: Member): string {
  return member.displayName ?? member.email ?? ''
}

// Ascending order puts paid before not-paid and subscribed before
// unsubscribed, so the first click surfaces the "good" state on top.
function compareMembers(a: Member, b: Member, key: SortKey, renewalTargetUtc: string): number {
  switch (key) {
    case 'name': return collator.compare(memberName(a), memberName(b))
    case 'email': return collator.compare(a.email ?? '', b.email ?? '')
    case 'phone': return collator.compare(a.phoneNumber ?? '', b.phoneNumber ?? '')
    case 'joinedAt': return Date.parse(a.joinedAt) - Date.parse(b.joinedAt)
    case 'membership': return MEMBERSHIP_RANK[membershipStatus(a, renewalTargetUtc)] - MEMBERSHIP_RANK[membershipStatus(b, renewalTargetUtc)]
    case 'mailingList': return Number(b.isSubscribedToMailingList) - Number(a.isSubscribedToMailingList)
  }
}

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
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [emailOpen, setEmailOpen] = useState(false)

  if (members.length === 0) {
    return <p className="admin-empty">No users registered yet.</p>
  }

  const statuses = members.map((m) => membershipStatus(m, renewalTargetUtc))
  const countOf = (status: MembershipStatus) => statuses.filter((s) => s === status).length

  const visible = members.filter((_, i) => filter === 'all' || statuses[i] === filter)

  const toggleFilter = (value: Exclude<MemberFilter, 'all'>) =>
    onFilterChange(filter === value ? 'all' : value)

  const chips: { value: Exclude<MemberFilter, 'all'>; label: string; count: number }[] = [
    { value: 'paid', label: 'Paid', count: countOf('paid') },
    { value: 'notPaid', label: 'Not Yet Paid', count: countOf('notPaid') },
    { value: 'mailingListOnly', label: 'Mailing List Only', count: countOf('mailingListOnly') },
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
          <button type="button" className="primary-button" onClick={() => setEmailOpen(true)}>
            Email selected
          </button>
          <button type="button" className="footer-link" onClick={() => setSelectedIds(new Set())}>
            Clear
          </button>
        </div>
      )}

      <MembersTable
        members={visible}
        renewalTargetUtc={renewalTargetUtc}
        selectedIds={selectedIds}
        onToggleOne={toggleOne}
        allVisibleSelected={allVisibleSelected}
        someVisibleSelected={someVisibleSelected}
        onToggleAllVisible={toggleAllVisible}
      />

      {emailOpen && (
        <MemberEmailModal
          members={selectedMembers}
          onClose={() => setEmailOpen(false)}
          onSent={(result) => {
            setEmailOpen(false)
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
  mailingListOnly: { className: 'pill', label: 'Mailing List Only' },
}

function MembersTable({
  members,
  renewalTargetUtc,
  selectedIds,
  onToggleOne,
  allVisibleSelected,
  someVisibleSelected,
  onToggleAllVisible,
}: {
  members: Member[]
  renewalTargetUtc: string
  selectedIds: Set<string>
  onToggleOne: (id: string) => void
  allVisibleSelected: boolean
  someVisibleSelected: boolean
  onToggleAllVisible: () => void
}) {
  const navigate = useNavigate()
  const [sort, setSort] = useState<{ key: SortKey; dir: SortDir }>({ key: 'name', dir: 'asc' })

  const sorted = useMemo(() => {
    const dir = sort.dir === 'asc' ? 1 : -1
    return [...members].sort((a, b) => dir * compareMembers(a, b, sort.key, renewalTargetUtc))
  }, [members, sort, renewalTargetUtc])

  if (members.length === 0) {
    return <p className="admin-empty">No members match this filter.</p>
  }

  const toggleSort = (key: SortKey) =>
    setSort((prev) => (prev.key === key ? { key, dir: prev.dir === 'asc' ? 'desc' : 'asc' } : { key, dir: 'asc' }))

  const columns: { key: SortKey; label: string }[] = [
    { key: 'name', label: 'Name' },
    { key: 'email', label: 'Email' },
    { key: 'phone', label: 'Phone' },
    { key: 'joinedAt', label: 'Member since' },
    { key: 'membership', label: 'Membership' },
    { key: 'mailingList', label: 'Mailing list' },
  ]

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
            {columns.map(({ key, label }) => (
              <th key={key} scope="col" aria-sort={sort.key === key ? (sort.dir === 'asc' ? 'ascending' : 'descending') : undefined}>
                <button type="button" className="admin-table-sort" onClick={() => toggleSort(key)}>
                  {label}
                  <span className="admin-table-sort-arrow" aria-hidden="true">
                    {sort.key === key ? (sort.dir === 'asc' ? '▲' : '▼') : ''}
                  </span>
                </button>
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {sorted.map((member) => {
            const isAdmin = member.roles.includes(ADMIN_ROLE)
            const pill = MEMBERSHIP_PILL[membershipStatus(member, renewalTargetUtc)]
            const name = memberName(member) || '(no name)'
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
