import { useEffect, useState } from 'react'
import {
  fetchMemberPayments,
  fetchMyPayments,
  recordRefund,
  type Member,
  type PaymentRecord,
} from '../../api/auth'
import RecordPaymentModal from './RecordPaymentModal'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; payments: PaymentRecord[] }

const dateFormatter = new Intl.DateTimeFormat('en-AU', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
})

// Includes the time, for when a payment was actually made.
const dateTimeFormatter = new Intl.DateTimeFormat('en-AU', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
})

function formatDate(iso: string | null): string {
  if (!iso) return '—'
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '—' : dateFormatter.format(d)
}

function formatDateTime(iso: string | null): string {
  if (!iso) return '—'
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '—' : dateTimeFormatter.format(d)
}

function formatAmount(cents: number, currency: string): string {
  const code = currency.toUpperCase()
  try {
    return new Intl.NumberFormat('en-AU', { style: 'currency', currency: code }).format(cents / 100)
  } catch {
    return `${(cents / 100).toFixed(2)} ${code}`
  }
}

function formatPeriod(start: string | null, end: string | null): string {
  if (!start || !end) return '—'
  return `${formatDate(start)} – ${formatDate(end)}`
}

// What the payment was for: a membership (with its period, if known) or a leased bed.
function describePayment(p: PaymentRecord): string {
  if (p.kind === 'LeasedBed') return p.bedLabel ? `Leased bed ${p.bedLabel}` : 'Leased bed'
  const period = formatPeriod(p.periodStartUtc, p.periodEndUtc)
  return period === '—' ? 'Membership' : `Membership · ${period}`
}

const STATUS_PILL: Record<string, string> = {
  Paid: 'pill pill-ok',
  Failed: 'pill pill-warn',
  Pending: 'pill',
}

function refundableCents(p: PaymentRecord): number {
  return p.status === 'Paid' ? p.amountCents - p.refundedAmountCents : 0
}

function statusLabel(p: PaymentRecord): string {
  if (p.status !== 'Paid' || p.refundedAmountCents === 0) return p.status
  return p.refundedAmountCents >= p.amountCents ? 'Refunded' : 'Partly refunded'
}

// Admin views pass a memberId and see a Status column (Paid/Failed); the member's
// own profile omits memberId and sees only their successful payments. Bumping
// refreshToken re-fetches - e.g. after an admin records a manual payment. Admins can also
// record a refund they made in Stripe; onRefunded receives the updated member so the page
// can show any change to their renewal date.
export default function PaymentHistoryTable({
  memberId,
  refreshToken,
  onRefunded,
}: {
  memberId?: string
  refreshToken?: number
  onRefunded?: (member: Member) => void
}) {
  const showStatus = memberId !== undefined
  const [state, setState] = useState<State>({ status: 'loading' })
  const [refunding, setRefunding] = useState<PaymentRecord | null>(null)

  useEffect(() => {
    let cancelled = false
    setState({ status: 'loading' })
    const request = memberId ? fetchMemberPayments(memberId) : fetchMyPayments()
    request
      .then((payments) => {
        if (!cancelled) setState({ status: 'ready', payments })
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load payments' })
        }
      })
    return () => {
      cancelled = true
    }
  }, [memberId, refreshToken])

  if (state.status === 'loading') {
    return <p className="admin-loading">Loading payments&hellip;</p>
  }

  if (state.status === 'error') {
    return <div className="form-error" role="alert">{state.message}</div>
  }

  if (state.payments.length === 0) {
    return <p className="admin-empty">No payments recorded yet.</p>
  }

  return (
    <div className="admin-table-wrap">
      <table className="admin-table">
        <thead>
          <tr>
            <th scope="col">Date</th>
            <th scope="col">Amount</th>
            <th scope="col">Method</th>
            <th scope="col">For</th>
            {showStatus && <th scope="col">Status</th>}
            {showStatus && <th scope="col" aria-label="Actions" />}
          </tr>
        </thead>
        <tbody>
          {state.payments.map((p) => (
            <tr key={`${p.kind}-${p.id}`}>
              <td data-label="Date">{formatDateTime(p.paidAtUtc ?? p.createdAtUtc)}</td>
              <td data-label="Amount">
                <span>
                  {formatAmount(p.amountCents, p.currency)}
                  {p.refundedAmountCents > 0 && (
                    <>
                      <br />
                      <span className="card-note">
                        {formatAmount(p.refundedAmountCents, p.currency)} refunded {formatDate(p.refundedAtUtc)}
                      </span>
                    </>
                  )}
                </span>
              </td>
              <td data-label="Method">{p.method === 'Manual' ? 'Manual' : 'Stripe'}</td>
              <td data-label="For">{describePayment(p)}</td>
              {showStatus && (
                <td data-label="Status">
                  <span className={p.refundedAmountCents > 0 ? 'pill' : (STATUS_PILL[p.status] ?? 'pill')}>{statusLabel(p)}</span>
                </td>
              )}
              {showStatus && (
                <td data-label="">
                  {refundableCents(p) > 0 && (
                    <button type="button" className="secondary-button" onClick={() => setRefunding(p)}>
                      Record refund
                    </button>
                  )}
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
      {memberId && (
        <RecordPaymentModal
          open={refunding !== null}
          title="Record a refund"
          description="First refund the money in Stripe (or hand back the cash), then record it here. If this refunds the whole payment and no other payment covers the year, their renewal date goes back."
          initialAmountCents={refunding ? refundableCents(refunding) : 0}
          amountLabel="Amount refunded"
          confirmLabel="Record refund"
          onClose={() => setRefunding(null)}
          onConfirm={async (amountCents) => {
            if (!refunding) return
            onRefunded?.(await recordRefund(memberId, refunding, amountCents))
          }}
        />
      )}
    </div>
  )
}
