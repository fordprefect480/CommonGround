export interface AppConfig {
  gardenName: string
}

export async function fetchAppConfig(): Promise<AppConfig> {
  const res = await fetch('/api/config')
  if (!res.ok) throw new Error(`Failed to load config (${res.status})`)
  return res.json()
}
