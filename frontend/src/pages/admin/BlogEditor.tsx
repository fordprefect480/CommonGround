import { useEditor, EditorContent, ReactNodeViewRenderer, NodeViewWrapper, type ReactNodeViewProps } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Image from '@tiptap/extension-image'
import TextAlign from '@tiptap/extension-text-align'
import { TextStyle, Color } from '@tiptap/extension-text-style'
import { useEffect, useRef, useState, type ReactNode } from 'react'
import { uploadBlogImage } from '../../api/blog'
import PromptModal from './PromptModal'
import {
  IMAGE_SIZE_CLASSES,
  imageAlignFromClass,
  imageSizeFromClass,
  DEFAULT_IMAGE_SIZE,
  DEFAULT_IMAGE_ALIGN,
  type ImageAlignClass,
  type ImageSizeClass,
} from './blogImageClasses'

interface BlogEditorProps {
  value: string
  onChange: (html: string) => void
}

const SIZE_LABELS: Record<ImageSizeClass, string> = {
  'blog-img-small': 'S',
  'blog-img-medium': 'M',
  'blog-img-wide': 'L',
}

const SIZE_TITLES: Record<ImageSizeClass, string> = {
  'blog-img-small': 'Small image',
  'blog-img-medium': 'Medium image',
  'blog-img-wide': 'Large image',
}

const BLOCK_OPTIONS = [
  { value: 'paragraph', label: 'Normal text' },
  { value: 'title', label: 'Title' },
  { value: 'heading', label: 'Heading' },
  { value: 'subheading', label: 'Subheading' },
] as const

const BLOCK_LEVEL: Record<'title' | 'heading' | 'subheading', 2 | 3 | 4> = {
  title: 2,
  heading: 3,
  subheading: 4,
}

// Curated palette drawn from the site's brand tokens. Values are lower-cased hex
// so they compare cleanly against what TipTap reports as the active colour.
const TEXT_COLORS: { value: string; label: string }[] = [
  { value: '#16140f', label: 'Ink' },
  { value: '#6a6452', label: 'Stone' },
  { value: '#c84a30', label: 'Apple red' },
  { value: '#a13927', label: 'Deep apple' },
  { value: '#527e40', label: 'Leaf green' },
  { value: '#2e4f25', label: 'Deep leaf' },
  { value: '#8c8a2e', label: 'Olive' },
]

const iconProps = {
  width: 18,
  height: 18,
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 2,
  strokeLinecap: 'round',
  strokeLinejoin: 'round',
} as const

const BulletListIcon = () => (
  <svg {...iconProps} aria-hidden="true">
    <line x1="9" y1="6" x2="20" y2="6" />
    <line x1="9" y1="12" x2="20" y2="12" />
    <line x1="9" y1="18" x2="20" y2="18" />
    <circle cx="4.5" cy="6" r="1.3" fill="currentColor" stroke="none" />
    <circle cx="4.5" cy="12" r="1.3" fill="currentColor" stroke="none" />
    <circle cx="4.5" cy="18" r="1.3" fill="currentColor" stroke="none" />
  </svg>
)

const OrderedListIcon = () => (
  <svg {...iconProps} aria-hidden="true">
    <line x1="10" y1="6" x2="20" y2="6" />
    <line x1="10" y1="12" x2="20" y2="12" />
    <line x1="10" y1="18" x2="20" y2="18" />
    <path d="M4 6h1v4" />
    <path d="M4 10h2" />
    <path d="M6 18H4c0-1 2-1.5 2-2.5S5 14 4 14.5" />
  </svg>
)

const QuoteIcon = () => (
  <svg {...iconProps} aria-hidden="true">
    <path d="M6 6v12" />
    <line x1="11" y1="7" x2="20" y2="7" />
    <line x1="11" y1="12" x2="20" y2="12" />
    <line x1="11" y1="17" x2="20" y2="17" />
  </svg>
)

const AlignLeftIcon = () => (
  <svg {...iconProps} aria-hidden="true">
    <line x1="3" y1="6" x2="21" y2="6" />
    <line x1="3" y1="12" x2="15" y2="12" />
    <line x1="3" y1="18" x2="18" y2="18" />
  </svg>
)

const AlignCenterIcon = () => (
  <svg {...iconProps} aria-hidden="true">
    <line x1="3" y1="6" x2="21" y2="6" />
    <line x1="6" y1="12" x2="18" y2="12" />
    <line x1="5" y1="18" x2="19" y2="18" />
  </svg>
)

const AlignRightIcon = () => (
  <svg {...iconProps} aria-hidden="true">
    <line x1="3" y1="6" x2="21" y2="6" />
    <line x1="9" y1="12" x2="21" y2="12" />
    <line x1="6" y1="18" x2="21" y2="18" />
  </svg>
)

const LinkIcon = () => (
  <svg {...iconProps} aria-hidden="true">
    <path d="M9 17H7A5 5 0 0 1 7 7h2" />
    <path d="M15 7h2a5 5 0 0 1 0 10h-2" />
    <line x1="8" y1="12" x2="16" y2="12" />
  </svg>
)

const ImageIcon = () => (
  <svg {...iconProps} aria-hidden="true">
    <rect x="3" y="3" width="18" height="18" rx="2" />
    <circle cx="8.5" cy="8.5" r="1.5" />
    <path d="M21 15l-5-5L5 21" />
  </svg>
)

const TrashIcon = () => (
  <svg {...iconProps} aria-hidden="true">
    <path d="M4 7h16" />
    <path d="M10 11v6" />
    <path d="M14 11v6" />
    <path d="M6 7l1 13a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1l1-13" />
    <path d="M9 7V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v3" />
  </svg>
)

function ToolbarButton({ label, onClick, pressed, disabled, children }: {
  label: string
  onClick: () => void
  pressed?: boolean
  disabled?: boolean
  children: ReactNode
}) {
  return (
    <button type="button" onClick={onClick} title={label} aria-label={label} aria-pressed={pressed} disabled={disabled}>
      {children}
    </button>
  )
}

function ImageNodeView({ node, selected, updateAttributes, deleteNode }: ReactNodeViewProps) {
  const size = (node.attrs.size as ImageSizeClass | undefined) ?? DEFAULT_IMAGE_SIZE
  const align = (node.attrs.align as ImageAlignClass | undefined) ?? DEFAULT_IMAGE_ALIGN
  return (
    <NodeViewWrapper className="tiptap-image-nodeview" data-selected={selected || undefined} data-align={align}>
      <img src={node.attrs.src} alt={node.attrs.alt ?? ''} className={size} draggable={false} />
      {selected && (
        <div className="tiptap-image-menu" contentEditable={false} onMouseDown={(e) => e.preventDefault()}>
          {IMAGE_SIZE_CLASSES.map((s) => (
            <button
              type="button"
              key={s}
              title={SIZE_TITLES[s]}
              aria-label={SIZE_TITLES[s]}
              aria-pressed={size === s}
              onClick={() => updateAttributes({ size: s })}
            >
              {SIZE_LABELS[s]}
            </button>
          ))}
          <span className="tiptap-image-menu-divider" aria-hidden="true" />
          <button type="button" className="tiptap-image-delete" title="Delete image" aria-label="Delete image" onClick={() => deleteNode()}>
            <TrashIcon />
          </button>
        </div>
      )}
    </NodeViewWrapper>
  )
}

const SizedImage = Image.extend({
  addAttributes() {
    return {
      ...this.parent?.(),
      size: {
        default: DEFAULT_IMAGE_SIZE,
        parseHTML: (el: HTMLElement) => imageSizeFromClass(el.getAttribute('class')),
        renderHTML: (attrs: { size?: string | null }) => (attrs.size ? { class: attrs.size } : {}),
      },
      align: {
        default: DEFAULT_IMAGE_ALIGN,
        parseHTML: (el: HTMLElement) => imageAlignFromClass(el.getAttribute('class')),
        renderHTML: (attrs: { align?: string | null }) => (attrs.align ? { class: attrs.align } : {}),
      },
    }
  },
  addNodeView() {
    return ReactNodeViewRenderer(ImageNodeView)
  },
})

export default function BlogEditor({ value, onChange }: BlogEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [1, 2, 3, 4] },
      }),
      Link.configure({
        autolink: true,
        openOnClick: false,
        HTMLAttributes: { rel: 'noopener noreferrer', target: '_blank' },
      }),
      TextAlign.configure({ types: ['heading', 'paragraph'] }),
      TextStyle,
      Color,
      SizedImage,
    ],
    content: value,
    shouldRerenderOnTransaction: true,
    onUpdate: ({ editor }) => onChange(editor.getHTML()),
  })

  useEffect(() => {
    if (editor && !editor.isDestroyed && editor.getHTML() !== value) editor.commands.setContent(value, { emitUpdate: false })
  }, [value, editor])

  // null = closed; a string is the initial value shown in the link dialog.
  const [linkInitial, setLinkInitial] = useState<string | null>(null)

  const [colorOpen, setColorOpen] = useState(false)
  const colorRef = useRef<HTMLDivElement>(null)

  // Close the colour popover when clicking outside it.
  useEffect(() => {
    if (!colorOpen) return
    const onPointerDown = (e: MouseEvent) => {
      if (colorRef.current && !colorRef.current.contains(e.target as Node)) setColorOpen(false)
    }
    document.addEventListener('mousedown', onPointerDown)
    return () => document.removeEventListener('mousedown', onPointerDown)
  }, [colorOpen])

  if (!editor) return null

  const insertImage = async (file: File) => {
    try {
      const result = await uploadBlogImage(file)
      editor.chain().focus().setImage({ src: result.url, alt: '' }).run()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Upload failed')
    }
  }

  const openLinkModal = () => {
    const previous = editor.getAttributes('link').href as string | undefined
    setLinkInitial(previous ?? 'https://')
  }

  const applyLink = (url: string) => {
    if (url === '') editor.chain().focus().unsetLink().run()
    else editor.chain().focus().setLink({ href: url }).run()
  }

  const currentBlock = editor.isActive('heading', { level: 2 })
    ? 'title'
    : editor.isActive('heading', { level: 3 })
      ? 'heading'
      : editor.isActive('heading', { level: 4 })
        ? 'subheading'
        : 'paragraph'

  const applyBlock = (value: string) => {
    if (value === 'paragraph') editor.chain().focus().setParagraph().run()
    else editor.chain().focus().setHeading({ level: BLOCK_LEVEL[value as keyof typeof BLOCK_LEVEL] }).run()
  }

  const currentColor = (editor.getAttributes('textStyle').color as string | undefined)?.toLowerCase()

  const applyColor = (color: string | null) => {
    if (color) editor.chain().focus().setColor(color).run()
    else editor.chain().focus().unsetColor().run()
    setColorOpen(false)
  }

  return (
    <div className="tiptap-wrapper">
      <div className="tiptap-toolbar" role="toolbar" aria-label="Editor formatting">
        <select className="tiptap-style-select" aria-label="Text style" value={currentBlock} onChange={(e) => applyBlock(e.target.value)}>
          {BLOCK_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>

        <span className="tiptap-toolbar-divider" aria-hidden="true" />

        <ToolbarButton label="Bold" onClick={() => editor.chain().focus().toggleBold().run()} pressed={editor.isActive('bold')}><b>B</b></ToolbarButton>
        <ToolbarButton label="Italic" onClick={() => editor.chain().focus().toggleItalic().run()} pressed={editor.isActive('italic')}><i style={{ display: 'inline-block', transform: 'skewX(-12deg)' }}>I</i></ToolbarButton>

        <div className="tiptap-color-control" ref={colorRef}>
          <button
            type="button"
            className="tiptap-color-button"
            title="Text colour"
            aria-label="Text colour"
            aria-haspopup="true"
            aria-expanded={colorOpen}
            onClick={() => setColorOpen((open) => !open)}
          >
            <span aria-hidden="true">A</span>
            <span className="tiptap-color-bar" style={{ background: currentColor ?? 'var(--ink-900)' }} />
          </button>
          {colorOpen && (
            <div className="tiptap-color-popover" role="menu" aria-label="Text colour">
              <div className="tiptap-color-swatches">
                {TEXT_COLORS.map((c) => (
                  <button
                    type="button"
                    key={c.value}
                    role="menuitemradio"
                    aria-checked={currentColor === c.value}
                    className="tiptap-color-swatch"
                    title={c.label}
                    aria-label={c.label}
                    style={{ background: c.value }}
                    onClick={() => applyColor(c.value)}
                  />
                ))}
              </div>
              <button type="button" className="tiptap-color-reset" role="menuitem" onClick={() => applyColor(null)}>
                Default colour
              </button>
            </div>
          )}
        </div>

        <span className="tiptap-toolbar-divider" aria-hidden="true" />

        <ToolbarButton label="Bullet list" onClick={() => editor.chain().focus().toggleBulletList().run()} pressed={editor.isActive('bulletList')}><BulletListIcon /></ToolbarButton>
        <ToolbarButton label="Numbered list" onClick={() => editor.chain().focus().toggleOrderedList().run()} pressed={editor.isActive('orderedList')}><OrderedListIcon /></ToolbarButton>
        <ToolbarButton label="Quote" onClick={() => editor.chain().focus().toggleBlockquote().run()} pressed={editor.isActive('blockquote')}><QuoteIcon /></ToolbarButton>

        <span className="tiptap-toolbar-divider" aria-hidden="true" />

        <ToolbarButton
          label="Align left"
          onClick={() =>
            editor.isActive('image')
              ? editor.chain().focus().updateAttributes('image', { align: 'blog-img-left' }).run()
              : editor.chain().focus().setTextAlign('left').run()
          }
          pressed={editor.isActive('image') ? editor.getAttributes('image').align === 'blog-img-left' : editor.isActive({ textAlign: 'left' })}
        ><AlignLeftIcon /></ToolbarButton>
        <ToolbarButton
          label="Align center"
          onClick={() =>
            editor.isActive('image')
              ? editor.chain().focus().updateAttributes('image', { align: 'blog-img-center' }).run()
              : editor.chain().focus().setTextAlign('center').run()
          }
          pressed={editor.isActive('image') ? editor.getAttributes('image').align === 'blog-img-center' : editor.isActive({ textAlign: 'center' })}
        ><AlignCenterIcon /></ToolbarButton>
        <ToolbarButton
          label="Align right"
          onClick={() =>
            editor.isActive('image')
              ? editor.chain().focus().updateAttributes('image', { align: 'blog-img-right' }).run()
              : editor.chain().focus().setTextAlign('right').run()
          }
          pressed={editor.isActive('image') ? editor.getAttributes('image').align === 'blog-img-right' : editor.isActive({ textAlign: 'right' })}
        ><AlignRightIcon /></ToolbarButton>

        <span className="tiptap-toolbar-divider" aria-hidden="true" />

        <ToolbarButton label="Link" onClick={openLinkModal} pressed={editor.isActive('link')}><LinkIcon /></ToolbarButton>
        <label className="tiptap-image-button" title="Insert image">
          <ImageIcon />
          <input
            type="file"
            accept="image/jpeg,image/png,image/webp,image/gif"
            aria-label="Insert image"
            style={{ display: 'none' }}
            onChange={(e) => {
              const f = e.target.files?.[0]
              if (f) {
                insertImage(f)
                e.target.value = ''
              }
            }}
          />
        </label>
      </div>
      <EditorContent editor={editor} className="tiptap-content blog-post-body" />

      <PromptModal
        open={linkInitial !== null}
        title="Link"
        label="Link URL"
        description="Leave blank to remove the link."
        initialValue={linkInitial ?? ''}
        placeholder="https://"
        inputMode="url"
        confirmLabel="Apply link"
        onClose={() => setLinkInitial(null)}
        validate={(v) => (v === '' || /^(https?:|mailto:)/i.test(v) ? null : 'Links must start with http://, https://, or mailto:')}
        onConfirm={applyLink}
      />
    </div>
  )
}
