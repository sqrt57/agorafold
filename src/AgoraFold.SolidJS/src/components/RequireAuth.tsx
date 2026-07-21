import { Navigate, useLocation } from '@solidjs/router'
import { Show, type JSX } from 'solid-js'
import { useAuth } from '../context/AuthContext'

export default function RequireAuth(props: { children: JSX.Element }) {
  const { isAuthenticated, hydrated } = useAuth()
  const location = useLocation()

  return (
    <Show when={hydrated()}>
      <Show
        when={isAuthenticated()}
        fallback={<Navigate href={`/login?returnUrl=${encodeURIComponent(location.pathname + location.search)}`} />}
      >
        {props.children}
      </Show>
    </Show>
  )
}
