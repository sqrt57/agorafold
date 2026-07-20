const API_BASE_URL = import.meta.env.VITE_API_BASE_URL

const MUTATING_METHODS = new Set(['POST', 'PUT', 'DELETE', 'PATCH'])

// ASP.NET's antiforgery token is bound to whichever identity (anonymous or a specific
// user) was active when it was issued — a token fetched before login/register/logout
// is rejected on the next mutating call once the identity changes. So this is fetched
// fresh per mutating request rather than cached.
async function getCsrfToken() {
  const response = await fetch(`${API_BASE_URL}/api/antiforgery/token`, {
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error('Failed to obtain a CSRF token.')
  }

  const { token } = await response.json()
  return token
}

export class ApiError extends Error {
  constructor(status, errors) {
    super(errors?.[0] ?? `Request failed with status ${status}`)
    this.status = status
    this.errors = errors ?? []
  }
}

export function imageUrl(path) {
  return path ? `${API_BASE_URL}${path}` : path
}

/**
 * @param {string} path
 * @param {{ method?: string, body?: unknown, isForm?: boolean }} [options]
 */
export async function apiFetch(path, { method = 'GET', body, isForm = false } = {}) {
  const headers = {}
  let requestBody

  if (body !== undefined) {
    if (isForm) {
      requestBody = body
    } else {
      headers['Content-Type'] = 'application/json'
      requestBody = JSON.stringify(body)
    }
  }

  if (MUTATING_METHODS.has(method)) {
    headers['X-CSRF-TOKEN'] = await getCsrfToken()
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    body: requestBody,
    credentials: 'include',
  })

  if (response.status === 204) {
    return null
  }

  const contentType = response.headers.get('content-type') ?? ''
  const data = contentType.includes('application/json') ? await response.json() : null

  if (!response.ok) {
    throw new ApiError(response.status, data?.errors)
  }

  return data
}
