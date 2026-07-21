import { createSignal, For, Show } from 'solid-js'
import { useNavigate } from '@solidjs/router'
import { useAuth } from '../context/AuthContext'
import { ApiError } from '../api/client'

export default function RegisterPage() {
  const navigate = useNavigate()
  const { register } = useAuth()

  const [email, setEmail] = createSignal('')
  const [displayName, setDisplayName] = createSignal('')
  const [password, setPassword] = createSignal('')
  const [errors, setErrors] = createSignal<string[]>([])
  const [submitting, setSubmitting] = createSignal(false)

  async function submit(e: SubmitEvent) {
    e.preventDefault()
    setSubmitting(true)
    setErrors([])
    try {
      await register(email(), displayName(), password())
      navigate('/')
    } catch (err) {
      setErrors(err instanceof ApiError && err.errors.length ? err.errors : [(err as Error).message])
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <>
      <h1>Register</h1>
      <Show when={errors().length > 0}>
        <ul class="error-list">
          <For each={errors()}>{(e) => <li>{e}</li>}</For>
        </ul>
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
          Display name
          <input value={displayName()} onInput={(e) => setDisplayName(e.currentTarget.value)} type="text" required />
        </label>
        <label>
          Password
          <input
            value={password()}
            onInput={(e) => setPassword(e.currentTarget.value)}
            type="password"
            required
            autocomplete="new-password"
          />
        </label>
        <button type="submit" disabled={submitting()}>
          Register
        </button>
      </form>
    </>
  )
}
