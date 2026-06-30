import { Lock } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { getTeam, getFlagUrl } from '../data/teamsData'
import { pointsColor } from '../utils/pointsColor'

function FlagOnly({ name, label }) {
  const { flag } = getTeam(name)
  const flagUrl = getFlagUrl(flag)
  return (
    <div className="w-9 h-10 rounded-md overflow-hidden bg-[#2A2A3E] flex-shrink-0">
      {flagUrl && <img src={flagUrl} alt={label ?? name} loading="lazy" className="w-full h-full object-cover opacity-85" />}
    </div>
  )
}

function calcLivePoints(pred, homeScore, awayScore) {
  if (!pred || homeScore == null || awayScore == null) return null
  const ph = pred.predictedHomeScore
  const pa = pred.predictedAwayScore
  if (ph === homeScore && pa === awayScore) return 3
  const predWinner = ph > pa ? 'H' : ph < pa ? 'A' : 'D'
  const realWinner = homeScore > awayScore ? 'H' : homeScore < awayScore ? 'A' : 'D'
  return predWinner === realWinner ? 1 : 0
}

function teamDisplay(match, side) {
  const name = side === 'home' ? match.homeTeam : match.awayTeam
  const label = side === 'home' ? match.homeTeamLabel : match.awayTeamLabel
  return name !== 'TBD' ? name : (label ?? name)
}

export function LiveCard({ match, compact = false }) {
  const { t } = useTranslation()
  const pred = match.userPrediction
  const homeDisplay = teamDisplay(match, 'home')
  const awayDisplay = teamDisplay(match, 'away')

  if (compact) {
    return (
      <div className="p-3 rounded-2xl bg-[#FF6B35]/10 border border-[#FF6B35]/40 flex flex-col gap-2">
        <div className="flex items-center gap-1.5">
          <span className="w-1.5 h-1.5 rounded-full bg-[#FF6B35] animate-pulse flex-shrink-0" />
          <span className="text-xs font-bold text-[#FF6B35]">
            {match.livePhase ?? match.minuteDisplay ?? (match.minute != null ? `${match.minute}'` : t('home.live'))}
          </span>
        </div>
        <div className="flex items-center gap-2">
          <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
            <FlagOnly name={match.homeTeam} label={match.homeTeamLabel} />
            <p className="text-[9px] font-semibold text-white text-center leading-tight w-full truncate">{homeDisplay}</p>
          </div>
          <span className="text-2xl font-black text-white tabular-nums flex-shrink-0" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>
            {match.homeScore ?? 0}–{match.awayScore ?? 0}
          </span>
          <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
            <FlagOnly name={match.awayTeam} label={match.awayTeamLabel} />
            <p className="text-[9px] font-semibold text-white text-center leading-tight w-full truncate">{awayDisplay}</p>
          </div>
        </div>
        {pred && (() => {
          const pts = calcLivePoints(pred, match.homeScore, match.awayScore)
          return (
            <div className="pt-2 border-t border-[#FF6B35]/20 flex items-center justify-between">
              <span className="text-[9px] text-[#8A8A9A]">
                PRED <span className="font-bold text-white">{pred.predictedHomeScore}–{pred.predictedAwayScore}</span>
              </span>
              {pts != null && (
                <span className={`text-xs font-black ${pts > 0 ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
                  +{pts}pts
                </span>
              )}
            </div>
          )
        })()}
      </div>
    )
  }

  return (
    <div className="p-4 rounded-2xl bg-[#FF6B35]/10 border border-[#FF6B35]/40">
      <div className="flex items-center justify-center gap-2 mb-3">
        <span className="w-2 h-2 rounded-full bg-[#FF6B35] animate-pulse" />
        <span className="text-sm font-bold text-[#FF6B35] uppercase tracking-wider">{t('home.live')}</span>
        {(match.livePhase != null || match.minuteDisplay != null || match.minute != null) && (
          <span className="text-sm font-bold text-[#FF6B35]">
            · {match.livePhase ?? match.minuteDisplay ?? `${match.minute}'`}
          </span>
        )}
      </div>
      <div className="flex items-center gap-2">
        <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
          <FlagOnly name={match.homeTeam} label={match.homeTeamLabel} />
          <p className="text-[10px] font-semibold text-white text-center leading-tight" style={{ maxWidth: 72, wordBreak: 'break-word' }}>{homeDisplay}</p>
        </div>
        <div className="flex items-center gap-2 px-2">
          <span className="text-4xl font-black text-white tabular-nums" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>{match.homeScore ?? 0}</span>
          <span className="text-2xl text-[#3A3A4E] font-light">–</span>
          <span className="text-4xl font-black text-white tabular-nums" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>{match.awayScore ?? 0}</span>
        </div>
        <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
          <FlagOnly name={match.awayTeam} label={match.awayTeamLabel} />
          <p className="text-[10px] font-semibold text-white text-center leading-tight" style={{ maxWidth: 72, wordBreak: 'break-word' }}>{awayDisplay}</p>
        </div>
      </div>
      {match.goals && match.goals.length > 0 && (() => {
        const homeGoals = match.goals.filter(g => g.team === match.homeTeam)
        const awayGoals = match.goals.filter(g => g.team !== match.homeTeam)
        return (
          <div className="mt-2 grid grid-cols-2 gap-x-2 items-start">
            <div className="flex flex-col gap-1">
              {homeGoals.map((g, i) => (
                <div key={i} className="flex items-center gap-1 text-[10px] text-[#8A8A9A] min-w-0">
                  <span className="shrink-0">⚽</span>
                  <span className="truncate">{g.scorer}</span>
                  <span className="shrink-0 text-[#FF6B35]/60">{g.minute}</span>
                </div>
              ))}
            </div>
            <div className="flex flex-col gap-1 items-end">
              {awayGoals.map((g, i) => (
                <div key={i} className="flex items-center gap-1 text-[10px] text-[#8A8A9A] min-w-0">
                  <span className="shrink-0 text-[#FF6B35]/60">{g.minute}</span>
                  <span className="truncate">{g.scorer}</span>
                  <span className="shrink-0">⚽</span>
                </div>
              ))}
            </div>
          </div>
        )
      })()}
      {pred && (() => {
        const pts = calcLivePoints(pred, match.homeScore, match.awayScore)
        return (
          <div className="mt-3 pt-3 border-t border-[#FF6B35]/20 flex items-center justify-between">
            <span className="text-[9px] text-[#8A8A9A]">
              PRED <span className="font-bold text-white">{pred.predictedHomeScore}–{pred.predictedAwayScore}</span>
            </span>
            {pts != null && (
              <span className={`text-xs font-black ${pts > 0 ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
                +{pts}pts
              </span>
            )}
          </div>
        )
      })()}
    </div>
  )
}

export function FinishedCard({ match }) {
  const { t } = useTranslation()
  const pred = match.userPrediction
  const home = match.homeScore ?? 0
  const away = match.awayScore ?? 0
  const homeDisplay = teamDisplay(match, 'home')
  const awayDisplay = teamDisplay(match, 'away')

  return (
    <div className="p-3 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] flex flex-col gap-2">
      <div className="flex items-center gap-2">
        <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
          <FlagOnly name={match.homeTeam} label={match.homeTeamLabel} />
          <p className="text-[9px] font-semibold text-white text-center leading-tight w-full truncate">{homeDisplay}</p>
        </div>
        <div className="flex flex-col items-center gap-0.5 flex-shrink-0">
          <span className="text-2xl font-black text-white tabular-nums" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>
            {home}–{away}
          </span>
          {match.homePenaltyScore != null && match.awayPenaltyScore != null && (
            <span className="text-[9px] text-[#F59E0B] font-semibold">({match.homePenaltyScore}-{match.awayPenaltyScore})</span>
          )}
          <span className="text-[8px] uppercase tracking-wider text-[#3A3A4E] font-semibold">{t('matches.final')}</span>
        </div>
        <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
          <FlagOnly name={match.awayTeam} label={match.awayTeamLabel} />
          <p className="text-[9px] font-semibold text-white text-center leading-tight w-full truncate">{awayDisplay}</p>
        </div>
      </div>
      {pred && (
        <div className="pt-2 border-t border-[#2A2A3E] flex items-center justify-between">
          <span className="text-[9px] text-[#8A8A9A]">
            PRED <span className="font-bold text-white">{pred.predictedHomeScore}–{pred.predictedAwayScore}</span>
          </span>
          <span className={`text-xs font-black ${pred.pointsEarned > 0 ? pointsColor(pred.pointsEarned) : 'text-[#3A3A4E]'}`}>
            +{pred.pointsEarned}pts
          </span>
        </div>
      )}
    </div>
  )
}

export function PlaceholderCard({ match }) {
  const homeDisplay = teamDisplay(match, 'home')
  const awayDisplay = teamDisplay(match, 'away')
  const dateLabel = new Date(match.matchDate).toLocaleDateString(undefined, { day: 'numeric', month: 'short' })
  return (
    <div className="p-3 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] border-dashed flex flex-col gap-2 opacity-50">
      <div className="flex items-center gap-2">
        <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
          <FlagOnly name={match.homeTeam} label={match.homeTeamLabel} />
          <p className="text-[9px] font-semibold text-white text-center leading-tight w-full truncate">{homeDisplay}</p>
        </div>
        <div className="flex flex-col items-center gap-0.5 flex-shrink-0">
          <span className="text-base font-black text-[#3A3A4E] tabular-nums" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>VS</span>
          <span className="text-[8px] uppercase tracking-wider text-[#3A3A4E] font-semibold">{dateLabel}</span>
        </div>
        <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
          <FlagOnly name={match.awayTeam} label={match.awayTeamLabel} />
          <p className="text-[9px] font-semibold text-white text-center leading-tight w-full truncate">{awayDisplay}</p>
        </div>
      </div>
    </div>
  )
}

export function UpcomingCard({ match, navigate }) {
  const { t } = useTranslation()
  const hasPred = !!match.userPrediction
  const matchDate = new Date(match.matchDate)
  const now = new Date()
  const teamsConfirmed = match.homeTeam !== 'TBD' && match.awayTeam !== 'TBD'
  const isLocked = matchDate - now < 15 * 60 * 1000
  const canPredict = teamsConfirmed && !isLocked
  const isToday = matchDate.toDateString() === now.toDateString()
  const isTomorrow = matchDate.toDateString() === new Date(now.getTime() + 86400000).toDateString()
  const dayLabel = isToday ? t('matches.today') : isTomorrow ? t('matches.tomorrow') : matchDate.toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' })
  const timeLabel = matchDate.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
  const homeDisplay = match.homeTeam !== 'TBD' ? match.homeTeam : (match.homeTeamLabel ?? t('matches.teamsToConfirm'))
  const awayDisplay = match.awayTeam !== 'TBD' ? match.awayTeam : (match.awayTeamLabel ?? t('matches.teamsToConfirm'))

  return (
    <div
      onClick={() => canPredict && navigate(`/predicciones/${match.id}`)}
      className={`p-3 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] transition-colors ${canPredict ? 'active:border-[#00FF87] cursor-pointer' : 'cursor-default'}`}
    >
      <div className="flex items-center gap-3">
        {teamsConfirmed
          ? <FlagOnly name={match.homeTeam} label={match.homeTeamLabel} />
          : <div className="w-9 h-10 rounded-md bg-[#2A2A3E] flex-shrink-0" />
        }
        <div className="flex flex-col flex-1 min-w-0 gap-0.5">
          <span className="text-[10px] text-[#8A8A9A]">{dayLabel} · {timeLabel}</span>
          <span className="text-xs font-semibold text-white leading-tight">
            {homeDisplay} <span className="text-[#3A3A4E]">vs</span> {awayDisplay}
          </span>
        </div>
        {teamsConfirmed
          ? <FlagOnly name={match.awayTeam} label={match.awayTeamLabel} />
          : <div className="w-9 h-10 rounded-md bg-[#2A2A3E] flex-shrink-0" />
        }
      </div>
      <div className="mt-2.5 pt-2.5 border-t border-[#2A2A3E] flex items-center justify-between">
        <span className="text-[9px] uppercase tracking-wider text-[#8A8A9A] font-semibold">{t('matches.prediction')}</span>
        {!teamsConfirmed ? (
          <span className="text-xs font-semibold text-[#8A8A9A]">{t('matches.teamsToConfirm')}</span>
        ) : hasPred ? (
          <div className="flex items-center gap-3">
            <span className="text-sm font-bold text-[#00FF87]">
              {match.userPrediction.predictedHomeScore} – {match.userPrediction.predictedAwayScore}
            </span>
            {canPredict && <span className="text-[10px] text-[#00FF87] font-semibold">Ver →</span>}
          </div>
        ) : isLocked ? (
          <Lock size={13} className="text-[#8A8A9A]" />
        ) : (
          <span className="text-xs font-bold text-[#FF6B35]">{t('matches.predict')}</span>
        )}
      </div>
    </div>
  )
}
