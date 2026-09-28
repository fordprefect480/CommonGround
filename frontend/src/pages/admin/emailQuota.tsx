import { useEffect, useState } from 'react'
import { fetchEmailQuota, type EmailQuota, type RecipientStatus } from '../../api/email'

/**
 * Loads the daily sending allowance. Failures resolve to null: the allowance is
 * informational only and sending still works (the server queues what won't fit).
 */
export function useEmailQuota(enabled = true): EmailQuota | null {
  const [quota, setQuota] = useState<EmailQuota | null>(null)

  useEffect(() => {
    if (!enabled) return
    let cancelled = false
    fetchEmailQuota()
      .then((q) => { if (!cancelled) setQuota(q) })
      .catch(() => { if (!cancelled) setQuota(null) })
    return () => { cancelled = true }
  }, [enabled])

  return quota
}

const timeFormatter = new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' })
const dayFormatter = new Intl.DateTimeFormat(undefined, { weekday: 'long', day: 'numeric', month: 'long' })

function startOfDay(d: Date): number {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime()
}

/** "around 9:15 am tomorrow" - the queue worker runs every 15 minutes, hence "around". */
export function describeNextBatch(iso: string | null): string {
  if (!iso) return 'shortly'
  const at = new Date(iso)
  const dayDiff = Math.round((startOfDay(at) - startOfDay(new Date())) / 86_400_000)
  const time = timeFormatter.format(at)
  if (dayDiff <= 0) return `around ${time} today`
  if (dayDiff === 1) return `around ${time} tomorrow`
  return `around ${time} on ${dayFormatter.format(at)}`
}

export interface SendPlan {
  sendNow: number
  sendLater: number
  /** Days the queue needs, beyond today, to clear everything including this send. */
  extraDays: number
}

/**
 * Mirrors the server: queued recipients go out oldest email first, so a new send
 * only gets whatever allowance is left after emails already waiting.
 */
export function planSend(recipientCount: number, quota: EmailQuota): SendPlan {
  const waitingAhead = quota.queuedTotal
  const sendNow = Math.max(0, Math.min(recipientCount, quota.bulkRemaining - waitingAhead))
  const sendLater = recipientCount - sendNow
  const bulkPerDay = Math.max(0, quota.dailyLimit - quota.transactionalReserve)
  const backlogAfterToday = Math.max(0, waitingAhead + recipientCount - quota.bulkRemaining)
  const extraDays = bulkPerDay > 0 ? Math.ceil(backlogAfterToday / bulkPerDay) : 0
  return { sendNow, sendLater, extraDays }
}

function laterPhrase(extraDays: number): string {
  return extraDays <= 1 ? 'tomorrow' : `over the next ${extraDays} days`
}

/** Plain-language heads-up shown before sending when not everyone fits in today's allowance. */
export function SendPlanNote({ recipientCount, quota }: { recipientCount: number; quota: EmailQuota | null }) {
  if (!quota || recipientCount === 0) return null
  const { sendNow, sendLater, extraDays } = planSend(recipientCount, quota)
  if (sendLater === 0) return null

  return (
    <p className="card-note" role="note" style={{ fontStyle: 'normal' }}>
      Our email service lets us send {quota.dailyLimit} emails a day.{' '}
      {sendNow > 0 ? (
        <>
          <strong>{sendNow}</strong> will be sent now and the other <strong>{sendLater}</strong> will be sent
          automatically {laterPhrase(extraDays)}.
        </>
      ) : (
        <>
          Today&rsquo;s allowance is used up, so all <strong>{sendLater}</strong> will be sent automatically,
          starting {describeNextBatch(quota.nextAllowanceAtUtc)}.
        </>
      )}{' '}
      You don&rsquo;t need to do anything.
    </p>
  )
}

/** "Emails sent in the last 24 hours: 34 of 100", plus how many are still waiting to go out. */
export function EmailAllowanceSummary({ quota }: { quota: EmailQuota | null }) {
  if (!quota) return null
  return (
    <p className="card-note" style={{ fontStyle: 'normal' }}>
      Emails sent in the last 24 hours: <strong>{quota.sentLast24Hours} of {quota.dailyLimit}</strong>.
      {quota.queuedTotal > 0 && (
        <> {quota.queuedTotal} more {quota.queuedTotal === 1 ? 'is' : 'are'} waiting and will be sent automatically, starting {describeNextBatch(quota.nextAllowanceAtUtc)}.</>
      )}
    </p>
  )
}

const RECIPIENT_STATUS_PILL: Record<RecipientStatus, { className: string; label: string }> = {
  sent: { className: 'pill pill-ok', label: 'Sent' },
  failed: { className: 'pill pill-warn', label: 'Failed' },
  queued: { className: 'pill', label: 'Waiting' },
  skipped: { className: 'pill', label: 'Skipped' },
}

export function RecipientStatusPill({ status, title }: { status: RecipientStatus; title?: string }) {
  const pill = RECIPIENT_STATUS_PILL[status]
  return <span className={pill.className} title={title}>{pill.label}</span>
}
