import { useEffect } from 'react'
import { X } from 'lucide-react'
import AdBanner, { adsEnabled } from './AdBanner'

const AUTO_DISMISS_MS = 15000

export default function AdInterstitial({ onClose }) {
  useEffect(() => {
    if (!adsEnabled()) { onClose(); return }
    const timer = setTimeout(onClose, AUTO_DISMISS_MS)
    return () => clearTimeout(timer)
  }, [onClose])

  if (!adsEnabled()) return null

  return (
    <div
      className="fixed inset-0 z-[70] flex items-center justify-center bg-black/80"
      onClick={onClose}
    >
      <div
        className="relative flex flex-col items-center gap-3"
        onClick={(e) => e.stopPropagation()}
      >
        <button
          onClick={onClose}
          className="absolute -top-10 right-0 w-8 h-8 rounded-full bg-[#1A1A2E] flex items-center justify-center text-[#8A8A9A] active:text-white"
        >
          <X size={18} />
        </button>
        <span className="text-[10px] uppercase tracking-widest text-[#8A8A9A]">Publicidad</span>
        <AdBanner format="rectangle" />
      </div>
    </div>
  )
}
