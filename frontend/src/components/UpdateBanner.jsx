import { RefreshCw } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useRegisterSW } from 'virtual:pwa-register/react'

const CHECK_INTERVAL_MS = 60 * 60 * 1000 // 1 hora

export default function UpdateBanner() {
  const { t } = useTranslation()
  const {
    needRefresh: [needRefresh],
    updateServiceWorker,
  } = useRegisterSW({
    onRegisteredSW(_url, registration) {
      if (!registration) return
      setInterval(() => registration.update(), CHECK_INTERVAL_MS)
      document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') registration.update()
      })
    },
  })

  // No forzamos el reload solos: si cae justo en medio de una carga de datos
  // en curso, la deja atrapada en un estado vacío sin ningún error visible
  // (el usuario recién lo notaba al cerrar sesión y volver a entrar).
  if (!needRefresh) return null

  return (
    <div className="fixed top-0 left-0 right-0 z-50 flex items-center justify-center gap-3 px-4 py-2.5 bg-[#1A1A2E] border-b border-[#00FF87]/30">
      <RefreshCw size={14} className="text-[#00FF87] shrink-0" />
      <p className="flex-1 text-white text-xs font-semibold text-center">{t('update.available')}</p>
      <button
        onClick={() => updateServiceWorker(true)}
        className="shrink-0 px-3 py-1.5 rounded-lg bg-[#00FF87] text-black text-xs font-bold active:scale-95 transition-transform"
      >
        {t('update.reload')}
      </button>
    </div>
  )
}
