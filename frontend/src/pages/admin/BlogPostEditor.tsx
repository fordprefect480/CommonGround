import { lazy, Suspense, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  blogImageUrl,
  createBlogPost,
  fetchAdminBlogPost,
  fetchBlogCategories,
  updateBlogPost,
  uploadBlogImage,
  type BlogCategory,
} from '../../api/blog'

const BlogEditor = lazy(() => import('./BlogEditor'))

const STATUS_DRAFT = 0
const STATUS_PUBLISHED = 1

interface FormState {
  title: string
  bodyHtml: string
  categoryId: number | null
  featuredImageId: number | null
  status: number
}

const EMPTY: FormState = {
  title: '',
  bodyHtml: '',
  categoryId: null,
  featuredImageId: null,
  status: STATUS_DRAFT,
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
          featuredImageId: post.featuredImageId,
          status: post.status,
        })
        setOriginalStatus(post.status)
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Load failed'))
      .finally(() => setLoading(false))
  }, [id, isNew])

  const update = (patch: Partial<FormState>) => setForm((prev) => ({ ...prev, ...patch }))

  const handleFeaturedUpload = async (file: File) => {
    try {
      const result = await uploadBlogImage(file)
      update({ featuredImageId: result.id })
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Upload failed')
    }
  }

  const submit = async (status: number) => {
    setSubmitting(true)
    setError(null)
    try {
      const payload = {
        title: form.title,
        excerpt: null,
        bodyHtml: form.bodyHtml,
        categoryId: form.categoryId,
        featuredImageId: form.featuredImageId,
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

  const featuredUrl = blogImageUrl(form.featuredImageId)
  const isPublished = originalStatus === STATUS_PUBLISHED

  return (
    <section className="admin-page" aria-labelledby="editor-heading">
      <header className="admin-page-header">
        <h1 id="editor-heading" className="admin-page-title">{isNew ? 'New post' : 'Edit post'}</h1>
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
          <span className="field-label">Featured image</span>
          {featuredUrl && <img src={featuredUrl} alt="" style={{ maxWidth: 240, display: 'block', marginBottom: 8 }} />}
          <input
            type="file"
            accept="image/jpeg,image/png,image/webp,image/gif"
            onChange={(e) => {
              const f = e.target.files?.[0]
              if (f) handleFeaturedUpload(f)
            }}
          />
        </div>

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
