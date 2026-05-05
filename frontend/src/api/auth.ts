export interface AdminMe {
  email: string
  isAdmin: boolean
}

export interface Member {
  id: string
  email: string | null
  userName: string | null
  emailConfirmed: boolean
  roles: string[]
}

const json = { 'Content-Type': 'application/json' }

export async function login(email: string, password: string): Promise<void> {
  const res = await fetch('/api/auth/login?useCookies=true', {
    method: 'POST',
    headers: json,
    credentials: 'include',
    body: JSON.stringify({ email, password }),
  })
  if (!res.ok) throw new Error('Invalid email or password')
}

export async function logout(): Promise<void> {
  await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
}

export async function fetchAdminMe(): Promise<AdminMe | null> {
  const res = await fetch('/api/admin/me', { credentials: 'include' })
  if (res.status === 401 || res.status === 403) return null
  if (!res.ok) throw new Error(`Unexpected status ${res.status}`)
  return res.json()
}

export async function fetchMembers(): Promise<Member[]> {
  const res = await fetch('/api/admin/members', { credentials: 'include' })
  if (!res.ok) throw new Error(`Failed to load members (${res.status})`)
  return res.json()
}
