import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { fetchSentEmails, type SentEmailListItem } from '../../api/email'
import { formatAbsolute, formatRelative } from './activityFormatting'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; items: SentEmailListItem[] }

export default function EmailList() {
  const navigate = useNavigate()
  const [state, setState] = useState<State>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false
    fetchSentEmails()
      .then((items) => { if (!cancelled) setState({ status: 'ready', items }) })
      .catch((err: unknown) => {
        if (!cancelled) setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load' })
      })
    return () => { cancelled = true }
  }, [])

  return (
    <section className="admin-page" aria-labelledby="email-heading">
      <header className="admin-page-header">
        <h1 id="email-heading" className="admin-page-title">Email</h1>
        <button type="button" className="primary-button" onClick={() => navigate('/admin/email/new')}>
          New email
        </button>
      </header>

      {state.status === 'loading' && <p className="admin-loading">Loading&hellip;</p>}
      {state.status === 'error' && <div className="form-error" role="alert">{state.message}</div>}
      {state.status === 'ready' && state.items.length === 0 && (
        <p className="admin-empty">No emails sent yet. Click &ldquo;New email&rdquo; to compose one.</p>
      )}
      {state.status === 'ready' && state.items.length > 0 && (
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th scope="col">Sent</th>
                <th scope="col">Subject</th>
                <th scope="col">From</th>
                <th scope="col">Delivery</th>
              </tr>
            </thead>
            <tbody>
              {state.items.map((item) => (
                <tr key={item.id}>
                  <td title={formatAbsolute(item.sentAt)}>{formatRelative(item.sentAt)}</td>
                  <td><Link to={`/admin/email/${item.id}`}>{item.subject}</Link></td>
                  <td>{item.senderEmail ?? '—'}</td>
                  <td>
                    {item.failedCount > 0 ? (
                      <span className="pill pill-warn">
                        {item.sentCount}/{item.recipientCount} delivered
                      </span>
                    ) : (
                      <span className="pill pill-ok">
                        {item.sentCount} delivered
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
