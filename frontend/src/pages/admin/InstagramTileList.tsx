import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  deleteInstagramPost,
  fetchAdminInstagramPosts,
  type InstagramPostAdmin,
} from '../../api/instagram'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; posts: InstagramPostAdmin[] }

const PERMALINK_RE = /data-instgrm-permalink="([^"]+)"/i

function extractPermalink(embed: string): string | null {
  const m = embed.match(PERMALINK_RE)
  return m ? m[1] : null
}

export default function InstagramTileList() {
  const navigate = useNavigate()
  const [state, setState] = useState<State>({ status: 'loading' })

  const reload = () => {
    setState({ status: 'loading' })
    fetchAdminInstagramPosts()
      .then((posts) => setState({ status: 'ready', posts }))
      .catch((err: unknown) => setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load' }))
  }

  useEffect(() => { reload() }, [])

  const handleDelete = async (post: InstagramPostAdmin) => {
    const label = extractPermalink(post.embedHtml) ?? `tile #${post.id}`
    if (!confirm(`Remove ${label}? This cannot be undone.`)) return
    try {
      await deleteInstagramPost(post.id)
      reload()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Delete failed')
    }
  }

  return (
    <section className="admin-page" aria-labelledby="instagram-heading">
      <header className="admin-page-header">
        <h1 id="instagram-heading" className="admin-page-title">Instagram tiles</h1>
        <button type="button" className="primary-button" onClick={() => navigate('/admin/instagram/new')}>
          New tile
        </button>
      </header>

      <p className="admin-empty">
        Paste the Embed snippet from each Instagram post (post ⋯ menu → Embed → Copy Embed Code). The first six tiles appear on the homepage; reorder with the display order field.
      </p>

      {state.status === 'loading' && <p className="admin-loading">Loading&hellip;</p>}
      {state.status === 'error' && <div className="form-error" role="alert">{state.message}</div>}
      {state.status === 'ready' && state.posts.length === 0 && (
        <p className="admin-empty">No tiles yet. Click "New tile" to add one.</p>
      )}
      {state.status === 'ready' && state.posts.length > 0 && (
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th scope="col">Permalink</th>
                <th scope="col">Order</th>
                <th scope="col">Updated</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {state.posts.map((p) => {
                const permalink = extractPermalink(p.embedHtml)
                return (
                  <tr key={p.id}>
                    <td>
                      <Link to={`/admin/instagram/${p.id}/edit`}>
                        {permalink ?? `Tile #${p.id}`}
                      </Link>
                    </td>
                    <td>{p.displayOrder}</td>
                    <td>{new Date(p.updatedAt).toLocaleDateString()}</td>
                    <td>
                      {permalink && (
                        <>
                          <a
                            className="footer-link"
                            href={permalink}
                            target="_blank"
                            rel="noopener noreferrer"
                          >
                            View
                          </a>
                          {' · '}
                        </>
                      )}
                      <button type="button" className="footer-link" onClick={() => handleDelete(p)}>Delete</button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
