import { defineStore } from 'pinia'
import * as accountApi from '../api/account'
import type { AuthUser } from '../api/types'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null as AuthUser | null,
    hydrated: false,
  }),
  getters: {
    isAuthenticated: (state) => state.user !== null,
  },
  actions: {
    async hydrate() {
      if (this.hydrated) return
      this.user = await accountApi.me()
      this.hydrated = true
    },
    async register(email: string, displayName: string, password: string) {
      this.user = await accountApi.register(email, displayName, password)
    },
    async login(email: string, password: string, rememberMe: boolean) {
      this.user = await accountApi.login(email, password, rememberMe)
    },
    async logout() {
      await accountApi.logout()
      this.user = null
    },
  },
})
