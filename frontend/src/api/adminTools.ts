export interface ImportBlogResult {
  imported: number
  skipped: number
  failed: number
  errors: { slug: string | null; message: string }[]
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

export async function importBlog(limit?: number): Promise<ImportBlogResult> {
  const url = limit && limit > 0
    ? `/api/admin/tools/import-blog?limit=${limit}`
    : '/api/admin/tools/import-blog'
  return postJson(url)
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
  const res = await fetch('/api/admin/tools/membership-price', {
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
