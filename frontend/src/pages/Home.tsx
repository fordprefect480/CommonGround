import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Seo from '../Seo'
import { useAppConfig } from '../AppConfigContext'
import { MSFooter, MSHeader, type NavId } from './home/Chrome'
import {
  ContactPage,
  FeatureGrid,
  HomeAbout,
  HomeEvents,
  HomeHero,
  InstagramStrip,
  MembershipBanner,
  PartnersStrip,
} from './home/Sections'

const HOME_DESCRIPTION =
  "Seaford Wetlands Community Garden is South Australia's largest community garden - join as a member, lease a raised garden bed, and take part in workshops, working bees and events in Seaford, SA."

export default function Home() {
  const [active, setActive] = useState<NavId>('home')
  const navigate = useNavigate()
  const { gardenName } = useAppConfig()

  useEffect(() => {
    const hash = window.location.hash
    if (!hash) return
    const target = document.getElementById(hash.slice(1))
    if (target) {
      target.scrollIntoView({ behavior: 'instant', block: 'start' })
      const sectionId = hash.slice(1).replace(/^section-/, '') as NavId
      setActive(sectionId)
    }
  }, [])

  const handleNav = useCallback((id: NavId) => {
    if (id === 'blog') {
      navigate('/blog')
      return
    }
    if (id === 'membership') {
      navigate('/membership')
      return
    }
    if (id === 'lease') {
      navigate('/lease-a-plot')
      return
    }
    if (id === 'events') {
      navigate('/events')
      return
    }
    if (id === 'donate') {
      navigate('/donate')
      return
    }
    if (id === 'resources') {
      navigate('/resources')
      return
    }
    setActive(id)
    if (id === 'home') {
      window.scrollTo({ top: 0, behavior: 'smooth' })
      return
    }
    const target = document.getElementById(`section-${id}`)
    target?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [navigate])

  return (
    <div data-screen-label="SWCG · home">
      <Seo
        description={HOME_DESCRIPTION}
        jsonLd={{
          '@context': 'https://schema.org',
          '@type': 'Organization',
          name: gardenName,
          alternateName: 'SWCG',
          description: HOME_DESCRIPTION,
          url: window.location.origin,
          logo: `${window.location.origin}/swcg/logo-mark.svg`,
          image: `${window.location.origin}/swcg/hero-image.png`,
          email: 'seafordcg@gmail.com',
          address: {
            '@type': 'PostalAddress',
            streetAddress: '100 Seaford Rd',
            addressLocality: 'Seaford',
            addressRegion: 'SA',
            postalCode: '5169',
            addressCountry: 'AU',
          },
          sameAs: ['https://www.facebook.com/SeafordCG'],
        }}
      />
      <MSHeader active={active} onNav={handleNav} />
      <HomeHero onNav={handleNav} />
      <HomeAbout onNav={handleNav} />
      <FeatureGrid />
      <HomeEvents onNav={handleNav} />
      <MembershipBanner onNav={handleNav} />
      <PartnersStrip />
      <InstagramStrip />
      <ContactPage />
      <MSFooter onNav={handleNav} />
    </div>
  )
}
