import { useEffect, useState } from 'react'
import { X, Share, Plus } from 'lucide-react'

const STORAGE_KEY = 'prodea_install_dismissed'
const DISMISS_DAYS = 7

function isStandalone() {
  return window.matchMedia('(display-mode: standalone)').matches
    || window.navigator.standalone === true
}

function isIOS() {
  return /iphone|ipad|ipod/i.test(navigator.userAgent)
}

export default function InstallBanner() {
  const [show, setShow] = useState(false)
  const [deferredPrompt, setDeferredPrompt] = useState(null)
  const [ios, setIos] = useState(false)

  useEffect(() => {
    if (isStandalone()) return

    const dismissed = localStorage.getItem(STORAGE_KEY)
    if (dismissed) {
      const until = new Date(dismissed)
      if (new Date() < until) return
    }

    if (isIOS()) {
      setIos(true)
      setShow(true)
      return
    }

    const handler = (e) => {
      e.preventDefault()
      setDeferredPrompt(e)
      setShow(true)
    }
    window.addEventListener('beforeinstallprompt', handler)
    return () => window.removeEventListener('beforeinstallprompt', handler)
  }, [])

  function dismiss() {
    const until = new Date()
    until.setDate(until.getDate() + DISMISS_DAYS)
    localStorage.setItem(STORAGE_KEY, until.toISOString())
    setShow(false)
  }

  async function install() {
    if (!deferredPrompt) return
    deferredPrompt.prompt()
    const { outcome } = await deferredPrompt.userChoice
    if (outcome === 'accepted') {
      localStorage.setItem(STORAGE_KEY, new Date(9999, 0).toISOString())
    }
    setShow(false)
    setDeferredPrompt(null)
  }

  if (!show) return null

  return (
    <div className="mx-4 mb-4 rounded-2xl bg-[#1A1A2E] border border-[#00FF87]/30 p-4 flex gap-3 items-start">
      <img src="/logo-icon.png" alt="" className="w-10 h-10 object-contain shrink-0" />
      <div className="flex-1 min-w-0">
        <p className="text-white font-semibold text-sm">¡Instalá Prodea en tu celular!</p>
        {ios ? (
          <p className="text-[#8A8A9A] text-xs mt-0.5">
            Tocá <Share size={11} className="inline mb-0.5" /> <strong>Compartir</strong> → <strong>"Agregar a inicio"</strong> para una mejor experiencia
          </p>
        ) : (
          <p className="text-[#8A8A9A] text-xs mt-0.5">
            Instalala en tu pantalla de inicio para una mejor experiencia
          </p>
        )}
        {!ios && (
          <button
            onClick={install}
            className="mt-2 flex items-center gap-1 px-3 py-1.5 rounded-lg bg-[#00FF87] text-black text-xs font-bold active:scale-95 transition-transform"
          >
            <Plus size={12} /> Instalar
          </button>
        )}
      </div>
      <button onClick={dismiss} className="text-[#8A8A9A] shrink-0 -mt-0.5">
        <X size={18} />
      </button>
    </div>
  )
}
