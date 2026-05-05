import { Navigate, Route, Routes } from 'react-router-dom'
import './App.css'
import { AppConfigProvider } from './AppConfigContext'
import Home from './pages/Home'
import AdminLayout from './pages/admin/AdminLayout'
import Members from './pages/admin/Members'

export default function App() {
  return (
    <AppConfigProvider>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/admin" element={<AdminLayout />}>
          <Route index element={<Navigate to="members" replace />} />
          <Route path="members" element={<Members />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppConfigProvider>
  )
}
