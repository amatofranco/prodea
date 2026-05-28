import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ChevronLeft, Award } from 'lucide-react'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'
import { BadgePill, EMOJIS } from '../components/BadgePill'
import FigurineCard from '../components/FigurineCard'

function jornadaLabel(phase, matchday) {
  if (phase === 'Group') return `Fecha ${matchday}`
  return { R32: 'Dieciseisavos', R16: 'Octavos', QF: 'Cuartos', SF: 'Semis', ThirdPlace: '3er Puesto', Final: 'Final' }[phase] ?? phase
}

const ACCUMULATIVE_LABELS = {
  EnCaidaLibre: 'En caída libre',
  RachaInfernal: 'Racha infernal',
  ElMuro: 'El Muro',
  ElFantasma: 'El Fantasma',
}

export default function ProfilePage() {
  const { tournamentId, userId } = useParams()
  const navigate = useNavigate()
  const currentUser = useAuthStore((s) => s.user)
  const [profile, setProfile] = useState(null)
  const [tournament, setTournament] = useState(null)
  const [selectedBadge, setSelectedBadge] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    Promise.all([
      api.getProfile(tournamentId, userId),
      api.getTournament(tournamentId),
    ]).then(([p, t]) => {
      setProfile(p)
      setTournament(t)
    }).finally(() => setLoading(false))
  }, [tournamentId, userId])

  if (loading) return <div className="flex-1 bg-[#0D0D0D]" />

  const avatar = profile.username[0].toUpperCase()
  const isMe = currentUser?.id === Number(userId)

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D]">
      {/* Header */}
      <div className="px-4 pt-12 pb-5 bg-gradient-to-b from-[#1A1A2E] to-[#0D0D0D]">
        <div className="flex items-center gap-3 mb-5">
          <button onClick={() => navigate(-1)} className="text-[#8A8A9A]">
            <ChevronLeft size={24} />
          </button>
          <p className="text-[#8A8A9A] text-sm">{tournament?.name}</p>
        </div>

        <div className="flex items-center gap-4">
          <div className="w-16 h-16 rounded-full bg-[#00FF87] flex items-center justify-center text-black text-2xl font-bold">
            {avatar}
          </div>
          <div>
            <h1 className="text-2xl font-bold text-white">
              {profile.username} {isMe && <span className="text-[#8A8A9A] text-base font-normal">(vos)</span>}
            </h1>
            <p className="text-[#8A8A9A] text-sm">#{profile.rank} en el torneo</p>
          </div>
          <div className="ml-auto text-right">
            <p className="text-3xl font-bold text-[#00FF87]" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
              {profile.totalPoints}
            </p>
            <p className="text-[#8A8A9A] text-xs">puntos totales</p>
          </div>
        </div>

        {/* Accumulative badges */}
        {profile.accumulativeBadges.length > 0 && (
          <div className="flex flex-wrap gap-2 mt-4">
            {profile.accumulativeBadges.map((b) => (
              <span
                key={b.badgeType}
                className="flex items-center gap-1 px-2 py-1 rounded-full bg-[#2A2A3E] text-white text-xs font-semibold"
              >
                {EMOJIS[b.badgeType]} {ACCUMULATIVE_LABELS[b.badgeType] || b.badgeType}
              </span>
            ))}
          </div>
        )}
      </div>

      {/* Matchday badges history */}
      <div className="flex-1 px-4 py-4 flex flex-col gap-3 overflow-y-auto">
        <h2 className="text-[#8A8A9A] text-xs uppercase tracking-widest font-semibold">Historial de jornadas</h2>

        {profile.matchdayBadges.length === 0 ? (
          <div className="flex flex-col items-center gap-2 py-12 text-center">
            <Award size={36} className="text-[#2A2A3E]" />
            <p className="text-[#8A8A9A] text-sm">Todavía no hay jornadas terminadas</p>
          </div>
        ) : (
          profile.matchdayBadges.map((b) => (
            <div
              key={`${b.phase}-${b.matchday}`}
              className="p-4 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] flex items-center gap-3"
            >
              <span className="text-3xl leading-none">{EMOJIS[b.badgeType] || '❓'}</span>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 flex-wrap">
                  <BadgePill type={b.badgeType} />
                  <span className="text-[#8A8A9A] text-xs">{jornadaLabel(b.phase, b.matchday)}</span>
                </div>
                <p className="text-white/60 text-xs italic mt-1 line-clamp-2">"{b.randomPhrase}"</p>
              </div>
              <div className="text-right shrink-0">
                <p className="text-lg font-bold text-white" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                  {b.pointsInMatchday}
                </p>
                <p className="text-[10px] text-[#8A8A9A]">pts</p>
              </div>
              <button
                onClick={() => setSelectedBadge(selectedBadge?.phase === b.phase && selectedBadge?.matchday === b.matchday ? null : b)}
                className="shrink-0 px-2 py-1 rounded-lg bg-[#00FF87]/10 text-[#00FF87] text-xs font-semibold"
              >
                Card
              </button>
            </div>
          ))
        )}
      </div>

      {/* Figurine card modal */}
      {selectedBadge && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 px-6"
          onClick={() => setSelectedBadge(null)}
        >
          <div onClick={(e) => e.stopPropagation()}>
            <FigurineCard
              badge={selectedBadge}
              username={profile.username}
              tournamentName={tournament?.name}
            />
          </div>
        </div>
      )}
    </div>
  )
}
