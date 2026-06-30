import { useEffect, useState } from 'react'
import { Share, Download } from 'lucide-react'
import { useTranslation } from 'react-i18next'

function isStandalone() {
  return window.matchMedia('(display-mode: standalone)').matches
    || window.navigator.standalone === true
}

function isIOS() {
  return /iphone|ipad|ipod/i.test(navigator.userAgent)
}

function isMobile() {
  return window.matchMedia('(pointer: coarse)').matches
}

export default function InstallBanner({ className = '' }) {
  const [show, setShow] = useState(false)
  const [deferredPrompt, setDeferredPrompt] = useState(null)
  const [ios, setIos] = useState(false)

  useEffect(() => {
    if (isStandalone()) return
    if (!isMobile()) return

    if (isIOS()) {
      setIos(true)
      setShow(true)
      return
    }

    if (window.__pwaInstallPrompt) {
      setDeferredPrompt(window.__pwaInstallPrompt)
      setShow(true)
      return
    }

    const handler = (e) => {
      e.preventDefault()
      window.__pwaInstallPrompt = e
      setDeferredPrompt(e)
      setShow(true)
    }
    window.addEventListener('beforeinstallprompt', handler)
    return () => window.removeEventListener('beforeinstallprompt', handler)
  }, [])

  async function install() {
    if (!deferredPrompt) return
    deferredPrompt.prompt()
    await deferredPrompt.userChoice
    setShow(false)
    window.__pwaInstallPrompt = null
  }

  const { t } = useTranslation()

  if (!show) return null

  return (
    <div className={`mx-4 mb-3 rounded-xl bg-[#1A1A2E] border border-[#00FF87]/20 px-3 py-2.5 flex items-center gap-2 ${className}`}>
      <img src="/logo-icon.png" alt="" className="w-7 h-7 object-contain shrink-0" />
      <p className="flex-1 text-[13px] text-[#8A8A9A]">
        {ios
          ? <><span className="text-white font-semibold">{t('install.installApp')}</span> {t('install.iosTip', { defaultValue: '' }).replace('<1/>', '')} <Share size={11} className="inline mb-0.5" /></>
          : <span className="text-white font-semibold">{t('install.installApp')}</span>
        }
      </p>
      {!ios && (
        <button
          onClick={install}
          className="shrink-0 flex items-center gap-1 px-2.5 py-1.5 rounded-lg bg-[#00FF87] text-black text-xs font-bold active:scale-95 transition-transform"
        >
          <Download size={11} /> {t('install.install')}
        </button>
      )}
    </div>
  )
}
