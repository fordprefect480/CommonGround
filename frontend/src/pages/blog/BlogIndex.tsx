import { useEffect, useState } from 'react'
import { fetchBlogPosts, type BlogPostSummary } from '../../api/blog'
import { useAppConfig } from '../../AppConfigContext'
import { MSFooter, MSHeader, usePageNav } from '../home/Chrome'
import BlogCard from './BlogCard'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; posts: BlogPostSummary[] }

export default function BlogIndex() {
  const { gardenName } = useAppConfig()
  const [state, setState] = useState<State>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false
    fetchBlogPosts()
      .then((result) => { if (!cancelled) setState({ status: 'ready', posts: result.posts }) })
      .catch((err: unknown) => {
        if (!cancelled) {
          setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load posts' })
        }
      })
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    document.title = `Blog | ${gardenName}`
    return () => { document.title = gardenName }
  }, [gardenName])

  const handleNav = usePageNav('blog')

  return (
    <div>
      <MSHeader active="blog" onNav={handleNav} />
      <main className="blog-index-main">
        <header className="blog-index-header">
          <h1 className="section-title">Blog</h1>
          <p className="blog-index-subtitle">News from {gardenName}</p>
        </header>
        <div className="blog-index-grid">
          {state.status === 'loading' && <p className="admin-loading">Loading posts&hellip;</p>}
          {state.status === 'error' && <div className="form-error" role="alert">{state.message}</div>}
          {state.status === 'ready' && state.posts.length === 0 && (
            <p className="admin-empty">No posts yet - check back soon.</p>
          )}
          {state.status === 'ready' && state.posts.map((post) => (
            <BlogCard key={post.id} post={post} />
          ))}
        </div>
      </main>
      <MSFooter onNav={handleNav} />
    </div>
  )
}
