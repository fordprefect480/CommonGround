import { Link } from 'react-router-dom'
import Seo from '../Seo'
import { useAppConfig } from '../AppConfigContext'
import { formatPrice } from '../format'
import { MSFooter, MSHeader, usePageNav } from './home/Chrome'
import { MSButton, MSEyebrow, MSSection } from './home/Primitives'
import { BP_MOBILE, BP_TABLET, useMediaQuery } from './home/responsive'

const BED_FEATURES: ReadonlyArray<string> = [
  'compost, mulch, fertiliser and water are all included in your rental',
  'standard beds are 2.4m x 1.2m and 0.6m in height',
  'produce from your bed is for your own consumption',
  'wheelchair-accessible beds are available - let us know when you apply',
  'two communal wicking beds are planned',
  'plants are not to overgrow onto the ground - please use your own trellising within the bed',
]

const PLOTHOLDER_POLICY_URL =
  'https://docs.google.com/document/d/1qo1aQFR9cNnfqWeZBeoupWW1nB5y596vn5SLJeGq5v0/edit?usp=sharing'

export default function LeaseAPlot() {
  const config = useAppConfig()
  const priceLabel = formatPrice(config.membershipPriceCents)
  const bedPriceLabel = formatPrice(config.leasedBedPriceCents)
  const handleNav = usePageNav('lease')
  const isTablet = useMediaQuery(BP_TABLET)
  const isMobile = useMediaQuery(BP_MOBILE)
  const detailCols = isMobile ? 1 : isTablet ? 2 : 3

  return (
    <div data-screen-label="SWCG · lease a plot">
      <Seo
        title="Lease a plot"
        description={`Lease your own raised garden bed at Seaford Wetlands Community Garden from ${bedPriceLabel}. Beds come with compost, mulch, fertiliser and water - grow your own vegetables, herbs and flowers.`}
      />
      <MSHeader active="lease" onNav={handleNav} />

      <MSSection bg="var(--paper)" py={88}>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: isTablet ? '1fr' : '1.2fr 1fr',
            gap: isTablet ? 32 : 64,
            alignItems: 'center',
          }}
        >
          <div>
            <MSEyebrow>Lease a plot</MSEyebrow>
            <h1
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 800,
                fontSize: 'clamp(44px, 5vw, 64px)',
                lineHeight: 0.98,
                letterSpacing: '-0.03em',
                textTransform: 'uppercase',
                margin: '16px 0 20px',
                fontVariationSettings: '"opsz" 48',
              }}
            >
              Grow your own.
            </h1>
            <p style={bodyPara}>
              We have a limited number of raised beds in our community garden
              for private rental each year. Produce from these beds is for
              individual consumption, and bed rental includes compost, mulch,
              fertiliser and water.
            </p>
            <p style={{ ...bodyPara, marginBottom: 0 }}>
              Rental rates are {bedPriceLabel}/year and raised bed rental is only
              available to members.
            </p>
          </div>
          <img
            src="/swcg/hero-image.png"
            alt="Raised garden beds in the community garden"
            style={{
              width: '100%',
              aspectRatio: '4 / 3',
              objectFit: 'cover',
              borderRadius: 'var(--r-lg)',
              display: 'block',
            }}
          />
        </div>
      </MSSection>

      <MSSection bg="var(--paper-soft)" py={96}>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: isTablet ? '1fr' : '1fr 1.3fr',
            gap: isTablet ? 32 : 64,
            alignItems: 'start',
          }}
        >
          <div>
            <MSEyebrow>Your raised bed</MSEyebrow>
            <h2
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 700,
                fontSize: 'clamp(30px, 5vw, 44px)',
                lineHeight: 1.05,
                letterSpacing: '-0.025em',
                textTransform: 'uppercase',
                margin: '14px 0 20px',
              }}
            >
              Just {bedPriceLabel} a year.
            </h2>
            <p style={bodyPara}>
              A private bed gives you your own space to grow what you like,
              with everything you need included.
            </p>
            <p style={{ ...bodyPara, marginBottom: 14 }}>
              Have a read of our{' '}
              <a href={PLOTHOLDER_POLICY_URL} target="_blank" rel="noopener noreferrer">
                Plotholder Policy
              </a>{' '}
              before applying.
            </p>
            <p style={{ ...bodyPara, marginBottom: 28 }}>
              Raised beds are only available to members. Become a
              member first, then{' '}
              <Link to="/login">sign in</Link> and apply for a bed from your
              profile.
            </p>
            <MSButton size="lg" onClick={() => handleNav('membership')}>
              Become a member
            </MSButton>
          </div>
          <ul
            style={{
              listStyle: 'none',
              padding: 0,
              margin: 0,
              display: 'flex',
              flexDirection: 'column',
              gap: 14,
            }}
          >
            {BED_FEATURES.map((feature) => (
              <li
                key={feature}
                style={{
                  display: 'flex',
                  gap: 12,
                  alignItems: 'flex-start',
                  fontSize: 16,
                  lineHeight: 1.55,
                  color: 'var(--fg-2)',
                }}
              >
                <img
                  src="/swcg/leaf-bullet.svg"
                  width="18"
                  height="18"
                  alt=""
                  style={{ flexShrink: 0, marginTop: 4 }}
                />
                {feature}
              </li>
            ))}
          </ul>
        </div>
      </MSSection>

      <MSSection bg="var(--paper)" py={96}>
        <MSEyebrow>How it works</MSEyebrow>
        <h2
          style={{
            fontFamily: 'var(--font-display)',
            fontWeight: 700,
            fontSize: 'clamp(30px, 5vw, 44px)',
            lineHeight: 1.05,
            letterSpacing: '-0.025em',
            textTransform: 'uppercase',
            margin: '14px 0 32px',
          }}
        >
          Getting a plot.
        </h2>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: `repeat(${detailCols}, 1fr)`,
            gap: 24,
            alignItems: 'stretch',
          }}
        >
          <DetailCard title="Become a member first">
            Raised bed rental is only available to members.
            Membership is just {priceLabel}/year and comes with plenty of other
            benefits.{' '}
            <a
              href="#"
              onClick={(e) => {
                e.preventDefault()
                handleNav('membership')
              }}
            >
              Become a member
            </a>
            .
          </DetailCard>
          <DetailCard title="New plotholders">
            If you don&rsquo;t yet have a plot number, get in touch to join
            the waitlist. A committee member will be in touch when a plot
            becomes available.
          </DetailCard>
          <DetailCard title="Already have a plot?">
            Plot leases are renewed each year. If you have a plot number
            already, or recently applied and have been given one, contact us
            to renew your lease and arrange payment.
          </DetailCard>
        </div>
      </MSSection>

      <MSFooter onNav={handleNav} />
    </div>
  )
}

function DetailCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div
      style={{
        background: 'var(--ivory)',
        border: '1px solid var(--ink-200)',
        borderRadius: 'var(--r-lg)',
        padding: '26px 28px',
      }}
    >
      <h3
        style={{
          fontFamily: 'var(--font-display)',
          fontWeight: 700,
          fontSize: 22,
          textTransform: 'uppercase',
          letterSpacing: '-0.01em',
          margin: '0 0 10px',
        }}
      >
        {title}
      </h3>
      <p style={{ fontSize: 15, lineHeight: 1.65, color: 'var(--fg-2)', margin: 0 }}>
        {children}
      </p>
    </div>
  )
}

const bodyPara: React.CSSProperties = {
  fontSize: 17,
  lineHeight: 1.65,
  color: 'var(--fg-2)',
  margin: '0 0 14px',
}
