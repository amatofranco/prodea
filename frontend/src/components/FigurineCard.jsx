import { useRef, useState } from 'react'
import html2canvas from 'html2canvas'
import { Share2 } from 'lucide-react'
import { EMOJIS } from './BadgePill'

const BADGE_GRADIENTS = {
  Crack:         'from-yellow-400 via-amber-500 to-yellow-700',
  Mufa:          'from-red-500 via-red-700 to-red-950',
  Adivino:       'from-violet-400 via-purple-600 to-purple-950',
  Francotirador: 'from-cyan-400 via-sky-500 to-sky-900',
  Payaso:        'from-pink-400 via-rose-500 to-rose-900',
  Dormido:       'from-slate-400 via-slate-600 to-slate-900',
}

const BADGE_ACCENT = {
  Crack:         '#F59E0B',
  Mufa:          '#EF4444',
  Adivino:       '#8B5CF6',
  Francotirador: '#06B6D4',
  Payaso:        '#EC4899',
  Dormido:       '#64748B',
}

const BADGE_LABELS = {
  Crack:         'El Crack',
  Mufa:          'El Mufa',
  Adivino:       'El Adivino',
  Francotirador: 'El Francotirador',
  Payaso:        'El Payaso',
  Dormido:       'El Dormido',
}

function jornadaLabel(phase, matchday) {
  if (phase === 'Group') return `Fecha ${matchday}`
  return { R32: 'Dieciseisavos', R16: 'Octavos', QF: 'Cuartos', SF: 'Semis', ThirdPlace: '3er Puesto', Final: 'Final' }[phase] ?? phase
}

export default function FigurineCard({ badge, username, tournamentName, rank }) {
  const cardRef  = useRef(null)
  const [sharing, setSharing] = useState(false)
  const [error,   setError]   = useState(null)

  const gradient = BADGE_GRADIENTS[badge.badgeType] || BADGE_GRADIENTS.Dormido
  const accent   = BADGE_ACCENT[badge.badgeType]   || '#00FF87'
  const emoji    = EMOJIS[badge.badgeType]         || '❓'
  const label    = BADGE_LABELS[badge.badgeType]   || badge.badgeType
  const avatar   = username[0].toUpperCase()
  const jornada  = jornadaLabel(badge.phase, badge.matchday)

  async function exportCard() {
    if (!cardRef.current || sharing) return
    setSharing(true)
    setError(null)
    try {
      await document.fonts.ready
      const canvas = await html2canvas(cardRef.current, {
        backgroundColor: null,
        scale: 3,
        useCORS: true,
        logging: false,
        allowTaint: true,
      })

      const blob = await new Promise((resolve, reject) =>
        canvas.toBlob(b => (b ? resolve(b) : reject(new Error('toBlob falló'))), 'image/png')
      )
      const file = new File([blob], `prodea-${username}.png`, { type: 'image/png' })

      if (navigator.canShare?.({ files: [file] })) {
        await navigator.share({
          text: `${username} fue "${label}" en ${jornada} · Prodeá Mundial 2026`,
          files: [file],
        })
      } else {
        // Fallback: descarga directa
        const url = URL.createObjectURL(blob)
        const a   = document.createElement('a')
        a.href     = url
        a.download = `prodea-${username}-${jornada}.png`
        document.body.appendChild(a)
        a.click()
        document.body.removeChild(a)
        setTimeout(() => URL.revokeObjectURL(url), 1000)
      }
    } catch (err) {
      if (err?.name !== 'AbortError') {
        setError('No se pudo generar la imagen. Intentá de nuevo.')
        console.error('[FigurineCard]', err)
      }
    } finally {
      setSharing(false)
    }
  }

  return (
    <div className="flex flex-col items-center gap-5">
      {/* Card — html2canvas-safe: no backdrop-blur, no bg-opacity tricks */}
      <div
        ref={cardRef}
        className={`w-56 rounded-3xl bg-gradient-to-b ${gradient} p-[3px]`}
      >
        <div className="rounded-[22px] bg-[#0A0A0A] px-5 pt-4 pb-5 flex flex-col items-center gap-3">

          {/* Header */}
          <div className="w-full flex justify-between items-center">
            <span className="text-[9px] text-white/40 uppercase tracking-widest font-bold">Prodeá</span>
            <span className="text-[9px] text-white/40 uppercase tracking-wider">{jornada}</span>
          </div>

          {/* Avatar */}
          <div
            className="w-16 h-16 rounded-full flex items-center justify-center text-2xl font-black text-[#0A0A0A]"
            style={{ backgroundColor: accent }}
          >
            {avatar}
          </div>

          {/* Username */}
          <p
            className="text-xl font-bold text-white text-center leading-tight"
            style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif', letterSpacing: '0.08em' }}
          >
            {username}
          </p>

          {/* Divider */}
          <div
            className="w-full h-px"
            style={{ background: `linear-gradient(to right, transparent, ${accent}80, transparent)` }}
          />

          {/* Emoji + Badge label */}
          <div className="flex flex-col items-center gap-1 mt-1">
            <span className="text-6xl leading-none">{emoji}</span>
            <p
              className="text-2xl font-bold text-center mt-1"
              style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif', letterSpacing: '0.05em', color: accent }}
            >
              {label}
            </p>
          </div>

          {/* Stats row */}
          <div className="flex gap-6 justify-center mt-1">
            <div className="text-center">
              <p
                className="text-3xl font-black leading-none"
                style={{ fontFamily: 'Bebas Neue, sans-serif', color: accent }}
              >
                {badge.pointsInMatchday}
              </p>
              <p className="text-[9px] text-white/40 uppercase tracking-wide mt-0.5">pts jornada</p>
            </div>
            {rank != null && (
              <div className="text-center">
                <p
                  className="text-3xl font-black text-white leading-none"
                  style={{ fontFamily: 'Bebas Neue, sans-serif' }}
                >
                  #{rank}
                </p>
                <p className="text-[9px] text-white/40 uppercase tracking-wide mt-0.5">en tabla</p>
              </div>
            )}
          </div>

          {/* Phrase */}
          <p className="text-[11px] italic text-white/50 text-center leading-snug px-1 mt-1">
            &ldquo;{badge.randomPhrase}&rdquo;
          </p>

          {/* Footer */}
          <div className="w-full flex justify-center pt-2 border-t border-white/10">
            <p className="text-[9px] text-white/25 uppercase tracking-widest font-bold">
              {tournamentName} · Mundial 2026
            </p>
          </div>
        </div>
      </div>

      {/* Share button */}
      <button
        onClick={exportCard}
        disabled={sharing}
        className="flex items-center gap-2 px-6 py-3 rounded-full bg-[#00FF87] text-black font-bold text-sm active:scale-95 transition-transform disabled:opacity-70"
      >
        {sharing
          ? <span className="w-4 h-4 rounded-full border-2 border-black border-t-transparent animate-spin" />
          : <Share2 size={16} />
        }
        {sharing ? 'Generando...' : 'Compartir card'}
      </button>

      {error && (
        <p className="text-red-400 text-xs text-center px-4">{error}</p>
      )}
    </div>
  )
}
