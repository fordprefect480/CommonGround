import { useEffect, useState, type ReactNode } from 'react'
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

const ICON_PROPS = {
  width: 28,
  height: 28,
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.75,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
  'aria-hidden': true,
}

const MembershipIcon = () => (
  <svg {...ICON_PROPS}>
    <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
    <circle cx="9" cy="7" r="4" />
    <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
    <path d="M16 3.13a4 4 0 0 1 0 7.75" />
  </svg>
)

const BlogIcon = () => (
  <svg {...ICON_PROPS}>
    <path d="M12 20h9" />
    <path d="M16.5 3.5a2.121 2.121 0 1 1 3 3L7 19l-4 1 1-4 12.5-12.5z" />
  </svg>
)

const ToolsIcon = () => (
  <svg {...ICON_PROPS}>
    <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z" />
  </svg>
)

const ActivityIcon = () => (
  <svg {...ICON_PROPS}>
    <path d="M22 12h-4l-3 9L9 3l-3 9H2" />
  </svg>
)

const EmailIcon = () => (
  <svg {...ICON_PROPS}>
    <rect x="2" y="4" width="20" height="16" rx="2" />
    <path d="m2 7 10 7 10-7" />
  </svg>
)

const SECTIONS: { to: string; title: string; description: string; icon: ReactNode }[] = [
  { to: '/admin/members', title: 'Membership', description: 'View, add, and edit members. Export to XLSX.', icon: <MembershipIcon /> },
  { to: '/admin/blog', title: 'Blog', description: 'Write, edit, and publish blog posts.', icon: <BlogIcon /> },
  { to: '/admin/email', title: 'Email', description: 'View sent emails and compose a new one to all subscribed members.', icon: <EmailIcon /> },
  { to: '/admin/tools', title: 'Settings', description: 'Advanced tools - import historical posts and clean up orphan images.', icon: <ToolsIcon /> },
  { to: '/admin/activity', title: 'Activity', description: 'Full log of recent admin and member actions.', icon: <ActivityIcon /> },
]

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

      <div className="admin-stats-grid">
        <div className="admin-stat-card">
          <span className="admin-stat-value">{statValue(stats.status === 'ready' ? stats.stats.activeMembers : null)}</span>
          <span className="admin-stat-label">Active members</span>
        </div>
        <div className="admin-stat-card">
          <span className="admin-stat-value">{statValue(stats.status === 'ready' ? stats.stats.lapsedMembers : null)}</span>
          <span className="admin-stat-label">Lapsed members</span>
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

      <div className="admin-tools-grid">
        {SECTIONS.map((section) => (
          <Link key={section.to} to={section.to} className="card admin-section-card">
            <span className="admin-section-card-icon" aria-hidden="true">{section.icon}</span>
            <h2 className="section-title">{section.title}</h2>
            <p className="card-note">{section.description}</p>
          </Link>
        ))}
      </div>
    </section>
  )
}
