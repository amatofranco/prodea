const BADGE_STYLES = {
  Crack:         { bg: 'badge-crack',    label: 'El Crack' },
  Mufa:          { bg: 'badge-mufa',     label: 'El Mufa' },
  Adivino:       { bg: 'badge-adivino',  label: 'El Adivino' },
  Francotirador: { bg: 'badge-franco',   label: 'El Francotirador' },
  PechoFrio:     { bg: 'badge-pechofrio', label: 'El Pecho Frío' },
  Goleador:      { bg: 'badge-goleador',  label: 'El Goleador' },
  Rustico:       { bg: 'badge-rustico',      label: 'El Rústico' },
  Tambaleante:   { bg: 'badge-tambaleante', label: 'El Tambaleante' },
  Payaso:        { bg: 'badge-payaso',   label: 'El Payaso' },
  Dormido:       { bg: 'badge-dormido',  label: 'El Dormido' },
  Tibio:         { bg: 'badge-tibio',    label: 'El Tibio' },
  Campeon:       { bg: 'badge-campeon',      label: 'El Campeón' },
  Subcampeon:    { bg: 'badge-subcampeon',   label: 'El Subcampeón' },
  TercerPuesto:  { bg: 'badge-tercerpuesto', label: 'Tercer Puesto' },
  Ultimo:        { bg: 'badge-mufa',         label: 'Último' },
  Penultimo:     { bg: 'badge-tambaleante',  label: 'Penúltimo' },
  GoleadorTorneo: { bg: 'badge-goleador', label: 'El Goleador' },
  RusticoTorneo:  { bg: 'badge-rustico',  label: 'El Rústico' },
}

const EMOJIS = {
  Crack: '🏆', Mufa: '💀', Adivino: '🔮',
  Francotirador: '🎯', PechoFrio: '❄️', Goleador: '⚽', Rustico: '⛏️', Tambaleante: '🥴', Payaso: '🤡', Dormido: '😴', Tibio: '🌡️',
  PecheadaTotal: '🥶', RachaInfernal: '🔥', ElMuro: '🧱', ElFantasma: '👻', TripleMufa: '💀🔥', TibiezaTotal: '🌡️', GoleadorSerial: '⚽', RusticoTotal: '⛏️',
  Campeon: '🏆', Subcampeon: '🥈', TercerPuesto: '🥉', Ultimo: '💀', Penultimo: '🥴',
  GoleadorTorneo: '⚽', RusticoTorneo: '⛏️',
}

export const BADGE_TAGS = {
  Crack:         'Mayor puntaje en la fecha',
  Mufa:          'Menor puntaje en la fecha',
  Francotirador: '3 exactos en la fecha',
  Adivino:       '4 exactos en la fecha',
  PechoFrio:     '2° puntaje en la fecha',
  Goleador:      'Más goles cargados',
  Rustico:       'Menos goles cargados',
  Tambaleante:   'Penúltimo de la fecha',
  Tibio:         'Ni fu ni fa en la fecha',
  Payaso:        '0 puntos',
  Dormido:       'Sin predicciones',
  Ultimo:        'Último del torneo',
  Penultimo:     'Penúltimo del torneo',
  GoleadorTorneo: 'Más goles cargados en total',
  RusticoTorneo:  'Menos goles cargados en total',
}

export function BadgePill({ type, className = '', showTag = true }) {
  const style = BADGE_STYLES[type] || { bg: 'badge-dormido', label: type }
  const emoji = EMOJIS[type] || '❓'
  const tag = BADGE_TAGS[type]
  return (
    <span className={`inline-flex items-center gap-0.5 ${className}`}>
      <span className={`inline-flex items-center gap-0.5 px-1 py-0.5 rounded-full text-[10px] font-semibold text-white whitespace-nowrap shrink-0 ${style.bg}`}>
        {emoji} {style.label}
      </span>
      {showTag && tag && (
        <span className="text-[8px] font-semibold px-1 py-0.5 rounded-full bg-[#2A2A3E] text-[#8A8A9A] whitespace-nowrap shrink-0">
          {tag}
        </span>
      )}
    </span>
  )
}

export { EMOJIS }
