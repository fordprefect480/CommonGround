import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { fetchAppConfig, type AppConfig } from './api/config'
import { initializeApplicationInsights } from './applicationInsights'

const AppConfigContext = createContext<AppConfig | null>(null)

export function AppConfigProvider({ children }: { children: ReactNode }) {
  const [config, setConfig] = useState<AppConfig | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchAppConfig()
      .then((c) => {
        // Fire-and-forget: analytics loads its own chunk after paint and must
        // never block rendering or surface an error if the SDK fails to load.
        void initializeApplicationInsights(c.applicationInsightsConnectionString).catch(() => {})
        setConfig(c)
        document.title = c.gardenName
      })
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : 'Failed to load configuration')
      })
  }, [])

  if (error) {
    return (
      <main className="app-bootstrap-error" role="alert">
        <p>{error}</p>
      </main>
    )
  }

  if (!config) return null

  return <AppConfigContext.Provider value={config}>{children}</AppConfigContext.Provider>
}

export function useAppConfig(): AppConfig {
  const config = useContext(AppConfigContext)
  if (!config) {
    throw new Error('useAppConfig must be used inside <AppConfigProvider>')
  }
  return config
}
