import { A, useNavigate } from '@solidjs/router'
import { Show } from 'solid-js'
import { useAuth } from '../context/AuthContext'

export default function NavBar() {
  const { user, isAuthenticated, logout } = useAuth()
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  return (
    <nav class="navbar">
      <A class="brand" href="/">AgoraFold</A>
      <div class="links">
        <A href="/">Browse</A>
        <Show
          when={isAuthenticated()}
          fallback={
            <>
              <A href="/login">Log in</A>
              <A href="/register">Register</A>
            </>
          }
        >
          <A href="/listings/new">Post a listing</A>
          <A href="/listings/mine">My listings</A>
          <A href="/conversations">Messages</A>
          <span class="muted">{user()?.displayName}</span>
          <button class="secondary" onClick={handleLogout}>Log out</button>
        </Show>
      </div>
    </nav>
  )
}
