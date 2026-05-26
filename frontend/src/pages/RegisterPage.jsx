import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'

export default function RegisterPage() {
  const [form, setForm] = useState({ username: '', email: '', password: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const setAuth = useAuthStore((s) => s.setAuth)
  const navigate = useNavigate()

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

        <p className="text-center text-[#8A8A9A] text-sm">
          ¿Ya tenés cuenta?{' '}
          <Link to="/login" className="text-[#00FF87] font-semibold">Iniciá sesión</Link>
        </p>
      </form>
    </div>
  )
}
