const ACTIVITY_LABELS: Record<string, string> = {
  'member.created': 'Member created',
  'member.updated': 'Member updated',
  'member.profile_updated': 'Profile updated',
  'blog.post_created': 'Blog post created',
  'blog.post_updated': 'Blog post updated',
  'blog.post_deleted': 'Blog post deleted',
  'email.test_sent': 'Test email sent',
  'email.newsletter_sent': 'Newsletter sent',
  'tool.blog_import_run': 'Blog import run',
  'tool.orphan_cleanup_run': 'Orphan image cleanup',
  'tool.members_exported': 'Members exported',
}

const absoluteFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'short',
})

const relativeFormatter = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' })

export function formatRelative(iso: string): string {
  const then = new Date(iso).getTime()
  if (Number.isNaN(then)) return '-'
  const diffSeconds = Math.round((then - Date.now()) / 1000)
  const abs = Math.abs(diffSeconds)
  if (abs < 60) return relativeFormatter.format(diffSeconds, 'second')
  if (abs < 3600) return relativeFormatter.format(Math.round(diffSeconds / 60), 'minute')
  if (abs < 86400) return relativeFormatter.format(Math.round(diffSeconds / 3600), 'hour')
  if (abs < 86400 * 30) return relativeFormatter.format(Math.round(diffSeconds / 86400), 'day')
  return absoluteFormatter.format(new Date(iso))
}

export function formatAbsolute(iso: string): string {
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '-' : absoluteFormatter.format(d)
}

export function labelFor(activityType: string): string {
  return ACTIVITY_LABELS[activityType] ?? activityType
}

export function prettifyDetails(detailsJson: string): string {
  try {
    return JSON.stringify(JSON.parse(detailsJson), null, 2)
  } catch {
    return detailsJson
  }
}
