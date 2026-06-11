// Genera imágenes para posteos de Facebook/Instagram (1080x1080) con el branding de Prodea
import { createCanvas, loadImage } from 'canvas'
import { writeFileSync, mkdirSync } from 'fs'
import { fileURLToPath } from 'url'
import { dirname, join } from 'path'

const __dir = dirname(fileURLToPath(import.meta.url))
const outDir = join(__dir, '..', '..', 'marketing', 'posts')
mkdirSync(outDir, { recursive: true })

const W = 1080
const H = 1080

const BG = '#0D0D0D'
const SURFACE = '#1A1A2E'
const GREEN = '#00FF87'
const ORANGE = '#FF6B35'
const WHITE = '#FFFFFF'
const GRAY = '#8A8A9A'

const wordmark = await loadImage(join(__dir, '..', 'public', 'logo-wordmark.png'))
const icon = await loadImage(join(__dir, '..', 'public', 'logo-icon.png'))

function hexToRgba(hex, alpha) {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

function roundRect(ctx, x, y, w, h, r) {
  ctx.beginPath()
  ctx.moveTo(x + r, y)
  ctx.arcTo(x + w, y, x + w, y + h, r)
  ctx.arcTo(x + w, y + h, x, y + h, r)
  ctx.arcTo(x, y + h, x, y, r)
  ctx.arcTo(x, y, x + w, y, r)
  ctx.closePath()
}

function drawGlow(ctx, x, y, radius, color, alpha) {
  const g = ctx.createRadialGradient(x, y, 0, x, y, radius)
  g.addColorStop(0, hexToRgba(color, alpha))
  g.addColorStop(1, hexToRgba(color, 0))
  ctx.fillStyle = g
  ctx.fillRect(x - radius, y - radius, radius * 2, radius * 2)
}

function drawTexture(ctx) {
  // Líneas diagonales sutiles para dar textura, estilo cards de figurita
  ctx.save()
  roundRect(ctx, 28, 28, W - 56, H - 56, 36)
  ctx.clip()
  ctx.strokeStyle = 'rgba(255,255,255,0.025)'
  ctx.lineWidth = 2
  for (let x = -H; x < W + H; x += 36) {
    ctx.beginPath()
    ctx.moveTo(x, 0)
    ctx.lineTo(x + H, H)
    ctx.stroke()
  }
  ctx.restore()
}

function drawBase(ctx) {
  // Fondo con degradé
  const grad = ctx.createLinearGradient(0, 0, W, H)
  grad.addColorStop(0, BG)
  grad.addColorStop(1, SURFACE)
  ctx.fillStyle = grad
  ctx.fillRect(0, 0, W, H)

  // Glows de color en las esquinas (estética neón)
  drawGlow(ctx, 80, 80, 480, GREEN, 0.16)
  drawGlow(ctx, W - 60, H - 60, 520, ORANGE, 0.14)

  drawTexture(ctx)

  // Marco con borde degradado
  const borderGrad = ctx.createLinearGradient(0, 0, W, H)
  borderGrad.addColorStop(0, GREEN)
  borderGrad.addColorStop(1, ORANGE)
  ctx.lineWidth = 8
  ctx.strokeStyle = borderGrad
  roundRect(ctx, 28, 28, W - 56, H - 56, 36)
  ctx.stroke()
}

function drawIconBadge(ctx, y, size = 120) {
  const w = size * (icon.width / icon.height)
  drawGlow(ctx, W / 2, y + size / 2, size * 1.3, GREEN, 0.35)
  ctx.drawImage(icon, (W - w) / 2, y, w, size)
}

function drawLogo(ctx) {
  const logoW = 360
  const logoH = (wordmark.height / wordmark.width) * logoW
  ctx.drawImage(wordmark, (W - logoW) / 2, H - 130, logoW, logoH)
}

function drawCTA(ctx, y, text = 'prodea.app') {
  ctx.font = 'bold 44px sans-serif'
  ctx.textAlign = 'center'
  const padX = 50
  const w = ctx.measureText(text).width + padX * 2
  const h = 84
  const x = (W - w) / 2

  ctx.save()
  ctx.shadowColor = hexToRgba(GREEN, 0.6)
  ctx.shadowBlur = 30
  ctx.fillStyle = GREEN
  roundRect(ctx, x, y, w, h, h / 2)
  ctx.fill()
  ctx.restore()

  ctx.fillStyle = '#0D0D0D'
  ctx.textBaseline = 'middle'
  ctx.fillText(text, W / 2, y + h / 2 + 4)
  ctx.textBaseline = 'alphabetic'
}

function drawEyebrow(ctx, text, color, y) {
  ctx.font = 'bold 34px sans-serif'
  ctx.textAlign = 'center'
  const padX = 36
  const w = ctx.measureText(text).width + padX * 2
  const h = 64
  const x = (W - w) / 2

  ctx.fillStyle = hexToRgba(color, 0.12)
  roundRect(ctx, x, y, w, h, h / 2)
  ctx.fill()

  ctx.strokeStyle = color
  ctx.lineWidth = 3
  roundRect(ctx, x, y, w, h, h / 2)
  ctx.stroke()

  ctx.fillStyle = color
  ctx.textBaseline = 'middle'
  ctx.fillText(text, W / 2, y + h / 2 + 2)
  ctx.textBaseline = 'alphabetic'
}

// Devuelve las líneas en las que se parte el texto para que entre en maxWidth
function wrapLines(ctx, text, maxWidth) {
  const words = text.split(' ')
  const lines = []
  let current = ''
  for (const word of words) {
    const test = current ? `${current} ${word}` : word
    if (ctx.measureText(test).width > maxWidth && current) {
      lines.push(current)
      current = word
    } else {
      current = test
    }
  }
  if (current) lines.push(current)
  return lines
}

// Texto grande "condensado" (estilo Bebas Neue/Barlow Condensed) + glow neón
function drawHeadline(ctx, text, y, fontSize = 84, color = WHITE, opts = {}) {
  const { scaleX = 0.82, maxWidth = W - 160, glow = true } = opts
  const lineHeight = fontSize * 1.08
  ctx.font = `bold ${fontSize}px sans-serif`
  ctx.textAlign = 'center'
  const lines = wrapLines(ctx, text, maxWidth / scaleX)

  lines.forEach((line, i) => {
    const ly = y + i * lineHeight
    ctx.save()
    ctx.translate(W / 2, ly)
    ctx.scale(scaleX, 1)
    if (glow) {
      ctx.shadowColor = hexToRgba(color, 0.7)
      ctx.shadowBlur = 24
    }
    ctx.fillStyle = color
    ctx.fillText(line, 0, 0)
    ctx.restore()
  })
  return y + (lines.length - 1) * lineHeight
}

function drawBody(ctx, text, y, fontSize = 38, color = GRAY, maxWidth = W - 220, lineHeight = fontSize * 1.4) {
  ctx.font = `${fontSize}px sans-serif`
  ctx.fillStyle = color
  ctx.textAlign = 'center'
  const lines = wrapLines(ctx, text, maxWidth)
  lines.forEach((line, i) => ctx.fillText(line, W / 2, y + i * lineHeight))
  return y + (lines.length - 1) * lineHeight
}

function save(canvas, name) {
  writeFileSync(join(outDir, name), canvas.toBuffer('image/png'))
  console.log(`✓ ${name}`)
}

// ---------------------------------------------------------------------------
// Post 1 — Lanzamiento / Presentación
// ---------------------------------------------------------------------------
{
  const canvas = createCanvas(W, H)
  const ctx = canvas.getContext('2d')
  drawBase(ctx)

  drawIconBadge(ctx, 60, 110)
  drawEyebrow(ctx, '⚽ NUEVO', GREEN, 210)

  let y = drawHeadline(ctx, 'LLEGÓ PRODEA', 380, 110, GREEN)
  y = drawBody(ctx, 'El prode del Mundial 2026 entre amigos', y + 70, 42, WHITE)

  // VS entre los dos motes
  const cardY = y + 90
  const cardW = 380
  const cardH = 220
  const gap = 40

  // El Crack
  let cx = W / 2 - cardW - gap / 2
  ctx.fillStyle = hexToRgba(GREEN, 0.08)
  roundRect(ctx, cx, cardY, cardW, cardH, 24)
  ctx.fill()
  ctx.strokeStyle = GREEN
  ctx.lineWidth = 3
  roundRect(ctx, cx, cardY, cardW, cardH, 24)
  ctx.stroke()
  ctx.font = '64px sans-serif'
  ctx.textAlign = 'center'
  ctx.fillStyle = WHITE
  ctx.fillText('🏆', cx + cardW / 2, cardY + 80)
  ctx.font = 'bold 38px sans-serif'
  ctx.fillStyle = GREEN
  ctx.fillText('EL CRACK', cx + cardW / 2, cardY + 150)

  // VS
  ctx.font = 'bold 40px sans-serif'
  ctx.fillStyle = GRAY
  ctx.fillText('VS', W / 2, cardY + cardH / 2 + 14)

  // El Mufa
  cx = W / 2 + gap / 2
  ctx.fillStyle = hexToRgba(ORANGE, 0.08)
  roundRect(ctx, cx, cardY, cardW, cardH, 24)
  ctx.fill()
  ctx.strokeStyle = ORANGE
  ctx.lineWidth = 3
  roundRect(ctx, cx, cardY, cardW, cardH, 24)
  ctx.stroke()
  ctx.font = '64px sans-serif'
  ctx.fillStyle = WHITE
  ctx.fillText('💀', cx + cardW / 2, cardY + 80)
  ctx.font = 'bold 38px sans-serif'
  ctx.fillStyle = ORANGE
  ctx.fillText('EL MUFA', cx + cardW / 2, cardY + 150)

  drawBody(ctx, 'Cada fecha, alguien se la banca.', cardY + cardH + 70, 36, WHITE)

  drawCTA(ctx, 880, 'prodea.app')
  drawLogo(ctx)

  save(canvas, '1-llego-prodea.png')
}

// ---------------------------------------------------------------------------
// Post 2 — Por qué es distinto / Tabla en vivo
// ---------------------------------------------------------------------------
{
  const canvas = createCanvas(W, H)
  const ctx = canvas.getContext('2d')
  drawBase(ctx)

  drawIconBadge(ctx, 60, 110)
  drawEyebrow(ctx, '⚡ EN VIVO', GREEN, 210)

  let y = drawHeadline(ctx, 'LA TABLA SE MUEVE SOLA', 360, 84, WHITE, { maxWidth: W - 100, scaleX: 0.8 })
  y = drawBody(ctx, 'Mientras se juega el partido, vos mirás cómo cambia todo en tiempo real', y + 70, 40, GRAY, W - 220)

  const items = [
    'Resultados en vivo, sin recargar nada',
    'Card de figurita con tu mote en cada fecha',
    'Gratis para vos y tus amigos',
  ]

  let itemY = y + 110
  for (const item of items) {
    ctx.font = 'bold 44px sans-serif'
    ctx.fillStyle = GREEN
    ctx.textAlign = 'left'
    ctx.shadowColor = hexToRgba(GREEN, 0.7)
    ctx.shadowBlur = 16
    ctx.fillText('✓', 140, itemY)
    ctx.shadowBlur = 0

    ctx.font = '38px sans-serif'
    ctx.fillStyle = WHITE
    const lines = wrapLines(ctx, item, W - 280)
    lines.forEach((line, i) => ctx.fillText(line, 200, itemY + i * 50))
    itemY += 50 * lines.length + 40
  }

  drawCTA(ctx, 880, 'prodea.app')
  drawLogo(ctx)

  save(canvas, '2-tabla-en-vivo.png')
}

// ---------------------------------------------------------------------------
// Post 3 — CTA urgencia / Fecha 1
// ---------------------------------------------------------------------------
{
  const canvas = createCanvas(W, H)
  const ctx = canvas.getContext('2d')
  drawBase(ctx)

  drawIconBadge(ctx, 60, 110)
  drawEyebrow(ctx, '🏆 FECHA 1', ORANGE, 210)

  ctx.font = '130px sans-serif'
  ctx.textAlign = 'center'
  ctx.fillStyle = WHITE
  ctx.fillText('⚽', W / 2, 410)

  let y = drawHeadline(ctx, '¿QUIÉN VA A SER EL CRACK?', 560, 84, GREEN, { maxWidth: W - 140 })
  y = drawBody(ctx, 'Creá tu torneo, invitá a tus amigos y empezá a competir antes de que arranque el Mundial', y + 60, 38, WHITE, W - 220)

  drawCTA(ctx, y + 60, 'CREAR TORNEO → prodea.app')
  drawLogo(ctx)

  save(canvas, '3-fecha-1-cta.png')
}

// ---------------------------------------------------------------------------
// Post 4 — ¡Hoy arranca el Mundial 2026!
// ---------------------------------------------------------------------------
{
  const canvas = createCanvas(W, H)
  const ctx = canvas.getContext('2d')
  drawBase(ctx)

  drawIconBadge(ctx, 60, 110)
  drawEyebrow(ctx, '🔴 HOY', ORANGE, 210)

  ctx.font = '120px sans-serif'
  ctx.textAlign = 'center'
  ctx.fillStyle = WHITE
  ctx.fillText('🏆⚽', W / 2, 415)

  let y = drawHeadline(ctx, '¡ARRANCA EL MUNDIAL 2026!', 565, 84, GREEN, { maxWidth: W - 100 })
  y = drawBody(ctx, '¡Cargá tus pronósticos antes del primer partido!', y + 60, 38, WHITE, W - 220)

  drawCTA(ctx, y + 60, 'prodea.app')
  drawLogo(ctx)

  save(canvas, '4-hoy-arranca-mundial.png')
}
