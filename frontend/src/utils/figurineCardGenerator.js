import i18next from 'i18next'
import { EMOJIS } from '../components/BadgePill'

export const BADGE_GRADIENTS = {
  MVP:                  ['#FFD700', '#FFFDE7', '#F59E0B', '#78350F'],
  Jinx:                 ['#EF4444', '#FECACA', '#B91C1C', '#450A0A'],
  Oracle:               ['#A78BFA', '#EDE9FE', '#7C3AED', '#3B0764'],
  Sniper:               ['#22D3EE', '#CFFAFE', '#0284C7', '#0C4A6E'],
  Choker:               ['#BAE6FD', '#F0F9FF', '#0284C7', '#0C4A6E'],
  Goalscorer:           ['#22C55E', '#DCFCE7', '#16A34A', '#14532D'],
  Miser:                ['#92400E', '#FEF3C7', '#78350F', '#451A03'],
  Wobbler:              ['#F97316', '#FFF7ED', '#EA580C', '#7C2D12'],
  Clown:                ['#F472B6', '#FCE7F3', '#E11D48', '#4C0519'],
  Sleeper:              ['#94A3B8', '#F1F5F9', '#475569', '#0F172A'],
  Lukewarm:             ['#38BDF8', '#E0F2FE', '#0284C7', '#0C4A6E'],
  Champion:             ['#FFD700', '#FFFDE7', '#DAA520', '#7A5C00'],
  RunnerUp:             ['#C0C0C0', '#F5F5F5', '#9CA3AF', '#4B5563'],
  ThirdPlace:           ['#CD7F32', '#FBE3C7', '#A6651A', '#5C3A12'],
  LastPlace:            ['#EF4444', '#FECACA', '#B91C1C', '#450A0A'],
  SecondToLast:         ['#F97316', '#FFF7ED', '#EA580C', '#7C2D12'],
  TournamentGoalscorer: ['#22C55E', '#DCFCE7', '#16A34A', '#14532D'],
  TournamentMiser:      ['#92400E', '#FEF3C7', '#78350F', '#451A03'],
}

export const BADGE_ACCENT = {
  MVP:                  '#F59E0B',
  Jinx:                 '#EF4444',
  Oracle:               '#8B5CF6',
  Sniper:               '#06B6D4',
  Choker:               '#38BDF8',
  Goalscorer:           '#22C55E',
  Miser:                '#92400E',
  Wobbler:              '#F97316',
  Clown:                '#EC4899',
  Sleeper:              '#64748B',
  Lukewarm:             '#38BDF8',
  Champion:             '#FFD700',
  RunnerUp:             '#C0C0C0',
  ThirdPlace:           '#CD7F32',
  LastPlace:            '#EF4444',
  SecondToLast:         '#F97316',
  TournamentGoalscorer: '#22C55E',
  TournamentMiser:      '#92400E',
}

export function getBadgeLabel(type) {
  return i18next.t(`badges.${type}`, type)
}
export const BADGE_LABELS = new Proxy({}, { get: (_, key) => getBadgeLabel(key) })

export function getBadgePhrase(badgeType, userId = 0, occurrenceIndex = 0) {
  const phrases = i18next.t(`badgePhrases.${badgeType}`, { returnObjects: true })
  if (!Array.isArray(phrases) || phrases.length === 0) return ''
  const seed = (userId * 31 + badgeType.length * 17) >>> 0
  const indices = phrases.map((_, i) => i)
  for (let i = indices.length - 1; i > 0; i--) {
    const j = (seed + i * 7) % (i + 1)
    ;[indices[i], indices[j]] = [indices[j], indices[i]]
  }
  return phrases[indices[occurrenceIndex % phrases.length]]
}

const FINAL_RESULT_TYPES = ['Champion', 'RunnerUp', 'ThirdPlace', 'LastPlace', 'SecondToLast', 'TournamentGoalscorer', 'TournamentMiser']
export const isFinalResultBadge = (badgeType) => FINAL_RESULT_TYPES.includes(badgeType)

export function jornadaLabel(phase, matchday, badgeType) {
  if (isFinalResultBadge(badgeType)) return i18next.t('common.worldCup')
  if (phase === 'Group') return i18next.t('phases.Group', { matchday })
  return i18next.t(`phases.${phase}`, phase)
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

export async function generateCardBlob({ badge, username, tournamentName, rank }) {
  await document.fonts.ready

  let wordmarkImg = null
  try { wordmarkImg = await loadImage('/logo-wordmark.png') } catch { /* fallback a texto */ }

  let trophyImg = null
  if (badge.badgeType === 'Champion') {
    try { trophyImg = await loadImage('/trophy.svg') } catch { /* fallback a emoji */ }
  }

  const W      = 320
  const SCALE  = 3
  const BORDER = 5
  const PAD    = 20
  const GAP    = 14
  const CX     = W / 2
  const stops  = BADGE_GRADIENTS[badge.badgeType] || BADGE_GRADIENTS.Sleeper
  const accent = BADGE_ACCENT[badge.badgeType]    || '#00FF87'
  const emoji  = EMOJIS[badge.badgeType]          || '❓'
  const label  = BADGE_LABELS[badge.badgeType]    || badge.badgeType
  const tag    = i18next.t(`badgeTags.${badge.badgeType}`, '') || null
  const finalResult = isFinalResultBadge(badge.badgeType)
  const jornada = jornadaLabel(badge.phase, badge.matchday, badge.badgeType)
  const phrase  = `"${getBadgePhrase(badge.badgeType, badge.userId ?? 0, badge.occurrenceIndex ?? 0)}"`

  const tmp = document.createElement('canvas').getContext('2d')
  tmp.font = 'italic 11px "DM Sans", system-ui, sans-serif'
  const phraseLines = wrapLines(tmp, phrase, W - 52)

  const H = PAD + 14 + GAP
          + 30 + GAP
          + 1 + GAP
          + 68 + 8 + 30 + 6
          + (tag ? 14 + GAP : GAP)
          + 56 + GAP
          + phraseLines.length * 17
          + GAP + 1 + 12 + 12 + PAD

  const canvas = document.createElement('canvas')
  canvas.width  = W * SCALE
  canvas.height = H * SCALE
  const ctx = canvas.getContext('2d')
  ctx.scale(SCALE, SCALE)

  const borderGrad = ctx.createLinearGradient(0, 0, W, H)
  borderGrad.addColorStop(0,    stops[0])
  borderGrad.addColorStop(0.18, stops[1])
  borderGrad.addColorStop(0.38, stops[0])
  borderGrad.addColorStop(0.55, stops[2])
  borderGrad.addColorStop(0.72, stops[3])
  borderGrad.addColorStop(0.88, stops[1])
  borderGrad.addColorStop(1,    stops[0])
  ctx.fillStyle = borderGrad
  roundedRect(ctx, 0, 0, W, H, 24)
  ctx.fill()

  ctx.fillStyle = '#0A0A0A'
  roundedRect(ctx, BORDER, BORDER, W - BORDER * 2, H - BORDER * 2, 20)
  ctx.fill()

  ctx.strokeStyle = stops[0] + '50'
  ctx.lineWidth = 0.75
  roundedRect(ctx, BORDER + 1.5, BORDER + 1.5, W - (BORDER + 1.5) * 2, H - (BORDER + 1.5) * 2, 18.5)
  ctx.stroke()

  let y = PAD

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

  ctx.font = '700 26px "Bebas Neue", "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = '#FFFFFF'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  ctx.fillText(username, CX, y)
  y += 30 + GAP

  const divGrad = ctx.createLinearGradient(20, 0, W - 20, 0)
  divGrad.addColorStop(0,   'transparent')
  divGrad.addColorStop(0.5, accent + '80')
  divGrad.addColorStop(1,   'transparent')
  ctx.fillStyle = divGrad
  ctx.fillRect(20, y, W - 40, 1)
  y += 1 + GAP

  if (trophyImg) {
    const trophyH = 76
    const trophyW = trophyH * (trophyImg.width / trophyImg.height)
    ctx.drawImage(trophyImg, CX - trophyW / 2, y - 4, trophyW, trophyH)
  } else {
    ctx.font = '60px serif'
    ctx.textAlign = 'center'
    ctx.textBaseline = 'top'
    ctx.fillText(emoji, CX, y)
  }
  y += 68 + 8

  ctx.font = '700 26px "Bebas Neue", "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = accent
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  ctx.fillText(label, CX, y)
  y += 30 + 6

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

  const hasRank = rank != null && !finalResult
  const ptsCX = hasRank ? W / 4 : CX
  const rkCX  = hasRank ? (W * 3) / 4 : null

  ctx.textBaseline = 'top'
  ctx.textAlign = 'center'

  ctx.font = '900 38px "Bebas Neue", "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = accent
  ctx.fillText(String(badge.pointsInMatchday), ptsCX, y)
  ctx.font = '400 10px "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = 'rgba(255,255,255,0.4)'
  ctx.fillText(finalResult ? i18next.t('figurine.totalPts') : i18next.t('figurine.matchdayPts'), ptsCX, y + 42)

  if (rkCX !== null) {
    ctx.font = '900 38px "Bebas Neue", "DM Sans", system-ui, sans-serif'
    ctx.fillStyle = '#FFFFFF'
    ctx.fillText(`#${rank}`, rkCX, y)
    ctx.font = '400 10px "DM Sans", system-ui, sans-serif'
    ctx.fillStyle = 'rgba(255,255,255,0.4)'
    ctx.fillText(i18next.t('figurine.inTable'), rkCX, y + 42)
  }
  y += 56 + GAP

  ctx.font = 'italic 11px "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = 'rgba(255,255,255,0.5)'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  phraseLines.forEach((line, i) => ctx.fillText(line, CX, y + i * 17))
  y += phraseLines.length * 17 + GAP

  ctx.fillStyle = 'rgba(255,255,255,0.1)'
  ctx.fillRect(20, y, W - 40, 1)
  y += 1 + 12

  ctx.font = '700 9px "DM Sans", system-ui, sans-serif'
  ctx.fillStyle = 'rgba(255,255,255,0.25)'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  ctx.fillText(`${tournamentName} · ${i18next.t('common.worldCup').toUpperCase()}`, CX, y)

  return new Promise((resolve, reject) =>
    canvas.toBlob(b => b ? resolve(b) : reject(new Error('toBlob failed')), 'image/png')
  )
}
