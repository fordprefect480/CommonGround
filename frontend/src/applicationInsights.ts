import { ApplicationInsights } from '@microsoft/applicationinsights-web'
import { ReactPlugin } from '@microsoft/applicationinsights-react-js'

export const reactPlugin = new ReactPlugin()

let appInsights: ApplicationInsights | null = null

export function initializeApplicationInsights(connectionString: string | null | undefined): ApplicationInsights | null {
  if (appInsights) return appInsights
  if (!connectionString) return null

  appInsights = new ApplicationInsights({
    config: {
      connectionString,
      enableAutoRouteTracking: true,
      enableCorsCorrelation: true,
      enableRequestHeaderTracking: true,
      enableResponseHeaderTracking: true,
      autoTrackPageVisitTime: true,
      extensions: [reactPlugin],
    },
  })

  appInsights.loadAppInsights()
  appInsights.trackPageView()
  return appInsights
}

export function getApplicationInsights(): ApplicationInsights | null {
  return appInsights
}
