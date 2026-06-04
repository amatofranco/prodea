import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'
import GoogleButton from '../components/GoogleButton'

export default function RegisterPage() {
  const [form, setForm] = useState({ username: '', email: '', password: '', firstName: '', lastName: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const setAuth = useAuthStore((s) => s.setAuth)
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const redirect = searchParams.get('redirect') || '/'

  async function handleGoogleCredential(credential) {
    setError('')
    try {
      const data = await api.googleLogin(credential)
      setAuth(data.token, data.user)
      navigate(redirect, { replace: true })
    } catch (err) {
      setError(err.message)
    }
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')
    if (!/^[a-zA-Z0-9_]+$/.test(form.username)) {
      setError('El nombre de usuario solo puede contener letras, números y guión bajo (_)')
      return
    }
    if (form.password.length < 6) { setError('La contraseña debe tener al menos 6 caracteres'); return }
    setLoading(true)
    try {
      const data = await api.register(form)
      setAuth(data.token, data.user)
      navigate(redirect, { replace: true })
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
      className="w-full px-4 py-2.5 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] transition-colors"
    />
  )

  return (
    <div className="min-h-dvh flex flex-col items-center justify-center px-6 py-6 bg-[#0D0D0D]">
      <div className="mb-5 flex flex-col items-center gap-0">
        <img src="/logo-icon.png" alt="Prodea" className="w-[140px] h-[140px] object-contain" />
        <img src="/logo-wordmark.png" alt="Prodea" className="h-[46px] object-contain -mt-3" />
        <p className="text-[#8A8A9A] text-sm font-semibold tracking-widest uppercase -mt-1.5" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>Mundial 2026</p>
      </div>

      <div className="w-full max-w-sm md:max-w-[400px] flex flex-col gap-3">
        <GoogleButton onCredential={handleGoogleCredential} text="Registrarse con Google" />

        <div className="flex items-center gap-3">
          <div className="flex-1 h-px bg-[#2A2A3E]" />
          <span className="text-[#8A8A9A] text-xs">o</span>
          <div className="flex-1 h-px bg-[#2A2A3E]" />
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          <div className="flex gap-3">
            {field('firstName', 'text', 'Nombre')}
            {field('lastName', 'text', 'Apellido')}
          </div>
          <div className="flex flex-col gap-1">
            {field('username', 'text', 'Nombre de usuario')}
            <p className="text-xs text-[#8A8A9A] px-1">Solo letras, números y _ (sin espacios ni símbolos)</p>
          </div>
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
        </form>

        <p className="text-center text-[#8A8A9A] text-sm">
          ¿Ya tenés cuenta?{' '}
          <Link to="/login" className="text-[#00FF87] font-semibold">Iniciá sesión</Link>
        </p>
      </div>

    </div>
  )
}
