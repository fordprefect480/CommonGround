import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AdminBackButton from './AdminBackButton'
import { fetchSubscriberCount, type NewsletterRecipients } from '../../api/email'
import { fetchMembers, type Member } from '../../api/auth'
import { ComposeForm, memberLabel, pluralize, useEmailTemplate } from './emailComposer'

type SubscriberCountState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; count: number }

type MembersState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; members: Member[] }

type RecipientMode = 'all_subscribers' | 'specific_members' | 'custom_emails'

function parseEmails(raw: string): string[] {
  return raw
    .split(/[\s,;]+/)
    .map((part) => part.trim().replace(/^.*<([^>]+)>\s*$/, '$1'))
    .filter((part) => part.length > 0)
}

export default function EmailCompose() {
  const navigate = useNavigate()
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [isNewsletter, setIsNewsletter] = useState(true)
  const template = useEmailTemplate(isNewsletter)
  const [mode, setMode] = useState<RecipientMode>('all_subscribers')
  const [subscriberCount, setSubscriberCount] = useState<SubscriberCountState>({ status: 'loading' })
  const [members, setMembers] = useState<MembersState>({ status: 'idle' })
  const [selectedMemberIds, setSelectedMemberIds] = useState<Set<string>>(new Set())
  const [memberQuery, setMemberQuery] = useState('')
  const [comboOpen, setComboOpen] = useState(false)
  const [comboHighlight, setComboHighlight] = useState(0)
  const comboRef = useRef<HTMLDivElement | null>(null)
  const comboInputRef = useRef<HTMLInputElement | null>(null)
  const [customEmailsText, setCustomEmailsText] = useState('')
  const [sending, setSending] = useState(false)

  useEffect(() => {
    let cancelled = false
    const load = async () => {
      try {
        const count = await fetchSubscriberCount()
        if (cancelled) return
        setSubscriberCount({ status: 'ready', count })
      } catch (err) {
        if (cancelled) return
        setSubscriberCount({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load recipient count' })
      }
    }
    load()
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    if (mode !== 'specific_members') return
    if (members.status !== 'idle') return
    setMembers({ status: 'loading' })
    let active = true
    ;(async () => {
      try {
        const list = await fetchMembers()
        if (active) setMembers({ status: 'ready', members: list })
      } catch (err) {
        if (active) setMembers({ status: 'error', message: err instanceof Error ? err.message : 'Failed to load members' })
      }
    })()
    return () => { active = false }
    // members.status intentionally omitted: re-running on its change would cancel the in-flight fetch
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mode])

  const customEmails = useMemo(() => parseEmails(customEmailsText), [customEmailsText])

  const selectedMembers = useMemo(() => {
    if (members.status !== 'ready') return []
    const byId = new Map(members.members.map((m) => [m.id, m]))
    return Array.from(selectedMemberIds)
      .map((id) => byId.get(id))
      .filter((m): m is Member => m !== undefined)
  }, [members, selectedMemberIds])

  const SUGGESTION_LIMIT = 8

  const suggestions = useMemo(() => {
    if (members.status !== 'ready') return []
    const q = memberQuery.trim().toLowerCase()
    return members.members
      .filter((m) => !selectedMemberIds.has(m.id))
      .filter((m) => !!(m.email && m.email.length > 0))
      .filter((m) => {
        if (q.length === 0) return true
        return memberLabel(m).toLowerCase().includes(q) || (m.email ?? '').toLowerCase().includes(q)
      })
      .slice(0, SUGGESTION_LIMIT)
  }, [members, memberQuery, selectedMemberIds])

  useEffect(() => {
    setComboHighlight(0)
  }, [memberQuery, comboOpen])

  useEffect(() => {
    if (!comboOpen) return
    const handler = (e: MouseEvent) => {
      if (comboRef.current && !comboRef.current.contains(e.target as Node)) {
        setComboOpen(false)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [comboOpen])

  const recipientCount = (() => {
    switch (mode) {
      case 'all_subscribers':
        return subscriberCount.status === 'ready' ? subscriberCount.count : 0
      case 'specific_members':
        return selectedMemberIds.size
      case 'custom_emails':
        return customEmails.length
    }
  })()

  const addMember = (id: string) => {
    setSelectedMemberIds((prev) => {
      if (prev.has(id)) return prev
      const next = new Set(prev)
      next.add(id)
      return next
    })
    setMemberQuery('')
    comboInputRef.current?.focus()
  }

  const removeMember = (id: string) => {
    setSelectedMemberIds((prev) => {
      if (!prev.has(id)) return prev
      const next = new Set(prev)
      next.delete(id)
      return next
    })
  }

  const clearSelection = () => setSelectedMemberIds(new Set())

  const onComboKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setComboOpen(true)
      setComboHighlight((h) => (suggestions.length === 0 ? 0 : (h + 1) % suggestions.length))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setComboOpen(true)
      setComboHighlight((h) => (suggestions.length === 0 ? 0 : (h - 1 + suggestions.length) % suggestions.length))
    } else if (e.key === 'Enter') {
      if (suggestions.length > 0) {
        e.preventDefault()
        const pick = suggestions[Math.min(comboHighlight, suggestions.length - 1)]
        addMember(pick.id)
      }
    } else if (e.key === 'Escape') {
      if (comboOpen) {
        e.preventDefault()
        setComboOpen(false)
      }
    } else if (e.key === 'Backspace' && memberQuery.length === 0 && selectedMembers.length > 0) {
      e.preventDefault()
      removeMember(selectedMembers[selectedMembers.length - 1].id)
    }
  }

  const buildRecipients = (): NewsletterRecipients => {
    switch (mode) {
      case 'all_subscribers':
        return { mode: 'all_subscribers' }
      case 'specific_members':
        return { mode: 'specific_members', memberIds: Array.from(selectedMemberIds) }
      case 'custom_emails':
        return { mode: 'custom_emails', emails: customEmails }
    }
  }

  const recipientNoun = mode === 'custom_emails' ? 'address' : 'recipient'

  return (
    <section className="admin-page" aria-labelledby="email-heading">
      <header className="admin-page-header">
        <div className="admin-page-heading-group">
          <AdminBackButton to="/admin/email" label="Back to emails" />
          <h1 id="email-heading" className="admin-page-title">New email</h1>
        </div>
      </header>

      {subscriberCount.status === 'error' && mode === 'all_subscribers' && (
        <div className="form-error" role="alert">{subscriberCount.message}</div>
      )}

      <div className="card">
        <fieldset className="field" disabled={sending}>
          <legend className="field-label">Recipients</legend>
          <div className="recipient-mode-options">
            <label className="checkbox-field">
              <input
                type="radio"
                name="recipient-mode"
                value="all_subscribers"
                checked={mode === 'all_subscribers'}
                onChange={() => setMode('all_subscribers')}
              />
              <span>
                All subscribers
                {subscriberCount.status === 'ready' && (
                  <span className="card-note" style={{ marginLeft: '0.5rem', fontStyle: 'normal' }}>
                    ({pluralize(subscriberCount.count, 'subscriber')})
                  </span>
                )}
              </span>
            </label>
            <label className="checkbox-field">
              <input
                type="radio"
                name="recipient-mode"
                value="specific_members"
                checked={mode === 'specific_members'}
                onChange={() => setMode('specific_members')}
              />
              <span>Specific members</span>
            </label>
            <label className="checkbox-field">
              <input
                type="radio"
                name="recipient-mode"
                value="custom_emails"
                checked={mode === 'custom_emails'}
                onChange={() => setMode('custom_emails')}
              />
              <span>Any email address</span>
            </label>
          </div>

          {mode === 'specific_members' && (
            <div className="recipient-mode-detail">
              {members.status === 'loading' && <p className="admin-loading">Loading members&hellip;</p>}
              {members.status === 'error' && (
                <div className="form-error" role="alert">{members.message}</div>
              )}
              {members.status === 'ready' && (
                <div className="member-combobox" ref={comboRef}>
                <div
                  className="member-combobox-input"
                  onClick={() => {
                    comboInputRef.current?.focus()
                    setComboOpen(true)
                  }}
                  role="presentation"
                >
                  {selectedMembers.map((m) => (
                    <span key={m.id} className="member-chip">
                      <span className="member-chip-label">{memberLabel(m)}</span>
                      <button
                        type="button"
                        className="member-chip-remove"
                        aria-label={`Remove ${memberLabel(m)}`}
                        onClick={(e) => {
                          e.stopPropagation()
                          removeMember(m.id)
                        }}
                        disabled={sending}
                      >
                        ×
                      </button>
                    </span>
                  ))}
                  <input
                    ref={comboInputRef}
                    type="text"
                    value={memberQuery}
                    onChange={(e) => {
                      setMemberQuery(e.target.value)
                      setComboOpen(true)
                    }}
                    onFocus={() => setComboOpen(true)}
                    onKeyDown={onComboKeyDown}
                    placeholder={selectedMembers.length === 0 ? 'Type a name or email…' : ''}
                    autoComplete="off"
                    disabled={sending}
                    role="combobox"
                    aria-expanded={comboOpen}
                    aria-autocomplete="list"
                  />
                </div>
                {comboOpen && suggestions.length > 0 && (
                  <ul className="member-suggestions" role="listbox">
                    {suggestions.map((m, i) => (
                      <li
                        key={m.id}
                        role="option"
                        aria-selected={i === comboHighlight}
                        className={`member-suggestion${i === comboHighlight ? ' is-highlighted' : ''}`}
                        onMouseDown={(e) => {
                          e.preventDefault()
                          addMember(m.id)
                        }}
                        onMouseEnter={() => setComboHighlight(i)}
                      >
                        <span className="member-suggestion-name">{memberLabel(m)}</span>
                        <span className="member-suggestion-email">{m.email}</span>
                      </li>
                    ))}
                  </ul>
                )}
                {comboOpen && suggestions.length === 0 && memberQuery.trim().length > 0 && (
                  <p className="member-suggestions-empty">No matching members.</p>
                )}
                {selectedMembers.length > 0 && (
                  <div className="admin-actions" style={{ marginTop: '0.5rem' }}>
                    <span className="card-note">{pluralize(selectedMembers.length, 'member')} selected</span>
                    <button type="button" className="footer-link" onClick={clearSelection} disabled={sending}>
                      Clear
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>
        )}

          {mode === 'custom_emails' && (
            <div className="recipient-mode-detail">
              <textarea
                id="email-custom-recipients"
                value={customEmailsText}
                onChange={(e) => setCustomEmailsText(e.target.value)}
                disabled={sending}
                rows={3}
                placeholder="alice@example.com, bob@example.com"
                autoComplete="off"
                spellCheck={false}
              />
              <span className="card-note">
                Separate addresses with commas, spaces, or newlines. Each recipient gets their own copy.
                {customEmails.length > 0 && (
                  <> Parsed <strong>{customEmails.length}</strong> address{customEmails.length === 1 ? '' : 'es'}.</>
                )}
              </span>
            </div>
          )}
        </fieldset>

        <ComposeForm
          subject={subject}
          onSubjectChange={setSubject}
          body={body}
          onBodyChange={setBody}
          isNewsletter={isNewsletter}
          onIsNewsletterChange={setIsNewsletter}
          template={template}
          recipientCount={recipientCount}
          recipientNoun={recipientNoun}
          buildRecipients={buildRecipients}
          onSent={(result) => navigate(`/admin/email/${result.id}`, { replace: true })}
          onSendingChange={setSending}
          secondaryAction={
            <button type="button" className="footer-link" onClick={() => navigate('/admin/email')}>
              Cancel
            </button>
          }
          footerNote={
            mode === 'all_subscribers' && subscriberCount.status === 'ready' && subscriberCount.count === 0 ? (
              <span className="card-note">There are no subscribed members to send to.</span>
            ) : undefined
          }
        />
      </div>
    </section>
  )
}
