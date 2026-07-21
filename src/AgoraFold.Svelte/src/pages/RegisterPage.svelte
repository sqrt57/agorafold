<script lang="ts">
  import { ApiError } from '../api/client'
  import { register } from '../auth'
  import { navigate } from '../router'

  let email = ''
  let displayName = ''
  let password = ''
  let errors: string[] = []
  let submitting = false

  async function submit(event: SubmitEvent) {
    event.preventDefault()
    submitting = true
    errors = []
    try {
      await register(email, displayName, password)
      navigate('/')
    } catch (exception) {
      errors = exception instanceof ApiError && exception.errors.length ? exception.errors : [(exception as Error).message]
    } finally {
      submitting = false
    }
  }
</script>

<h1>Register</h1>
{#if errors.length}
  <ul class="error-list">
    {#each errors as error}<li>{error}</li>{/each}
  </ul>
{/if}
<form onsubmit={submit}>
  <label>
    Email
    <input bind:value={email} type="email" required autocomplete="username" />
  </label>
  <label>
    Display name
    <input bind:value={displayName} type="text" required />
  </label>
  <label>
    Password
    <input bind:value={password} type="password" required autocomplete="new-password" />
  </label>
  <button type="submit" disabled={submitting}>Register</button>
</form>
