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
  address: string | null
  secondaryMembers: string[]
  joinedAt: string
  membershipPaidThroughUtc: string | null
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

export async function requestPasswordReset(email: string): Promise<void> {
  let res: Response
  try {
    res = await fetch('/api/auth/forgotPassword', {
      method: 'POST',
      headers: json,
      body: JSON.stringify({ email }),
    })
  } catch {
    throw new Error('Could not reach the server. Please try again.')
  }
  // The endpoint always returns 200 for valid input so it can't be used to
  // probe which emails have accounts; any non-OK status is a real error.
  if (res.ok) return
  const text = await res.text().catch(() => '')
  throw new Error(parseProblemDetails(text) ?? `Request failed (HTTP ${res.status})`)
}

export async function resetPassword(email: string, resetCode: string, newPassword: string): Promise<void> {
  let res: Response
  try {
    res = await fetch('/api/auth/resetPassword', {
      method: 'POST',
      headers: json,
      body: JSON.stringify({ email, resetCode, newPassword }),
    })
  } catch {
    throw new Error('Could not reach the server. Please try again.')
  }
  if (res.ok) return
  const text = await res.text().catch(() => '')
  throw new Error(
    parseProblemDetails(text) ??
      'This reset link is invalid or has expired. Please request a new one.',
  )
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

export interface MemberStats {
  paidMembers: number
  notYetPaidMembers: number
  newMembersLast30Days: number
}

export async function fetchMemberStats(): Promise<MemberStats> {
  const res = await fetch('/api/admin/members/stats', { credentials: 'include' })
  if (!res.ok) throw new Error(`Failed to load member stats (${res.status})`)
  return res.json()
}

// The paid-through date a membership payment made now would reach (the next
// 1 July renewal boundary, in the garden's local time). A member whose
// membershipPaidThroughUtc is null or earlier than this hasn't paid for the
// upcoming year.
export async function fetchMembershipRenewalTarget(): Promise<string> {
  const res = await fetch('/api/admin/members/membership-renewal', { credentials: 'include' })
  if (!res.ok) throw new Error(`Failed to load renewal date (${res.status})`)
  const body = (await res.json()) as { renewalTargetUtc: string }
  return body.renewalTargetUtc
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

export interface PaymentRecord {
  id: number
  kind: 'Membership' | 'LeasedBed'
  amountCents: number
  currency: string
  status: string
  method: 'Stripe' | 'Manual'
  paidAtUtc: string | null
  periodStartUtc: string | null
  periodEndUtc: string | null
  bedLabel: string | null
  createdAtUtc: string
}

export async function recordMembershipPayment(id: string, amountCents: number): Promise<Member> {
  const res = await fetch(`/api/admin/members/${encodeURIComponent(id)}/record-membership-payment`, {
    method: 'POST',
    credentials: 'include',
    headers: json,
    body: JSON.stringify({ amountCents }),
  })
  if (!res.ok) throw new Error(`Failed to record payment (${res.status})`)
  return res.json()
}

export async function fetchMemberPayments(id: string): Promise<PaymentRecord[]> {
  const res = await fetch(`/api/admin/members/${encodeURIComponent(id)}/payments`, { credentials: 'include' })
  if (!res.ok) throw new Error(`Failed to load payments (${res.status})`)
  return res.json()
}

export async function fetchMyPayments(): Promise<PaymentRecord[]> {
  const res = await fetch('/api/account/me/payments', { credentials: 'include' })
  if (!res.ok) throw new Error(`Failed to load payments (${res.status})`)
  return res.json()
}

export interface UpdateMemberInput {
  firstName: string | null
  lastName: string | null
  phoneNumber: string | null
  address: string | null
  secondaryMembers: string[]
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
