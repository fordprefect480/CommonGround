import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchActivity, type ActivityItem } from '../../api/activity'
import { fetchMemberStats, type MemberStats } from '../../api/auth'
import { formatAbsolute, formatRelative, labelFor } from './activityFormatting'

type ActivityState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; items: ActivityItem[] }

type StatsState =
  | { status: 'loading' }
  | { status: 'error' }
  | { status: 'ready'; stats: MemberStats }

const TOTAL_BEDS = 32

export default function Dashboard() {
  const [activity, setActivity] = useState<ActivityState>({ status: 'loading' })
  const [stats, setStats] = useState<StatsState>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false
    const load = async () => {
      try {
        const result = await fetchActivity(null, 5)
        if (cancelled) return
        setActivity({ status: 'ready', items: result.items })
      } catch (err) {
        if (cancelled) return
        setActivity({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load activity' })
      }
    }
    load()
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    let cancelled = false
    const load = async () => {
      try {
        const result = await fetchMemberStats()
        if (cancelled) return
        setStats({ status: 'ready', stats: result })
      } catch {
        if (cancelled) return
        setStats({ status: 'error' })
      }
    }
    load()
    return () => { cancelled = true }
  }, [])

  const statValue = (value: number | null) =>
    stats.status === 'loading' ? '…' : stats.status === 'error' ? '—' : String(value)

  return (
    <section className="admin-page" aria-labelledby="dashboard-heading">
      <header className="admin-page-header">
        <h1 id="dashboard-heading" className="admin-page-title">Dashboard</h1>
      </header>

      <div className="admin-stats-grid">
        <div className="admin-stat-card">
          <span className="admin-stat-value">{statValue(stats.status === 'ready' ? stats.stats.paidMembers : null)}</span>
          <span className="admin-stat-label">Paid members</span>
        </div>
        <div className="admin-stat-card">
          <span className="admin-stat-value">{statValue(stats.status === 'ready' ? stats.stats.notYetPaidMembers : null)}</span>
          <span className="admin-stat-label">Not yet paid</span>
        </div>
        <div className="admin-stat-card">
          <span className="admin-stat-value">{statValue(stats.status === 'ready' ? stats.stats.newMembersLast30Days : null)}</span>
          <span className="admin-stat-label">Sign-ups in past 30 days</span>
        </div>
        <div className="admin-stat-card">
          <span className="admin-stat-value">0 <span className="admin-stat-value-total">/ {TOTAL_BEDS}</span></span>
          <span className="admin-stat-label">Leased beds</span>
        </div>
      </div>

      <div className="card">
        <h2 className="section-title">Recent activity</h2>

        {activity.status === 'loading' && (
          <p className="admin-loading">Loading activity&hellip;</p>
        )}

        {activity.status === 'error' && (
          <div className="form-error" role="alert">{activity.message}</div>
        )}

        {activity.status === 'ready' && activity.items.length === 0 && (
          <p className="admin-empty">No activity recorded yet.</p>
        )}

        {activity.status === 'ready' && activity.items.length > 0 && (
          <>
            <ul className="admin-activity-feed">
              {activity.items.map((item) => (
                <li key={item.id} className="admin-activity-feed-item">
                  <span className="pill">{labelFor(item.activityType)}</span>
                  <span className="admin-activity-feed-summary">{item.summary}</span>
                  <span
                    className="admin-activity-feed-when"
                    title={formatAbsolute(item.occurredAt)}
                  >
                    {formatRelative(item.occurredAt)}
                  </span>
                </li>
              ))}
            </ul>
            <div className="admin-activity-feed-more">
              <Link to="/admin/activity" className="footer-link admin-activity-feed-more-link">View more&hellip;</Link>
            </div>
          </>
        )}
      </div>
    </section>
  )
}
