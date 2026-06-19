import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { blogImageUrl, fetchBlogPost, type BlogPost as BlogPostT } from '../../api/blog'
import Seo, { SITE_URL } from '../../Seo'
import { useAppConfig } from '../../AppConfigContext'
import { MSFooter, MSHeader, usePageNav } from '../home/Chrome'
import BlogCard from './BlogCard'

type State =
  | { status: 'loading' }
  | { status: 'notfound' }
  | { status: 'error'; message: string }
  | { status: 'ready'; post: BlogPostT }

export default function BlogPost() {
  const { slug = '' } = useParams()
  const { gardenName } = useAppConfig()
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

  const handleNav = usePageNav()
  const post = state.status === 'ready' ? state.post : null

  return (
    <div>
      <Seo
        title={post ? post.title : 'Blog'}
        description={post?.excerpt ?? `Read the latest posts from ${gardenName}.`}
        image={post ? blogImageUrl(post.featuredImageId) : undefined}
        type={post ? 'article' : 'website'}
        jsonLd={post ? {
          '@context': 'https://schema.org',
          '@type': 'BlogPosting',
          headline: post.title,
          ...(post.excerpt ? { description: post.excerpt } : {}),
          ...(post.publishedAt ? { datePublished: post.publishedAt } : {}),
          ...(post.categoryName ? { articleSection: post.categoryName } : {}),
          ...(blogImageUrl(post.featuredImageId)
            ? { image: new URL(blogImageUrl(post.featuredImageId)!, SITE_URL).href }
            : {}),
          author: { '@type': 'Person', name: post.authorName },
          publisher: { '@type': 'Organization', name: gardenName },
          mainEntityOfPage: `${SITE_URL}/blog/${post.slug}`,
        } : undefined}
      />
      <MSHeader active="blog" onNav={handleNav} />
      <main className="blog-post-main">
        <PostBody state={state} />
      </main>
      <MSFooter onNav={handleNav} />
    </div>
  )
}

function PostBody({ state }: { state: State }) {
  switch (state.status) {
    case 'loading':
      return <p className="admin-loading">Loading&hellip;</p>
    case 'notfound':
      return (
        <p className="admin-empty">
          That post wasn&apos;t found. <Link to="/blog" className="footer-link">Back to the blog</Link>.
        </p>
      )
    case 'error':
      return <div className="form-error" role="alert">{state.message}</div>
    case 'ready':
      return <PostArticle post={state.post} />
  }
}

function PostArticle({ post }: { post: BlogPostT }) {
  const heroUrl = blogImageUrl(post.featuredImageId)
  const date = post.publishedAt
    ? new Date(post.publishedAt).toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })
    : null

  return (
    <>
      <article className="blog-post">
        {heroUrl && <img src={heroUrl} alt={post.title} className="blog-post-hero" />}
        <header className="blog-post-header">
          {post.categoryName && <span className="pill">{post.categoryName}</span>}
          <h1 className="blog-post-title">{post.title}</h1>
          <p className="blog-post-meta">
            <span>{post.authorName}</span>
            {date && post.publishedAt && (
              <> &middot; <time dateTime={post.publishedAt}>{date}</time></>
            )}
          </p>
        </header>
        <div className="blog-post-body" dangerouslySetInnerHTML={{ __html: post.bodyHtml }} />
      </article>

      {post.morePosts.length > 0 && (
        <section className="blog-related" aria-labelledby="related-heading">
          <h2 id="related-heading" className="section-title">More posts</h2>
          <div className="blog-related-grid">
            {post.morePosts.map((p) => <BlogCard key={p.id} post={p} />)}
          </div>
        </section>
      )}

      <div className="blog-back-link">
        <p className="card-note">
          <Link to="/blog" className="footer-link">&larr; All posts</Link>
        </p>
      </div>
    </>
  )
}
