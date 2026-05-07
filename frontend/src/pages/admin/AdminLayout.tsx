import { useEffect, useState } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { fetchAdminMe, logout, type AdminMe } from '../../api/auth'
import AdminLogin from './AdminLogin'

type AuthState =
  | { status: 'loading' }
  | { status: 'anonymous' }
  | { status: 'authenticated'; me: AdminMe }

export default function AdminLayout() {
  const [auth, setAuth] = useState<AuthState>({ status: 'loading' })
  const navigate = useNavigate()

  const refresh = async (): Promise<AdminMe | null> => {
    const me = await fetchAdminMe()
    setAuth(me ? { status: 'authenticated', me } : { status: 'anonymous' })
    return me
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
        <div className="admin-header-inner">
          <div className="admin-brand-block">
            <Link to="/" className="admin-brand">
              <img
                src="/swcg/logo-apple.png"
                width={44}
                height={52}
                alt=""
                className="admin-brand-logo"
              />
              <div className="admin-brand-text">
                <div className="admin-brand-overline">Mid-Coast Sustainability Inc.</div>
                <div className="admin-brand-wordmark">Seaford Wetlands<br />Community Garden</div>
              </div>
            </Link>
            <span className="admin-brand-tag">Admin</span>
          </div>
          <nav className="admin-nav" aria-label="Admin">
            <NavLink to="/admin/members" className="admin-nav-link">Members</NavLink>
            <NavLink to="/admin/blog" className="admin-nav-link">Blog</NavLink>
            <NavLink to="/admin/tools" className="admin-nav-link">Tools</NavLink>
            <NavLink to="/admin/profile" className="admin-nav-link" title="Edit profile">
              {auth.me.displayName ?? auth.me.email}
            </NavLink>
            <button type="button" className="admin-logout" onClick={handleLogout}>
              Sign out
            </button>
          </nav>
        </div>
      </header>
      <main className="admin-main">
        <Outlet />
      </main>
    </div>
  )
}
