import { useState } from 'react'
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

const BADGE_GRADIENT_STOPS = {
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

function buildCardHTML({ badge, username, tournamentName, rank }) {
  const stops  = BADGE_GRADIENT_STOPS[badge.badgeType] || BADGE_GRADIENT_STOPS.Dormido
  const accent = BADGE_ACCENT[badge.badgeType] || '#00FF87'
  const emoji  = EMOJIS[badge.badgeType] || '❓'
  const label  = BADGE_LABELS[badge.badgeType] || badge.badgeType
  const avatar = username[0].toUpperCase()
  const jornada = jornadaLabel(badge.phase, badge.matchday)

  const statHTML = `
    <div style="display:flex;gap:32px;justify-content:center;">
      <div style="text-align:center;">
        <p style="font-size:40px;font-weight:900;color:${accent};font-family:'Bebas Neue',sans-serif;line-height:1;margin:0;">${badge.pointsInMatchday}</p>
        <p style="font-size:10px;color:rgba(255,255,255,0.4);text-transform:uppercase;letter-spacing:1px;font-family:'DM Sans',sans-serif;margin:4px 0 0;">pts jornada</p>
      </div>
      ${rank != null ? `
      <div style="text-align:center;">
        <p style="font-size:40px;font-weight:900;color:#FFFFFF;font-family:'Bebas Neue',sans-serif;line-height:1;margin:0;">#${rank}</p>
        <p style="font-size:10px;color:rgba(255,255,255,0.4);text-transform:uppercase;letter-spacing:1px;font-family:'DM Sans',sans-serif;margin:4px 0 0;">en tabla</p>
      </div>` : ''}
    </div>
  `

  return `
    <div style="
      display:inline-block;
      background:linear-gradient(to bottom,${stops[0]},${stops[1]},${stops[2]});
      border-radius:24px;
      padding:3px;
      width:320px;
    ">
      <div style="
        background:#0A0A0A;
        border-radius:21px;
        padding:24px 20px 20px;
        display:flex;
        flex-direction:column;
        align-items:center;
        gap:14px;
        width:100%;
        box-sizing:border-box;
      ">
        <div style="width:100%;display:flex;justify-content:space-between;align-items:center;">
          <span style="font-size:10px;color:rgba(255,255,255,0.4);text-transform:uppercase;letter-spacing:3px;font-weight:700;font-family:'DM Sans',sans-serif;">Prodeá</span>
          <span style="font-size:10px;color:rgba(255,255,255,0.4);text-transform:uppercase;letter-spacing:1px;font-family:'DM Sans',sans-serif;">${jornada}</span>
        </div>

        <div style="
          width:72px;height:72px;border-radius:50%;
          background:${accent};
          display:flex;align-items:center;justify-content:center;
          font-size:28px;font-weight:900;color:#0A0A0A;
          font-family:'Bebas Neue',sans-serif;
        ">${avatar}</div>

        <p style="font-size:24px;font-weight:700;color:#FFFFFF;font-family:'Bebas Neue',sans-serif;letter-spacing:3px;margin:0;">${username}</p>

        <div style="width:100%;height:1px;background:linear-gradient(to right,transparent,${accent}80,transparent);"></div>

        <div style="display:flex;flex-direction:column;align-items:center;gap:8px;">
          <span style="font-size:72px;line-height:1;">${emoji}</span>
          <p style="font-size:28px;font-weight:700;color:${accent};font-family:'Bebas Neue',sans-serif;letter-spacing:2px;margin:0;">${label}</p>
        </div>

        ${statHTML}

        <p style="font-size:12px;font-style:italic;color:rgba(255,255,255,0.5);text-align:center;line-height:1.5;margin:0;padding:0 8px;font-family:'DM Sans',sans-serif;">&#8220;${badge.randomPhrase}&#8221;</p>

        <div style="width:100%;display:flex;justify-content:center;padding-top:12px;border-top:1px solid rgba(255,255,255,0.1);">
          <p style="font-size:9px;color:rgba(255,255,255,0.25);text-transform:uppercase;letter-spacing:3px;font-weight:700;font-family:'DM Sans',sans-serif;margin:0;">${tournamentName} · Mundial 2026</p>
        </div>
      </div>
    </div>
  `
}

export default function FigurineCard({ badge, username, tournamentName, rank }) {
  const [sharing, setSharing] = useState(false)
  const [error,   setError]   = useState(null)

  const gradient = BADGE_GRADIENTS[badge.badgeType] || BADGE_GRADIENTS.Dormido
  const accent   = BADGE_ACCENT[badge.badgeType]   || '#00FF87'
  const emoji    = EMOJIS[badge.badgeType]         || '❓'
  const label    = BADGE_LABELS[badge.badgeType]   || badge.badgeType
  const avatar   = username[0].toUpperCase()
  const jornada  = jornadaLabel(badge.phase, badge.matchday)

  async function exportCard() {
    if (sharing) return
    setSharing(true)
    setError(null)

    const container = document.createElement('div')
    container.style.cssText = 'position:fixed;top:0;left:0;pointer-events:none;z-index:1;'

    try {
      await document.fonts.ready

      container.innerHTML = buildCardHTML({ badge, username, tournamentName, rank })
      document.body.appendChild(container)

      // Dos frames: layout + paint
      await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)))

      const cardEl = container.firstElementChild
      const blob = await Promise.race([
        toBlob(cardEl, { pixelRatio: 3, skipFonts: true }),
        new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), 12_000)),
      ])

      document.body.removeChild(container)

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
      if (document.body.contains(container)) document.body.removeChild(container)
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
      {/* Tarjeta visible en UI */}
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
