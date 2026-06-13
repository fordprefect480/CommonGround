export interface SubscribeRequest {
  email: string
  captchaToken?: string
}

export async function subscribeToMailingList(payload: SubscribeRequest): Promise<void> {
  const res = await fetch('/api/subscribe', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  if (!res.ok) {
    const text = await res.text()
    try {
      const body = JSON.parse(text) as { error?: string; detail?: string }
      throw new Error(body.error || body.detail || `Request failed (${res.status})`)
    } catch {
      throw new Error(text || `Request failed (${res.status})`)
    }
  }
}
