import { useNavigate } from 'react-router-dom'
import Seo from '../Seo'
import { useAppConfig } from '../AppConfigContext'
import { MSButton } from './home/Primitives'

/**
 * Pre-launch gate. While the "coming soon" setting is on, the public sees this
 * page instead of the real SPA; only signed-in admins get through (see the gate
 * in App.tsx). The log-in button lets the owner authenticate and preview the
 * finished site before announcing it.
 */
export default function UnderConstruction() {
  const { gardenName } = useAppConfig()
  const navigate = useNavigate()

  return (
    <main
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '32px 20px',
        background: 'linear-gradient(160deg, var(--paper) 0%, #EBE3CF 100%)',
        fontFamily: 'var(--font-sans)',
        color: 'var(--ink-900)',
      }}
    >
      <Seo title="Coming soon" noindex />
      <div
        style={{
          maxWidth: 540,
          textAlign: 'center',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 20,
        }}
      >
        <img
          src="/swcg/logo-apple.png"
          width="72"
          height="85"
          alt=""
          style={{ flexShrink: 0, height: 'auto', objectFit: 'contain' }}
        />

        <div
          style={{
            fontWeight: 700,
            fontSize: 12,
            letterSpacing: '0.16em',
            textTransform: 'uppercase',
            color: 'var(--apple-700)',
          }}
        >
          Coming soon
        </div>

        <h1 style={{ fontFamily: 'var(--font-display)', fontSize: 34, lineHeight: 1.15, margin: 0 }}>
          {gardenName}
        </h1>

        <p style={{ fontSize: 17, lineHeight: 1.6, color: 'var(--ink-700, #4a4a42)', margin: 0 }}>
          Our site is offline for a bit &mdash; apologies! It will be back online ASAP.
       
          Stay up to date with our socials:{' '}
          <a href="https://www.facebook.com/SeafordCG" target="_blank" rel="noopener noreferrer" style={{ color: 'var(--apple-700)', fontWeight: 600 }}>
            Facebook
          </a>{' '}
          and{' '}
          <a href="https://www.instagram.com/seafordwetlandscg" target="_blank" rel="noopener noreferrer" style={{ color: 'var(--apple-700)', fontWeight: 600 }}>
            Instagram
          </a>.
        </p>

        <MSButton variant="primary" size="lg" onClick={() => navigate('/login')}>
          Log in
        </MSButton>
      </div>
    </main>
  )
}
