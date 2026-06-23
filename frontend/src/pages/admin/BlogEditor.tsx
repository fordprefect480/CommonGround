import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Image from '@tiptap/extension-image'
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
  'blog-img-wide': 'W',
}

const SIZE_TITLES: Record<ImageSizeClass, string> = {
  'blog-img-small': 'Small image',
  'blog-img-medium': 'Medium image',
  'blog-img-wide': 'Wide image',
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
})

export default function BlogEditor({ value, onChange }: BlogEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [2, 3, 4] },
      }),
      Link.configure({
        autolink: true,
        openOnClick: false,
        HTMLAttributes: { rel: 'noopener noreferrer', target: '_blank' },
      }),
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

  const setImageSize = (size: ImageSizeClass) =>
    editor.chain().focus().updateAttributes('image', { class: size }).run()

  const imageSelected = editor.isActive('image')

  return (
    <div className="tiptap-wrapper">
      <div className="tiptap-toolbar" role="toolbar" aria-label="Editor formatting">
        <ToolbarButton label="Bold" onClick={() => editor.chain().focus().toggleBold().run()} pressed={editor.isActive('bold')}><b>B</b></ToolbarButton>
        <ToolbarButton label="Italic" onClick={() => editor.chain().focus().toggleItalic().run()} pressed={editor.isActive('italic')}><i>I</i></ToolbarButton>

        <span className="tiptap-toolbar-divider" aria-hidden="true" />

        <ToolbarButton label="Heading" onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()} pressed={editor.isActive('heading', { level: 2 })}>H2</ToolbarButton>
        <ToolbarButton label="Subheading" onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()} pressed={editor.isActive('heading', { level: 3 })}>H3</ToolbarButton>

        <span className="tiptap-toolbar-divider" aria-hidden="true" />

        <ToolbarButton label="Bullet list" onClick={() => editor.chain().focus().toggleBulletList().run()} pressed={editor.isActive('bulletList')}><BulletListIcon /></ToolbarButton>
        <ToolbarButton label="Numbered list" onClick={() => editor.chain().focus().toggleOrderedList().run()} pressed={editor.isActive('orderedList')}><OrderedListIcon /></ToolbarButton>
        <ToolbarButton label="Quote" onClick={() => editor.chain().focus().toggleBlockquote().run()} pressed={editor.isActive('blockquote')}><QuoteIcon /></ToolbarButton>

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

        <span className="tiptap-toolbar-divider" aria-hidden="true" />

        {IMAGE_SIZE_CLASSES.map((size) => (
          <ToolbarButton
            key={size}
            label={SIZE_TITLES[size]}
            onClick={() => setImageSize(size)}
            pressed={editor.isActive('image', { class: size })}
            disabled={!imageSelected}
          >
            {SIZE_LABELS[size]}
          </ToolbarButton>
        ))}
      </div>
      <EditorContent editor={editor} className="tiptap-content blog-post-body" />
    </div>
  )
}
