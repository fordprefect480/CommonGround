export type EventTone = 'leaf' | 'apple' | 'flesh'

export type RepeatFrequency = 'none' | 'weekly' | 'fortnightly' | 'monthly'

export interface UpcomingEvent {
  id: string
  source: 'manual' | 'eventbrite'
  title: string
  body: string
  startUtc: string
  endUtc: string | null
  url: string | null
  tone: EventTone
  imageUrl: string | null
}

export interface CommunityEventAdmin {
  id: number
  title: string
  startUtc: string
  endUtc: string | null
  body: string
  url: string | null
  tone: EventTone
  displayOrder: number
  featuredImageId: number | null
  imageUrl: string | null
  createdAt: string
  updatedAt: string
}

export interface CommunityEventCreated {
  event: CommunityEventAdmin
  createdCount: number
}

export interface CommunityEventWrite {
  title: string
  startUtc: string
  endUtc: string | null
  body: string
  url: string | null
  featuredImageId: number | null
  tone?: EventTone
  displayOrder?: number | null
  repeatFrequency?: RepeatFrequency
  repeatUntil?: string | null
}

export interface UploadedImage {
  id: number
  url: string
}

const JSON_HEADERS = { 'Content-Type': 'application/json' }

async function ensureOk(response: Response): Promise<Response> {
  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`
    try {
      const body = (await response.clone().json()) as { error?: string }
      if (body?.error) message = body.error
    } catch {
      const text = await response.text()
      if (text) message = text
    }
    throw new Error(message)
  }
  return response
}

async function getJson<T>(url: string): Promise<T> {
  const res = await ensureOk(await fetch(url, { credentials: 'include' }))
  return res.json() as Promise<T>
}

async function sendJson<T>(url: string, method: 'POST' | 'PUT', body: unknown): Promise<T> {
  const res = await ensureOk(
    await fetch(url, {
      method,
      headers: JSON_HEADERS,
      credentials: 'include',
      body: JSON.stringify(body),
    }),
  )
  return res.json() as Promise<T>
}

export async function fetchUpcomingEvents(take = 3): Promise<UpcomingEvent[]> {
  const params = new URLSearchParams({ take: String(take) })
  return getJson(`/api/events/upcoming?${params}`)
}

export async function fetchAdminCommunityEvents(): Promise<CommunityEventAdmin[]> {
  return getJson('/api/admin/events')
}

export async function fetchAdminCommunityEvent(id: number): Promise<CommunityEventAdmin> {
  return getJson(`/api/admin/events/${id}`)
}

export async function createCommunityEvent(input: CommunityEventWrite): Promise<CommunityEventCreated> {
  return sendJson('/api/admin/events', 'POST', input)
}

export async function updateCommunityEvent(
  id: number,
  input: CommunityEventWrite,
): Promise<CommunityEventAdmin> {
  return sendJson(`/api/admin/events/${id}`, 'PUT', input)
}

export async function deleteCommunityEvent(id: number): Promise<void> {
  await ensureOk(
    await fetch(`/api/admin/events/${id}`, {
      method: 'DELETE',
      credentials: 'include',
    }),
  )
}

export async function uploadEventImage(file: File): Promise<UploadedImage> {
  const form = new FormData()
  form.append('file', file)
  const res = await ensureOk(
    await fetch('/api/admin/blog/images', {
      method: 'POST',
      credentials: 'include',
      body: form,
    }),
  )
  return res.json() as Promise<UploadedImage>
}
