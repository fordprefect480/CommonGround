import { useCallback, useState } from 'react'
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

export default function Home() {
  const [active, setActive] = useState<NavId>('home')

  const handleNav = useCallback((id: NavId) => {
    setActive(id)
    if (id === 'home') {
      window.scrollTo({ top: 0, behavior: 'smooth' })
      return
    }
    const target = document.getElementById(`section-${id}`)
    target?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [])

  return (
    <div data-screen-label="SWCG · home">
      <MSHeader active={active} onNav={handleNav} />
      <HomeHero onNav={handleNav} />
      <HomeAbout onNav={handleNav} />
      <FeatureGrid />
      <HomeEvents onNav={handleNav} />
      <MembershipBanner onNav={handleNav} />
      <PartnersStrip onNav={handleNav} />
      <InstagramStrip />
      <ContactPage />
      <MSFooter onNav={handleNav} />
    </div>
  )
}
