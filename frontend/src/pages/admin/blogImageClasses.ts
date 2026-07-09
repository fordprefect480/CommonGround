export const IMAGE_SIZE_CLASSES = ['blog-img-small', 'blog-img-medium', 'blog-img-wide'] as const
export type ImageSizeClass = typeof IMAGE_SIZE_CLASSES[number]

export const IMAGE_ALIGN_CLASSES = ['blog-img-left', 'blog-img-center', 'blog-img-right'] as const
export type ImageAlignClass = typeof IMAGE_ALIGN_CLASSES[number]

export const DEFAULT_IMAGE_SIZE: ImageSizeClass = 'blog-img-medium'
export const DEFAULT_IMAGE_ALIGN: ImageAlignClass = 'blog-img-center'

function tokens(className: string | null | undefined): string[] {
  return (className ?? '').split(/\s+/).filter(Boolean)
}

export function imageSizeFromClass(className: string | null | undefined): ImageSizeClass {
  const present = tokens(className)
  return IMAGE_SIZE_CLASSES.find((c) => present.includes(c)) ?? DEFAULT_IMAGE_SIZE
}

export function imageAlignFromClass(className: string | null | undefined): ImageAlignClass {
  const present = tokens(className)
  return IMAGE_ALIGN_CLASSES.find((c) => present.includes(c)) ?? DEFAULT_IMAGE_ALIGN
}
