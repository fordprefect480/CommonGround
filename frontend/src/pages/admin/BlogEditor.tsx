import { useEditor, EditorContent, ReactNodeViewRenderer, NodeViewWrapper, type ReactNodeViewProps } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Image from '@tiptap/extension-image'
import TextAlign from '@tiptap/extension-text-align'
import { useEffect, type ReactNode } from 'react'
import { uploadBlogImage } from '../../api/blog'

interface BlogEditorProps {
  value: string
  onChange: (html: string) => void
}

const IMAGE_SIZE_CLASSES = ['blog-img-small', 'blog-img-medium', 'blog-img-wide'] as const
type ImageSizeClass = typeof IMAGE_SIZE_CLASSES[number]

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
  const sizeClass = (node.attrs.class as string | null) ?? 'blog-img-medium'
  return (
    <NodeViewWrapper className="tiptap-image-nodeview" data-selected={selected || undefined}>
      <img src={node.attrs.src} alt={node.attrs.alt ?? ''} className={sizeClass} draggable={false} />
      {selected && (
        <div className="tiptap-image-menu" contentEditable={false} onMouseDown={(e) => e.preventDefault()}>
          {IMAGE_SIZE_CLASSES.map((size) => (
            <button
              type="button"
              key={size}
              title={SIZE_TITLES[size]}
              aria-label={SIZE_TITLES[size]}
              aria-pressed={sizeClass === size}
              onClick={() => updateAttributes({ class: size })}
            >
              {SIZE_LABELS[size]}
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
      class: {
        default: 'blog-img-medium',
        parseHTML: (el) => el.getAttribute('class'),
        renderHTML: (attrs: { class?: string | null }) =>
          attrs.class ? { class: attrs.class } : {},
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
      SizedImage,
    ],
    content: value,
    shouldRerenderOnTransaction: true,
    onUpdate: ({ editor }) => onChange(editor.getHTML()),
  })

  useEffect(() => {
    if (editor && !editor.isDestroyed && editor.getHTML() !== value) editor.commands.setContent(value, { emitUpdate: false })
  }, [value, editor])

  if (!editor) return null

  const insertImage = async (file: File) => {
    try {
      const result = await uploadBlogImage(file)
      editor
        .chain()
        .focus()
        .setImage({ src: result.url, alt: '' })
        .updateAttributes('image', { class: 'blog-img-medium' })
        .run()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Upload failed')
    }
  }

  const promptLink = () => {
    const previous = editor.getAttributes('link').href as string | undefined
    const url = window.prompt('Link URL', previous ?? 'https://')
    if (url === null) return
    if (url === '') {
      editor.chain().focus().unsetLink().run()
      return
    }
    if (!/^(https?:|mailto:)/i.test(url)) {
      alert('Links must start with http://, https://, or mailto:')
      return
    }
    editor.chain().focus().setLink({ href: url }).run()
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

        <span className="tiptap-toolbar-divider" aria-hidden="true" />

        <ToolbarButton label="Bullet list" onClick={() => editor.chain().focus().toggleBulletList().run()} pressed={editor.isActive('bulletList')}><BulletListIcon /></ToolbarButton>
        <ToolbarButton label="Numbered list" onClick={() => editor.chain().focus().toggleOrderedList().run()} pressed={editor.isActive('orderedList')}><OrderedListIcon /></ToolbarButton>
        <ToolbarButton label="Quote" onClick={() => editor.chain().focus().toggleBlockquote().run()} pressed={editor.isActive('blockquote')}><QuoteIcon /></ToolbarButton>

        <span className="tiptap-toolbar-divider" aria-hidden="true" />

        <ToolbarButton label="Align left" onClick={() => editor.chain().focus().setTextAlign('left').run()} pressed={editor.isActive({ textAlign: 'left' })}><AlignLeftIcon /></ToolbarButton>
        <ToolbarButton label="Align center" onClick={() => editor.chain().focus().setTextAlign('center').run()} pressed={editor.isActive({ textAlign: 'center' })}><AlignCenterIcon /></ToolbarButton>
        <ToolbarButton label="Align right" onClick={() => editor.chain().focus().setTextAlign('right').run()} pressed={editor.isActive({ textAlign: 'right' })}><AlignRightIcon /></ToolbarButton>

        <span className="tiptap-toolbar-divider" aria-hidden="true" />

        <ToolbarButton label="Link" onClick={promptLink} pressed={editor.isActive('link')}><LinkIcon /></ToolbarButton>
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
    </div>
  )
}
