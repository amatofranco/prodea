import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../services/api'

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [loading, setLoading] = useState(false)
  const [done, setDone] = useState(false)
  const [error, setError] = useState('')
  const navigate = useNavigate()

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')
    if (password.length < 6) { setError('La contraseña debe tener al menos 6 caracteres'); return }
    if (password !== confirm) { setError('Las contraseñas no coinciden'); return }
    setLoading(true)
    try {
      await api.resetPassword(token, password)
      setDone(true)
      setTimeout(() => navigate('/login'), 2500)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  if (!token) {
    return (
      <div className="min-h-dvh flex flex-col items-center justify-center px-6 bg-[#0D0D0D] text-center space-y-4">
        <p className="text-red-400">Link inválido. Pedí uno nuevo.</p>
        <Link to="/forgot-password" className="text-[#00FF87] font-semibold">Recuperar contraseña</Link>
      </div>
    )
  }

  return (
    <div className="min-h-dvh flex flex-col items-center justify-center px-6 bg-[#0D0D0D]">
      <div className="mb-10 text-center">
        <h1
          className="text-5xl text-[#00FF87] neon-text"
          style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif', letterSpacing: '0.08em' }}
        >
          PRODEÁ
        </h1>
        <p className="text-[#8A8A9A] text-sm mt-1">Nueva contraseña</p>
      </div>

      {done ? (
        <div className="text-center space-y-3">
          <div className="text-4xl">✅</div>
          <p className="text-white font-semibold">¡Contraseña actualizada!</p>
          <p className="text-[#8A8A9A] text-sm">Redirigiendo al login...</p>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="w-full max-w-sm flex flex-col gap-4">
          <input
            type="password"
            placeholder="Nueva contraseña (mín. 6 caracteres)"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            className="w-full px-4 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] transition-colors"
          />
          <input
            type="password"
            placeholder="Repetí la contraseña"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            required
            className="w-full px-4 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] transition-colors"
          />

          {error && <p className="text-red-400 text-sm text-center">{error}</p>}

          <button
            type="submit"
            disabled={loading}
            className="w-full py-3 rounded-xl bg-[#00FF87] text-black font-bold text-base disabled:opacity-50 active:scale-95 transition-transform"
          >
            {loading ? 'Guardando...' : 'Guardar contraseña'}
          </button>
        </form>
      )}
    </div>
  )
}
