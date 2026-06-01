import { useEffect } from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { api } from '../services/api'

export default function PrivateRoute() {
  const token = useAuthStore((s) => s.token)
  const setAuth = useAuthStore((s) => s.setAuth)

  useEffect(() => {
    if (!token) return
    api.getMe().then((user) => setAuth(token, user)).catch(() => {})
  }, [])

  return token ? <Outlet /> : <Navigate to="/login" replace />
}
