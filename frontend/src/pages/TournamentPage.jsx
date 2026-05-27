import { useEffect, useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { Share2, ChevronLeft, Wifi } from 'lucide-react'
import { api } from '../services/api'
import { useTournamentStore } from '../store/tournamentStore'
import { useAuthStore } from '../store/authStore'
import { joinTournament, leaveTournament, onMatchUpdated } from '../services/signalr'
import { BadgePill } from '../components/BadgePill'
import ApiStatusBanner from '../components/ApiStatusBanner'

const MATCHDAY_TAB_LABEL = {
  1: 'Fecha 1', 2: 'Fecha 2', 3: 'Fecha 3',
  4: 'Dieciseisavos', 5: 'Octavos', 6: 'Cuartos',
  7: 'Semis', 8: '3er Puesto', 9: 'Final',
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

export default function TournamentPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  const { matches, setMatches, leaderboard, setLeaderboard, updateMatchLive } = useTournamentStore()
  const [tournament, setTournament] = useState(null)
  const [tab, setTab] = useState('fixture')
  const [loading, setLoading] = useState(true)
  const [liveCount, setLiveCount] = useState(0)
  const [selectedMatchday, setSelectedMatchday] = useState(1)
  const tabBarRef = useRef(null)

  useEffect(() => {
    Promise.all([
      api.getTournament(id).then(setTournament),
      api.getMatches(id).then(setMatches),
      api.getLeaderboard(id).then(setLeaderboard),
    ]).finally(() => setLoading(false))

    joinTournament(id)
    const off = onMatchUpdated((update) => {
      updateMatchLive(update)
      api.getLeaderboard(id).then(setLeaderboard)
    })
    return () => { off(); leaveTournament(id) }
  }, [id])

  // Seleccionar automáticamente la fecha más relevante
  useEffect(() => {
    if (matches.length === 0) return

    const liveMatchday = matches.find((m) => m.status === 'InProgress')?.matchday
    if (liveMatchday) { setSelectedMatchday(liveMatchday); return }

    const nextMatchday = matches
      .filter((m) => m.status === 'Scheduled')
      .sort((a, b) => new Date(a.matchDate) - new Date(b.matchDate))[0]?.matchday
    if (nextMatchday) { setSelectedMatchday(nextMatchday); return }

    const lastMatchday = Math.max(...matches.map((m) => m.matchday ?? 1))
    setSelectedMatchday(lastMatchday)
  }, [matches])

  // Scroll al tab activo cuando cambia
  useEffect(() => {
    const bar = tabBarRef.current
    if (!bar) return
    const active = bar.querySelector('[data-active="true"]')
    active?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' })
  }, [selectedMatchday])

  useEffect(() => {
    setLiveCount(matches.filter((m) => m.status === 'InProgress').length)
  }, [matches])

  function copyInvite() {
    const link = `${window.location.origin}/join/${tournament?.inviteLink}`
    navigator.clipboard.writeText(link).catch(() => {})
  }

  if (loading) return <LoadingScreen />

  const matchdays = [...new Set(matches.map((m) => m.matchday ?? 1))].sort((a, b) => a - b)
  const visibleMatches = matches.filter((m) => (m.matchday ?? 1) === selectedMatchday)
  const currentPhase = visibleMatches[0]?.phase

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D]">
      {/* Header */}
      <div className="px-4 pt-12 pb-4 bg-[#1A1A2E]">
        <div className="flex items-center gap-3 mb-3">
          <button onClick={() => navigate('/')} className="text-[#8A8A9A] active:text-white">
            <ChevronLeft size={24} />
          </button>
          <div className="flex-1 min-w-0">
            <h1 className="text-xl font-bold text-white truncate">{tournament?.name}</h1>
            <p className="text-[#8A8A9A] text-xs">{tournament?.participantCount ?? leaderboard.length} participantes</p>
          </div>
          <button
            onClick={copyInvite}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[#00FF87]/10 text-[#00FF87] text-xs font-semibold"
          >
            <Share2 size={14} /> Invitar
          </button>
        </div>

        {liveCount > 0 && (
          <div className="flex items-center gap-2 px-3 py-2 rounded-xl bg-[#FF6B35]/10 border border-[#FF6B35]/30 mb-2">
            <Wifi size={14} className="text-[#FF6B35] animate-pulse" />
            <span className="text-[#FF6B35] text-xs font-semibold">
              {liveCount} partido{liveCount > 1 ? 's' : ''} en curso — actualizando en tiempo real
            </span>
          </div>
        )}
        <ApiStatusBanner hasLiveMatches={liveCount > 0} />

        {/* Fixture / Tabla */}
        <div className="flex gap-1 mt-1 bg-[#0D0D0D]/60 rounded-xl p-1">
          {['fixture', 'tabla'].map((t) => (
            <button
              key={t}
              onClick={() => setTab(t)}
              className={`flex-1 py-2 rounded-lg text-sm font-semibold transition-colors ${
                tab === t ? 'bg-[#00FF87] text-black' : 'text-[#8A8A9A]'
              }`}
            >
              {t === 'fixture' ? 'Fixture' : 'Tabla'}
            </button>
          ))}
        </div>
      </div>

      {/* Tab bar de fechas */}
      {tab === 'fixture' && (
        <div
          ref={tabBarRef}
          className="flex gap-2 px-4 py-3 overflow-x-auto bg-[#0D0D0D] border-b border-[#1A1A2E] scrollbar-none"
          style={{ scrollbarWidth: 'none' }}
        >
          {matchdays.map((md) => {
            const isActive = md === selectedMatchday
            const hasLive = matches.some((m) => (m.matchday ?? 1) === md && m.status === 'InProgress')
            return (
              <button
                key={md}
                data-active={isActive}
                onClick={() => setSelectedMatchday(md)}
                className={`relative shrink-0 px-4 py-1.5 rounded-full text-xs font-semibold transition-all ${
                  isActive
                    ? 'bg-[#00FF87] text-black'
                    : 'bg-[#1A1A2E] text-[#8A8A9A] border border-[#2A2A3E]'
                }`}
              >
                {MATCHDAY_TAB_LABEL[md] ?? `Fecha ${md}`}
                {hasLive && (
                  <span className="absolute -top-0.5 -right-0.5 w-2 h-2 rounded-full bg-[#FF6B35]" />
                )}
              </button>
            )
          })}
        </div>
      )}

      {/* Content */}
      <div className="flex-1 overflow-y-auto">
        <AnimatePresence mode="wait">
          {tab === 'fixture' ? (
            <motion.div
              key={`fixture-${selectedMatchday}`}
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
              {visibleMatches.map((m) => (
                <MatchRow key={m.id} match={m} tournamentId={id} navigate={navigate} />
              ))}
            </motion.div>
          ) : (
            <motion.div
              key="tabla"
              initial={{ opacity: 0, x: 10 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -10 }}
              transition={{ duration: 0.2 }}
              className="px-4 py-4 flex flex-col gap-2"
            >
              {leaderboard.map((entry, i) => (
                <LeaderboardRow
                  key={entry.userId}
                  entry={entry}
                  isMe={entry.userId === user?.id}
                  index={i}
                  tournamentId={id}
                  navigate={navigate}
                />
              ))}
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  )
}

function MatchRow({ match, tournamentId, navigate }) {
  const isLive = match.status === 'InProgress'
  const isFinished = match.status === 'Finished'
  const canPredict = match.status === 'Scheduled'
  const hasPred = match.userPrediction !== null

  return (
    <div
      onClick={() => canPredict && navigate(`/torneos/${tournamentId}/match/${match.id}`)}
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

      <div className="flex items-center justify-between">
        <div className="flex-1 text-center">
          <p className="text-sm font-semibold text-white leading-tight">{match.homeTeam}</p>
        </div>

        <div className="flex items-center gap-2 px-3">
          {isFinished || isLive ? (
            <span className="text-2xl font-bold text-white" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>
              {match.homeScore ?? '-'} – {match.awayScore ?? '-'}
            </span>
          ) : (
            <div className="flex flex-col items-center">
              <span className="text-xs text-[#8A8A9A]">
                {new Date(match.matchDate).toLocaleDateString('es-AR', { day: '2-digit', month: 'short' })}
              </span>
              <span className="text-xs text-[#8A8A9A]">
                {new Date(match.matchDate).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })}
              </span>
            </div>
          )}
        </div>

        <div className="flex-1 text-center">
          <p className="text-sm font-semibold text-white leading-tight">{match.awayTeam}</p>
        </div>
      </div>

      {hasPred && (
        <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex items-center justify-center gap-2">
          <span className="text-xs text-[#8A8A9A]">Tu pred:</span>
          <span className="text-xs font-bold text-[#00FF87]">
            {match.userPrediction.predictedHomeScore} – {match.userPrediction.predictedAwayScore}
          </span>
          {isFinished && (
            <span className={`text-xs font-bold ml-2 ${match.userPrediction.pointsEarned > 0 ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
              +{match.userPrediction.pointsEarned} pts
            </span>
          )}
        </div>
      )}
      {canPredict && !hasPred && (
        <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex justify-center">
          <span className="text-xs text-[#FF6B35] font-semibold">Tocar para predecir →</span>
        </div>
      )}
    </div>
  )
}

function LeaderboardRow({ entry, isMe, index, tournamentId, navigate }) {
  const rankColors = ['text-yellow-400', 'text-gray-300', 'text-amber-600']

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.04 }}
      onClick={() => navigate(`/torneos/${tournamentId}/perfil/${entry.userId}`)}
      className={`flex items-center gap-3 p-3 rounded-2xl border cursor-pointer active:border-[#00FF87] transition-colors ${
        isMe ? 'bg-[#00FF87]/5 border-[#00FF87]/30' : 'bg-[#1A1A2E] border-[#2A2A3E]'
      }`}
    >
      <span className={`w-7 text-center font-bold text-sm ${rankColors[index] || 'text-[#8A8A9A]'}`}>{entry.rank}</span>

      <div className="w-9 h-9 rounded-full bg-[#2A2A3E] flex items-center justify-center text-white font-bold text-sm shrink-0">
        {entry.username[0].toUpperCase()}
      </div>

      <div className="flex-1 min-w-0">
        <p className={`font-semibold text-sm truncate ${isMe ? 'text-[#00FF87]' : 'text-white'}`}>
          {entry.username} {isMe && <span className="text-xs font-normal">(vos)</span>}
        </p>
        {entry.currentBadge && <BadgePill type={entry.currentBadge} className="mt-0.5 text-[10px]" />}
      </div>

      <span className="text-xl font-bold text-white" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
        {entry.totalPoints}<span className="text-xs text-[#8A8A9A] ml-0.5">pts</span>
      </span>
    </motion.div>
  )
}

function LoadingScreen() {
  return (
    <div className="flex-1 flex flex-col gap-3 p-4 pt-16 bg-[#0D0D0D]">
      {[1, 2, 3, 4].map((i) => (
        <div key={i} className="h-20 rounded-2xl bg-[#1A1A2E] animate-pulse" />
      ))}
    </div>
  )
}
