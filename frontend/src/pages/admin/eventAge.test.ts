import { describe, expect, it } from 'vitest'
import { isOlderThanAMonth } from './eventAge'

const now = new Date('2026-10-02T00:00:00Z').getTime()

describe('isOlderThanAMonth', () => {
  it('is false for an upcoming event', () => {
    expect(isOlderThanAMonth({ startUtc: '2026-10-10T00:00:00Z' }, now)).toBe(false)
  })

  it('is false for an event that ended within the last 30 days', () => {
    expect(isOlderThanAMonth({ startUtc: '2026-09-05T00:00:00Z' }, now)).toBe(false)
  })

  it('is true for an event that ended more than 30 days ago', () => {
    expect(isOlderThanAMonth({ startUtc: '2026-08-20T00:00:00Z' }, now)).toBe(true)
  })

  it('uses the end time when present', () => {
    expect(isOlderThanAMonth({ startUtc: '2026-08-01T00:00:00Z', endUtc: '2026-09-20T00:00:00Z' }, now)).toBe(false)
  })

  it('falls back to the start time when end is null', () => {
    expect(isOlderThanAMonth({ startUtc: '2026-08-01T00:00:00Z', endUtc: null }, now)).toBe(true)
  })
})
