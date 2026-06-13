import { useEffect, useRef, useState } from 'react'
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../AuthContext'

type Crumb = { label: string; to?: string }

interface SectionConfig {
  label: string
  detail?: string
  new?: string
  edit?: string
  children?: Record<string, string>
}

const SECTIONS: Record<string, SectionConfig> = {
  members: { label: 'Membership', detail: 'Member' },
  blog: { label: 'Blog', new: 'New post', edit: 'Edit post' },
  instagram: { label: 'Instagram', new: 'New tile', edit: 'Edit tile' },
  events: { label: 'Events', new: 'New event', edit: 'Edit event' },
  email: { label: 'Email', new: 'New email', detail: 'Message' },
  tools: { label: 'Settings', children: { 'email-test': 'Send test email' } },
  activity: { label: 'Activity' },
  profile: { label: 'Profile' },
}

function buildCrumbs(pathname: string): Crumb[] {
  const segments = pathname.replace(/^\/admin\/?/, '').split('/').filter(Boolean)
  if (segments.length === 0) return []

  const crumbs: Crumb[] = [{ label: 'Dashboard', to: '/admin' }]
  const [key, ...rest] = segments
  const section = SECTIONS[key]
  if (!section) return crumbs

  crumbs.push({ label: section.label, to: `/admin/${key}` })
  if (rest.length === 0) return crumbs

  if (rest[0] === 'new') {
    crumbs.push({ label: section.new ?? 'New' })
  } else if (section.children?.[rest[0]]) {
    crumbs.push({ label: section.children[rest[0]] })
  } else if (rest[1] === 'edit') {
    crumbs.push({ label: section.edit ?? 'Edit' })
  } else {
    crumbs.push({ label: section.detail ?? section.label })
  }
  return crumbs
}

export default function AdminLayout() {
  const { state, signOut } = useAuth()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const crumbs = buildCrumbs(pathname)
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
            <NavLink to="/admin/tools" className="admin-nav-link">Settings</NavLink>
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
        {crumbs.length > 0 && (
          <nav className="admin-breadcrumbs" aria-label="Breadcrumb">
            <ol className="admin-breadcrumb-list">
              {crumbs.map((crumb, i) => {
                const isCurrent = i === crumbs.length - 1 || !crumb.to
                return (
                  <li key={crumb.to ?? crumb.label} className="admin-breadcrumb-item">
                    {isCurrent ? (
                      <span aria-current="page" className="admin-breadcrumb-current">{crumb.label}</span>
                    ) : (
                      <Link to={crumb.to!} className="admin-breadcrumb-link">{crumb.label}</Link>
                    )}
                  </li>
                )
              })}
            </ol>
          </nav>
        )}
        <Outlet />
      </main>
    </div>
  )
}
