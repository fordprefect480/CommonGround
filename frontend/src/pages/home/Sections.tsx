import { useEffect, useRef, useState } from 'react'
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
import { BP_MOBILE, BP_TABLET, useMediaQuery } from './responsive'
import { useAppConfig } from '../../AppConfigContext'
import { sendContactMessage } from '../../api/contact'
import {
  fetchPublicInstagramPosts,
  type InstagramPost,
} from '../../api/instagram'
import { fetchUpcomingEvents, type UpcomingEvent } from '../../api/events'
import { loadTurnstileScript } from '../../turnstile'
import { MailingListModal } from './MailingListModal'

interface NavProps {
  onNav: (id: NavId) => void
}

export function HomeHero({ onNav }: NavProps) {
  const heroMask =
    'linear-gradient(to right, rgba(0,0,0,0.08) 0%, rgba(0,0,0,0.1) 35%, rgba(0,0,0,0.3) 50%, rgba(0,0,0,1) 70%)'
  const [mediaReady, setMediaReady] = useState(false)
  const [mailingOpen, setMailingOpen] = useState(false)
  const useVideo = useMediaQuery('(min-width: 768px)')
  const isMobile = useMediaQuery(BP_MOBILE)

  const headlineColor = isMobile ? 'var(--paper)' : 'var(--ink-900)'
  const accentColor = isMobile ? 'var(--apple-300)' : 'var(--apple-700)'
  const bodyColor = isMobile ? 'rgba(250,246,238,0.92)' : 'var(--fg-2)'
  const statLabelColor = isMobile ? 'rgba(250,246,238,0.82)' : 'var(--fg-3)'
  const statValueColor = isMobile ? 'var(--paper)' : 'var(--ink-900)'

  const mediaStyle: React.CSSProperties = {
    position: 'absolute',
    inset: 0,
    width: '100%',
    height: '100%',
    objectFit: 'cover',
    maskImage: isMobile ? 'none' : heroMask,
    WebkitMaskImage: isMobile ? 'none' : heroMask,
    pointerEvents: 'none',
    opacity: mediaReady ? 1 : 0,
    transition: 'opacity 700ms ease-out',
  }

  return (
    <section
      id="section-home"
      style={{ background: 'var(--paper)', position: 'relative', overflow: 'hidden' }}
    >
      {useVideo ? (
        <video
          src="/swcg/hero_compressed.mp4"
          autoPlay
          muted
          loop
          playsInline
          preload="auto"
          aria-hidden="true"
          onCanPlay={() => setMediaReady(true)}
          style={mediaStyle}
        />
      ) : (
        <img
          src="/swcg/hero-image.png"
          alt=""
          aria-hidden="true"
          onLoad={() => setMediaReady(true)}
          style={mediaStyle}
        />
      )}
      {isMobile && (
        <div
          aria-hidden="true"
          style={{
            position: 'absolute',
            inset: 0,
            pointerEvents: 'none',
            background:
              'linear-gradient(to bottom, rgba(22,20,15,0.35) 0%, rgba(22,20,15,0.55) 55%, rgba(22,20,15,0.80) 100%)',
          }}
        />
      )}
      <div
        style={{
          position: 'relative',
          maxWidth: 1240,
          margin: '0 auto',
          padding: isMobile ? '56px 20px 64px' : '88px 32px 72px',
        }}
      >
        <div style={{ maxWidth: 620 }}>
          <MSEyebrow color={isMobile ? 'var(--apple-200)' : undefined}>
            Seaford, South Australia &middot; Kaurna Country
          </MSEyebrow>
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
              color: headlineColor,
            }}
          >
            Seaford Wetlands
            <br />
            Community
            <br />
            <span style={{ color: accentColor }}>Garden.</span>
          </h1>
          <p
            style={{
              fontSize: 19,
              lineHeight: 1.5,
              color: bodyColor,
              maxWidth: 540,
              margin: '0 0 32px',
            }}
          >
            Welcome to South Australia&rsquo;s largest community garden in
            Seaford, SA. A community garden, public orchard, regenerating
            bushland park and community hub - right next to the
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
            <MSButton
              size="lg"
              variant="outlined"
              onClick={() => setMailingOpen(true)}
            >
              Join mailing list
            </MSButton>
          </div>
          <div
            style={{
              marginTop: 36,
              display: 'flex',
              gap: 32,
              fontSize: 13,
              color: statLabelColor,
            }}
          >
            <div>
              <strong
                style={{
                  color: statValueColor,
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
                  color: statValueColor,
                  fontSize: 18,
                  fontFamily: 'var(--font-display)',
                  fontWeight: 700,
                }}
              >
                $25
              </strong>{' '}
              a year
            </div>
            <div>
              <strong
                style={{
                  color: statValueColor,
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
      </div>
      <MailingListModal open={mailingOpen} onClose={() => setMailingOpen(false)} />
    </section>
  )
}

export function HomeAbout({ onNav }: NavProps) {
  const isTablet = useMediaQuery(BP_TABLET)
  return (
    <MSSection bg="var(--paper-soft)" py={104}>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: isTablet ? '1fr' : '1fr 1.2fr',
          gap: isTablet ? 32 : 64,
          alignItems: 'center',
        }}
      >
        <img
          src="/swcg/working-bee.jpg"
          alt="Volunteers at a working bee in the garden"
          style={{
            width: '100%',
            aspectRatio: '4 / 5',
            objectFit: 'cover',
            borderRadius: 'var(--r-lg)',
            display: 'block',
          }}
        />
        <div>
          <MSEyebrow>About the garden</MSEyebrow>
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
    body: 'A growing collection of native edibles - yam daisy, warrigal greens, finger lime and more.',
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
  const isMobile = useMediaQuery(BP_MOBILE)
  const isTablet = useMediaQuery(BP_TABLET)
  const cols = isMobile ? 1 : isTablet ? 2 : 4
  return (
    <MSSection py={96}>
      <div style={{ textAlign: 'center', marginBottom: 56 }}>
        <MSEyebrow>What&rsquo;s on the site</MSEyebrow>
        <h2
          style={{
            fontFamily: 'var(--font-display)',
            fontWeight: 700,
            fontSize: 'clamp(30px, 5vw, 44px)',
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
          gridTemplateColumns: `repeat(${cols}, 1fr)`,
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

export function eventIcon(tone: PhotoTone) {
  if (tone === 'leaf') return <Leaf size={64} style={{ opacity: 0.5 }} />
  if (tone === 'apple') return <Tomato size={64} style={{ opacity: 0.5 }} />
  return <Beet size={64} style={{ opacity: 0.5 }} />
}

export const eventDayFmt = new Intl.DateTimeFormat('en-AU', {
  weekday: 'short',
  day: 'numeric',
  month: 'short',
})
const eventTimeFmt = new Intl.DateTimeFormat('en-AU', {
  hour: 'numeric',
  minute: '2-digit',
  hour12: true,
})

function formatEventTime(d: Date): string {
  return eventTimeFmt.format(d).toLowerCase().replace(/\s/g, '')
}

export function formatEventTimeRange(startIso: string, endIso: string | null): string {
  const start = new Date(startIso)
  const startTime = formatEventTime(start)
  if (!endIso) return startTime
  const end = new Date(endIso)
  const endTime = formatEventTime(end)
  const sameDay = start.toDateString() === end.toDateString()
  return sameDay ? `${startTime}–${endTime}` : `${startTime} – ${eventDayFmt.format(end)} ${endTime}`
}

export function HomeEvents({ onNav }: NavProps) {
  const [events, setEvents] = useState<UpcomingEvent[] | null>(null)
  const [loadFailed, setLoadFailed] = useState(false)
  const isMobile = useMediaQuery(BP_MOBILE)
  const isTablet = useMediaQuery(BP_TABLET)
  const cols = isMobile ? 1 : isTablet ? 2 : 3

  useEffect(() => {
    let cancelled = false
    fetchUpcomingEvents(3)
      .then((items) => { if (!cancelled) setEvents(items) })
      .catch(() => {
        if (cancelled) return
        setEvents([])
        setLoadFailed(true)
      })
    return () => { cancelled = true }
  }, [])

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
              fontSize: 'clamp(30px, 5vw, 44px)',
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
      {events === null && (
        <p style={{ color: 'var(--fg-3)', margin: 0 }}>Loading events&hellip;</p>
      )}
      {events !== null && events.length === 0 && (
        <p style={{ color: 'var(--fg-2)', margin: 0, maxWidth: 540 }}>
          {loadFailed
            ? 'Events couldn’t be loaded right now. Please check back soon.'
            : 'Nothing on the calendar right now. Check back soon, or follow us on socials for live updates.'}
        </p>
      )}
      {events !== null && events.length > 0 && (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: `repeat(${cols}, 1fr)`,
            gap: 24,
          }}
        >
          {events.map((e) => {
            const start = new Date(e.startUtc)
            const sourceLabel = e.source === 'eventbrite' ? 'Eventbrite' : 'Garden'
            const sourceBg = e.source === 'eventbrite' ? 'var(--ink-100)' : 'var(--apple-100)'
            const sourceFg = e.source === 'eventbrite' ? 'var(--ink-900)' : 'var(--apple-800)'
            return (
              <article
                key={e.id}
                style={{
                  background: 'var(--ivory)',
                  border: '1px solid var(--ink-200)',
                  borderRadius: 'var(--r-lg)',
                  overflow: 'hidden',
                  display: 'flex',
                  flexDirection: 'column',
                }}
              >
                {e.imageUrl ? (
                  <img
                    src={e.imageUrl}
                    alt=""
                    style={{
                      width: '100%',
                      aspectRatio: '16 / 10',
                      objectFit: 'cover',
                      display: 'block',
                    }}
                  />
                ) : (
                  <Photo
                    ratio="16 / 10"
                    tone={e.tone}
                    icon={eventIcon(e.tone)}
                    style={{ borderRadius: 0 }}
                  />
                )}
                <div
                  style={{
                    padding: 24,
                    display: 'flex',
                    flexDirection: 'column',
                    gap: 10,
                    flex: 1,
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
                    <span
                      style={{
                        background: sourceBg,
                        color: sourceFg,
                        fontSize: 11,
                        fontWeight: 700,
                        padding: '3px 9px',
                        borderRadius: 999,
                        letterSpacing: '0.04em',
                      }}
                    >
                      {sourceLabel}
                    </span>
                    <span
                      style={{
                        fontSize: 12,
                        color: 'var(--fg-4)',
                        fontFamily: 'var(--font-mono)',
                      }}
                    >
                      {eventDayFmt.format(start)} &middot; {formatEventTimeRange(e.startUtc, e.endUtc)}
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
                  {e.url && (
                    <a
                      href={e.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      style={{
                        fontSize: 14,
                        fontWeight: 600,
                        color: 'var(--apple-700)',
                        textDecoration: 'none',
                        marginTop: 4,
                      }}
                    >
                      Learn more &rarr;
                    </a>
                  )}
                </div>
              </article>
            )
          })}
        </div>
      )}
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
            $25 a year.
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

interface Partner {
  name: string
  logo: string
  url: string
}

const PARTNERS: ReadonlyArray<Partner> = [
  { name: 'Green Adelaide', logo: '/swcg/partners/green-adelaide.png', url: 'https://www.greenadelaide.sa.gov.au/' },
  { name: 'Adelaide Edible Garden Trail', logo: '/swcg/partners/adelaide-edible-garden-trail.png', url: 'https://www.youtube.com/channel/UCPyvtMQYuBkA8hpc6P2V14g' },
  { name: 'Junction Australia', logo: '/swcg/partners/junction-australia.png', url: 'https://junctionaustralia.org.au/' },
  { name: 'Seaford Secondary College', logo: '/swcg/partners/seaford-secondary-college.png', url: 'https://www.seafordhs.sa.edu.au/' },
  { name: 'Chris Picton MP', logo: '/swcg/partners/chris-picton-mp.png', url: 'https://chrispicton.com.au/' },
  { name: 'Rotary Club of Seaford', logo: '/swcg/partners/rotary-club-of-seaford.png', url: 'https://www.seafordrotary.org.au/' },
  { name: 'Resilient South', logo: '/swcg/partners/resilient-south.png', url: 'https://www.resilientsouth.com/' },
  { name: 'Onkaparinga Food Security Collaborative', logo: '/swcg/partners/onkaparinga-food-security-collaborative.png', url: 'https://onkaparingafoodsecurity.org.au' },
  { name: 'City of Onkaparinga', logo: '/swcg/partners/city-of-onkaparinga.png', url: 'https://www.onkaparingacity.com/Home' },
  { name: 'Eco Logical Australia', logo: '/swcg/partners/eco-logical-australia.png', url: 'https://www.ecoaus.com.au/' },
  { name: 'Wollemi Natives', logo: '/swcg/partners/wollemi-natives.png', url: 'https://willunganatives.com/' },
  { name: 'Seaford C.F.S.', logo: '/swcg/partners/seaford-cfs.png', url: 'https://www.fire-brigade.asn.au/Station_Display.asp?Service_Code=SACFS&Station_Code=SEAF' },
  { name: 'Australian Association for Environmental Education', logo: '/swcg/partners/aaee.jpg', url: 'https://www.aaee.org.au/' },
  { name: 'Magain Real Estate Seaford', logo: '/swcg/partners/magain-real-estate-seaford.png', url: 'https://www.magain.com.au/agency/seaford/' },
  { name: 'Seaford and Districts Lions Club', logo: '/swcg/partners/seaford-districts-lions-club.jpg', url: 'https://www.facebook.com/seafordanddistrictslionsclub' },
]

function PartnerCard({ partner, ariaHidden }: { partner: Partner; ariaHidden?: boolean }) {
  return (
    <a
      href={partner.url}
      target="_blank"
      rel="noopener noreferrer"
      aria-hidden={ariaHidden}
      tabIndex={ariaHidden ? -1 : undefined}
      title={partner.name}
      className="partner-card"
      style={{
        flex: '0 0 auto',
        width: 168,
        marginRight: 16,
        background: '#fff',
        border: '1px solid var(--ink-100)',
        borderRadius: 'var(--r-md)',
        overflow: 'hidden',
        height: 80,
        padding: '12px 16px',
        boxSizing: 'border-box',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <img
        src={partner.logo}
        alt={partner.name}
        loading="lazy"
        style={{
          maxWidth: '100%',
          maxHeight: '100%',
          objectFit: 'contain',
        }}
      />
    </a>
  )
}

export function PartnersStrip() {
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
      <div className="partners-marquee" style={{ marginBottom: 28 }}>
        <div className="partners-marquee__track">
          {PARTNERS.map((p) => (
            <PartnerCard key={p.name} partner={p} />
          ))}
          {PARTNERS.map((p) => (
            <PartnerCard key={`${p.name}-dup`} partner={p} ariaHidden />
          ))}
        </div>
      </div>
    </MSSection>
  )
}

const INSTAGRAM_EMBED_SRC = 'https://www.instagram.com/embed.js'

function loadInstagramEmbedScript(): Promise<void> {
  if (typeof window === 'undefined') return Promise.resolve()
  if (window.instgrm?.Embeds) return Promise.resolve()
  const existing = document.querySelector<HTMLScriptElement>(`script[src="${INSTAGRAM_EMBED_SRC}"]`)
  if (existing) {
    return new Promise((resolve) => {
      if (window.instgrm?.Embeds) { resolve(); return }
      existing.addEventListener('load', () => resolve(), { once: true })
    })
  }
  return new Promise((resolve) => {
    const script = document.createElement('script')
    script.src = INSTAGRAM_EMBED_SRC
    script.async = true
    script.onload = () => resolve()
    document.head.appendChild(script)
  })
}

export function InstagramStrip() {
  const [livePosts, setLivePosts] = useState<InstagramPost[] | null>(null)

  useEffect(() => {
    let cancelled = false
    fetchPublicInstagramPosts(6)
      .then((posts) => { if (!cancelled) setLivePosts(posts) })
      .catch(() => { if (!cancelled) setLivePosts([]) })
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    if (!livePosts || livePosts.length === 0) return
    let cancelled = false
    loadInstagramEmbedScript().then(() => {
      if (cancelled) return
      window.instgrm?.Embeds.process()
    })
    return () => { cancelled = true }
  }, [livePosts])

  if (livePosts === null || livePosts.length === 0) return null

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
          href="https://www.instagram.com/seafordwetlandscg/"
          target="_blank"
          rel="noopener noreferrer"
          style={{
            fontSize: 14,
            fontWeight: 600,
            color: 'var(--apple-700)',
            textDecoration: 'none',
          }}
        >
          Follow on Instagram &rarr;
        </a>
      </div>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(3, 1fr)',
          gap: 16,
          alignItems: 'start',
        }}
      >
        {livePosts.map((tile) => (
          <div
            key={tile.id}
            dangerouslySetInnerHTML={{ __html: tile.embedHtml }}
          />
        ))}
      </div>
    </MSSection>
  )
}

type ContactFieldKey = 'name' | 'email' | 'subject'

const CONTACT_FIELDS: ReadonlyArray<{
  key: ContactFieldKey
  label: string
  type: 'text' | 'email'
  placeholder: string
}> = [
  { key: 'name', label: 'Name', type: 'text', placeholder: 'Sam Reilly' },
  { key: 'email', label: 'Email', type: 'email', placeholder: 'sam@example.com' },
  { key: 'subject', label: 'Subject', type: 'text', placeholder: 'I’d like to lease a plot' },
]

interface InstagramEmbedsApi {
  process: () => void
}

declare global {
  interface Window {
    instgrm?: { Embeds: InstagramEmbedsApi }
  }
}

export function ContactPage() {
  const config = useAppConfig()
  const captchaSiteKey = config.turnstileSiteKey ?? null
  const captchaContainerRef = useRef<HTMLDivElement | null>(null)
  const captchaWidgetIdRef = useRef<string | null>(null)
  const [captchaToken, setCaptchaToken] = useState<string | null>(null)
  const [form, setForm] = useState({ name: '', email: '', subject: '', message: '' })
  const [sent, setSent] = useState(false)
  const [sending, setSending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!captchaSiteKey || sent) return
    let cancelled = false
    loadTurnstileScript()
      .then((api) => {
        if (cancelled || !captchaContainerRef.current) return
        captchaWidgetIdRef.current = api.render(captchaContainerRef.current, {
          sitekey: captchaSiteKey,
          callback: (token) => setCaptchaToken(token),
          'error-callback': () => setCaptchaToken(null),
          'expired-callback': () => setCaptchaToken(null),
        })
      })
      .catch(() => {
        if (!cancelled) setError('Could not load the captcha widget. Please refresh the page.')
      })
    return () => {
      cancelled = true
      const id = captchaWidgetIdRef.current
      if (id && window.turnstile) {
        try {
          window.turnstile.remove(id)
        } catch {
          // widget already removed
        }
        captchaWidgetIdRef.current = null
      }
    }
  }, [captchaSiteKey, sent])

  const captchaReady = !captchaSiteKey || captchaToken !== null

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (sending) return
    if (captchaSiteKey && !captchaToken) {
      setError('Please complete the captcha before submitting.')
      return
    }
    setSending(true)
    setError(null)
    try {
      await sendContactMessage({
        name: form.name,
        email: form.email,
        subject: form.subject,
        message: form.message,
        captchaToken: captchaToken ?? undefined,
      })
      setSent(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not send your message. Please try again.')
      const id = captchaWidgetIdRef.current
      if (id && window.turnstile) {
        try {
          window.turnstile.reset(id)
        } catch {
          // widget gone - no-op
        }
      }
      setCaptchaToken(null)
    } finally {
      setSending(false)
    }
  }

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
            ideas, show your support - or anything else - get in
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
              <a
                href="https://www.facebook.com/SeafordCG"
                target="_blank"
                rel="noopener noreferrer"
              >
                Facebook
              </a>{' '}
              &middot;{' '}
              <a
                href="https://www.instagram.com/seafordwetlandscg"
                target="_blank"
                rel="noopener noreferrer"
              >
                Instagram
              </a>
            </div>
          </div>
        </div>
        <form
          onSubmit={handleSubmit}
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
                <label key={field.key}>
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
                    value={form[field.key]}
                    onChange={(e) =>
                      setForm((prev) => ({ ...prev, [field.key]: e.target.value }))
                    }
                    disabled={sending}
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
                  value={form.message}
                  onChange={(e) => setForm((prev) => ({ ...prev, message: e.target.value }))}
                  disabled={sending}
                  style={{ ...contactInput, resize: 'none' }}
                />
              </label>
              {captchaSiteKey && (
                <div ref={captchaContainerRef} style={{ minHeight: 65 }} />
              )}
              {error && (
                <div
                  role="alert"
                  style={{
                    fontSize: 14,
                    color: '#9B1B1B',
                    background: '#FBEAEA',
                    border: '1px solid #E8B9B9',
                    borderRadius: 'var(--r-md)',
                    padding: '10px 12px',
                  }}
                >
                  {error}
                </div>
              )}
              <MSButton
                type="submit"
                size="lg"
                disabled={sending || !captchaReady}
                style={{ marginTop: 8, alignSelf: 'flex-start' }}
              >
                {sending ? 'Sending…' : 'Submit'}
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
