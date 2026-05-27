import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID

export default function LoginPage() {
  const [form, setForm] = useState({ usernameOrEmail: '', password: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [googleLoading, setGoogleLoading] = useState(false)
  const setAuth = useAuthStore((s) => s.setAuth)
  const navigate = useNavigate()

  useEffect(() => {
    if (!GOOGLE_CLIENT_ID) return
    const script = document.createElement('script')
    script.src = 'https://accounts.google.com/gsi/client'
    script.async = true
    script.onload = () => {
      window.google?.accounts.id.initialize({
        client_id: GOOGLE_CLIENT_ID,
        callback: handleGoogleResponse,
      })
      window.google?.accounts.id.renderButton(
        document.getElementById('google-btn'),
        { theme: 'filled_black', size: 'large', width: 360, text: 'signin_with' }
      )
    }
    document.head.appendChild(script)
    return () => script.remove()
  }, [])

  async function handleGoogleResponse({ credential }) {
    setError('')
    setGoogleLoading(true)
    try {
      const data = await api.googleLogin(credential)
      setAuth(data.token, data.user)
      navigate('/')
    } catch (err) {
      setError(err.message)
    } finally {
      setGoogleLoading(false)
    }
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const data = await api.login(form)
      setAuth(data.token, data.user)
      navigate('/')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-dvh flex flex-col items-center justify-center px-6 bg-[#0D0D0D]">
      {/* Logo */}
      <div className="mb-10 text-center">
        <h1
          className="text-6xl text-[#00FF87] neon-text"
          style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif', letterSpacing: '0.08em' }}
        >
          PRODEÁ
        </h1>
        <p className="text-[#8A8A9A] text-sm mt-1">El prode del Mundial 2026</p>
      </div>

      <form onSubmit={handleSubmit} className="w-full max-w-sm flex flex-col gap-4">
        <input
          type="text"
          placeholder="Usuario o email"
          value={form.usernameOrEmail}
          onChange={(e) => setForm({ ...form, usernameOrEmail: e.target.value })}
          required
          className="w-full px-4 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] transition-colors"
        />
        <div className="relative">
          <input
            type="password"
            placeholder="Contraseña"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            required
            className="w-full px-4 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] transition-colors"
          />
        </div>

        <div className="flex justify-end -mt-2">
          <Link to="/forgot-password" className="text-xs text-[#8A8A9A] hover:text-[#00FF87] transition-colors">
            ¿Olvidaste tu contraseña?
          </Link>
        </div>

        {error && <p className="text-red-400 text-sm text-center">{error}</p>}

        <button
          type="submit"
          disabled={loading || googleLoading}
          className="w-full py-3 rounded-xl bg-[#00FF87] text-black font-bold text-base disabled:opacity-50 active:scale-95 transition-transform"
        >
          {loading ? 'Entrando...' : 'Entrar'}
        </button>

        {GOOGLE_CLIENT_ID && (
          <>
            <div className="flex items-center gap-3 my-1">
              <div className="flex-1 h-px bg-[#2A2A3E]" />
              <span className="text-[#8A8A9A] text-xs">o</span>
              <div className="flex-1 h-px bg-[#2A2A3E]" />
            </div>
            <div id="google-btn" className="flex justify-center" />
          </>
        )}

        <p className="text-center text-[#8A8A9A] text-sm">
          ¿No tenés cuenta?{' '}
          <Link to="/register" className="text-[#00FF87] font-semibold">
            Registrate
          </Link>
        </p>
      </form>
    </div>
  )
}
