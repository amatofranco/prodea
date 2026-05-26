const BADGE_STYLES = {
  Crack:         { bg: 'badge-crack',   label: 'El Crack' },
  Mufa:          { bg: 'badge-mufa',    label: 'El Mufa' },
  Adivino:       { bg: 'badge-adivino', label: 'El Adivino' },
  Francotirador: { bg: 'badge-franco',  label: 'El Francotirador' },
  Payaso:        { bg: 'badge-payaso',  label: 'El Payaso' },
  Dormido:       { bg: 'badge-dormido', label: 'El Dormido' },
}

const EMOJIS = {
  Crack: '🏆', Mufa: '💀', Adivino: '🔮',
  Francotirador: '🎯', Payaso: '🤡', Dormido: '😴',
  EnCaidaLibre: '📉', RachaInfernal: '🔥', ElMuro: '🧱', ElFantasma: '👻',
}

export function BadgePill({ type, className = '' }) {
  const style = BADGE_STYLES[type] || { bg: 'badge-dormido', label: type }
  const emoji = EMOJIS[type] || '❓'
  return (
    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold text-white ${style.bg} ${className}`}>
      {emoji} {style.label}
    </span>
  )
}

export { EMOJIS }
