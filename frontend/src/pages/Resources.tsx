import { useEffect } from 'react'
import { CircleMarker, MapContainer, Popup, TileLayer } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import { useAppConfig } from '../AppConfigContext'
import { MSFooter, MSHeader, usePageNav } from './home/Chrome'
import { MSEyebrow, MSSection } from './home/Primitives'

const DOCUMENTS: ReadonlyArray<{ title: string; description: string; url: string }> = [
  {
    title: 'Membership Policy',
    description: 'Everything you need to know about becoming and being a member, including fees, renewals and member benefits.',
    url: 'https://docs.google.com/document/d/e/2PACX-1vTLkpl3NPXKBBSoaMlIGRp-j6OnU-jmZ5BG209WEY5SPZrA6GN0ql7LAJF9gCYx92oKBt1oEey-BCn7/pub',
  },
  {
    title: 'Plotholder Policy',
    description: 'The rules and responsibilities that come with leasing a private raised bed in the garden.',
    url: 'https://docs.google.com/document/d/1qo1aQFR9cNnfqWeZBeoupWW1nB5y596vn5SLJeGq5v0/edit?usp=sharing',
  },
]

interface NearbyGarden {
  name: string
  address: string
  lat: number
  lng: number
  contact?: string
}

const NEARBY_GARDENS: ReadonlyArray<NearbyGarden> = [
  { name: 'Seaford Wetlands Community Garden', address: 'Grange Ct, Seaford SA 5169', lat: -35.1822546, lng: 138.4807322, contact: 'mailto:seafordcg@gmail.com?subject=Seaford Community Garden' },
  { name: 'Aberfoyle Community Garden', address: 'Budapest Rd, Aberfoyle Park SA 5159', lat: -35.0667892, lng: 138.5941861, contact: 'https://www.facebook.com/AberfoyleCommunityGarden/' },
  { name: 'Aldinga Community Garden', address: '7 Stewart Ave, Aldinga Beach SA 5173', lat: -35.272947, lng: 138.4540366, contact: 'mailto:ACC@aldingacc.org?subject=Aldinga Community Garden' },
  { name: 'Christie Downs Community House Community Garden', address: 'Morton Rd & Flaxmill Rd, Christie Downs SA 5164', lat: -35.1275883, lng: 138.4980979, contact: 'tel:+61883846894' },
  { name: 'Elizabeth House Community Garden', address: '112 Elizabeth Rd, Christie Downs SA 5164', lat: -35.1343299, lng: 138.5029324, contact: 'tel:+61883845170' },
  { name: 'Giving Garden – Aldinga Beach', address: 'Evans St, Aldinga Beach SA 5173', lat: -35.2694497, lng: 138.4604201, contact: 'https://www.facebook.com/Evansstreetgivinggarden/' },
  { name: 'Hackham Connected Community Garden', address: '88 Penneys Hill Rd, Hackham SA 5163', lat: -35.1471009, lng: 138.5349409, contact: 'https://www.facebook.com/Hackham-Connected-Community-Gardens-1016396365166278/' },
  { name: 'Big Back Yard Community Garden', address: '268 Beach Rd, Hackham West SA 5163', lat: -35.1375537, lng: 138.5122957, contact: 'https://www.cho.org.au/' },
  { name: 'Hackham West Community Centre Garden', address: 'Majorca Rd & Warsaw Cres, Hackham West SA 5163', lat: -35.1426203, lng: 138.5146561, contact: 'tel:+61883841065' },
  { name: 'Happy Patch Community Garden', address: '179 Hub Dr, Aberfoyle Park SA 5159', lat: -35.0798758, lng: 138.5912202, contact: 'tel:+61883706006' },
  { name: 'People Pantry Community Garden', address: "1798 Main S Rd, O'Halloran Hill SA 5158", lat: -35.078246, lng: 138.5478498, contact: 'tel:+61451742014' },
  { name: 'Seaford Community Centre Community Garden', address: 'Beechwood Grove, Seaford SA 5169', lat: -35.1902445, lng: 138.4762474, contact: 'mailto:info@seafordcc.com.au?subject=Seaford Community Centre Community Garden' },
  { name: 'Seaford Ecumenical Community Garden', address: 'Grand Blvd & Main St, Seaford SA 5169', lat: -35.1869709, lng: 138.4801523, contact: 'mailto:sem1@adam.com.au?subject=Seaford Ecumenical Community Garden' },
  { name: 'Seaford Scouts Community Garden', address: '7 Railway Rd, Seaford Meadows SA 5169', lat: -35.1775623, lng: 138.4946001, contact: 'mailto:seafordmeadows@sa.scouts.com.au?subject=Seaford Scouts Community Garden' },
  { name: 'Sellicks Community Garden', address: '18 Riviera Rd, Sellicks Beach SA 5174', lat: -35.3253225, lng: 138.4561639, contact: 'tel:+61883840666' },
  { name: 'Wakefield House Community Garden', address: '65 Acre Ave, Morphett Vale SA 5162', lat: -35.1107054, lng: 138.5169396, contact: 'tel:+61883846158' },
  { name: 'Wardli Youth Centre', address: '13 McKinna Rd, Christie Downs SA 5164', lat: -35.1361037, lng: 138.4884731, contact: 'tel:+61881865133' },
  { name: 'Willunga Community Garden', address: '17 St Lukes St, Willunga SA 5172', lat: -35.2732916, lng: 138.5590813, contact: 'mailto:willungacommunitygarden@gmail.com?subject=Willunga Community Garden' },
  { name: 'Woodcroft Community Centre', address: '175 Bains Rd, Morphett Vale SA 5162', lat: -35.1137737, lng: 138.5461595, contact: 'tel:+61883840070' },
  { name: 'Vine Street Centre/Neporendi', address: '7 Vine St, Old Reynella SA 5161', lat: -35.0931377, lng: 138.5400809, contact: 'tel:+61883221120' },
]

function contactLabel(href: string): string {
  if (href.startsWith('mailto:')) return 'Email'
  if (href.startsWith('tel:')) return 'Phone'
  if (href.includes('facebook.com')) return 'Facebook'
  return 'Website'
}

function directionsUrl(garden: NearbyGarden): string {
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(`${garden.name}, ${garden.address}, Australia`)}`
}

export default function Resources() {
  const { gardenName } = useAppConfig()
  const handleNav = usePageNav('resources')

  useEffect(() => {
    document.title = `Resources | ${gardenName}`
    return () => { document.title = gardenName }
  }, [gardenName])

  return (
    <div data-screen-label="SWCG · resources">
      <MSHeader active="resources" onNav={handleNav} />

      <MSSection bg="var(--paper)" py={88}>
        <MSEyebrow>Resources</MSEyebrow>
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
          Documents.
        </h1>
        <p style={{ ...bodyPara, maxWidth: 640, marginBottom: 32 }}>
          Our policies and guidelines, all in one place.
        </p>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '1fr 1fr',
            gap: 24,
            alignItems: 'stretch',
          }}
        >
          {DOCUMENTS.map((doc) => (
            <a
              key={doc.title}
              href={doc.url}
              target="_blank"
              rel="noopener noreferrer"
              style={{
                background: 'var(--ivory)',
                border: '1px solid var(--ink-200)',
                borderRadius: 'var(--r-lg)',
                padding: '26px 28px',
                textDecoration: 'none',
                display: 'block',
              }}
            >
              <h2
                style={{
                  fontFamily: 'var(--font-display)',
                  fontWeight: 700,
                  fontSize: 22,
                  textTransform: 'uppercase',
                  letterSpacing: '-0.01em',
                  color: 'var(--ink-900)',
                  margin: '0 0 10px',
                }}
              >
                {doc.title}
              </h2>
              <p style={{ fontSize: 15, lineHeight: 1.65, color: 'var(--fg-2)', margin: '0 0 14px' }}>
                {doc.description}
              </p>
              <span style={{ fontSize: 14, fontWeight: 600, color: 'var(--apple-700)' }}>
                View document &rarr;
              </span>
            </a>
          ))}
        </div>
      </MSSection>

      <MSSection bg="var(--paper-soft)" py={96}>
        <MSEyebrow>Find a garden</MSEyebrow>
        <h2
          style={{
            fontFamily: 'var(--font-display)',
            fontWeight: 700,
            fontSize: 44,
            lineHeight: 1.05,
            letterSpacing: '-0.025em',
            textTransform: 'uppercase',
            margin: '14px 0 16px',
          }}
        >
          Community gardens near you.
        </h2>
        <p style={{ ...bodyPara, maxWidth: 640, marginBottom: 32 }}>
          Not local to Seaford? There are wonderful community gardens right
          across the southern suburbs &mdash; find one near you.
        </p>
        <MapContainer
          center={[-35.17, 138.51]}
          zoom={11}
          scrollWheelZoom={false}
          style={{
            height: 460,
            borderRadius: 'var(--r-lg)',
            border: '1px solid var(--ink-200)',
            marginBottom: 32,
            zIndex: 0,
          }}
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          {NEARBY_GARDENS.map((garden) => (
            <CircleMarker
              key={garden.name}
              center={[garden.lat, garden.lng]}
              radius={9}
              pathOptions={{ color: '#A13927', fillColor: '#C84A30', fillOpacity: 0.85, weight: 2 }}
            >
              <Popup>
                <strong>{garden.name}</strong>
                <br />
                {garden.address}
                <br />
                <a href={directionsUrl(garden)} target="_blank" rel="noopener noreferrer">
                  Directions
                </a>
                {garden.contact && (
                  <>
                    {' · '}
                    <a
                      href={garden.contact}
                      {...(garden.contact.startsWith('http')
                        ? { target: '_blank', rel: 'noopener noreferrer' }
                        : {})}
                    >
                      {contactLabel(garden.contact)}
                    </a>
                  </>
                )}
              </Popup>
            </CircleMarker>
          ))}
        </MapContainer>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '1fr 1fr 1fr',
            gap: 16,
            alignItems: 'stretch',
          }}
        >
          {NEARBY_GARDENS.map((garden) => (
            <div
              key={garden.name}
              style={{
                background: 'var(--paper)',
                border: '1px solid var(--ink-100)',
                borderRadius: 'var(--r-md)',
                padding: '18px 20px',
                display: 'flex',
                flexDirection: 'column',
                gap: 6,
              }}
            >
              <div
                style={{
                  fontFamily: 'var(--font-display)',
                  fontWeight: 700,
                  fontSize: 16,
                  color: 'var(--ink-900)',
                  lineHeight: 1.2,
                }}
              >
                {garden.name}
              </div>
              <div style={{ fontSize: 13, color: 'var(--fg-3)', lineHeight: 1.4 }}>
                {garden.address}
              </div>
              <div style={{ display: 'flex', gap: 14, marginTop: 4, fontSize: 13, fontWeight: 600 }}>
                <a href={directionsUrl(garden)} target="_blank" rel="noopener noreferrer">
                  Directions
                </a>
                {garden.contact && (
                  <a
                    href={garden.contact}
                    {...(garden.contact.startsWith('http')
                      ? { target: '_blank', rel: 'noopener noreferrer' }
                      : {})}
                  >
                    {contactLabel(garden.contact)}
                  </a>
                )}
              </div>
            </div>
          ))}
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
