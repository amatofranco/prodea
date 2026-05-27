import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../services/api'

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [sent, setSent] = useState(false)
  const [error, setError] = useState('')

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await api.forgotPassword(email)
      setSent(true)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
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
        <p className="text-[#8A8A9A] text-sm mt-1">Recuperá tu contraseña</p>
      </div>

      {sent ? (
        <div className="w-full max-w-sm text-center space-y-4">
          <div className="text-4xl">📬</div>
          <p className="text-white font-semibold">¡Revisá tu email!</p>
          <p className="text-[#8A8A9A] text-sm">
            Si existe una cuenta con ese email, te enviamos un link para recuperar tu contraseña. Revisá también el spam.
          </p>
          <Link
            to="/login"
            className="block mt-4 text-[#00FF87] font-semibold text-sm"
          >
            ← Volver al login
          </Link>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="w-full max-w-sm flex flex-col gap-4">
          <p className="text-[#8A8A9A] text-sm text-center">
            Ingresá el email de tu cuenta y te mandamos un link para crear una nueva contraseña.
          </p>
          <input
            type="email"
            placeholder="tu@email.com"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            className="w-full px-4 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] transition-colors"
          />

          {error && <p className="text-red-400 text-sm text-center">{error}</p>}

          <button
            type="submit"
            disabled={loading}
            className="w-full py-3 rounded-xl bg-[#00FF87] text-black font-bold text-base disabled:opacity-50 active:scale-95 transition-transform"
          >
            {loading ? 'Enviando...' : 'Enviar link'}
          </button>

          <Link to="/login" className="text-center text-[#8A8A9A] text-sm hover:text-[#00FF87] transition-colors">
            ← Volver al login
          </Link>
        </form>
      )}
    </div>
  )
}
