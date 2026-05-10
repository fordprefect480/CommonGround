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
