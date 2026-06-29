import QRCode from 'qrcode'

export async function shareInviteImage(tournamentName, inviteLink) {
  const W = 800, H = 800
  const canvas = document.createElement('canvas')
  canvas.width = W
  canvas.height = H
  const ctx = canvas.getContext('2d')

  ctx.fillStyle = '#0D0D0D'
  ctx.fillRect(0, 0, W, H)

  ctx.strokeStyle = '#00FF87'
  ctx.lineWidth = 6
  ctx.roundRect(20, 20, W - 40, H - 40, 24)
  ctx.stroke()

  ctx.fillStyle = '#8A8A9A'
  ctx.font = '500 28px Arial'
  ctx.textAlign = 'center'
  ctx.fillText('¡Unite al torneo!', W / 2, 100)

  ctx.fillStyle = '#FFFFFF'
  ctx.font = 'bold 60px Arial Black, Arial'
  ctx.textAlign = 'center'
  const name = tournamentName ?? ''
  ctx.fillText(name.length > 20 ? name.slice(0, 20) + '…' : name, W / 2, 175)

  const qrDataUrl = await QRCode.toDataURL(inviteLink, {
    width: 320, margin: 2,
    color: { dark: '#FFFFFF', light: '#111111' },
  })
  const qrImg = new Image()
  qrImg.src = qrDataUrl
  await new Promise((r) => { qrImg.onload = r })
  const qrSize = 320
  ctx.drawImage(qrImg, (W - qrSize) / 2, 230, qrSize, qrSize)

  ctx.fillStyle = '#8A8A9A'
  ctx.font = '24px Arial'
  ctx.fillText('Escaneá para unirte', W / 2, 590)

  ctx.strokeStyle = '#2A2A3E'
  ctx.lineWidth = 1
  ctx.beginPath()
  ctx.moveTo(60, 620)
  ctx.lineTo(W - 60, 620)
  ctx.stroke()

  const wordmark = new Image()
  wordmark.src = '/logo-wordmark.png'
  await new Promise((r) => { wordmark.onload = r; wordmark.onerror = r })
  const wmH = 70
  const wmW = wordmark.naturalWidth * (wmH / wordmark.naturalHeight)
  ctx.drawImage(wordmark, (W - wmW) / 2, 648, wmW, wmH)

  ctx.fillStyle = '#8A8A9A'
  ctx.font = '22px Arial'
  ctx.fillText('prodea.app', W / 2, 738)

  canvas.toBlob(async (blob) => {
    const file = new File([blob], 'torneo-prodea.png', { type: 'image/png' })
    if (navigator.canShare?.({ files: [file] })) {
      await navigator.share({ files: [file], title: tournamentName })
    } else {
      const a = document.createElement('a')
      a.href = URL.createObjectURL(blob)
      a.download = 'torneo-prodea.png'
      a.click()
    }
  }, 'image/png')
}
