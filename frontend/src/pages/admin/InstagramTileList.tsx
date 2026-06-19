import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  DndContext,
  KeyboardSensor,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core'
import {
  SortableContext,
  arrayMove,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import {
  deleteInstagramPost,
  fetchAdminInstagramPosts,
  reorderInstagramPosts,
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
  const [reorderError, setReorderError] = useState<string | null>(null)

  const reload = () => {
    setState({ status: 'loading' })
    fetchAdminInstagramPosts()
      .then((posts) => setState({ status: 'ready', posts }))
      .catch((err: unknown) => setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load' }))
  }

  useEffect(() => { reload() }, [])

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )

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

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event
    if (state.status !== 'ready' || !over || active.id === over.id) return

    const oldIndex = state.posts.findIndex((p) => p.id === active.id)
    const newIndex = state.posts.findIndex((p) => p.id === over.id)
    if (oldIndex === -1 || newIndex === -1) return

    // Optimistically reorder, then persist. On failure, reload to resync.
    const previous = state.posts
    const reordered = arrayMove(state.posts, oldIndex, newIndex)
    setState({ status: 'ready', posts: reordered })
    setReorderError(null)
    try {
      await reorderInstagramPosts(reordered.map((p) => p.id))
    } catch (err) {
      setState({ status: 'ready', posts: previous })
      setReorderError(err instanceof Error ? err.message : 'Could not save the new order')
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
        Paste the Embed snippet from each Instagram post (post ⋯ menu → Embed → Copy Embed Code). The first six tiles appear on the homepage; drag the handle to reorder them.
      </p>

      {reorderError && <div className="form-error" role="alert">{reorderError}</div>}

      {state.status === 'loading' && <p className="admin-loading">Loading&hellip;</p>}
      {state.status === 'error' && <div className="form-error" role="alert">{state.message}</div>}
      {state.status === 'ready' && state.posts.length === 0 && (
        <p className="admin-empty">No tiles yet. Click "New tile" to add one.</p>
      )}
      {state.status === 'ready' && state.posts.length > 0 && (
        <div className="admin-table-wrap">
          <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
            <table className="admin-table">
              <thead>
                <tr>
                  <th scope="col"><span className="visually-hidden">Reorder</span></th>
                  <th scope="col">Permalink</th>
                  <th scope="col">Updated</th>
                  <th scope="col">Actions</th>
                </tr>
              </thead>
              <tbody>
                <SortableContext items={state.posts.map((p) => p.id)} strategy={verticalListSortingStrategy}>
                  {state.posts.map((p) => (
                    <SortableTileRow key={p.id} post={p} onDelete={handleDelete} />
                  ))}
                </SortableContext>
              </tbody>
            </table>
          </DndContext>
        </div>
      )}
    </section>
  )
}

function SortableTileRow({
  post,
  onDelete,
}: {
  post: InstagramPostAdmin
  onDelete: (post: InstagramPostAdmin) => void
}) {
  const permalink = extractPermalink(post.embedHtml)
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: post.id })

  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.6 : undefined,
  }

  return (
    <tr ref={setNodeRef} style={style} className={isDragging ? 'admin-table-row-dragging' : undefined}>
      <td data-label="Reorder">
        <button
          type="button"
          className="admin-drag-handle"
          aria-label={`Reorder ${permalink ?? `tile #${post.id}`}`}
          {...attributes}
          {...listeners}
        >
          ⠿
        </button>
      </td>
      <td data-label="Permalink">
        <Link to={`/admin/instagram/${post.id}/edit`}>
          {permalink ?? `Tile #${post.id}`}
        </Link>
      </td>
      <td data-label="Updated">{new Date(post.updatedAt).toLocaleDateString()}</td>
      <td data-label="Actions">
        <Link className="footer-link" to={`/admin/instagram/${post.id}/edit`}>Edit</Link>
        {' · '}
        <button type="button" className="footer-link" onClick={() => onDelete(post)}>Delete</button>
      </td>
    </tr>
  )
}
