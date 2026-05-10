import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchActivity, type ActivityItem, type ActivityList } from '../../api/activity'
import { formatAbsolute, formatRelative, labelFor, prettifyDetails } from './activityFormatting'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; items: ActivityItem[]; nextCursor: string | null; loadingMore: boolean }

export default function Activity() {
  const [state, setState] = useState<State>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false
    const load = async () => {
      try {
        const result: ActivityList = await fetchActivity()
        if (cancelled) return
        setState({ status: 'ready', items: result.items, nextCursor: result.nextCursor, loadingMore: false })
      } catch (err) {
        if (cancelled) return
        setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load activity' })
      }
    }
    load()
    return () => { cancelled = true }
  }, [])

  const loadMore = async () => {
    if (state.status !== 'ready' || !state.nextCursor || state.loadingMore) return
    setState({ ...state, loadingMore: true })
    try {
      const result = await fetchActivity(state.nextCursor)
      setState((prev) => prev.status === 'ready'
        ? { status: 'ready', items: [...prev.items, ...result.items], nextCursor: result.nextCursor, loadingMore: false }
        : prev)
    } catch (err) {
      setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load more activity' })
    }
  }

  return (
    <section className="admin-page" aria-labelledby="activity-heading">
      <header className="admin-page-header">
        <h1 id="activity-heading" className="admin-page-title">Activity</h1>
      </header>

      {state.status === 'loading' && (
        <p className="admin-loading">Loading activity&hellip;</p>
      )}

      {state.status === 'error' && (
        <div className="form-error" role="alert">{state.message}</div>
      )}

      {state.status === 'ready' && (
        <ActivityTable items={state.items} />
      )}

      {state.status === 'ready' && state.nextCursor && (
        <div className="admin-actions">
          <button type="button" className="primary-button" onClick={loadMore} disabled={state.loadingMore}>
            {state.loadingMore ? 'Loading…' : 'Load more'}
          </button>
        </div>
      )}
    </section>
  )
}

function ActivityTable({ items }: { items: ActivityItem[] }) {
  if (items.length === 0) {
    return <p className="admin-empty">No activity recorded yet.</p>
  }

  return (
    <div className="admin-table-wrap">
      <table className="admin-table">
        <thead>
          <tr>
            <th scope="col">When</th>
            <th scope="col">Who</th>
            <th scope="col">Type</th>
            <th scope="col">Summary</th>
            <th scope="col">Details</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td title={formatAbsolute(item.occurredAt)}>{formatRelative(item.occurredAt)}</td>
              <td>
                {item.actorUserId
                  ? <Link to={`/admin/members/${item.actorUserId}`} className="admin-table-link">{item.actorEmail ?? '(unknown)'}</Link>
                  : (item.actorEmail ?? '—')}
              </td>
              <td><span className="pill">{labelFor(item.activityType)}</span></td>
              <td>{item.summary}</td>
              <td>
                {item.detailsJson ? (
                  <details>
                    <summary>View</summary>
                    <pre style={{ whiteSpace: 'pre-wrap', margin: 0 }}>{prettifyDetails(item.detailsJson)}</pre>
                  </details>
                ) : null}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
