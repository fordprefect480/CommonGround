import { useEffect, useState } from 'react'
import {
  fetchMemberPayments,
  fetchMyPayments,
  type MembershipPaymentRecord,
} from '../../api/auth'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; payments: MembershipPaymentRecord[] }

const dateFormatter = new Intl.DateTimeFormat('en-AU', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
})

function formatDate(iso: string | null): string {
  if (!iso) return '—'
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '—' : dateFormatter.format(d)
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

const STATUS_PILL: Record<string, string> = {
  Paid: 'pill pill-ok',
  Failed: 'pill pill-warn',
  Pending: 'pill',
}

// Admin views pass a memberId and see a Status column (Paid/Failed); the member's
// own profile omits memberId and sees only their successful payments.
export default function PaymentHistoryTable({ memberId }: { memberId?: string }) {
  const showStatus = memberId !== undefined
  const [state, setState] = useState<State>({ status: 'loading' })

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
  }, [memberId])

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
            <th scope="col">Membership period</th>
            {showStatus && <th scope="col">Status</th>}
          </tr>
        </thead>
        <tbody>
          {state.payments.map((p) => (
            <tr key={p.id}>
              <td>{formatDate(p.paidAtUtc ?? p.createdAtUtc)}</td>
              <td>{formatAmount(p.amountCents, p.currency)}</td>
              <td>{formatPeriod(p.periodStartUtc, p.periodEndUtc)}</td>
              {showStatus && (
                <td>
                  <span className={STATUS_PILL[p.status] ?? 'pill'}>{p.status}</span>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
