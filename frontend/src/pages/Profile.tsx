import { useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import AdminProfile from './admin/AdminProfile'
import { MSFooter, MSHeader, type NavId } from './home/Chrome'

export default function Profile() {
  const navigate = useNavigate()

  const handleNav = useCallback((id: NavId) => {
    if (id === 'home') {
      navigate('/')
      return
    }
    if (id === 'blog') {
      navigate('/blog')
      return
    }
    navigate(`/#section-${id}`)
  }, [navigate])

  return (
    <div data-screen-label="SWCG · profile">
      <MSHeader active="home" onNav={handleNav} />
      <main className="admin-main profile-main">
        <AdminProfile />
      </main>
      <MSFooter onNav={handleNav} />
    </div>
  )
}
