import { useRef, useState, forwardRef } from 'react'
import { toBlob } from 'html-to-image'
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

// Hex explícitos para la tarjeta de exportación (html-to-image no depende de oklch)
const BADGE_EXPORT_GRADIENT = {
  Crack:         ['#FACC15', '#F59E0B', '#B45309'],
  Mufa:          ['#EF4444', '#B91C1C', '#450A0A'],
  Adivino:       ['#A78BFA', '#7C3AED', '#3B0764'],
  Francotirador: ['#22D3EE', '#0284C7', '#0C4A6E'],
  Payaso:        ['#F472B6', '#E11D48', '#4C0519'],
  Dormido:       ['#94A3B8', '#475569', '#0F172A'],
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

// Tarjeta solo para exportación: todo en inline styles con px fijos.
// Evita que html-to-image interprete oklch de Tailwind.
// forwardRef para que el ref apunte al div raíz con el gradiente,
// no al wrapper oculto — así html-to-image captura desde (0,0).
const ExportCard = forwardRef(function ExportCard({ badge, username, tournamentName, rank }, ref) {
  const stops  = BADGE_EXPORT_GRADIENT[badge.badgeType] || BADGE_EXPORT_GRADIENT.Dormido
  const accent = BADGE_ACCENT[badge.badgeType] || '#00FF87'
  const emoji  = EMOJIS[badge.badgeType] || '❓'
  const label  = BADGE_LABELS[badge.badgeType] || badge.badgeType
  const jornada = jornadaLabel(badge.phase, badge.matchday)
  const avatar  = username[0].toUpperCase()

  const s = {
    wrap: {
      display: 'inline-block',
      background: `linear-gradient(to bottom, ${stops[0]}, ${stops[1]}, ${stops[2]})`,
      borderRadius: '24px',
      padding: '3px',
      width: '320px',
    },
    inner: {
      background: '#0A0A0A',
      borderRadius: '21px',
      padding: '24px 20px 20px',
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      gap: '14px',
      width: '100%',
      boxSizing: 'border-box',
    },
    row: { width: '100%', display: 'flex', justifyContent: 'space-between', alignItems: 'center' },
    label: { fontSize: '10px', color: 'rgba(255,255,255,0.4)', textTransform: 'uppercase', letterSpacing: '3px', fontWeight: '700', fontFamily: 'DM Sans, system-ui, sans-serif', margin: 0 },
    labelRight: { fontSize: '10px', color: 'rgba(255,255,255,0.4)', textTransform: 'uppercase', letterSpacing: '1px', fontFamily: 'DM Sans, system-ui, sans-serif', margin: 0 },
    avatar: {
      width: '72px', height: '72px', borderRadius: '50%',
      background: accent,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      fontSize: '28px', fontWeight: '900', color: '#0A0A0A',
      fontFamily: 'Bebas Neue, DM Sans, system-ui, sans-serif',
    },
    username: { fontSize: '24px', fontWeight: '700', color: '#FFFFFF', fontFamily: 'Bebas Neue, DM Sans, system-ui, sans-serif', letterSpacing: '3px', margin: 0 },
    divider: { width: '100%', height: '1px', background: `linear-gradient(to right, transparent, ${accent}80, transparent)` },
    emojiWrap: { display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px' },
    emoji: { fontSize: '72px', lineHeight: '1', display: 'block' },
    badgeLabel: { fontSize: '28px', fontWeight: '700', color: accent, fontFamily: 'Bebas Neue, DM Sans, system-ui, sans-serif', letterSpacing: '2px', margin: 0 },
    statsRow: { display: 'flex', gap: '32px', justifyContent: 'center' },
    statNum: { fontSize: '40px', fontWeight: '900', color: accent, fontFamily: 'Bebas Neue, DM Sans, system-ui, sans-serif', lineHeight: '1', margin: 0 },
    statNumWhite: { fontSize: '40px', fontWeight: '900', color: '#FFFFFF', fontFamily: 'Bebas Neue, DM Sans, system-ui, sans-serif', lineHeight: '1', margin: 0 },
    statLabel: { fontSize: '10px', color: 'rgba(255,255,255,0.4)', textTransform: 'uppercase', letterSpacing: '1px', fontFamily: 'DM Sans, system-ui, sans-serif', margin: '4px 0 0', textAlign: 'center' },
    phrase: { fontSize: '12px', fontStyle: 'italic', color: 'rgba(255,255,255,0.5)', textAlign: 'center', lineHeight: '1.5', margin: 0, padding: '0 8px', fontFamily: 'DM Sans, system-ui, sans-serif' },
    footer: { width: '100%', display: 'flex', justifyContent: 'center', paddingTop: '12px', borderTop: '1px solid rgba(255,255,255,0.1)', marginTop: '2px' },
    footerText: { fontSize: '9px', color: 'rgba(255,255,255,0.25)', textTransform: 'uppercase', letterSpacing: '3px', fontWeight: '700', fontFamily: 'DM Sans, system-ui, sans-serif', margin: 0 },
  }

  return (
    <div ref={ref} style={s.wrap}>
      <div style={s.inner}>
        <div style={s.row}>
          <p style={s.label}>Prodeá</p>
          <p style={s.labelRight}>{jornada}</p>
        </div>
        <div style={s.avatar}>{avatar}</div>
        <p style={s.username}>{username}</p>
        <div style={s.divider} />
        <div style={s.emojiWrap}>
          <span style={s.emoji}>{emoji}</span>
          <p style={s.badgeLabel}>{label}</p>
        </div>
        <div style={s.statsRow}>
          <div style={{ textAlign: 'center' }}>
            <p style={s.statNum}>{badge.pointsInMatchday}</p>
            <p style={s.statLabel}>pts jornada</p>
          </div>
          {rank != null && (
            <div style={{ textAlign: 'center' }}>
              <p style={s.statNumWhite}>#{rank}</p>
              <p style={s.statLabel}>en tabla</p>
            </div>
          )}
        </div>
        <p style={s.phrase}>&ldquo;{badge.randomPhrase}&rdquo;</p>
        <div style={s.footer}>
          <p style={s.footerText}>{tournamentName} · Mundial 2026</p>
        </div>
      </div>
    </div>
  )
}

export default function FigurineCard({ badge, username, tournamentName, rank }) {
  const exportRef = useRef(null)
  const [sharing, setSharing] = useState(false)
  const [error,   setError]   = useState(null)

  const gradient = BADGE_GRADIENTS[badge.badgeType] || BADGE_GRADIENTS.Dormido
  const accent   = BADGE_ACCENT[badge.badgeType]   || '#00FF87'
  const emoji    = EMOJIS[badge.badgeType]         || '❓'
  const label    = BADGE_LABELS[badge.badgeType]   || badge.badgeType
  const avatar   = username[0].toUpperCase()
  const jornada  = jornadaLabel(badge.phase, badge.matchday)

  async function exportCard() {
    if (!exportRef.current || sharing) return
    setSharing(true)
    setError(null)
    try {
      await document.fonts.ready
      const blob = await Promise.race([
        toBlob(exportRef.current, { pixelRatio: 3, skipFonts: true }),
        new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), 12_000)),
      ])
      const file = new File([blob], `prodea-${username}.png`, { type: 'image/png' })

      if (navigator.canShare?.({ files: [file] })) {
        await navigator.share({
          text: `${username} fue "${label}" en ${jornada} · Prodeá Mundial 2026`,
          files: [file],
        })
      } else {
        const url = URL.createObjectURL(blob)
        const a   = document.createElement('a')
        a.href = url
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
      {/* Tarjeta visible — Tailwind para la UI */}
      <div className={`w-56 rounded-3xl bg-gradient-to-b ${gradient} p-[3px]`}>
        <div className="rounded-[22px] bg-[#0A0A0A] px-5 pt-4 pb-5 flex flex-col items-center gap-3">
          <div className="w-full flex justify-between items-center">
            <span className="text-[9px] text-white/40 uppercase tracking-widest font-bold">Prodeá</span>
            <span className="text-[9px] text-white/40 uppercase tracking-wider">{jornada}</span>
          </div>
          <div className="w-16 h-16 rounded-full flex items-center justify-center text-2xl font-black text-[#0A0A0A]" style={{ backgroundColor: accent }}>
            {avatar}
          </div>
          <p className="text-xl font-bold text-white text-center leading-tight" style={{ fontFamily: 'Bebas Neue, sans-serif', letterSpacing: '0.08em' }}>
            {username}
          </p>
          <div className="w-full h-px" style={{ background: `linear-gradient(to right, transparent, ${accent}80, transparent)` }} />
          <div className="flex flex-col items-center gap-1 mt-1">
            <span className="text-6xl leading-none">{emoji}</span>
            <p className="text-2xl font-bold text-center mt-1" style={{ fontFamily: 'Bebas Neue, sans-serif', letterSpacing: '0.05em', color: accent }}>
              {label}
            </p>
          </div>
          <div className="flex gap-6 justify-center mt-1">
            <div className="text-center">
              <p className="text-3xl font-black leading-none" style={{ fontFamily: 'Bebas Neue, sans-serif', color: accent }}>{badge.pointsInMatchday}</p>
              <p className="text-[9px] text-white/40 uppercase tracking-wide mt-0.5">pts jornada</p>
            </div>
            {rank != null && (
              <div className="text-center">
                <p className="text-3xl font-black text-white leading-none" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>#{rank}</p>
                <p className="text-[9px] text-white/40 uppercase tracking-wide mt-0.5">en tabla</p>
              </div>
            )}
          </div>
          <p className="text-[11px] italic text-white/50 text-center leading-snug px-1 mt-1">
            &ldquo;{badge.randomPhrase}&rdquo;
          </p>
          <div className="w-full flex justify-center pt-2 border-t border-white/10">
            <p className="text-[9px] text-white/25 uppercase tracking-widest font-bold">{tournamentName} · Mundial 2026</p>
          </div>
        </div>
      </div>

      {/* Wrapper clipeado: el card está en el viewport (browser lo renderiza)
          pero invisible. overflow:hidden no se clona por html-to-image. */}
      <div style={{ position: 'fixed', top: 0, left: 0, width: 0, height: 0, overflow: 'hidden', pointerEvents: 'none' }}>
        <ExportCard ref={exportRef} badge={badge} username={username} tournamentName={tournamentName} rank={rank} />
      </div>

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

      {error && <p className="text-red-400 text-xs text-center px-4">{error}</p>}
    </div>
  )
}
