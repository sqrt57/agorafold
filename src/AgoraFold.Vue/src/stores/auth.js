import { defineStore } from 'pinia'
import * as accountApi from '../api/account'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null,
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
    async register(email, displayName, password) {
      this.user = await accountApi.register(email, displayName, password)
    },
    async login(email, password, rememberMe) {
      this.user = await accountApi.login(email, password, rememberMe)
    },
    async logout() {
      await accountApi.logout()
      this.user = null
    },
  },
})
