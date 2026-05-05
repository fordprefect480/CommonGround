export interface BlogPostSummary {
  id: number
  slug: string
  title: string
  excerpt: string | null
  authorName: string
  publishedAt: string | null
  categoryName: string | null
  featuredImageId: number | null
}

export interface BlogPost {
  id: number
  slug: string
  title: string
  excerpt: string | null
  bodyHtml: string
  authorName: string
  publishedAt: string | null
  categoryName: string | null
  featuredImageId: number | null
  relatedPosts: BlogPostSummary[]
}

export interface BlogPostAdmin {
  id: number
  slug: string
  title: string
  excerpt: string | null
  bodyHtml: string
  authorName: string
  categoryId: number | null
  featuredImageId: number | null
  status: number
  publishedAt: string | null
  createdAt: string
  updatedAt: string
}

export interface BlogPostWrite {
  title: string
  slug?: string
  excerpt?: string | null
  bodyHtml: string
  authorName: string
  categoryId: number | null
  featuredImageId: number | null
  status: number
}

export interface BlogCategory {
  id: number
  name: string
  slug: string
}

export interface BlogImageUploadResult {
  id: number
  url: string
}

async function jsonOrThrow<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `${response.status} ${response.statusText}`)
  }
  return response.json() as Promise<T>
}

export async function fetchBlogPosts(): Promise<BlogPostSummary[]> {
  return jsonOrThrow(await fetch('/api/blog/posts', { credentials: 'include' }))
}

export async function fetchBlogPost(slug: string): Promise<BlogPost | null> {
  const res = await fetch(`/api/blog/posts/${encodeURIComponent(slug)}`, { credentials: 'include' })
  if (res.status === 404) return null
  return jsonOrThrow(res)
}

export async function fetchBlogCategories(): Promise<BlogCategory[]> {
  return jsonOrThrow(await fetch('/api/blog/categories', { credentials: 'include' }))
}

export async function fetchAdminBlogPosts(): Promise<BlogPostAdmin[]> {
  return jsonOrThrow(await fetch('/api/admin/blog/posts', { credentials: 'include' }))
}

export async function fetchAdminBlogPost(id: number): Promise<BlogPostAdmin> {
  return jsonOrThrow(await fetch(`/api/admin/blog/posts/${id}`, { credentials: 'include' }))
}

export async function createBlogPost(input: BlogPostWrite): Promise<BlogPostAdmin> {
  const res = await fetch('/api/admin/blog/posts', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(input),
  })
  return jsonOrThrow(res)
}

export async function updateBlogPost(id: number, input: BlogPostWrite): Promise<BlogPostAdmin> {
  const res = await fetch(`/api/admin/blog/posts/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(input),
  })
  return jsonOrThrow(res)
}

export async function deleteBlogPost(id: number): Promise<void> {
  const res = await fetch(`/api/admin/blog/posts/${id}`, {
    method: 'DELETE',
    credentials: 'include',
  })
  if (!res.ok) throw new Error(await res.text())
}

export async function uploadBlogImage(file: File): Promise<BlogImageUploadResult> {
  const form = new FormData()
  form.append('file', file)
  const res = await fetch('/api/admin/blog/images', {
    method: 'POST',
    credentials: 'include',
    body: form,
  })
  return jsonOrThrow(res)
}

export function blogImageUrl(id: number | null | undefined): string | null {
  return id == null ? null : `/api/blog/images/${id}`
}
