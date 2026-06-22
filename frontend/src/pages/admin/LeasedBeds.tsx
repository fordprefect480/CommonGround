import { useEffect, useState } from 'react'
import { getLeasedBedPrice } from '../../api/adminTools'
import {
  addBed,
  assignBed,
  deleteBed,
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

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; overview: LeasedBedsOverview; requests: AdminBedRequests; standardPriceCents: number }

const dateFmt = new Intl.DateTimeFormat('en-AU', { day: 'numeric', month: 'short', year: 'numeric' })
const formatDate = (value: string) => dateFmt.format(new Date(value))

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
  const [addLabel, setAddLabel] = useState('')
  const [busy, setBusy] = useState(false)

  const reloadAll = async () => {
    const [overview, requests, price] = await Promise.all([
      fetchLeasedBeds(),
      fetchBedRequests(),
      getLeasedBedPrice(),
    ])
    setState({ status: 'ready', overview, requests, standardPriceCents: price.priceCents })
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

  const handleRecordPayment = async (bed: AdminBed) => {
    if (!bed.currentLease) return
    if (!confirm(`Record an offline payment for bed ${bed.label}? This marks the lease active.`)) return
    setError(null)
    try {
      await recordLeasePayment(bed.currentLease.leaseId)
      await reloadAll()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not record the payment.')
    }
  }

  const handleRemove = async (request: AdminBedRequest) => {
    if (!confirm(`Remove ${request.memberName ?? 'this member'} from the waiting list?`)) return
    setError(null)
    try {
      await removeBedRequest(request.requestId)
      await reloadAll()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not remove the request.')
    }
  }

  const handleAddBed = async () => {
    const trimmed = addLabel.trim()
    if (!trimmed) {
      setError('Enter a label for the new bed.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      await addBed({ label: trimmed })
      setAddLabel('')
      await reloadAll()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not add the bed.')
    } finally {
      setBusy(false)
    }
  }

  const handleToggleService = async (bed: AdminBed) => {
    setBusy(true)
    setError(null)
    try {
      await updateBed(bed.id, { isActive: !bed.isActive })
      await reloadAll()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not update the bed.')
    } finally {
      setBusy(false)
    }
  }

  const handleDelete = async (bed: AdminBed) => {
    if (!confirm(`Delete bed ${bed.label}? It will be removed from the list; any past lease records are kept.`)) return
    setBusy(true)
    setError(null)
    try {
      await deleteBed(bed.id)
      await reloadAll()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not delete the bed.')
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
            <div className="field" style={{ display: 'flex', flexDirection: 'row', alignItems: 'center', gap: 8, flexWrap: 'wrap', marginBottom: 12 }}>
              <input
                type="text"
                aria-label="New bed label"
                placeholder="Label, e.g. N1 or X2039"
                value={addLabel}
                onChange={(e) => setAddLabel(e.target.value)}
                disabled={busy}
                maxLength={50}
                style={{ maxWidth: 220 }}
              />
              <button type="button" className="primary-button" onClick={handleAddBed} disabled={busy}>
                Add bed
              </button>
            </div>
            <div className="admin-table-wrap">
              <table className="admin-table">
                <thead>
                  <tr>
                    <th scope="col">Bed</th>
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
                                  <button type="button" className="footer-link" onClick={() => handleRecordPayment(bed)} disabled={busy}>Record payment</button>
                                  {' · '}
                                </>
                              )}
                              <button type="button" className="footer-link" onClick={() => handleRelease(bed)} disabled={busy}>Release</button>
                            </>
                          ) : (
                            <>
                              <button type="button" className="footer-link" onClick={() => handleToggleService(bed)} disabled={busy}>
                                {bed.isActive ? 'Take out of service' : 'Return to service'}
                              </button>
                              {' · '}
                              <button type="button" className="footer-link" onClick={() => handleDelete(bed)} disabled={busy}>Delete</button>
                            </>
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
      <h2 className="section-title">Waiting list</h2>
      {requests.length === 0 ? (
        <p className="card-note">The waiting list is empty.</p>
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
    const dollars = Number(priceDollars)
    if (!Number.isFinite(dollars) || dollars < 0) {
      setError('Enter a price of $0 or more.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      await onAssign(requestId, bedId, Math.round(dollars * 100))
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
