import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import AdminBackButton from './AdminBackButton'
import {
  createInstagramPost,
  fetchAdminInstagramPosts,
  updateInstagramPost,
} from '../../api/instagram'

interface FormState {
  embedHtml: string
}

const EMPTY: FormState = {
  embedHtml: '',
}

export default function InstagramTileEditor() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isNew = !id

  const [form, setForm] = useState<FormState>(EMPTY)
  const [loading, setLoading] = useState(!isNew)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (isNew) return
    setLoading(true)
    fetchAdminInstagramPosts()
      .then((posts) => {
        const found = posts.find((p) => p.id === Number(id))
        if (!found) {
          setError('Tile not found.')
          return
        }
        setForm({ embedHtml: found.embedHtml })
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Load failed'))
      .finally(() => setLoading(false))
  }, [id, isNew])

  const update = (patch: Partial<FormState>) => setForm((prev) => ({ ...prev, ...patch }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      const payload = {
        embedHtml: form.embedHtml,
      }
      if (isNew) {
        await createInstagramPost(payload)
      } else {
        await updateInstagramPost(Number(id), payload)
      }
      navigate('/admin/instagram', { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed')
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <p className="admin-loading">Loading&hellip;</p>

  return (
    <section className="admin-page" aria-labelledby="instagram-editor-heading">
      <header className="admin-page-header">
        <div className="admin-page-heading-group">
          <AdminBackButton to="/admin/instagram" label="Back to Instagram tiles" />
          <h1 id="instagram-editor-heading" className="admin-page-title">
            {isNew ? 'New Instagram tile' : 'Edit Instagram tile'}
          </h1>
        </div>
      </header>

      {error && <div className="form-error" role="alert">{error}</div>}

      <form className="card admin-form" onSubmit={handleSubmit}>
        <label className="field">
          <span className="field-label">Embed snippet</span>
          <p className="admin-empty" style={{ marginTop: 0 }}>
            On Instagram: open the post → ⋯ menu → Embed → <strong>Copy Embed Code</strong>. Paste the entire snippet below.
          </p>
          <textarea
            value={form.embedHtml}
            onChange={(e) => update({ embedHtml: e.target.value })}
            required
            rows={10}
            placeholder='<blockquote class="instagram-media" data-instgrm-permalink="..." …>…</blockquote>'
            style={{ fontFamily: 'var(--font-mono)', fontSize: 12, width: '100%', resize: 'vertical' }}
          />
        </label>

        <div className="admin-actions">
          <button type="submit" className="primary-button" disabled={submitting}>
            {submitting ? 'Saving…' : isNew ? 'Add tile' : 'Save'}
          </button>
          <button type="button" className="footer-link" onClick={() => navigate('/admin/instagram')}>
            Cancel
          </button>
        </div>
      </form>
    </section>
  )
}
