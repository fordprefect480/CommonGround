import { describe, expect, it } from 'vitest'
import { imageAlignFromClass, imageSizeFromClass } from './blogImageClasses'

describe('imageSizeFromClass', () => {
  it('extracts the size token from a combined class string', () => {
    expect(imageSizeFromClass('blog-img-small blog-img-right')).toBe('blog-img-small')
  })

  it('defaults to medium when no size token is present', () => {
    expect(imageSizeFromClass('blog-img-left')).toBe('blog-img-medium')
  })

  it('defaults to medium for null/empty input (legacy images)', () => {
    expect(imageSizeFromClass(null)).toBe('blog-img-medium')
    expect(imageSizeFromClass('')).toBe('blog-img-medium')
  })
})

describe('imageAlignFromClass', () => {
  it('extracts the align token from a combined class string', () => {
    expect(imageAlignFromClass('blog-img-medium blog-img-right')).toBe('blog-img-right')
  })

  it('defaults to center when no align token is present (legacy images)', () => {
    expect(imageAlignFromClass('blog-img-medium')).toBe('blog-img-center')
    expect(imageAlignFromClass(null)).toBe('blog-img-center')
  })
})
