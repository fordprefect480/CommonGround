import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import PromptModal from './PromptModal'
import {
  cleanupOrphanImages,
  getComingSoon,
  getLeasedBedPrice,
  getMembershipPrice,
  importBlog,
  updateComingSoon,
  updateLeasedBedPrice,
  updateMembershipPrice,
  type ImportBlogProgress,
  type ImportBlogResult,
} from '../../api/adminTools'

type ImportState =
  | { status: 'idle' }
  | { status: 'running'; progress: ImportBlogProgress | null }
  | { status: 'done'; result: ImportBlogResult }
  | { status: 'error'; message: string }

type CleanupState =
  | { status: 'idle' }
  | { status: 'running' }
  | { status: 'done'; deleted: number }
  | { status: 'error'; message: string }

export default function AdminTools() {
  const navigate = useNavigate()
  const [importState, setImportState] = useState<ImportState>({ status: 'idle' })
  const [importOpen, setImportOpen] = useState(false)
  const [cleanupState, setCleanupState] = useState<CleanupState>({ status: 'idle' })

  const startImport = (answer: string) => {
    const trimmed = answer.trim()
    const limit = trimmed === '' ? undefined : Number(trimmed)
    setImportState({ status: 'running', progress: null })
    importBlog(limit, (progress) => setImportState({ status: 'running', progress }))
      .then((result) => setImportState({ status: 'done', result }))
      .catch((err) => setImportState({ status: 'error', message: err instanceof Error ? err.message : 'Import failed' }))
  }

  const runCleanup = async () => {
    setCleanupState({ status: 'running' })
    try {
      const result = await cleanupOrphanImages()
      setCleanupState({ status: 'done', deleted: result.deleted })
    } catch (err) {
      setCleanupState({ status: 'error', message: err instanceof Error ? err.message : 'Cleanup failed' })
    }
  }

  return (
    <section className="admin-page" aria-labelledby="settings-heading">
      <header className="admin-page-header">
        <h1 id="settings-heading" className="admin-page-title">Settings</h1>
      </header>

      <div className="admin-tools-grid">
        <PriceCard
          id="membership-price"
          title="Membership price"
          note="The annual membership fee shown on the website and charged at checkout."
          load={() => getMembershipPrice().then((r) => r.priceCents)}
          save={(cents) => updateMembershipPrice(cents).then((r) => r.priceCents)}
          allowZero={false}
        />

        <PriceCard
          id="leased-bed-price"
          title="Leased bed price"
          note="The default annual fee for a leased bed. An admin can override it per bed when allocating."
          load={() => getLeasedBedPrice().then((r) => r.priceCents)}
          save={(cents) => updateLeasedBedPrice(cents).then((r) => r.priceCents)}
          allowZero
        />
      </div>

      <details className="admin-advanced-panel">
        <summary className="admin-advanced-summary">Advanced</summary>
        <div className="admin-tools-grid">
          <ComingSoonCard />

          <div className="card">
            <h2 className="section-title">Send test email</h2>
            <p className="card-note">Compose a one-off email and send it to specific addresses without touching the mailing list. Useful for previewing templates.</p>
            <button type="button" className="primary-button" onClick={() => navigate('/admin/tools/email-test')}>
              Open
            </button>
          </div>

          <div className="card">
            <h2 className="section-title">Import historical blog posts</h2>
            <p className="card-note">Pulls newsletters from the existing reference site. Idempotent - already-imported posts are skipped.</p>
            {importState.status === 'running' ? (
              <ImportProgressBar progress={importState.progress} />
            ) : (
              <button type="button" className="primary-button" onClick={() => setImportOpen(true)}>
                Run import
              </button>
            )}
            {importState.status === 'done' && (
              <div className="card-note" role="status">
                Imported {importState.result.imported}, skipped {importState.result.skipped}, failed {importState.result.failed}.
                {importState.result.errors.length > 0 && (
                  <ul>{importState.result.errors.map((e, i) => <li key={i}>{e.slug ?? '(enumeration)'}: {e.message}</li>)}</ul>
                )}
              </div>
            )}
            {importState.status === 'error' && <div className="form-error" role="alert">{importState.message}</div>}
          </div>

          <div className="card">
            <h2 className="section-title">Clean up orphan images</h2>
            <p className="card-note">Removes uploaded images older than 24h that aren't referenced by any post.</p>
            <button type="button" className="primary-button" onClick={runCleanup} disabled={cleanupState.status === 'running'}>
              {cleanupState.status === 'running' ? 'Working…' : 'Run cleanup'}
            </button>
            {cleanupState.status === 'done' && <div className="card-note" role="status">Deleted {cleanupState.deleted} orphan image(s).</div>}
            {cleanupState.status === 'error' && <div className="form-error" role="alert">{cleanupState.message}</div>}
          </div>
        </div>
      </details>

      <PromptModal
        open={importOpen}
        title="Import historical blog posts"
        label="Number of posts"
        description="How many of the most recent posts to import? Leave blank to import all."
        initialValue="5"
        placeholder="5"
        inputMode="numeric"
        confirmLabel="Run import"
        onClose={() => setImportOpen(false)}
        validate={(v) => {
          const t = v.trim()
          if (t === '') return null
          const n = Number(t)
          return Number.isInteger(n) && n > 0 ? null : 'Enter a positive whole number, or leave blank to import all.'
        }}
        onConfirm={startImport}
      />
    </section>
  )
}

function ImportProgressBar({ progress }: { progress: ImportBlogProgress | null }) {
  const pct = progress && progress.total > 0 ? Math.round((progress.current / progress.total) * 100) : 0
  const label = progress
    ? `${progress.phase === 'fetching' ? 'Reading posts' : 'Importing posts'} ${progress.current} of ${progress.total}`
    : 'Starting…'

  return (
    <div className="import-progress" role="status">
      <div className="import-progress-label">
        {label}
        {progress?.slug && <span className="import-progress-slug"> — {progress.slug}</span>}
      </div>
      <div
        className="progress-track"
        role="progressbar"
        aria-label="Import progress"
        aria-valuemin={0}
        aria-valuemax={progress?.total ?? 0}
        aria-valuenow={progress?.current ?? 0}
      >
        <div className="progress-fill" style={{ width: `${pct}%` }} />
      </div>
    </div>
  )
}

function ComingSoonCard() {
  const [comingSoon, setComingSoon] = useState(false)
  const [loaded, setLoaded] = useState(false)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState<{ kind: 'ok' | 'error'; text: string } | null>(null)

  useEffect(() => {
    getComingSoon()
      .then((r) => {
        setComingSoon(r.comingSoon)
        setLoaded(true)
      })
      .catch((err) => setMessage({ kind: 'error', text: err instanceof Error ? err.message : 'Could not load the setting.' }))
  }, [])

  const toggle = async () => {
    const next = !comingSoon
    setSaving(true)
    setMessage(null)
    try {
      const result = await updateComingSoon(next)
      setComingSoon(result.comingSoon)
      setMessage({
        kind: 'ok',
        text: result.comingSoon
          ? 'Under-construction page is on. Only signed-in admins can see the site.'
          : 'The site is now live and visible to everyone.',
      })
    } catch (err) {
      setMessage({ kind: 'error', text: err instanceof Error ? err.message : 'Could not save the setting.' })
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="card">
      <h2 className="section-title">Under-construction page</h2>
      <p className="card-note">
        While this is on, the public sees an &ldquo;under construction&rdquo; page with a log-in
        button. Only signed-in admins can view the real site &mdash; use it to preview before launch.
        Turn it off when you&rsquo;re ready to go live.
      </p>
      <div className="field" style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
        <button
          type="button"
          role="switch"
          id="coming-soon"
          className="switch"
          aria-checked={comingSoon}
          aria-labelledby="coming-soon-label"
          onClick={toggle}
          disabled={!loaded || saving}
        >
          <span className="switch-thumb" aria-hidden="true" />
        </button>
        <label id="coming-soon-label" className="field-label" htmlFor="coming-soon" style={{ margin: 0 }}>
          {comingSoon ? 'Site is hidden from the public' : 'Site is live for everyone'}
        </label>
      </div>
      {message?.kind === 'ok' && <div className="card-note" role="status">{message.text}</div>}
      {message?.kind === 'error' && <div className="form-error" role="alert">{message.text}</div>}
    </div>
  )
}

function PriceCard({
  id,
  title,
  note,
  load,
  save,
  allowZero,
}: {
  id: string
  title: string
  note: string
  load: () => Promise<number>
  save: (cents: number) => Promise<number>
  allowZero: boolean
}) {
  const [dollars, setDollars] = useState('')
  const [loaded, setLoaded] = useState(false)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState<{ kind: 'ok' | 'error'; text: string } | null>(null)

  useEffect(() => {
    load()
      .then((cents) => {
        setDollars((cents / 100).toString())
        setLoaded(true)
      })
      .catch((err) => setMessage({ kind: 'error', text: err instanceof Error ? err.message : 'Could not load the price.' }))
  }, [load])

  const onSave = async () => {
    const value = Number(dollars)
    if (!Number.isFinite(value) || value < 0 || (!allowZero && value <= 0)) {
      setMessage({ kind: 'error', text: allowZero ? 'Enter a price in dollars, for example 80.' : 'Enter a price in dollars, for example 25.' })
      return
    }
    setSaving(true)
    setMessage(null)
    try {
      const cents = await save(Math.round(value * 100))
      setDollars((cents / 100).toString())
      setMessage({ kind: 'ok', text: 'Saved. The new price is now shown across the site.' })
    } catch (err) {
      setMessage({ kind: 'error', text: err instanceof Error ? err.message : 'Could not save the price.' })
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="card">
      <h2 className="section-title">{title}</h2>
      <p className="card-note">{note}</p>
      <div className="field">
        <label className="field-label" htmlFor={id}>Price per year</label>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, maxWidth: 220 }}>
          <span aria-hidden="true">$</span>
          <input
            id={id}
            type="number"
            min={allowZero ? '0' : '1'}
            step="1"
            inputMode="decimal"
            value={dollars}
            onChange={(e) => setDollars(e.target.value)}
            disabled={!loaded || saving}
          />
        </div>
      </div>
      <button type="button" className="primary-button" onClick={onSave} disabled={!loaded || saving}>
        {saving ? 'Saving…' : 'Save price'}
      </button>
      {message?.kind === 'ok' && <div className="card-note" role="status">{message.text}</div>}
      {message?.kind === 'error' && <div className="form-error" role="alert">{message.text}</div>}
    </div>
  )
}
