import { useEffect, useState, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Users, Zap } from 'lucide-react'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'
import { useTournamentStore } from '../store/tournamentStore'
import { getTeam, getFlagUrl } from '../data/teamsData'

const LIVE_POLL_MS = 60_000

function FlagOnly({ name, label }) {
  const { flag } = getTeam(name)
  const flagUrl = getFlagUrl(flag)
  return (
    <div className="w-9 h-10 rounded-md overflow-hidden bg-[#2A2A3E] flex-shrink-0">
      {flagUrl && <img src={flagUrl} alt={label ?? name} className="w-full h-full object-cover opacity-85" />}
    </div>
  )
}

function TeamMini({ name, label }) {
  const { flag } = getTeam(name)
  const flagUrl = getFlagUrl(flag)
  const display = label ?? name
  return (
    <div className="flex flex-col items-center gap-1 min-w-0">
      <div className="w-9 h-10 rounded-md overflow-hidden bg-[#2A2A3E] flex-shrink-0">
        {flagUrl && <img src={flagUrl} alt={display} className="w-full h-full object-cover opacity-85" />}
      </div>
      <p className="text-[10px] font-semibold text-white text-center leading-tight" style={{ maxWidth: 56, wordBreak: 'break-word' }}>
        {display}
      </p>
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

function LiveCard({ match, compact = false }) {
  const pred = match.userPrediction
  const homeDisplay = match.homeTeamLabel ?? match.homeTeam
  const awayDisplay = match.awayTeamLabel ?? match.awayTeam

  if (compact) {
    return (
      <div className="p-3 rounded-2xl bg-[#FF6B35]/10 border border-[#FF6B35]/40 flex flex-col gap-2">
        <div className="flex items-center gap-1.5">
          <span className="w-1.5 h-1.5 rounded-full bg-[#FF6B35] animate-pulse flex-shrink-0" />
          <span className="text-xs font-bold text-[#FF6B35]">
            {match.minute != null ? `${match.minute}'` : 'En vivo'}
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
          const hasPts = pts != null
          return (
            <div className="pt-2 border-t border-[#FF6B35]/20 flex items-center justify-between">
              <div className="flex items-baseline gap-1.5">
                <span className="text-[8px] uppercase tracking-wider text-[#8A8A9A] font-semibold">Tu pred</span>
                <span className="text-xs font-bold text-white">{pred.predictedHomeScore}–{pred.predictedAwayScore}</span>
              </div>
              {hasPts && (
                <span className={`px-2 py-0.5 rounded-full text-xs font-black ${pts > 0 ? 'bg-[#00FF87]/15 text-[#00FF87]' : 'bg-[#2A2A3E] text-[#8A8A9A]'}`}>
                  +{pts} pts
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
        <span className="text-sm font-bold text-[#FF6B35] uppercase tracking-wider">En vivo</span>
        {match.minute != null && (
          <span className="text-sm font-bold text-[#FF6B35]">· {match.minute}'</span>
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

      {pred && (() => {
        const pts = calcLivePoints(pred, match.homeScore, match.awayScore)
        return (
          <div className="mt-3 pt-3 border-t border-[#FF6B35]/20 flex items-center justify-between">
            <div className="flex items-baseline gap-2">
              <span className="text-[9px] uppercase tracking-wider text-[#8A8A9A] font-semibold">Tu pred</span>
              <span className="text-sm font-bold text-white">{pred.predictedHomeScore} – {pred.predictedAwayScore}</span>
            </div>
            {pts != null && (
              <span className={`px-3 py-1 rounded-full text-sm font-black ${pts > 0 ? 'bg-[#00FF87]/15 text-[#00FF87]' : 'bg-[#2A2A3E] text-[#8A8A9A]'}`}>
                +{pts} pts
              </span>
            )}
          </div>
        )
      })()}
    </div>
  )
}

function FinishedCard({ match, compact = false }) {
  const pred = match.userPrediction
  const home = match.homeScore ?? 0
  const away = match.awayScore ?? 0
  const homeDisplay = match.homeTeamLabel ?? match.homeTeam
  const awayDisplay = match.awayTeamLabel ?? match.awayTeam

  if (compact) {
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
            <span className="text-[8px] uppercase tracking-wider text-[#3A3A4E] font-semibold">Final</span>
          </div>
          <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
            <FlagOnly name={match.awayTeam} label={match.awayTeamLabel} />
            <p className="text-[9px] font-semibold text-white text-center leading-tight w-full truncate">{awayDisplay}</p>
          </div>
        </div>

        {pred && (
          <div className="pt-2 border-t border-[#2A2A3E] flex items-baseline justify-between">
            <div className="flex items-baseline gap-1.5">
              <span className="text-[8px] uppercase tracking-wider text-[#8A8A9A] font-semibold">Tu pred</span>
              <span className="text-xs font-bold text-[#8A8A9A]">{pred.predictedHomeScore}–{pred.predictedAwayScore}</span>
            </div>
            <span className={`text-xs font-black ${pred.pointsEarned > 0 ? 'text-[#00FF87]' : 'text-[#3A3A4E]'}`}>
              +{pred.pointsEarned} pts
            </span>
          </div>
        )}
      </div>
    )
  }

  return (
    <div className="p-4 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E]">
      <div className="flex items-center gap-2">
        <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
          <FlagOnly name={match.homeTeam} label={match.homeTeamLabel} />
          <p className="text-[10px] font-semibold text-white text-center leading-tight" style={{ maxWidth: 72, wordBreak: 'break-word' }}>{homeDisplay}</p>
        </div>

        <div className="flex flex-col items-center gap-0.5 px-2">
          <span className="text-4xl font-black text-white tabular-nums" style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}>
            {home} – {away}
          </span>
          <span className="text-[9px] uppercase tracking-wider text-[#3A3A4E] font-semibold">Final</span>
        </div>

        <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
          <FlagOnly name={match.awayTeam} label={match.awayTeamLabel} />
          <p className="text-[10px] font-semibold text-white text-center leading-tight" style={{ maxWidth: 72, wordBreak: 'break-word' }}>{awayDisplay}</p>
        </div>
      </div>

      {pred && (
        <div className="mt-3 pt-3 border-t border-[#2A2A3E] flex items-center justify-between">
          <span className="text-[9px] uppercase tracking-wider text-[#8A8A9A] font-semibold">Tu predicción</span>
          <div className="flex items-center gap-2">
            <span className="text-sm text-[#8A8A9A]">{pred.predictedHomeScore} – {pred.predictedAwayScore}</span>
            <span className={`text-sm font-bold ${pred.pointsEarned > 0 ? 'text-[#00FF87]' : 'text-[#3A3A4E]'}`}>
              +{pred.pointsEarned} pts
            </span>
          </div>
        </div>
      )}
    </div>
  )
}

function UpcomingCard({ match, navigate }) {
  const hasPred = !!match.userPrediction
  const matchDate = new Date(match.matchDate)
  const now = new Date()
  const isToday = matchDate.toDateString() === now.toDateString()
  const isTomorrow = matchDate.toDateString() === new Date(now.getTime() + 86400000).toDateString()
  const dayLabel = isToday ? 'Hoy' : isTomorrow ? 'Mañana' : matchDate.toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' })
  const timeLabel = matchDate.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
  const homeDisplay = match.homeTeamLabel ?? match.homeTeam
  const awayDisplay = match.awayTeamLabel ?? match.awayTeam

  return (
    <button
      onClick={() => navigate(`/predicciones/${match.id}`)}
      className="w-full text-left p-3 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] active:border-[#00FF87] transition-colors"
    >
      <div className="flex items-center gap-3">
        <FlagOnly name={match.homeTeam} label={match.homeTeamLabel} />
        <div className="flex flex-col flex-1 min-w-0 gap-0.5">
          <span className="text-[10px] text-[#8A8A9A]">{dayLabel} · {timeLabel}</span>
          <span className="text-xs font-semibold text-white leading-tight">
            {homeDisplay} <span className="text-[#3A3A4E]">vs</span> {awayDisplay}
          </span>
        </div>
        <FlagOnly name={match.awayTeam} label={match.awayTeamLabel} />
      </div>

      <div className="mt-2.5 pt-2.5 border-t border-[#2A2A3E] flex items-center justify-between">
        <span className="text-[9px] uppercase tracking-wider text-[#8A8A9A] font-semibold">Tu predicción</span>
        {hasPred ? (
          <span className="text-sm font-bold text-[#00FF87]">
            {match.userPrediction.predictedHomeScore} – {match.userPrediction.predictedAwayScore}
          </span>
        ) : (
          <span className="text-xs font-bold text-[#FF6B35]">Predecir →</span>
        )}
      </div>
    </button>
  )
}

export default function HomePage() {
  const user = useAuthStore((s) => s.user)
  const { tournaments, setTournaments } = useTournamentStore()
  const [loading, setLoading] = useState(true)
  const [matches, setMatches] = useState([])
  const [showJoin, setShowJoin] = useState(false)
  const [showCreate, setShowCreate] = useState(false)
  const [joinCode, setJoinCode] = useState('')
  const [createName, setCreateName] = useState('')
  const [error, setError] = useState('')
  const navigate = useNavigate()
  const pollRef = useRef(null)

  useEffect(() => {
    api.getTournaments().then(setTournaments).finally(() => setLoading(false))
    api.getMyPredictions().then(setMatches)
  }, [])

  useEffect(() => {
    function scheduleNext() {
      pollRef.current = setTimeout(async () => {
        const updated = await api.getMyPredictions().catch(() => null)
        if (updated) setMatches(updated)
        scheduleNext()
      }, LIVE_POLL_MS)
    }
    scheduleNext()
    return () => clearTimeout(pollRef.current)
  }, [])

  async function handleCreate(e) {
    e.preventDefault()
    setError('')
    try {
      const t = await api.createTournament({ name: createName })
      setTournaments([...tournaments, t])
      setShowCreate(false)
      setCreateName('')
      navigate(`/torneos/${t.id}`)
    } catch (err) { setError(err.message) }
  }

  async function handleJoin(e) {
    e.preventDefault()
    setError('')
    try {
      const t = await api.joinTournament({ codeOrInviteLink: joinCode.trim() })
      setTournaments([...tournaments, t])
      setShowJoin(false)
      setJoinCode('')
      navigate(`/torneos/${t.id}`)
    } catch (err) { setError(err.message) }
  }

  const today = new Date().toDateString()
  const liveMatches = matches.filter((m) => m.status === 'InProgress')
  const recentFinished = matches
    .filter((m) => m.status === 'Finished')
    .sort((a, b) => new Date(b.matchDate) - new Date(a.matchDate))
    .slice(0, 3)
  const tomorrow = new Date(new Date().getTime() + 86400000).toDateString()
  const allUpcoming = matches
    .filter((m) => m.status === 'Scheduled' && m.homeTeam !== 'TBD' && m.awayTeam !== 'TBD')
    .sort((a, b) => new Date(a.matchDate) - new Date(b.matchDate))
  const todayTomorrowMatches = allUpcoming.filter((m) => {
    const d = new Date(m.matchDate).toDateString()
    return d === today || d === tomorrow
  })
  const upcomingMatches = todayTomorrowMatches.length > 0 ? todayTomorrowMatches : allUpcoming.slice(0, 3)

  const avatar = user?.username?.[0]?.toUpperCase() || '?'

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D] pb-4">
      {/* Header */}
      <div className="px-5 pt-12 pb-5 bg-gradient-to-b from-[#1A1A2E] to-[#0D0D0D]">
        <div className="flex items-center justify-between mb-5">
          <div>
            <p className="text-[#8A8A9A] text-sm">Hola,</p>
            <h2 className="text-2xl font-bold text-white">{user?.username}</h2>
          </div>
          <div className="w-11 h-11 rounded-full bg-[#00FF87] flex items-center justify-center text-black font-bold text-lg">
            {avatar}
          </div>
        </div>

        {/* Action buttons — arriba, dentro del header */}
        <div className="flex gap-3">
          <button
            onClick={() => { setShowCreate(true); setError('') }}
            className="flex-1 flex items-center justify-center gap-2 py-3 rounded-xl bg-[#00FF87] text-black font-bold text-sm active:scale-95 transition-transform"
          >
            <Plus size={18} /> Crear torneo
          </button>
          <button
            onClick={() => { setShowJoin(true); setError('') }}
            className="flex-1 flex items-center justify-center gap-2 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white font-semibold text-sm active:scale-95 transition-transform"
          >
            <Users size={18} /> Unirse
          </button>
        </div>
      </div>

      {/* Partidos en curso */}
      {liveMatches.length > 0 && (
        <div className="px-5 mt-5 mb-5">
          <h3 className="text-[#FF6B35] text-xs uppercase tracking-widest mb-2 font-semibold flex items-center gap-1.5">
            <span className="w-1.5 h-1.5 rounded-full bg-[#FF6B35] animate-pulse" />
            En curso
          </h3>
          {liveMatches.length === 1
            ? liveMatches.map((m) => <LiveCard key={m.id} match={m} />)
            : (
              <div className="grid grid-cols-2 gap-2">
                {liveMatches.map((m) => <LiveCard key={m.id} match={m} compact />)}
              </div>
            )
          }
        </div>
      )}

      {/* Últimos 3 finalizados */}
      {recentFinished.length > 0 && (
        <div className="px-5 mt-5 mb-5">
          <h3 className="text-[#8A8A9A] text-xs uppercase tracking-widest mb-2 font-semibold">Últimos resultados</h3>
          {recentFinished.length === 1
            ? <FinishedCard match={recentFinished[0]} />
            : (
              <div className={`grid gap-2 ${recentFinished.length === 2 ? 'grid-cols-2' : 'grid-cols-3'}`}>
                {recentFinished.map((m) => <FinishedCard key={m.id} match={m} compact />)}
              </div>
            )
          }
        </div>
      )}

      {/* Próximos partidos — siempre visible */}
      {upcomingMatches.length > 0 && (
        <div className="px-5 mt-5 mb-5">
          <h3 className="text-[#8A8A9A] text-xs uppercase tracking-widest mb-2 font-semibold">Próximos partidos</h3>
          <div className="flex flex-col gap-2">
            {upcomingMatches.map((m) => <UpcomingCard key={m.id} match={m} navigate={navigate} />)}
          </div>
        </div>
      )}

      {/* Tournaments list */}
      <div className="px-5 flex-1">
        <h3 className="text-[#8A8A9A] text-xs uppercase tracking-widest mb-3 font-semibold">Mis torneos</h3>

        {loading ? (
          <div className="flex flex-col gap-3">
            {[1, 2].map((i) => (
              <div key={i} className="h-24 rounded-2xl bg-[#1A1A2E] animate-pulse" />
            ))}
          </div>
        ) : tournaments.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
            <Zap size={40} className="text-[#2A2A3E]" />
            <p className="text-[#8A8A9A] text-sm">No estás en ningún torneo todavía.<br />Creá uno o unite con un código.</p>
          </div>
        ) : (
          <div className="flex flex-col gap-3">
            {tournaments.map((t) => (
              <button
                key={t.id}
                onClick={() => navigate(`/torneos/${t.id}`)}
                className="w-full text-left p-4 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] active:border-[#00FF87] transition-colors"
              >
                <div className="flex items-start justify-between">
                  <div>
                    <p className="font-bold text-white text-base">{t.name}</p>
                    <p className="text-[#8A8A9A] text-xs mt-0.5">{t.participantCount} participantes</p>
                  </div>
                  <span className="text-xs bg-[#0D0D0D] px-2 py-1 rounded-lg font-mono text-[#00FF87] tracking-widest">
                    {t.code}
                  </span>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Modal: Crear torneo */}
      {showCreate && (
        <Modal title="Crear torneo" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate} className="flex flex-col gap-4">
            <input
              type="text"
              placeholder="Nombre del torneo"
              value={createName}
              onChange={(e) => setCreateName(e.target.value)}
              required minLength={3} maxLength={100}
              autoFocus
              className="w-full px-4 py-3 rounded-xl bg-[#0D0D0D] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87]"
            />
            {error && <p className="text-red-400 text-sm">{error}</p>}
            <button type="submit" className="w-full py-3 rounded-xl bg-[#00FF87] text-black font-bold">Crear</button>
          </form>
        </Modal>
      )}

      {/* Modal: Unirse */}
      {showJoin && (
        <Modal title="Unirse a un torneo" onClose={() => setShowJoin(false)}>
          <form onSubmit={handleJoin} className="flex flex-col gap-4">
            <input
              type="text"
              placeholder="Código de 6 letras o link de invitación"
              value={joinCode}
              onChange={(e) => setJoinCode(e.target.value)}
              required
              autoFocus
              className="w-full px-4 py-3 rounded-xl bg-[#0D0D0D] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] font-mono tracking-widest uppercase"
            />
            {error && <p className="text-red-400 text-sm">{error}</p>}
            <button type="submit" className="w-full py-3 rounded-xl bg-[#00FF87] text-black font-bold">Unirse</button>
          </form>
        </Modal>
      )}
    </div>
  )
}

function Modal({ title, onClose, children }) {
  return (
    <div className="fixed inset-0 z-50 flex items-end" onClick={onClose}>
      <div
        className="w-full max-w-[480px] mx-auto bg-[#1A1A2E] rounded-t-3xl p-6 pb-10 flex flex-col gap-4"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-bold text-white">{title}</h3>
          <button onClick={onClose} className="text-[#8A8A9A] text-2xl leading-none">×</button>
        </div>
        {children}
      </div>
    </div>
  )
}
