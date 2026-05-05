import { Link } from 'react-router-dom'
import { blogImageUrl, type BlogPostSummary } from '../../api/blog'

interface BlogCardProps {
  post: BlogPostSummary
}

export default function BlogCard({ post }: BlogCardProps) {
  const imgUrl = blogImageUrl(post.featuredImageId)
  const date = post.publishedAt
    ? new Date(post.publishedAt).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
    : null

  return (
    <article className="card blog-card">
      {imgUrl && (
        <Link to={`/blog/${post.slug}`} className="blog-card-image-link">
          <img src={imgUrl} alt="" className="blog-card-image" loading="lazy" />
        </Link>
      )}
      <div className="blog-card-body">
        {post.categoryName && <span className="pill">{post.categoryName}</span>}
        <h3 className="blog-card-title">
          <Link to={`/blog/${post.slug}`}>{post.title}</Link>
        </h3>
        <p className="blog-card-meta">
          <span>{post.authorName}</span>
          {date && post.publishedAt && (
            <> &middot; <time dateTime={post.publishedAt}>{date}</time></>
          )}
        </p>
        {post.excerpt && <p className="blog-card-excerpt">{post.excerpt}</p>}
        <p className="card-note">
          <Link to={`/blog/${post.slug}`} className="footer-link">Read more &rarr;</Link>
        </p>
      </div>
    </article>
  )
}
