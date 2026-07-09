import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from 'react'
import { Link } from 'react-router-dom'
import { getLeasedBedPrice } from '../../api/adminTools'
import { fetchMembers, type Member } from '../../api/auth'
import {
  assignBed,
  type AssignResult,
  fetchBedRequests,
  fetchLeasedBeds,
  recordLeasePayment,
  releaseLease,
  removeBedRequest,
  updateBed,
  type AdminBed,
  type AdminBedRequest,
  type AdminBedRequests,
  type BedLeaseStatus,
  type LeasedBedsOverview,
} from '../../api/leasedBeds'
import { formatPrice } from '../../format'
import RecordPaymentModal from './RecordPaymentModal'
import PromptModal from './PromptModal'
import ConfirmModal from './ConfirmModal'

interface PendingConfirm {
  title: string
  message: string
  confirmLabel: string
  onConfirm: () => Promise<void>
}

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; overview: LeasedBedsOverview; requests: AdminBedRequests; standardPriceCents: number }

const dateFmt = new Intl.DateTimeFormat('en-AU', { day: 'numeric', month: 'short', year: 'numeric' })
const formatDate = (value: string) => dateFmt.format(new Date(value))

function memberName(m: Member): string {
  return m.displayName?.trim() || [m.firstName, m.lastName].filter(Boolean).join(' ').trim() || m.email || m.id
}

function memberLabel(m: Member): string {
  const name = m.displayName?.trim() || [m.firstName, m.lastName].filter(Boolean).join(' ').trim()
  if (name && m.email) return `${name} (${m.email})`
  return name || m.email || m.id
}

// Cap the suggestion list so a long membership doesn't render hundreds of rows;
// the filter narrows things down well before this bites.
const MEMBER_SUGGESTION_LIMIT = 50

/** Parse a dollar string into whole cents, or return a user-facing error message. */
function parsePriceCents(priceDollars: string): { cents: number } | { error: string } {
  const dollars = Number(priceDollars)
  if (!Number.isFinite(dollars) || dollars < 0) return { error: 'Enter a price of $0 or more.' }
  return { cents: Math.round(dollars * 100) }
}

const iconProps = {
  width: 15,
  height: 15,
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 2,
  strokeLinecap: 'round',
  strokeLinejoin: 'round',
  'aria-hidden': true,
} as const

function PaymentIcon() {
  return (
    <svg {...iconProps}>
      <line x1="12" y1="1" x2="12" y2="23" />
      <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
    </svg>
  )
}

function ReleaseIcon() {
  return (
    <svg {...iconProps}>
      <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
      <circle cx="8.5" cy="7" r="4" />
      <line x1="23" y1="11" x2="17" y2="11" />
    </svg>
  )
}

function AssignIcon() {
  return (
    <svg {...iconProps}>
      <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
      <circle cx="8.5" cy="7" r="4" />
      <line x1="20" y1="8" x2="20" y2="14" />
      <line x1="23" y1="11" x2="17" y2="11" />
    </svg>
  )
}

function WheelchairIcon({ title = 'Wheelchair-accessible' }: { title?: string }) {
  return (
    <svg {...iconProps} aria-hidden={undefined} role="img" aria-label={title}>
      <title>{title}</title>
      <circle cx="9" cy="4" r="1.6" />
      <path d="M9 6v6h5l3 6" />
      <path d="M14 12a5 5 0 1 1-5 5" />
    </svg>
  )
}

function leaseStatusLabel(status: BedLeaseStatus): string {
  switch (status) {
    case 'AwaitingPayment':
      return 'Awaiting payment'
    case 'Active':
      return 'Active'
    case 'Expired':
      return 'Expired'
    case 'Released':
      return 'Released'
  }
}

export default function LeasedBeds() {
  const [state, setState] = useState<State>({ status: 'loading' })
  const [paymentBed, setPaymentBed] = useState<AdminBed | null>(null)
  const [noteBed, setNoteBed] = useState<AdminBed | null>(null)
  const [pendingConfirm, setPendingConfirm] = useState<PendingConfirm | null>(null)
  const [toast, setToast] = useState<string | null>(null)
  // Members back the "assign to a member" dropdown. Loaded as part of reloadAll so the list
  // stays fresh and a failure surfaces as the page error rather than a silent empty dropdown.
  // Sorted once here for display rather than per row. Mailing-list-only members (never held a
  // membership) are excluded - only actual members can hold a bed.
  const [members, setMembers] = useState<Member[]>([])
  const sortedMembers = useMemo(
    () =>
      members
        .filter((m) => m.membershipPaidThroughUtc !== null)
        .sort((a, b) => memberLabel(a).localeCompare(memberLabel(b))),
    [members],
  )

  const reloadAll = async () => {
    const [overview, requests, price, members] = await Promise.all([
      fetchLeasedBeds(),
      fetchBedRequests(),
      getLeasedBedPrice(),
      fetchMembers(),
    ])
    setState({ status: 'ready', overview, requests, standardPriceCents: price.priceCents })
    setMembers(members)
  }

  useEffect(() => {
    reloadAll().catch((err: unknown) =>
      setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load leased beds' }),
    )
  }, [])

  // Auto-dismiss the success toast so it doesn't linger.
  useEffect(() => {
    if (!toast) return
    const timer = setTimeout(() => setToast(null), 6000)
    return () => clearTimeout(timer)
  }, [toast])

  const announceAssignment = (result: AssignResult, bedId: number) => {
    const label = result.overview.beds.find((b) => b.id === bedId)?.label ?? `#${bedId}`
    const emailNote = result.emailSent ? ' A confirmation email has been sent to them.' : ''
    setToast(`Bed ${label} assigned to ${result.memberName}.${emailNote}`)
  }

  const requestRelease = (bed: AdminBed) => {
    const lease = bed.currentLease
    if (!lease) return
    setPendingConfirm({
      title: 'Release bed',
      message: `Release bed ${bed.label} from ${lease.memberName ?? 'this member'}? This frees the bed.`,
      confirmLabel: 'Release bed',
      onConfirm: async () => {
        await releaseLease(lease.leaseId)
        await reloadAll()
      },
    })
  }

  const handleRecordPayment = async (amountCents: number) => {
    if (!paymentBed?.currentLease) return
    await recordLeasePayment(paymentBed.currentLease.leaseId, amountCents)
    await reloadAll()
  }

  const requestRemove = (request: AdminBedRequest) => {
    setPendingConfirm({
      title: 'Remove from waitlist',
      message: `Remove ${request.memberName ?? 'this member'} from the waitlist?`,
      confirmLabel: 'Remove',
      onConfirm: async () => {
        await removeBedRequest(request.requestId)
        await reloadAll()
      },
    })
  }

  const handleSaveNote = async (value: string) => {
    if (!noteBed) return
    await updateBed(noteBed.id, { notes: value })
    await reloadAll()
  }

  return (
    <section className="admin-page" aria-labelledby="leased-beds-heading">
      <header className="admin-page-header">
        <h1 id="leased-beds-heading" className="admin-page-title">Leased beds</h1>
      </header>

      {state.status === 'loading' && <p className="admin-loading">Loading&hellip;</p>}
      {state.status === 'error' && <div className="form-error" role="alert">{state.message}</div>}

      {state.status === 'ready' && (() => {
        const availableBeds = state.overview.beds.filter((b) => b.isActive && !b.isOccupied)
        const assign = async (requestId: number, bedId: number, customPriceCents: number) => {
          const result = await assignBed({ requestId, bedId, customPriceCents })
          await reloadAll()
          announceAssignment(result, bedId)
        }
        const assignToMember = async (userId: string, bedId: number, customPriceCents: number) => {
          const result = await assignBed({ userId, bedId, customPriceCents })
          await reloadAll()
          announceAssignment(result, bedId)
        }
        return (
          <>
            <p className="admin-empty">
              {state.overview.capacity.leased} of {state.overview.capacity.total} beds leased
              {' · '}
              {state.overview.capacity.remaining} available
            </p>

            <PendingSection
              requests={state.requests.pending}
              availableBeds={availableBeds}
              standardPriceCents={state.standardPriceCents}
              onAssign={assign}
            />

            <WaitlistSection
              requests={state.requests.waitlist}
              availableBeds={availableBeds}
              standardPriceCents={state.standardPriceCents}
              onAssign={assign}
              onRemove={requestRemove}
            />

            <h2 className="section-title">All beds</h2>
            <div className="admin-table-wrap">
              <table className="admin-table admin-table--beds">
                <thead>
                  <tr>
                    <th scope="col">Bed</th>
                    <th scope="col">Holder</th>
                    <th scope="col">Lease</th>
                    <th scope="col">Payment</th>
                    <th scope="col">Expires</th>
                    <th scope="col">Notes</th>
                    <th scope="col">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {state.overview.beds.map((bed) => {
                    const lease = bed.currentLease
                    return (
                      <tr key={bed.id} className={bed.isActive ? undefined : 'admin-table-row-muted'}>
                        <td data-label="Bed">
                          <strong>{bed.label}</strong>
                          {!bed.isActive && <> <span className="pill">Out of service</span></>}
                          {bed.isWheelchairAccessible && <> <WheelchairIcon /></>}
                        </td>
                        <td data-label="Holder" data-empty={!lease && !bed.isActive ? '' : undefined}>
                          {lease
                            ? <Link to={`/admin/members/${lease.memberId}`} className="admin-table-link">{lease.memberName ?? lease.memberId}</Link>
                            : (bed.isActive ? <span className="pill pill-ok">Available</span> : '—')}
                        </td>
                        <td data-label="Lease" data-empty={lease ? undefined : ''}>
                          {lease ? <>{leaseStatusLabel(lease.status)}{' · '}{formatPrice(lease.priceAtAllocationCents)}</> : '—'}
                        </td>
                        <td data-label="Payment" data-empty={lease ? undefined : ''}>
                          {lease
                            ? lease.isPaid
                              ? lease.paidOnUtc ? `Paid ${formatDate(lease.paidOnUtc)}` : 'No payment required'
                              : <span className="pill pill-warn">Awaiting payment</span>
                            : '—'}
                        </td>
                        <td data-label="Expires" data-empty={lease ? undefined : ''}>{lease ? formatDate(lease.expiresOn) : '—'}</td>
                        <td data-label="Notes" className="admin-note-cell">
                          <span className="admin-note-value">
                            {bed.notes
                              ? <span style={{ whiteSpace: 'pre-wrap' }}>{bed.notes}</span>
                              : <span className="card-note">No note</span>}
                            {' '}
                            <button type="button" className="footer-link" onClick={() => setNoteBed(bed)}>
                              {bed.notes ? 'Edit' : 'Add'}
                            </button>
                          </span>
                        </td>
                        <td data-label="Actions">
                          {lease ? (
                            <div className="admin-cell-actions">
                              {lease.status === 'AwaitingPayment' && (
                                <button type="button" className="footer-link icon-link" onClick={() => setPaymentBed(bed)}>
                                  <PaymentIcon />Record manual payment
                                </button>
                              )}
                              <button type="button" className="footer-link icon-link" onClick={() => requestRelease(bed)}>
                                <ReleaseIcon />Release bed
                              </button>
                            </div>
                          ) : (
                            bed.isActive ? (
                              <BedAssignControls
                                bed={bed}
                                members={sortedMembers}
                                standardPriceCents={state.standardPriceCents}
                                onAssign={assignToMember}
                              />
                            ) : (
                              <span className="card-note">—</span>
                            )
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </>
        )
      })()}

      <RecordPaymentModal
        open={paymentBed !== null}
        title="Record a manual payment"
        description={paymentBed ? `Record a cash or bank-transfer payment for bed ${paymentBed.label}.` : undefined}
        initialAmountCents={paymentBed?.currentLease?.priceAtAllocationCents ?? 0}
        onClose={() => setPaymentBed(null)}
        onConfirm={handleRecordPayment}
      />

      <PromptModal
        open={noteBed !== null}
        title={noteBed?.notes ? `Edit note for bed ${noteBed.label}` : `Add a note to bed ${noteBed?.label ?? ''}`}
        label="Note"
        description="Leave blank to clear the note."
        initialValue={noteBed?.notes ?? ''}
        placeholder="e.g. drip line needs repair"
        multiline
        maxLength={500}
        confirmLabel="Save note"
        onClose={() => setNoteBed(null)}
        onConfirm={handleSaveNote}
      />

      <ConfirmModal
        open={pendingConfirm !== null}
        title={pendingConfirm?.title ?? ''}
        message={pendingConfirm?.message}
        confirmLabel={pendingConfirm?.confirmLabel}
        onCancel={() => setPendingConfirm(null)}
        onConfirm={pendingConfirm?.onConfirm ?? (() => {})}
      />

      {toast && (
        <div className="toast toast-success" role="status" aria-live="polite">
          <span className="toast-message">{toast}</span>
          <button type="button" className="toast-close" aria-label="Dismiss" onClick={() => setToast(null)}>×</button>
        </div>
      )}
    </section>
  )
}

function PendingSection({
  requests,
  availableBeds,
  standardPriceCents,
  onAssign,
}: {
  requests: AdminBedRequest[]
  availableBeds: AdminBed[]
  standardPriceCents: number
  onAssign: (requestId: number, bedId: number, customPriceCents: number) => Promise<void>
}) {
  return (
    <div className="card">
      <h2 className="section-title">Pending applications</h2>
      {requests.length === 0 ? (
        <p className="card-note">No pending applications.</p>
      ) : (
        requests.map((r) => (
          <div key={r.requestId} className="field" style={{ borderTop: '1px solid var(--hairline, #e5e5e5)', paddingTop: 12 }}>
            <strong>{r.memberName ?? r.memberEmail ?? r.memberId}</strong>
            {r.memberEmail && <span className="card-note"> · {r.memberEmail}</span>}
            {r.requiresWheelchairAccessible && <> <WheelchairIcon title="Needs a wheelchair-accessible bed" /></>}
            <AssignControls
              requestId={r.requestId}
              availableBeds={availableBeds}
              standardPriceCents={standardPriceCents}
              onAssign={onAssign}
            />
          </div>
        ))
      )}
    </div>
  )
}

function WaitlistSection({
  requests,
  availableBeds,
  standardPriceCents,
  onAssign,
  onRemove,
}: {
  requests: AdminBedRequest[]
  availableBeds: AdminBed[]
  standardPriceCents: number
  onAssign: (requestId: number, bedId: number, customPriceCents: number) => Promise<void>
  onRemove: (request: AdminBedRequest) => void
}) {
  return (
    <div className="card">
      <h2 className="section-title">Waitlist</h2>
      {requests.length === 0 ? (
        <p className="card-note">The waitlist is empty.</p>
      ) : (
        requests.map((r) => (
          <div key={r.requestId} className="field" style={{ borderTop: '1px solid var(--hairline, #e5e5e5)', paddingTop: 12 }}>
            <strong>#{r.position} · {r.memberName ?? r.memberEmail ?? r.memberId}</strong>
            {r.memberEmail && <span className="card-note"> · {r.memberEmail}</span>}
            {r.requiresWheelchairAccessible && <> <WheelchairIcon title="Needs a wheelchair-accessible bed" /></>}
            <div className="admin-actions" style={{ marginTop: 4 }}>
              <button type="button" className="secondary-button" onClick={() => onRemove(r)}>Remove</button>
            </div>
            {availableBeds.length > 0 && (
              <AssignControls
                requestId={r.requestId}
                availableBeds={availableBeds}
                standardPriceCents={standardPriceCents}
                onAssign={onAssign}
              />
            )}
          </div>
        ))
      )}
    </div>
  )
}

// Inline "assign to a member" control shown in an available bed's Actions cell.
// Lets an admin give the bed to any existing member directly (no application/waitlist needed).
// `members` is expected pre-sorted (the parent sorts once for all rows).
function BedAssignControls({
  bed,
  members,
  standardPriceCents,
  onAssign,
}: {
  bed: AdminBed
  members: Member[]
  standardPriceCents: number
  onAssign: (userId: string, bedId: number, customPriceCents: number) => Promise<void>
}) {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [userId, setUserId] = useState('')
  const [comboOpen, setComboOpen] = useState(false)
  const [dropUp, setDropUp] = useState(false)
  const [highlight, setHighlight] = useState(0)
  const [priceDollars, setPriceDollars] = useState((standardPriceCents / 100).toString())
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const comboRef = useRef<HTMLDivElement | null>(null)
  const inputRef = useRef<HTMLInputElement | null>(null)

  const suggestions = useMemo(() => {
    const q = query.trim().toLowerCase()
    const matches = q ? members.filter((m) => memberLabel(m).toLowerCase().includes(q)) : members
    return matches.slice(0, MEMBER_SUGGESTION_LIMIT)
  }, [members, query])

  // Reset the keyboard highlight to the top whenever the visible list changes.
  useEffect(() => {
    setHighlight(0)
  }, [query, comboOpen])

  // Close the suggestion list on an outside click, matching the recipient
  // combobox in the email composer.
  useEffect(() => {
    if (!comboOpen) return
    const handler = (e: MouseEvent) => {
      if (comboRef.current && !comboRef.current.contains(e.target as Node)) setComboOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [comboOpen])

  if (!open) {
    return <button type="button" className="footer-link icon-link" onClick={() => setOpen(true)}><AssignIcon />Assign bed</button>
  }

  // The beds table lives in a horizontally-scrollable wrapper, which (per the
  // CSS overflow spec) also clips vertically - a downward list on a low row is
  // cut off. Open upward when there isn't ~a list's height of room below.
  const openList = () => {
    const wrap = comboRef.current?.closest('.admin-table-wrap')
    const inputRect = inputRef.current?.getBoundingClientRect()
    if (wrap && inputRect) {
      setDropUp(wrap.getBoundingClientRect().bottom - inputRect.bottom < 260)
    }
    setComboOpen(true)
  }

  const selectMember = (m: Member) => {
    setUserId(m.id)
    setQuery(memberLabel(m))
    setComboOpen(false)
    setError(null)
  }

  const onComboKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      openList()
      setHighlight((h) => (suggestions.length === 0 ? 0 : (h + 1) % suggestions.length))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      openList()
      setHighlight((h) => (suggestions.length === 0 ? 0 : (h - 1 + suggestions.length) % suggestions.length))
    } else if (e.key === 'Enter') {
      if (comboOpen && suggestions.length > 0) {
        e.preventDefault()
        selectMember(suggestions[Math.min(highlight, suggestions.length - 1)])
      }
    } else if (e.key === 'Escape') {
      if (comboOpen) {
        e.preventDefault()
        setComboOpen(false)
      }
    }
  }

  const submit = async () => {
    if (!userId) {
      setError('Choose a member.')
      return
    }
    const price = parsePriceCents(priceDollars)
    if ('error' in price) {
      setError(price.error)
      return
    }
    setBusy(true)
    setError(null)
    try {
      // On success the page reloads and this row gains a lease, so the control unmounts.
      await onAssign(userId, bed.id, price.cents)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not assign the bed.')
      setBusy(false)
    }
  }

  return (
    <div className="field bed-assign" style={{ gap: 6, minWidth: 200 }}>
      <div className="member-combobox" ref={comboRef}>
        <div
          className="member-combobox-input"
          onClick={() => { inputRef.current?.focus(); openList() }}
          role="presentation"
        >
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => { setQuery(e.target.value); setUserId(''); setComboOpen(true) }}
            onFocus={() => { openList(); inputRef.current?.select() }}
            onKeyDown={onComboKeyDown}
            placeholder="Search members…"
            aria-label="Member"
            autoComplete="off"
            disabled={busy}
            role="combobox"
            aria-expanded={comboOpen}
            aria-autocomplete="list"
          />
        </div>
        {comboOpen && suggestions.length > 0 && (
          <ul className={`member-suggestions${dropUp ? ' drop-up' : ''}`} role="listbox">
            {suggestions.map((m, i) => (
              <li
                key={m.id}
                role="option"
                aria-selected={i === highlight}
                className={`member-suggestion${i === highlight ? ' is-highlighted' : ''}`}
                onMouseDown={(e) => { e.preventDefault(); selectMember(m) }}
                onMouseEnter={() => setHighlight(i)}
              >
                <span className="member-suggestion-name">{memberName(m)}</span>
                {m.email && memberName(m) !== m.email && (
                  <span className="member-suggestion-email">{m.email}</span>
                )}
              </li>
            ))}
          </ul>
        )}
        {comboOpen && suggestions.length === 0 && (
          <p className="member-suggestions-empty">No matching members.</p>
        )}
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'nowrap' }}>
        <span className="bed-assign-price-field">
          <span className="bed-assign-price-prefix" aria-hidden="true">$</span>
          <input
            className="bed-assign-price"
            type="number"
            min="0"
            max="999"
            step="1"
            inputMode="numeric"
            aria-label="Lease price"
            value={priceDollars}
            onChange={(e) => setPriceDollars(e.target.value.slice(0, 3))}
            disabled={busy}
          />
        </span>
        <button type="button" className="primary-button" onClick={submit} disabled={busy}>
          {busy ? 'Assigning…' : 'Assign'}
        </button>
        <button type="button" className="footer-link" onClick={() => { setOpen(false); setError(null) }} disabled={busy}>Cancel</button>
      </div>
      {error && <span className="form-error" role="alert">{error}</span>}
    </div>
  )
}

function AssignControls({
  requestId,
  availableBeds,
  standardPriceCents,
  onAssign,
}: {
  requestId: number
  availableBeds: AdminBed[]
  standardPriceCents: number
  onAssign: (requestId: number, bedId: number, customPriceCents: number) => Promise<void>
}) {
  const [bedId, setBedId] = useState<number | ''>(availableBeds[0]?.id ?? '')
  const [priceDollars, setPriceDollars] = useState((standardPriceCents / 100).toString())
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (availableBeds.length === 0) {
    return <p className="card-note" style={{ margin: '4px 0 0' }}>No beds available to assign.</p>
  }

  const submit = async () => {
    if (bedId === '') {
      setError('Choose a bed.')
      return
    }
    const price = parsePriceCents(priceDollars)
    if ('error' in price) {
      setError(price.error)
      return
    }
    setBusy(true)
    setError(null)
    try {
      await onAssign(requestId, bedId, price.cents)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not assign the bed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap', marginTop: 8 }}>
      <select value={bedId} onChange={(e) => setBedId(e.target.value === '' ? '' : Number(e.target.value))} disabled={busy}>
        {availableBeds.map((b) => (
          <option key={b.id} value={b.id}>{b.label}</option>
        ))}
      </select>
      <span aria-hidden="true">$</span>
      <input
        type="number"
        min="0"
        step="1"
        inputMode="decimal"
        aria-label="Lease price"
        value={priceDollars}
        onChange={(e) => setPriceDollars(e.target.value)}
        disabled={busy}
        style={{ maxWidth: 100 }}
      />
      <button type="button" className="primary-button" onClick={submit} disabled={busy}>
        {busy ? 'Assigning…' : 'Assign bed'}
      </button>
      {error && <span className="form-error" role="alert" style={{ width: '100%' }}>{error}</span>}
    </div>
  )
}
