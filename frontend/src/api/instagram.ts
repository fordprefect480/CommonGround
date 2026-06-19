export interface InstagramPost {
  id: number
  embedHtml: string
  displayOrder: number
}

export interface InstagramPostAdmin extends InstagramPost {
  createdAt: string
  updatedAt: string
}

export interface InstagramPostWrite {
  embedHtml: string
}

const JSON_HEADERS = { 'Content-Type': 'application/json' }

async function ensureOk(response: Response): Promise<Response> {
  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`
    try {
      const body = await response.clone().json() as { error?: string }
      if (body?.error) message = body.error
    } catch {
      // body wasn't JSON; fall back to status text
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
  const res = await ensureOk(await fetch(url, {
    method,
    headers: JSON_HEADERS,
    credentials: 'include',
    body: JSON.stringify(body),
  }))
  return res.json() as Promise<T>
}

export async function fetchPublicInstagramPosts(take = 6): Promise<InstagramPost[]> {
  const params = new URLSearchParams({ take: String(take) })
  return getJson(`/api/instagram/posts?${params}`)
}

export async function fetchAdminInstagramPosts(): Promise<InstagramPostAdmin[]> {
  return getJson('/api/admin/instagram/posts')
}

export async function createInstagramPost(input: InstagramPostWrite): Promise<InstagramPostAdmin> {
  return sendJson('/api/admin/instagram/posts', 'POST', input)
}

export async function updateInstagramPost(id: number, input: InstagramPostWrite): Promise<InstagramPostAdmin> {
  return sendJson(`/api/admin/instagram/posts/${id}`, 'PUT', input)
}

export async function deleteInstagramPost(id: number): Promise<void> {
  await ensureOk(await fetch(`/api/admin/instagram/posts/${id}`, {
    method: 'DELETE',
    credentials: 'include',
  }))
}

export async function reorderInstagramPosts(ids: number[]): Promise<void> {
  await ensureOk(await fetch('/api/admin/instagram/posts/reorder', {
    method: 'PUT',
    headers: JSON_HEADERS,
    credentials: 'include',
    body: JSON.stringify({ ids }),
  }))
}
