import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../../AuthContext'
import { MSHeader, usePageNav } from '../home/Chrome'

const ADMIN_NAV: ReadonlyArray<readonly [string, string]> = [
  ['/admin', 'Dashboard'],
  ['/admin/members', 'Members'],
  ['/admin/blog', 'Blog'],
  ['/admin/events', 'Events'],
  ['/admin/instagram', 'Instagram'],
  ['/admin/email', 'Email'],
  ['/admin/tools', 'Settings'],
  ['/admin/activity', 'Activity'],
]

export default function AdminLayout() {
  const { state } = useAuth()
  const onNav = usePageNav()

  if (state.status !== 'authenticated') return null

  return (
    <div className="admin-shell">
      <MSHeader active="home" onNav={onNav} />
      <div className="admin-body">
        <aside className="admin-sidebar">
          <span className="admin-brand-tag">Admin</span>
          <nav className="admin-sidebar-nav" aria-label="Admin">
            {ADMIN_NAV.map(([to, label]) => (
              <NavLink key={to} to={to} end={to === '/admin'} className="admin-nav-link">
                {label}
              </NavLink>
            ))}
          </nav>
        </aside>
        <main className="admin-main">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
