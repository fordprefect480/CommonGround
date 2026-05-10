export interface ActivityItem {
  id: number
  occurredAt: string
  activityType: string
  actorUserId: string | null
  actorEmail: string | null
  summary: string
  targetType: string | null
  targetId: string | null
  detailsJson: string | null
}

export interface ActivityList {
  items: ActivityItem[]
  nextCursor: string | null
}

export async function fetchActivity(cursor?: string | null, take = 50): Promise<ActivityList> {
  const params = new URLSearchParams()
  if (cursor) params.set('cursor', cursor)
  params.set('take', String(take))
  const res = await fetch(`/api/admin/activity?${params.toString()}`, { credentials: 'include' })
  if (!res.ok) throw new Error(`Failed to load activity (${res.status})`)
  return res.json()
}
