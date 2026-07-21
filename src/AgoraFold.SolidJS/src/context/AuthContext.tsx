import { createContext, useContext, createSignal, onMount, type Accessor, type JSX } from 'solid-js'
import * as accountApi from '../api/account'
import type { AuthUser } from '../api/types'

interface AuthContextValue {
  user: Accessor<AuthUser | null>
  hydrated: Accessor<boolean>
  isAuthenticated: Accessor<boolean>
  register: (email: string, displayName: string, password: string) => Promise<void>
  login: (email: string, password: string, rememberMe: boolean) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue>()

export function AuthProvider(props: { children: JSX.Element }) {
  const [user, setUser] = createSignal<AuthUser | null>(null)
  const [hydrated, setHydrated] = createSignal(false)

  onMount(async () => {
    setUser(await accountApi.me())
    setHydrated(true)
  })

  const value: AuthContextValue = {
    user,
    hydrated,
    isAuthenticated: () => user() !== null,
    register: async (email, displayName, password) => {
      setUser(await accountApi.register(email, displayName, password))
    },
    login: async (email, password, rememberMe) => {
      setUser(await accountApi.login(email, password, rememberMe))
    },
    logout: async () => {
      await accountApi.logout()
      setUser(null)
    },
  }

  return <AuthContext.Provider value={value}>{props.children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within an AuthProvider')
  return context
}
