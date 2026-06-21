import { useEffect, useState } from 'react'
import {
  addBed,
  deleteBed,
  fetchLeasedBeds,
  updateBed,
  type AdminBed,
  type LeasedBedsOverview,
} from '../../api/leasedBeds'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; overview: LeasedBedsOverview }

export default function LeasedBedInventory() {
  const [state, setState] = useState<State>({ status: 'loading' })
  const [section, setSection] = useState<'N' | 'S'>('N')
  const [label, setLabel] = useState('')
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState<{ kind: 'ok' | 'error'; text: string } | null>(null)

  useEffect(() => {
    fetchLeasedBeds()
      .then((overview) => setState({ status: 'ready', overview }))
      .catch((err: unknown) =>
        setState({ status: 'error', message: err instanceof Error ? err.message : 'Could not load beds' }),
      )
  }, [])

  const apply = (overview: LeasedBedsOverview) => setState({ status: 'ready', overview })

  const handleAdd = async () => {
    setBusy(true)
    setMessage(null)
    try {
      apply(await addBed({ section, label: label.trim() || null }))
      setLabel('')
      setMessage({ kind: 'ok', text: `Added a bed to section ${section}.` })
    } catch (err) {
      setMessage({ kind: 'error', text: err instanceof Error ? err.message : 'Could not add the bed.' })
    } finally {
      setBusy(false)
    }
  }

  const handleToggleService = async (bed: AdminBed) => {
    setBusy(true)
    setMessage(null)
    try {
      apply(await updateBed(bed.id, { isActive: !bed.isActive }))
    } catch (err) {
      setMessage({ kind: 'error', text: err instanceof Error ? err.message : 'Could not update the bed.' })
    } finally {
      setBusy(false)
    }
  }

  const handleDelete = async (bed: AdminBed) => {
    if (!confirm(`Delete bed ${bed.code}? This is only possible if it has never been leased.`)) return
    setBusy(true)
    setMessage(null)
    try {
      apply(await deleteBed(bed.id))
      setMessage({ kind: 'ok', text: `Deleted bed ${bed.code}.` })
    } catch (err) {
      setMessage({ kind: 'error', text: err instanceof Error ? err.message : 'Could not delete the bed.' })
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="card">
      <h2 className="section-title">Leased beds</h2>
      <p className="card-note">
        The total number of beds is however many are in service. Add a bed, or take one out of service when
        it&rsquo;s no longer available. A bed that&rsquo;s currently leased must be released first.
      </p>

      {state.status === 'loading' && <p className="admin-loading">Loading&hellip;</p>}
      {state.status === 'error' && <div className="form-error" role="alert">{state.message}</div>}

      {state.status === 'ready' && (
        <>
          <p className="card-note">
            <strong>{state.overview.beds.length}</strong> beds &middot;{' '}
            <strong>{state.overview.capacity.total}</strong> in service
          </p>

          <div className="field">
            <label className="field-label" htmlFor="add-bed-section">Add a bed</label>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
              <select
                id="add-bed-section"
                value={section}
                onChange={(e) => setSection(e.target.value as 'N' | 'S')}
                disabled={busy}
              >
                <option value="N">North</option>
                <option value="S">South</option>
              </select>
              <input
                type="text"
                placeholder="Label (optional)"
                value={label}
                onChange={(e) => setLabel(e.target.value)}
                disabled={busy}
                style={{ maxWidth: 200 }}
              />
              <button type="button" className="primary-button" onClick={handleAdd} disabled={busy}>
                Add bed
              </button>
            </div>
          </div>

          {message?.kind === 'ok' && <div className="card-note" role="status">{message.text}</div>}
          {message?.kind === 'error' && <div className="form-error" role="alert">{message.text}</div>}

          <div className="admin-table-wrap">
            <table className="admin-table">
              <thead>
                <tr>
                  <th scope="col">Bed</th>
                  <th scope="col">Status</th>
                  <th scope="col">Actions</th>
                </tr>
              </thead>
              <tbody>
                {state.overview.beds.map((bed) => (
                  <tr key={bed.id} className={bed.isActive ? undefined : 'admin-table-row-muted'}>
                    <td data-label="Bed">
                      <strong>{bed.code}</strong>
                      {bed.label ? <> &middot; {bed.label}</> : null}
                    </td>
                    <td data-label="Status">
                      {bed.isActive
                        ? bed.isOccupied
                          ? <span className="pill pill-ok">Leased</span>
                          : <span className="pill pill-ok">In service</span>
                        : <span className="pill">Out of service</span>}
                    </td>
                    <td data-label="Actions">
                      <button
                        type="button"
                        className="footer-link"
                        onClick={() => handleToggleService(bed)}
                        disabled={busy || (bed.isActive && bed.isOccupied)}
                        title={bed.isActive && bed.isOccupied ? 'Release the lease before taking this bed out of service.' : undefined}
                      >
                        {bed.isActive ? 'Take out of service' : 'Return to service'}
                      </button>
                      {!bed.isOccupied && (
                        <>
                          {' · '}
                          <button type="button" className="footer-link" onClick={() => handleDelete(bed)} disabled={busy}>
                            Delete
                          </button>
                        </>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  )
}
