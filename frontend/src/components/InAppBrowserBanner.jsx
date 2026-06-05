import { useState } from 'react'

const ua = navigator.userAgent
const isInAppBrowser = /Instagram|FBAN|FBAV|FB_IAB|WhatsApp|TikTok|Twitter|LinkedIn/.test(ua)
const isAndroid = /Android/.test(ua)

export default function InAppBrowserBanner() {
  const [copied, setCopied] = useState(false)
  const [dismissed, setDismissed] = useState(false)

  if (!isInAppBrowser || dismissed) return null

  function handleOpen() {
    if (isAndroid) {
      const { host, pathname, search } = window.location
      window.location.href = `intent://${host}${pathname}${search}#Intent;scheme=https;package=com.android.chrome;end`
    } else {
      navigator.clipboard?.writeText(window.location.href).then(() => {
        setCopied(true)
        setTimeout(() => setDismissed(true), 1500)
      }).catch(() => {})
    }
  }

  return (
    <div className="w-full bg-[#1A1A2E] border-b border-[#2A2A3E] px-4 py-2.5 flex items-center gap-2.5">
      <span className="text-base shrink-0">🌐</span>
      <p className="text-[11px] text-[#8A8A9A] flex-1 leading-tight">
        {isAndroid
          ? 'Estás en el browser de Instagram. Para Google login o instalar la app, abrí en Chrome.'
          : 'Estás en el browser de Instagram. Para Google login o instalar la app, abrí en Safari.'}
      </p>
      <button
        onClick={handleOpen}
        className="text-[11px] font-bold text-[#00FF87] shrink-0 active:opacity-70"
      >
        {copied ? '¡Copiado!' : isAndroid ? 'Abrir →' : 'Copiar link →'}
      </button>
      <button
        onClick={() => setDismissed(true)}
        className="text-[#8A8A9A] text-sm shrink-0 active:opacity-70 pl-1"
      >
        ✕
      </button>
    </div>
  )
}
