import { writable } from 'svelte/store'

export interface Route {
  path: string
  query: URLSearchParams
  params: Record<string, string>
}

function parseLocation(): Route {
  const url = new URL(window.location.href)
  const listingMatch = url.pathname.match(/^\/listings\/(\d+)(?:\/(edit))?$/)
  const conversationMatch = url.pathname.match(/^\/conversations\/(\d+)$/)

  return {
    path: url.pathname,
    query: url.searchParams,
    params: {
      ...(listingMatch ? { id: listingMatch[1], ...(listingMatch[2] ? { action: listingMatch[2] } : {}) } : {}),
      ...(conversationMatch ? { id: conversationMatch[1] } : {}),
    },
  }
}

export const route = writable<Route>(parseLocation())

function updateRoute() {
  route.set(parseLocation())
  window.scrollTo({ top: 0, behavior: 'auto' })
}

export function navigate(path: string, replace = false): void {
  if (replace) window.history.replaceState({}, '', path)
  else window.history.pushState({}, '', path)
  updateRoute()
}

export function navigateLink(event: MouseEvent, path: string): void {
  if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return
  event.preventDefault()
  navigate(path)
}

window.addEventListener('popstate', updateRoute)
