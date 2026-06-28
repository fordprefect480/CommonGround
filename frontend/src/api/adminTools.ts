export interface ImportBlogResult {
  imported: number
  skipped: number
  failed: number
  errors: { slug: string | null; message: string }[]
}

export interface ImportBlogProgress {
  phase: 'fetching' | 'importing'
  current: number
  total: number
  slug: string | null
}

async function postJson<T>(url: string): Promise<T> {
  const res = await fetch(url, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: '{}',
  })
  if (!res.ok) throw new Error(await res.text())
  return res.json()
}

// The import endpoint streams Server-Sent Events: a `progress` event per post as it
// is fetched/imported, then a final `done` event carrying the result.
export async function importBlog(
  limit?: number,
  onProgress?: (progress: ImportBlogProgress) => void,
): Promise<ImportBlogResult> {
  const url = limit && limit > 0
    ? `/api/admin/tools/import-blog?limit=${limit}`
    : '/api/admin/tools/import-blog'
  const res = await fetch(url, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
    body: '{}',
  })
  if (!res.ok || !res.body) throw new Error(await res.text())

  const reader = res.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  let result: ImportBlogResult | undefined

  for (;;) {
    const { done, value } = await reader.read()
    if (done) break
    buffer += decoder.decode(value, { stream: true })

    let sep: number
    while ((sep = buffer.indexOf('\n\n')) !== -1) {
      const { event, data } = parseSseFrame(buffer.slice(0, sep))
      buffer = buffer.slice(sep + 2)
      if (!data) continue
      if (event === 'progress') onProgress?.(JSON.parse(data) as ImportBlogProgress)
      else if (event === 'done') result = JSON.parse(data) as ImportBlogResult
    }
  }

  if (!result) throw new Error('Import ended without a result')
  return result
}

function parseSseFrame(frame: string): { event: string; data: string } {
  let event = 'message'
  const dataLines: string[] = []
  for (const rawLine of frame.split('\n')) {
    const line = rawLine.replace(/\r$/, '')
    if (line.startsWith('event:')) event = line.slice(6).trim()
    else if (line.startsWith('data:')) dataLines.push(line.slice(5).replace(/^ /, ''))
  }
  return { event, data: dataLines.join('\n') }
}

export async function cleanupOrphanImages(): Promise<{ deleted: number }> {
  return postJson('/api/admin/tools/orphan-images/cleanup')
}

async function getJson<T>(url: string): Promise<T> {
  const res = await fetch(url, { credentials: 'include' })
  if (!res.ok) throw new Error(await res.text())
  return res.json()
}

export async function getMembershipPrice(): Promise<{ priceCents: number }> {
  return getJson('/api/admin/tools/membership-price')
}

export async function updateMembershipPrice(priceCents: number): Promise<{ priceCents: number }> {
  return putPrice('/api/admin/tools/membership-price', priceCents)
}

export async function getLeasedBedPrice(): Promise<{ priceCents: number }> {
  return getJson('/api/admin/tools/leased-bed-price')
}

export async function updateLeasedBedPrice(priceCents: number): Promise<{ priceCents: number }> {
  return putPrice('/api/admin/tools/leased-bed-price', priceCents)
}

export async function getComingSoon(): Promise<{ comingSoon: boolean }> {
  return getJson('/api/admin/tools/coming-soon')
}

export async function updateComingSoon(comingSoon: boolean): Promise<{ comingSoon: boolean }> {
  const res = await fetch('/api/admin/tools/coming-soon', {
    method: 'PUT',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ comingSoon }),
  })
  if (!res.ok) throw new Error(await res.text())
  return res.json()
}

async function putPrice(url: string, priceCents: number): Promise<{ priceCents: number }> {
  const res = await fetch(url, {
    method: 'PUT',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ priceCents }),
  })
  if (!res.ok) {
    const text = await res.text()
    let message = text
    try {
      message = JSON.parse(text).error ?? text
    } catch {
      // Response body wasn't JSON - fall back to the raw text.
    }
    throw new Error(message)
  }
  return res.json()
}
