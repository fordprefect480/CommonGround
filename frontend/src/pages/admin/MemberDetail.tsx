import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { fetchMember, fetchMembershipRenewalTarget, recordMembershipPayment, updateMember, type Member } from '../../api/auth'
import { fetchMemberEmails, type MemberEmailListItem } from '../../api/email'
import { fetchLeasedBeds, recordLeasePayment, type AdminBed, type BedLeaseStatus } from '../../api/leasedBeds'
import { useAppConfig } from '../../AppConfigContext'
import { financialYearLabel, formatPrice, membershipPaidThroughFyLabel } from '../../format'
import { formatAbsolute, formatRelative } from './activityFormatting'
import PaymentHistoryTable from './PaymentHistoryTable'
import RecordPaymentModal from './RecordPaymentModal'
import AdminBackButton from './AdminBackButton'

const ADMIN_ROLE = 'Admin'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; member: Member }

interface FormState {
  firstName: string
  lastName: string
  phoneNumber: string
  isAdmin: boolean
  isSubscribedToMailingList: boolean
}

const dateFormatter = new Intl.DateTimeFormat('en-AU', {
  day: 'numeric',
  month: 'long',
  year: 'numeric',
})

function formatJoinedAt(iso: string): string {
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '-' : dateFormatter.format(d)
}

function MembershipCard({ memberId, paidThrough, onRecorded }: { memberId: string; paidThrough: string | null; onRecorded: (m: Member) => void }) {
  const { membershipPriceCents } = useAppConfig()
  const now = new Date()
  const [renewalTargetUtc, setRenewalTargetUtc] = useState<string | null>(null)

  // "Paid" means covered through the upcoming renewal boundary (the next 1 July,
  // with the late-join carry-over), matching the Members list. Until that date
  // loads, fall back to "covered as of now".
  useEffect(() => {
    let active = true
    fetchMembershipRenewalTarget()
      .then((iso) => { if (active) setRenewalTargetUtc(iso) })
      .catch(() => { /* keep the now-based fallback */ })
    return () => { active = false }
  }, [])

  const threshold = renewalTargetUtc ? new Date(renewalTargetUtc).getTime() : now.getTime()
  const isPaid = paidThrough != null && new Date(paidThrough).getTime() >= threshold
  // When paid, label the FY the membership runs to; otherwise the upcoming FY
  // they need to renew for (falling back to the current FY before it loads).
  const fyLabel = isPaid
    ? membershipPaidThroughFyLabel(paidThrough!)
    : renewalTargetUtc
      ? membershipPaidThroughFyLabel(renewalTargetUtc)
      : financialYearLabel(now)
  const [modalOpen, setModalOpen] = useState(false)

  return (
    <section className="card admin-form">
      <h2 className="section-title">Membership</h2>

      <p className="card-note" style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 8 }}>
        <span className={isPaid ? 'pill pill-ok' : 'pill pill-warn'} style={{ whiteSpace: 'nowrap', flexShrink: 0 }}>
          {isPaid ? 'Paid' : 'Not yet paid'}
        </span>
        <span>for the {fyLabel} financial year</span>
      </p>

      {isPaid ? (
        <p className="card-note" style={{ margin: 0 }}>
          Membership is valid until {dateFormatter.format(new Date(paidThrough!))}.
        </p>
      ) : (
        paidThrough && (
          <p className="card-note" style={{ margin: 0 }}>
            {new Date(paidThrough).getTime() >= now.getTime()
              ? `Currently covered until ${dateFormatter.format(new Date(paidThrough))}, not yet renewed for the year ahead.`
              : `Last membership ran until ${dateFormatter.format(new Date(paidThrough))}.`}
          </p>
        )
      )}

      <div className="admin-actions">
        <button type="button" className="secondary-button" onClick={() => setModalOpen(true)}>
          Record a manual payment
        </button>
      </div>

      <RecordPaymentModal
        open={modalOpen}
        title="Record a manual payment"
        description="Record a cash or bank-transfer membership payment. The renewal date is set automatically."
        initialAmountCents={membershipPriceCents}
        onClose={() => setModalOpen(false)}
        onConfirm={async (amountCents) => { onRecorded(await recordMembershipPayment(memberId, amountCents)) }}
      />
    </section>
  )
}

function leaseStatusLabel(status: BedLeaseStatus): string {
  switch (status) {
    case 'AwaitingPayment':
      return 'Awaiting payment'
    case 'Active':
      return 'Active'
    case 'Expired':
      return 'Expired'
    case 'Released':
      return 'Released'
  }
}

function LeasedBedsCard({ memberId }: { memberId: string }) {
  const [beds, setBeds] = useState<AdminBed[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [paymentBed, setPaymentBed] = useState<AdminBed | null>(null)

  const reload = async () => {
    const overview = await fetchLeasedBeds()
    setBeds(overview.beds.filter((b) => b.currentLease?.memberId === memberId))
  }

  useEffect(() => {
    let cancelled = false
    fetchLeasedBeds()
      .then((overview) => {
        if (!cancelled) setBeds(overview.beds.filter((b) => b.currentLease?.memberId === memberId))
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Could not load leased beds.')
      })
    return () => { cancelled = true }
  }, [memberId])

  return (
    <section className="card admin-form">
      <h2 className="section-title">Leased beds</h2>

      {error && <div className="form-error" role="alert">{error}</div>}
      {!error && beds === null && <p className="card-note" style={{ margin: 0 }}>Loading&hellip;</p>}
      {beds !== null && beds.length === 0 && <p className="card-note" style={{ margin: 0 }}>No leased bed.</p>}

      {beds?.map((bed) => {
        const lease = bed.currentLease!
        return (
          <p key={bed.id} className="card-note" style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 8 }}>
            <span className={lease.isPaid ? 'pill pill-ok' : 'pill pill-warn'} style={{ whiteSpace: 'nowrap', flexShrink: 0 }}>
              Bed {bed.label}
            </span>
            <span>
              {leaseStatusLabel(lease.status)} · {formatPrice(lease.priceAtAllocationCents)} · expires {dateFormatter.format(new Date(lease.expiresOn))}
            </span>
          </p>
        )
      })}

      {beds && beds.length > 0 && (
        <div className="admin-actions">
          {beds.map((bed) => (
            <button key={bed.id} type="button" className="secondary-button" onClick={() => setPaymentBed(bed)}>
              Record a manual payment{beds.length > 1 ? ` (bed ${bed.label})` : ''}
            </button>
          ))}
        </div>
      )}

      <RecordPaymentModal
        open={paymentBed !== null}
        title="Record a manual payment"
        description={paymentBed ? `Record a cash or bank-transfer payment for bed ${paymentBed.label}.` : undefined}
        initialAmountCents={paymentBed?.currentLease?.priceAtAllocationCents ?? 0}
        onClose={() => setPaymentBed(null)}
        onConfirm={async (amountCents) => {
          if (!paymentBed?.currentLease) return
          await recordLeasePayment(paymentBed.currentLease.leaseId, amountCents)
          await reload()
        }}
      />
    </section>
  )
}

function EmailHistoryCard({ memberId }: { memberId: string }) {
  const [emails, setEmails] = useState<MemberEmailListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    fetchMemberEmails(memberId)
      .then((items) => { if (!cancelled) setEmails(items) })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Could not load email history.')
      })
    return () => { cancelled = true }
  }, [memberId])

  return (
    <section className="card admin-form">
      <h2 className="section-title">Email history</h2>

      {error && <div className="form-error" role="alert">{error}</div>}
      {!error && emails === null && <p className="card-note" style={{ margin: 0 }}>Loading&hellip;</p>}
      {emails !== null && emails.length === 0 && (
        <p className="card-note" style={{ margin: 0 }}>No emails have been sent to this member.</p>
      )}

      {emails !== null && emails.length > 0 && (
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th scope="col">Sent</th>
                <th scope="col">Subject</th>
                <th scope="col">From</th>
                <th scope="col">Status</th>
              </tr>
            </thead>
            <tbody>
              {emails.map((email) => (
                <tr key={email.id}>
                  <td data-label="Sent" title={formatAbsolute(email.sentAt)}>{formatRelative(email.sentAt)}</td>
                  <td data-label="Subject"><Link to={`/admin/email/${email.id}`}>{email.subject}</Link></td>
                  <td data-label="From">{email.senderEmail ?? '-'}</td>
                  <td data-label="Status">
                    {email.status === 'failed' ? (
                      <span className="pill pill-warn" title={email.errorMessage ?? undefined}>Failed</span>
                    ) : (
                      <span className="pill pill-ok">Delivered</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

function memberToForm(member: Member): FormState {
  return {
    firstName: member.firstName ?? '',
    lastName: member.lastName ?? '',
    phoneNumber: member.phoneNumber ?? '',
    isAdmin: member.roles.includes(ADMIN_ROLE),
    isSubscribedToMailingList: member.isSubscribedToMailingList,
  }
}

export default function MemberDetail() {
  const { id } = useParams<{ id: string }>()
  const [state, setState] = useState<State>({ status: 'loading' })
  const [form, setForm] = useState<FormState | null>(null)
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [paymentsRefresh, setPaymentsRefresh] = useState(0)

  useEffect(() => {
    if (!id) return
    setState({ status: 'loading' })
    fetchMember(id)
      .then((member) => {
        setState({ status: 'ready', member })
        setForm(memberToForm(member))
      })
      .catch((err: unknown) => {
        setState({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load member' })
      })
  }, [id])

  if (!id) return <p className="form-error" role="alert">Missing member id.</p>

  if (state.status === 'loading') return <p className="admin-loading">Loading&hellip;</p>

  if (state.status === 'error') {
    return (
      <section className="admin-page" aria-labelledby="member-heading">
        <header className="admin-page-header">
          <div className="admin-page-heading-group">
            <AdminBackButton to="/admin/members" label="Back to members" />
            <h1 id="member-heading" className="admin-page-title">Member</h1>
          </div>
        </header>
        <div className="form-error" role="alert">{state.message}</div>
      </section>
    )
  }

  const { member } = state
  if (!form) return null

  const updateForm = (patch: Partial<FormState>) => {
    setForm((prev) => (prev ? { ...prev, ...patch } : prev))
    if (saved) setSaved(false)
  }

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setSaveError(null)
    try {
      const updated = await updateMember(member.id, {
        firstName: form.firstName.trim() || null,
        lastName: form.lastName.trim() || null,
        phoneNumber: form.phoneNumber.trim() || null,
        isAdmin: form.isAdmin,
        isSubscribedToMailingList: form.isSubscribedToMailingList,
      })
      setState({ status: 'ready', member: updated })
      setForm(memberToForm(updated))
      setSaved(true)
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : 'Save failed')
    } finally {
      setSaving(false)
    }
  }

  const heading = member.displayName ?? member.email ?? 'Member'

  return (
    <section className="admin-page" aria-labelledby="member-heading">
      <header className="admin-page-header">
        <div className="admin-page-heading-group">
          <AdminBackButton to="/admin/members" label="Back to members" />
          <h1 id="member-heading" className="admin-page-title">{heading}</h1>
        </div>
      </header>

      <form className="card admin-form" onSubmit={submit}>
        {saveError && <div className="form-error" role="alert">{saveError}</div>}

        <div className="field">
          <span className="field-label">Email</span>
          <span className="field-readonly">{member.email ?? '-'}</span>
        </div>

        <div className="field">
          <span className="field-label">Member since</span>
          <span className="field-readonly">{formatJoinedAt(member.joinedAt)}</span>
        </div>

        <label className="field">
          <span className="field-label">First name</span>
          <input
            value={form.firstName}
            onChange={(e) => updateForm({ firstName: e.target.value })}
            maxLength={100}
          />
        </label>

        <label className="field">
          <span className="field-label">Last name</span>
          <input
            value={form.lastName}
            onChange={(e) => updateForm({ lastName: e.target.value })}
            maxLength={100}
          />
        </label>

        <label className="field">
          <span className="field-label">Phone</span>
          <input
            type="tel"
            value={form.phoneNumber}
            onChange={(e) => updateForm({ phoneNumber: e.target.value })}
          />
        </label>

        <label className="checkbox-field">
          <input
            type="checkbox"
            checked={form.isAdmin}
            onChange={(e) => updateForm({ isAdmin: e.target.checked })}
          />
          <span>Administrator</span>
        </label>

        <label className="checkbox-field">
          <input
            type="checkbox"
            checked={form.isSubscribedToMailingList}
            onChange={(e) => updateForm({ isSubscribedToMailingList: e.target.checked })}
          />
          <span>Subscribed to newsletter</span>
        </label>

        <div className="admin-actions">
          <button type="submit" className="primary-button" disabled={saving}>
            {saving ? 'Saving…' : 'Save'}
          </button>
          {saved && <span className="admin-whoami">Saved.</span>}
        </div>
      </form>

      <div className="profile-forms">
        <MembershipCard
          memberId={member.id}
          paidThrough={member.membershipPaidThroughUtc}
          onRecorded={(updated) => { setState({ status: 'ready', member: updated }); setForm(memberToForm(updated)); setPaymentsRefresh((n) => n + 1) }}
        />

        <LeasedBedsCard memberId={member.id} />
      </div>

      <section className="card admin-form">
        <h2 className="section-title">Payment history</h2>
        <PaymentHistoryTable memberId={member.id} refreshToken={paymentsRefresh} />
      </section>

      <EmailHistoryCard memberId={member.id} />
    </section>
  )
}
