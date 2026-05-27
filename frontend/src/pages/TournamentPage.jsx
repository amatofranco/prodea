import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import { Share2, ChevronLeft, Wifi } from 'lucide-react'
import { api } from '../services/api'
import { useTournamentStore } from '../store/tournamentStore'
import { useAuthStore } from '../store/authStore'
import { joinTournament, leaveTournament, onMatchUpdated } from '../services/signalr'
import { BadgePill } from '../components/BadgePill'
import ApiStatusBanner from '../components/ApiStatusBanner'

export default function TournamentPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  const { leaderboard, setLeaderboard, updateMatchLive } = useTournamentStore()
  const [tournament, setTournament] = useState(null)
  const [loading, setLoading] = useState(true)
  const [liveCount, setLiveCount] = useState(0)

  useEffect(() => {
    Promise.all([
      api.getTournament(id).then(setTournament),
      api.getLeaderboard(id).then(setLeaderboard),
    ]).finally(() => setLoading(false))

    joinTournament(id)
    const off = onMatchUpdated((update) => {
      updateMatchLive(update)
      api.getLeaderboard(id).then(setLeaderboard)
      api.getMatches(id).then((matches) => {
        setLiveCount(matches.filter((m) => m.status === 'InProgress').length)
      })
    })

    api.getMatches(id).then((matches) => {
      setLiveCount(matches.filter((m) => m.status === 'InProgress').length)
    })

    return () => { off(); leaveTournament(id) }
  }, [id])

  function copyInvite() {
    const link = `${window.location.origin}/join/${tournament?.inviteLink}`
    navigator.clipboard.writeText(link).catch(() => {})
  }

  if (loading) {
    return (
      <div className="flex flex-col gap-3 p-4 pt-16 bg-[#0D0D0D] min-h-full">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-16 rounded-2xl bg-[#1A1A2E] animate-pulse" />
        ))}
      </div>
    )
  }

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
      </div>

      {/* Leaderboard */}
      <div className="flex-1 overflow-y-auto px-4 py-4 flex flex-col gap-2">
        <p className="text-[#8A8A9A] text-xs uppercase tracking-widest font-semibold mb-1">Tabla de posiciones</p>
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
