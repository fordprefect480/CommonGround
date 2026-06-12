import { useEffect } from 'react'
import { useAppConfig } from '../AppConfigContext'
import { MSFooter, MSHeader, usePageNav } from './home/Chrome'
import { MSButton, MSEyebrow, MSSection } from './home/Primitives'

const BENEFITS: ReadonlyArray<string> = [
  'use of the community garden, including access codes, equipment and water',
  'pick from the communal garden spaces',
  'insurance coverage',
  'the ability to nominate for a committee position, and take a leadership role in our organisation and local community',
  'attendance and voting rights at the AGM each year — have your say on what the association should focus on, who should be on the steering committee and what community garden projects should be developed next',
  'invitations to club social events',
  'free training and workshop opportunities',
  'discounted rates on all public workshops and events',
]

const MEMBERSHIP_POLICY_URL =
  'https://docs.google.com/document/d/e/2PACX-1vTLkpl3NPXKBBSoaMlIGRp-j6OnU-jmZ5BG209WEY5SPZrA6GN0ql7LAJF9gCYx92oKBt1oEey-BCn7/pub'

export default function Membership() {
  const { gardenName } = useAppConfig()
  const handleNav = usePageNav('membership')

  useEffect(() => {
    document.title = `Membership | ${gardenName}`
    return () => { document.title = gardenName }
  }, [gardenName])

  return (
    <div data-screen-label="SWCG · membership">
      <MSHeader active="membership" onNav={handleNav} />

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
            <MSEyebrow>Membership</MSEyebrow>
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
              Become a member.
            </h1>
            <p style={bodyPara}>
              Seaford Wetlands Community Garden is managed by Mid Coast
              Sustainability Inc. We are a grass-roots, not-for-profit
              community group primarily focussed on community-led
              sustainability education and grass roots environmental action.
              Becoming a member is the best way to support our small
              organisation, which is entirely run by volunteers.
            </p>
            <p style={{ ...bodyPara, marginBottom: 0 }}>
              You don&rsquo;t need to be a gardener to be a member; just a
              person or business that cares about our local community and
              sustainability. Membership doesn&rsquo;t come with any
              expectations of time commitment, although we encourage and
              support everyone to participate as actively as they can in
              their local community!
            </p>
          </div>
          <img
            src="/swcg/working-bee.jpg"
            alt="Volunteers planting together in the garden"
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
            gridTemplateColumns: '1fr 1.3fr',
            gap: 64,
            alignItems: 'start',
          }}
        >
          <div>
            <MSEyebrow>What you get</MSEyebrow>
            <h2
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 700,
                fontSize: 44,
                lineHeight: 1.05,
                letterSpacing: '-0.025em',
                textTransform: 'uppercase',
                margin: '14px 0 20px',
              }}
            >
              Just $25 a year.
            </h2>
            <p style={bodyPara}>
              Membership is just $25/year and it comes with some great
              benefits.
            </p>
            <p style={{ ...bodyPara, marginBottom: 28 }}>
              Please have a read of our{' '}
              <a href={MEMBERSHIP_POLICY_URL} target="_blank" rel="noopener noreferrer">
                Membership Policy
              </a>{' '}
              before joining. This is also available on our Resources page
              along with all of our other policies and guidelines.
            </p>
            <MSButton size="lg" onClick={() => handleNav('contact')}>
              Get in touch to join
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
            {BENEFITS.map((benefit) => (
              <li
                key={benefit}
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
                {benefit}
              </li>
            ))}
          </ul>
        </div>
      </MSSection>

      <MSSection bg="var(--paper)" py={96}>
        <MSEyebrow>The details</MSEyebrow>
        <h2
          style={{
            fontFamily: 'var(--font-display)',
            fontWeight: 700,
            fontSize: 44,
            lineHeight: 1.05,
            letterSpacing: '-0.025em',
            textTransform: 'uppercase',
            margin: '14px 0 32px',
          }}
        >
          Good to know.
        </h2>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '1fr 1fr 1fr',
            gap: 24,
            alignItems: 'stretch',
          }}
        >
          <DetailCard title="Renewals">
            Memberships are renewed at 1st July each year. This means a
            member&rsquo;s first year of membership may not be a full twelve
            months. If you sign up as a new member within 8 weeks of the 1st
            July you do not need to pay a renewal fee &mdash; your membership
            will be carried over to the new financial year.
          </DetailCard>
          <DetailCard title="Household memberships">
            Household memberships are available for $25/year but we will
            require details of all members who will attend the garden for
            insurance purposes. Only the nominated primary member has voting
            rights at the AGM.
          </DetailCard>
          <DetailCard title="Want your own bed?">
            Private raised beds are also available for lease to members, so
            you can grow what you like in your own space.{' '}
            <a
              href="#"
              onClick={(e) => {
                e.preventDefault()
                handleNav('lease')
              }}
            >
              Learn about leasing a plot
            </a>
            .
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
