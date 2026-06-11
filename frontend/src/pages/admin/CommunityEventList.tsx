import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  deleteCommunityEvent,
  fetchAdminCommunityEvents,
  type CommunityEventAdmin,
} from '../../api/events'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; events: CommunityEventAdmin[] }

const dateFmt = new Intl.DateTimeFormat('en-AU', {
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
})

export default function CommunityEventList() {
  const navigate = useNavigate()
  const [state, setState] = useState<State>({ status: 'loading' })

  const reload = () => {
    setState({ status: 'loading' })
    fetchAdminCommunityEvents()
      .then((events) => setState({ status: 'ready', events }))
      .catch((err: unknown) =>
        setState({
          status: 'error',
          message: err instanceof Error ? err.message : 'Failed to load',
        }),
      )
  }

  useEffect(() => {
    reload()
  }, [])

  const handleDelete = async (ev: CommunityEventAdmin) => {
    if (!confirm(`Remove "${ev.title}"? This cannot be undone.`)) return
    try {
      await deleteCommunityEvent(ev.id)
      reload()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Delete failed')
    }
  }

  const now = Date.now()

  return (
    <section className="admin-page" aria-labelledby="events-heading">
      <header className="admin-page-header">
        <h1 id="events-heading" className="admin-page-title">Events</h1>
        <button
          type="button"
          className="primary-button"
          onClick={() => navigate('/admin/events/new')}
        >
          New event
        </button>
      </header>

      <p className="admin-empty">
        Add events that aren&rsquo;t on Eventbrite (working bees, social groups,
        workshops). Upcoming events from{' '}
        <a
          href="https://www.eventbrite.com.au/o/mid-coast-sustainability-inc-47275698483"
          target="_blank"
          rel="noopener noreferrer"
        >
          Mid Coast Sustainability on Eventbrite
        </a>{' '}
        appear automatically alongside these on the homepage.
      </p>

      {state.status === 'loading' && <p className="admin-loading">Loading&hellip;</p>}
      {state.status === 'error' && (
        <div className="form-error" role="alert">{state.message}</div>
      )}
      {state.status === 'ready' && state.events.length === 0 && (
        <p className="admin-empty">No manual events yet. Click "New event" to add one.</p>
      )}
      {state.status === 'ready' && state.events.length > 0 && (
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th scope="col">Title</th>
                <th scope="col">Starts</th>
                <th scope="col">Status</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {state.events.map((ev) => {
                const start = new Date(ev.startUtc)
                const past = (ev.endUtc ? new Date(ev.endUtc) : start).getTime() < now
                return (
                  <tr key={ev.id}>
                    <td>{ev.title}</td>
                    <td>{dateFmt.format(start)}</td>
                    <td>{past ? <span style={{ color: 'var(--fg-3)' }}>Past</span> : 'Upcoming'}</td>
                    <td>
                      <button
                        type="button"
                        className="footer-link"
                        onClick={() => navigate(`/admin/events/${ev.id}/edit`)}
                      >
                        Edit
                      </button>
                      {' · '}
                      <button
                        type="button"
                        className="footer-link"
                        onClick={() => handleDelete(ev)}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
