import { useEffect, useState, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ChevronLeft, Check } from 'lucide-react'
import { api } from '../services/api'
import GoalPicker from '../components/GoalPicker'
import { getTeam, getFlagUrl } from '../data/teamsData'

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
    function tick() {
      setDiff(Math.max(0, new Date(matchDate) - Date.now()))
    }
    tick()
    const iv = setInterval(tick, 1000)
    return () => clearInterval(iv)
  }, [matchDate])

  if (diff <= 0) return <span className="text-red-400 font-semibold text-sm">Predicciones cerradas</span>

  const h = Math.floor(diff / 3600000)
  const m = Math.floor((diff % 3600000) / 60000)
  const s = Math.floor((diff % 60000) / 1000)

  return (
    <span className="font-mono text-[#00FF87] font-bold text-sm">
      {h > 0 ? `${h}h ` : ''}{String(m).padStart(2, '0')}:{String(s).padStart(2, '0')}
    </span>
  )
}

export default function PredictionPage() {
  const { matchId } = useParams()
  const navigate = useNavigate()
  const [match, setMatch] = useState(null)
  const [home, setHome] = useState(0)
  const [away, setAway] = useState(0)
  const [saved, setSaved] = useState(false)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    api.getMyPredictions().then((matches) => {
      const m = matches.find((m) => m.id === Number(matchId))
      if (!m) { navigate(-1); return }
      setMatch(m)
      if (m.userPrediction) {
        setHome(m.userPrediction.predictedHomeScore)
        setAway(m.userPrediction.predictedAwayScore)
        setSaved(true)
      }
      setLoading(false)
    })
  }, [matchId])

  const isLocked = match?.status !== 'Scheduled'

  async function handleSubmit() {
    if (isLocked || saving) return
    setSaving(true)
    setError('')
    try {
      await api.submitPrediction(matchId, {
        predictedHomeScore: home,
        predictedAwayScore: away,
      })
      setSaved(true)
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

        {/* Teams header */}
        <div className="flex items-center justify-center gap-4">
          <p className="flex-1 text-right text-lg font-bold text-white leading-tight">
            {match.homeTeam === 'TBD' && !match.homeTeamLabel ? <span className="text-[#8A8A9A] italic text-sm">Por confirmar</span> : (match.homeTeamLabel ?? match.homeTeam)}
          </p>
          <span className="text-[#8A8A9A] text-xl font-light">vs</span>
          <p className="flex-1 text-left text-lg font-bold text-white leading-tight">
            {match.awayTeam === 'TBD' && !match.awayTeamLabel ? <span className="text-[#8A8A9A] italic text-sm">Por confirmar</span> : (match.awayTeamLabel ?? match.awayTeam)}
          </p>
        </div>
      </div>

      {/* Picker area */}
      <div className="flex-1 flex flex-col items-center justify-center gap-8 px-6">
        {isLocked ? (
          <div className="text-center">
            <p className="text-[#FF6B35] font-semibold text-lg">Predicciones cerradas</p>
            <p className="text-[#8A8A9A] text-sm mt-1">El partido ya empezó o terminó</p>
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
            {/* Countdown */}
            <div className="flex flex-col items-center gap-1">
              <p className="text-[#8A8A9A] text-xs uppercase tracking-wider">Cierra en</p>
              <Countdown matchDate={match.matchDate} />
            </div>

            {/* Goal pickers con banderas */}
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

            {/* Dynamic summary */}
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

            {/* Submit button */}
            <button
              onClick={handleSubmit}
              disabled={saving || isLocked}
              className={`w-full py-4 rounded-2xl font-bold text-base transition-all active:scale-95 flex items-center justify-center gap-2 ${
                saved
                  ? 'bg-[#1A1A2E] border border-[#00FF87] text-[#00FF87]'
                  : 'bg-[#00FF87] text-black'
              } disabled:opacity-50`}
            >
              {saved && <Check size={18} />}
              {saving ? 'Guardando...' : saved ? 'Predicción guardada' : 'Confirmar predicción'}
            </button>
          </>
        )}
      </div>
    </div>
  )
}
