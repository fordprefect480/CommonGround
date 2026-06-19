import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { useAppConfig } from './AppConfigContext'

/**
 * Manages the document title and per-page metadata (description, Open Graph,
 * Twitter cards, canonical URL and optional JSON-LD structured data) for SEO
 * and answer-engine optimisation.
 *
 * Tags are updated in place against the defaults declared in `index.html`, so a
 * page renders exactly one of each - no duplicates, no stale values when the
 * SPA navigates between routes.
 */
interface SeoProps {
  /** Page-specific title; the garden name is appended automatically. Omit on the home page. */
  title?: string
  description?: string
  /** Social-share image (absolute or root-relative URL). Falls back to the site default. */
  image?: string | null
  type?: 'website' | 'article'
  /** Keep auth/admin screens out of search indexes. */
  noindex?: boolean
  /** JSON-LD structured data for rich results and answer engines. */
  jsonLd?: Record<string, unknown> | Record<string, unknown>[]
}

const DEFAULT_IMAGE = '/swcg/hero-image.png'
const JSON_LD_ID = 'seo-jsonld'

function upsertMeta(attr: 'name' | 'property', key: string, content: string | null) {
  let el = document.head.querySelector<HTMLMetaElement>(`meta[${attr}="${key}"]`)
  if (content == null) {
    el?.remove()
    return
  }
  if (!el) {
    el = document.createElement('meta')
    el.setAttribute(attr, key)
    document.head.appendChild(el)
  }
  el.setAttribute('content', content)
}

function upsertLink(rel: string, href: string) {
  let el = document.head.querySelector<HTMLLinkElement>(`link[rel="${rel}"]`)
  if (!el) {
    el = document.createElement('link')
    el.setAttribute('rel', rel)
    document.head.appendChild(el)
  }
  el.setAttribute('href', href)
}

export default function Seo({ title, description, image, type = 'website', noindex = false, jsonLd }: SeoProps) {
  const { gardenName } = useAppConfig()
  const { pathname } = useLocation()
  const desc = description ?? null
  const jsonLdString = jsonLd ? JSON.stringify(jsonLd) : null

  useEffect(() => {
    const fullTitle = title ? `${title} | ${gardenName}` : gardenName
    const url = window.location.origin + pathname
    const imageUrl = new URL(image ?? DEFAULT_IMAGE, window.location.origin).href

    document.title = fullTitle
    upsertMeta('name', 'description', desc)
    upsertMeta('name', 'robots', noindex ? 'noindex, nofollow' : 'index, follow')

    // Open Graph (Facebook, LinkedIn, Slack, ...)
    upsertMeta('property', 'og:title', fullTitle)
    upsertMeta('property', 'og:description', desc)
    upsertMeta('property', 'og:type', type)
    upsertMeta('property', 'og:url', url)
    upsertMeta('property', 'og:image', imageUrl)
    upsertMeta('property', 'og:site_name', gardenName)

    // Twitter / X cards
    upsertMeta('name', 'twitter:card', 'summary_large_image')
    upsertMeta('name', 'twitter:title', fullTitle)
    upsertMeta('name', 'twitter:description', desc)
    upsertMeta('name', 'twitter:image', imageUrl)

    upsertLink('canonical', url)

    document.getElementById(JSON_LD_ID)?.remove()
    if (jsonLdString) {
      const script = document.createElement('script')
      script.type = 'application/ld+json'
      script.id = JSON_LD_ID
      script.textContent = jsonLdString
      document.head.appendChild(script)
    }
  }, [title, desc, image, type, noindex, jsonLdString, gardenName, pathname])

  return null
}
