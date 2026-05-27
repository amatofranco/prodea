import { useEffect, useState, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ChevronLeft, ChevronRight, Check, Zap } from 'lucide-react'
import { AnimatePresence, motion } from 'framer-motion'
import { api } from '../services/api'
import GoalPicker from '../components/GoalPicker'
import { getTeam, getFlagUrl } from '../data/teamsData'

const PREDICTION_CLOSE_BEFORE_MS = 15 * 60 * 1000

function canPredictMatch(m) {
  return (
    m.status === 'Scheduled' &&
    m.homeTeam !== 'TBD' &&
    m.awayTeam !== 'TBD' &&
    new Date(m.matchDate) - Date.now() >= PREDICTION_CLOSE_BEFORE_MS &&
    !m.userPrediction
  )
}

function FlagCard({ name }) {
  const { flag } = getTeam(name)
  const flagUrl = getFlagUrl(flag)
  return (
    <div className="flex flex-col items-center gap-1.5">
      <div className="relative w-20 h-24 rounded-xl overflow-hidden bg-[#2A2A3E]">
        {flagUrl && (
          <img src={flagUrl} alt={name} className="absolute inset-0 w-full h-full object-cover opacity-85" />
        )}
      </div>
      <p className="text-xs font-semibold text-white text-center leading-tight" style={{ maxWidth: 88, wordBreak: 'break-word' }}>{name}</p>
    </div>
  )
}

function Countdown({ matchDate }) {
  const [diff, setDiff] = useState(0)

  useEffect(() => {
    function tick() { setDiff(Math.max(0, new Date(matchDate) - Date.now())) }
    tick()
    const iv = setInterval(tick, 1000)
    return () => clearInterval(iv)
  }, [matchDate])

  if (diff <= PREDICTION_CLOSE_BEFORE_MS)
    return <span className="text-red-400 font-semibold text-sm">Predicciones cerradas</span>

  const remaining = diff - PREDICTION_CLOSE_BEFORE_MS
  const h = Math.floor(remaining / 3600000)
  const m = Math.floor((remaining % 3600000) / 60000)
  const s = Math.floor((remaining % 60000) / 1000)
  const isUrgent = remaining < 30 * 60 * 1000

  return (
    <span className={`font-mono font-bold text-sm ${isUrgent ? 'text-[#FF6B35]' : 'text-[#00FF87]'}`}>
      {h > 0 ? `${h}h ` : ''}{String(m).padStart(2, '0')}:{String(s).padStart(2, '0')}
    </span>
  )
}

export default function PredictionPage() {
  const { matchId } = useParams()
  const navigate = useNavigate()

  const [allMatches, setAllMatches] = useState([])
  const [match, setMatch] = useState(null)
  const [home, setHome] = useState(0)
  const [away, setAway] = useState(0)
  const [saved, setSaved] = useState(false)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [turboMode, setTurboMode] = useState(false)
  const [turboComplete, setTurboComplete] = useState(false)
  const slideDir = useRef(0)

  useEffect(() => {
    api.getMyPredictions()
      .then(setAllMatches)
      .catch(() => navigate(-1))
  }, [])

  useEffect(() => {
    if (allMatches.length === 0) return
    const m = allMatches.find((m) => m.id === Number(matchId))
    if (!m) { navigate(-1); return }
    setMatch(m)
    setError('')
    setTurboComplete(false)
    if (m.userPrediction) {
      setHome(m.userPrediction.predictedHomeScore)
      setAway(m.userPrediction.predictedAwayScore)
      setSaved(true)
    } else {
      setHome(0)
      setAway(0)
      setSaved(false)
    }
    setLoading(false)
  }, [matchId, allMatches])

  const currentIndex = allMatches.findIndex((m) => m.id === Number(matchId))
  const prevMatch = currentIndex > 0 ? allMatches[currentIndex - 1] : null
  const nextMatch = currentIndex < allMatches.length - 1 ? allMatches[currentIndex + 1] : null
  const pendingCount = allMatches.filter(canPredictMatch).length

  function navTo(id, dir) {
    slideDir.current = dir
    navigate(`/predicciones/${id}`)
  }

  const teamsConfirmed = match?.homeTeam !== 'TBD' && match?.awayTeam !== 'TBD'
  const [isPastDeadline, setIsPastDeadline] = useState(false)

  useEffect(() => {
    if (!match?.matchDate) return
    function check() {
      setIsPastDeadline(new Date(match.matchDate) - Date.now() < PREDICTION_CLOSE_BEFORE_MS)
    }
    check()
    const iv = setInterval(check, 10000)
    return () => clearInterval(iv)
  }, [match?.matchDate])

  const isLocked = match?.status !== 'Scheduled' || !teamsConfirmed || isPastDeadline

  async function handleSubmit() {
    if (isLocked || saving) return
    setSaving(true)
    setError('')
    try {
      await api.submitPrediction(matchId, { predictedHomeScore: home, predictedAwayScore: away })

      const updatedMatches = allMatches.map((m) =>
        m.id === Number(matchId)
          ? { ...m, userPrediction: { predictedHomeScore: home, predictedAwayScore: away, pointsEarned: 0 } }
          : m
      )
      setAllMatches(updatedMatches)
      setSaved(true)

      if (turboMode) {
        const next = updatedMatches
          .slice(currentIndex + 1)
          .find(canPredictMatch)

        setTimeout(() => {
          if (next) {
            slideDir.current = 1
            navigate(`/predicciones/${next.id}`)
          } else {
            setTurboComplete(true)
            setTimeout(() => navigate('/predicciones'), 1800)
          }
        }, 600)
      }
    } catch (err) {
      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div className="flex-1 bg-[#0D0D0D]" />

  const resultLabel =
    home > away ? `Gana ${match.homeTeam}` :
    away > home ? `Gana ${match.awayTeam}` :
    'Empate'

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D]">
      {/* Header */}
      <div className="px-4 pt-12 pb-4 bg-[#1A1A2E]">
        <div className="flex items-center gap-3 mb-4">
          <button onClick={() => navigate(-1)} className="text-[#8A8A9A]">
            <ChevronLeft size={24} />
          </button>
          <p className="text-[#8A8A9A] text-sm">
            {new Date(match.matchDate).toLocaleDateString(undefined, {
              weekday: 'long', day: 'numeric', month: 'long',
            })}
          </p>
        </div>

        <div className="flex items-center justify-center gap-4">
          <p className="flex-1 text-right text-lg font-bold text-white leading-tight">
            {match.homeTeam === 'TBD' && !match.homeTeamLabel
              ? <span className="text-[#8A8A9A] italic text-sm">Por confirmar</span>
              : (match.homeTeamLabel ?? match.homeTeam)}
          </p>
          <span className="text-[#8A8A9A] text-xl font-light">vs</span>
          <p className="flex-1 text-left text-lg font-bold text-white leading-tight">
            {match.awayTeam === 'TBD' && !match.awayTeamLabel
              ? <span className="text-[#8A8A9A] italic text-sm">Por confirmar</span>
              : (match.awayTeamLabel ?? match.awayTeam)}
          </p>
        </div>
      </div>

      {/* Barra de navegación */}
      {allMatches.length > 0 && (
        <div className="flex flex-col items-center gap-1.5 px-4 py-3 bg-[#0D0D0D] border-b border-[#1A1A2E]">
          <div className="flex items-center justify-center gap-3 w-full">
            <button
              onClick={() => prevMatch && navTo(prevMatch.id, -1)}
              disabled={!prevMatch}
              className="flex items-center gap-1.5 px-4 py-2 rounded-full bg-white text-black text-xs font-bold disabled:opacity-20 active:scale-95 transition-transform"
            >
              <ChevronLeft size={15} />
              Anterior
            </button>

            {/* Botón Modo Turbo */}
            <button
              onClick={() => setTurboMode((v) => !v)}
              className={`relative flex items-center gap-1.5 px-4 py-2 rounded-full text-xs font-bold transition-all active:scale-95 ${
                turboMode
                  ? 'bg-[#FF6B35] text-white shadow-[0_0_12px_rgba(255,107,53,0.6)]'
                  : 'bg-[#1A1A2E] text-[#8A8A9A] border border-[#2A2A3E]'
              }`}
            >
              {turboMode && (
                <motion.span
                  className="absolute inset-0 rounded-full border border-[#FF6B35]"
                  animate={{ scale: [1, 1.4], opacity: [0.6, 0] }}
                  transition={{ duration: 1, repeat: Infinity, ease: 'easeOut' }}
                />
              )}
              <Zap size={13} className={turboMode ? 'fill-white' : ''} />
              {turboMode ? 'Turbo ON' : 'Modo Turbo'}
            </button>

            <button
              onClick={() => nextMatch && navTo(nextMatch.id, 1)}
              disabled={!nextMatch}
              className="flex items-center gap-1.5 px-4 py-2 rounded-full bg-white text-black text-xs font-bold disabled:opacity-20 active:scale-95 transition-transform"
            >
              Siguiente
              <ChevronRight size={15} />
            </button>
          </div>

          <div className="flex items-center gap-2">
            <span className="text-[10px] text-[#3A3A4E] font-semibold tabular-nums">
              {currentIndex + 1} / {allMatches.length}
            </span>
            {turboMode && pendingCount > 0 && (
              <span className="text-[10px] text-[#FF6B35] font-semibold">
                · {pendingCount} por predecir
              </span>
            )}
          </div>
        </div>
      )}

      {/* Contenido animado */}
      <AnimatePresence mode="wait" custom={slideDir.current}>
        <motion.div
          key={matchId}
          custom={slideDir.current}
          variants={{
            enter: (dir) => ({ opacity: 0, x: dir * 60 }),
            center: { opacity: 1, x: 0 },
            exit: (dir) => ({ opacity: 0, x: dir * -60 }),
          }}
          initial="enter"
          animate="center"
          exit="exit"
          transition={{ duration: 0.22, ease: 'easeOut' }}
          className="flex-1 flex flex-col items-center justify-center gap-8 px-6"
        >
          {turboComplete ? (
            <motion.div
              initial={{ scale: 0.8, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              className="text-center"
            >
              <p className="text-5xl mb-4">🎉</p>
              <p className="text-white font-bold text-xl">¡Todo listo!</p>
              <p className="text-[#8A8A9A] text-sm mt-1">No quedan partidos por predecir</p>
            </motion.div>
          ) : isLocked ? (
            <div className="text-center">
              <p className="text-[#FF6B35] font-semibold text-lg">
                {!teamsConfirmed ? 'Equipos por confirmar' : 'Predicciones cerradas'}
              </p>
              <p className="text-[#8A8A9A] text-sm mt-1">
                {!teamsConfirmed
                  ? 'Podrás predecir cuando se definan los cruces'
                  : isPastDeadline && match?.status === 'Scheduled'
                  ? 'Las predicciones cerraron 15 minutos antes del partido'
                  : 'El partido ya empezó o terminó'}
              </p>
              {match.userPrediction && (
                <div className="mt-4 p-4 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E]">
                  <p className="text-[#8A8A9A] text-xs uppercase tracking-wider mb-1">Tu predicción</p>
                  <p className="text-2xl font-bold text-white" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                    {match.userPrediction.predictedHomeScore} – {match.userPrediction.predictedAwayScore}
                  </p>
                  {match.status === 'Finished' && (
                    <p className={`text-sm font-bold mt-1 ${match.userPrediction.pointsEarned > 0 ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
                      +{match.userPrediction.pointsEarned} puntos
                    </p>
                  )}
                </div>
              )}
            </div>
          ) : (
            <>
              <div className="flex flex-col items-center gap-1">
                <p className="text-[#8A8A9A] text-xs uppercase tracking-wider">Cierra en</p>
                <Countdown matchDate={match.matchDate} />
              </div>

              <div className="flex items-center gap-6 w-full">
                <div className="flex-1 flex flex-col items-center gap-3">
                  <FlagCard name={match.homeTeam} />
                  <GoalPicker value={home} onChange={setHome} disabled={isLocked} />
                </div>
                <span className="text-3xl text-[#2A2A3E] font-light mb-10">–</span>
                <div className="flex-1 flex flex-col items-center gap-3">
                  <FlagCard name={match.awayTeam} />
                  <GoalPicker value={away} onChange={setAway} disabled={isLocked} />
                </div>
              </div>

              <div className="w-full p-4 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] text-center">
                <p className="text-white font-bold text-lg">
                  {match.homeTeam} <span className="text-[#00FF87]">{home}</span>
                  {' – '}
                  <span className="text-[#00FF87]">{away}</span> {match.awayTeam}
                </p>
                <p className="text-[#8A8A9A] text-sm mt-1">{resultLabel}</p>
                <p className="text-[#00FF87] text-xs font-semibold mt-1">
                  Si acertás el marcador exacto → +3 pts
                </p>
              </div>

              {error && <p className="text-red-400 text-sm">{error}</p>}

              <button
                onClick={handleSubmit}
                disabled={saving || isLocked}
                className={`w-full py-4 rounded-2xl font-bold text-base transition-all active:scale-95 flex items-center justify-center gap-2 ${
                  saved
                    ? 'bg-[#1A1A2E] border border-[#00FF87] text-[#00FF87]'
                    : turboMode
                    ? 'bg-[#FF6B35] text-white'
                    : 'bg-[#00FF87] text-black'
                } disabled:opacity-50`}
              >
                {saved && !turboMode && <Check size={18} />}
                {saving
                  ? 'Guardando...'
                  : saved && turboMode
                  ? '✓ Yendo al siguiente...'
                  : saved
                  ? 'Predicción guardada'
                  : turboMode
                  ? <><Zap size={16} className="fill-white" /> Confirmar y seguir</>
                  : 'Confirmar predicción'}
              </button>
            </>
          )}
        </motion.div>
      </AnimatePresence>
    </div>
  )
}
