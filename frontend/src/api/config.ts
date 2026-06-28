export interface AppConfig {
  gardenName: string
  applicationInsightsConnectionString?: string | null
  turnstileSiteKey?: string | null
  version?: string | null
  commitSha?: string | null
  paymentsEnabled: boolean
  membershipPriceCents: number
  leasedBedPriceCents: number
  /** When true, the public site is gated behind an under-construction page; only admins see the SPA. */
  comingSoon: boolean
}

export async function fetchAppConfig(): Promise<AppConfig> {
  const res = await fetch('/api/config')
  if (!res.ok) throw new Error(`Failed to load config (${res.status})`)
  return res.json()
}
