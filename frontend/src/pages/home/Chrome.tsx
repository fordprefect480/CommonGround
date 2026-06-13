import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../AuthContext'
import { MSButton } from './Primitives'

export type NavId =
  | 'home'
  | 'membership'
  | 'lease'
  | 'blog'
  | 'events'
  | 'donate'
  | 'resources'
  | 'contact'
  | 'acknowledgement'

interface ChromeProps {
  active: NavId
  onNav: (id: NavId) => void
}

const PAGE_ROUTES: Partial<Record<NavId, string>> = {
  home: '/',
  membership: '/membership',
  lease: '/lease-a-plot',
  blog: '/blog',
  events: '/events',
  donate: '/donate',
  resources: '/resources',
}

/**
 * Shared nav handler for standalone pages. Clicking the current page scrolls
 * to the top; other ids navigate to their route, or to the matching home
 * section for ids without a page of their own.
 */
export function usePageNav(current?: NavId) {
  const navigate = useNavigate()
  return useCallback((id: NavId) => {
    if (id === current) {
      window.scrollTo({ top: 0, behavior: 'smooth' })
      return
    }
    const route = PAGE_ROUTES[id]
    navigate(route ?? `/#section-${id}`)
  }, [navigate, current])
}

const NAV_ITEMS: ReadonlyArray<readonly [NavId, string]> = [
  ['membership', 'Membership'],
  ['lease', 'Lease a plot'],
  ['blog', 'Blog'],
  ['events', 'Events'],
  ['donate', 'Donate'],
  ['resources', 'Resources'],
]

export function MSHeader({ active, onNav }: ChromeProps) {
  const { state } = useAuth()
  const navigate = useNavigate()
  return (
    <header
      style={{
        position: 'sticky',
        top: 0,
        zIndex: 50,
        background: 'rgba(250,246,238,0.94)',
        backdropFilter: 'blur(10px)',
        borderBottom: '1px solid var(--ink-100)',
      }}
    >
      <div
        style={{
          maxWidth: 1240,
          margin: '0 auto',
          padding: '14px 24px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 16,
        }}
      >
        <a
          href="#"
          onClick={(e) => {
            e.preventDefault()
            onNav('home')
          }}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 12,
            textDecoration: 'none',
            flexShrink: 0,
          }}
        >
          <img
            src="/swcg/logo-apple.png"
            width="44"
            height="52"
            alt=""
            style={{ objectFit: 'contain', display: 'block' }}
          />
          <div style={{ lineHeight: 1.05 }}>
            <div
              style={{
                fontSize: 9,
                fontWeight: 700,
                color: 'var(--ink-700)',
                letterSpacing: '0.08em',
                textTransform: 'uppercase',
              }}
            >
              Mid-Coast Sustainability Inc.
            </div>
            <div
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 800,
                fontSize: 16,
                color: 'var(--ink-900)',
                letterSpacing: '-0.01em',
                textTransform: 'uppercase',
                marginTop: 2,
              }}
            >
              Seaford Wetlands<br />Community Garden
            </div>
          </div>
        </a>
        <nav
          style={{
            display: 'flex',
            gap: 2,
            alignItems: 'center',
            flexShrink: 1,
            minWidth: 0,
          }}
        >
          {NAV_ITEMS.map(([id, label]) => (
            <a
              key={id}
              href="#"
              onClick={(e) => {
                e.preventDefault()
                onNav(id)
              }}
              style={{
                fontFamily: 'var(--font-sans)',
                fontWeight: 500,
                fontSize: 13,
                color: active === id ? 'var(--apple-700)' : 'var(--ink-700)',
                textDecoration: 'none',
                padding: '7px 9px',
                borderRadius: 'var(--r-md)',
                background: active === id ? 'var(--apple-100)' : 'transparent',
                transition: 'all 120ms var(--ease-out)',
                whiteSpace: 'nowrap',
              }}
            >
              {label}
            </a>
          ))}
          {state.status !== 'authenticated' && (
            <MSButton
              size="sm"
              onClick={() => navigate('/membership?join=1')}
              style={{ marginLeft: 6, whiteSpace: 'nowrap' }}
            >
              Join
            </MSButton>
          )}
          <AuthChip />
        </nav>
      </div>
    </header>
  )
}

function AuthChip() {
  const { state, signOut } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const wrapperRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    const onPointerDown = (e: PointerEvent) => {
      if (!wrapperRef.current?.contains(e.target as Node)) setOpen(false)
    }
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false)
    }
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  if (state.status !== 'authenticated') {
    if (state.status === 'loading') return null
    return (
      <Link
        to="/login"
        style={{
          fontFamily: 'var(--font-sans)',
          fontWeight: 500,
          fontSize: 13,
          color: 'var(--ink-700)',
          textDecoration: 'none',
          padding: '7px 9px',
          borderRadius: 'var(--r-md)',
          marginLeft: 6,
          whiteSpace: 'nowrap',
        }}
      >
        Sign in
      </Link>
    )
  }

  const { me } = state
  const handleSignOut = async () => {
    setOpen(false)
    await signOut()
    navigate('/')
  }

  const itemStyle: React.CSSProperties = {
    fontFamily: 'var(--font-sans)',
    fontWeight: 500,
    fontSize: 13,
    color: 'var(--ink-900)',
    textDecoration: 'none',
    padding: '9px 12px',
    borderRadius: 'var(--r-sm, 6px)',
    background: 'transparent',
    border: 'none',
    cursor: 'pointer',
    textAlign: 'left',
    whiteSpace: 'nowrap',
    display: 'block',
    width: '100%',
  }

  return (
    <div
      ref={wrapperRef}
      style={{ position: 'relative', marginLeft: 6 }}
    >
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="menu"
        aria-expanded={open}
        title="Account menu"
        style={{
          fontFamily: 'var(--font-sans)',
          fontWeight: 500,
          fontSize: 13,
          color: 'var(--ink-900)',
          background: 'var(--apple-100)',
          border: 'none',
          padding: '7px 9px',
          borderRadius: 'var(--r-md)',
          cursor: 'pointer',
          whiteSpace: 'nowrap',
          display: 'inline-flex',
          alignItems: 'center',
          gap: 6,
        }}
      >
        {me.displayName ?? me.email}
        <span
          aria-hidden="true"
          style={{
            display: 'inline-block',
            transition: 'transform 120ms var(--ease-out)',
            transform: open ? 'rotate(180deg)' : 'rotate(0deg)',
            fontSize: 10,
            lineHeight: 1,
          }}
        >
          ▾
        </span>
      </button>
      {open && (
        <div
          role="menu"
          style={{
            position: 'absolute',
            top: 'calc(100% + 6px)',
            right: 0,
            minWidth: 180,
            background: 'var(--paper, #fff)',
            border: '1px solid var(--ink-100)',
            borderRadius: 'var(--r-md)',
            boxShadow: '0 8px 24px rgba(0,0,0,0.08)',
            padding: 4,
            zIndex: 60,
            display: 'flex',
            flexDirection: 'column',
            gap: 2,
          }}
        >
          <Link
            to="/profile"
            role="menuitem"
            onClick={() => setOpen(false)}
            style={itemStyle}
          >
            Profile
          </Link>
          {me.isAdmin && (
            <Link
              to="/admin"
              role="menuitem"
              onClick={() => setOpen(false)}
              style={{ ...itemStyle, color: 'var(--apple-700)' }}
            >
              Admin
            </Link>
          )}
          <button
            type="button"
            role="menuitem"
            onClick={handleSignOut}
            style={{ ...itemStyle, color: 'var(--ink-700)' }}
          >
            Sign out
          </button>
        </div>
      )}
    </div>
  )
}

const FOOTER_GROUPS: ReadonlyArray<{
  title: string
  links: ReadonlyArray<readonly [NavId, string]>
}> = [
  {
    title: 'Get involved',
    links: [
      ['membership', 'Membership'],
      ['lease', 'Lease a plot'],
      ['donate', 'Donate'],
    ],
  },
  {
    title: "What's on",
    links: [
      ['events', 'Events'],
      ['blog', 'Blog'],
      ['resources', 'Resources'],
    ],
  },
  {
    title: 'About',
    links: [
      ['contact', 'Contact us'],
      ['acknowledgement', 'Acknowledgement'],
    ],
  },
]

export function MSFooter({ onNav }: { onNav: (id: NavId) => void }) {
  return (
    <footer
      style={{
        background: 'var(--ink-900)',
        color: 'var(--paper)',
        padding: '72px 0 32px',
      }}
    >
      <div style={{ maxWidth: 1240, margin: '0 auto', padding: '0 32px' }}>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '2fr 1fr 1fr 1fr',
            gap: 56,
            alignItems: 'start',
          }}
        >
          <div>
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 12,
                marginBottom: 18,
              }}
            >
              <img
                src="/swcg/logo-apple.png"
                width="48"
                height="56"
                alt=""
                style={{ objectFit: 'contain' }}
              />
              <div>
                <div
                  style={{
                    fontSize: 10,
                    fontWeight: 700,
                    letterSpacing: '0.1em',
                    textTransform: 'uppercase',
                    color: 'var(--apple-300)',
                  }}
                >
                  Mid-Coast Sustainability Inc.
                </div>
                <div
                  style={{
                    fontFamily: 'var(--font-display)',
                    fontWeight: 800,
                    fontSize: 18,
                    textTransform: 'uppercase',
                    letterSpacing: '-0.01em',
                    marginTop: 2,
                  }}
                >
                  Seaford Wetlands<br />Community Garden
                </div>
              </div>
            </div>
            <p
              style={{
                fontSize: 14,
                lineHeight: 1.65,
                color: 'rgba(250,246,238,0.7)',
                maxWidth: 380,
                margin: '0 0 16px',
              }}
            >
              South Australia&rsquo;s largest community garden - a
              community garden, public orchard, regenerating bushland park
              and community hub.
            </p>
            <div
              style={{
                fontSize: 13,
                color: 'rgba(250,246,238,0.55)',
                lineHeight: 1.7,
              }}
            >
              Seaford Rd, Seaford SA 5169<br />
              <a
                href="mailto:seafordcg@gmail.com"
                style={{ color: 'var(--apple-300)', textDecoration: 'none' }}
              >
                seafordcg@gmail.com
              </a>
            </div>
          </div>

          {FOOTER_GROUPS.map((group) => (
            <div key={group.title}>
              <div
                style={{
                  fontSize: 11,
                  fontWeight: 700,
                  letterSpacing: '0.16em',
                  textTransform: 'uppercase',
                  color: 'var(--apple-300)',
                  marginBottom: 14,
                }}
              >
                {group.title}
              </div>
              <ul
                style={{
                  listStyle: 'none',
                  padding: 0,
                  margin: 0,
                  display: 'flex',
                  flexDirection: 'column',
                  gap: 10,
                }}
              >
                {group.links.map(([id, label]) => (
                  <li key={id}>
                    <a
                      href="#"
                      onClick={(e) => {
                        e.preventDefault()
                        onNav(id)
                      }}
                      style={{
                        color: 'rgba(250,246,238,0.85)',
                        textDecoration: 'none',
                        fontSize: 14,
                      }}
                    >
                      {label}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div
          style={{
            height: 1,
            background: 'rgba(250,246,238,0.1)',
            margin: '48px 0 24px',
          }}
        />

        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: 16,
          }}
        >
          <div
            style={{
              fontSize: 12,
              color: 'rgba(250,246,238,0.5)',
              maxWidth: 640,
              lineHeight: 1.6,
            }}
          >
            We acknowledge the Kaurna people as the Traditional Owners of
            the land on which we garden, and pay our respects to Elders
            past, present and emerging.
          </div>
          <div
            style={{
              fontSize: 12,
              color: 'rgba(250,246,238,0.5)',
              display: 'flex',
              gap: 18,
              alignItems: 'center',
            }}
          >
            <span>Seeded by &copy; 2026 Mid Coast Sustainability Inc.</span>
            <Link
              to="/admin"
              style={{
                color: 'var(--apple-300)',
                textDecoration: 'none',
              }}
            >
              Admin
            </Link>
          </div>
        </div>
      </div>
    </footer>
  )
}
