import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchActivity, type ActivityItem } from '../../api/activity'
import { formatAbsolute, formatRelative, labelFor, prettifyDetails } from './activityFormatting'

const PAGE_SIZE = 50

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; items: ActivityItem[]; nextCursor: string | null }

export default function Activity() {
  const [state, setState] = useState<State>({ status: 'loading' })
  // Stack of cursors used to enter each page. cursorStack[i] is the cursor passed
  // to load page (i + 1); cursorStack[0] is always null. Length = current page number.
  const [cursorStack, setCursorStack] = useState<(string | null)[]>([null])
  const [navigating, setNavigating] = useState(false)

  const pageNumber = cursorStack.length
  const entryCursor = cursorStack[pageNumber - 1]

  useEffect(() => {
    let cancelled = false
    setNavigating(true)
    fetchActivity(entryCursor, PAGE_SIZE)
      .then((result) => {
        if (cancelled) return
        setState({ status: 'ready', items: result.items, nextCursor: result.nextCursor })
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load activity' })
      })
      .finally(() => {
        if (!cancelled) setNavigating(false)
      })
    return () => { cancelled = true }
  }, [entryCursor])

  const goNext = () => {
    if (state.status !== 'ready' || !state.nextCursor || navigating) return
    setCursorStack((prev) => [...prev, state.nextCursor])
  }

  const goPrev = () => {
    if (pageNumber <= 1 || navigating) return
    setCursorStack((prev) => prev.slice(0, -1))
  }

  const hasNext = state.status === 'ready' && state.nextCursor !== null
  const hasPrev = pageNumber > 1

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
        <>
          <ActivityTable items={state.items} />
          {(hasPrev || hasNext) && (
            <nav className="admin-actions" aria-label="Activity pagination">
              <button
                type="button"
                className="primary-button"
                onClick={goPrev}
                disabled={!hasPrev || navigating}
              >
                Previous
              </button>
              <span aria-live="polite">Page {pageNumber}</span>
              <button
                type="button"
                className="primary-button"
                onClick={goNext}
                disabled={!hasNext || navigating}
              >
                Next
              </button>
            </nav>
          )}
        </>
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
      <table className="admin-table admin-table--stacked">
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
              <td data-label="When" title={formatAbsolute(item.occurredAt)}>{formatRelative(item.occurredAt)}</td>
              <td data-label="Who">
                {(() => {
                  const display = item.actorName ?? item.actorEmail ?? '(unknown)'
                  const tooltip = item.actorName && item.actorEmail ? item.actorEmail : undefined
                  return item.actorUserId
                    ? <Link to={`/admin/members/${item.actorUserId}`} className="admin-table-link" title={tooltip}>{display}</Link>
                    : <span title={tooltip}>{item.actorName ?? item.actorEmail ?? '-'}</span>
                })()}
              </td>
              <td data-label="Type"><span className="pill">{labelFor(item.activityType)}</span></td>
              <td data-label="Summary">{item.summary}</td>
              <td data-label="Details">
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
