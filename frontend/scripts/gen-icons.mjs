// Genera los íconos PNG de la PWA usando Canvas API de Node
import { createCanvas } from 'canvas'
import { writeFileSync, mkdirSync } from 'fs'
import { fileURLToPath } from 'url'
import { dirname, join } from 'path'

const __dir = dirname(fileURLToPath(import.meta.url))
const publicDir = join(__dir, '..', 'public')

function drawIcon(size) {
  const canvas = createCanvas(size, size)
  const ctx = canvas.getContext('2d')
  const r = size * 0.22

  // Background
  ctx.fillStyle = '#0D0D0D'
  ctx.beginPath()
  ctx.roundRect(0, 0, size, size, r)
  ctx.fill()

  // Neon "P"
  ctx.fillStyle = '#00FF87'
  ctx.shadowColor = '#00FF87'
  ctx.shadowBlur = size * 0.08
  ctx.font = `bold ${size * 0.65}px Arial`
  ctx.textAlign = 'center'
  ctx.textBaseline = 'middle'
  ctx.fillText('P', size / 2, size * 0.52)

  return canvas.toBuffer('image/png')
}

for (const size of [192, 512]) {
  const buf = drawIcon(size)
  const path = join(publicDir, `pwa-${size}x${size}.png`)
  writeFileSync(path, buf)
  console.log(`✓ ${path}`)
}
