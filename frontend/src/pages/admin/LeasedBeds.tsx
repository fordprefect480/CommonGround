import { useEffect, useMemo, useState } from 'react'
import { getLeasedBedPrice } from '../../api/adminTools'
import { fetchMembers, type Member } from '../../api/auth'
import {
  assignBed,
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

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; overview: LeasedBedsOverview; requests: AdminBedRequests; standardPriceCents: number }

const dateFmt = new Intl.DateTimeFormat('en-AU', { day: 'numeric', month: 'short', year: 'numeric' })
const formatDate = (value: string) => dateFmt.format(new Date(value))

function memberLabel(m: Member): string {
  const name = m.displayName?.trim() || [m.firstName, m.lastName].filter(Boolean).join(' ').trim()
  if (name && m.email) return `${name} (${m.email})`
  return name || m.email || m.id
}

/** Parse a dollar string into whole cents, or return a user-facing error message. */
function parsePriceCents(priceDollars: string): { cents: number } | { error: string } {
  const dollars = Number(priceDollars)
  if (!Number.isFinite(dollars) || dollars < 0) return { error: 'Enter a price of $0 or more.' }
  return { cents: Math.round(dollars * 100) }
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
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [paymentBed, setPaymentBed] = useState<AdminBed | null>(null)
  // Members back the "assign to a member" dropdown. Loaded as part of reloadAll so the list
  // stays fresh and a failure surfaces as the page error rather than a silent empty dropdown.
  // Sorted once here for display rather than per row.
  const [members, setMembers] = useState<Member[]>([])
  const sortedMembers = useMemo(
    () => [...members].sort((a, b) => memberLabel(a).localeCompare(memberLabel(b))),
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

  const handleRelease = async (bed: AdminBed) => {
    if (!bed.currentLease) return
    if (!confirm(`Release bed ${bed.label} from ${bed.currentLease.memberName ?? 'this member'}? This frees the bed.`)) return
    setError(null)
    try {
      await releaseLease(bed.currentLease.leaseId)
      await reloadAll()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not release the lease.')
    }
  }

  const handleRecordPayment = async (amountCents: number) => {
    if (!paymentBed?.currentLease) return
    await recordLeasePayment(paymentBed.currentLease.leaseId, amountCents)
    await reloadAll()
  }

  const handleRemove = async (request: AdminBedRequest) => {
    if (!confirm(`Remove ${request.memberName ?? 'this member'} from the waitlist?`)) return
    setError(null)
    try {
      await removeBedRequest(request.requestId)
      await reloadAll()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not remove the request.')
    }
  }

  const handleEditNote = async (bed: AdminBed) => {
    const next = window.prompt(`Note for bed ${bed.label} (leave blank to clear):`, bed.notes ?? '')
    if (next === null) return
    if (next.length > 500) {
      setError('A bed note must be 500 characters or fewer.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      await updateBed(bed.id, { notes: next })
      await reloadAll()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not save the note.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="admin-page" aria-labelledby="leased-beds-heading">
      <header className="admin-page-header">
        <h1 id="leased-beds-heading" className="admin-page-title">Leased beds</h1>
      </header>

      {state.status === 'loading' && <p className="admin-loading">Loading&hellip;</p>}
      {state.status === 'error' && <div className="form-error" role="alert">{state.message}</div>}
      {error && <div className="form-error" role="alert">{error}</div>}

      {state.status === 'ready' && (() => {
        const availableBeds = state.overview.beds.filter((b) => b.isActive && !b.isOccupied)
        const assign = async (requestId: number, bedId: number, customPriceCents: number) => {
          await assignBed({ requestId, bedId, customPriceCents })
          await reloadAll()
        }
        const assignToMember = async (userId: string, bedId: number, customPriceCents: number) => {
          await assignBed({ userId, bedId, customPriceCents })
          await reloadAll()
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
              onRemove={handleRemove}
            />

            <h2 className="section-title">All beds</h2>
            <div className="admin-table-wrap">
              <table className="admin-table">
                <thead>
                  <tr>
                    <th scope="col">Bed</th>
                    <th scope="col">Notes</th>
                    <th scope="col">Holder</th>
                    <th scope="col">Lease</th>
                    <th scope="col">Payment</th>
                    <th scope="col">Expires</th>
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
                        </td>
                        <td data-label="Notes">
                          {bed.notes
                            ? <span style={{ whiteSpace: 'pre-wrap' }}>{bed.notes}</span>
                            : <span className="card-note">No note</span>}
                          {' '}
                          <button type="button" className="footer-link" onClick={() => handleEditNote(bed)} disabled={busy}>
                            {bed.notes ? 'Edit' : 'Add'}
                          </button>
                        </td>
                        <td data-label="Holder">{lease?.memberName ?? (bed.isActive ? <span className="pill pill-ok">Available</span> : '—')}</td>
                        <td data-label="Lease">
                          {lease ? <>{leaseStatusLabel(lease.status)}{' · '}{formatPrice(lease.priceAtAllocationCents)}</> : '—'}
                        </td>
                        <td data-label="Payment">
                          {lease
                            ? lease.isPaid
                              ? lease.paidOnUtc ? `Paid ${formatDate(lease.paidOnUtc)}` : 'No payment required'
                              : <span className="pill pill-warn">Awaiting payment</span>
                            : '—'}
                        </td>
                        <td data-label="Expires">{lease ? formatDate(lease.expiresOn) : '—'}</td>
                        <td data-label="Actions">
                          {lease ? (
                            <>
                              {lease.status === 'AwaitingPayment' && (
                                <>
                                  <button type="button" className="footer-link" onClick={() => setPaymentBed(bed)} disabled={busy}>Record a manual payment</button>
                                  {' · '}
                                </>
                              )}
                              <button type="button" className="footer-link" onClick={() => handleRelease(bed)} disabled={busy}>Release</button>
                            </>
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
  const [filter, setFilter] = useState('')
  const [userId, setUserId] = useState('')
  const [priceDollars, setPriceDollars] = useState((standardPriceCents / 100).toString())
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const filtered = useMemo(() => {
    const q = filter.trim().toLowerCase()
    if (!q) return members
    return members.filter((m) => memberLabel(m).toLowerCase().includes(q))
  }, [members, filter])

  if (!open) {
    return <button type="button" className="footer-link" onClick={() => setOpen(true)}>Assign member</button>
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
    <div className="field" style={{ gap: 6, minWidth: 220 }}>
      <input
        type="search"
        aria-label="Search members"
        placeholder="Search members"
        value={filter}
        onChange={(e) => setFilter(e.target.value)}
        disabled={busy}
      />
      <select value={userId} onChange={(e) => setUserId(e.target.value)} disabled={busy} aria-label="Member">
        <option value="">Choose a member…</option>
        {filtered.map((m) => (
          <option key={m.id} value={m.id}>{memberLabel(m)}</option>
        ))}
      </select>
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
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
          style={{ maxWidth: 90 }}
        />
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
