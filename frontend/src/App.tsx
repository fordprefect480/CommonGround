import { Navigate, Route, Routes } from 'react-router-dom'
import './App.css'
import { AppConfigProvider } from './AppConfigContext'
import Home from './pages/Home'
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
    <AppConfigProvider>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/blog" element={<BlogIndex />} />
        <Route path="/blog/:slug" element={<BlogPost />} />
        <Route path="/admin" element={<AdminLayout />}>
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
    </AppConfigProvider>
  )
}
