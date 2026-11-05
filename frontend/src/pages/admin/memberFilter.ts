import type { Member } from '../../api/auth'
import { isPaidForRenewalYear } from '../../format'

export type MembershipStatus = 'paid' | 'notPaid' | 'mailingListOnly'

// "Paid" means the member is paid up for the financial year the next renewal
// covers (renewalTargetUtc - the next 1 July, with the late-join carry-over,
// from the server's MembershipPeriod). For most of the year that's the current
// financial year; within the 8-week renewal window it flags members still
// covered this year but not yet renewed for the year ahead. See
// isPaidForRenewalYear for why coverage is tested against the year's start.
// A null paid-through means the user has never held a membership at all -
// they only joined the mailing list - which is a different thing from a
// member who has paid before and lapsed.
export function membershipStatus(member: Member, renewalTargetUtc: string): MembershipStatus {
  if (isPaidForRenewalYear(member.membershipPaidThroughUtc, renewalTargetUtc)) return 'paid'
  return member.membershipPaidThroughUtc ? 'notPaid' : 'mailingListOnly'
}

export type MemberFilter = 'all' | MembershipStatus | 'noLongerMember'

export function matchesMemberFilter(member: Member, status: MembershipStatus, filter: MemberFilter): boolean {
  switch (filter) {
    case 'all': return true
    case 'noLongerMember': return status === 'mailingListOnly' && !member.isSubscribedToMailingList
    default: return status === filter
  }
}
