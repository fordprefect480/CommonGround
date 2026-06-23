import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import AdminBackButton from './AdminBackButton'
import {
  createCommunityEvent,
  fetchAdminCommunityEvent,
  updateCommunityEvent,
  uploadEventImage,
  type CommunityEventWrite,
  type RepeatFrequency,
} from '../../api/events'

interface FormState {
  title: string
  startLocal: string
  endLocal: string
  body: string
  url: string
  featuredImageId: number | null
  imageUrl: string | null
  repeatFrequency: RepeatFrequency
  repeatUntil: string
}

const EMPTY: FormState = {
  title: '',
  startLocal: '',
  endLocal: '',
  body: '',
  url: '',
  featuredImageId: null,
  imageUrl: null,
  repeatFrequency: 'none',
  repeatUntil: '',
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

const MAX_OCCURRENCES = 104

function countOccurrences(
  startLocal: string,
  frequency: RepeatFrequency,
  untilDate: string,
): number | null {
  if (!startLocal || frequency === 'none' || !untilDate) return null
  const start = new Date(startLocal)
  const until = new Date(`${untilDate}T23:59:59`)
  if (Number.isNaN(start.getTime()) || Number.isNaN(until.getTime()) || until < start) {
    return null
  }

  if (frequency === 'weekly' || frequency === 'fortnightly') {
    const stepDays = frequency === 'weekly' ? 7 : 14
    let count = 0
    const cursor = new Date(start)
    while (cursor <= until && count < MAX_OCCURRENCES) {
      count++
      cursor.setDate(cursor.getDate() + stepDays)
    }
    return count
  }

  // monthly: same weekday position (e.g. 3rd Saturday)
  const weekday = start.getDay()
  const ordinal = Math.floor((start.getDate() - 1) / 7) + 1
  let count = 0
  for (let monthOffset = 0; count < MAX_OCCURRENCES; monthOffset++) {
    const probe = new Date(start.getFullYear(), start.getMonth() + monthOffset, 1)
    if (probe > until) break
    const offset = (weekday - probe.getDay() + 7) % 7
    const day = 1 + offset + (ordinal - 1) * 7
    const daysInMonth = new Date(probe.getFullYear(), probe.getMonth() + 1, 0).getDate()
    if (day <= daysInMonth) {
      const occ = new Date(probe.getFullYear(), probe.getMonth(), day, start.getHours(), start.getMinutes())
      if (occ > until) break
      if (occ >= start) count++
    }
  }
  return count
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
          repeatFrequency: 'none',
          repeatUntil: '',
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
    if (isNew && form.repeatFrequency !== 'none' && !form.repeatUntil) {
      setError('Please choose a date to repeat until.')
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
        repeatFrequency: isNew ? form.repeatFrequency : 'none',
        repeatUntil:
          isNew && form.repeatFrequency !== 'none' && form.repeatUntil
            ? form.repeatUntil
            : null,
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
        <div className="admin-page-heading-group">
          <AdminBackButton to="/admin/events" label="Back to events" />
          <h1 id="event-editor-heading" className="admin-page-title">
            {isNew ? 'New event' : 'Edit event'}
          </h1>
        </div>
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

        {isNew && (
          <div className="field">
            <span className="field-label">Repeats</span>
            <select
              value={form.repeatFrequency}
              onChange={(e) =>
                update({ repeatFrequency: e.target.value as RepeatFrequency })
              }
            >
              <option value="none">Does not repeat</option>
              <option value="weekly">Weekly</option>
              <option value="fortnightly">Every 2 weeks</option>
              <option value="monthly">Monthly (same weekday)</option>
            </select>

            {form.repeatFrequency !== 'none' && (
              <label className="field" style={{ marginTop: 12 }}>
                <span className="field-label">Repeat until</span>
                <input
                  type="date"
                  value={form.repeatUntil}
                  onChange={(e) => update({ repeatUntil: e.target.value })}
                  required
                />
              </label>
            )}

            {(() => {
              const count = countOccurrences(
                form.startLocal,
                form.repeatFrequency,
                form.repeatUntil,
              )
              if (count === null) return null
              return (
                <p className="admin-loading" style={{ marginTop: 8 }}>
                  This will create {count} {count === 1 ? 'event' : 'separate events'}.
                </p>
              )
            })()}
          </div>
        )}

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
