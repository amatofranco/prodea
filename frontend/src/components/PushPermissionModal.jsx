import { Bell, X } from 'lucide-react'

export default function PushPermissionModal({ onActivate, onDismiss }) {
  return (
    <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center">
      <div className="absolute inset-0 bg-black/60" onClick={onDismiss} />
      <div className="relative w-full sm:max-w-sm mx-4 mb-6 sm:mb-0 bg-[#1A1A2E] rounded-2xl border border-[#2A2A3E] p-6 shadow-xl">
        <button
          onClick={onDismiss}
          className="absolute top-4 right-4 text-[#8A8A9A] hover:text-white transition-colors"
        >
          <X size={18} />
        </button>

        <div className="flex flex-col items-center text-center gap-4">
          <div className="w-14 h-14 rounded-full bg-[#00FF87]/10 flex items-center justify-center">
            <Bell size={26} className="text-[#00FF87]" />
          </div>

          <div>
            <h2 className="text-white font-bold text-lg leading-tight mb-1">
              Activá las notificaciones
            </h2>
            <p className="text-[#8A8A9A] text-sm leading-relaxed">
              Te avisamos cuando arrancan los partidos del día y cuando termina la jornada. Dos notificaciones por día, nada más.
            </p>
          </div>

          <div className="flex flex-col gap-2 w-full">
            <button
              onClick={onActivate}
              className="w-full py-3 rounded-xl bg-[#00FF87] text-black font-bold text-sm hover:bg-[#00e07a] transition-colors"
            >
              Activar notificaciones
            </button>
            <button
              onClick={onDismiss}
              className="w-full py-2.5 rounded-xl text-[#8A8A9A] text-sm hover:text-white transition-colors"
            >
              Ahora no
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
