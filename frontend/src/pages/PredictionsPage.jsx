import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { AnimatePresence, motion } from 'framer-motion'
import { Target, Lock } from 'lucide-react'
import { api } from '../services/api'

const PREDICTION_CLOSE_BEFORE_MS = 15 * 60 * 1000
import { getTeam, getFlagUrl } from '../data/teamsData'

const PHASE_ORDER = ['group-1', 'group-2', 'group-3', 'R32', 'R16', 'QF', 'SF', 'ThirdPlace', 'Final']

function useTabLabels() {
  const { t } = useTranslation()
  return {
    'group-1': t('phases.Group', { matchday: 1 }),
    'group-2': t('phases.Group', { matchday: 2 }),
    'group-3': t('phases.Group', { matchday: 3 }),
    R32: t('phases.R32'), R16: t('phases.R16'), QF: t('phases.QF'), SF: t('phases.SF'),
    ThirdPlace: t('phases.ThirdPlace'), Final: t('phases.Final'),
  }
}

function usePhaseLabels() {
  const { t } = useTranslation()
  return {
    Group: t('phasesLong.Group'), R32: t('phasesLong.R32'), R16: t('phasesLong.R16'),
    QF: t('phasesLong.QF'), SF: t('phasesLong.SF'),
    ThirdPlace: t('phasesLong.ThirdPlace'), Final: t('phasesLong.Final'),
  }
}

function getTabKey(match) {
  if (match.phase === 'Group') return `group-${match.matchday ?? 1}`
  return match.phase
}

function TeamFlag({ name, label }) {
  const { t } = useTranslation()
  const isTbd = name === 'TBD'
  const { flag } = getTeam(name)
  const flagUrl = getFlagUrl(flag)
  const displayName = name !== 'TBD' ? name : (label ?? name)
  return (
    <div className="flex flex-col items-center gap-1 min-w-0 flex-1">
      <div className="relative w-12 h-14 rounded-lg overflow-hidden bg-[#2A2A3E]">
        {flagUrl ? (
          <img src={flagUrl} alt={displayName} loading="lazy" className="absolute inset-0 w-full h-full object-cover opacity-85" />
        ) : (
          <div className="absolute inset-0 flex items-center justify-center text-[#8A8A9A] text-lg">?</div>
        )}
      </div>
      {isTbd && !label ? (
        <p className="text-[10px] text-[#8A8A9A] text-center leading-tight italic" style={{ maxWidth: 64 }}>{t('predictions.toBeConfirmed')}</p>
      ) : (
        <p className="text-[10px] font-semibold text-white text-center leading-tight" style={{ maxWidth: 64, wordBreak: 'break-word' }}>{displayName}</p>
      )}
    </div>
  )
}

function MatchCard({ match, navigate }) {
  const { t } = useTranslation()
  const isLive = match.status === 'InProgress'
  const isFinished = match.status === 'Finished'
  const teamsConfirmed = match.homeTeam !== 'TBD' && match.awayTeam !== 'TBD'
  const pastDeadline = new Date(match.matchDate) - Date.now() < PREDICTION_CLOSE_BEFORE_MS
  const canPredict = match.status === 'Scheduled' && teamsConfirmed && !pastDeadline
  const pred = match.userPrediction

  return (
    <div
      onClick={() => canPredict && navigate(`/predicciones/${match.id}`)}
      className={`relative p-3 rounded-2xl border transition-colors ${
        isLive
          ? 'bg-[#FF6B35]/5 border-[#FF6B35]/40'
          : isFinished
          ? 'bg-[#1A1A2E] border-[#F59E0B]/20 border-l-2 border-l-[#F59E0B]/60'
          : canPredict
          ? 'bg-[#1A1A2E] border-[#2A2A3E] active:border-[#00FF87] cursor-pointer'
          : 'bg-[#1A1A2E] border-[#2A2A3E]'
      }`}
    >
      {isLive && (
        <span className="absolute top-2 right-2 flex items-center gap-1 text-[10px] text-[#FF6B35] font-bold uppercase">
          <span className="w-1.5 h-1.5 rounded-full bg-[#FF6B35] animate-pulse" />
          {t('home.live').toUpperCase()}
        </span>
      )}
      {pastDeadline && !isLive && !isFinished && (
        <Lock size={13} className="absolute top-2.5 right-2.5 text-[#8A8A9A]" />
      )}

      <div className="flex items-center justify-between gap-2">
        <TeamFlag name={match.homeTeam} label={match.homeTeamLabel} />

        <div className="flex flex-col items-center shrink-0 px-1">
          {isFinished || isLive ? (
            <div className="flex flex-col items-center">
              <span className="text-2xl font-bold text-white" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>
                {match.homeScore ?? '-'} – {match.awayScore ?? '-'}
              </span>
              {match.homePenaltyScore != null && match.awayPenaltyScore != null && (
                <span className="text-[9px] text-[#F59E0B] font-semibold">({match.homePenaltyScore}-{match.awayPenaltyScore} pen.)</span>
              )}
            </div>
          ) : (
            <div className="flex flex-col items-center">
              <span className="text-xs text-[#8A8A9A]">
                {new Date(match.matchDate).toLocaleDateString(undefined, { day: '2-digit', month: 'short' })}
              </span>
              <span className="text-xs text-[#8A8A9A]">
                {new Date(match.matchDate).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
              </span>
            </div>
          )}
          {isFinished
            ? <span className="text-[9px] text-[#F59E0B]/80 font-semibold uppercase tracking-wider mt-1">{t('matches.final')}</span>
            : <span className="text-[10px] text-[#3A3A4E] font-semibold mt-1">VS</span>
          }
        </div>

        <TeamFlag name={match.awayTeam} label={match.awayTeamLabel} />
      </div>

      {pred ? (
        <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex flex-col items-center gap-0.5">
          <div className="flex items-center gap-2">
            <span className="text-xs text-[#8A8A9A]">{t('matches.prediction')}:</span>
            <span className="text-xs font-bold text-[#00FF87]">
              {pred.predictedHomeScore} – {pred.predictedAwayScore}
            </span>
            {pred.predictedPenaltyWinner && (
              <span className="text-xs text-[#F59E0B]">
                · {t('predictions.advances')} {pred.predictedPenaltyWinner === 'home' ? match.homeTeam : match.awayTeam}
              </span>
            )}
            {isFinished && (
              <span className={`text-xs font-bold ml-1 ${pred.pointsEarned > 0 ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
                +{pred.pointsEarned} pts
              </span>
            )}
          </div>
        </div>
      ) : canPredict ? (
        <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex justify-center">
          <span className="text-xs text-[#FF6B35] font-semibold">{t('predictions.tapToPredict')}</span>
        </div>
      ) : !teamsConfirmed ? (
        <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex justify-center">
          <span className="text-xs text-[#8A8A9A]">{t('matches.teamsToConfirm')}</span>
        </div>
      ) : pastDeadline && match.status === 'Scheduled' ? (
        <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex justify-center">
          <span className="text-xs text-[#8A8A9A]">{t('predictions.predictionsClosed')}</span>
        </div>
      ) : null}
    </div>
  )
}

function ChampionPickEntry({ navigate, myPick, isLocked }) {
  const { t } = useTranslation()
  const flagUrl = myPick ? getFlagUrl(getTeam(myPick).flag) : null
  return (
    <div
      onClick={() => navigate('/predicciones/campeon')}
      className="flex items-center gap-3 px-4 py-3 border-b border-[#1A1A2E] bg-[#0D0D0D] cursor-pointer active:bg-[#1A1A2E] transition-colors"
    >
      <div className="w-12 h-[34px] rounded-lg overflow-hidden bg-[#F59E0B]/10 border border-[#F59E0B]/30 flex items-center justify-center text-xl flex-shrink-0">
        {flagUrl
          ? <img src={flagUrl} alt={myPick} className="w-full h-full object-cover" />
          : '🏆'
        }
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-white font-semibold text-sm">🏆 {t('predictions.championPick')}</p>
        <p className="text-[#8A8A9A] text-xs truncate">{myPick ?? t('predictions.noChampionPick')}</p>
      </div>
      <span className="text-[#F59E0B] text-xs font-semibold shrink-0">
        {isLocked ? t('predictions.view') : myPick ? t('predictions.change') : t('predictions.choose')}
      </span>
    </div>
  )
}

export default function PredictionsPage() {
  const { t } = useTranslation()
  const tabLabels = useTabLabels()
  const phaseLabels = usePhaseLabels()
  const navigate = useNavigate()
  const [matches, setMatches] = useState([])
  const [tournaments, setTournaments] = useState([])
  const [championPick, setChampionPick] = useState(null)
  const [loading, setLoading] = useState(true)
  const [selectedTab, setSelectedTab] = useState('group-1')
  const tabBarRef = useRef(null)

  useEffect(() => {
    api.getMyPredictions().then(setMatches).finally(() => setLoading(false))
    api.getTournaments().then(setTournaments).catch(() => {})
    api.getChampionPick().then(setChampionPick).catch(() => {})
  }, [])

  useEffect(() => {
    if (matches.length === 0) return

    const liveTab = matches.find((m) => m.status === 'InProgress')
    if (liveTab) { setSelectedTab(getTabKey(liveTab)); return }

    const nextMatch = matches
      .filter((m) => m.status === 'Scheduled')
      .sort((a, b) => new Date(a.matchDate) - new Date(b.matchDate))[0]
    if (nextMatch) { setSelectedTab(getTabKey(nextMatch)); return }

    const tabs = [...new Set(matches.map(getTabKey))].sort(
      (a, b) => PHASE_ORDER.indexOf(a) - PHASE_ORDER.indexOf(b)
    )
    if (tabs.length > 0) setSelectedTab(tabs[tabs.length - 1])
  }, [matches])

  useEffect(() => {
    const bar = tabBarRef.current
    if (!bar) return
    const active = bar.querySelector('[data-active="true"]')
    active?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' })
  }, [selectedTab])

  if (loading) {
    return (
      <div className="flex flex-col gap-3 p-4 pt-16 bg-[#0D0D0D] min-h-full">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="h-24 rounded-2xl bg-[#1A1A2E] animate-pulse" />
        ))}
      </div>
    )
  }

  const allTabs = [...new Set(matches.map(getTabKey))].sort(
    (a, b) => PHASE_ORDER.indexOf(a) - PHASE_ORDER.indexOf(b)
  )
  const visible = matches
    .filter((m) => getTabKey(m) === selectedTab)
    .sort((a, b) => {
      if (a.status === 'InProgress' && b.status !== 'InProgress') return -1
      if (b.status === 'InProgress' && a.status !== 'InProgress') return 1
      return new Date(a.matchDate) - new Date(b.matchDate)
    })
  const currentPhase = visible[0]?.phase

  const predCount = visible.filter((m) => m.userPrediction !== null).length
  const scheduledCount = visible.filter((m) => m.status === 'Scheduled').length

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D]">
      {/* Header */}
      <div className="px-4 pt-12 md:pt-6 pb-4 bg-[#1A1A2E]">
        <div className="flex items-center gap-2 mb-1">
          <Target size={22} className="text-[#00FF87]" />
          <h1 className="text-xl font-bold text-white">{t('predictions.title')}</h1>
        </div>
        <p className="text-[#8A8A9A] text-xs">{t('predictions.subtitle')}</p>
      </div>

      {/* Champion pick — entrada única global */}
      {tournaments.length > 0 && (
        <ChampionPickEntry
          navigate={navigate}
          myPick={championPick?.myPick}
          isLocked={championPick?.isLocked}
        />
      )}

      {/* Tabs */}
      <div
        ref={tabBarRef}
        className="flex gap-2 px-4 py-3 overflow-x-auto bg-[#0D0D0D] border-b border-[#1A1A2E] scrollbar-none"
        style={{ scrollbarWidth: 'none' }}
      >
        {allTabs.map((tab) => {
          const isActive = tab === selectedTab
          const hasLive = matches.some((m) => getTabKey(m) === tab && m.status === 'InProgress')
          return (
            <button
              key={tab}
              data-active={isActive}
              onClick={() => setSelectedTab(tab)}
              className={`relative shrink-0 px-4 py-1.5 rounded-full text-xs font-semibold transition-all ${
                isActive
                  ? 'bg-[#00FF87] text-black'
                  : 'bg-[#1A1A2E] text-[#8A8A9A] border border-[#2A2A3E]'
              }`}
            >
              {tabLabels[tab] ?? tab}
              {hasLive && (
                <span className="absolute -top-0.5 -right-0.5 w-2 h-2 rounded-full bg-[#FF6B35]" />
              )}
            </button>
          )
        })}
      </div>

      {/* Stats bar */}
      {scheduledCount > 0 && (
        <div className="px-4 py-2 bg-[#0D0D0D]">
          <p className="text-xs text-[#8A8A9A]">
            <span className="text-[#00FF87] font-semibold">{predCount}</span> / <span className="font-semibold text-white">{scheduledCount}</span> {t('matches.prediction').toLowerCase()}
          </p>
        </div>
      )}

      {/* Match list */}
      <div className="flex-1 overflow-y-auto">
        <AnimatePresence mode="wait">
          <motion.div
            key={selectedTab}
            initial={{ opacity: 0, y: 6 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -6 }}
            transition={{ duration: 0.18 }}
            className="px-4 py-4 flex flex-col gap-2"
          >
            {currentPhase && (
              <p className="text-[#8A8A9A] text-xs uppercase tracking-widest font-semibold mb-1">
                {phaseLabels[currentPhase]}
              </p>
            )}
            {visible.map((m) => (
              <MatchCard key={m.id} match={m} navigate={navigate} />
            ))}
          </motion.div>
        </AnimatePresence>
      </div>
    </div>
  )
}
