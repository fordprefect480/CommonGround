import { useEffect, useMemo, useState } from 'react'
import { fetchMembers, type Member } from '../../api/auth'

const ADMIN_ROLE = 'Admin'

type TabKey = 'members' | 'administrators'

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; members: Member[] }

interface TabDef {
  key: TabKey
  label: string
  emptyText: string
}

const TABS: TabDef[] = [
  { key: 'members', label: 'Members', emptyText: 'No members are registered yet.' },
  { key: 'administrators', label: 'Administrators', emptyText: 'No administrators yet.' },
]

export default function Members() {
  const [state, setState] = useState<State>({ status: 'loading' })
  const [activeTab, setActiveTab] = useState<TabKey>('members')

  useEffect(() => {
    let cancelled = false
    fetchMembers()
      .then((members) => {
        if (!cancelled) setState({ status: 'ready', members })
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setState({
            status: 'error',
            message: err instanceof Error ? err.message : 'Failed to load members',
          })
        }
      })
    return () => {
      cancelled = true
    }
  }, [])

  const buckets = useMemo(() => {
    if (state.status !== 'ready') return null
    return {
      members: state.members.filter((m) => !m.roles.includes(ADMIN_ROLE)),
      administrators: state.members.filter((m) => m.roles.includes(ADMIN_ROLE)),
    }
  }, [state])

  return (
    <section className="admin-page" aria-labelledby="members-heading">
      <header className="admin-page-header">
        <h1 id="members-heading" className="admin-page-title">Members</h1>
      </header>

      {state.status === 'loading' && (
        <p className="admin-loading">Loading members&hellip;</p>
      )}

      {state.status === 'error' && (
        <div className="form-error" role="alert">{state.message}</div>
      )}

      {state.status === 'ready' && buckets && (
        <>
          <div className="tabs" role="tablist" aria-label="Member groups">
            {TABS.map((tab) => {
              const count = buckets[tab.key].length
              const selected = activeTab === tab.key
              return (
                <button
                  key={tab.key}
                  type="button"
                  role="tab"
                  id={`tab-${tab.key}`}
                  aria-controls={`panel-${tab.key}`}
                  aria-selected={selected}
                  tabIndex={selected ? 0 : -1}
                  className={`tab ${selected ? 'tab-active' : ''}`}
                  onClick={() => setActiveTab(tab.key)}
                >
                  <span>{tab.label}</span>
                  <span className="tab-count" aria-hidden="true">{count}</span>
                  <span className="visually-hidden">({count})</span>
                </button>
              )
            })}
          </div>

          {TABS.map((tab) => (
            <div
              key={tab.key}
              role="tabpanel"
              id={`panel-${tab.key}`}
              aria-labelledby={`tab-${tab.key}`}
              hidden={activeTab !== tab.key}
            >
              <MembersTable members={buckets[tab.key]} emptyText={tab.emptyText} />
            </div>
          ))}
        </>
      )}
    </section>
  )
}

interface MembersTableProps {
  members: Member[]
  emptyText: string
}

function MembersTable({ members, emptyText }: MembersTableProps) {
  if (members.length === 0) {
    return <p className="admin-empty">{emptyText}</p>
  }

  return (
    <div className="admin-table-wrap">
      <table className="admin-table">
        <thead>
          <tr>
            <th scope="col">Email</th>
            <th scope="col">Username</th>
            <th scope="col">Email confirmed</th>
          </tr>
        </thead>
        <tbody>
          {members.map((member) => (
            <tr key={member.id}>
              <td>{member.email ?? '—'}</td>
              <td>{member.userName ?? '—'}</td>
              <td>
                <span className={member.emailConfirmed ? 'pill pill-ok' : 'pill pill-warn'}>
                  {member.emailConfirmed ? 'Yes' : 'No'}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
