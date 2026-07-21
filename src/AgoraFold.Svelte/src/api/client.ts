const API_BASE_URL = import.meta.env.VITE_API_BASE_URL

const MUTATING_METHODS = new Set(['POST', 'PUT', 'DELETE', 'PATCH'])

// ASP.NET antiforgery tokens are bound to the current identity, so fetch a fresh
// token for every mutation instead of caching one across login/logout boundaries.
async function getCsrfToken(): Promise<string> {
  const response = await fetch(`${API_BASE_URL}/api/antiforgery/token`, { credentials: 'include' })

  if (!response.ok) throw new Error('Failed to obtain a CSRF token.')

  const { token } = (await response.json()) as { token: string }
  return token
}

export class ApiError extends Error {
  status: number
  errors: string[]

  constructor(status: number, errors?: string[]) {
    super(errors?.[0] ?? `Request failed with status ${status}`)
    this.status = status
    this.errors = errors ?? []
  }
}

export function imageUrl(path: string | null | undefined): string | undefined {
  return path ? `${API_BASE_URL}${path}` : undefined
}

interface ApiFetchOptions {
  method?: string
  body?: unknown
  isForm?: boolean
}

export async function apiFetch<T>(
  path: string,
  { method = 'GET', body, isForm = false }: ApiFetchOptions = {},
): Promise<T> {
  const headers: Record<string, string> = {}
  let requestBody: BodyInit | undefined

  if (body !== undefined) {
    if (isForm) requestBody = body as FormData
    else {
      headers['Content-Type'] = 'application/json'
      requestBody = JSON.stringify(body)
    }
  }

  if (MUTATING_METHODS.has(method)) headers['X-CSRF-TOKEN'] = await getCsrfToken()

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    body: requestBody,
    credentials: 'include',
  })

  if (response.status === 204) return null as T

  const contentType = response.headers.get('content-type') ?? ''
  const data = contentType.includes('application/json') ? await response.json() : null

  if (!response.ok) throw new ApiError(response.status, data?.errors)

  return data as T
}
