export interface Me {
  email: string
  firstName: string | null
  lastName: string | null
  displayName: string | null
  phoneNumber: string | null
  address: string | null
  secondaryMembers: string[]
  membershipPaidThroughUtc: string | null
  isAdmin: boolean
  isSubscribedToMailingList: boolean
}

export interface Member {
  id: string
  email: string | null
  userName: string | null
  firstName: string | null
  lastName: string | null
  displayName: string | null
  phoneNumber: string | null
  joinedAt: string
  emailConfirmed: boolean
  isSubscribedToMailingList: boolean
  roles: string[]
}

const json = { 'Content-Type': 'application/json' }

export async function login(email: string, password: string): Promise<void> {
  let res: Response
  try {
    res = await fetch('/api/auth/login?useCookies=true', {
      method: 'POST',
      headers: json,
      credentials: 'include',
      body: JSON.stringify({ email, password }),
    })
  } catch {
    throw new Error('Could not reach the server. Please try again.')
  }
  if (res.ok) return

  const text = await res.text().catch(() => '')
  const detail = parseProblemDetails(text)
  if (res.status === 401) {
    throw new Error(detail ?? 'Invalid email or password')
  }
  throw new Error(detail ?? `Sign-in failed (HTTP ${res.status})`)
}

export async function logout(): Promise<void> {
  await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
}

export async function fetchMe(): Promise<Me | null> {
  const res = await fetch('/api/account/me', { credentials: 'include' })
  if (res.status === 401 || res.status === 403) return null
  if (!res.ok) throw new Error(`Unexpected status ${res.status}`)
  return res.json()
}

export interface UpdateMeInput {
  firstName: string | null
  lastName: string | null
  phoneNumber: string | null
  address: string | null
  secondaryMembers: string[]
  isSubscribedToMailingList: boolean
}

export async function updateMe(input: UpdateMeInput): Promise<Me> {
  const res = await fetch('/api/account/me', {
    method: 'PUT',
    headers: json,
    credentials: 'include',
    body: JSON.stringify(input),
  })
  if (!res.ok) throw new Error(await res.text())
  return res.json()
}

export async function changePassword(oldPassword: string, newPassword: string): Promise<void> {
  const res = await fetch('/api/auth/manage/info', {
    method: 'POST',
    headers: json,
    credentials: 'include',
    body: JSON.stringify({ oldPassword, newPassword }),
  })
  if (res.ok) return
  const text = await res.text()
  throw new Error(parseProblemDetails(text) ?? `Password change failed (${res.status})`)
}

function parseProblemDetails(text: string): string | null {
  if (!text) return null
  try {
    const parsed = JSON.parse(text) as {
      detail?: string
      title?: string
      error?: string
      errors?: Record<string, string[]>
    }
    if (parsed.errors) {
      const messages = Object.values(parsed.errors).flat().filter(Boolean)
      if (messages.length > 0) return messages.join(' ')
    }
    return parsed.detail ?? parsed.error ?? parsed.title ?? null
  } catch {
    return text
  }
}

export async function fetchMembers(): Promise<Member[]> {
  const res = await fetch('/api/admin/members', { credentials: 'include' })
  if (!res.ok) throw new Error(`Failed to load members (${res.status})`)
  return res.json()
}

export interface CreateMemberInput {
  email: string
  firstName: string | null
  lastName: string | null
  phoneNumber: string | null
  password: string
  isAdmin: boolean
  isSubscribedToMailingList: boolean
}

export async function createMember(input: CreateMemberInput): Promise<Member> {
  const res = await fetch('/api/admin/members', {
    method: 'POST',
    headers: json,
    credentials: 'include',
    body: JSON.stringify(input),
  })
  if (res.ok) return res.json()
  throw new Error(await readErrorMessage(res, 'Create failed'))
}

export async function fetchMember(id: string): Promise<Member> {
  const res = await fetch(`/api/admin/members/${encodeURIComponent(id)}`, { credentials: 'include' })
  if (!res.ok) throw new Error(`Failed to load member (${res.status})`)
  return res.json()
}

export interface UpdateMemberInput {
  firstName: string | null
  lastName: string | null
  phoneNumber: string | null
  isAdmin: boolean
  isSubscribedToMailingList: boolean
}

export async function updateMember(id: string, input: UpdateMemberInput): Promise<Member> {
  const res = await fetch(`/api/admin/members/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: json,
    credentials: 'include',
    body: JSON.stringify(input),
  })
  if (res.ok) return res.json()
  throw new Error(await readErrorMessage(res, 'Update failed'))
}

async function readErrorMessage(res: Response, fallback: string): Promise<string> {
  const text = await res.text()
  if (!text) return `${fallback} (${res.status})`
  try {
    const parsed = JSON.parse(text) as { error?: string }
    if (parsed?.error) return parsed.error
  } catch {
    // body was not JSON; fall through
  }
  return text
}
