import { useEffect, useState, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ChevronLeft, ChevronRight, Check, Zap } from 'lucide-react'
import { AnimatePresence, motion } from 'framer-motion'
import { api } from '../services/api'
import GoalPicker from '../components/GoalPicker'
import { getTeam, getFlagUrl } from '../data/teamsData'

const PREDICTION_CLOSE_BEFORE_MS = 15 * 60 * 1000
const TURBO_COUNTDOWN_SECS = 5

function canPredictMatch(m) {
  return (
    m.status === 'Scheduled' &&
    m.homeTeam !== 'TBD' &&
    m.awayTeam !== 'TBD' &&
    new Date(m.matchDate) - Date.now() >= PREDICTION_CLOSE_BEFORE_MS &&
    !m.userPrediction
  )
}

// Para turbo: incluye partidos ya predichos, pero respeta el deadline
function canPredictInTurbo(m) {
  return (
    m.status === 'Scheduled' &&
    m.homeTeam !== 'TBD' &&
    m.awayTeam !== 'TBD' &&
    new Date(m.matchDate) - Date.now() >= PREDICTION_CLOSE_BEFORE_MS
  )
}

function FlagCard({ name }) {
  const { flag } = getTeam(name)
  const flagUrl = getFlagUrl(flag)
  return (
    <div className="flex flex-col items-center gap-1">
      <div className="relative w-16 h-20 rounded-xl overflow-hidden bg-[#2A2A3E]">
        {flagUrl && (
          <img src={flagUrl} alt={name} className="absolute inset-0 w-full h-full object-cover opacity-85" />
        )}
      </div>
      <p className="text-xs font-semibold text-white text-center leading-tight" style={{ maxWidth: 72, wordBreak: 'break-word' }}>{name}</p>
    </div>
  )
}

function MatchCountdown({ matchDate }) {
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

function TurboCountdown({ onComplete, onSkip, resetKey }) {
  const [progress, setProgress] = useState(1)
  const [display, setDisplay] = useState(TURBO_COUNTDOWN_SECS)
  const doneRef = useRef(false)
  const onCompleteRef = useRef(onComplete)
  onCompleteRef.current = onComplete

  useEffect(() => {
    doneRef.current = false
    setProgress(1)
    setDisplay(TURBO_COUNTDOWN_SECS)
    const start = Date.now()
    const totalMs = TURBO_COUNTDOWN_SECS * 1000
    let rafId

    const tick = () => {
      const elapsed = Date.now() - start
      const remaining = Math.max(0, totalMs - elapsed)
      setProgress(remaining / totalMs)
      setDisplay(Math.ceil(remaining / 1000))
      if (remaining <= 0) {
        if (!doneRef.current) { doneRef.current = true; onCompleteRef.current() }
      } else {
        rafId = requestAnimationFrame(tick)
      }
    }

    rafId = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(rafId)
  }, [resetKey])

  const r = 44
  const circumference = 2 * Math.PI * r

  return (
    <motion.div
      initial={{ scale: 0.7, opacity: 0 }}
      animate={{ scale: 1, opacity: 1 }}
      transition={{ type: 'spring', stiffness: 280, damping: 22 }}
      className="flex flex-col items-center gap-3"
    >
      <button
        onClick={onSkip}
        className="relative flex items-center justify-center w-28 h-28 active:scale-95 transition-transform"
        aria-label="Confirmar ya"
      >
        <svg width="112" height="112" className="-rotate-90">
          <circle cx="56" cy="56" r={r} fill="none" stroke="#2A2A3E" strokeWidth="5" />
          <circle
            cx="56" cy="56" r={r}
            fill="none" stroke="#FF6B35" strokeWidth="5"
            strokeDasharray={circumference}
            strokeDashoffset={circumference * (1 - progress)}
            strokeLinecap="round"
          />
        </svg>
        <div className="absolute flex flex-col items-center leading-none select-none">
          <span className="text-4xl font-black text-[#FF6B35] tabular-nums">{display}</span>
          <span className="text-[10px] text-[#8A8A9A] mt-0.5">seg</span>
        </div>
      </button>
      <button onClick={onSkip} className="text-xs font-semibold text-[#FF6B35] underline underline-offset-2 active:opacity-60">
        Confirmar ya →
      </button>
    </motion.div>
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
  const [justSaved, setJustSaved] = useState(false)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [turboMode, setTurboMode] = useState(false)
  const [turboComplete, setTurboComplete] = useState(false)
  const [turboFlash, setTurboFlash] = useState(false)
  const prevTurboRef = useRef(false)
  const slideDir = useRef(0)
  const submitGuard = useRef(false)

  // Refs siempre actualizados — se leen en setTimeouts para evitar closures stale
  const allMatchesRef = useRef([])
  const matchIdRef = useRef(matchId)
  const turboModeRef = useRef(false)
  allMatchesRef.current = allMatches
  matchIdRef.current = matchId
  turboModeRef.current = turboMode

  // Carga inicial
  useEffect(() => {
    api.getMyPredictions().then(setAllMatches).catch(() => navigate(-1))
  }, [])

  // Reset por-partido cuando cambia matchId
  useEffect(() => {
    submitGuard.current = false
    setJustSaved(false)
  }, [matchId])

  // Sincroniza datos del partido actual
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

  // Flash al activar turbo
  useEffect(() => {
    if (turboMode && !prevTurboRef.current) setTurboFlash(true)
    prevTurboRef.current = turboMode
  }, [turboMode])

  function turboAdvance() {
    const matches = allMatchesRef.current
    const id = Number(matchIdRef.current)
    const idx = matches.findIndex((m) => m.id === id)
    const next = matches.slice(idx + 1).find(canPredictInTurbo)
    slideDir.current = 1
    if (next) {
      navigate(`/predicciones/${next.id}`)
    } else {
      setTurboComplete(true)
      setTimeout(() => navigate('/predicciones'), 1800)
    }
  }

  const currentIndex = allMatches.findIndex((m) => m.id === Number(matchId))
  const prevMatch = currentIndex > 0 ? allMatches[currentIndex - 1] : null
  const nextMatch = currentIndex < allMatches.length - 1 ? allMatches[currentIndex + 1] : null
  const pendingCount = allMatches.filter(canPredictMatch).length

  function navTo(id, dir) { slideDir.current = dir; navigate(`/predicciones/${id}`) }

  const teamsConfirmed = match?.homeTeam !== 'TBD' && match?.awayTeam !== 'TBD'
  const [isPastDeadline, setIsPastDeadline] = useState(false)
  useEffect(() => {
    if (!match?.matchDate) return
    function check() { setIsPastDeadline(new Date(match.matchDate) - Date.now() < PREDICTION_CLOSE_BEFORE_MS) }
    check()
    const iv = setInterval(check, 10000)
    return () => clearInterval(iv)
  }, [match?.matchDate])

  const isLocked = match?.status !== 'Scheduled' || !teamsConfirmed || isPastDeadline
  const showTurboCountdown = turboMode && !isLocked && !saving && !justSaved

  async function handleSubmit() {
    if (submitGuard.current || isLocked || saving) return
    submitGuard.current = true
    setSaving(true)
    setError('')
    try {
      await api.submitPrediction(matchId, { predictedHomeScore: home, predictedAwayScore: away })
      setAllMatches((prev) =>
        prev.map((m) =>
          m.id === Number(matchId)
            ? { ...m, userPrediction: { predictedHomeScore: home, predictedAwayScore: away, pointsEarned: 0 } }
            : m
        )
      )
      setSaved(true)
      setJustSaved(true)

      if (turboModeRef.current) {
        // Usa refs: siempre tienen los valores actuales, sin closures stale
        setTimeout(() => turboAdvance(), 700)
      }
    } catch (err) {
      setError(err.message)
      submitGuard.current = false
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div className="flex-1 bg-[#0D0D0D]" />

  const resultLabel =
    home > away ? `Gana ${match.homeTeam}` :
    away > home ? `Gana ${match.awayTeam}` : 'Empate'

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D]">
      {/* Header */}
      <div className="px-4 pt-12 pb-3 bg-[#1A1A2E]">
        <div className="flex items-center gap-3">
          <button onClick={() => navigate(-1)} className="text-[#8A8A9A] flex-shrink-0">
            <ChevronLeft size={24} />
          </button>
          <div className="flex items-center gap-2 flex-1 min-w-0">
            <p className="flex-1 text-right text-base font-bold text-white leading-tight">
              {match.homeTeam === 'TBD' && !match.homeTeamLabel
                ? <span className="text-[#8A8A9A] italic text-sm">Por confirmar</span>
                : (match.homeTeamLabel ?? match.homeTeam)}
            </p>
            <span className="text-[#8A8A9A] text-sm font-light flex-shrink-0">vs</span>
            <p className="flex-1 text-left text-base font-bold text-white leading-tight">
              {match.awayTeam === 'TBD' && !match.awayTeamLabel
                ? <span className="text-[#8A8A9A] italic text-sm">Por confirmar</span>
                : (match.awayTeamLabel ?? match.awayTeam)}
            </p>
          </div>
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
              <ChevronLeft size={15} /> Anterior
            </button>

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
              Siguiente <ChevronRight size={15} />
            </button>
          </div>
          <div className="flex items-center gap-1.5">
            <span className="text-[10px] text-[#3A3A4E] font-semibold tabular-nums">
              {currentIndex + 1} / {allMatches.length}
            </span>
            <span className="text-[10px] text-[#3A3A4E]">·</span>
            <span className="text-[10px] text-[#3A3A4E]">
              {new Date(match.matchDate).toLocaleDateString(undefined, { day: 'numeric', month: 'short' })}
              {' '}
              {new Date(match.matchDate).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
            </span>
            {turboMode && pendingCount > 0 && (
              <span className="text-[10px] text-[#FF6B35] font-semibold">· {pendingCount} por predecir</span>
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
            enter: (dir) => ({ opacity: 0, x: dir === 0 ? 0 : dir * 60 }),
            center: { opacity: 1, x: 0 },
            exit:  (dir) => ({ opacity: 0, x: dir === 0 ? 0 : dir * -60 }),
          }}
          initial="enter" animate="center" exit="exit"
          transition={{ duration: 0.22, ease: 'easeOut' }}
          className="flex-1 flex flex-col items-center justify-center gap-4 px-6"
        >
          {turboComplete ? (
            <motion.div initial={{ scale: 0.8, opacity: 0 }} animate={{ scale: 1, opacity: 1 }} className="text-center">
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
              <div className="flex items-center gap-1.5">
                <p className="text-[#8A8A9A] text-xs uppercase tracking-wider">Cierra en</p>
                <MatchCountdown matchDate={match.matchDate} />
              </div>

              <div className="flex items-center gap-4 w-full">
                <div className="flex-1 flex flex-col items-center gap-2">
                  <FlagCard name={match.homeTeam} />
                  <GoalPicker value={home} onChange={setHome} disabled={isLocked} />
                </div>
                <span className="text-2xl text-[#2A2A3E] font-light mb-8">–</span>
                <div className="flex-1 flex flex-col items-center gap-2">
                  <FlagCard name={match.awayTeam} />
                  <GoalPicker value={away} onChange={setAway} disabled={isLocked} />
                </div>
              </div>

              <div className="w-full p-3 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] text-center">
                <p className="text-white font-bold text-base">
                  {match.homeTeam} <span className="text-[#00FF87]">{home}</span>
                  {' – '}
                  <span className="text-[#00FF87]">{away}</span> {match.awayTeam}
                </p>
                <p className="text-[#8A8A9A] text-xs mt-0.5">{resultLabel} · Si acertás exacto → <span className="text-[#00FF87] font-semibold">+3 pts</span></p>
              </div>

              {error && <p className="text-red-400 text-sm">{error}</p>}

              {showTurboCountdown ? (
                <TurboCountdown key={matchId} onComplete={handleSubmit} onSkip={handleSubmit} resetKey={matchId} />
              ) : justSaved && turboMode ? (
                <motion.div
                  initial={{ opacity: 0, y: 4 }} animate={{ opacity: 1, y: 0 }}
                  className="flex items-center gap-2 text-[#00FF87] font-semibold text-sm"
                >
                  <Check size={16} /> Guardado · cargando siguiente...
                </motion.div>
              ) : (
                <button
                  onClick={handleSubmit}
                  disabled={saving || isLocked}
                  className={`w-full py-3 rounded-2xl font-bold text-base transition-all active:scale-95 flex items-center justify-center gap-2 ${
                    saved ? 'bg-[#1A1A2E] border border-[#00FF87] text-[#00FF87]' : 'bg-[#00FF87] text-black'
                  } disabled:opacity-50`}
                >
                  {saved && <Check size={18} />}
                  {saving ? 'Guardando...' : justSaved ? 'Predicción guardada' : saved ? 'Actualizar predicción' : 'Confirmar predicción'}
                </button>
              )}
            </>
          )}
        </motion.div>
      </AnimatePresence>

      {/* Efecto de activación turbo */}
      <AnimatePresence>
        {turboFlash && (
          <>
            <motion.div
              className="fixed inset-0 pointer-events-none z-50 bg-[#FF6B35]"
              initial={{ opacity: 0.3 }} animate={{ opacity: 0 }} exit={{}}
              transition={{ duration: 0.5 }}
              onAnimationComplete={() => setTurboFlash(false)}
            />
            {[
              { left: 12, delay: 0,    rot: -15 },
              { left: 35, delay: 0.1,  rot: -5  },
              { left: 55, delay: 0.05, rot: 8   },
              { left: 72, delay: 0.15, rot: -10 },
              { left: 88, delay: 0.08, rot: 12  },
            ].map((p, i) => (
              <motion.div
                key={i}
                className="fixed pointer-events-none z-50 text-4xl select-none"
                style={{ left: `${p.left}%`, bottom: '15%' }}
                initial={{ y: 0, opacity: 1, rotate: p.rot, scale: 0.8 }}
                animate={{ y: -320, opacity: 0, scale: 2 }}
                transition={{ duration: 0.7, delay: p.delay, ease: 'easeOut' }}
              >
                ⚡
              </motion.div>
            ))}
          </>
        )}
      </AnimatePresence>
    </div>
  )
}
