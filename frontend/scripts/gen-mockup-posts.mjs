// Genera posteos "mirá la app" (1080x1350) con captura real dentro de un mockup
import { createCanvas, loadImage } from 'canvas'
import { writeFileSync, mkdirSync } from 'fs'
import { fileURLToPath } from 'url'
import { dirname, join } from 'path'

const __dir = dirname(fileURLToPath(import.meta.url))
const outDir = join(__dir, '..', '..', 'marketing', 'posts')
mkdirSync(outDir, { recursive: true })

const W = 1080
const H = 1350

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
  const grad = ctx.createLinearGradient(0, 0, W, H)
  grad.addColorStop(0, BG)
  grad.addColorStop(1, SURFACE)
  ctx.fillStyle = grad
  ctx.fillRect(0, 0, W, H)

  drawGlow(ctx, 80, 80, 480, GREEN, 0.16)
  drawGlow(ctx, W - 60, H - 60, 520, ORANGE, 0.14)

  drawTexture(ctx)

  const borderGrad = ctx.createLinearGradient(0, 0, W, H)
  borderGrad.addColorStop(0, GREEN)
  borderGrad.addColorStop(1, ORANGE)
  ctx.lineWidth = 8
  ctx.strokeStyle = borderGrad
  roundRect(ctx, 28, 28, W - 56, H - 56, 36)
  ctx.stroke()
}

function drawIconBadge(ctx, y, size = 110) {
  const w = size * (icon.width / icon.height)
  drawGlow(ctx, W / 2, y + size / 2, size * 1.3, GREEN, 0.35)
  ctx.drawImage(icon, (W - w) / 2, y, w, size)
}

function drawLogo(ctx) {
  const logoW = 360
  const logoH = (wordmark.height / wordmark.width) * logoW
  ctx.drawImage(wordmark, (W - logoW) / 2, H - 110, logoW, logoH)
}

function drawCTA(ctx, y, text = 'prodea.app') {
  ctx.font = 'bold 40px sans-serif'
  ctx.textAlign = 'center'
  const padX = 46
  const w = ctx.measureText(text).width + padX * 2
  const h = 76
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
  ctx.font = 'bold 32px sans-serif'
  ctx.textAlign = 'center'
  const padX = 34
  const w = ctx.measureText(text).width + padX * 2
  const h = 60
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

function drawHeadline(ctx, text, y, fontSize = 70, color = WHITE, opts = {}) {
  const { scaleX = 0.85, maxWidth = W - 140, glow = true } = opts
  const lineHeight = fontSize * 1.1
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
      ctx.shadowBlur = 22
    }
    ctx.fillStyle = color
    ctx.fillText(line, 0, 0)
    ctx.restore()
  })
  return y + (lines.length - 1) * lineHeight
}

function drawBody(ctx, text, y, fontSize = 34, color = GRAY, maxWidth = W - 220, lineHeight = fontSize * 1.4) {
  ctx.font = `${fontSize}px sans-serif`
  ctx.fillStyle = color
  ctx.textAlign = 'center'
  const lines = wrapLines(ctx, text, maxWidth)
  lines.forEach((line, i) => ctx.fillText(line, W / 2, y + i * lineHeight))
  return y + (lines.length - 1) * lineHeight
}

// Mockup de pantalla: marco con borde degradado + glow + screenshot "contain"
async function drawScreenshotFrame(ctx, screenshotPath, frame) {
  const { x, y, w, h } = frame
  const img = await loadImage(screenshotPath)

  drawGlow(ctx, x + w / 2, y + h / 2, Math.max(w, h) * 0.75, GREEN, 0.18)

  // Marco
  ctx.fillStyle = '#000000'
  roundRect(ctx, x, y, w, h, 32)
  ctx.fill()

  // Screenshot (contain, centrado, esquinas redondeadas)
  const pad = 10
  const innerX = x + pad
  const innerY = y + pad
  const innerW = w - pad * 2
  const innerH = h - pad * 2

  const scale = Math.min(innerW / img.width, innerH / img.height)
  const drawW = img.width * scale
  const drawH = img.height * scale
  const drawX = innerX + (innerW - drawW) / 2
  const drawY = innerY + (innerH - drawH) / 2

  ctx.save()
  roundRect(ctx, innerX, innerY, innerW, innerH, 24)
  ctx.clip()
  ctx.fillStyle = SURFACE
  ctx.fillRect(innerX, innerY, innerW, innerH)
  ctx.drawImage(img, drawX, drawY, drawW, drawH)
  ctx.restore()

  // Borde degradado
  const borderGrad = ctx.createLinearGradient(x, y, x + w, y + h)
  borderGrad.addColorStop(0, GREEN)
  borderGrad.addColorStop(1, ORANGE)
  ctx.lineWidth = 5
  ctx.strokeStyle = borderGrad
  roundRect(ctx, x, y, w, h, 32)
  ctx.stroke()
}

function save(canvas, name) {
  writeFileSync(join(outDir, name), canvas.toBuffer('image/png'))
  console.log(`✓ ${name}`)
}

const FRAME = { x: (W - 560) / 2, y: 490, w: 560, h: 650 }

// ---------------------------------------------------------------------------
// Post 5 — Card de mote (figurita)
// ---------------------------------------------------------------------------
{
  const canvas = createCanvas(W, H)
  const ctx = canvas.getContext('2d')
  drawBase(ctx)

  drawIconBadge(ctx, 50, 100)
  drawEyebrow(ctx, '🃏 CARDS', GREEN, 190)

  drawHeadline(ctx, 'TU MOTE, EN UNA CARD PARA COMPARTIR', 320, 60, GREEN, { maxWidth: W - 140 })
  drawBody(ctx, 'Al terminar cada fecha, Prodea te arma tu card de figurita. Mostrásela al grupo... o escondela 😅', 430, 34, WHITE, W - 240)

  await drawScreenshotFrame(ctx, join(__dir, '..', 'public', 'screens', 'carta.png'), FRAME)

  drawCTA(ctx, 1200, 'prodea.app')
  drawLogo(ctx)

  save(canvas, '5-card-mote.png')
}

// ---------------------------------------------------------------------------
// Post 6 — Tabla en vivo
// ---------------------------------------------------------------------------
{
  const canvas = createCanvas(W, H)
  const ctx = canvas.getContext('2d')
  drawBase(ctx)

  drawIconBadge(ctx, 50, 100)
  drawEyebrow(ctx, '📊 EN VIVO', GREEN, 190)

  drawHeadline(ctx, 'MIRÁ EL RANKING EN VIVO DEL TORNEO', 320, 60, GREEN, { maxWidth: W - 140 })
  drawBody(ctx, 'La tabla de posiciones se actualiza sola mientras se juegan los partidos. Sin recargar nada.', 430, 34, WHITE, W - 240)

  await drawScreenshotFrame(ctx, join(__dir, '..', 'public', 'screens', 'tabla.png'), FRAME)

  drawCTA(ctx, 1200, 'prodea.app')
  drawLogo(ctx)

  save(canvas, '6-tabla-en-vivo.png')
}

// ---------------------------------------------------------------------------
// Post 7 — Predicción / picker de goles
// ---------------------------------------------------------------------------
{
  const canvas = createCanvas(W, H)
  const ctx = canvas.getContext('2d')
  drawBase(ctx)

  drawIconBadge(ctx, 50, 100)
  drawEyebrow(ctx, '🎯 PREDICCIÓN', GREEN, 190)

  drawHeadline(ctx, 'CARGÁ TU MARCADOR EN SEGUNDOS', 320, 60, GREEN, { maxWidth: W - 140 })
  drawBody(ctx, 'Elegí el resultado con el picker animado antes de que cierre la ventana de predicción.', 430, 34, WHITE, W - 240)

  await drawScreenshotFrame(ctx, join(__dir, '..', 'public', 'screens', 'prediccion.png'), FRAME)

  drawCTA(ctx, 1200, 'prodea.app')
  drawLogo(ctx)

  save(canvas, '7-prediccion.png')
}
