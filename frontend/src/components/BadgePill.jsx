import { useTranslation } from 'react-i18next'

const BADGE_BG = {
  MVP: 'badge-mvp', Jinx: 'badge-jinx', Oracle: 'badge-oracle',
  Sniper: 'badge-sniper', Choker: 'badge-choker', Goalscorer: 'badge-goalscorer',
  Miser: 'badge-miser', Wobbler: 'badge-wobbler', Clown: 'badge-clown',
  Sleeper: 'badge-sleeper', Lukewarm: 'badge-lukewarm', Champion: 'badge-champion',
  RunnerUp: 'badge-runnerup', ThirdPlace: 'badge-thirdplace',
  LastPlace: 'badge-jinx', SecondToLast: 'badge-wobbler',
  TournamentGoalscorer: 'badge-goalscorer', TournamentMiser: 'badge-miser',
}

const EMOJIS = {
  MVP: '🏆', Jinx: '💀', Oracle: '🔮',
  Sniper: '🎯', Choker: '❄️', Goalscorer: '⚽', Miser: '⛏️', Wobbler: '🥴', Clown: '🤡', Sleeper: '😴', Lukewarm: '🌡️',
  TotalChoke: '🥶', HotStreak: '🔥', TheWall: '🧱', TheGhost: '👻', TripleJinx: '💀🔥', TotalLukewarm: '🌡️', SerialGoalscorer: '⚽', TotalMiser: '⛏️',
  Champion: '🏆', RunnerUp: '🥈', ThirdPlace: '🥉', LastPlace: '💀', SecondToLast: '🥴',
  TournamentGoalscorer: '⚽', TournamentMiser: '⛏️',
}

export function BadgePill({ type, className = '', showTag = true }) {
  const { t } = useTranslation()
  const bg = BADGE_BG[type] || 'badge-sleeper'
  const emoji = EMOJIS[type] || '❓'
  const label = t(`badges.${type}`, type)
  const tag = t(`badgeTags.${type}`, '')
  return (
    <span className={`inline-flex items-center gap-0.5 ${className}`}>
      <span className={`inline-flex items-center gap-0.5 px-1 py-0.5 rounded-full text-[10px] font-semibold text-white whitespace-nowrap shrink-0 ${bg}`}>
        {emoji} {label}
      </span>
      {showTag && tag && (
        <span className="text-[8px] font-semibold px-1 py-0.5 rounded-full bg-[#2A2A3E] text-[#8A8A9A] whitespace-nowrap shrink-0">
          {tag}
        </span>
      )}
    </span>
  )
}

export { EMOJIS, BADGE_BG }
