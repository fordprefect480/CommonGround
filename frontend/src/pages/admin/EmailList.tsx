import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { fetchSentEmails, type SentEmailListItem } from '../../api/email'
import { formatAbsolute, formatRelative } from './activityFormatting'
import EmailComposeModal from './EmailComposeModal'
import RecipientsPopover from './RecipientsPopover'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; items: SentEmailListItem[] }

export default function EmailList() {
  const navigate = useNavigate()
  const [state, setState] = useState<State>({ status: 'loading' })
  const [popover, setPopover] = useState<{ id: number; anchor: DOMRect } | null>(null)
  const [composeOpen, setComposeOpen] = useState(false)

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
        <button type="button" className="primary-button" onClick={() => setComposeOpen(true)}>
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
                <th scope="col">Type</th>
                <th scope="col">From</th>
                <th scope="col">To</th>
              </tr>
            </thead>
            <tbody>
              {state.items.map((item) => (
                <tr key={item.id}>
                  <td data-label="Sent" title={formatAbsolute(item.sentAt)}>{formatRelative(item.sentAt)}</td>
                  <td data-label="Subject"><Link to={`/admin/email/${item.id}`} className="admin-table-link">{item.subject}</Link></td>
                  <td data-label="Type">
                    {item.isNewsletter
                      ? <span className="pill pill-warn">Newsletter</span>
                      : <span className="pill pill-ok">Membership</span>}
                  </td>
                  <td data-label="From">{item.senderEmail ?? '—'}</td>
                  <td data-label="To">
                    {item.recipientCount === 0 ? '—' :
                     item.recipientCount === 1 ? (item.recipientEmail ?? '—') : (
                      <button
                        type="button"
                        className="recipients-toggle"
                        aria-haspopup="dialog"
                        onClick={(e) => setPopover({ id: item.id, anchor: e.currentTarget.getBoundingClientRect() })}
                      >
                        {item.recipientEmail ?? `${item.recipientCount} recipients`}
                        {item.recipientEmail && <span className="recipients-toggle-more"> +{item.recipientCount - 1} more</span>}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {popover && (
        <RecipientsPopover
          emailId={popover.id}
          anchor={popover.anchor}
          onClose={() => setPopover(null)}
        />
      )}

      {composeOpen && (
        <EmailComposeModal
          onClose={() => setComposeOpen(false)}
          onSent={(result) => {
            setComposeOpen(false)
            navigate(`/admin/email/${result.id}`)
          }}
        />
      )}
    </section>
  )
}
