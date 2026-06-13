import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
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
  const navigate = useNavigate()

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
