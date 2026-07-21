<script lang="ts">
  import { ApiError } from '../api/client'
  import { login } from '../auth'
  import { navigate, route } from '../router'

  let email = ''
  let password = ''
  let rememberMe = false
  let error = ''
  let submitting = false

  async function submit(event: SubmitEvent) {
    event.preventDefault()
    submitting = true
    error = ''
    try {
      await login(email, password, rememberMe)
      navigate($route.query.get('returnUrl') ?? '/')
    } catch (exception) {
      error = exception instanceof ApiError ? (exception.errors[0] ?? exception.message) : (exception as Error).message
    } finally {
      submitting = false
    }
  }
</script>

<h1>Log in</h1>
{#if error}<p class="error">{error}</p>{/if}
<form onsubmit={submit}>
  <label>
    Email
    <input bind:value={email} type="email" required autocomplete="username" />
  </label>
  <label>
    Password
    <input bind:value={password} type="password" required autocomplete="current-password" />
  </label>
  <label class="checkbox-label">
    <input bind:checked={rememberMe} type="checkbox" />
    Remember me
  </label>
  <button type="submit" disabled={submitting}>Log in</button>
</form>
