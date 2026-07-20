import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '../stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
  }
}

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'browse', component: () => import('../views/BrowseView.vue') },
  { path: '/login', name: 'login', component: () => import('../views/LoginView.vue') },
  { path: '/register', name: 'register', component: () => import('../views/RegisterView.vue') },
  { path: '/listings/new', name: 'listing-new', component: () => import('../views/ListingFormView.vue'), meta: { requiresAuth: true } },
  { path: '/listings/mine', name: 'listing-mine', component: () => import('../views/MyListingsView.vue'), meta: { requiresAuth: true } },
  { path: '/listings/:id/edit', name: 'listing-edit', component: () => import('../views/ListingFormView.vue'), meta: { requiresAuth: true } },
  { path: '/listings/:id', name: 'listing-detail', component: () => import('../views/ListingDetailView.vue') },
  { path: '/conversations', name: 'conversations-inbox', component: () => import('../views/ConversationsInboxView.vue'), meta: { requiresAuth: true } },
  { path: '/conversations/:id', name: 'conversation-thread', component: () => import('../views/ConversationThreadView.vue'), meta: { requiresAuth: true } },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  await auth.hydrate()

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { returnUrl: to.fullPath } }
  }
})

export default router
