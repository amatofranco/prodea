import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { Target } from 'lucide-react'
import { api } from '../services/api'

const PREDICTION_CLOSE_BEFORE_MS = 15 * 60 * 1000
import { getTeam, getFlagUrl } from '../data/teamsData'

const PHASE_ORDER = ['group-1', 'group-2', 'group-3', 'R32', 'R16', 'QF', 'SF', 'ThirdPlace', 'Final']

const TAB_LABELS = {
  'group-1': 'Fecha 1',
  'group-2': 'Fecha 2',
  'group-3': 'Fecha 3',
  R32: 'Dieciseisavos',
  R16: 'Octavos',
  QF: 'Cuartos',
  SF: 'Semis',
  ThirdPlace: '3er Puesto',
  Final: 'Final',
}

const PHASE_LABELS = {
  Group: 'Fase de Grupos',
  R32: 'Dieciseisavos de Final',
  R16: 'Octavos de Final',
  QF: 'Cuartos de Final',
  SF: 'Semifinales',
  ThirdPlace: 'Tercer Puesto',
  Final: 'Final',
}

function getTabKey(match) {
  if (match.phase === 'Group') return `group-${match.matchday ?? 1}`
  return match.phase
}

function TeamFlag({ name, label }) {
  const isTbd = name === 'TBD'
  const { flag } = getTeam(name)
  const flagUrl = getFlagUrl(flag)
  const displayName = label ?? name
  return (
    <div className="flex flex-col items-center gap-1 min-w-0 flex-1">
      <div className="relative w-12 h-14 rounded-lg overflow-hidden bg-[#2A2A3E]">
        {flagUrl ? (
          <img src={flagUrl} alt={displayName} className="absolute inset-0 w-full h-full object-cover opacity-85" />
        ) : (
          <div className="absolute inset-0 flex items-center justify-center text-[#8A8A9A] text-lg">?</div>
        )}
      </div>
      {isTbd && !label ? (
        <p className="text-[10px] text-[#8A8A9A] text-center leading-tight italic" style={{ maxWidth: 64 }}>Por confirmar</p>
      ) : (
        <p className="text-[10px] font-semibold text-white text-center leading-tight" style={{ maxWidth: 64, wordBreak: 'break-word' }}>{displayName}</p>
      )}
    </div>
  )
}

function MatchCard({ match, navigate }) {
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
          : canPredict
          ? 'bg-[#1A1A2E] border-[#2A2A3E] active:border-[#00FF87] cursor-pointer'
          : 'bg-[#1A1A2E] border-[#2A2A3E]'
      }`}
    >
      {isLive && (
        <span className="absolute top-2 right-2 flex items-center gap-1 text-[10px] text-[#FF6B35] font-bold uppercase">
          <span className="w-1.5 h-1.5 rounded-full bg-[#FF6B35] animate-pulse" />
          LIVE
        </span>
      )}

      <div className="flex items-center justify-between gap-2">
        <TeamFlag name={match.homeTeam} label={match.homeTeamLabel} />

        <div className="flex flex-col items-center shrink-0 px-1">
          {isFinished || isLive ? (
            <span className="text-2xl font-bold text-white" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>
              {match.homeScore ?? '-'} – {match.awayScore ?? '-'}
            </span>
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
          <span className="text-[10px] text-[#3A3A4E] font-semibold mt-1">VS</span>
        </div>

        <TeamFlag name={match.awayTeam} label={match.awayTeamLabel} />
      </div>

      {pred ? (
        <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex items-center justify-center gap-2">
          <span className="text-xs text-[#8A8A9A]">Tu pred:</span>
          <span className="text-xs font-bold text-[#00FF87]">
            {pred.predictedHomeScore} – {pred.predictedAwayScore}
          </span>
          {isFinished && (
            <span className={`text-xs font-bold ml-2 ${pred.pointsEarned > 0 ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
              +{pred.pointsEarned} pts
            </span>
          )}
        </div>
      ) : canPredict ? (
        <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex justify-center">
          <span className="text-xs text-[#FF6B35] font-semibold">Tocar para predecir →</span>
        </div>
      ) : !teamsConfirmed ? (
        <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex justify-center">
          <span className="text-xs text-[#8A8A9A]">Equipos por confirmar</span>
        </div>
      ) : pastDeadline && match.status === 'Scheduled' ? (
        <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex justify-center">
          <span className="text-xs text-[#8A8A9A]">Predicciones cerradas</span>
        </div>
      ) : null}
    </div>
  )
}

export default function PredictionsPage() {
  const navigate = useNavigate()
  const [matches, setMatches] = useState([])
  const [loading, setLoading] = useState(true)
  const [selectedTab, setSelectedTab] = useState('group-1')
  const tabBarRef = useRef(null)

  useEffect(() => {
    api.getMyPredictions().then(setMatches).finally(() => setLoading(false))
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
  const visible = matches.filter((m) => getTabKey(m) === selectedTab)
  const currentPhase = visible[0]?.phase

  const predCount = visible.filter((m) => m.userPrediction !== null).length
  const scheduledCount = visible.filter((m) => m.status === 'Scheduled').length

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D]">
      {/* Header */}
      <div className="px-4 pt-12 pb-4 bg-[#1A1A2E]">
        <div className="flex items-center gap-2 mb-1">
          <Target size={22} className="text-[#00FF87]" />
          <h1 className="text-xl font-bold text-white">Predicciones</h1>
        </div>
        <p className="text-[#8A8A9A] text-xs">Cargá tus resultados antes de cada partido</p>
      </div>

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
              {TAB_LABELS[tab] ?? tab}
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
            <span className="text-[#00FF87] font-semibold">{predCount}</span> de{' '}
            <span className="font-semibold text-white">{scheduledCount}</span> partidos pendientes con predicción
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
                {PHASE_LABELS[currentPhase]}
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
