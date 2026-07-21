import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function RequireAuth({ children }: { children: ReactNode }) {
  const { isAuthenticated, hydrated } = useAuth()
  const location = useLocation()

  if (!hydrated) return null

  if (!isAuthenticated) {
    const returnUrl = `${location.pathname}${location.search}`
    return <Navigate to={`/login?returnUrl=${encodeURIComponent(returnUrl)}`} replace />
  }

  return children
}
