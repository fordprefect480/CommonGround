const ACTIVITY_LABELS: Record<string, string> = {
  'member.created': 'Member created',
  'member.updated': 'Member updated',
  'member.profile_updated': 'Profile updated',
  'member.signup_started': 'Membership signup started',
  'member.signup_unpaid': 'Membership signup (unpaid)',
  'member.joined': 'Member joined',
  'blog.post_created': 'Blog post created',
  'blog.post_updated': 'Blog post updated',
  'blog.post_deleted': 'Blog post deleted',
  'event.created': 'Event created',
  'event.updated': 'Event updated',
  'event.deleted': 'Event deleted',
  'instagram.post_created': 'Instagram tile created',
  'instagram.post_updated': 'Instagram tile updated',
  'instagram.post_deleted': 'Instagram tile deleted',
  'instagram.posts_reordered': 'Instagram tiles reordered',
  'email.test_sent': 'Test email sent',
  'email.newsletter_sent': 'Newsletter sent',
  'email.unsubscribed': 'Unsubscribed from mailing list',
  'mailinglist.subscribed': 'Subscribed to mailing list',
  'contact.submitted': 'Contact form submitted',
  'tool.blog_import_run': 'Blog import run',
  'tool.orphan_cleanup_run': 'Orphan image cleanup',
  'tool.members_exported': 'Members exported',
  'settings.membership_price.updated': 'Membership price updated',
  'settings.leased_bed_price.updated': 'Leased bed price updated',
  'settings.coming_soon.updated': 'Under-construction page toggled',
  'member.payment_recorded': 'Membership payment recorded',
  'leased_bed.added': 'Leased bed added',
  'leased_bed.updated': 'Leased bed updated',
  'leased_bed.deleted': 'Leased bed deleted',
  'leased_bed.requested': 'Bed application submitted',
  'leased_bed.request_withdrawn': 'Bed request withdrawn',
  'leased_bed.request_removed': 'Bed request removed',
  'leased_bed.assigned': 'Bed assigned',
  'leased_bed.released': 'Bed released',
  'leased_bed.paid': 'Bed payment received',
  'leased_bed.payment_recorded': 'Bed payment recorded',
  'leased_bed.payment_orphaned': 'Bed payment needs review',
  'leased_bed.renewed': 'Bed lease renewed',
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
