import { useState } from 'react'
import {
  Beet,
  Carrot,
  Leaf,
  MSButton,
  MSEyebrow,
  MSSection,
  Photo,
  type PhotoTone,
  Tomato,
} from './Primitives'
import type { NavId } from './Chrome'

interface NavProps {
  onNav: (id: NavId) => void
}

export function HomeHero({ onNav }: NavProps) {
  return (
    <section
      id="section-home"
      style={{ background: 'var(--paper)', position: 'relative', overflow: 'hidden' }}
    >
      <div
        style={{
          maxWidth: 1240,
          margin: '0 auto',
          padding: '88px 32px 72px',
          display: 'grid',
          gridTemplateColumns: '1.3fr 1fr',
          gap: 56,
          alignItems: 'center',
        }}
      >
        <div>
          <MSEyebrow>Seaford, South Australia &middot; Kaurna Country</MSEyebrow>
          <h1
            style={{
              fontFamily: 'var(--font-display)',
              fontWeight: 800,
              fontSize: 'clamp(44px, 5.5vw, 76px)',
              lineHeight: 0.96,
              letterSpacing: '-0.03em',
              textTransform: 'uppercase',
              margin: '20px 0 24px',
              fontVariationSettings: '"opsz" 48',
            }}
          >
            Seaford Wetlands
            <br />
            Community
            <br />
            <span style={{ color: 'var(--apple-700)' }}>Garden.</span>
          </h1>
          <p
            style={{
              fontSize: 19,
              lineHeight: 1.5,
              color: 'var(--fg-2)',
              maxWidth: 540,
              margin: '0 0 32px',
            }}
          >
            Welcome to South Australia&rsquo;s largest community garden in
            Seaford, SA. A community garden, public orchard, regenerating
            bushland park and community hub &mdash; right next to the
            Seaford Wetlands.
          </p>
          <div
            style={{
              display: 'flex',
              gap: 12,
              alignItems: 'center',
              flexWrap: 'wrap',
            }}
          >
            <MSButton size="lg" onClick={() => onNav('membership')}>
              Become a member
            </MSButton>
            <MSButton size="lg" variant="outlined">
              Join mailing list
            </MSButton>
          </div>
          <div
            style={{
              marginTop: 36,
              display: 'flex',
              gap: 32,
              fontSize: 13,
              color: 'var(--fg-3)',
            }}
          >
            <div>
              <strong
                style={{
                  color: 'var(--ink-900)',
                  fontSize: 18,
                  fontFamily: 'var(--font-display)',
                  fontWeight: 700,
                }}
              >
                32
              </strong>{' '}
              raised beds
            </div>
            <div>
              <strong
                style={{
                  color: 'var(--ink-900)',
                  fontSize: 18,
                  fontFamily: 'var(--font-display)',
                  fontWeight: 700,
                }}
              >
                $20
              </strong>{' '}
              a year
            </div>
            <div>
              <strong
                style={{
                  color: 'var(--ink-900)',
                  fontSize: 18,
                  fontFamily: 'var(--font-display)',
                  fontWeight: 700,
                }}
              >
                Fri
              </strong>{' '}
              social group
            </div>
          </div>
        </div>
        <div style={{ position: 'relative', minHeight: 440 }}>
          <Photo
            ratio="3 / 4"
            tone="leaf"
            label="The garden"
            style={{ width: '100%', height: '100%' }}
            icon={<Leaf size={120} style={{ opacity: 0.4 }} />}
          />
          <img
            src="/swcg/logo-apple.png"
            alt=""
            style={{
              position: 'absolute',
              bottom: -30,
              left: -50,
              width: 200,
              transform: 'rotate(-8deg)',
              filter: 'drop-shadow(0 8px 24px rgba(22,20,15,0.18))',
            }}
          />
        </div>
      </div>
    </section>
  )
}

export function HomeAbout({ onNav }: NavProps) {
  return (
    <MSSection bg="var(--paper-soft)" py={104}>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '1fr 1.2fr',
          gap: 64,
          alignItems: 'center',
        }}
      >
        <Photo
          ratio="4 / 5"
          tone="flesh"
          label="Working bee · Apr 2025"
          icon={<Carrot size={120} style={{ opacity: 0.45 }} />}
        />
        <div>
          <MSEyebrow>About the garden</MSEyebrow>
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
            We&rsquo;ve established a community garden in Seaford.
          </h2>
          <p style={aboutPara}>
            After much planning and hard work we now have a wonderful space
            for gardeners in our community to gather together.
          </p>
          <p style={aboutPara}>
            Our garden includes <strong>32 raised beds for lease</strong>, a
            shared central community space, an Indigenous food garden, a
            market garden and a greenhouse. And there&rsquo;s more still to
            do.
          </p>
          <p style={{ ...aboutPara, marginBottom: 28 }}>
            With the Seaford Wetlands also right nearby, why not wander down
            and check us out? If you like what you see, sign up for
            membership below. We offer terrific workshops throughout the
            year and meet for a shared morning tea every fortnight.
          </p>
          <MSButton onClick={() => onNav('membership')}>Become a member</MSButton>
        </div>
      </div>
    </MSSection>
  )
}

const aboutPara: React.CSSProperties = {
  fontSize: 17,
  lineHeight: 1.65,
  color: 'var(--fg-2)',
  margin: '0 0 14px',
}

interface FeatureDef {
  icon: React.ReactNode
  title: string
  body: string
}

const FEATURES: ReadonlyArray<FeatureDef> = [
  {
    icon: <Tomato size={48} />,
    title: 'Raised beds for lease',
    body: '32 individual plots, ready for you to plant. Water, mulch and shared tools included.',
  },
  {
    icon: <Leaf size={48} />,
    title: 'Indigenous food garden',
    body: 'A growing collection of native edibles — yam daisy, warrigal greens, finger lime and more.',
  },
  {
    icon: <Carrot size={48} />,
    title: 'Market garden',
    body: 'A productive garden growing seasonal vegetables for the wider community.',
  },
  {
    icon: <Beet size={48} />,
    title: 'Greenhouse',
    body: 'Propagating seedlings year-round. Members get first dibs at planting time.',
  },
]

export function FeatureGrid() {
  return (
    <MSSection py={96}>
      <div style={{ textAlign: 'center', marginBottom: 56 }}>
        <MSEyebrow>What&rsquo;s on the site</MSEyebrow>
        <h2
          style={{
            fontFamily: 'var(--font-display)',
            fontWeight: 700,
            fontSize: 44,
            lineHeight: 1.05,
            letterSpacing: '-0.025em',
            textTransform: 'uppercase',
            margin: '14px 0 0',
          }}
        >
          More than a garden.
        </h2>
      </div>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(4, 1fr)',
          gap: 24,
        }}
      >
        {FEATURES.map((f) => (
          <div
            key={f.title}
            style={{
              background: 'var(--ivory)',
              border: '1px solid var(--ink-200)',
              borderRadius: 'var(--r-lg)',
              padding: 28,
            }}
          >
            <div style={{ marginBottom: 16 }}>{f.icon}</div>
            <h3
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 700,
                fontSize: 19,
                letterSpacing: '-0.01em',
                margin: '0 0 8px',
                textTransform: 'uppercase',
              }}
            >
              {f.title}
            </h3>
            <p
              style={{
                fontSize: 14,
                lineHeight: 1.55,
                color: 'var(--fg-2)',
                margin: 0,
              }}
            >
              {f.body}
            </p>
          </div>
        ))}
      </div>
    </MSSection>
  )
}

interface EventDef {
  date: string
  time: string
  kind: string
  title: string
  body: string
  tone: PhotoTone
}

const EVENTS: ReadonlyArray<EventDef> = [
  {
    date: 'Multiple dates',
    time: 'Fri 8 May · 9:30–11:30am',
    kind: 'Recurring',
    title: 'Gardening social group',
    body: 'Every Friday gardeners and volunteers meet at the garden to do a few of the regular maintenance tasks, tend their own beds and share gardening stories together over morning tea.',
    tone: 'leaf',
  },
  {
    date: 'Sat 16 May',
    time: '10am–2pm',
    kind: 'Working bee',
    title: 'Native bed planting',
    body: 'Preparing native garden beds between the new pathways. Lend a hand if you can.',
    tone: 'apple',
  },
  {
    date: 'Sun 14 Jun',
    time: '10am–12pm',
    kind: 'Workshop',
    title: 'Seed saving for beginners',
    body: 'Learn how to save and store seeds from your summer crops. Free for members.',
    tone: 'flesh',
  },
]

function eventIcon(tone: PhotoTone) {
  if (tone === 'leaf') return <Leaf size={64} style={{ opacity: 0.5 }} />
  if (tone === 'apple') return <Tomato size={64} style={{ opacity: 0.5 }} />
  return <Beet size={64} style={{ opacity: 0.5 }} />
}

export function HomeEvents({ onNav }: NavProps) {
  return (
    <MSSection bg="var(--paper-soft)" py={104} id="section-events">
      <div
        style={{
          display: 'flex',
          alignItems: 'flex-end',
          justifyContent: 'space-between',
          marginBottom: 40,
          flexWrap: 'wrap',
          gap: 16,
        }}
      >
        <div>
          <MSEyebrow>What&rsquo;s on</MSEyebrow>
          <h2
            style={{
              fontFamily: 'var(--font-display)',
              fontWeight: 700,
              fontSize: 44,
              lineHeight: 1.05,
              letterSpacing: '-0.025em',
              textTransform: 'uppercase',
              margin: '14px 0 0',
            }}
          >
            Coming up at the garden.
          </h2>
        </div>
        <MSButton variant="outlined" onClick={() => onNav('events')}>
          All events &rarr;
        </MSButton>
      </div>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(3, 1fr)',
          gap: 24,
        }}
      >
        {EVENTS.map((e) => (
          <article
            key={e.title}
            style={{
              background: 'var(--ivory)',
              border: '1px solid var(--ink-200)',
              borderRadius: 'var(--r-lg)',
              overflow: 'hidden',
              display: 'flex',
              flexDirection: 'column',
            }}
          >
            <Photo
              ratio="16 / 10"
              tone={e.tone}
              icon={eventIcon(e.tone)}
              style={{ borderRadius: 0 }}
            />
            <div
              style={{
                padding: 24,
                display: 'flex',
                flexDirection: 'column',
                gap: 10,
                flex: 1,
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <span
                  style={{
                    background: 'var(--apple-100)',
                    color: 'var(--apple-800)',
                    fontSize: 11,
                    fontWeight: 700,
                    padding: '3px 9px',
                    borderRadius: 999,
                    letterSpacing: '0.04em',
                  }}
                >
                  {e.kind}
                </span>
                <span
                  style={{
                    fontSize: 12,
                    color: 'var(--fg-4)',
                    fontFamily: 'var(--font-mono)',
                  }}
                >
                  {e.date} &middot; {e.time}
                </span>
              </div>
              <h3
                style={{
                  fontFamily: 'var(--font-display)',
                  fontWeight: 700,
                  fontSize: 22,
                  letterSpacing: '-0.015em',
                  margin: 0,
                  textTransform: 'uppercase',
                }}
              >
                {e.title}
              </h3>
              <p
                style={{
                  fontSize: 14,
                  lineHeight: 1.55,
                  color: 'var(--fg-2)',
                  margin: 0,
                  flex: 1,
                }}
              >
                {e.body}
              </p>
              <a
                href="#"
                style={{
                  fontSize: 14,
                  fontWeight: 600,
                  color: 'var(--apple-700)',
                  textDecoration: 'none',
                  marginTop: 4,
                }}
                onClick={(ev) => ev.preventDefault()}
              >
                Learn more &rarr;
              </a>
            </div>
          </article>
        ))}
      </div>
    </MSSection>
  )
}

export function MembershipBanner({ onNav }: NavProps) {
  return (
    <MSSection bg="var(--apple-700)" py={88} id="section-membership">
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '1.5fr 1fr',
          gap: 56,
          alignItems: 'center',
        }}
      >
        <div>
          <MSEyebrow color="var(--apple-200)">Membership</MSEyebrow>
          <h2
            style={{
              fontFamily: 'var(--font-display)',
              fontWeight: 800,
              fontSize: 56,
              lineHeight: 0.98,
              letterSpacing: '-0.03em',
              textTransform: 'uppercase',
              margin: '14px 0 16px',
              color: 'var(--paper)',
              fontVariationSettings: '"opsz" 48',
            }}
          >
            Join from
            <br />
            $20 a year.
          </h2>
          <p
            style={{
              fontSize: 18,
              lineHeight: 1.55,
              color: 'rgba(250,246,238,0.85)',
              maxWidth: 540,
              margin: '0 0 28px',
            }}
          >
            Lease a plot, attend workshops, share morning tea every
            fortnight, and help shape what the garden becomes.
          </p>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            <MSButton size="lg" variant="cream" onClick={() => onNav('membership')}>
              Become a member
            </MSButton>
            <MSButton
              size="lg"
              variant="outlined"
              onClick={() => onNav('lease')}
              style={{ borderColor: 'var(--paper)', color: 'var(--paper)' }}
            >
              Lease a plot &rarr;
            </MSButton>
          </div>
        </div>
        <div style={{ display: 'flex', justifyContent: 'center' }}>
          <img
            src="/swcg/logo-apple.png"
            alt=""
            style={{
              width: '100%',
              maxWidth: 280,
              transform: 'rotate(-6deg)',
              filter: 'drop-shadow(0 16px 32px rgba(0,0,0,0.25))',
            }}
          />
        </div>
      </div>
    </MSSection>
  )
}

const PARTNERS: ReadonlyArray<string> = [
  'Seaford Rotary',
  'City of Onkaparinga',
  'Green Adelaide',
  'Bunnings Seaford',
  'Mid Coast Sustainability',
  'Onkaparinga Council',
]

export function PartnersStrip({ onNav }: NavProps) {
  return (
    <MSSection py={88} id="section-partners">
      <div style={{ textAlign: 'center', marginBottom: 32 }}>
        <MSEyebrow>Partners &amp; sponsors</MSEyebrow>
        <h2
          style={{
            fontFamily: 'var(--font-display)',
            fontWeight: 700,
            fontSize: 36,
            lineHeight: 1.1,
            letterSpacing: '-0.025em',
            textTransform: 'uppercase',
            margin: '14px 0 12px',
          }}
        >
          Built together.
        </h2>
        <p
          style={{
            fontSize: 16,
            lineHeight: 1.6,
            color: 'var(--fg-2)',
            maxWidth: 640,
            margin: '0 auto',
          }}
        >
          Through the help of our local businesses and organisations,
          we&rsquo;ve gained an incredible amount of support already. Our
          collaborative work makes a world of difference.
        </p>
      </div>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(6, 1fr)',
          gap: 16,
          marginBottom: 28,
        }}
      >
        {PARTNERS.map((p) => (
          <div
            key={p}
            style={{
              background: 'var(--paper-soft)',
              border: '1px solid var(--ink-100)',
              borderRadius: 'var(--r-md)',
              padding: '24px 16px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              textAlign: 'center',
              minHeight: 80,
              fontFamily: 'var(--font-display)',
              fontWeight: 700,
              fontSize: 13,
              color: 'var(--ink-700)',
              letterSpacing: '-0.005em',
            }}
          >
            {p}
          </div>
        ))}
      </div>
      <div style={{ textAlign: 'center' }}>
        <MSButton variant="ghost" onClick={() => onNav('partners')}>
          See all our partners &rarr;
        </MSButton>
      </div>
    </MSSection>
  )
}

interface InstagramTile {
  caption: string
  meta: string
  tone: PhotoTone
}

const INSTAGRAM_TILES: ReadonlyArray<InstagramTile> = [
  { caption: 'Nature craft for kids', meta: 'Thu 23 Apr · 10:30am', tone: 'leaf' },
  { caption: 'Terrarium workshop wrap', meta: 'Open day · 19 Oct', tone: 'flesh' },
  { caption: 'Free workshop coming up', meta: 'bit.ly/swcg-small', tone: 'apple' },
  { caption: 'First working bee for 2025', meta: 'Sat 5 Apr · 11am–2pm', tone: 'earth' },
  { caption: 'Wave hello — new season', meta: 'Garden update', tone: 'sand' },
  { caption: 'Winter clean-up working bee', meta: 'Sat 27 Jul · 10am–2pm', tone: 'leaf' },
]

function instagramIcon(tone: PhotoTone) {
  if (tone === 'apple') return <Tomato size={40} style={{ opacity: 0.45 }} />
  if (tone === 'flesh') return <Carrot size={40} style={{ opacity: 0.45 }} />
  if (tone === 'earth') return <Beet size={40} style={{ opacity: 0.45 }} />
  return <Leaf size={40} style={{ opacity: 0.45 }} />
}

export function InstagramStrip() {
  return (
    <MSSection bg="var(--paper-soft)" py={88}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'flex-end',
          marginBottom: 32,
          flexWrap: 'wrap',
          gap: 12,
        }}
      >
        <div>
          <MSEyebrow>Instagram</MSEyebrow>
          <h2
            style={{
              fontFamily: 'var(--font-display)',
              fontWeight: 700,
              fontSize: 36,
              letterSpacing: '-0.025em',
              textTransform: 'uppercase',
              margin: '14px 0 4px',
            }}
          >
            @seafordwetlandscg
          </h2>
        </div>
        <a
          href="#"
          style={{
            fontSize: 14,
            fontWeight: 600,
            color: 'var(--apple-700)',
            textDecoration: 'none',
          }}
          onClick={(e) => e.preventDefault()}
        >
          Follow on Instagram &rarr;
        </a>
      </div>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(6, 1fr)',
          gap: 12,
        }}
      >
        {INSTAGRAM_TILES.map((tile, i) => (
          <div key={i} style={{ position: 'relative' }}>
            <Photo
              ratio="1 / 1"
              tone={tile.tone}
              icon={instagramIcon(tile.tone)}
              style={{ borderRadius: 'var(--r-md)' }}
            />
          </div>
        ))}
      </div>
    </MSSection>
  )
}

const CONTACT_FIELDS: ReadonlyArray<{
  label: string
  type: 'text' | 'email'
  placeholder: string
}> = [
  { label: 'Name', type: 'text', placeholder: 'Sam Reilly' },
  { label: 'Email', type: 'email', placeholder: 'sam@example.com' },
  { label: 'Subject', type: 'text', placeholder: 'I’d like to lease a plot' },
]

export function ContactPage() {
  const [sent, setSent] = useState(false)
  return (
    <MSSection bg="var(--paper)" py={88} id="section-contact">
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '1fr 1fr',
          gap: 56,
        }}
      >
        <div>
          <MSEyebrow>Contact</MSEyebrow>
          <h1
            style={{
              fontFamily: 'var(--font-display)',
              fontWeight: 800,
              fontSize: 56,
              lineHeight: 0.98,
              letterSpacing: '-0.03em',
              textTransform: 'uppercase',
              margin: '16px 0 20px',
            }}
          >
            We want to
            <br />
            hear from you.
          </h1>
          <p
            style={{
              fontSize: 17,
              lineHeight: 1.6,
              color: 'var(--fg-2)',
              margin: '0 0 28px',
              maxWidth: 460,
            }}
          >
            If you&rsquo;re keen to give us feedback on our plan, add your
            ideas, show your support &mdash; or anything else &mdash; get in
            touch.
          </p>
          <div
            style={{
              display: 'flex',
              flexDirection: 'column',
              gap: 12,
              fontSize: 15,
            }}
          >
            <div>
              <strong style={contactLabel}>Address</strong>
              <br />
              Seaford Rd, Seaford SA 5169
            </div>
            <div>
              <strong style={contactLabel}>Email</strong>
              <br />
              <a href="mailto:seafordcg@gmail.com">seafordcg@gmail.com</a>
            </div>
            <div>
              <strong style={contactLabel}>Socials</strong>
              <br />
              Facebook &middot; Instagram (@seafordwetlandscg)
            </div>
          </div>
        </div>
        <form
          onSubmit={(e) => {
            e.preventDefault()
            setSent(true)
          }}
          style={{
            background: 'var(--ivory)',
            border: '1px solid var(--ink-200)',
            borderRadius: 'var(--r-lg)',
            padding: 32,
            display: 'flex',
            flexDirection: 'column',
            gap: 14,
          }}
        >
          {sent ? (
            <div style={{ padding: '32px 0', textAlign: 'center' }}>
              <h3
                style={{
                  fontFamily: 'var(--font-display)',
                  fontWeight: 700,
                  fontSize: 26,
                  textTransform: 'uppercase',
                  margin: '0 0 8px',
                }}
              >
                Thanks for getting in touch!
              </h3>
              <p style={{ fontSize: 15, color: 'var(--fg-2)', margin: 0 }}>
                We&rsquo;ll reply within a few days.
              </p>
            </div>
          ) : (
            <>
              {CONTACT_FIELDS.map((field) => (
                <label key={field.label}>
                  <div
                    style={{
                      fontSize: 12,
                      fontWeight: 600,
                      marginBottom: 5,
                      color: 'var(--ink-900)',
                    }}
                  >
                    {field.label}
                  </div>
                  <input
                    type={field.type}
                    placeholder={field.placeholder}
                    required
                    style={contactInput}
                  />
                </label>
              ))}
              <label>
                <div
                  style={{
                    fontSize: 12,
                    fontWeight: 600,
                    marginBottom: 5,
                    color: 'var(--ink-900)',
                  }}
                >
                  Your message
                </div>
                <textarea
                  rows={4}
                  required
                  placeholder="Enter your message"
                  style={{ ...contactInput, resize: 'none' }}
                />
              </label>
              <MSButton
                type="submit"
                size="lg"
                style={{ marginTop: 8, alignSelf: 'flex-start' }}
              >
                Submit
              </MSButton>
            </>
          )}
        </form>
      </div>
    </MSSection>
  )
}

const contactLabel: React.CSSProperties = {
  fontFamily: 'var(--font-display)',
  textTransform: 'uppercase',
  letterSpacing: '0.04em',
}

const contactInput: React.CSSProperties = {
  width: '100%',
  boxSizing: 'border-box',
  fontFamily: 'var(--font-sans)',
  fontSize: 15,
  padding: '11px 14px',
  background: 'var(--paper)',
  border: '1px solid var(--ink-200)',
  borderRadius: 'var(--r-md)',
  color: 'var(--ink-900)',
  outline: 0,
}
