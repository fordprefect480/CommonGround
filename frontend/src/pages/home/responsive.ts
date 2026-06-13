import { useEffect, useState } from 'react'

/** Phones (portrait). */
export const BP_MOBILE = '(max-width: 640px)'
/** Small tablets / large phones in landscape. */
export const BP_TABLET = '(max-width: 960px)'
/** Width below which the header switches to the hamburger drawer. */
export const BP_HEADER = '(max-width: 860px)'

/**
 * Subscribes to a CSS media query and returns whether it currently matches.
 * Initializes synchronously from `matchMedia` so there is no first-render flash.
 */
export function useMediaQuery(query: string): boolean {
  const [matches, setMatches] = useState(
    () => typeof window !== 'undefined' && window.matchMedia(query).matches,
  )

  useEffect(() => {
    const mq = window.matchMedia(query)
    const handler = (e: MediaQueryListEvent) => setMatches(e.matches)
    setMatches(mq.matches)
    mq.addEventListener('change', handler)
    return () => mq.removeEventListener('change', handler)
  }, [query])

  return matches
}
