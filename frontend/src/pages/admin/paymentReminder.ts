import { formatPrice } from '../../format'

/** Starter subject + body for a membership payment reminder. */
export interface ReminderDraft {
  subject: string
  bodyHtml: string
}

/**
 * Builds a starter reminder email. The admin reviews and edits everything in the
 * modal before sending; this only saves them typing the boilerplate.
 */
export function buildReminderDraft(opts: {
  gardenName: string
  fyLabel: string
  priceCents: number
  paymentsEnabled: boolean
  membershipUrl: string
}): ReminderDraft {
  const { gardenName, fyLabel, priceCents, paymentsEnabled, membershipUrl } = opts

  const renewLine = paymentsEnabled
    ? `<p>You can renew online for ${formatPrice(priceCents)} here: <a href="${membershipUrl}">${membershipUrl}</a>.</p>`
    : `<p>Visit <a href="${membershipUrl}">${membershipUrl}</a> for renewal details.</p>`

  const bodyHtml =
    `<p>Hi there,</p>` +
    `<p>Our records show your ${gardenName} membership hasn't been renewed for the ${fyLabel} financial year yet. ` +
    `Renewing keeps your membership and garden benefits active for the year ahead.</p>` +
    renewLine +
    `<p>If you've already paid, please disregard this reminder &mdash; and thank you for being part of ${gardenName}.</p>`

  return {
    subject: `Renew your ${gardenName} membership for ${fyLabel}`,
    bodyHtml,
  }
}
