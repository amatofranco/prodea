import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'
import GoogleButton from '../components/GoogleButton'
import InAppBrowserBanner from '../components/InAppBrowserBanner'

const IS_TESTING = import.meta.env.VITE_APP_ENV === 'testing'

export default function LoginPage() {
  const { t } = useTranslation()
  const [form, setForm] = useState({ usernameOrEmail: '', password: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const setAuth = useAuthStore((s) => s.setAuth)
  const [searchParams] = useSearchParams()
  const redirect = searchParams.get('redirect') || '/'

  async function handleGoogleCredential(credential) {
    setError('')
    try {
      const data = await api.googleLogin(credential)
      setAuth(data.token, data.user)
      window.location.replace(redirect)
    } catch (err) {
      setError(t('errors.' + err.message, { ...err.data, defaultValue: t('errors.generic') }))
    }
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const data = await api.login(form)
      setAuth(data.token, data.user)
      window.location.replace(redirect)
    } catch (err) {
      setError(t('errors.' + err.message, { ...err.data, defaultValue: t('errors.generic') }))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-dvh flex flex-col bg-[#0D0D0D]">
      <InAppBrowserBanner />
      <div className="flex flex-col flex-1 items-center justify-center px-6 py-6">
      <div className="mb-6 flex flex-col items-center gap-0">
        <img src="/logo-icon.png" alt="Prodea" className="w-[180px] h-[180px] md:w-[240px] md:h-[240px] object-contain" />
        <img src="/logo-wordmark.png" alt="Prodea" className="h-[54px] md:h-[72px] object-contain -mt-4" />
        <p className="text-[#8A8A9A] text-base font-semibold tracking-widest uppercase -mt-2" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>{t('common.worldCup')}</p>
        {IS_TESTING && (
          <span className="mt-2 px-3 py-0.5 rounded-full text-xs font-bold tracking-widest uppercase bg-[#FF6B35]/20 text-[#FF6B35] border border-[#FF6B35]/40">
            Testing
          </span>
        )}
      </div>

      <div className="w-full max-w-sm md:max-w-[400px] flex flex-col gap-4">
        <GoogleButton onCredential={handleGoogleCredential} text={t('auth.loginWithGoogle')} />

        <div className="flex items-center gap-3">
          <div className="flex-1 h-px bg-[#2A2A3E]" />
          <span className="text-[#8A8A9A] text-xs">{t('common.or')}</span>
          <div className="flex-1 h-px bg-[#2A2A3E]" />
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <input
            type="text"
            placeholder={t('auth.userOrEmail')}
            value={form.usernameOrEmail}
            onChange={(e) => setForm({ ...form, usernameOrEmail: e.target.value })}
            required
            className="w-full px-4 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] transition-colors"
          />
          <div className="relative">
            <input
              type="password"
              placeholder={t('auth.password')}
              value={form.password}
              onChange={(e) => setForm({ ...form, password: e.target.value })}
              required
              className="w-full px-4 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] transition-colors"
            />
          </div>

          <div className="flex justify-end -mt-2">
            <Link to="/forgot-password" className="text-xs text-[#8A8A9A] hover:text-[#00FF87] transition-colors">
              {t('auth.forgotPassword')}
            </Link>
          </div>

          {error && <p className="text-red-400 text-sm text-center">{error}</p>}

          <button
            type="submit"
            disabled={loading}
            className="w-full py-3 rounded-xl bg-[#00FF87] text-black font-bold text-base disabled:opacity-50 active:scale-95 transition-transform"
          >
            {loading ? t('auth.loggingIn') : t('auth.login')}
          </button>
        </form>

        <p className="text-center text-[#8A8A9A] text-sm">
          {t('auth.noAccount')}{' '}
          <Link to="/register" className="text-[#00FF87] font-semibold">
            {t('auth.signUp')}
          </Link>
        </p>

        <p className="text-center text-[#8A8A9A] text-xs mt-2">
          <Link to="/privacidad" className="hover:text-white transition-colors">
            {t('common.privacyPolicy')}
          </Link>
        </p>
      </div>
      </div>
    </div>
  )
}
