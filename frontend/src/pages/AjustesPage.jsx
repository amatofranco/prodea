import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Bell, BellOff, LogOut, MessageSquare, Send, HelpCircle, ChevronRight } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useAuthStore } from '../store/authStore'
import { usePushNotifications } from '../hooks/usePushNotifications'
import { api } from '../services/api'

const MAX_MSG = 500

export default function AjustesPage() {
  const { t } = useTranslation()
  const logout = useAuthStore((s) => s.logout)
  const navigate = useNavigate()
  const { supported, isStandalone, subscribed, subscribe, unsubscribe } = usePushNotifications()
  const [optimistic, setOptimistic] = useState(subscribed)
  const [message, setMessage] = useState('')
  const [sending, setSending] = useState(false)
  const [sent, setSent] = useState(false)

  useEffect(() => { setOptimistic(subscribed) }, [subscribed])

  function handleLogout() {
    logout()
    navigate('/login')
  }

  async function handleTogglePush() {
    const next = !optimistic
    setOptimistic(next)
    if (next) {
      await subscribe()
    } else {
      await unsubscribe()
    }
  }

  async function handleSendContact() {
    if (!message.trim()) return
    setSending(true)
    try {
      await api.sendContact(message.trim())
      setSent(true)
      setMessage('')
      setTimeout(() => setSent(false), 4000)
    } catch {
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D] px-4 pt-14 pb-6">
      <h1 className="text-white text-2xl font-bold mb-8">{t('settings.title')}</h1>

      <div className="flex flex-col gap-3">
        {supported && isStandalone && (
          <div className="flex items-center justify-between bg-[#1A1A2E] rounded-2xl px-4 py-4 border border-[#2A2A3E]">
            <div className="flex items-center gap-3">
              {optimistic
                ? <Bell size={20} className="text-[#00FF87]" />
                : <BellOff size={20} className="text-[#8A8A9A]" />
              }
              <div>
                <p className="text-white text-sm font-semibold">{t('settings.notifications')}</p>
                <p className="text-[#8A8A9A] text-xs mt-0.5">
                  {optimistic ? t('settings.enabled') : t('settings.disabled')}
                </p>
              </div>
            </div>
            <button
              onClick={handleTogglePush}
              className={`relative w-11 h-6 rounded-full transition-colors duration-200 ${optimistic ? 'bg-[#00FF87]' : 'bg-[#2A2A3E]'}`}
            >
              <span className={`absolute top-0.5 left-0.5 w-5 h-5 rounded-full bg-white shadow transition-transform duration-200 ${optimistic ? 'translate-x-5' : 'translate-x-0'}`} />
            </button>
          </div>
        )}

        {/* Contacto */}
        <div className="bg-[#1A1A2E] rounded-2xl px-4 py-4 border border-[#2A2A3E] flex flex-col gap-3">
          <div className="flex items-center gap-3">
            <MessageSquare size={20} className="text-[#00FF87]" />
            <p className="text-white text-sm font-semibold">{t('settings.contact')}</p>
          </div>
          <textarea
            value={message}
            onChange={(e) => setMessage(e.target.value.slice(0, MAX_MSG))}
            placeholder={t('settings.contactPlaceholder')}
            rows={3}
            className="w-full px-3 py-2.5 rounded-xl bg-[#0D0D0D] border border-[#2A2A3E] text-white text-sm placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87]/40 resize-none"
          />
          <div className="flex items-center justify-between">
            <span className="text-[#8A8A9A] text-xs">{message.length}/{MAX_MSG}</span>
            <button
              onClick={handleSendContact}
              disabled={!message.trim() || sending}
              className={`flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-bold transition-colors ${
                sent
                  ? 'bg-[#00FF87]/20 text-[#00FF87]'
                  : 'bg-[#00FF87] text-black disabled:opacity-40'
              }`}
            >
              <Send size={14} />
              {sent ? t('common.sent') : sending ? t('common.sending') : t('common.send')}
            </button>
          </div>
        </div>

        <button
          onClick={() => navigate('/how-to-play')}
          className="flex items-center gap-3 bg-[#1A1A2E] rounded-2xl px-4 py-4 border border-[#2A2A3E] text-left w-full hover:border-[#00FF87]/40 transition-colors"
        >
          <HelpCircle size={20} className="text-[#00FF87]" />
          <span className="text-white text-sm font-semibold flex-1">{t('settings.howToPlay')}</span>
          <ChevronRight size={18} className="text-[#8A8A9A]" />
        </button>

        <button
          onClick={handleLogout}
          className="flex items-center gap-3 bg-[#1A1A2E] rounded-2xl px-4 py-4 border border-[#2A2A3E] text-left w-full hover:border-red-500/40 transition-colors"
        >
          <LogOut size={20} className="text-red-400" />
          <span className="text-red-400 text-sm font-semibold">{t('settings.logout')}</span>
        </button>
      </div>

      <p className="text-center text-[#8A8A9A] text-[10px] mt-8">Prodea v1.{__APP_VERSION__.padStart(5, '0')}</p>
    </div>
  )
}
