export interface ContactMessage {
  name: string
  email: string
  subject: string
  message: string
  captchaToken?: string
}

export async function sendContactMessage(payload: ContactMessage): Promise<void> {
  const res = await fetch('/api/contact', {
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
