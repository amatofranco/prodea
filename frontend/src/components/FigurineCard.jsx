import { useState } from 'react'
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

function roundedRect(ctx, x, y, w, h, r) {
  ctx.beginPath()
  ctx.moveTo(x + r, y)
  ctx.lineTo(x + w - r, y)
  ctx.arcTo(x + w, y,     x + w, y + r,     r)
  ctx.lineTo(x + w, y + h - r)
  ctx.arcTo(x + w, y + h, x + w - r, y + h, r)
  ctx.lineTo(x + r, y + h)
  ctx.arcTo(x,      y + h, x, y + h - r,    r)
  ctx.lineTo(x, y + r)
  ctx.arcTo(x,      y,     x + r, y,         r)
  ctx.closePath()
}

function wrapLines(ctx, text, maxW) {
  const words = text.split(' ')
  const lines = []
  let line = ''
  for (const word of words) {
    const test = line ? `${line} ${word}` : word
    if (line && ctx.measureText(test).width > maxW) { lines.push(line); line = word }
    else line = test
  }
  if (line) lines.push(line)
  return lines
}

async function generateCardBlob({ badge, username, tournamentName, rank }) {
  await document.fonts.ready

  const W     = 320
  const SCALE = 3
  const PAD   = 20
  const GAP   = 14
  const CX    = W / 2
  const stops  = BADGE_GRADIENT_STOPS[badge.badgeType] || BADGE_GRADIENT_STOPS.Dormido
  const accent = BADGE_ACCENT[badge.badgeType]         || '#00FF87'
  const emoji  = EMOJIS[badge.badgeType]               || '❓'
  const label  = BADGE_LABELS[badge.badgeType]         || badge.badgeType
  const jornada = jornadaLabel(badge.phase, badge.matchday)
  const avatar  = username[0].toUpperCase()
  const phrase  = `“${badge.randomPhrase}”`

  // Pre-measure phrase lines on a temp canvas
  const tmp = document.createElement('canvas').getContext('2d')
  tmp.font = 'italic 12px "DM Sans", system-ui, sans-serif'
  const phraseLines = wrapLines(tmp, phrase, W - 80)

  // Dynamic height
  const H = PAD + 12 + GAP + 72 + GAP + 26 + GAP + 1 + GAP
          + 68 + 8 + 30 + GAP + 56 + GAP
          + phraseLines.length * 17
          + GAP + 1 + 12 + 12 + PAD

  const canvas = document.createElement('canvas')
  canvas.width  = W * SCALE
  canvas.height = H * SCALE
  const ctx = canvas.getContext('2d')
  ctx.scale(SCALE, SCALE)

  // --- Gradient border ---
  const grad = ctx.createLinearGradient(0, 0, 0, H)
  grad.addColorStop(0,   stops[0])
  grad.addColorStop(0.5, stops[1])
  grad.addColorStop(1,   stops[2])
  ctx.fillStyle = grad
  roundedRect(ctx, 0, 0, W, H, 24)
  ctx.fill()

  // --- Inner background ---
  ctx.fillStyle = '#0A0A0A'
  roundedRect(ctx, 3, 3, W - 6, H - 6, 21)
  ctx.fill()

  let y = PAD

  // --- Header ---
  ctx.font = '700 10px "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = 'rgba(255,255,255,0.4)'
  ctx.textBaseline = 'top'
  ctx.textAlign = 'left';  ctx.fillText('PRODEÁ', 20, y)
  ctx.textAlign = 'right'; ctx.fillText(jornada.toUpperCase(), W - 20, y)
  y += 12 + GAP

  // --- Avatar ---
  ctx.beginPath()
  ctx.arc(CX, y + 36, 36, 0, Math.PI * 2)
  ctx.fillStyle = accent
  ctx.fill()
  ctx.font = '900 26px "Bebas Neue", "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = '#0A0A0A'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'middle'
  ctx.fillText(avatar, CX, y + 36)
  y += 72 + GAP

  // --- Username ---
  ctx.font = '700 22px "Bebas Neue", "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = '#FFFFFF'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  ctx.fillText(username, CX, y)
  y += 26 + GAP

  // --- Divider ---
  const divGrad = ctx.createLinearGradient(20, 0, W - 20, 0)
  divGrad.addColorStop(0,   'transparent')
  divGrad.addColorStop(0.5, accent + '80')
  divGrad.addColorStop(1,   'transparent')
  ctx.fillStyle = divGrad
  ctx.fillRect(20, y, W - 40, 1)
  y += 1 + GAP

  // --- Emoji ---
  ctx.font = '60px serif'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  ctx.fillText(emoji, CX, y)
  y += 68 + 8

  // --- Badge label ---
  ctx.font = '700 26px "Bebas Neue", "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = accent
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  ctx.fillText(label, CX, y)
  y += 30 + GAP

  // --- Stats ---
  const hasRank = rank != null
  const ptsCX = hasRank ? W / 4 : CX
  const rkCX  = hasRank ? (W * 3) / 4 : null

  ctx.textBaseline = 'top'
  ctx.textAlign = 'center'

  ctx.font = '900 38px "Bebas Neue", "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = accent
  ctx.fillText(String(badge.pointsInMatchday), ptsCX, y)
  ctx.font = '400 10px "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = 'rgba(255,255,255,0.4)'
  ctx.fillText('PTS JORNADA', ptsCX, y + 42)

  if (rkCX !== null) {
    ctx.font = '900 38px "Bebas Neue", "DM Sans", system-ui, sans-serif'
    ctx.fillStyle = '#FFFFFF'
    ctx.fillText(`#${rank}`, rkCX, y)
    ctx.font = '400 10px "DM Sans", system-ui, sans-serif'
    ctx.fillStyle = 'rgba(255,255,255,0.4)'
    ctx.fillText('EN TABLA', rkCX, y + 42)
  }
  y += 56 + GAP

  // --- Phrase ---
  ctx.font = 'italic 12px "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = 'rgba(255,255,255,0.5)'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  phraseLines.forEach((line, i) => ctx.fillText(line, CX, y + i * 17))
  y += phraseLines.length * 17 + GAP

  // --- Footer divider ---
  ctx.fillStyle = 'rgba(255,255,255,0.1)'
  ctx.fillRect(20, y, W - 40, 1)
  y += 1 + 12

  // --- Footer text ---
  ctx.font = '700 9px "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = 'rgba(255,255,255,0.25)'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  ctx.fillText(`${tournamentName} · MUNDIAL 2026`, CX, y)

  return new Promise((resolve, reject) =>
    canvas.toBlob(b => b ? resolve(b) : reject(new Error('toBlob failed')), 'image/png')
  )
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

  async function handleShare() {
    if (sharing) return
    setSharing(true)
    setError(null)
    try {
      const blob = await generateCardBlob({ badge, username, tournamentName, rank })
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
        onClick={handleShare}
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
