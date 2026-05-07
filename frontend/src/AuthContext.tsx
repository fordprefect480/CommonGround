import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { fetchMe, logout as apiLogout, type Me } from './api/auth'

export type AuthState =
  | { status: 'loading' }
  | { status: 'anonymous' }
  | { status: 'authenticated'; me: Me }

interface AuthContextValue {
  state: AuthState
  refresh: () => Promise<Me | null>
  signOut: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ status: 'loading' })

  const refresh = useCallback(async () => {
    try {
      const me = await fetchMe()
      setState(me ? { status: 'authenticated', me } : { status: 'anonymous' })
      return me
    } catch {
      setState({ status: 'anonymous' })
      return null
    }
  }, [])

  const signOut = useCallback(async () => {
    await apiLogout()
    setState({ status: 'anonymous' })
  }, [])

  useEffect(() => {
    refresh()
  }, [refresh])

  return <AuthContext.Provider value={{ state, refresh, signOut }}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
