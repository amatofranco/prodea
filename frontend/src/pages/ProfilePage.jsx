import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ChevronLeft, Award, X, Share2 } from 'lucide-react'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'
import { BadgePill, EMOJIS } from '../components/BadgePill'
import FigurineCard from '../components/FigurineCard'
import { getTeam, getFlagUrl } from '../data/teamsData'

function SmallFlag({ name }) {
  const { flag } = getTeam(name)
  const flagUrl = getFlagUrl(flag)
  return (
    <div className="w-5 h-4 rounded-[2px] overflow-hidden bg-[#2A2A3E] shrink-0">
      {flagUrl && <img src={flagUrl} alt={name} loading="lazy" className="w-full h-full object-cover opacity-85" />}
    </div>
  )
}

function jornadaLabel(phase, matchday) {
  if (phase === 'Group') return `Fecha ${matchday}`
  return { R32: 'Dieciseisavos', R16: 'Octavos', QF: 'Cuartos', SF: 'Semis', ThirdPlace: '3er Puesto', Final: 'Final' }[phase] ?? phase
}

const ACCUMULATIVE_LABELS = {
  PecheadaTotal: 'Pecheada Total',
  RachaInfernal: 'Crack Total',
  ElMuro: 'El Muro',
  ElFantasma: 'El Fantasma',
  TripleMufa: 'Triple Mufa',
  TibiezaTotal: 'Tibieza Total',
  GoleadorSerial: 'Goleador Serial',
  RusticoTotal: 'Rústico Total',
}

const ACCUMULATIVE_DESCRIPTIONS = {
  PecheadaTotal: '3 jornadas consecutivas siendo Pecho Frío',
  RachaInfernal: '3 jornadas consecutivas siendo El Crack',
  ElMuro: 'Nunca fue último en toda la competencia',
  ElFantasma: 'Olvidó cargar predicciones más de 3 veces',
  TripleMufa: '3 jornadas consecutivas siendo El Mufa',
  TibiezaTotal: '3 jornadas consecutivas siendo El Tibio',
  GoleadorSerial: '3 jornadas consecutivas siendo El Goleador',
  RusticoTotal: '3 jornadas consecutivas siendo El Rústico',
}

export default function ProfilePage() {
  const { tournamentId, userId } = useParams()
  const navigate = useNavigate()
  const currentUser = useAuthStore((s) => s.user)
  const [profile, setProfile] = useState(null)
  const [tournament, setTournament] = useState(null)
  const [predictions, setPredictions] = useState([])
  const [selectedTab, setSelectedTab] = useState('jornadas')
  const [selectedBadge, setSelectedBadge] = useState(null)
  const [selectedAccBadge, setSelectedAccBadge] = useState(null)
  const [loading, setLoading] = useState(true)
  useEffect(() => {
    Promise.all([
      api.getProfile(tournamentId, userId),
      api.getTournament(tournamentId),
      api.getUserPredictions(tournamentId, userId),
    ]).then(([p, t, preds]) => {
      setProfile(p)
      setTournament(t)
      setPredictions(preds)
    }).finally(() => setLoading(false))
  }, [tournamentId, userId])

  if (loading) return <div className="flex-1 bg-[#0D0D0D]" />

  const displayName = profile.fullName ?? profile.username
  const avatar = displayName[0].toUpperCase()
  const isMe = currentUser?.id === Number(userId)
  const exactCount = predictions.filter((m) => m.userPrediction?.pointsEarned === 3).length

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

        <div className="flex items-center gap-3">
          <div className="w-14 h-14 md:w-20 md:h-20 rounded-full bg-[#00FF87] flex items-center justify-center text-black text-xl md:text-3xl font-bold shrink-0">
            {avatar}
          </div>
          <div className="min-w-0 flex-1">
            <h1 className="text-base md:text-3xl font-bold text-white">
              {displayName} {isMe && <span className="text-[#8A8A9A] text-xs md:text-base font-normal">(vos)</span>}
            </h1>
            <p className="text-[#8A8A9A] text-sm">#{profile.rank} en el torneo</p>
          </div>
          <div className="ml-auto flex items-start gap-3 shrink-0">
            <div className="text-right">
              <p className="text-2xl md:text-4xl font-bold text-[#00FF87]" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                {profile.totalPoints}
              </p>
              <p className="text-[#8A8A9A] text-xs">puntos totales</p>
            </div>
            <div className="text-right">
              <p className="text-2xl md:text-4xl font-bold text-[#00FF87]" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                {exactCount}
              </p>
              <p className="text-[#8A8A9A] text-xs leading-tight">resultados<br />exactos</p>
            </div>
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

      {/* Tab switcher */}
      <div className="flex gap-2 px-4 pt-4">
        {[
          { key: 'jornadas', label: 'Historial de jornadas' },
          { key: 'pronosticos', label: 'Pronósticos' },
        ].map((tab) => (
          <button
            key={tab.key}
            onClick={() => setSelectedTab(tab.key)}
            className={`px-4 py-1.5 rounded-full text-xs font-semibold transition-colors ${
              selectedTab === tab.key
                ? 'bg-[#00FF87] text-black'
                : 'bg-[#1A1A2E] text-[#8A8A9A] border border-[#2A2A3E]'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {selectedTab === 'jornadas' && (
        <div className="flex-1 px-4 py-4 flex flex-col gap-3 overflow-y-auto">
          {profile.matchdayBadges.length === 0 ? (
            <div className="flex flex-col items-center gap-2 py-12 text-center">
              <Award size={36} className="text-[#2A2A3E]" />
              <p className="text-[#8A8A9A] text-sm">Todavía no hay jornadas terminadas</p>
            </div>
          ) : (
            profile.matchdayBadges.map((b) => (
              <div
                key={`${b.phase}-${b.matchday}`}
                className="px-4 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] flex items-center gap-3"
              >
                {b.badgeType === 'Campeon'
                  ? <img src="/trophy.svg" alt="" className="h-9 w-auto shrink-0" />
                  : <span className="text-3xl leading-none shrink-0">{EMOJIS[b.badgeType] || '❓'}</span>
                }
                <div className="flex-1 min-w-0">
                  <BadgePill type={b.badgeType} showTag={false} />
                  <span className="text-[#8A8A9A] text-xs mt-0.5 block">{jornadaLabel(b.phase, b.matchday)}</span>
                  <p className="text-white/50 text-xs italic mt-0.5">"{b.randomPhrase}"</p>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  <div className="text-right">
                    <span className="text-2xl font-bold text-white" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                      {b.pointsInMatchday}
                    </span>
                    <span className="text-[10px] text-[#8A8A9A] ml-0.5">pts</span>
                  </div>
                  <button
                    onClick={() => setSelectedBadge(selectedBadge?.phase === b.phase && selectedBadge?.matchday === b.matchday ? null : b)}
                    className="flex items-center gap-1 px-2 py-1 rounded-md bg-[#00FF87]/10 text-[#00FF87] text-xs font-semibold"
                  >
                    <Share2 size={11} />
                    Carta
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      )}

      {selectedTab === 'pronosticos' && (
        <div className="flex-1 px-4 py-4 flex flex-col gap-1.5 overflow-y-auto">
          {predictions.length === 0 ? (
            <div className="flex flex-col items-center gap-2 py-12 text-center">
              <Award size={36} className="text-[#2A2A3E]" />
              <p className="text-[#8A8A9A] text-sm">Todavía no hay pronósticos finalizados</p>
            </div>
          ) : (
            predictions.map((m) => {
              const homeName = m.homeTeam !== 'TBD' ? m.homeTeam : (m.homeTeamLabel ?? m.homeTeam)
              const awayName = m.awayTeam !== 'TBD' ? m.awayTeam : (m.awayTeamLabel ?? m.awayTeam)
              const pred = m.userPrediction
              return (
                <div key={m.id} className="px-2.5 py-2 rounded-lg bg-[#1A1A2E] border border-[#2A2A3E] flex items-center gap-2">
                  <span className="text-[8px] text-[#8A8A9A] w-10 shrink-0">{jornadaLabel(m.phase, m.matchday)}</span>
                  <div className="flex items-center gap-1 flex-1 min-w-0 justify-end">
                    <span className="text-[11px] text-white truncate">{homeName}</span>
                    <SmallFlag name={m.homeTeam} />
                  </div>
                  <span className="text-sm font-bold text-white shrink-0" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                    {m.homeScore}–{m.awayScore}
                  </span>
                  <div className="flex items-center gap-1 flex-1 min-w-0">
                    <SmallFlag name={m.awayTeam} />
                    <span className="text-[11px] text-white truncate">{awayName}</span>
                  </div>
                  <div className="flex items-center gap-1 shrink-0 pl-2 border-l border-[#2A2A3E]">
                    <span className="text-xs font-semibold text-white">{pred.predictedHomeScore}-{pred.predictedAwayScore}</span>
                    <span
                      className={`text-[11px] font-bold ${
                        pred.pointsEarned === 3
                          ? 'text-[#00FF87]'
                          : pred.pointsEarned > 0
                          ? 'text-white'
                          : 'text-[#8A8A9A]'
                      }`}
                    >
                      +{pred.pointsEarned}
                    </span>
                  </div>
                </div>
              )
            })
          )}
        </div>
      )}

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
