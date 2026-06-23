import { lazy, Suspense, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import AdminBackButton from './AdminBackButton'
import {
  createBlogPost,
  fetchAdminBlogPost,
  fetchBlogCategories,
  updateBlogPost,
  type BlogCategory,
} from '../../api/blog'

const BlogEditor = lazy(() => import('./BlogEditor'))

const STATUS_DRAFT = 0
const STATUS_PUBLISHED = 1

interface FormState {
  title: string
  bodyHtml: string
  categoryId: number | null
  status: number
}

const EMPTY: FormState = {
  title: '',
  bodyHtml: '',
  categoryId: null,
  status: STATUS_DRAFT,
}

// The featured image is derived from the first image in the post body. Image
// sources look like "/api/blog/images/{id}", so we recover the id from there.
function deriveFeaturedImageId(bodyHtml: string): number | null {
  const src = new DOMParser()
    .parseFromString(bodyHtml, 'text/html')
    .querySelector('img')
    ?.getAttribute('src')
  const match = src?.match(/\/api\/blog\/images\/(\d+)/)
  return match ? Number(match[1]) : null
}

export default function BlogPostEditor() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isNew = !id

  const [form, setForm] = useState<FormState>(EMPTY)
  const [categories, setCategories] = useState<BlogCategory[]>([])
  const [loading, setLoading] = useState(!isNew)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [originalStatus, setOriginalStatus] = useState(STATUS_DRAFT)

  useEffect(() => {
    fetchBlogCategories().then(setCategories).catch(() => setCategories([]))
  }, [])

  useEffect(() => {
    if (isNew) return
    setLoading(true)
    fetchAdminBlogPost(Number(id))
      .then((post) => {
        setForm({
          title: post.title,
          bodyHtml: post.bodyHtml,
          categoryId: post.categoryId,
          status: post.status,
        })
        setOriginalStatus(post.status)
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Load failed'))
      .finally(() => setLoading(false))
  }, [id, isNew])

  const update = (patch: Partial<FormState>) => setForm((prev) => ({ ...prev, ...patch }))

  const submit = async (status: number) => {
    setSubmitting(true)
    setError(null)
    try {
      const payload = {
        title: form.title,
        excerpt: null,
        bodyHtml: form.bodyHtml,
        categoryId: form.categoryId,
        featuredImageId: deriveFeaturedImageId(form.bodyHtml),
        status,
      }
      const result = isNew
        ? await createBlogPost(payload)
        : await updateBlogPost(Number(id), payload)
      navigate(`/admin/blog/${result.id}/edit`, { replace: true })
      setOriginalStatus(result.status)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed')
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <p className="admin-loading">Loading&hellip;</p>

  const isPublished = originalStatus === STATUS_PUBLISHED

  return (
    <section className="admin-page" aria-labelledby="editor-heading">
      <header className="admin-page-header">
        <div className="admin-page-heading-group">
          <AdminBackButton to="/admin/blog" label="Back to posts" />
          <h1 id="editor-heading" className="admin-page-title">{isNew ? 'New post' : 'Edit post'}</h1>
        </div>
      </header>

      {error && <div className="form-error" role="alert">{error}</div>}

      <form className="card admin-form" onSubmit={(e) => e.preventDefault()}>
        <label className="field">
          <span className="field-label">Title</span>
          <input value={form.title} onChange={(e) => update({ title: e.target.value })} required />
        </label>

        <label className="field">
          <span className="field-label">Category</span>
          <select
            value={form.categoryId ?? ''}
            onChange={(e) => update({ categoryId: e.target.value ? Number(e.target.value) : null })}
          >
            <option value="">- None -</option>
            {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </label>

        <div className="field">
          <span className="field-label">Body</span>
          <Suspense fallback={<p className="admin-loading">Loading editor&hellip;</p>}>
            <BlogEditor value={form.bodyHtml} onChange={(html) => update({ bodyHtml: html })} />
          </Suspense>
        </div>

        <div className="admin-actions">
          {!isPublished && (
            <button type="button" className="primary-button" disabled={submitting} onClick={() => submit(STATUS_DRAFT)}>
              {submitting ? 'Saving…' : 'Save draft'}
            </button>
          )}
          <button
            type="button"
            className="primary-button"
            disabled={submitting}
            onClick={() => submit(STATUS_PUBLISHED)}
          >
            {isPublished ? 'Save' : 'Publish'}
          </button>
          {isPublished && (
            <button type="button" className="footer-link" disabled={submitting} onClick={() => submit(STATUS_DRAFT)}>
              Unpublish
            </button>
          )}
          <button type="button" className="footer-link" onClick={() => navigate('/admin/blog')}>Cancel</button>
        </div>
      </form>
    </section>
  )
}
