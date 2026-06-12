import { useEffect } from 'react'
import { useAppConfig } from '../AppConfigContext'
import { MSFooter, MSHeader, usePageNav } from './home/Chrome'
import { MSEyebrow, MSSection } from './home/Primitives'

const BANK_DETAILS: ReadonlyArray<readonly [string, string]> = [
  ['Account name', 'Mid Coast Sustainability Inc.'],
  ['BSB', '633-000'],
  ['Account number', '187908280'],
]

export default function Donate() {
  const { gardenName } = useAppConfig()
  const handleNav = usePageNav('donate')

  useEffect(() => {
    document.title = `Donate | ${gardenName}`
    return () => { document.title = gardenName }
  }, [gardenName])

  return (
    <div data-screen-label="SWCG · donate">
      <MSHeader active="donate" onNav={handleNav} />

      <MSSection bg="var(--paper)" py={88}>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '1.2fr 1fr',
            gap: 64,
            alignItems: 'center',
          }}
        >
          <div>
            <MSEyebrow>Donate</MSEyebrow>
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
              Help us grow.
            </h1>
            <p style={bodyPara}>
              Seaford Wetlands Community Garden is a grassroots organisation
              funded entirely through memberships, donations and grants, and
              powered by hardworking volunteers. Your gift will help us grow
              into a true asset to the community of Seaford and the
              surrounding suburbs.
            </p>
            <p style={{ ...bodyPara, marginBottom: 0 }}>
              Thank you so much for your support!
            </p>
          </div>
          <div
            style={{
              background: 'var(--ivory)',
              border: '1px solid var(--ink-200)',
              borderRadius: 'var(--r-lg)',
              padding: 32,
            }}
          >
            <h2
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 700,
                fontSize: 26,
                textTransform: 'uppercase',
                letterSpacing: '-0.01em',
                margin: '0 0 8px',
              }}
            >
              Donate by bank transfer
            </h2>
            <p style={{ fontSize: 15, lineHeight: 1.6, color: 'var(--fg-2)', margin: '0 0 20px' }}>
              You can donate to us online by making a payment to:
            </p>
            <dl
              style={{
                margin: 0,
                display: 'flex',
                flexDirection: 'column',
                gap: 14,
              }}
            >
              {BANK_DETAILS.map(([label, value]) => (
                <div key={label}>
                  <dt
                    style={{
                      fontSize: 12,
                      fontWeight: 700,
                      letterSpacing: '0.1em',
                      textTransform: 'uppercase',
                      color: 'var(--apple-700)',
                      marginBottom: 2,
                    }}
                  >
                    {label}
                  </dt>
                  <dd
                    style={{
                      margin: 0,
                      fontSize: 17,
                      fontWeight: 600,
                      color: 'var(--ink-900)',
                    }}
                  >
                    {value}
                  </dd>
                </div>
              ))}
            </dl>
          </div>
        </div>
      </MSSection>

      <MSFooter onNav={handleNav} />
    </div>
  )
}

const bodyPara: React.CSSProperties = {
  fontSize: 17,
  lineHeight: 1.65,
  color: 'var(--fg-2)',
  margin: '0 0 14px',
}
