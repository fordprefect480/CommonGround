import { describe, expect, it } from 'vitest'
import type { Member } from '../../api/auth'
import { matchesMemberFilter, type MembershipStatus } from './memberFilter'

const member = (isSubscribedToMailingList: boolean) => ({ isSubscribedToMailingList }) as Member

describe('matchesMemberFilter', () => {
  it('matches everyone for "all"', () => {
    expect(matchesMemberFilter(member(false), 'paid', 'all')).toBe(true)
  })

  it.each<MembershipStatus>(['paid', 'notPaid', 'mailingListOnly'])('matches status "%s" exactly', (status) => {
    expect(matchesMemberFilter(member(true), status, status)).toBe(true)
    expect(matchesMemberFilter(member(true), status, status === 'paid' ? 'notPaid' : 'paid')).toBe(false)
  })

  it('"noLongerMember" matches mailing-list-only users who unsubscribed', () => {
    expect(matchesMemberFilter(member(false), 'mailingListOnly', 'noLongerMember')).toBe(true)
  })

  it('"noLongerMember" excludes mailing-list-only users still subscribed', () => {
    expect(matchesMemberFilter(member(true), 'mailingListOnly', 'noLongerMember')).toBe(false)
  })

  it.each<MembershipStatus>(['paid', 'notPaid'])('"noLongerMember" excludes unsubscribed %s members', (status) => {
    expect(matchesMemberFilter(member(false), status, 'noLongerMember')).toBe(false)
  })
})
