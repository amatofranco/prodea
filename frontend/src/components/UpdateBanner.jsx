import { useRegisterSW } from 'virtual:pwa-register/react'

export default function UpdateBanner() {
  const {
    needRefresh: [needRefresh],
    updateServiceWorker,
  } = useRegisterSW()

  if (!needRefresh) return null

  return (
    <div className="fixed bottom-20 left-4 right-4 z-50 flex items-center gap-3 px-4 py-3 rounded-2xl bg-[#1A1A2E] border border-[#00FF87]/40 shadow-lg max-w-[448px] mx-auto">
      <span className="text-[#00FF87] text-lg">🔄</span>
      <p className="flex-1 text-sm text-white">
        Hay una nueva versión de Prodeá disponible
      </p>
      <button
        onClick={() => updateServiceWorker(true)}
        className="px-3 py-1.5 rounded-lg bg-[#00FF87] text-black text-xs font-bold shrink-0 active:scale-95 transition-transform"
      >
        Actualizar
      </button>
    </div>
  )
}
