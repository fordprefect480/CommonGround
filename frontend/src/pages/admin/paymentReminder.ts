import type { Member } from '../../api/auth'
import { formatPrice } from '../../format'

// Two distinct "unpaid" cohorts an admin can remind:
//  - 'not_renewed': hasn't paid through the upcoming renewal boundary (next
//    1 July). Near end of financial year this includes members still covered
//    for the current year who haven't renewed for the year ahead.
//  - 'lapsed': membership has fully expired (paid-through is in the past).
// Every 'lapsed' member is also 'not_renewed'; the cohorts differ only for
// members covered for the current year but not yet the next.
export type ReminderGroup = 'not_renewed' | 'lapsed'

/** What the Members page hands to the email composer via router state. */
export interface ReminderPreset {
  memberIds: string[]
  subject: string
  bodyHtml: string
  note: string
}

function hasEmail(m: Member): boolean {
  return !!(m.email && m.email.trim().length > 0)
}

function paidThroughMs(m: Member): number | null {
  if (!m.membershipPaidThroughUtc) return null
  const t = new Date(m.membershipPaidThroughUtc).getTime()
  return Number.isNaN(t) ? null : t
}

/** Members with an email who haven't paid through the upcoming renewal boundary. */
export function membersNotRenewed(members: Member[], renewalTargetMs: number): Member[] {
  return members.filter(hasEmail).filter((m) => {
    const paid = paidThroughMs(m)
    return paid === null || paid < renewalTargetMs
  })
}

/** Members with an email whose membership has fully lapsed (expired before now). */
export function membersLapsed(members: Member[], nowMs: number): Member[] {
  return members.filter(hasEmail).filter((m) => {
    const paid = paidThroughMs(m)
    return paid === null || paid < nowMs
  })
}

function plural(n: number, singular: string): string {
  return `${n} ${singular}${n === 1 ? '' : 's'}`
}

/**
 * Builds a starter reminder email for one cohort. The admin reviews and edits
 * everything before sending; this only saves them typing the boilerplate.
 */
export function buildReminderPreset(opts: {
  group: ReminderGroup
  members: Member[]
  gardenName: string
  fyLabel: string
  priceCents: number
  paymentsEnabled: boolean
  membershipUrl: string
}): ReminderPreset {
  const { group, members, gardenName, fyLabel, priceCents, paymentsEnabled, membershipUrl } = opts

  const subject =
    group === 'lapsed'
      ? `Your ${gardenName} membership has lapsed`
      : `Renew your ${gardenName} membership for ${fyLabel}`

  const intro =
    group === 'lapsed'
      ? `Our records show your ${gardenName} membership has lapsed.`
      : `Our records show your ${gardenName} membership hasn't been renewed for the ${fyLabel} financial year yet.`

  const renewLine = paymentsEnabled
    ? `<p>You can renew online for ${formatPrice(priceCents)} here: <a href="${membershipUrl}">${membershipUrl}</a>.</p>`
    : `<p>Visit <a href="${membershipUrl}">${membershipUrl}</a> for renewal details.</p>`

  const bodyHtml =
    `<p>Hi there,</p>` +
    `<p>${intro} Renewing keeps your membership and garden benefits active for the year ahead.</p>` +
    renewLine +
    `<p>If you've already paid, please disregard this reminder &mdash; and thank you for being part of ${gardenName}.</p>`

  const note =
    group === 'lapsed'
      ? `${plural(members.length, 'member')} whose membership has lapsed`
      : `${plural(members.length, 'member')} who haven't paid for ${fyLabel}`

  return { memberIds: members.map((m) => m.id), subject, bodyHtml, note }
}
