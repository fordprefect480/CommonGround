import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../../AuthContext'

export default function AdminLayout() {
  const { state, signOut } = useAuth()
  const navigate = useNavigate()

  if (state.status !== 'authenticated') return null

  const me = state.me

  const handleLogout = async () => {
    await signOut()
    navigate('/')
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
            <NavLink to="/admin" end className="admin-nav-link">Dashboard</NavLink>
            <NavLink to="/admin/members" className="admin-nav-link">Members</NavLink>
            <NavLink to="/admin/blog" className="admin-nav-link">Blog</NavLink>
            <NavLink to="/admin/tools" className="admin-nav-link">Tools</NavLink>
            <NavLink to="/admin/activity" className="admin-nav-link">Activity</NavLink>
            <NavLink to="/admin/profile" className="admin-nav-link" title="Edit profile">
              {me.displayName ?? me.email}
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
