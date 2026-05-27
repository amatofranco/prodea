import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID

export default function RegisterPage() {
  const [form, setForm] = useState({ username: '', email: '', password: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
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
        locale: 'es',
      })
      window.google?.accounts.id.renderButton(
        document.getElementById('google-btn-register'),
        { theme: 'filled_black', size: 'large', width: 360, text: 'signup_with', locale: 'es' }
      )
    }
    document.head.appendChild(script)
    return () => script.remove()
  }, [])

  async function handleGoogleResponse({ credential }) {
    setError('')
    try {
      const data = await api.googleLogin(credential)
      setAuth(data.token, data.user)
      navigate('/')
    } catch (err) {
      setError(err.message)
    }
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')
    if (form.password.length < 6) { setError('La contraseña debe tener al menos 6 caracteres'); return }
    setLoading(true)
    try {
      const data = await api.register(form)
      setAuth(data.token, data.user)
      navigate('/')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  const field = (name, type, placeholder) => (
    <input
      type={type}
      placeholder={placeholder}
      value={form[name]}
      onChange={(e) => setForm({ ...form, [name]: e.target.value })}
      required
      className="w-full px-4 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] transition-colors"
    />
  )

  return (
    <div className="min-h-dvh flex flex-col items-center justify-center px-6 bg-[#0D0D0D]">
      <div className="mb-10 text-center">
        <h1
          className="text-6xl text-[#00FF87] neon-text"
          style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif', letterSpacing: '0.08em' }}
        >
          PRODEÁ
        </h1>
        <p className="text-[#8A8A9A] text-sm mt-1">Creá tu cuenta</p>
      </div>

      <form onSubmit={handleSubmit} className="w-full max-w-sm flex flex-col gap-4">
        {field('username', 'text', 'Nombre de usuario')}
        {field('email', 'email', 'Email')}
        {field('password', 'password', 'Contraseña (mín. 6 caracteres)')}

        {error && <p className="text-red-400 text-sm text-center">{error}</p>}

        <button
          type="submit"
          disabled={loading}
          className="w-full py-3 rounded-xl bg-[#00FF87] text-black font-bold text-base disabled:opacity-50 active:scale-95 transition-transform"
        >
          {loading ? 'Creando cuenta...' : 'Crear cuenta'}
        </button>

        {GOOGLE_CLIENT_ID && (
          <>
            <div className="flex items-center gap-3 my-1">
              <div className="flex-1 h-px bg-[#2A2A3E]" />
              <span className="text-[#8A8A9A] text-xs">o</span>
              <div className="flex-1 h-px bg-[#2A2A3E]" />
            </div>
            <div id="google-btn-register" className="flex justify-center" />
          </>
        )}

        <p className="text-center text-[#8A8A9A] text-sm">
          ¿Ya tenés cuenta?{' '}
          <Link to="/login" className="text-[#00FF87] font-semibold">Iniciá sesión</Link>
        </p>
      </form>
    </div>
  )
}
