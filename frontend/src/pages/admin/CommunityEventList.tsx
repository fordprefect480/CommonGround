import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import {
  deleteCommunityEvent,
  fetchAdminCommunityEvents,
  fetchUpcomingEvents,
  type CommunityEventAdmin,
  type UpcomingEvent,
} from '../../api/events'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; events: CommunityEventAdmin[]; eventbrite: UpcomingEvent[] }

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
  const location = useLocation()
  const [notice, setNotice] = useState<string | null>(
    (location.state as { notice?: string } | null)?.notice ?? null,
  )

  useEffect(() => {
    // Clear router state so a refresh or back-nav doesn't resurface the notice.
    if (window.history.state) window.history.replaceState({}, '')
  }, [])

  const [state, setState] = useState<State>({ status: 'loading' })

  const reload = () => {
    setState({ status: 'loading' })
    Promise.all([fetchAdminCommunityEvents(), fetchUpcomingEvents(24)])
      .then(([events, upcoming]) =>
        setState({
          status: 'ready',
          events,
          eventbrite: upcoming.filter((e) => e.source === 'eventbrite'),
        }),
      )
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
  const isEmpty =
    state.status === 'ready' && state.events.length === 0 && state.eventbrite.length === 0

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

      {notice && (
        <div className="form-success" role="status">
          {notice}
          <button
            type="button"
            className="footer-link"
            style={{ marginLeft: 12 }}
            onClick={() => setNotice(null)}
          >
            Dismiss
          </button>
        </div>
      )}

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
        appear automatically (greyed out below) and on the homepage. They&rsquo;re managed on
        Eventbrite, so they can&rsquo;t be edited here.
      </p>

      {state.status === 'loading' && <p className="admin-loading">Loading&hellip;</p>}
      {state.status === 'error' && (
        <div className="form-error" role="alert">{state.message}</div>
      )}
      {isEmpty && (
        <p className="admin-empty">No manual events yet. Click "New event" to add one.</p>
      )}
      {state.status === 'ready' && !isEmpty && (
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
                  <tr key={`manual-${ev.id}`}>
                    <td data-label="Title">{ev.title}</td>
                    <td data-label="Starts">{dateFmt.format(start)}</td>
                    <td data-label="Status">
                      {past ? <span className="pill">Past</span> : <span className="pill pill-ok">Upcoming</span>}
                    </td>
                    <td data-label="Actions">
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
              {state.eventbrite.map((ev) => (
                <tr key={ev.id} className="admin-table-row-muted">
                  <td data-label="Title">
                    {ev.title}
                    {' '}
                    <span className="pill admin-source-badge">Eventbrite</span>
                  </td>
                  <td data-label="Starts">{dateFmt.format(new Date(ev.startUtc))}</td>
                  <td data-label="Status">Upcoming</td>
                  <td data-label="Actions">
                    {ev.url ? (
                      <a className="footer-link" href={ev.url} target="_blank" rel="noopener noreferrer">
                        View on Eventbrite
                      </a>
                    ) : (
                      '—'
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
