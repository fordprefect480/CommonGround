import { useEffect, useState } from 'react'
import { useAppConfig } from '../../AppConfigContext'
import {
  applyForBed,
  fetchMyLeasedBedStatus,
  payLease,
  renewLease,
  withdrawBedRequest,
  type MyLease,
  type MyLeasedBedStatus,
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

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const payment = params.get('payment')
    if (payment) {
      params.delete('payment')
      const qs = params.toString()
      window.history.replaceState({}, '', window.location.pathname + (qs ? `?${qs}` : ''))
    }

    let cancelled = false
    const fail = (err: unknown) => {
      if (!cancelled) setState({ status: 'error', message: err instanceof Error ? err.message : 'Could not load your bed status' })
    }

    if (payment === 'cancelled') setCancelledNotice(true)

    if (payment === 'success') {
      // The webhook is the source of truth - poll until the lease flips to Active.
      setConfirming(true)
      let attempts = 0
      const poll = async () => {
        attempts++
        try {
          const data = await fetchMyLeasedBedStatus()
          if (cancelled) return
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
        if (!cancelled) setTimeout(poll, 2500)
      }
      poll()
    } else {
      fetchMyLeasedBedStatus()
        .then((data) => { if (!cancelled) setState({ status: 'ready', data }) })
        .catch(fail)
    }

    return () => { cancelled = true }
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
      <h2 className="section-title">Leased bed</h2>

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
          <>
            {state.data.leases.map((lease) => (
              <LeaseRow
                key={lease.leaseId}
                lease={lease}
                paymentsEnabled={paymentsEnabled}
                busy={busy}
                onPay={() => startPayment(lease.leaseId)}
                onRenew={() => run(() => renewLease(lease.leaseId))}
              />
            ))}

            <RequestSection
              data={state.data}
              hasActiveLease={state.data.leases.some((l) => l.status === 'Active')}
              awaitingPayment={state.data.leases.some((l) => l.status === 'AwaitingPayment')}
              busy={busy}
              onApply={() => run(applyForBed)}
              onWithdraw={() => run(withdrawBedRequest)}
            />

            {actionError && <div className="form-error" role="alert">{actionError}</div>}
          </>
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

  // Active
  return (
    <div style={leaseRowStyle}>
      <span className="pill pill-ok">Bed {lease.bedLabel}</span>
      <p className="card-note" style={{ margin: 0 }}>
        {lease.isPaid ? (lease.paidOnUtc ? `Paid ${formatDate(lease.paidOnUtc)}` : 'No payment required') : 'Awaiting payment'}
        {' · '}Lease expires {formatDate(lease.expiresOn)}.
      </p>
      {lease.canRenew && <RenewButton busy={busy} onRenew={onRenew} />}
    </div>
  )
}

function RequestSection({
  data,
  hasActiveLease,
  awaitingPayment,
  busy,
  onApply,
  onWithdraw,
}: {
  data: MyLeasedBedStatus
  hasActiveLease: boolean
  awaitingPayment: boolean
  busy: boolean
  onApply: () => void
  onWithdraw: () => void
}) {
  const { capacity, request } = data

  if (request?.status === 'Pending') {
    return (
      <p className="card-note" style={{ margin: 0 }}>
        Your application is pending — an admin will assign you a bed.
      </p>
    )
  }

  if (request?.status === 'Waitlisted') {
    return (
      <>
        <p className="card-note" style={{ margin: 0 }}>
          You are number {request.waitlistPosition} on the waiting list.
        </p>
        <div className="admin-actions">
          <button type="button" className="secondary-button" onClick={onWithdraw} disabled={busy}>
            Leave the waiting list
          </button>
        </div>
      </>
    )
  }

  // A bed is assigned but not yet paid for — no apply CTA until that's settled.
  if (awaitingPayment) return null

  // No active request — show a single call to action.
  return (
    <div className="admin-actions">
      <button type="button" className="primary-button" onClick={onApply} disabled={busy}>
        {capacity.isFull ? 'Join the waiting list' : hasActiveLease ? 'Apply for another bed' : 'Apply for a bed'}
      </button>
    </div>
  )
}
