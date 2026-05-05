import { useEffect, useState } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { fetchAdminMe, logout, type AdminMe } from '../../api/auth'
import { useAppConfig } from '../../AppConfigContext'
import AdminLogin from './AdminLogin'

type AuthState =
  | { status: 'loading' }
  | { status: 'anonymous' }
  | { status: 'authenticated'; me: AdminMe }

export default function AdminLayout() {
  const [auth, setAuth] = useState<AuthState>({ status: 'loading' })
  const navigate = useNavigate()
  const { gardenName } = useAppConfig()

  const refresh = async () => {
    const me = await fetchAdminMe()
    setAuth(me ? { status: 'authenticated', me } : { status: 'anonymous' })
  }

  useEffect(() => {
    refresh().catch(() => setAuth({ status: 'anonymous' }))
  }, [])

  const handleLogout = async () => {
    await logout()
    setAuth({ status: 'anonymous' })
    navigate('/admin')
  }

  if (auth.status === 'loading') {
    return (
      <main className="admin-shell">
        <p className="admin-loading">Checking your session&hellip;</p>
      </main>
    )
  }

  if (auth.status === 'anonymous') {
    return <AdminLogin onLoggedIn={refresh} />
  }

  return (
    <div className="admin-shell">
      <header className="admin-header">
        <div className="admin-brand">
          <Link to="/" className="admin-brand-link">{gardenName}</Link>
          <span className="admin-brand-tag">Admin</span>
        </div>
        <nav className="admin-nav" aria-label="Admin">
          <NavLink to="/admin/members" className="admin-nav-link">Members</NavLink>
          <NavLink to="/admin/blog" className="admin-nav-link">Blog</NavLink>
          <NavLink to="/admin/tools" className="admin-nav-link">Tools</NavLink>
        </nav>
        <div className="admin-actions">
          <NavLink to="/admin/profile" className="admin-whoami" title="Edit profile">
            {auth.me.displayName ?? auth.me.email}
          </NavLink>
          <button type="button" className="admin-logout" onClick={handleLogout}>
            Sign out
          </button>
        </div>
      </header>
      <main className="admin-main">
        <Outlet />
      </main>
    </div>
  )
}
