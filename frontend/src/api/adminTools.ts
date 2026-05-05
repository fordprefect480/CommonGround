export interface ImportBlogResult {
  imported: number
  skipped: number
  failed: number
  errors: { slug: string | null; message: string }[]
}

export async function importBlog(limit?: number): Promise<ImportBlogResult> {
  const url = limit && limit > 0
    ? `/api/admin/tools/import-blog?limit=${limit}`
    : '/api/admin/tools/import-blog'
  const res = await fetch(url, {
    method: 'POST',
    credentials: 'include',
  })
  if (!res.ok) throw new Error(await res.text())
  return res.json()
}

export async function cleanupOrphanImages(): Promise<{ deleted: number }> {
  const res = await fetch('/api/admin/tools/orphan-images/cleanup', {
    method: 'POST',
    credentials: 'include',
  })
  if (!res.ok) throw new Error(await res.text())
  return res.json()
}
