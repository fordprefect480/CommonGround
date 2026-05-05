import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { blogImageUrl, fetchBlogPost, type BlogPost as BlogPostT } from '../../api/blog'
import { MSFooter, MSHeader, type NavId } from '../home/Chrome'
import BlogCard from './BlogCard'

type State =
  | { status: 'loading' }
  | { status: 'notfound' }
  | { status: 'error'; message: string }
  | { status: 'ready'; post: BlogPostT }

export default function BlogPost() {
  const { slug = '' } = useParams()
  const navigate = useNavigate()
  const [state, setState] = useState<State>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false
    fetchBlogPost(slug)
      .then((post) => {
        if (cancelled) return
        setState(post ? { status: 'ready', post } : { status: 'notfound' })
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load post' })
        }
      })
    return () => { cancelled = true }
  }, [slug])

  const handleNav = useCallback((id: NavId) => {
    if (id === 'blog') {
      navigate('/blog')
      return
    }
    navigate('/')
  }, [navigate])

  const renderContent = () => {
    if (state.status === 'loading') {
      return (
        <main className="blog-post-main">
          <p className="admin-loading">Loading&hellip;</p>
        </main>
      )
    }
    if (state.status === 'notfound') {
      return (
        <main className="blog-post-main">
          <p className="admin-empty">
            That post wasn&apos;t found. <Link to="/blog" className="footer-link">Back to the blog</Link>.
          </p>
        </main>
      )
    }
    if (state.status === 'error') {
      return (
        <main className="blog-post-main">
          <div className="form-error" role="alert">{state.message}</div>
        </main>
      )
    }

    const { post } = state
    const heroUrl = blogImageUrl(post.featuredImageId)
    const date = post.publishedAt
      ? new Date(post.publishedAt).toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })
      : null

    return (
      <main className="blog-post-main">
        <article className="blog-post">
          {heroUrl && <img src={heroUrl} alt="" className="blog-post-hero" />}
          <header className="blog-post-header">
            {post.categoryName && <span className="pill">{post.categoryName}</span>}
            <h1 className="blog-post-title">{post.title}</h1>
            <p className="blog-post-meta">
              <span>{post.authorName}</span>
              {date && <> &middot; <time>{date}</time></>}
            </p>
          </header>
          <div className="blog-post-body" dangerouslySetInnerHTML={{ __html: post.bodyHtml }} />
        </article>

        {post.relatedPosts.length > 0 && (
          <section className="blog-related" aria-labelledby="related-heading">
            <h2 id="related-heading" className="section-title">More posts</h2>
            <div className="blog-related-grid">
              {post.relatedPosts.map((p) => <BlogCard key={p.id} post={p} />)}
            </div>
          </section>
        )}

        <div className="blog-back-link">
          <p className="card-note">
            <Link to="/blog" className="footer-link">&larr; All posts</Link>
          </p>
        </div>
      </main>
    )
  }

  return (
    <div>
      <MSHeader active="blog" onNav={handleNav} />
      {renderContent()}
      <MSFooter onNav={handleNav} />
    </div>
  )
}
