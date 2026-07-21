import { writable } from 'svelte/store'
import * as accountApi from './api/account'
import type { AuthUser } from './api/types'

export const user = writable<AuthUser | null>(null)
export const hydrated = writable(false)

let hydratePromise: Promise<void> | null = null

export function hydrate(): Promise<void> {
  if (!hydratePromise) {
    hydratePromise = accountApi.me().then((loadedUser) => {
      user.set(loadedUser)
      hydrated.set(true)
    })
  }
  return hydratePromise
}

export async function register(email: string, displayName: string, password: string): Promise<void> {
  user.set(await accountApi.register(email, displayName, password))
}

export async function login(email: string, password: string, rememberMe: boolean): Promise<void> {
  user.set(await accountApi.login(email, password, rememberMe))
}

export async function logout(): Promise<void> {
  await accountApi.logout()
  user.set(null)
}
