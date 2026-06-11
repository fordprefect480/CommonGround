import { useEffect, useRef, useState } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../../AuthContext'

export default function AdminLayout() {
  const { state, signOut } = useAuth()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!menuOpen) return
    const onPointerDown = (e: PointerEvent) => {
      if (!menuRef.current?.contains(e.target as Node)) setMenuOpen(false)
    }
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setMenuOpen(false)
    }
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [menuOpen])

  if (state.status !== 'authenticated') return null

  const me = state.me

  const handleLogout = async () => {
    setMenuOpen(false)
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
            <NavLink to="/admin/members" className="admin-nav-link">Membership</NavLink>
            <NavLink to="/admin/blog" className="admin-nav-link">Blog</NavLink>
            <NavLink to="/admin/events" className="admin-nav-link">Events</NavLink>
            <NavLink to="/admin/instagram" className="admin-nav-link">Instagram</NavLink>
            <NavLink to="/admin/email" className="admin-nav-link">Email</NavLink>
            <NavLink to="/admin/tools" className="admin-nav-link">Tools</NavLink>
            <NavLink to="/admin/activity" className="admin-nav-link">Activity</NavLink>
            <div ref={menuRef} className="admin-user-menu">
              <button
                type="button"
                className="admin-user-menu-trigger"
                aria-haspopup="menu"
                aria-expanded={menuOpen}
                title="Account menu"
                onClick={() => setMenuOpen((v) => !v)}
              >
                {me.displayName ?? me.email}
                <span aria-hidden="true" className="admin-user-menu-chevron">▾</span>
              </button>
              {menuOpen && (
                <div role="menu" className="admin-user-menu-panel">
                  <Link
                    to="/admin/profile"
                    role="menuitem"
                    className="admin-user-menu-item"
                    onClick={() => setMenuOpen(false)}
                  >
                    Profile
                  </Link>
                  <button
                    type="button"
                    role="menuitem"
                    className="admin-user-menu-item"
                    onClick={handleLogout}
                  >
                    Sign out
                  </button>
                </div>
              )}
            </div>
          </nav>
        </div>
      </header>
      <main className="admin-main">
        <Outlet />
      </main>
    </div>
  )
}
