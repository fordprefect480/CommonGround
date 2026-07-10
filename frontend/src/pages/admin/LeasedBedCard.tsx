import { useEffect, useRef, useState } from 'react'
import { useAppConfig } from '../../AppConfigContext'
import {
  applyForBed,
  fetchMyLeasedBedStatus,
  payLease,
  renewLease,
  withdrawBedRequest,
  type MyLease,
  type MyLeasedBedStatus,
  type MyRequestInfo,
} from '../../api/leasedBeds'
import { formatPrice } from '../../format'

const dateFmt = new Intl.DateTimeFormat('en-AU', { day: 'numeric', month: 'short', year: 'numeric' })
const formatDate = (value: string) => dateFmt.format(new Date(value))

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; data: MyLeasedBedStatus }

export default function LeasedBedCard() {
  const { paymentsEnabled } = useAppConfig()
  const [state, setState] = useState<State>({ status: 'loading' })
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [confirming, setConfirming] = useState(false)
  const [cancelledNotice, setCancelledNotice] = useState(false)
  const started = useRef(false)

  useEffect(() => {
    // Run once. StrictMode double-invokes effects in dev; without this guard the first
    // run wipes the payment param and gets torn down, leaving the confirming poll orphaned.
    if (started.current) return
    started.current = true

    const params = new URLSearchParams(window.location.search)
    const payment = params.get('payment')
    if (payment) {
      params.delete('payment')
      const qs = params.toString()
      window.history.replaceState({}, '', window.location.pathname + (qs ? `?${qs}` : ''))
    }

    const fail = (err: unknown) =>
      setState({ status: 'error', message: err instanceof Error ? err.message : 'Could not load your bed status' })

    if (payment === 'cancelled') setCancelledNotice(true)

    if (payment === 'success') {
      // The webhook is the source of truth - poll until the lease flips to Active.
      setConfirming(true)
      let attempts = 0
      const poll = async () => {
        attempts++
        try {
          const data = await fetchMyLeasedBedStatus()
          setState({ status: 'ready', data })
          if (!data.leases.some((l) => l.status === 'AwaitingPayment') || attempts >= 8) {
            setConfirming(false)
            return
          }
        } catch {
          if (attempts >= 8) {
            setConfirming(false)
            return
          }
        }
        setTimeout(poll, 2500)
      }
      poll()
    } else {
      fetchMyLeasedBedStatus()
        .then((data) => setState({ status: 'ready', data }))
        .catch(fail)
    }
  }, [])

  const run = async (action: () => Promise<MyLeasedBedStatus>) => {
    setBusy(true)
    setActionError(null)
    try {
      setState({ status: 'ready', data: await action() })
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setBusy(false)
    }
  }

  const startPayment = async (leaseId: number) => {
    setBusy(true)
    setActionError(null)
    try {
      const { checkoutUrl } = await payLease(leaseId)
      window.location.assign(checkoutUrl)
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Could not start payment. Please try again.')
      setBusy(false)
    }
  }

  return (
    <section className="card admin-form">
      <h2 className="section-title">Leased Beds</h2>

      {confirming && (
        <p className="card-note" style={{ margin: 0 }} role="status">
          Confirming your payment — this can take a few seconds…
        </p>
      )}
      {cancelledNotice && (
        <p className="card-note" style={{ margin: 0 }}>Payment cancelled. You can try again below.</p>
      )}

      {state.status === 'loading' && <p className="card-note" style={{ margin: 0 }}>Loading&hellip;</p>}
      {state.status === 'error' && <div className="form-error" role="alert">{state.message}</div>}

      {state.status === 'ready' && (
        !state.data.membership.isActive ? (
          <p className="card-note" style={{ margin: 0 }}>
            You need an active membership before you can lease a garden bed.
          </p>
        ) : (
          (() => {
            const currentLeases = state.data.leases.filter((l) => l.status === 'Active' || l.status === 'Expired')
            const pendingLeases = state.data.leases.filter((l) => l.status === 'AwaitingPayment')
            const pendingRequest = state.data.request?.status === 'Pending' || state.data.request?.status === 'Waitlisted'

            return (
              <>
                {currentLeases.length > 0 && (
                  <div className="admin-subsection">
                    <h3 className="card-subheading">Currently leased</h3>
                    {currentLeases.map((lease) => (
                      <LeaseRow
                        key={lease.leaseId}
                        lease={lease}
                        paymentsEnabled={paymentsEnabled}
                        busy={busy}
                        onPay={() => startPayment(lease.leaseId)}
                        onRenew={() => run(() => renewLease(lease.leaseId))}
                      />
                    ))}
                  </div>
                )}

                {(pendingLeases.length > 0 || pendingRequest) && (
                  <div className="admin-subsection">
                    <h3 className="card-subheading">Pending</h3>
                    {pendingLeases.map((lease) => (
                      <LeaseRow
                        key={lease.leaseId}
                        lease={lease}
                        paymentsEnabled={paymentsEnabled}
                        busy={busy}
                        onPay={() => startPayment(lease.leaseId)}
                        onRenew={() => run(() => renewLease(lease.leaseId))}
                      />
                    ))}
                    <PendingRequest
                      request={state.data.request}
                      busy={busy}
                      onWithdraw={() => run(withdrawBedRequest)}
                    />
                  </div>
                )}

                <ApplyCta
                  data={state.data}
                  hasActiveLease={state.data.leases.some((l) => l.status === 'Active')}
                  awaitingPayment={pendingLeases.length > 0}
                  busy={busy}
                  onApply={(requiresWheelchairAccessible) => run(() => applyForBed(requiresWheelchairAccessible))}
                />

                {actionError && <div className="form-error" role="alert">{actionError}</div>}
              </>
            )
          })()
        )
      )}
    </section>
  )
}

const leaseRowStyle = { display: 'flex', flexDirection: 'column', alignItems: 'flex-start', gap: 8 } as const

function RenewButton({ busy, onRenew }: { busy: boolean; onRenew: () => void }) {
  return (
    <div className="admin-actions">
      <button type="button" className="primary-button" onClick={onRenew} disabled={busy}>
        Renew for next year
      </button>
    </div>
  )
}

function LeaseRow({
  lease,
  paymentsEnabled,
  busy,
  onPay,
  onRenew,
}: {
  lease: MyLease
  paymentsEnabled: boolean
  busy: boolean
  onPay: () => void
  onRenew: () => void
}) {
  if (lease.status === 'AwaitingPayment') {
    return (
      <div style={leaseRowStyle}>
        <span className="pill pill-warn">Awaiting payment</span>
        <p className="card-note" style={{ margin: 0 }}>
          You&rsquo;ve been assigned bed {lease.bedLabel}. Pay {formatPrice(lease.priceAtAllocationCents)} to confirm your
          lease (expires {formatDate(lease.expiresOn)}).
        </p>
        {paymentsEnabled ? (
          <div className="admin-actions">
            <button type="button" className="primary-button" onClick={onPay} disabled={busy}>
              {busy ? 'Starting payment…' : 'Make payment'}
            </button>
          </div>
        ) : (
          <p className="card-note" style={{ margin: 0 }}>
            Online payments are currently unavailable. Please check back soon.
          </p>
        )}
      </div>
    )
  }

  if (lease.status === 'Expired') {
    return (
      <div style={leaseRowStyle}>
        <span className="pill pill-warn">Expired</span>
        <p className="card-note" style={{ margin: 0 }}>Your lease for bed {lease.bedLabel} expired on {formatDate(lease.expiresOn)}.</p>
        {lease.canRenew && <RenewButton busy={busy} onRenew={onRenew} />}
      </div>
    )
  }

  // Active. Only surface a payment label when there's something to convey — a "no
  // payment required" lease needs no action, so we drop it and just show the expiry.
  const paymentLabel = lease.isPaid
    ? (lease.paidOnUtc ? `Paid ${formatDate(lease.paidOnUtc)}` : null)
    : 'Awaiting payment'
  return (
    <div style={leaseRowStyle}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
        <span className="pill pill-ok">Bed {lease.bedLabel}</span>
        <span className="card-note">
          {paymentLabel ? `${paymentLabel} · ` : ''}Lease expires {formatDate(lease.expiresOn)}
        </span>
      </div>
      {lease.canRenew && <RenewButton busy={busy} onRenew={onRenew} />}
    </div>
  )
}

function PendingRequest({
  request,
  busy,
  onWithdraw,
}: {
  request: MyRequestInfo | null
  busy: boolean
  onWithdraw: () => void
}) {
  if (request?.status === 'Pending') {
    return (
      <>
        <p className="card-note" style={{ margin: 0 }}>
          Your application is pending — an admin will assign you a bed.
        </p>
        <div className="admin-actions">
          <button type="button" className="secondary-button" onClick={onWithdraw} disabled={busy}>
            Cancel my application
          </button>
        </div>
      </>
    )
  }

  if (request?.status === 'Waitlisted') {
    return (
      <>
        <p className="card-note" style={{ margin: 0 }}>
          You are number {request.waitlistPosition} on the waitlist.
        </p>
        <div className="admin-actions">
          <button type="button" className="secondary-button" onClick={onWithdraw} disabled={busy}>
            Leave the waitlist
          </button>
        </div>
      </>
    )
  }

  return null
}

function ApplyCta({
  data,
  hasActiveLease,
  awaitingPayment,
  busy,
  onApply,
}: {
  data: MyLeasedBedStatus
  hasActiveLease: boolean
  awaitingPayment: boolean
  busy: boolean
  onApply: (requiresWheelchairAccessible: boolean) => void
}) {
  const { capacity, request } = data
  const [needsAccessible, setNeedsAccessible] = useState(false)

  // An existing application or an assigned-but-unpaid bed already covers the next step.
  if (request?.status === 'Pending' || request?.status === 'Waitlisted' || awaitingPayment) return null

  return (
    <div className="admin-subsection">
      <label style={{ display: 'flex', alignItems: 'flex-start', gap: 8 }}>
        <input
          type="checkbox"
          checked={needsAccessible}
          onChange={(e) => setNeedsAccessible(e.target.checked)}
          disabled={busy}
        />
        <span className="card-note" style={{ margin: 0 }}>I need a wheelchair-accessible bed</span>
      </label>
      <div className="admin-actions">
        <button type="button" className="primary-button" onClick={() => onApply(needsAccessible)} disabled={busy}>
          {capacity.isFull ? 'Join the waitlist' : hasActiveLease ? 'Apply for another bed' : 'Apply for a bed'}
        </button>
      </div>
    </div>
  )
}
