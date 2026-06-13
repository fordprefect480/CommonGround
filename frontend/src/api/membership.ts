export interface SignupInput {
  firstName: string
  lastName: string
  email: string
  phoneNumber: string | null
  address: string | null
  password: string
  secondaryMembers: string[]
  subscribeNewsletter: boolean
  captchaToken?: string
}

export async function signup(input: SignupInput): Promise<{ checkoutUrl: string }> {
  const res = await fetch('/api/membership/signup', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (res.ok) return res.json()
  throw await readError(res, 'Signup failed')
}

export async function completeCheckout(sessionId: string): Promise<void> {
  const res = await fetch('/api/membership/complete', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ sessionId }),
  })
  if (res.ok) return
  throw await readError(res, 'Could not complete signup')
}

async function readError(res: Response, fallback: string): Promise<Error> {
  const text = await res.text()
  try {
    const body = JSON.parse(text) as { error?: string; detail?: string }
    return new Error(body.error || body.detail || `${fallback} (${res.status})`)
  } catch {
    return new Error(text || `${fallback} (${res.status})`)
  }
}
