import { useEffect } from 'react'
import { createPortal } from 'react-dom'

interface BlogPreviewModalProps {
  title: string
  bodyHtml: string
  /** Category name to show as a pill, or null when the draft has no category. */
  categoryName: string | null
  onClose: () => void
}

/**
 * Read-only preview of the current draft, shown in a modal. The title, category,
 * and body reuse the public post's classes (`blog-post`, `blog-post-body`, …) so
 * the draft renders the way readers will see it once published. Rendered only
 * while open (the parent conditionally mounts it).
 */
export default function BlogPreviewModal({ title, bodyHtml, categoryName, onClose }: BlogPreviewModalProps) {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [onClose])

  return createPortal(
    <div role="presentation" onClick={onClose} style={overlayStyle}>
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Draft preview"
        className="card"
        onClick={(e) => e.stopPropagation()}
        style={{ width: '100%', maxWidth: 760, margin: 0, maxHeight: '90vh', overflowY: 'auto' }}
      >
        <div className="admin-actions" style={{ justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
          <span className="card-note" style={{ margin: 0 }}>Draft preview</span>
          <button type="button" className="footer-link" onClick={onClose}>Close</button>
        </div>

        <article className="blog-post">
          <header className="blog-post-header">
            {categoryName && <span className="pill">{categoryName}</span>}
            <h1 className="blog-post-title">{title || 'Untitled post'}</h1>
          </header>
          {bodyHtml.trim()
            ? <div className="blog-post-body" dangerouslySetInnerHTML={{ __html: bodyHtml }} />
            : <p className="admin-empty">Nothing to preview yet.</p>}
        </article>
      </div>
    </div>,
    document.body,
  )
}

const overlayStyle: React.CSSProperties = {
  position: 'fixed',
  inset: 0,
  zIndex: 1000,
  display: 'flex',
  alignItems: 'flex-start',
  justifyContent: 'center',
  padding: 20,
  background: 'rgba(20, 20, 16, 0.55)',
  overflowY: 'auto',
}
