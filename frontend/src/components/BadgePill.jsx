import { useTranslation } from 'react-i18next'

const BADGE_BG = {
  Crack: 'badge-crack', Mufa: 'badge-mufa', Adivino: 'badge-adivino',
  Francotirador: 'badge-franco', PechoFrio: 'badge-pechofrio', Goleador: 'badge-goleador',
  Rustico: 'badge-rustico', Tambaleante: 'badge-tambaleante', Payaso: 'badge-payaso',
  Dormido: 'badge-dormido', Tibio: 'badge-tibio', Campeon: 'badge-campeon',
  Subcampeon: 'badge-subcampeon', TercerPuesto: 'badge-tercerpuesto',
  Ultimo: 'badge-mufa', Penultimo: 'badge-tambaleante',
  GoleadorTorneo: 'badge-goleador', RusticoTorneo: 'badge-rustico',
}

const EMOJIS = {
  Crack: '🏆', Mufa: '💀', Adivino: '🔮',
  Francotirador: '🎯', PechoFrio: '❄️', Goleador: '⚽', Rustico: '⛏️', Tambaleante: '🥴', Payaso: '🤡', Dormido: '😴', Tibio: '🌡️',
  PecheadaTotal: '🥶', RachaInfernal: '🔥', ElMuro: '🧱', ElFantasma: '👻', TripleMufa: '💀🔥', TibiezaTotal: '🌡️', GoleadorSerial: '⚽', RusticoTotal: '⛏️',
  Campeon: '🏆', Subcampeon: '🥈', TercerPuesto: '🥉', Ultimo: '💀', Penultimo: '🥴',
  GoleadorTorneo: '⚽', RusticoTorneo: '⛏️',
}

export function BadgePill({ type, className = '', showTag = true }) {
  const { t } = useTranslation()
  const bg = BADGE_BG[type] || 'badge-dormido'
  const emoji = EMOJIS[type] || '❓'
  const label = t(`badges.${type}`, type)
  const tag = t(`badgeTags.${type}`, '')
  return (
    <span className={`inline-flex items-center gap-0.5 ${className}`}>
      <span className={`inline-flex items-center gap-0.5 px-1 py-0.5 rounded-full text-[10px] font-semibold text-white whitespace-nowrap shrink-0 ${bg}`}>
        {emoji} {label}
      </span>
      {showTag && tag && (
        <span className="text-[8px] font-semibold px-1 py-0.5 rounded-full bg-[#2A2A3E] text-[#8A8A9A] whitespace-nowrap shrink-0">
          {tag}
        </span>
      )}
    </span>
  )
}

export { EMOJIS, BADGE_BG }
