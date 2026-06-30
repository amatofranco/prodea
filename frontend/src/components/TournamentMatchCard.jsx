import { useTranslation } from 'react-i18next'
import FlagImg from './FlagImg'

export default function TournamentMatchCard({ match, onTap }) {
  const { t } = useTranslation()
  const isFinished = match.status === 'Finished'
  const isLive = match.status === 'InProgress'
  const pred = match.userPrediction

  return (
    <div
      onClick={() => isFinished && onTap(match)}
      className={`p-3 rounded-2xl border transition-colors ${
        isLive
          ? 'bg-[#FF6B35]/5 border-[#FF6B35]/40'
          : isFinished
          ? 'bg-[#1A1A2E] border-[#F59E0B]/20 border-l-2 border-l-[#F59E0B]/60 cursor-pointer active:border-[#00FF87]'
          : 'bg-[#1A1A2E] border-[#2A2A3E]'
      }`}
    >
      {isLive && (
        <span className="flex items-center gap-1 text-[10px] text-[#FF6B35] font-bold uppercase mb-1">
          <span className="w-1.5 h-1.5 rounded-full bg-[#FF6B35] animate-pulse" />
          {match.livePhase ?? t('matchCard.live')}
          {!match.livePhase && (match.minuteDisplay || match.minute != null) && ` · ${match.minuteDisplay ?? `${match.minute}'`}`}
        </span>
      )}

      <div className="flex items-center justify-between gap-2">
        <FlagImg name={match.homeTeam} label={match.homeTeamLabel} />
        <div className="flex flex-col items-center shrink-0 px-1">
          {isFinished || isLive ? (
            <div className="flex flex-col items-center">
              <span className="text-xl font-bold text-white" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                {match.homeScore ?? '-'} – {match.awayScore ?? '-'}
              </span>
              {match.homePenaltyScore != null && match.awayPenaltyScore != null && (
                <span className="text-[9px] text-[#F59E0B] font-semibold">({match.homePenaltyScore}-{match.awayPenaltyScore})</span>
              )}
            </div>
          ) : (
            <>
              <span className="text-xs text-[#8A8A9A]">
                {new Date(match.matchDate).toLocaleDateString(undefined, { day: '2-digit', month: 'short' })}
              </span>
              <span className="text-xs text-[#8A8A9A]">
                {new Date(match.matchDate).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
              </span>
            </>
          )}
          {isFinished
            ? <span className="text-[9px] text-[#F59E0B]/80 font-semibold uppercase mt-0.5">{t('matchCard.final')}</span>
            : <span className="text-[9px] text-[#3A3A4E] font-semibold mt-0.5">VS</span>
          }
        </div>
        <FlagImg name={match.awayTeam} label={match.awayTeamLabel} />
      </div>

      <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex items-center justify-between">
        {pred ? (
          <span className="text-xs text-[#8A8A9A]">
            {t('matchCard.prediction')} <span className="text-[#00FF87] font-bold">{pred.predictedHomeScore} – {pred.predictedAwayScore}</span>
            {pred.predictedPenaltyWinner && (
              <span className="text-[#F59E0B]">
                {` · ${t('matchCard.advances')} `}{pred.predictedPenaltyWinner === 'home' ? match.homeTeam : match.awayTeam}
              </span>
            )}
            {isFinished && (
              <span className={`ml-2 font-bold ${pred.pointsEarned > 0 ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
                +{pred.pointsEarned} pts
              </span>
            )}
          </span>
        ) : (
          <span className="text-xs text-[#8A8A9A]">{isFinished ? t('matchCard.noPrediction') : t('matchCard.noPredictionYet')}</span>
        )}
        {isFinished && (
          <span className="text-[10px] text-[#00FF87] font-semibold shrink-0 ml-2">{t('matchCard.viewAll')}</span>
        )}
      </div>

      {match.phase === 'Final' && (
        <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex items-center justify-between">
          {match.userChampionPick ? (
            <span className="text-xs text-[#8A8A9A]">
              🏆 {t('matchCard.championPicked')} <span className="text-[#F59E0B] font-bold">{match.userChampionPick}</span>
              {isFinished && (
                <span className={`ml-2 font-bold ${match.userChampionPickPoints > 0 ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
                  +{match.userChampionPickPoints ?? 0} pts
                </span>
              )}
            </span>
          ) : (
            <span className="text-xs text-[#8A8A9A]">🏆 {t('matchCard.noChampionPick')}</span>
          )}
        </div>
      )}
    </div>
  )
}
