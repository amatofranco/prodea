import { useEffect, useState } from 'react'

const ADSENSE_CLIENT = import.meta.env.VITE_ADSENSE_CLIENT
const ADSENSE_SLOT = import.meta.env.VITE_ADSENSE_SLOT
const CLOSE_DELAY_MS = 3000

export default function AdInterstitial({ onClose }) {
  const [countdown, setCountdown] = useState(Math.ceil(CLOSE_DELAY_MS / 1000))
  const [canClose, setCanClose] = useState(false)

  useEffect(() => {
    const interval = setInterval(() => {
      setCountdown((c) => {
        if (c <= 1) {
          clearInterval(interval)
          setCanClose(true)
          return 0
        }
        return c - 1
      })
    }, 1000)
    return () => clearInterval(interval)
  }, [])

  useEffect(() => {
    if (!ADSENSE_CLIENT) return
    try {
      ;(window.adsbygoogle = window.adsbygoogle || []).push({})
    } catch {}
  }, [])

  return (
    <div className="fixed inset-0 z-50 bg-black/90 flex flex-col items-center justify-center px-4">
      <div className="w-full max-w-sm flex flex-col items-center gap-4">
        <p className="text-[#8A8A9A] text-[10px] uppercase tracking-widest">Publicidad</p>

        {ADSENSE_CLIENT ? (
          <ins
            className="adsbygoogle"
            style={{ display: 'block', width: '100%', minHeight: 250 }}
            data-ad-client={ADSENSE_CLIENT}
            data-ad-slot={ADSENSE_SLOT}
            data-ad-format="rectangle"
            data-full-width-responsive="true"
          />
        ) : (
          <div className="w-full h-[250px] rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] flex flex-col items-center justify-center gap-2">
            <span className="text-3xl">📢</span>
            <p className="text-[#8A8A9A] text-sm">Espacio publicitario</p>
            <p className="text-[#3A3A4E] text-xs">ca-pub-XXXXXXXXXXXXXXXX</p>
          </div>
        )}

        {canClose ? (
          <button
            onClick={onClose}
            className="w-full py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white text-sm font-semibold active:scale-95 transition-transform"
          >
            Cerrar
          </button>
        ) : (
          <p className="text-[#8A8A9A] text-xs">
            Podés cerrar en {countdown}s
          </p>
        )}
      </div>
    </div>
  )
}
