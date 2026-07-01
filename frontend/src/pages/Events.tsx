import { useEffect, useState } from 'react'
import Seo from '../Seo'
import { fetchUpcomingEvents, type UpcomingEvent } from '../api/events'
import { MSFooter, MSHeader, usePageNav } from './home/Chrome'
import { MSEyebrow, MSSection, Photo } from './home/Primitives'
import { BP_TABLET, useMediaQuery } from './home/responsive'
import { eventDayFmt, eventIcon, formatEventTimeRange } from './home/Sections'

export default function Events() {
  const handleNav = usePageNav('events')
  const [events, setEvents] = useState<UpcomingEvent[] | null>(null)
  const [loadFailed, setLoadFailed] = useState(false)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const isStacked = useMediaQuery(BP_TABLET)

  useEffect(() => {
    let cancelled = false
    fetchUpcomingEvents(50)
      .then((items) => { if (!cancelled) setEvents(items) })
      .catch(() => {
        if (cancelled) return
        setEvents([])
        setLoadFailed(true)
      })
    return () => { cancelled = true }
  }, [])

  const selected = events?.find((e) => e.id === selectedId) ?? events?.[0]

  return (
    <div data-screen-label="SWCG · events">
      <Seo
        title="Events"
        description="Workshops, working bees and community events at Seaford Wetlands Community Garden. See what's coming up and join us in the garden in Seaford, SA."
      />
      <MSHeader active="events" onNav={handleNav} />

      <MSSection bg="var(--paper)" py={88}>
        <MSEyebrow>What&rsquo;s on</MSEyebrow>
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
          Events.
        </h1>
        <p style={{ fontSize: 17, lineHeight: 1.65, color: 'var(--fg-2)', margin: 0, maxWidth: 640 }}>
          Workshops, working bees and get-togethers at the garden. Pick an
          event from the list to see the details.
        </p>
      </MSSection>

      <MSSection bg="var(--paper-soft)" py={96}>
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
        {events !== null && events.length > 0 && selected && (
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: isStacked ? '1fr' : 'minmax(300px, 380px) 1fr',
              gap: 28,
              alignItems: 'start',
            }}
          >
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12, order: isStacked ? 1 : 0 }}>
              {events.map((e) => {
                const isSelected = e.id === selected.id
                const start = new Date(e.startUtc)
                return (
                  <button
                    key={e.id}
                    type="button"
                    onClick={() => setSelectedId(e.id)}
                    aria-pressed={isSelected}
                    style={{
                      textAlign: 'left',
                      cursor: 'pointer',
                      background: isSelected ? 'var(--ivory)' : 'transparent',
                      border: isSelected ? '1px solid var(--apple-600)' : '1px solid var(--ink-200)',
                      borderRadius: 'var(--r-lg)',
                      padding: '16px 18px',
                      display: 'flex',
                      flexDirection: 'row',
                      alignItems: 'center',
                      gap: 14,
                      transition: 'all 120ms var(--ease-out)',
                    }}
                  >
                    <div
                      style={{
                        flex: '0 0 auto',
                        width: 56,
                        height: 56,
                        borderRadius: 'var(--r-md)',
                        overflow: 'hidden',
                      }}
                    >
                      {e.imageUrl ? (
                        <img
                          src={e.imageUrl}
                          alt=""
                          style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
                        />
                      ) : (
                        <Photo
                          ratio="1 / 1"
                          tone={e.tone}
                          icon={eventIcon(e.tone, 26)}
                          style={{ borderRadius: 0, width: '100%', height: '100%' }}
                        />
                      )}
                    </div>
                    <span style={{ display: 'flex', flexDirection: 'column', gap: 6, minWidth: 0 }}>
                      <span style={{ fontSize: 12, color: 'var(--fg-4)', fontFamily: 'var(--font-mono)' }}>
                        {eventDayFmt.format(start)} &middot; {formatEventTimeRange(e.startUtc, e.endUtc)}
                      </span>
                      <span
                        style={{
                          fontFamily: 'var(--font-display)',
                          fontWeight: 700,
                          fontSize: 18,
                          letterSpacing: '-0.015em',
                          textTransform: 'uppercase',
                          color: isSelected ? 'var(--apple-700)' : 'var(--ink-900)',
                        }}
                      >
                        {e.title}
                      </span>
                    </span>
                  </button>
                )
              })}
            </div>

            <article
              style={{
                background: 'var(--ivory)',
                border: '1px solid var(--ink-200)',
                borderRadius: 'var(--r-lg)',
                overflow: 'hidden',
                position: isStacked ? 'static' : 'sticky',
                top: 96,
                order: isStacked ? 0 : 1,
              }}
            >
              {selected.imageUrl ? (
                <img
                  src={selected.imageUrl}
                  alt=""
                  style={{
                    width: '100%',
                    aspectRatio: '16 / 7',
                    objectFit: 'cover',
                    display: 'block',
                  }}
                />
              ) : (
                <Photo
                  ratio="16 / 7"
                  tone={selected.tone}
                  icon={eventIcon(selected.tone)}
                  style={{ borderRadius: 0 }}
                />
              )}
              <div style={{ padding: isStacked ? '22px 22px 26px' : '28px 32px 32px', display: 'flex', flexDirection: 'column', gap: 14 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
                  <span
                    style={{
                      background: selected.source === 'eventbrite' ? 'var(--ink-100)' : 'var(--apple-100)',
                      color: selected.source === 'eventbrite' ? 'var(--ink-900)' : 'var(--apple-800)',
                      fontSize: 11,
                      fontWeight: 700,
                      padding: '3px 9px',
                      borderRadius: 999,
                      letterSpacing: '0.04em',
                    }}
                  >
                    {selected.source === 'eventbrite' ? 'Eventbrite' : 'Garden'}
                  </span>
                  <span style={{ fontSize: 13, color: 'var(--fg-4)', fontFamily: 'var(--font-mono)' }}>
                    {eventDayFmt.format(new Date(selected.startUtc))} &middot; {formatEventTimeRange(selected.startUtc, selected.endUtc)}
                  </span>
                </div>
                <h2
                  style={{
                    fontFamily: 'var(--font-display)',
                    fontWeight: 700,
                    fontSize: 32,
                    lineHeight: 1.05,
                    letterSpacing: '-0.02em',
                    textTransform: 'uppercase',
                    margin: 0,
                  }}
                >
                  {selected.title}
                </h2>
                {selected.location && (
                  <span style={{ fontSize: 13, color: 'var(--fg-3)', fontFamily: 'var(--font-mono)' }}>
                    📍 {selected.location}
                  </span>
                )}
                <p
                  style={{
                    fontSize: 15,
                    lineHeight: 1.65,
                    color: 'var(--fg-2)',
                    margin: 0,
                    whiteSpace: 'pre-line',
                  }}
                >
                  {selected.body}
                </p>
                {selected.url && (
                  <a
                    href={selected.url}
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
          </div>
        )}
      </MSSection>

      <MSFooter onNav={handleNav} />
    </div>
  )
}
