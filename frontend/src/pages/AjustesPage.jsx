import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Bell, BellOff, LogOut } from 'lucide-react'
import { useAuthStore } from '../store/authStore'
import { usePushNotifications } from '../hooks/usePushNotifications'

export default function AjustesPage() {
  const logout = useAuthStore((s) => s.logout)
  const navigate = useNavigate()
  const { supported, subscribed, subscribe, unsubscribe } = usePushNotifications()
  const [optimistic, setOptimistic] = useState(subscribed)

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

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D] px-4 pt-14 pb-6">
      <h1 className="text-white text-2xl font-bold mb-8">Ajustes</h1>

      <div className="flex flex-col gap-3">
        {supported && (
          <div className="flex items-center justify-between bg-[#1A1A2E] rounded-2xl px-4 py-4 border border-[#2A2A3E]">
            <div className="flex items-center gap-3">
              {optimistic
                ? <Bell size={20} className="text-[#00FF87]" />
                : <BellOff size={20} className="text-[#8A8A9A]" />
              }
              <div>
                <p className="text-white text-sm font-semibold">Notificaciones</p>
                <p className="text-[#8A8A9A] text-xs mt-0.5">
                  {optimistic ? 'Activadas' : 'Desactivadas'}
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

        <button
          onClick={handleLogout}
          className="flex items-center gap-3 bg-[#1A1A2E] rounded-2xl px-4 py-4 border border-[#2A2A3E] text-left w-full hover:border-red-500/40 transition-colors"
        >
          <LogOut size={20} className="text-red-400" />
          <span className="text-red-400 text-sm font-semibold">Cerrar sesión</span>
        </button>
      </div>
    </div>
  )
}
