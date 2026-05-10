import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchActivity, type ActivityItem } from '../../api/activity'
import { formatAbsolute, formatRelative, labelFor } from './activityFormatting'

type ActivityState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; items: ActivityItem[] }

const SECTIONS: { to: string; title: string; description: string }[] = [
  { to: '/admin/members', title: 'Members', description: 'View, add, and edit members. Export to XLSX.' },
  { to: '/admin/blog', title: 'Blog', description: 'Write, edit, and publish blog posts.' },
  { to: '/admin/tools', title: 'Tools', description: 'Import historical posts and clean up orphan images.' },
  { to: '/admin/activity', title: 'Activity', description: 'Full log of recent admin and member actions.' },
]

export default function Dashboard() {
  const [activity, setActivity] = useState<ActivityState>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false
    const load = async () => {
      try {
        const result = await fetchActivity(null, 10)
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

  return (
    <section className="admin-page" aria-labelledby="dashboard-heading">
      <header className="admin-page-header">
        <h1 id="dashboard-heading" className="admin-page-title">Dashboard</h1>
      </header>

      <div className="card">
        <div className="admin-page-header">
          <h2 className="section-title">Recent activity</h2>
          <Link to="/admin/activity" className="footer-link">View all activity →</Link>
        </div>

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
        )}
      </div>

      <div className="admin-tools-grid">
        {SECTIONS.map((section) => (
          <Link key={section.to} to={section.to} className="card admin-section-card">
            <h2 className="section-title">{section.title}</h2>
            <p className="card-note">{section.description}</p>
          </Link>
        ))}
      </div>
    </section>
  )
}
