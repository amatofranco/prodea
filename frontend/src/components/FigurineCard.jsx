import { useState } from 'react'
import { Share2 } from 'lucide-react'
import { EMOJIS, BADGE_TAGS } from './BadgePill'

const BADGE_GRADIENTS = {
  Crack:         ['#FFD700', '#FFFDE7', '#F59E0B', '#78350F'],
  Mufa:          ['#EF4444', '#FECACA', '#B91C1C', '#450A0A'],
  Adivino:       ['#A78BFA', '#EDE9FE', '#7C3AED', '#3B0764'],
  Francotirador: ['#22D3EE', '#CFFAFE', '#0284C7', '#0C4A6E'],
  PechoFrio:     ['#BAE6FD', '#F0F9FF', '#0284C7', '#0C4A6E'],
  Goleador:      ['#22C55E', '#DCFCE7', '#16A34A', '#14532D'],
  Rustico:       ['#92400E', '#FEF3C7', '#78350F', '#451A03'],
  Tambaleante:   ['#F97316', '#FFF7ED', '#EA580C', '#7C2D12'],
  Payaso:        ['#F472B6', '#FCE7F3', '#E11D48', '#4C0519'],
  Dormido:       ['#94A3B8', '#F1F5F9', '#475569', '#0F172A'],
  Tibio:         ['#38BDF8', '#E0F2FE', '#0284C7', '#0C4A6E'],
}

const BADGE_ACCENT = {
  Crack:         '#F59E0B',
  Mufa:          '#EF4444',
  Adivino:       '#8B5CF6',
  Francotirador: '#06B6D4',
  PechoFrio:     '#38BDF8',
  Goleador:      '#22C55E',
  Rustico:       '#92400E',
  Tambaleante:   '#F97316',
  Payaso:        '#EC4899',
  Dormido:       '#64748B',
  Tibio:         '#38BDF8',
}

const BADGE_LABELS = {
  Crack:         'El Crack',
  Mufa:          'El Mufa',
  Adivino:       'El Adivino',
  Francotirador: 'El Francotirador',
  PechoFrio:     'El Pecho Frío',
  Goleador:      'El Goleador',
  Rustico:       'El Rústico',
  Tambaleante:   'El Tambaleante',
  Payaso:        'El Payaso',
  Dormido:       'El Dormido',
  Tibio:         'El Tibio',
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

function loadImage(src) {
  return new Promise((resolve, reject) => {
    const img = new Image()
    img.onload = () => resolve(img)
    img.onerror = reject
    img.src = src
  })
}

async function generateCardBlob({ badge, username, tournamentName, rank }) {
  await document.fonts.ready

  let wordmarkImg = null
  try { wordmarkImg = await loadImage('/logo-wordmark.png') } catch { /* fallback a texto */ }

  const W      = 320
  const SCALE  = 3
  const BORDER = 5
  const PAD    = 20
  const GAP    = 14
  const CX     = W / 2
  const stops  = BADGE_GRADIENTS[badge.badgeType] || BADGE_GRADIENTS.Dormido
  const accent = BADGE_ACCENT[badge.badgeType]    || '#00FF87'
  const emoji  = EMOJIS[badge.badgeType]          || '❓'
  const label  = BADGE_LABELS[badge.badgeType]    || badge.badgeType
  const tag    = BADGE_TAGS[badge.badgeType]      || null
  const jornada = jornadaLabel(badge.phase, badge.matchday)
  const phrase  = `"${badge.randomPhrase}"`

  const tmp = document.createElement('canvas').getContext('2d')
  tmp.font = 'italic 11px "DM Sans", system-ui, sans-serif'
  const phraseLines = wrapLines(tmp, phrase, W - 52)

  const H = PAD + 14 + GAP            // header (wordmark)
          + 30 + GAP                  // username
          + 1 + GAP                   // divider
          + 68 + 8 + 30 + 6          // emoji + label
          + (tag ? 14 + GAP : GAP)   // pill opcional
          + 56 + GAP                  // stats
          + phraseLines.length * 17
          + GAP + 1 + 12 + 12 + PAD  // footer

  const canvas = document.createElement('canvas')
  canvas.width  = W * SCALE
  canvas.height = H * SCALE
  const ctx = canvas.getContext('2d')
  ctx.scale(SCALE, SCALE)

  // Borde foil: gradiente con spot de luz central
  const borderGrad = ctx.createLinearGradient(0, 0, W, H)
  borderGrad.addColorStop(0,    stops[0])
  borderGrad.addColorStop(0.18, stops[1])   // spot brillante
  borderGrad.addColorStop(0.38, stops[0])
  borderGrad.addColorStop(0.55, stops[2])
  borderGrad.addColorStop(0.72, stops[3])
  borderGrad.addColorStop(0.88, stops[1])   // segundo spot
  borderGrad.addColorStop(1,    stops[0])
  ctx.fillStyle = borderGrad
  roundedRect(ctx, 0, 0, W, H, 24)
  ctx.fill()

  // Fondo interior oscuro
  ctx.fillStyle = '#0A0A0A'
  roundedRect(ctx, BORDER, BORDER, W - BORDER * 2, H - BORDER * 2, 20)
  ctx.fill()

  // Línea interior fina en color acento
  ctx.strokeStyle = stops[0] + '50'
  ctx.lineWidth = 0.75
  roundedRect(ctx, BORDER + 1.5, BORDER + 1.5, W - (BORDER + 1.5) * 2, H - (BORDER + 1.5) * 2, 18.5)
  ctx.stroke()

  let y = PAD

  // Header: wordmark + jornada
  if (wordmarkImg) {
    const logoH = 16
    const logoW = logoH * (wordmarkImg.width / wordmarkImg.height)
    ctx.drawImage(wordmarkImg, 20, y, logoW, logoH)
  } else {
    ctx.font = '700 10px "DM Sans", system-ui, sans-serif'
    ctx.fillStyle = 'rgba(255,255,255,0.4)'
    ctx.textAlign = 'left'
    ctx.textBaseline = 'top'
    ctx.fillText('Prodea', 20, y)
  }
  ctx.font = '700 10px "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = 'rgba(255,255,255,0.4)'
  ctx.textAlign = 'right'
  ctx.textBaseline = 'top'
  ctx.fillText(jornada.toUpperCase(), W - 20, y)
  y += 14 + GAP

  // Username
  ctx.font = '700 26px "Bebas Neue", "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = '#FFFFFF'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  ctx.fillText(username, CX, y)
  y += 30 + GAP

  // Divider
  const divGrad = ctx.createLinearGradient(20, 0, W - 20, 0)
  divGrad.addColorStop(0,   'transparent')
  divGrad.addColorStop(0.5, accent + '80')
  divGrad.addColorStop(1,   'transparent')
  ctx.fillStyle = divGrad
  ctx.fillRect(20, y, W - 40, 1)
  y += 1 + GAP

  // Emoji
  ctx.font = '60px serif'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  ctx.fillText(emoji, CX, y)
  y += 68 + 8

  // Badge label
  ctx.font = '700 26px "Bebas Neue", "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = accent
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  ctx.fillText(label, CX, y)
  y += 30 + 6

  // Pill centrado debajo del label (solo para Francotirador / Adivino)
  if (tag) {
    ctx.font = '700 9px "DM Sans", system-ui, sans-serif'
    const pillPadX = 7
    const pillW = ctx.measureText(tag).width + pillPadX * 2
    const pillH = 14
    const pillX = CX - pillW / 2
    ctx.fillStyle = accent + '25'
    roundedRect(ctx, pillX, y, pillW, pillH, 7)
    ctx.fill()
    ctx.strokeStyle = accent + '70'
    ctx.lineWidth = 0.75
    roundedRect(ctx, pillX, y, pillW, pillH, 7)
    ctx.stroke()
    ctx.fillStyle = accent
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    ctx.fillText(tag, CX, y + pillH / 2)
    y += pillH + GAP
  } else {
    y += GAP
  }

  // Stats
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

  // Phrase
  ctx.font = 'italic 11px "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = 'rgba(255,255,255,0.5)'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  phraseLines.forEach((line, i) => ctx.fillText(line, CX, y + i * 17))
  y += phraseLines.length * 17 + GAP

  // Footer divider
  ctx.fillStyle = 'rgba(255,255,255,0.1)'
  ctx.fillRect(20, y, W - 40, 1)
  y += 1 + 12

  // Footer text
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

  const stops  = BADGE_GRADIENTS[badge.badgeType] || BADGE_GRADIENTS.Dormido
  const accent = BADGE_ACCENT[badge.badgeType]    || '#00FF87'
  const emoji  = EMOJIS[badge.badgeType]          || '❓'
  const label  = BADGE_LABELS[badge.badgeType]    || badge.badgeType
  const jornada = jornadaLabel(badge.phase, badge.matchday)

  // Gradiente foil con spot de luz diagonal
  const borderGradient = `linear-gradient(135deg, ${stops[0]}, ${stops[1]} 25%, ${stops[0]} 42%, ${stops[2]} 60%, ${stops[3]} 78%, ${stops[1]} 92%, ${stops[0]})`

  async function handleShare() {
    if (sharing) return
    setSharing(true)
    setError(null)
    try {
      const blob = await generateCardBlob({ badge, username, tournamentName, rank })
      const file = new File([blob], `prodea-${username}.png`, { type: 'image/png' })

      if (navigator.canShare?.({ files: [file] })) {
        await navigator.share({
          text: `${username} fue "${label}" en ${jornada} · Prodea Mundial 2026`,
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
      <div className="w-56" style={{ borderRadius: '24px', background: borderGradient, padding: '4px' }}>
        <div style={{ borderRadius: '20px', background: '#0A0A0A', border: `0.5px solid ${stops[0]}40` }}>
          <div className="px-5 pt-4 pb-5 flex flex-col items-center gap-3">

            {/* Header */}
            <div className="w-full flex justify-between items-center">
              <img src="/logo-wordmark.png" alt="Prodea" style={{ height: '15px', objectFit: 'contain' }} />
              <span className="text-[9px] text-white/40 uppercase tracking-wider">{jornada}</span>
            </div>

            {/* Username */}
            <p className="text-[22px] font-bold text-white text-center leading-tight"
               style={{ fontFamily: 'Bebas Neue, sans-serif', letterSpacing: '0.08em' }}>
              {username}
            </p>

            {/* Divider */}
            <div className="w-full h-px" style={{ background: `linear-gradient(to right, transparent, ${accent}80, transparent)` }} />

            {/* Badge */}
            <div className="flex flex-col items-center gap-1 mt-1">
              <span className="text-6xl leading-none">{emoji}</span>
              <p className="text-2xl font-bold text-center mt-1"
                 style={{ fontFamily: 'Bebas Neue, sans-serif', letterSpacing: '0.05em', color: accent }}>
                {label}
              </p>
              {BADGE_TAGS[badge.badgeType] && (
                <span className="text-[9px] font-bold px-1.5 py-0.5 rounded-full whitespace-nowrap"
                      style={{ border: `0.75px solid ${accent}70`, color: accent, background: accent + '25' }}>
                  {BADGE_TAGS[badge.badgeType]}
                </span>
              )}
            </div>

            {/* Stats */}
            <div className="flex gap-6 justify-center mt-1">
              <div className="text-center">
                <p className="text-3xl font-black leading-none" style={{ fontFamily: 'Bebas Neue, sans-serif', color: accent }}>
                  {badge.pointsInMatchday}
                </p>
                <p className="text-[9px] text-white/40 uppercase tracking-wide mt-0.5">pts jornada</p>
              </div>
              {rank != null && (
                <div className="text-center">
                  <p className="text-3xl font-black text-white leading-none" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                    #{rank}
                  </p>
                  <p className="text-[9px] text-white/40 uppercase tracking-wide mt-0.5">en tabla</p>
                </div>
              )}
            </div>

            {/* Phrase */}
            <p className="text-[10px] italic text-white/50 text-center leading-snug mt-1">
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
        {sharing ? 'Generando...' : 'Compartir carta'}
      </button>

      {error && <p className="text-red-400 text-xs text-center px-4">{error}</p>}
    </div>
  )
}
