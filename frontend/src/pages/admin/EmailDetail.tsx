import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { fetchSentEmail, type SentEmailDetail } from '../../api/email'
import { formatAbsolute } from './activityFormatting'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; email: SentEmailDetail }

export default function EmailDetailPage() {
  const { id } = useParams()
  const [state, setState] = useState<State>({ status: 'loading' })

  useEffect(() => {
    if (!id) return
    let cancelled = false
    fetchSentEmail(Number(id))
      .then((email) => { if (!cancelled) setState({ status: 'ready', email }) })
      .catch((err: unknown) => {
        if (!cancelled) setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load' })
      })
    return () => { cancelled = true }
  }, [id])

  if (state.status === 'loading') return <p className="admin-loading">Loading&hellip;</p>
  if (state.status === 'error') return <div className="form-error" role="alert">{state.message}</div>

  const email = state.email

  return (
    <section className="admin-page" aria-labelledby="email-heading">
      <header className="admin-page-header">
        <h1 id="email-heading" className="admin-page-title">{email.subject}</h1>
        <Link to="/admin/email" className="footer-link">&larr; All emails</Link>
      </header>

      <div className="card">
        <div className="field">
          <span className="field-label">Sent</span>
          <span className="field-readonly">{formatAbsolute(email.sentAt)}</span>
        </div>
        <div className="field">
          <span className="field-label">From</span>
          <span className="field-readonly">{email.senderEmail ?? '—'}</span>
        </div>
        <div className="field">
          <span className="field-label">Delivery</span>
          <span className="field-readonly">
            {email.sentCount} of {email.recipientCount} delivered
            {email.failedCount > 0 && <> &middot; {email.failedCount} failed</>}
          </span>
        </div>
        <div className="field">
          <span className="field-label">Body</span>
          <div className="blog-post-body" dangerouslySetInnerHTML={{ __html: email.htmlBody }} />
        </div>
      </div>

      <div className="card">
        <h2 className="section-title">Recipients</h2>
        {email.recipients.length === 0 ? (
          <p className="admin-empty">No recipients recorded.</p>
        ) : (
          <div className="admin-table-wrap">
            <table className="admin-table">
              <thead>
                <tr>
                  <th scope="col">Email</th>
                  <th scope="col">Status</th>
                  <th scope="col">Error</th>
                </tr>
              </thead>
              <tbody>
                {email.recipients.map((r) => (
                  <tr key={r.id}>
                    <td>
                      {r.userId
                        ? <Link to={`/admin/members/${r.userId}`} className="admin-table-link">{r.email}</Link>
                        : r.email}
                    </td>
                    <td>
                      <span className={r.status === 'sent' ? 'pill pill-ok' : 'pill pill-warn'}>
                        {r.status === 'sent' ? 'Sent' : 'Failed'}
                      </span>
                    </td>
                    <td>{r.errorMessage ?? ''}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  )
}
