import { useEffect } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { api } from '../services/api'
import LandingPage from '../pages/LandingPage'

export default function PrivateRoute() {
  const token = useAuthStore((s) => s.token)
  const setAuth = useAuthStore((s) => s.setAuth)
  const location = useLocation()

  useEffect(() => {
    if (!token) return
    api.getMe().then((user) => setAuth(token, user)).catch(() => {})
  }, [])

  if (token) return <Outlet />

  // Visitantes sin sesión que entran a la raíz ven la landing pública
  // (con contenido real, en vez de quedar redirigidos directo a /login)
  if (location.pathname === '/') return <LandingPage />

  return <Navigate to="/login" replace />
}
