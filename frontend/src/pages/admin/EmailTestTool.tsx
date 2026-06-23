import { lazy, Suspense, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AdminBackButton from './AdminBackButton'
import { sendTestEmail, type SendTestEmailResult } from '../../api/email'

const BlogEditor = lazy(() => import('./BlogEditor'))

type SendState =
  | { status: 'idle' }
  | { status: 'sending' }
  | { status: 'error'; message: string }
  | { status: 'sent'; result: SendTestEmailResult }

const EMPTY_BODY_HTML_PATTERNS = [/^\s*$/, /^<p>\s*<\/p>$/i]

function isBodyEmpty(html: string): boolean {
  const stripped = html.replace(/<br\s*\/?>(\s|&nbsp;)*/gi, '').trim()
  return EMPTY_BODY_HTML_PATTERNS.some((re) => re.test(stripped))
}

function parseRecipients(raw: string): string[] {
  return raw
    .split(/[\s,;]+/)
    .map((part) => part.trim().replace(/^.*<([^>]+)>\s*$/, '$1'))
    .filter((part) => part.length > 0)
}

export default function EmailTestTool() {
  const navigate = useNavigate()
  const [recipientsText, setRecipientsText] = useState('')
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [send, setSend] = useState<SendState>({ status: 'idle' })

  const recipients = parseRecipients(recipientsText)
  const subjectTrimmed = subject.trim()
  const bodyEmpty = isBodyEmpty(body)
  const canSend =
    subjectTrimmed.length > 0 &&
    !bodyEmpty &&
    recipients.length > 0 &&
    send.status !== 'sending'

  const performSend = async () => {
    if (!canSend) return
    setSend({ status: 'sending' })
    try {
      const result = await sendTestEmail(subjectTrimmed, body, recipients)
      setSend({ status: 'sent', result })
    } catch (err) {
      setSend({ status: 'error', message: err instanceof Error ? err.message : 'Send failed' })
    }
  }

  return (
    <section className="admin-page" aria-labelledby="email-test-heading">
      <header className="admin-page-header">
        <div className="admin-page-heading-group">
          <AdminBackButton to="/admin/tools" label="Back to tools" />
          <h1 id="email-test-heading" className="admin-page-title">Send test email</h1>
        </div>
      </header>

      <div className="card">
        <p className="card-note">
          Sends to the recipients below only - does not touch the mailing list and is not saved to the sent-email history.
        </p>

        <div className="field">
          <label className="field-label" htmlFor="email-test-recipients">Recipients</label>
          <textarea
            id="email-test-recipients"
            value={recipientsText}
            onChange={(e) => setRecipientsText(e.target.value)}
            disabled={send.status === 'sending'}
            rows={2}
            placeholder="alice@example.com, bob@example.com"
            autoComplete="off"
            spellCheck={false}
          />
          <span className="card-note">
            Separate addresses with commas, spaces, or newlines. Each recipient is sent their own copy.
            {recipients.length > 0 && (
              <> Parsed <strong>{recipients.length}</strong> address{recipients.length === 1 ? '' : 'es'}.</>
            )}
          </span>
        </div>

        <div className="field">
          <label className="field-label" htmlFor="email-test-subject">Subject</label>
          <input
            id="email-test-subject"
            type="text"
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
            disabled={send.status === 'sending'}
            maxLength={200}
            autoComplete="off"
          />
        </div>

        <div className="field">
          <label className="field-label">Body</label>
          <Suspense fallback={<p className="admin-loading">Loading editor&hellip;</p>}>
            <BlogEditor value={body} onChange={setBody} />
          </Suspense>
        </div>

        {(send.status === 'idle' || send.status === 'error') && (
          <div className="admin-actions">
            <button
              type="button"
              className="primary-button"
              onClick={performSend}
              disabled={!canSend}
            >
              {recipients.length > 0
                ? `Send to ${recipients.length} recipient${recipients.length === 1 ? '' : 's'}`
                : 'Send'}
            </button>
            <button type="button" className="footer-link" onClick={() => navigate('/admin/tools')}>
              Cancel
            </button>
          </div>
        )}

        {send.status === 'sending' && (
          <p className="admin-loading">Sending&hellip;</p>
        )}

        {send.status === 'error' && (
          <div className="form-error" role="alert">{send.message}</div>
        )}

        {send.status === 'sent' && (
          <div className="card-note" role="status" style={{ marginTop: '0.75rem' }}>
            <p>
              <strong>
                Delivered to {send.result.sent} of {send.result.sent + send.result.failed} recipient{send.result.sent + send.result.failed === 1 ? '' : 's'}.
              </strong>
            </p>
            {send.result.failures.length > 0 && (
              <ul>
                {send.result.failures.map((f) => (
                  <li key={f.email}><code>{f.email}</code>: {f.error}</li>
                ))}
              </ul>
            )}
            <div className="admin-actions" style={{ marginTop: '0.75rem' }}>
              <button type="button" className="footer-link" onClick={() => setSend({ status: 'idle' })}>
                Send another
              </button>
            </div>
          </div>
        )}
      </div>
    </section>
  )
}
