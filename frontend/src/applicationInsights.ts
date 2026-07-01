import type { ApplicationInsights } from '@microsoft/applicationinsights-web'

// The entire Application Insights SDK is loaded lazily inside
// initializeApplicationInsights() via a dynamic import, so none of it lands in
// the initial bundle - it downloads in its own chunk after the config fetch
// resolves, keeping analytics off the first-paint critical path. Route tracking
// is handled by enableAutoRouteTracking, so the React plugin is not needed.
let appInsights: ApplicationInsights | null = null

export async function initializeApplicationInsights(connectionString: string | null | undefined): Promise<ApplicationInsights | null> {
  if (appInsights) return appInsights
  if (!connectionString) return null

  const { ApplicationInsights } = await import('@microsoft/applicationinsights-web')

  appInsights = new ApplicationInsights({
    config: {
      connectionString,
      enableAutoRouteTracking: true,
      enableCorsCorrelation: true,
      enableRequestHeaderTracking: true,
      enableResponseHeaderTracking: true,
      autoTrackPageVisitTime: true,
    },
  })

  appInsights.loadAppInsights()
  appInsights.trackPageView()
  return appInsights
}

export function getApplicationInsights(): ApplicationInsights | null {
  return appInsights
}
