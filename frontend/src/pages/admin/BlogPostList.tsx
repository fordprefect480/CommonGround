import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { blogImageUrl, deleteBlogPost, fetchAdminBlogPosts, type BlogPostAdminListItem } from '../../api/blog'
import BlogEmailModal from './BlogEmailModal'

const STATUS_PUBLISHED = 1

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; posts: BlogPostAdminListItem[] }

export default function BlogPostList() {
  const navigate = useNavigate()
  const [state, setState] = useState<State>({ status: 'loading' })
  const [emailPost, setEmailPost] = useState<BlogPostAdminListItem | null>(null)

  const reload = () => {
    setState({ status: 'loading' })
    fetchAdminBlogPosts()
      .then((posts) => setState({ status: 'ready', posts }))
      .catch((err: unknown) => setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load' }))
  }

  useEffect(() => { reload() }, [])

  const handleDelete = async (post: BlogPostAdminListItem) => {
    if (!confirm(`Delete "${post.title}"? This cannot be undone.`)) return
    try {
      await deleteBlogPost(post.id)
      reload()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Delete failed')
    }
  }

  return (
    <section className="admin-page" aria-labelledby="blog-heading">
      <header className="admin-page-header">
        <h1 id="blog-heading" className="admin-page-title">Blog</h1>
        <button type="button" className="primary-button" onClick={() => navigate('/admin/blog/new')}>
          New post
        </button>
      </header>

      {state.status === 'loading' && <p className="admin-loading">Loading&hellip;</p>}
      {state.status === 'error' && <div className="form-error" role="alert">{state.message}</div>}
      {state.status === 'ready' && state.posts.length === 0 && (
        <p className="admin-empty">No posts yet. Click "New post" to create one.</p>
      )}
      {state.status === 'ready' && state.posts.length > 0 && (
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th scope="col">Image</th>
                <th scope="col">Title</th>
                <th scope="col">Status</th>
                <th scope="col">Author</th>
                <th scope="col">Updated</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {state.posts.map((p) => {
                const published = p.status === STATUS_PUBLISHED
                const thumbUrl = blogImageUrl(p.featuredImageId)
                return (
                  <tr key={p.id}>
                    <td>
                      {thumbUrl
                        ? <img className="blog-thumb" src={thumbUrl} alt="" />
                        : <span className="blog-thumb blog-thumb-empty" aria-hidden="true" />}
                    </td>
                    <td>
                      <Link to={`/admin/blog/${p.id}/edit`}>{p.title}</Link>
                      {p.excerpt && <p className="blog-row-excerpt">{p.excerpt}</p>}
                    </td>
                    <td>
                      <span className={published ? 'pill pill-ok' : 'pill pill-warn'}>
                        {published ? 'Published' : 'Draft'}
                      </span>
                    </td>
                    <td>{p.authorName}</td>
                    <td>{new Date(p.updatedAt).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })}</td>
                    <td>
                      <Link className="footer-link" to={`/admin/blog/${p.id}/edit`}>Edit</Link>
                      {' · '}
                      {published && <a className="footer-link" href={`/blog/${p.slug}`} target="_blank" rel="noopener noreferrer">View</a>}
                      {published && ' · '}
                      {published && <button type="button" className="footer-link" onClick={() => setEmailPost(p)}>Send Email</button>}
                      {published && ' · '}
                      <button type="button" className="footer-link" onClick={() => handleDelete(p)}>Delete</button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {emailPost && (
        <BlogEmailModal
          post={emailPost}
          onClose={() => setEmailPost(null)}
          onSent={(result) => {
            setEmailPost(null)
            navigate(`/admin/email/${result.id}`)
          }}
        />
      )}
    </section>
  )
}
