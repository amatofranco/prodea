import { useRef } from 'react'
import html2canvas from 'html2canvas'
import { Share2, Download } from 'lucide-react'
import { EMOJIS } from './BadgePill'

const BADGE_GRADIENTS = {
  Crack:         'from-yellow-500 to-amber-700',
  Mufa:          'from-red-700 to-red-950',
  Adivino:       'from-violet-600 to-purple-950',
  Francotirador: 'from-cyan-600 to-sky-900',
  Payaso:        'from-pink-500 to-rose-900',
  Dormido:       'from-gray-600 to-gray-900',
}

const BADGE_LABELS = {
  Crack: 'El Crack', Mufa: 'El Mufa', Adivino: 'El Adivino',
  Francotirador: 'El Francotirador', Payaso: 'El Payaso', Dormido: 'El Dormido',
}

export default function FigurineCard({ badge, username, tournamentName }) {
  const cardRef = useRef(null)
  const gradient = BADGE_GRADIENTS[badge.badgeType] || BADGE_GRADIENTS.Dormido
  const emoji = EMOJIS[badge.badgeType] || '❓'
  const label = BADGE_LABELS[badge.badgeType] || badge.badgeType

  async function exportCard() {
    if (!cardRef.current) return
    try {
      const canvas = await html2canvas(cardRef.current, {
        backgroundColor: null,
        scale: 3,
        useCORS: true,
      })
      const url = canvas.toDataURL('image/png')

      if (navigator.share) {
        const blob = await (await fetch(url)).blob()
        await navigator.share({
          title: `${username} — ${label}`,
          files: [new File([blob], 'card-prodea.png', { type: 'image/png' })],
        })
      } else {
        const a = document.createElement('a')
        a.href = url
        a.download = `prodea-${username}-jornada${badge.matchday}.png`
        a.click()
      }
    } catch {
      // user cancelled share or html2canvas failed silently
    }
  }

  return (
    <div className="flex flex-col items-center gap-4">
      {/* Card */}
      <div
        ref={cardRef}
        className={`relative w-64 rounded-2xl overflow-hidden bg-gradient-to-b ${gradient} p-[2px]`}
      >
        <div className="rounded-2xl overflow-hidden bg-[#0D0D0D]/70 backdrop-blur-sm p-5 flex flex-col items-center gap-3">
          {/* Header */}
          <div className="w-full flex justify-between items-start text-[10px] text-white/50 uppercase tracking-wider">
            <span>{tournamentName}</span>
            <span>Jornada {badge.matchday}</span>
          </div>

          {/* Emoji giant */}
          <span className="text-7xl leading-none mt-1">{emoji}</span>

          {/* Badge name */}
          <p
            className="text-3xl font-bold text-white text-center leading-tight"
            style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif', letterSpacing: '0.05em' }}
          >
            {label}
          </p>

          {/* Username */}
          <p className="text-lg font-semibold text-white/90">{username}</p>

          {/* Points + rank */}
          <div className="flex gap-6 mt-1">
            <div className="text-center">
              <p className="text-2xl font-bold text-[#00FF87]" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                {badge.pointsInMatchday}
              </p>
              <p className="text-[10px] text-white/50 uppercase tracking-wide">pts jornada</p>
            </div>
          </div>

          {/* Phrase */}
          <p className="text-xs italic text-white/60 text-center px-2 leading-snug">
            "{badge.randomPhrase}"
          </p>

          {/* Footer */}
          <div className="w-full flex justify-center mt-1">
            <p className="text-[10px] text-white/30 uppercase tracking-widest font-bold">Prodeá · Mundial 2026</p>
          </div>
        </div>
      </div>

      {/* Share button */}
      <button
        onClick={exportCard}
        className="flex items-center gap-2 px-5 py-2.5 rounded-full bg-[#00FF87] text-black font-semibold text-sm active:scale-95 transition-transform"
      >
        <Share2 size={16} />
        Compartir card
      </button>
    </div>
  )
}
