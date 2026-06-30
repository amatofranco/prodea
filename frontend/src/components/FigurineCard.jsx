import { useState } from 'react'
import { Share2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { EMOJIS } from './BadgePill'
import {
  BADGE_GRADIENTS, BADGE_ACCENT, BADGE_LABELS,
  isFinalResultBadge, jornadaLabel, generateCardBlob,
} from '../utils/figurineCardGenerator'

export default function FigurineCard({ badge, username, tournamentName, rank }) {
  const { t } = useTranslation()
  const [sharing, setSharing] = useState(false)
  const [error,   setError]   = useState(null)

  const stops  = BADGE_GRADIENTS[badge.badgeType] || BADGE_GRADIENTS.Dormido
  const accent = BADGE_ACCENT[badge.badgeType]    || '#00FF87'
  const emoji  = EMOJIS[badge.badgeType]          || '❓'
  const label  = BADGE_LABELS[badge.badgeType]    || badge.badgeType
  const finalResult = isFinalResultBadge(badge.badgeType)
  const jornada = jornadaLabel(badge.phase, badge.matchday, badge.badgeType)

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
          text: finalResult
            ? `${username} fue "${label}" del torneo · Prodea Mundial 2026`
            : `${username} fue "${label}" en ${jornada} · Prodea Mundial 2026`,
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
        setError(t('errors.generic'))
        console.error('[FigurineCard]', err)
      }
    } finally {
      setSharing(false)
    }
  }

  return (
    <div className="flex flex-col items-center gap-5">
      <div className="w-56" style={{ borderRadius: '24px', background: borderGradient, padding: '4px' }}>
        <div style={{ borderRadius: '20px', background: '#0A0A0A', border: `0.5px solid ${stops[0]}40` }}>
          <div className="px-5 pt-4 pb-5 flex flex-col items-center gap-3">

            <div className="w-full flex justify-between items-center">
              <img src="/logo-wordmark.png" alt="Prodea" style={{ height: '15px', objectFit: 'contain' }} />
              <span className="text-[9px] text-white/40 uppercase tracking-wider">{jornada}</span>
            </div>

            <p className="text-[22px] font-bold text-white text-center leading-tight"
               style={{ fontFamily: 'Bebas Neue, sans-serif', letterSpacing: '0.08em' }}>
              {username}
            </p>

            <div className="w-full h-px" style={{ background: `linear-gradient(to right, transparent, ${accent}80, transparent)` }} />

            <div className="flex flex-col items-center gap-1 mt-1">
              {badge.badgeType === 'Campeon'
                ? <img src="/trophy.svg" alt="" className="h-16 w-auto" />
                : <span className="text-6xl leading-none">{emoji}</span>
              }
              <p className="text-2xl font-bold text-center mt-1"
                 style={{ fontFamily: 'Bebas Neue, sans-serif', letterSpacing: '0.05em', color: accent }}>
                {label}
              </p>
              {t(`badgeTags.${badge.badgeType}`, '') && (
                <span className="text-[9px] font-bold px-1.5 py-0.5 rounded-full whitespace-nowrap"
                      style={{ border: `0.75px solid ${accent}70`, color: accent, background: accent + '25' }}>
                  {t(`badgeTags.${badge.badgeType}`)}
                </span>
              )}
            </div>

            <div className="flex gap-6 justify-center mt-1">
              <div className="text-center">
                <p className="text-3xl font-black leading-none" style={{ fontFamily: 'Bebas Neue, sans-serif', color: accent }}>
                  {badge.pointsInMatchday}
                </p>
                <p className="text-[9px] text-white/40 uppercase tracking-wide mt-0.5">{t(finalResult ? 'common.points' : 'common.points')}</p>
              </div>
              {rank != null && !finalResult && (
                <div className="text-center">
                  <p className="text-3xl font-black text-white leading-none" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                    #{rank}
                  </p>
                  <p className="text-[9px] text-white/40 uppercase tracking-wide mt-0.5">{t('profile.inTournament')}</p>
                </div>
              )}
            </div>

            <p className="text-[10px] italic text-white/50 text-center leading-snug mt-1">
              &ldquo;{badge.randomPhrase}&rdquo;
            </p>

            <div className="w-full flex justify-center pt-2 border-t border-white/10">
              <p className="text-[9px] text-white/25 uppercase tracking-widest font-bold">
                {tournamentName} · {t('common.worldCup')}
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
        {sharing ? t('common.loading') : `${t('common.share')} ${t('profile.card').toLowerCase()}`}
      </button>

      {error && <p className="text-red-400 text-xs text-center px-4">{error}</p>}
    </div>
  )
}
