/* @refresh reload */
import { render } from 'solid-js/web'
import { Router } from '@solidjs/router'
import type { JSX } from 'solid-js'
import './style.css'
import App from './App.tsx'
import NavBar from './components/NavBar.tsx'
import { AuthProvider } from './context/AuthContext.tsx'

function Layout(props: { children?: JSX.Element }) {
  return (
    <AuthProvider>
      <NavBar />
      <main class="container">{props.children}</main>
    </AuthProvider>
  )
}

const root = document.getElementById('root')

render(
  () => (
    <Router root={Layout}>
      <App />
    </Router>
  ),
  root!,
)
