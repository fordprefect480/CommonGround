export type BedLeaseStatus = 'AwaitingPayment' | 'Active' | 'Expired' | 'Released'

export interface CapacitySummary {
  total: number
  leased: number
  remaining: number
  isFull: boolean
}

export interface AdminBedAllocation {
  leaseId: number
  memberId: string
  memberName: string | null
  startDate: string
  expiresOn: string
  status: BedLeaseStatus
  priceAtAllocationCents: number
  isPaid: boolean
  paidOnUtc: string | null
}

export interface AdminBed {
  id: number
  label: string
  isActive: boolean
  notes: string | null
  isOccupied: boolean
  currentLease: AdminBedAllocation | null
}

export interface LeasedBedsOverview {
  capacity: CapacitySummary
  beds: AdminBed[]
}

export interface AddBedInput {
  label: string
  notes?: string | null
}

export interface UpdateBedInput {
  label?: string | null
  notes?: string | null
  isActive?: boolean | null
}

// ---- Member-facing (Profile card) ----

export interface MembershipInfo {
  isActive: boolean
  paidThroughUtc: string | null
  canRenew: boolean
}

export interface MyLease {
  leaseId: number
  bedLabel: string
  status: BedLeaseStatus
  priceAtAllocationCents: number
  isPaid: boolean
  paidOnUtc: string | null
  expiresOn: string
  paymentDue: boolean
  canRenew: boolean
}

export interface MyRequestInfo {
  status: 'Pending' | 'Waitlisted'
  waitlistPosition: number | null
}

export interface MyLeasedBedStatus {
  membership: MembershipInfo
  capacity: CapacitySummary
  leases: MyLease[]
  request: MyRequestInfo | null
}

export async function fetchMyLeasedBedStatus(): Promise<MyLeasedBedStatus> {
  const res = await fetch('/api/leased-beds/my-status', { credentials: 'include' })
  if (!res.ok) throw await readError(res, 'Could not load your bed status')
  return res.json()
}

export async function applyForBed(): Promise<MyLeasedBedStatus> {
  const res = await fetch('/api/leased-beds/requests', { method: 'POST', credentials: 'include' })
  if (!res.ok) throw await readError(res, 'Could not submit your application')
  return res.json()
}

export async function withdrawBedRequest(): Promise<MyLeasedBedStatus> {
  const res = await fetch('/api/leased-beds/requests/withdraw', { method: 'POST', credentials: 'include' })
  if (!res.ok) throw await readError(res, 'Could not withdraw your request')
  return res.json()
}

export async function payLease(leaseId: number): Promise<{ checkoutUrl: string }> {
  const res = await fetch(`/api/leased-beds/leases/${leaseId}/pay`, { method: 'POST', credentials: 'include' })
  if (!res.ok) throw await readError(res, 'Could not start payment')
  return res.json()
}

export async function renewLease(leaseId: number): Promise<MyLeasedBedStatus> {
  const res = await fetch(`/api/leased-beds/leases/${leaseId}/renew`, { method: 'POST', credentials: 'include' })
  if (!res.ok) throw await readError(res, 'Could not renew your lease')
  return res.json()
}

async function readError(res: Response, fallback: string): Promise<Error> {
  const text = await res.text()
  try {
    return new Error(JSON.parse(text).error ?? text ?? fallback)
  } catch {
    return new Error(text || fallback)
  }
}

export async function fetchLeasedBeds(): Promise<LeasedBedsOverview> {
  const res = await fetch('/api/admin/leased-beds', { credentials: 'include' })
  if (!res.ok) throw await readError(res, 'Could not load leased beds')
  return res.json()
}

export async function addBed(input: AddBedInput): Promise<LeasedBedsOverview> {
  const res = await fetch('/api/admin/leased-beds/beds', {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!res.ok) throw await readError(res, 'Could not add the bed')
  return res.json()
}

export async function updateBed(bedId: number, input: UpdateBedInput): Promise<LeasedBedsOverview> {
  const res = await fetch(`/api/admin/leased-beds/beds/${bedId}`, {
    method: 'PATCH',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!res.ok) throw await readError(res, 'Could not update the bed')
  return res.json()
}

export async function deleteBed(bedId: number): Promise<LeasedBedsOverview> {
  const res = await fetch(`/api/admin/leased-beds/beds/${bedId}`, {
    method: 'DELETE',
    credentials: 'include',
  })
  if (!res.ok) throw await readError(res, 'Could not delete the bed')
  return res.json()
}

export interface AdminBedRequest {
  requestId: number
  memberId: string
  memberName: string | null
  memberEmail: string | null
  createdAtUtc: string
  position: number | null
}

export interface AdminBedRequests {
  pending: AdminBedRequest[]
  waitlist: AdminBedRequest[]
}

interface AssignBedBase {
  bedId: number
  customPriceCents?: number | null
}

/**
 * Assign a bed either by fulfilling an existing request (requestId) or directly to a
 * member who hasn't applied (userId) — exactly one, enforced by the union.
 */
export type AssignBedInput =
  | (AssignBedBase & { requestId: number })
  | (AssignBedBase & { userId: string })

export interface ReleaseResult {
  overview: LeasedBedsOverview
  waitlistCount: number
}

export async function fetchBedRequests(): Promise<AdminBedRequests> {
  const res = await fetch('/api/admin/leased-beds/requests', { credentials: 'include' })
  if (!res.ok) throw await readError(res, 'Could not load bed requests')
  return res.json()
}

export async function assignBed(input: AssignBedInput): Promise<LeasedBedsOverview> {
  const res = await fetch('/api/admin/leased-beds/assign', {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!res.ok) throw await readError(res, 'Could not assign the bed')
  return res.json()
}

export async function releaseLease(leaseId: number): Promise<ReleaseResult> {
  const res = await fetch(`/api/admin/leased-beds/leases/${leaseId}/release`, {
    method: 'POST',
    credentials: 'include',
  })
  if (!res.ok) throw await readError(res, 'Could not release the lease')
  return res.json()
}

export async function removeBedRequest(requestId: number): Promise<AdminBedRequests> {
  const res = await fetch(`/api/admin/leased-beds/requests/${requestId}`, {
    method: 'DELETE',
    credentials: 'include',
  })
  if (!res.ok) throw await readError(res, 'Could not remove the request')
  return res.json()
}

export async function recordLeasePayment(leaseId: number): Promise<LeasedBedsOverview> {
  const res = await fetch(`/api/admin/leased-beds/leases/${leaseId}/record-payment`, {
    method: 'POST',
    credentials: 'include',
  })
  if (!res.ok) throw await readError(res, 'Could not record the payment')
  return res.json()
}
