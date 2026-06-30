import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { api } from '../services/api'

export default function ForgotPasswordPage() {
  const { t } = useTranslation()
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
      <div className="mb-10 flex flex-col items-center gap-0">
        <img src="/logo-icon.png" alt="Prodea" className="w-[264px] h-[264px] object-contain" />
        <img src="/logo-wordmark.png" alt="Prodea" className="h-[70px] object-contain -mt-6" />
        <p className="text-[#8A8A9A] text-lg font-semibold tracking-widest uppercase -mt-3" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>{t('common.worldCup')}</p>
      </div>

      {sent ? (
        <div className="w-full max-w-sm text-center space-y-4">
          <div className="text-4xl">📬</div>
          <p className="text-white font-semibold">{t('forgotPassword.checkEmail')}</p>
          <p className="text-[#8A8A9A] text-sm">
            {t('forgotPassword.checkEmailDesc')}
          </p>
          <Link
            to="/login"
            className="block mt-4 text-[#00FF87] font-semibold text-sm"
          >
            {t('forgotPassword.backToLogin')}
          </Link>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="w-full max-w-sm flex flex-col gap-4">
          <p className="text-[#8A8A9A] text-sm text-center">
            {t('forgotPassword.instructions')}
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
            {loading ? t('common.sending') : t('forgotPassword.sendLink')}
          </button>

          <Link to="/login" className="text-center text-[#8A8A9A] text-sm hover:text-[#00FF87] transition-colors">
            {t('forgotPassword.backToLogin')}
          </Link>
        </form>
      )}
    </div>
  )
}
