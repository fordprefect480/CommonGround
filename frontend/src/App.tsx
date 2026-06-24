import { lazy, Suspense, useEffect, type ReactElement } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { AppInsightsContext, AppInsightsErrorBoundary } from '@microsoft/applicationinsights-react-js'
import './App.css'
import { AppConfigProvider } from './AppConfigContext'
import { reactPlugin } from './applicationInsights'
import { AuthProvider, useAuth } from './AuthContext'
import Home from './pages/Home'

// Route components are lazy-loaded so the initial bundle stays small. The
// public landing page (Home) ships eagerly for fast first paint; every other
// route - including the heavy TipTap editor and the Leaflet map - loads on
// demand when the user navigates to it.
const LeaseAPlot = lazy(() => import('./pages/LeaseAPlot'))
const Login = lazy(() => import('./pages/Login'))
const ForgotPassword = lazy(() => import('./pages/ForgotPassword'))
const ResetPassword = lazy(() => import('./pages/ResetPassword'))
const Membership = lazy(() => import('./pages/Membership'))
const MembershipWelcome = lazy(() => import('./pages/MembershipWelcome'))
const Profile = lazy(() => import('./pages/Profile'))
const Resources = lazy(() => import('./pages/Resources'))
const Donate = lazy(() => import('./pages/Donate'))
const Events = lazy(() => import('./pages/Events'))
const BlogIndex = lazy(() => import('./pages/blog/BlogIndex'))
const BlogPost = lazy(() => import('./pages/blog/BlogPost'))
const Activity = lazy(() => import('./pages/admin/Activity'))
const AdminLayout = lazy(() => import('./pages/admin/AdminLayout'))
const AdminProfile = lazy(() => import('./pages/admin/AdminProfile'))
const AdminTools = lazy(() => import('./pages/admin/AdminTools'))
const BlogPostEditor = lazy(() => import('./pages/admin/BlogPostEditor'))
const BlogPostList = lazy(() => import('./pages/admin/BlogPostList'))
const CommunityEventEditor = lazy(() => import('./pages/admin/CommunityEventEditor'))
const CommunityEventList = lazy(() => import('./pages/admin/CommunityEventList'))
const Dashboard = lazy(() => import('./pages/admin/Dashboard'))
const EmailCompose = lazy(() => import('./pages/admin/EmailCompose'))
const EmailDetail = lazy(() => import('./pages/admin/EmailDetail'))
const EmailList = lazy(() => import('./pages/admin/EmailList'))
const EmailTestTool = lazy(() => import('./pages/admin/EmailTestTool'))
const InstagramTileEditor = lazy(() => import('./pages/admin/InstagramTileEditor'))
const InstagramTileList = lazy(() => import('./pages/admin/InstagramTileList'))
const LeasedBeds = lazy(() => import('./pages/admin/LeasedBeds'))
const MemberDetail = lazy(() => import('./pages/admin/MemberDetail'))
const Members = lazy(() => import('./pages/admin/Members'))

export default function App() {
  return (
    <AppInsightsContext.Provider value={reactPlugin}>
      <AppInsightsErrorBoundary
        appInsights={reactPlugin}
        onError={() => (
          <main className="app-bootstrap-error" role="alert">
            <p>Something went wrong. Please refresh the page.</p>
          </main>
        )}
      >
        <AppConfigProvider>
          <AuthProvider>
            <Suspense fallback={<div style={{ minHeight: '60vh' }} aria-busy="true" />}>
            <ScrollToTop />
            <Routes>
              <Route path="/" element={<Home />} />
              <Route path="/membership" element={<Membership />} />
              <Route path="/membership/welcome" element={<MembershipWelcome />} />
              <Route path="/lease-a-plot" element={<LeaseAPlot />} />
              <Route path="/donate" element={<Donate />} />
              <Route path="/events" element={<Events />} />
              <Route path="/resources" element={<Resources />} />
              <Route path="/blog" element={<BlogIndex />} />
              <Route path="/blog/:slug" element={<BlogPost />} />
              <Route path="/login" element={<Login />} />
              <Route path="/forgot-password" element={<ForgotPassword />} />
              <Route path="/reset-password" element={<ResetPassword />} />
              <Route path="/profile" element={<RequireAuth><Profile /></RequireAuth>} />
              <Route
                path="/admin"
                element={<RequireAuth requireAdmin><AdminLayout /></RequireAuth>}
              >
                <Route index element={<Dashboard />} />
                <Route path="members" element={<Members />} />
                <Route path="members/:id" element={<MemberDetail />} />
                <Route path="blog" element={<BlogPostList />} />
                <Route path="blog/new" element={<BlogPostEditor />} />
                <Route path="blog/:id/edit" element={<BlogPostEditor />} />
                <Route path="instagram" element={<InstagramTileList />} />
                <Route path="instagram/new" element={<InstagramTileEditor />} />
                <Route path="instagram/:id/edit" element={<InstagramTileEditor />} />
                <Route path="events" element={<CommunityEventList />} />
                <Route path="events/new" element={<CommunityEventEditor />} />
                <Route path="events/:id/edit" element={<CommunityEventEditor />} />
                <Route path="leased-beds" element={<LeasedBeds />} />
                <Route path="email" element={<EmailList />} />
                <Route path="email/new" element={<EmailCompose />} />
                <Route path="email/:id" element={<EmailDetail />} />
                <Route path="tools" element={<AdminTools />} />
                <Route path="tools/email-test" element={<EmailTestTool />} />
                <Route path="activity" element={<Activity />} />
                <Route path="profile" element={<AdminProfile />} />
              </Route>
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
            </Suspense>
          </AuthProvider>
        </AppConfigProvider>
      </AppInsightsErrorBoundary>
    </AppInsightsContext.Provider>
  )
}

// React Router doesn't reset scroll on navigation, so a route change kept the
// previous page's scroll offset (e.g. clicking "Lease a plot" halfway down Home
// landed halfway down the lease page). Reset to the top on every path change,
// except when there's a hash - those target an on-page anchor (e.g. Home's
// /#section-events), which the destination page scrolls to itself.
function ScrollToTop() {
  const { pathname, hash } = useLocation()

  useEffect(() => {
    if (hash) return
    window.scrollTo(0, 0)
  }, [pathname, hash])

  return null
}

interface RequireAuthProps {
  children: ReactElement
  requireAdmin?: boolean
}

function RequireAuth({ children, requireAdmin = false }: RequireAuthProps) {
  const { state } = useAuth()
  const location = useLocation()

  if (state.status === 'loading') {
    return <p className="admin-loading">Checking your session&hellip;</p>
  }

  if (state.status === 'anonymous') {
    const next = encodeURIComponent(location.pathname + location.search)
    return <Navigate to={`/login?next=${next}`} replace />
  }

  if (requireAdmin && !state.me.isAdmin) {
    return <Navigate to="/profile" replace />
  }

  return children
}
