import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import * as accountApi from '../api/account'
import type { AuthUser } from '../api/types'

interface AuthContextValue {
  user: AuthUser | null
  hydrated: boolean
  isAuthenticated: boolean
  register: (email: string, displayName: string, password: string) => Promise<void>
  login: (email: string, password: string, rememberMe: boolean) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [hydrated, setHydrated] = useState(false)
  const hydratePromise = useRef<Promise<void> | null>(null)

  useEffect(() => {
    if (!hydratePromise.current) {
      hydratePromise.current = accountApi.me().then((loadedUser) => {
        setUser(loadedUser)
        setHydrated(true)
      })
    }
  }, [])

  const register = useCallback(async (email: string, displayName: string, password: string) => {
    setUser(await accountApi.register(email, displayName, password))
  }, [])

  const login = useCallback(async (email: string, password: string, rememberMe: boolean) => {
    setUser(await accountApi.login(email, password, rememberMe))
  }, [])

  const logout = useCallback(async () => {
    await accountApi.logout()
    setUser(null)
  }, [])

  const value = useMemo(
    () => ({ user, hydrated, isAuthenticated: user !== null, register, login, logout }),
    [user, hydrated, register, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within an AuthProvider')
  return context
}
