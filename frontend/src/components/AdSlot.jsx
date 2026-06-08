import { useEffect, useRef } from 'react'

const AD_CLIENT = 'ca-pub-3164303605679315'

/**
 * Bloque de anuncio manual de AdSense (no Auto ads).
 * Reemplazá `slot` por el ID real que generás al crear la unidad de anuncio
 * en el panel de AdSense (Anuncios > Por unidad de anuncio > Anuncio display).
 */
export default function AdSlot({ slot, className = '', label = 'Publicidad' }) {
  const insRef = useRef(null)
  const pushed = useRef(false)
  const ready = Boolean(slot) && !slot.startsWith('REEMPLAZAR')

  useEffect(() => {
    if (!ready || pushed.current || !insRef.current) return
    pushed.current = true
    try {
      ;(window.adsbygoogle = window.adsbygoogle || []).push({})
    } catch {
      // AdSense puede no estar disponible (bloqueador de anuncios, entorno de testing, etc.)
    }
  }, [ready])

  // Mientras no haya un slot real (post-aprobación de AdSense), no renderiza nada
  if (!ready) return null

  return (
    <div className={`flex flex-col items-center gap-1 ${className}`}>
      <span className="text-[10px] uppercase tracking-widest text-[#8A8A9A]">{label}</span>
      <ins
        ref={insRef}
        className="adsbygoogle"
        style={{ display: 'block', width: '100%' }}
        data-ad-client={AD_CLIENT}
        data-ad-slot={slot}
        data-ad-format="auto"
        data-full-width-responsive="true"
      />
    </div>
  )
}
