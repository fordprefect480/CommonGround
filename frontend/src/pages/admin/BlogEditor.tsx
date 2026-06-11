import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Image from '@tiptap/extension-image'
import { useEffect } from 'react'
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
    onUpdate: ({ editor }) => onChange(editor.getHTML()),
  })

  useEffect(() => {
    if (editor && editor.getHTML() !== value) editor.commands.setContent(value, { emitUpdate: false })
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
        <button type="button" onClick={() => editor.chain().focus().toggleBold().run()} aria-pressed={editor.isActive('bold')}><b>B</b></button>
        <button type="button" onClick={() => editor.chain().focus().toggleItalic().run()} aria-pressed={editor.isActive('italic')}><i>I</i></button>
        <button type="button" onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()} aria-pressed={editor.isActive('heading', { level: 2 })}>H2</button>
        <button type="button" onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()} aria-pressed={editor.isActive('heading', { level: 3 })}>H3</button>
        <button type="button" onClick={() => editor.chain().focus().toggleBulletList().run()} aria-pressed={editor.isActive('bulletList')}>&#8226; List</button>
        <button type="button" onClick={() => editor.chain().focus().toggleOrderedList().run()} aria-pressed={editor.isActive('orderedList')}>1. List</button>
        <button type="button" onClick={() => editor.chain().focus().toggleBlockquote().run()} aria-pressed={editor.isActive('blockquote')}>&ldquo; Quote</button>
        <button type="button" onClick={promptLink} aria-pressed={editor.isActive('link')}>Link</button>
        <label className="tiptap-image-button">
          Image
          <input
            type="file"
            accept="image/jpeg,image/png,image/webp,image/gif"
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
          <button
            key={size}
            type="button"
            onClick={() => setImageSize(size)}
            aria-pressed={editor.isActive('image', { class: size })}
            disabled={!imageSelected}
            title={`Image size: ${SIZE_LABELS[size] === 'S' ? 'small' : SIZE_LABELS[size] === 'M' ? 'medium' : 'wide'}`}
          >
            {SIZE_LABELS[size]}
          </button>
        ))}
      </div>
      <EditorContent editor={editor} className="tiptap-content blog-post-body" />
    </div>
  )
}
