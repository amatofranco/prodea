const BADGE_STYLES = {
  Crack:         { bg: 'badge-crack',    label: 'El Crack' },
  Mufa:          { bg: 'badge-mufa',     label: 'El Mufa' },
  Adivino:       { bg: 'badge-adivino',  label: 'El Adivino' },
  Francotirador: { bg: 'badge-franco',   label: 'El Francotirador' },
  PechoFrio:     { bg: 'badge-pechofrio', label: 'El Pecho Frío' },
  Goleador:      { bg: 'badge-goleador',  label: 'El Goleador' },
  Rustico:       { bg: 'badge-rustico',  label: 'El Rústico' },
  Payaso:        { bg: 'badge-payaso',   label: 'El Payaso' },
  Dormido:       { bg: 'badge-dormido',  label: 'El Dormido' },
  Tibio:         { bg: 'badge-tibio',    label: 'El Tibio' },
}

const EMOJIS = {
  Crack: '🏆', Mufa: '💀', Adivino: '🔮',
  Francotirador: '🎯', PechoFrio: '❄️', Goleador: '⚽', Rustico: '⛏️', Payaso: '🤡', Dormido: '😴', Tibio: '🌡️',
  PecheadaTotal: '🥶', RachaInfernal: '🔥', ElMuro: '🧱', ElFantasma: '👻', TripleMufa: '💀🔥',
}

export const BADGE_TAGS = {
  Crack:         'Líder de la fecha',
  Mufa:          'Último de la fecha',
  Francotirador: '3 exactos',
  Adivino:       '4 exactos',
  PechoFrio:     '2° de la fecha',
  Goleador:      'Más goles',
  Rustico:       'Menos goles',
  Tibio:         'Mitad de tabla',
  Payaso:        '0 puntos',
  Dormido:       'Sin predicciones',
}

export function BadgePill({ type, className = '' }) {
  const style = BADGE_STYLES[type] || { bg: 'badge-dormido', label: type }
  const emoji = EMOJIS[type] || '❓'
  const tag = BADGE_TAGS[type]
  return (
    <span className={`inline-flex items-center gap-1 ${className}`}>
      <span className={`inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded-full text-[11px] font-semibold text-white ${style.bg}`}>
        {emoji} {style.label}
      </span>
      {tag && (
        <span className="text-[9px] font-semibold px-1.5 py-0.5 rounded-full bg-[#2A2A3E] text-[#8A8A9A] whitespace-nowrap">
          {tag}
        </span>
      )}
    </span>
  )
}

export { EMOJIS }
