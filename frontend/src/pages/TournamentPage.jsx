import { useEffect, useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { Share2, ChevronLeft, Wifi, Lock, X } from 'lucide-react'
import { api } from '../services/api'
import { useTournamentStore } from '../store/tournamentStore'
import { useAuthStore } from '../store/authStore'
import { joinTournament, leaveTournament, onMatchUpdated } from '../services/signalr'
import { BadgePill } from '../components/BadgePill'
import ApiStatusBanner from '../components/ApiStatusBanner'
import ChampionPickBanner from '../components/ChampionPickBanner'
import { getTeam, getFlagUrl } from '../data/teamsData'

const PHASE_ORDER = ['group-1', 'group-2', 'group-3', 'R32', 'R16', 'QF', 'SF', 'ThirdPlace', 'Final']
const TAB_LABELS = {
  'group-1': 'Fecha 1', 'group-2': 'Fecha 2', 'group-3': 'Fecha 3',
  R32: 'Dieciseisavos', R16: 'Octavos', QF: 'Cuartos', SF: 'Semis',
  ThirdPlace: '3er Puesto', Final: 'Final',
}
function getPhaseKey(m) {
  return m.phase === 'Group' ? `group-${m.matchday ?? 1}` : m.phase
}

function FlagImg({ name, label, size = 40 }) {
  const { flag } = getTeam(name)
  const url = getFlagUrl(flag)
  const display = label ?? name
  return (
    <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
      <div className="rounded-md overflow-hidden bg-[#2A2A3E]" style={{ width: size, height: Math.round(size * 0.67) }}>
        {url
          ? <img src={url} alt={display} className="w-full h-full object-cover opacity-85" />
          : <div className="w-full h-full flex items-center justify-center text-[#8A8A9A] text-xs">?</div>
        }
      </div>
      <p className="text-[9px] text-white font-medium text-center leading-tight" style={{ maxWidth: size + 8, wordBreak: 'break-word' }}>
        {display === 'TBD' && !label ? 'Por confirmar' : display}
      </p>
    </div>
  )
}

function TournamentMatchCard({ match, onTap }) {
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
          <span className="w-1.5 h-1.5 rounded-full bg-[#FF6B35] animate-pulse" /> LIVE
        </span>
      )}

      <div className="flex items-center justify-between gap-2">
        <FlagImg name={match.homeTeam} label={match.homeTeamLabel} />
        <div className="flex flex-col items-center shrink-0 px-1">
          {isFinished || isLive ? (
            <span className="text-xl font-bold text-white" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
              {match.homeScore ?? '-'} – {match.awayScore ?? '-'}
            </span>
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
            ? <span className="text-[9px] text-[#F59E0B]/80 font-semibold uppercase mt-0.5">Final</span>
            : <span className="text-[9px] text-[#3A3A4E] font-semibold mt-0.5">VS</span>
          }
        </div>
        <FlagImg name={match.awayTeam} label={match.awayTeamLabel} />
      </div>

      <div className="mt-2 pt-2 border-t border-[#2A2A3E] flex items-center justify-between">
        {pred ? (
          <span className="text-xs text-[#8A8A9A]">
            Predicción: <span className="text-[#00FF87] font-bold">{pred.predictedHomeScore} – {pred.predictedAwayScore}</span>
            {isFinished && (
              <span className={`ml-2 font-bold ${pred.pointsEarned > 0 ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
                +{pred.pointsEarned} pts
              </span>
            )}
          </span>
        ) : (
          <span className="text-xs text-[#8A8A9A]">{isFinished ? 'Sin predicción' : 'Sin predicción cargada'}</span>
        )}
        {isFinished && (
          <span className="text-[10px] text-[#00FF87] font-semibold shrink-0 ml-2">Ver todos →</span>
        )}
      </div>
    </div>
  )
}

function MatchPredictionsSheet({ match, predictions, loading, onClose }) {
  const pointColor = (pts) => pts === 3 ? 'text-[#00FF87]' : pts === 1 ? 'text-[#F59E0B]' : 'text-[#8A8A9A]'
  const pointBg   = (pts) => pts === 3 ? 'bg-[#00FF87]/10 border-[#00FF87]/30' : pts === 1 ? 'bg-[#F59E0B]/10 border-[#F59E0B]/30' : 'bg-[#1A1A2E] border-[#2A2A3E]'

  return (
    <div className="fixed inset-0 z-50 flex flex-col justify-end bg-black/60" onClick={onClose}>
      <motion.div
        initial={{ y: '100%' }}
        animate={{ y: 0 }}
        exit={{ y: '100%' }}
        transition={{ type: 'spring', damping: 28, stiffness: 320 }}
        onClick={(e) => e.stopPropagation()}
        className="bg-[#0D0D0D] rounded-t-3xl overflow-hidden"
        style={{ maxHeight: '80vh' }}
      >
        {/* Handle */}
        <div className="flex justify-center pt-3 pb-1">
          <div className="w-10 h-1 rounded-full bg-[#2A2A3E]" />
        </div>

        {/* Match header */}
        <div className="flex items-center justify-between px-5 pb-3">
          <div className="flex items-center gap-2">
            <FlagImg name={match.homeTeam} label={match.homeTeamLabel} size={28} />
            <span className="text-white font-bold text-lg" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
              {match.homeScore} – {match.awayScore}
            </span>
            <FlagImg name={match.awayTeam} label={match.awayTeamLabel} size={28} />
          </div>
          <button onClick={onClose} className="text-[#8A8A9A] active:text-white p-1">
            <X size={20} />
          </button>
        </div>

        <div className="h-px bg-[#1A1A2E] mx-5" />

        {/* Predictions list */}
        <div className="overflow-y-auto px-5 py-3 flex flex-col gap-2" style={{ maxHeight: 'calc(80vh - 120px)' }}>
          {loading ? (
            [1, 2, 3].map((i) => (
              <div key={i} className="h-14 rounded-2xl bg-[#1A1A2E] animate-pulse" />
            ))
          ) : predictions.map((p, i) => (
            <div key={p.userId} className={`flex items-center gap-3 p-3 rounded-2xl border ${pointBg(p.pointsEarned)}`}>
              <span className="text-[#8A8A9A] text-xs w-4 text-center font-bold">{i + 1}</span>
              <div className="w-8 h-8 rounded-full bg-[#2A2A3E] flex items-center justify-center text-white text-xs font-bold shrink-0">
                {p.username[0].toUpperCase()}
              </div>
              <span className="flex-1 text-white text-sm font-medium truncate">{p.username}</span>
              {p.predictedHomeScore != null ? (
                <span className="text-white font-bold text-sm" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                  {p.predictedHomeScore} – {p.predictedAwayScore}
                </span>
              ) : (
                <span className="text-[#8A8A9A] text-xs italic">Sin pred</span>
              )}
              <span className={`text-sm font-bold w-12 text-right ${pointColor(p.pointsEarned)}`}>
                +{p.pointsEarned} pts
              </span>
            </div>
          ))}
        </div>
      </motion.div>
    </div>
  )
}

export default function TournamentPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  const { leaderboard, setLeaderboard, updateMatchLive } = useTournamentStore()
  const [tournament, setTournament] = useState(null)
  const [loading, setLoading] = useState(true)
  const [liveCount, setLiveCount] = useState(0)
  const [matches, setMatches] = useState([])
  const [activeTab, setActiveTab] = useState('tabla')
  const [phaseTab, setPhaseTab] = useState(null)
  const [predSheet, setPredSheet] = useState(null) // { match, predictions, loading }
  const phaseBarRef = useRef(null)

  function fetchMatches() {
    api.getMatches(id).then((m) => {
      setMatches(m)
      setLiveCount(m.filter((x) => x.status === 'InProgress').length)
    })
  }

  useEffect(() => {
    Promise.all([
      api.getTournament(id).then(setTournament),
      api.getLeaderboard(id).then(setLeaderboard),
    ]).finally(() => setLoading(false))

    fetchMatches()
    joinTournament(id)

    const off = onMatchUpdated((update) => {
      updateMatchLive(update)
      api.getLeaderboard(id).then(setLeaderboard)
      fetchMatches()
    })

    return () => { off(); leaveTournament(id) }
  }, [id])

  // Auto-select active phase
  useEffect(() => {
    if (matches.length === 0) return
    const live = matches.find((m) => m.status === 'InProgress')
    if (live) { setPhaseTab(getPhaseKey(live)); return }
    const next = matches.filter((m) => m.status === 'Scheduled').sort((a, b) => new Date(a.matchDate) - new Date(b.matchDate))[0]
    if (next) { setPhaseTab(getPhaseKey(next)); return }
    const allKeys = [...new Set(matches.map(getPhaseKey))].sort((a, b) => PHASE_ORDER.indexOf(a) - PHASE_ORDER.indexOf(b))
    if (allKeys.length) setPhaseTab(allKeys[allKeys.length - 1])
  }, [matches])

  useEffect(() => {
    const bar = phaseBarRef.current
    if (!bar) return
    const active = bar.querySelector('[data-active="true"]')
    active?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' })
  }, [phaseTab])

  async function openPredictions(match) {
    setPredSheet({ match, predictions: [], loading: true })
    try {
      const preds = await api.getMatchPredictions(id, match.id)
      setPredSheet((prev) => prev ? { ...prev, predictions: preds, loading: false } : null)
    } catch {
      setPredSheet(null)
    }
  }

  function copyInvite() {
    const link = `${window.location.origin}/join/${tournament?.inviteLink}`
    navigator.clipboard.writeText(link).catch(() => {})
  }

  if (loading) {
    return (
      <div className="flex flex-col gap-3 p-4 pt-16 bg-[#0D0D0D] min-h-full">
        {[1, 2, 3].map((i) => <div key={i} className="h-16 rounded-2xl bg-[#1A1A2E] animate-pulse" />)}
      </div>
    )
  }

  const phaseTabs = [...new Set(matches.map(getPhaseKey))].sort((a, b) => PHASE_ORDER.indexOf(a) - PHASE_ORDER.indexOf(b))
  const visibleMatches = matches
    .filter((m) => getPhaseKey(m) === phaseTab)
    .sort((a, b) => {
      if (a.status === 'InProgress' && b.status !== 'InProgress') return -1
      if (b.status === 'InProgress' && a.status !== 'InProgress') return 1
      return new Date(a.matchDate) - new Date(b.matchDate)
    })

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D]">
      {/* Header */}
      <div className="px-4 pt-12 pb-3 bg-[#1A1A2E]">
        <div className="flex items-center gap-3 mb-3">
          <button onClick={() => navigate('/')} className="text-[#8A8A9A] active:text-white">
            <ChevronLeft size={24} />
          </button>
          <div className="flex-1 min-w-0">
            <h1 className="text-xl font-bold text-white truncate">{tournament?.name}</h1>
            <p className="text-[#8A8A9A] text-xs">{leaderboard.length} participantes</p>
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
              {liveCount} partido{liveCount > 1 ? 's' : ''} en curso — puntos actualizándose
            </span>
          </div>
        )}
        <ApiStatusBanner hasLiveMatches={liveCount > 0} />

        {/* Tabs */}
        <div className="flex gap-1 mt-2">
          {['tabla', 'fixture'].map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`flex-1 py-2 rounded-xl text-xs font-bold uppercase tracking-wide transition-colors ${
                activeTab === tab
                  ? 'bg-[#00FF87] text-black'
                  : 'bg-[#0D0D0D] text-[#8A8A9A]'
              }`}
            >
              {tab === 'tabla' ? 'Tabla' : 'Fixture'}
            </button>
          ))}
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto">
        {activeTab === 'tabla' ? (
          <div className="py-4 flex flex-col gap-2">
            <ChampionPickBanner tournamentId={id} currentUserId={user?.id} />
            <p className="text-[#8A8A9A] text-xs uppercase tracking-widest font-semibold px-4 mb-1">
              Tabla de posiciones
            </p>
            <div className="px-4 flex flex-col gap-2">
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
            </div>
          </div>
        ) : (
          <div className="flex flex-col min-h-full">
            {/* Phase tabs */}
            <div
              ref={phaseBarRef}
              className="flex gap-2 px-4 py-3 overflow-x-auto border-b border-[#1A1A2E]"
              style={{ scrollbarWidth: 'none' }}
            >
              {phaseTabs.map((tab) => {
                const hasLive = matches.some((m) => getPhaseKey(m) === tab && m.status === 'InProgress')
                return (
                  <button
                    key={tab}
                    data-active={phaseTab === tab}
                    onClick={() => setPhaseTab(tab)}
                    className={`relative shrink-0 px-4 py-1.5 rounded-full text-xs font-semibold transition-all ${
                      phaseTab === tab
                        ? 'bg-[#00FF87] text-black'
                        : 'bg-[#1A1A2E] text-[#8A8A9A] border border-[#2A2A3E]'
                    }`}
                  >
                    {TAB_LABELS[tab] ?? tab}
                    {hasLive && <span className="absolute -top-0.5 -right-0.5 w-2 h-2 rounded-full bg-[#FF6B35]" />}
                  </button>
                )
              })}
            </div>

            <AnimatePresence mode="wait">
              <motion.div
                key={phaseTab}
                initial={{ opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -6 }}
                transition={{ duration: 0.15 }}
                className="px-4 py-4 flex flex-col gap-2"
              >
                {visibleMatches.map((m) => (
                  <TournamentMatchCard key={m.id} match={m} onTap={openPredictions} />
                ))}
              </motion.div>
            </AnimatePresence>
          </div>
        )}
      </div>

      {/* Predictions bottom sheet */}
      <AnimatePresence>
        {predSheet && (
          <MatchPredictionsSheet
            match={predSheet.match}
            predictions={predSheet.predictions}
            loading={predSheet.loading}
            onClose={() => setPredSheet(null)}
          />
        )}
      </AnimatePresence>
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
