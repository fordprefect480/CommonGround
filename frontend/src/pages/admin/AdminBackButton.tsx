import { Link } from 'react-router-dom'

interface AdminBackButtonProps {
  to: string
  label: string
}

export default function AdminBackButton({ to, label }: AdminBackButtonProps) {
  return (
    <Link to={to} className="admin-back-button" aria-label={label} title={label}>
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <line x1="19" y1="12" x2="5" y2="12" />
        <polyline points="12 19 5 12 12 5" />
      </svg>
    </Link>
  )
}
