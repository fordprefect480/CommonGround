import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  createCommunityEvent,
  fetchAdminCommunityEvent,
  updateCommunityEvent,
  uploadEventImage,
  type CommunityEventWrite,
} from '../../api/events'

interface FormState {
  title: string
  startLocal: string
  endLocal: string
  body: string
  url: string
  featuredImageId: number | null
  imageUrl: string | null
}

const EMPTY: FormState = {
  title: '',
  startLocal: '',
  endLocal: '',
  body: '',
  url: '',
  featuredImageId: null,
  imageUrl: null,
}

function toLocalInputValue(iso: string | null): string {
  if (!iso) return ''
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return (
    `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` +
    `T${pad(d.getHours())}:${pad(d.getMinutes())}`
  )
}

function fromLocalInputValue(value: string): string | null {
  if (!value) return null
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return null
  return d.toISOString()
}

export default function CommunityEventEditor() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isNew = !id

  const [form, setForm] = useState<FormState>(EMPTY)
  const [loading, setLoading] = useState(!isNew)
  const [submitting, setSubmitting] = useState(false)
  const [uploadingImage, setUploadingImage] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (isNew) return
    setLoading(true)
    fetchAdminCommunityEvent(Number(id))
      .then((ev) => {
        setForm({
          title: ev.title,
          startLocal: toLocalInputValue(ev.startUtc),
          endLocal: toLocalInputValue(ev.endUtc),
          body: ev.body,
          url: ev.url ?? '',
          featuredImageId: ev.featuredImageId,
          imageUrl: ev.imageUrl,
        })
      })
      .catch((err: unknown) =>
        setError(err instanceof Error ? err.message : 'Load failed'),
      )
      .finally(() => setLoading(false))
  }, [id, isNew])

  const update = (patch: Partial<FormState>) =>
    setForm((prev) => ({ ...prev, ...patch }))

  const handleImageChange = async (file: File | null) => {
    if (!file) return
    setUploadingImage(true)
    setError(null)
    try {
      const uploaded = await uploadEventImage(file)
      update({ featuredImageId: uploaded.id, imageUrl: uploaded.url })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Image upload failed')
    } finally {
      setUploadingImage(false)
    }
  }

  const handleRemoveImage = () => {
    update({ featuredImageId: null, imageUrl: null })
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const startIso = fromLocalInputValue(form.startLocal)
    if (!startIso) {
      setError('Start date and time is required.')
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      const payload: CommunityEventWrite = {
        title: form.title,
        startUtc: startIso,
        endUtc: fromLocalInputValue(form.endLocal),
        body: form.body,
        url: form.url.trim() === '' ? null : form.url.trim(),
        featuredImageId: form.featuredImageId,
      }
      if (isNew) {
        await createCommunityEvent(payload)
      } else {
        await updateCommunityEvent(Number(id), payload)
      }
      navigate('/admin/events', { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed')
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <p className="admin-loading">Loading&hellip;</p>

  return (
    <section className="admin-page" aria-labelledby="event-editor-heading">
      <header className="admin-page-header">
        <h1 id="event-editor-heading" className="admin-page-title">
          {isNew ? 'New event' : 'Edit event'}
        </h1>
      </header>

      {error && <div className="form-error" role="alert">{error}</div>}

      <form className="card admin-form" onSubmit={handleSubmit}>
        <label className="field">
          <span className="field-label">Title</span>
          <input
            type="text"
            value={form.title}
            onChange={(e) => update({ title: e.target.value })}
            required
            maxLength={200}
            placeholder="Native bed planting"
          />
        </label>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <label className="field">
            <span className="field-label">Starts</span>
            <input
              type="datetime-local"
              value={form.startLocal}
              onChange={(e) => update({ startLocal: e.target.value })}
              required
            />
          </label>
          <label className="field">
            <span className="field-label">Ends (optional)</span>
            <input
              type="datetime-local"
              value={form.endLocal}
              onChange={(e) => update({ endLocal: e.target.value })}
            />
          </label>
        </div>

        <label className="field">
          <span className="field-label">Description</span>
          <textarea
            value={form.body}
            onChange={(e) => update({ body: e.target.value })}
            required
            rows={4}
            maxLength={1000}
            placeholder="Preparing native garden beds between the new pathways. Lend a hand if you can."
          />
        </label>

        <label className="field">
          <span className="field-label">Learn more URL (optional)</span>
          <input
            type="url"
            value={form.url}
            onChange={(e) => update({ url: e.target.value })}
            maxLength={500}
            placeholder="https://…"
          />
        </label>

        <div className="field">
          <span className="field-label">Image (optional)</span>
          {form.imageUrl ? (
            <div
              style={{
                display: 'flex',
                gap: 16,
                alignItems: 'flex-start',
                marginTop: 4,
              }}
            >
              <img
                src={form.imageUrl}
                alt=""
                style={{
                  width: 200,
                  aspectRatio: '16 / 10',
                  objectFit: 'cover',
                  border: '1px solid var(--ink-200)',
                  borderRadius: 'var(--r-md)',
                }}
              />
              <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                <label className="footer-link" style={{ cursor: 'pointer' }}>
                  Replace image
                  <input
                    type="file"
                    accept="image/*"
                    style={{ display: 'none' }}
                    disabled={uploadingImage}
                    onChange={(e) => handleImageChange(e.target.files?.[0] ?? null)}
                  />
                </label>
                <button
                  type="button"
                  className="footer-link"
                  onClick={handleRemoveImage}
                >
                  Remove image
                </button>
              </div>
            </div>
          ) : (
            <input
              type="file"
              accept="image/*"
              disabled={uploadingImage}
              onChange={(e) => handleImageChange(e.target.files?.[0] ?? null)}
            />
          )}
          {uploadingImage && (
            <p className="admin-loading" style={{ marginTop: 6 }}>Uploading&hellip;</p>
          )}
        </div>

        <div className="admin-actions">
          <button type="submit" className="primary-button" disabled={submitting || uploadingImage}>
            {submitting ? 'Saving…' : isNew ? 'Add event' : 'Save'}
          </button>
          <button
            type="button"
            className="footer-link"
            onClick={() => navigate('/admin/events')}
          >
            Cancel
          </button>
        </div>
      </form>
    </section>
  )
}
