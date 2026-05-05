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
  morePosts: BlogPostSummary[]
}

export interface BlogPostAdminListItem {
  id: number
  slug: string
  title: string
  authorName: string
  categoryId: number | null
  featuredImageId: number | null
  status: number
  publishedAt: string | null
  createdAt: string
  updatedAt: string
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
  categoryId: number | null
  featuredImageId: number | null
  status: number
}

export interface BlogPostList {
  posts: BlogPostSummary[]
  page: number
  pageSize: number
  total: number
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

const JSON_HEADERS = { 'Content-Type': 'application/json' }

async function ensureOk(response: Response): Promise<Response> {
  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `${response.status} ${response.statusText}`)
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

export async function fetchBlogPosts(page = 1, pageSize = 20): Promise<BlogPostList> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return getJson(`/api/blog/posts?${params}`)
}

export async function fetchBlogPost(slug: string): Promise<BlogPost | null> {
  const res = await fetch(`/api/blog/posts/${encodeURIComponent(slug)}`, { credentials: 'include' })
  if (res.status === 404) return null
  await ensureOk(res)
  return res.json() as Promise<BlogPost>
}

export async function fetchBlogCategories(): Promise<BlogCategory[]> {
  return getJson('/api/blog/categories')
}

export async function fetchAdminBlogPosts(): Promise<BlogPostAdminListItem[]> {
  return getJson('/api/admin/blog/posts')
}

export async function fetchAdminBlogPost(id: number): Promise<BlogPostAdmin> {
  return getJson(`/api/admin/blog/posts/${id}`)
}

export async function createBlogPost(input: BlogPostWrite): Promise<BlogPostAdmin> {
  return sendJson('/api/admin/blog/posts', 'POST', input)
}

export async function updateBlogPost(id: number, input: BlogPostWrite): Promise<BlogPostAdmin> {
  return sendJson(`/api/admin/blog/posts/${id}`, 'PUT', input)
}

export async function deleteBlogPost(id: number): Promise<void> {
  await ensureOk(await fetch(`/api/admin/blog/posts/${id}`, {
    method: 'DELETE',
    credentials: 'include',
  }))
}

export async function uploadBlogImage(file: File): Promise<BlogImageUploadResult> {
  const form = new FormData()
  form.append('file', file)
  const res = await ensureOk(await fetch('/api/admin/blog/images', {
    method: 'POST',
    credentials: 'include',
    body: form,
  }))
  return res.json() as Promise<BlogImageUploadResult>
}

export function blogImageUrl(id: number | null | undefined): string | null {
  return id == null ? null : `/api/blog/images/${id}`
}
