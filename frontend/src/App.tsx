import type { ReactElement } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { AppInsightsContext, AppInsightsErrorBoundary } from '@microsoft/applicationinsights-react-js'
import './App.css'
import { AppConfigProvider } from './AppConfigContext'
import { reactPlugin } from './applicationInsights'
import { AuthProvider, useAuth } from './AuthContext'
import Home from './pages/Home'
import Login from './pages/Login'
import Profile from './pages/Profile'
import AdminLayout from './pages/admin/AdminLayout'
import AdminProfile from './pages/admin/AdminProfile'
import AdminTools from './pages/admin/AdminTools'
import BlogPostEditor from './pages/admin/BlogPostEditor'
import BlogPostList from './pages/admin/BlogPostList'
import MemberDetail from './pages/admin/MemberDetail'
import Members from './pages/admin/Members'
import BlogIndex from './pages/blog/BlogIndex'
import BlogPost from './pages/blog/BlogPost'

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
            <Routes>
              <Route path="/" element={<Home />} />
              <Route path="/blog" element={<BlogIndex />} />
              <Route path="/blog/:slug" element={<BlogPost />} />
              <Route path="/login" element={<Login />} />
              <Route path="/profile" element={<RequireAuth><Profile /></RequireAuth>} />
              <Route
                path="/admin"
                element={<RequireAuth requireAdmin><AdminLayout /></RequireAuth>}
              >
                <Route index element={<Navigate to="members" replace />} />
                <Route path="members" element={<Members />} />
                <Route path="members/:id" element={<MemberDetail />} />
                <Route path="blog" element={<BlogPostList />} />
                <Route path="blog/new" element={<BlogPostEditor />} />
                <Route path="blog/:id/edit" element={<BlogPostEditor />} />
                <Route path="tools" element={<AdminTools />} />
                <Route path="profile" element={<AdminProfile />} />
              </Route>
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </AuthProvider>
        </AppConfigProvider>
      </AppInsightsErrorBoundary>
    </AppInsightsContext.Provider>
  )
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
