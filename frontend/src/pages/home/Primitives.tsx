import type {
  ButtonHTMLAttributes,
  CSSProperties,
  ReactNode,
} from 'react'

export type PhotoTone = 'leaf' | 'apple' | 'flesh' | 'earth' | 'sand'

type ButtonVariant = 'primary' | 'cream' | 'outlined' | 'leaf' | 'ghost'
type ButtonSize = 'sm' | 'md' | 'lg'

interface MSButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  children: ReactNode
  variant?: ButtonVariant
  size?: ButtonSize
}

const BUTTON_SIZES: Record<ButtonSize, CSSProperties> = {
  sm: { padding: '8px 14px', fontSize: 13 },
  md: { padding: '11px 20px', fontSize: 14 },
  lg: { padding: '14px 26px', fontSize: 16 },
}

const BUTTON_VARIANTS: Record<ButtonVariant, CSSProperties> = {
  primary:  { background: 'var(--apple-600)', color: 'var(--paper)', border: 0 },
  cream:    { background: 'var(--paper)', color: 'var(--ink-900)', border: 0 },
  outlined: { background: 'transparent', color: 'var(--ink-900)', border: '1.5px solid var(--ink-900)' },
  leaf:     { background: 'var(--leaf-700)', color: 'var(--paper)', border: 0 },
  ghost: {
    background: 'transparent',
    color: 'var(--apple-700)',
    border: 0,
    textDecoration: 'underline',
    textUnderlineOffset: 4,
    textDecorationThickness: 1,
    textDecorationColor: 'var(--apple-300)',
  },
}

export function MSButton({
  children,
  variant = 'primary',
  size = 'md',
  style,
  ...rest
}: MSButtonProps) {
  return (
    <button
      {...rest}
      style={{
        fontFamily: 'var(--font-sans)',
        fontWeight: 600,
        borderRadius: 'var(--r-md)',
        cursor: 'pointer',
        transition: 'all 120ms var(--ease-out)',
        display: 'inline-flex',
        alignItems: 'center',
        gap: 8,
        ...BUTTON_SIZES[size],
        ...BUTTON_VARIANTS[variant],
        ...style,
      }}
    >
      {children}
    </button>
  )
}

interface MSEyebrowProps {
  children: ReactNode
  color?: string
  style?: CSSProperties
}

export function MSEyebrow({
  children,
  color = 'var(--apple-700)',
  style,
}: MSEyebrowProps) {
  return (
    <div
      style={{
        fontFamily: 'var(--font-sans)',
        fontWeight: 700,
        fontSize: 12,
        letterSpacing: '0.16em',
        textTransform: 'uppercase',
        color,
        ...style,
      }}
    >
      {children}
    </div>
  )
}

interface MSSectionProps {
  children: ReactNode
  bg?: string
  narrow?: boolean
  py?: number
  style?: CSSProperties
  id?: string
}

export function MSSection({
  children,
  bg = 'var(--paper)',
  narrow = false,
  py = 96,
  style,
  id,
}: MSSectionProps) {
  return (
    <section id={id} style={{ background: bg, padding: `${py}px 0`, ...style }}>
      <div
        style={{
          maxWidth: narrow ? 820 : 1240,
          margin: '0 auto',
          padding: '0 32px',
        }}
      >
        {children}
      </div>
    </section>
  )
}

/* Decorative produce SVGs - watercolour-style silhouettes */

interface IconProps {
  size?: number
  style?: CSSProperties
}

export function Tomato({ size = 60, style }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 60 60" style={style}>
      <ellipse cx="30" cy="36" rx="22" ry="20" fill="#C84A30" />
      <ellipse cx="22" cy="32" rx="6" ry="4" fill="#DC6748" opacity="0.6" />
      <path d="M30 14 L26 20 L22 18 L24 22 L20 24 L26 26 L30 22 L34 26 L40 24 L36 22 L38 18 L34 20 Z" fill="#3F6630" />
      <path d="M30 14 L30 22" stroke="#2E4F25" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}

export function Carrot({ size = 60, style }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 60 60" style={style}>
      <path d="M22 22 L30 50 L38 22 Z" fill="#DC6748" />
      <path d="M24 26 L36 26 M25 30 L35 30 M27 35 L33 35" stroke="#A13927" strokeWidth="0.8" />
      <path d="M22 22 Q18 14 22 8 M30 22 Q30 12 32 6 M38 22 Q42 14 38 8" stroke="#3F6630" strokeWidth="2.5" fill="none" strokeLinecap="round" />
    </svg>
  )
}

export function Beet({ size = 60, style }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 60 60" style={style}>
      <ellipse cx="30" cy="38" rx="14" ry="12" fill="#7C2A1C" />
      <path d="M30 50 L28 56" stroke="#3A362C" strokeWidth="1.5" fill="none" strokeLinecap="round" />
      <path d="M22 26 Q18 16 24 10 M30 24 Q30 12 30 6 M38 26 Q42 16 36 10" stroke="#527E40" strokeWidth="2.5" fill="none" strokeLinecap="round" />
      <ellipse cx="22" cy="14" rx="5" ry="7" fill="#76985E" transform="rotate(-20 22 14)" />
      <ellipse cx="38" cy="14" rx="5" ry="7" fill="#76985E" transform="rotate(20 38 14)" />
      <ellipse cx="30" cy="9" rx="5" ry="7" fill="#76985E" />
    </svg>
  )
}

export function Leaf({ size = 60, style }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 60 60" style={style}>
      <path d="M10 50 Q10 20 30 10 Q50 20 50 50 Q30 42 10 50 Z" fill="#527E40" />
      <path d="M10 50 Q30 30 50 50" stroke="#2E4F25" strokeWidth="1" fill="none" />
      <path d="M30 12 L30 50" stroke="#2E4F25" strokeWidth="1" fill="none" />
    </svg>
  )
}

interface PhotoProps {
  ratio?: string
  tone?: PhotoTone
  label?: string
  icon?: ReactNode
  style?: CSSProperties
}

const PHOTO_TONES: Record<PhotoTone, { bg: string }> = {
  leaf:  { bg: 'linear-gradient(135deg, #527E40 0%, #3F6630 100%)' },
  apple: { bg: 'linear-gradient(135deg, #C84A30 0%, #A13927 100%)' },
  flesh: { bg: 'linear-gradient(135deg, #C9C26A 0%, #8C8A2E 100%)' },
  earth: { bg: 'linear-gradient(135deg, #7C2A1C 0%, #5A1E14 100%)' },
  sand:  { bg: 'linear-gradient(135deg, #F3EDDF 0%, #EBE3CF 100%)' },
}

export function Photo({
  ratio = '4 / 3',
  tone = 'leaf',
  label = '',
  icon,
  style,
}: PhotoProps) {
  const { bg } = PHOTO_TONES[tone]
  const filterId = `n-${tone}-${label.replace(/\s/g, '')}`
  return (
    <div
      style={{
        aspectRatio: ratio,
        background: bg,
        borderRadius: 'var(--r-lg)',
        position: 'relative',
        overflow: 'hidden',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        ...style,
      }}
    >
      <svg
        style={{
          position: 'absolute',
          inset: 0,
          width: '100%',
          height: '100%',
          opacity: 0.2,
          mixBlendMode: 'overlay',
        }}
      >
        <filter id={filterId}>
          <feTurbulence baseFrequency="0.9" numOctaves={2} />
        </filter>
        <rect width="100%" height="100%" filter={`url(#${filterId})`} />
      </svg>
      {icon}
      {label && (
        <div
          style={{
            position: 'absolute',
            bottom: 12,
            left: 14,
            fontFamily: 'var(--font-sans)',
            fontSize: 11,
            fontWeight: 600,
            color: 'rgba(255,252,246,0.85)',
            letterSpacing: '0.1em',
            textTransform: 'uppercase',
          }}
        >
          {label}
        </div>
      )}
    </div>
  )
}
