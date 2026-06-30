export interface SendNewsletterResult {
  id: number
  sent: number
  failed: number
}

export interface SentEmailListItem {
  id: number
  sentAt: string
  subject: string
  senderEmail: string | null
  isNewsletter: boolean
  recipientEmail: string | null
  recipientCount: number
}

export interface SentEmailRecipient {
  id: number
  userId: string | null
  email: string
  status: 'sent' | 'failed'
  errorMessage: string | null
}

export interface SentEmailDetail {
  id: number
  sentAt: string
  subject: string
  htmlBody: string
  textBody: string
  senderUserId: string | null
  senderEmail: string | null
  isNewsletter: boolean
  recipientCount: number
  sentCount: number
  failedCount: number
  recipients: SentEmailRecipient[]
}

export interface SendTestEmailResult {
  sent: number
  failed: number
  failures: { email: string; error: string }[]
}

export async function sendTestEmail(
  subject: string,
  htmlBody: string,
  recipients: string[],
): Promise<SendTestEmailResult> {
  const res = await fetch('/api/admin/tools/email/test', {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ subject, htmlBody, recipients }),
  })
  if (!res.ok) throw new Error(await res.text())
  return res.json()
}

export async function fetchEmailTemplate(isNewsletter: boolean): Promise<string> {
  const res = await fetch(`/api/admin/tools/email/template?isNewsletter=${isNewsletter}`, {
    credentials: 'include',
  })
  if (!res.ok) throw new Error(await res.text())
  const body = (await res.json()) as { htmlBody: string }
  return body.htmlBody
}

export async function fetchSubscriberCount(): Promise<number> {
  const res = await fetch('/api/admin/tools/email/subscribers/count', {
    credentials: 'include',
  })
  if (!res.ok) throw new Error(await res.text())
  const body = (await res.json()) as { count: number }
  return body.count
}

export type NewsletterRecipients =
  | { mode: 'all_subscribers' }
  | { mode: 'specific_members'; memberIds: string[] }
  | { mode: 'custom_emails'; emails: string[] }

export async function sendNewsletter(
  subject: string,
  htmlBody: string,
  recipients: NewsletterRecipients,
  isNewsletter: boolean,
): Promise<SendNewsletterResult> {
  const payload: Record<string, unknown> = { subject, htmlBody, mode: recipients.mode, isNewsletter }
  if (recipients.mode === 'specific_members') payload.memberIds = recipients.memberIds
  if (recipients.mode === 'custom_emails') payload.emails = recipients.emails
  const res = await fetch('/api/admin/tools/email/newsletter', {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  if (!res.ok) throw new Error(await res.text())
  return res.json()
}

export async function fetchSentEmails(): Promise<SentEmailListItem[]> {
  const res = await fetch('/api/admin/tools/email/sent', { credentials: 'include' })
  if (!res.ok) throw new Error(await res.text())
  const body = (await res.json()) as { items: SentEmailListItem[] }
  return body.items
}

export async function fetchSentEmail(id: number): Promise<SentEmailDetail> {
  const res = await fetch(`/api/admin/tools/email/sent/${id}`, { credentials: 'include' })
  if (!res.ok) throw new Error(await res.text())
  return res.json()
}

export interface MemberEmailListItem {
  id: number
  sentAt: string
  subject: string
  senderEmail: string | null
  isNewsletter: boolean
  email: string
  status: 'sent' | 'failed'
  errorMessage: string | null
}

export async function fetchMemberEmails(memberId: string): Promise<MemberEmailListItem[]> {
  const res = await fetch(`/api/admin/members/${encodeURIComponent(memberId)}/emails`, {
    credentials: 'include',
  })
  if (!res.ok) throw new Error(await res.text())
  const body = (await res.json()) as { items: MemberEmailListItem[] }
  return body.items
}
