import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { cleanupOrphanImages, getMembershipPrice, importBlog, updateMembershipPrice, type ImportBlogResult } from '../../api/adminTools'

type ImportState =
  | { status: 'idle' }
  | { status: 'running' }
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
  const [cleanupState, setCleanupState] = useState<CleanupState>({ status: 'idle' })
  const [priceDollars, setPriceDollars] = useState('')
  const [priceLoaded, setPriceLoaded] = useState(false)
  const [priceSaving, setPriceSaving] = useState(false)
  const [priceMessage, setPriceMessage] = useState<{ kind: 'ok' | 'error'; text: string } | null>(null)

  useEffect(() => {
    getMembershipPrice()
      .then(({ priceCents }) => {
        setPriceDollars((priceCents / 100).toString())
        setPriceLoaded(true)
      })
      .catch((err) => setPriceMessage({ kind: 'error', text: err instanceof Error ? err.message : 'Could not load the price.' }))
  }, [])

  const savePrice = async () => {
    const dollars = Number(priceDollars)
    if (!Number.isFinite(dollars) || dollars <= 0) {
      setPriceMessage({ kind: 'error', text: 'Enter a price in dollars, for example 25.' })
      return
    }
    setPriceSaving(true)
    setPriceMessage(null)
    try {
      const { priceCents } = await updateMembershipPrice(Math.round(dollars * 100))
      setPriceDollars((priceCents / 100).toString())
      setPriceMessage({ kind: 'ok', text: 'Saved. The new price is now shown across the site.' })
    } catch (err) {
      setPriceMessage({ kind: 'error', text: err instanceof Error ? err.message : 'Could not save the price.' })
    } finally {
      setPriceSaving(false)
    }
  }

  const runImport = async () => {
    const answer = prompt('How many of the most recent posts to import? (leave blank for all)', '5')
    if (answer === null) return
    const trimmed = answer.trim()
    let limit: number | undefined
    if (trimmed !== '') {
      const parsed = Number(trimmed)
      if (!Number.isInteger(parsed) || parsed <= 0) {
        alert('Please enter a positive whole number, or leave blank to import all.')
        return
      }
      limit = parsed
    }
    setImportState({ status: 'running' })
    try {
      const result = await importBlog(limit)
      setImportState({ status: 'done', result })
    } catch (err) {
      setImportState({ status: 'error', message: err instanceof Error ? err.message : 'Import failed' })
    }
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

      <div className="card">
        <h2 className="section-title">Membership price</h2>
        <p className="card-note">The annual membership fee shown on the website and charged at checkout.</p>
        <div className="field">
          <label className="field-label" htmlFor="membership-price">Price per year</label>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, maxWidth: 220 }}>
            <span aria-hidden="true">$</span>
            <input
              id="membership-price"
              type="number"
              min="1"
              step="1"
              inputMode="decimal"
              value={priceDollars}
              onChange={(e) => setPriceDollars(e.target.value)}
              disabled={!priceLoaded || priceSaving}
            />
          </div>
        </div>
        <button type="button" className="primary-button" onClick={savePrice} disabled={!priceLoaded || priceSaving}>
          {priceSaving ? 'Saving…' : 'Save price'}
        </button>
        {priceMessage?.kind === 'ok' && <div className="card-note" role="status">{priceMessage.text}</div>}
        {priceMessage?.kind === 'error' && <div className="form-error" role="alert">{priceMessage.text}</div>}
      </div>

      <details className="admin-advanced-panel">
        <summary className="admin-advanced-summary">Advanced</summary>
        <div className="admin-tools-grid">
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
            <button type="button" className="primary-button" onClick={runImport} disabled={importState.status === 'running'}>
              {importState.status === 'running' ? 'Importing… (can take up to a minute)' : 'Run import'}
            </button>
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
    </section>
  )
}
