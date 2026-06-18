import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ChevronLeft, Award, X, Share2 } from 'lucide-react'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'
import { BadgePill, EMOJIS } from '../components/BadgePill'
import FigurineCard from '../components/FigurineCard'

function jornadaLabel(phase, matchday) {
  if (phase === 'Group') return `Fecha ${matchday}`
  return { R32: 'Dieciseisavos', R16: 'Octavos', QF: 'Cuartos', SF: 'Semis', ThirdPlace: '3er Puesto', Final: 'Final' }[phase] ?? phase
}

const ACCUMULATIVE_LABELS = {
  PecheadaTotal: 'Pecheada Total',
  RachaInfernal: 'Racha infernal',
  ElMuro: 'El Muro',
  ElFantasma: 'El Fantasma',
  TripleMufa: 'Triple Mufa',
}

const ACCUMULATIVE_DESCRIPTIONS = {
  PecheadaTotal: '3 fechas consecutivas bajando puntos',
  RachaInfernal: '3 jornadas consecutivas siendo El Crack',
  ElMuro: 'Nunca fue último en toda la competencia',
  ElFantasma: 'Olvidó cargar predicciones más de 3 veces',
  TripleMufa: '3 jornadas consecutivas siendo El Mufa',
}

export default function ProfilePage() {
  const { tournamentId, userId } = useParams()
  const navigate = useNavigate()
  const currentUser = useAuthStore((s) => s.user)
  const [profile, setProfile] = useState(null)
  const [tournament, setTournament] = useState(null)
  const [selectedBadge, setSelectedBadge] = useState(null)
  const [selectedAccBadge, setSelectedAccBadge] = useState(null)
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

  const displayName = profile.fullName ?? profile.username
  const avatar = displayName[0].toUpperCase()
  const isMe = currentUser?.id === Number(userId)

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D]">
      {/* Header */}
      <div className="px-4 pt-12 md:pt-6 pb-5 bg-gradient-to-b from-[#1A1A2E] to-[#0D0D0D]">
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
              {displayName} {isMe && <span className="text-[#8A8A9A] text-base font-normal">(vos)</span>}
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
          <div className="mt-4">
            <div className="flex flex-wrap gap-2">
              {profile.accumulativeBadges.map((b) => (
                <button
                  key={b.badgeType}
                  onClick={() => setSelectedAccBadge(selectedAccBadge === b.badgeType ? null : b.badgeType)}
                  className={`flex items-center gap-1 px-2 py-1 rounded-full text-xs font-semibold transition-colors ${
                    selectedAccBadge === b.badgeType
                      ? 'bg-[#00FF87]/20 text-[#00FF87] ring-1 ring-[#00FF87]/40'
                      : 'bg-[#2A2A3E] text-white'
                  }`}
                >
                  {EMOJIS[b.badgeType]} {ACCUMULATIVE_LABELS[b.badgeType] || b.badgeType}
                </button>
              ))}
            </div>
            {selectedAccBadge && (
              <div className="mt-2 px-3 py-2 rounded-xl bg-[#2A2A3E] border border-[#3A3A5E]">
                <p className="text-white text-xs font-semibold">
                  {EMOJIS[selectedAccBadge]} {ACCUMULATIVE_LABELS[selectedAccBadge]}
                </p>
                <p className="text-[#8A8A9A] text-xs mt-0.5">
                  {ACCUMULATIVE_DESCRIPTIONS[selectedAccBadge]}
                </p>
              </div>
            )}
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
              className="px-3 py-2.5 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] flex items-center gap-2"
            >
              <span className="text-2xl leading-none shrink-0">{EMOJIS[b.badgeType] || '❓'}</span>
              <div className="flex-1 min-w-0">
                <BadgePill type={b.badgeType} showTag={false} />
                <div className="flex items-center gap-1.5 mt-0.5">
                  <span className="text-[#8A8A9A] text-[10px]">{jornadaLabel(b.phase, b.matchday)}</span>
                  <span className="text-[#8A8A9A] text-[10px]">·</span>
                  <span className="text-white/50 text-[10px] italic truncate">"{b.randomPhrase}"</span>
                </div>
              </div>
              <div className="flex flex-col items-end gap-1 shrink-0">
                <div className="text-right">
                  <span className="text-base font-bold text-white" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                    {b.pointsInMatchday}
                  </span>
                  <span className="text-[9px] text-[#8A8A9A] ml-0.5">pts</span>
                </div>
                <button
                  onClick={() => setSelectedBadge(selectedBadge?.phase === b.phase && selectedBadge?.matchday === b.matchday ? null : b)}
                  className="flex items-center gap-1 px-1.5 py-0.5 rounded-md bg-[#00FF87]/10 text-[#00FF87] text-[10px] font-semibold"
                >
                  <Share2 size={10} />
                  Carta
                </button>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Figurine card modal */}
      {selectedBadge && (
        <div
          className="fixed inset-0 z-50 flex flex-col bg-black/90 overflow-y-auto"
          onClick={() => setSelectedBadge(null)}
        >
          <div className="flex justify-end px-4 pt-4 pb-2 shrink-0">
            <button
              onClick={() => setSelectedBadge(null)}
              className="w-9 h-9 rounded-full bg-[#1A1A2E] flex items-center justify-center text-[#8A8A9A]"
            >
              <X size={18} />
            </button>
          </div>
          <div
            className="flex-1 flex items-start justify-center px-6 pb-10 pt-2"
            onClick={(e) => e.stopPropagation()}
          >
            <FigurineCard
              badge={selectedBadge}
              username={displayName}
              tournamentName={tournament?.name}
              rank={profile.rank}
            />
          </div>
        </div>
      )}
    </div>
  )
}
