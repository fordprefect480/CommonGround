import { describe, expect, it } from 'vitest'
import { bedsFreedText } from './bedsFreedText'

describe('bedsFreedText', () => {
  it('names a single bed', () => {
    expect(bedsFreedText(['N1'])).toBe('Garden bed N1 will be freed up for someone else.')
  })

  it('joins two beds with an ampersand', () => {
    expect(bedsFreedText(['N1', 'N2'])).toBe('Garden beds N1 & N2 will be freed up for someone else.')
  })

  it('joins several beds with commas and a final ampersand', () => {
    expect(bedsFreedText(['N1', 'N2', 'S3'])).toBe('Garden beds N1, N2 & S3 will be freed up for someone else.')
  })

  it('says so when no beds are held', () => {
    expect(bedsFreedText([])).toBe('They do not hold any garden beds.')
  })

  it('falls back to a general sentence when beds are unknown', () => {
    expect(bedsFreedText(null)).toBe('Any garden bed they hold will be freed up for someone else.')
  })
})
