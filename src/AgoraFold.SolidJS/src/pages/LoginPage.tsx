import { createSignal, Show } from 'solid-js'
import { useNavigate, useSearchParams } from '@solidjs/router'
import { useAuth } from '../context/AuthContext'

export default function LoginPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { login } = useAuth()

  const [email, setEmail] = createSignal('')
  const [password, setPassword] = createSignal('')
  const [rememberMe, setRememberMe] = createSignal(false)
  const [error, setError] = createSignal('')
  const [submitting, setSubmitting] = createSignal(false)

  async function submit(e: SubmitEvent) {
    e.preventDefault()
    setSubmitting(true)
    setError('')
    try {
      await login(email(), password(), rememberMe())
      const returnUrl = searchParams.returnUrl
      navigate(Array.isArray(returnUrl) ? (returnUrl[0] ?? '/') : returnUrl || '/')
    } catch (err) {
      setError((err as Error).message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <>
      <h1>Log in</h1>
      <Show when={error()}>
        <p class="error">{error()}</p>
      </Show>
      <form onSubmit={submit}>
        <label>
          Email
          <input
            value={email()}
            onInput={(e) => setEmail(e.currentTarget.value)}
            type="email"
            required
            autocomplete="username"
          />
        </label>
        <label>
          Password
          <input
            value={password()}
            onInput={(e) => setPassword(e.currentTarget.value)}
            type="password"
            required
            autocomplete="current-password"
          />
        </label>
        <label class="checkbox-label">
          <input
            checked={rememberMe()}
            onChange={(e) => setRememberMe(e.currentTarget.checked)}
            type="checkbox"
          />
          Remember me
        </label>
        <button type="submit" disabled={submitting()}>
          Log in
        </button>
      </form>
    </>
  )
}
