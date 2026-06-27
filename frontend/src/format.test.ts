import { describe, expect, it } from 'vitest'
import { financialYearLabel, isPaidForRenewalYear, membershipPaidThroughFyLabel } from './format'

describe('isPaidForRenewalYear', () => {
  // The renewal target the server reports - the end (1 July) of the year the
  // next renewal covers. Coverage is tested against the year's START, one year
  // earlier. These cases use absolute instants, so they're timezone-independent.
  const renewalTarget = '2027-07-01T00:00:00Z'

  it('treats a missing paid-through date as not paid', () => {
    expect(isPaidForRenewalYear(null, renewalTarget)).toBe(false)
  })

  it('treats an unparseable paid-through date as not paid', () => {
    expect(isPaidForRenewalYear('not-a-date', renewalTarget)).toBe(false)
  })

  it('is paid when covered through the renewal year', () => {
    expect(isPaidForRenewalYear('2027-07-01T00:00:00Z', renewalTarget)).toBe(true)
  })

  it('is paid when covered to the last day before the boundary (the bug it guards)', () => {
    // A paid-through that lands just before the exact 1 July boundary still
    // covers the renewal year and must read as paid, not "not yet paid".
    expect(isPaidForRenewalYear('2027-06-30T00:00:00Z', renewalTarget)).toBe(true)
  })

  it('is not paid when covered only up to the start of the renewal year', () => {
    // Exactly the year start: still owes for the renewal year.
    expect(isPaidForRenewalYear('2026-07-01T00:00:00Z', renewalTarget)).toBe(false)
  })

  it('is not paid when coverage ends before the renewal year starts', () => {
    expect(isPaidForRenewalYear('2026-06-30T12:00:00Z', renewalTarget)).toBe(false)
  })

  it('is not paid for a long-lapsed membership', () => {
    expect(isPaidForRenewalYear('2025-07-01T00:00:00Z', renewalTarget)).toBe(false)
  })
})

describe('financialYearLabel', () => {
  // Dates built from local components so the label is deterministic regardless
  // of the machine's timezone (the Australian FY runs 1 July - 30 June).
  it('labels July onwards as the year starting that July', () => {
    expect(financialYearLabel(new Date(2026, 6, 1))).toBe('2026/27')
  })

  it('labels June as the prior financial year', () => {
    expect(financialYearLabel(new Date(2026, 5, 30))).toBe('2025/26')
  })

  it('labels January as the prior financial year', () => {
    expect(financialYearLabel(new Date(2026, 0, 1))).toBe('2025/26')
  })
})

describe('membershipPaidThroughFyLabel', () => {
  // Mid-month instants so the day-before step and timezone don't flip the FY.
  it('labels a March paid-through as the financial year ending that June', () => {
    expect(membershipPaidThroughFyLabel('2027-03-15T12:00:00Z')).toBe('2026/27')
  })

  it('labels a February paid-through as the financial year ending that June', () => {
    expect(membershipPaidThroughFyLabel('2026-02-15T12:00:00Z')).toBe('2025/26')
  })
})
