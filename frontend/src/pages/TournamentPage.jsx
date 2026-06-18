import { useEffect, useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { Share2, ChevronLeft, ChevronRight, Wifi, Lock, X, Pencil, ImageDown, LogOut, MoreVertical, Bell } from 'lucide-react'
import QRCode from 'qrcode'
import { api } from '../services/api'
import { useTournamentStore } from '../store/tournamentStore'
import { useAuthStore } from '../store/authStore'
import { joinTournament, leaveTournament, onMatchUpdated } from '../services/signalr'
import { BadgePill } from '../components/BadgePill'
import ApiStatusBanner from '../components/ApiStatusBanner'
import ChampionPickBanner from '../components/ChampionPickBanner'
import { getTeam, getFlagUrl } from '../data/teamsData'
import InstallBanner from '../components/InstallBanner'
import { usePushNotifications } from '../hooks/usePushNotifications'

const MAX_DESC = 150
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
  const display = name !== 'TBD' ? name : (label ?? name)
  return (
    <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
      <div className="rounded-md overflow-hidden bg-[#2A2A3E]" style={{ width: size, height: Math.round(size * 0.67) }}>
        {url
          ? <img src={url} alt={display} loading="lazy" className="w-full h-full object-cover opacity-85" />
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
          <span className="w-1.5 h-1.5 rounded-full bg-[#FF6B35] animate-pulse" />
          {match.livePhase ?? 'EN VIVO'}
          {!match.livePhase && (match.minuteDisplay || match.minute != null) && ` · ${match.minuteDisplay ?? `${match.minute}'`}`}
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
            {pred.predictedPenaltyWinner && (
              <span className="text-[#F59E0B]">
                {' · Pasa: '}{pred.predictedPenaltyWinner === 'home' ? match.homeTeam : match.awayTeam}
              </span>
            )}
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
    <div className="fixed inset-0 z-[60] flex flex-col justify-end bg-black/60" onClick={onClose}>
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
                {(p.fullName ?? p.username)[0].toUpperCase()}
              </div>
              <span className="flex-1 text-white text-sm font-medium truncate">{p.fullName ?? p.username}</span>
              {p.predictedHomeScore != null ? (
                <div className="flex flex-col items-end">
                  <span className="text-white font-bold text-sm" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                    {p.predictedHomeScore} – {p.predictedAwayScore}
                  </span>
                  {p.predictedPenaltyWinner && (
                    <span className="text-[9px] text-[#F59E0B]">
                      Pasa: {p.predictedPenaltyWinner === 'home' ? match.homeTeam : match.awayTeam}
                    </span>
                  )}
                </div>
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

function ChampionProdeaBanner({ leaderboard, matches, currentUserId }) {
  const isTournamentFinished = matches.some(m => m.phase === 'Final' && m.status === 'Finished')
  if (!isTournamentFinished || leaderboard.length === 0) return null

  const winner = leaderboard[0]
  const isMe = winner.userId === currentUserId

  return (
    <div className="mx-4 mb-1 p-4 rounded-2xl bg-gradient-to-br from-[#F59E0B]/20 to-[#FF6B35]/10 border border-[#F59E0B]/50">
      <p className="text-[#F59E0B] text-[10px] font-bold uppercase tracking-widest mb-3">
        🏆 Campeón del Prode
      </p>
      <div className="flex items-center gap-3">
        <div className="w-14 h-14 rounded-full bg-[#F59E0B] flex items-center justify-center text-black font-bold text-2xl shrink-0">
          {(winner.fullName ?? winner.username)[0].toUpperCase()}
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-white font-bold text-xl leading-tight truncate">{winner.fullName ?? winner.username}</p>
          <p className="text-[#F59E0B] text-sm font-semibold">{winner.totalPoints} puntos</p>
        </div>
        <span className="text-4xl">🏆</span>
      </div>
      {isMe && (
        <p className="mt-3 text-center text-[#00FF87] text-sm font-bold">
          🎉 ¡Ganaste el prode!
        </p>
      )}
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
  const [editingDesc, setEditingDesc] = useState(false)
  const [descDraft, setDescDraft] = useState('')
  const [showShareMenu, setShowShareMenu] = useState(false)
  const [showOptionsMenu, setShowOptionsMenu] = useState(false)
  const [matchdayWinners, setMatchdayWinners] = useState([])
  const [showLeaveConfirm, setShowLeaveConfirm] = useState(false)
  const [leaving, setLeaving] = useState(false)
  const { showModal: showPushBanner, subscribe: subscribePush, dismiss: dismissPush } = usePushNotifications()
  const phaseBarRef = useRef(null)

  async function saveDescription() {
    const updated = await api.updateTournament(id, { description: descDraft.trim() || null })
    setTournament(t => ({ ...t, description: updated.description }))
    setEditingDesc(false)
  }

  async function handleLeaveTournament() {
    setLeaving(true)
    try {
      await api.leaveTournament(id)
      navigate('/')
    } catch (err) {
      setLeaving(false)
      setShowLeaveConfirm(false)
    }
  }

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
      api.getMatchdayWinners(id).then(setMatchdayWinners),
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

  function getInviteLink() {
    return `${window.location.origin}/join/${tournament?.inviteLink}`
  }

  function shareText() {
    const link = getInviteLink()
    navigator.share({
      title: 'Prodea',
      text: `¡Te invito al torneo *${tournament?.name}* en Prodea!`,
      url: link,
    }).catch(() => {})
    setShowShareMenu(false)
  }

  async function shareImage() {
    setShowShareMenu(false)
    const link = getInviteLink()
    const W = 800, H = 800
    const canvas = document.createElement('canvas')
    canvas.width = W
    canvas.height = H
    const ctx = canvas.getContext('2d')

    // Fondo
    ctx.fillStyle = '#0D0D0D'
    ctx.fillRect(0, 0, W, H)

    // Borde verde
    ctx.strokeStyle = '#00FF87'
    ctx.lineWidth = 6
    ctx.roundRect(20, 20, W - 40, H - 40, 24)
    ctx.stroke()

    // "Unite al torneo"
    ctx.fillStyle = '#8A8A9A'
    ctx.font = '500 28px Arial'
    ctx.textAlign = 'center'
    ctx.fillText('¡Unite al torneo!', W / 2, 100)

    // Nombre del torneo
    ctx.fillStyle = '#FFFFFF'
    ctx.font = 'bold 60px Arial Black, Arial'
    ctx.textAlign = 'center'
    const name = tournament?.name ?? ''
    ctx.fillText(name.length > 20 ? name.slice(0, 20) + '…' : name, W / 2, 175)

    // QR
    const qrDataUrl = await QRCode.toDataURL(link, {
      width: 320, margin: 2,
      color: { dark: '#FFFFFF', light: '#111111' },
    })
    const qrImg = new Image()
    qrImg.src = qrDataUrl
    await new Promise((r) => { qrImg.onload = r })
    const qrSize = 320
    ctx.drawImage(qrImg, (W - qrSize) / 2, 230, qrSize, qrSize)

    // Instrucción
    ctx.fillStyle = '#8A8A9A'
    ctx.font = '24px Arial'
    ctx.fillText('Escaneá para unirte', W / 2, 590)

    // Divider
    ctx.strokeStyle = '#2A2A3E'
    ctx.lineWidth = 1
    ctx.beginPath()
    ctx.moveTo(60, 620)
    ctx.lineTo(W - 60, 620)
    ctx.stroke()

    // Wordmark
    const wordmark = new Image()
    wordmark.src = '/logo-wordmark.png'
    await new Promise((r) => { wordmark.onload = r; wordmark.onerror = r })
    const wmH = 70
    const wmW = wordmark.naturalWidth * (wmH / wordmark.naturalHeight)
    ctx.drawImage(wordmark, (W - wmW) / 2, 648, wmW, wmH)

    // URL
    ctx.fillStyle = '#8A8A9A'
    ctx.font = '22px Arial'
    ctx.fillText('prodea.app', W / 2, 738)

    canvas.toBlob(async (blob) => {
      const file = new File([blob], 'torneo-prodea.png', { type: 'image/png' })
      if (navigator.canShare?.({ files: [file] })) {
        await navigator.share({ files: [file], title: tournament?.name })
      } else {
        const a = document.createElement('a')
        a.href = URL.createObjectURL(blob)
        a.download = 'torneo-prodea.png'
        a.click()
      }
    }, 'image/png')
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
      <div className="px-4 pt-12 md:pt-6 pb-3 bg-[#1A1A2E]">
        <div className="flex items-center gap-3 mb-3">
          <button onClick={() => navigate('/')} className="text-[#8A8A9A] active:text-white">
            <ChevronLeft size={24} />
          </button>
          <div className="flex-1 min-w-0">
            <h1 className="text-xl font-bold text-white truncate">{tournament?.name}</h1>
            <p className="text-[#8A8A9A] text-xs">{leaderboard.length} participantes</p>
          </div>
          <div className="relative">
            <button
              onClick={() => setShowShareMenu((v) => !v)}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[#00FF87]/10 text-[#00FF87] text-xs font-semibold"
            >
              <Share2 size={14} /> Invitar
            </button>
            {showShareMenu && (
              <>
                <div className="fixed inset-0 z-40" onClick={() => setShowShareMenu(false)} />
                <div className="absolute right-0 top-9 z-50 bg-[#1A1A2E] border border-[#2A2A3E] rounded-2xl shadow-xl overflow-hidden min-w-[180px]">
                  <button
                    onClick={shareText}
                    className="flex items-center w-full px-4 py-3 text-sm text-white font-semibold active:bg-[#2A2A3E]"
                  >
                    <Share2 size={16} className="text-[#00FF87] shrink-0" />
                    <span className="flex-1 text-center">Compartir invitación</span>
                  </button>
                  <div className="h-px bg-[#2A2A3E]" />
                  <button
                    onClick={shareImage}
                    className="flex items-center w-full px-4 py-3 text-sm text-white font-semibold active:bg-[#2A2A3E]"
                  >
                    <ImageDown size={16} className="text-[#00FF87] shrink-0" />
                    <span className="flex-1 text-center leading-tight">Compartir<br />QR</span>
                  </button>
                </div>
              </>
            )}
          </div>
          <div className="relative">
            <button
              onClick={() => setShowOptionsMenu((v) => !v)}
              className="flex items-center justify-center w-8 h-8 rounded-lg bg-[#2A2A3E] text-[#8A8A9A]"
            >
              <MoreVertical size={16} />
            </button>
            {showOptionsMenu && (
              <>
                <div className="fixed inset-0 z-40" onClick={() => setShowOptionsMenu(false)} />
                <div className="absolute right-0 top-9 z-50 bg-[#1A1A2E] border border-[#2A2A3E] rounded-2xl shadow-xl overflow-hidden min-w-[180px]">
                  <button
                    onClick={() => { setShowOptionsMenu(false); setShowLeaveConfirm(true) }}
                    className="flex items-center w-full px-4 py-3 text-sm text-[#FF6B35] font-semibold active:bg-[#2A2A3E]"
                  >
                    <LogOut size={16} className="shrink-0" />
                    <span className="flex-1 text-center">Salir del torneo</span>
                  </button>
                </div>
              </>
            )}
          </div>
        </div>

        {editingDesc ? (
          <div className="mb-3 flex flex-col gap-2">
            <div className="relative">
              <textarea
                value={descDraft}
                onChange={(e) => setDescDraft(e.target.value.slice(0, MAX_DESC))}
                rows={3}
                autoFocus
                className="w-full px-3 py-2.5 rounded-xl bg-[#0D0D0D] border border-[#00FF87]/40 text-white text-sm placeholder-[#8A8A9A] focus:outline-none resize-none"
                placeholder="Premio al ganador, prenda al último..."
              />
              <p className="text-right text-xs text-[#8A8A9A] mt-0.5">{descDraft.length}/{MAX_DESC}</p>
            </div>
            <div className="flex gap-2">
              <button onClick={saveDescription} className="flex-1 py-2 rounded-xl bg-[#00FF87] text-black text-sm font-bold">Guardar</button>
              <button onClick={() => setEditingDesc(false)} className="flex-1 py-2 rounded-xl bg-[#2A2A3E] text-[#8A8A9A] text-sm">Cancelar</button>
            </div>
          </div>
        ) : (
          <div className="mb-3 flex items-start gap-2 group">
            {(tournament?.description || tournament?.adminUserId === user?.id) && (
              <p className={`flex-1 text-sm leading-relaxed whitespace-pre-wrap ${tournament?.description ? 'text-[#8A8A9A]' : 'text-[#2A2A3E] italic'}`}>
                {tournament?.description || (tournament?.adminUserId === user?.id ? 'Agregá una descripción, premios o prendas...' : '')}
              </p>
            )}
            {tournament?.adminUserId === user?.id && (
              <button
                onClick={() => { setDescDraft(tournament?.description || ''); setEditingDesc(true) }}
                className="shrink-0 text-[#8A8A9A] active:text-[#00FF87] mt-0.5"
              >
                <Pencil size={14} />
              </button>
            )}
          </div>
        )}

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

      <InstallBanner className="mt-3" />

      {/* Content */}
      <div className="flex-1 overflow-y-auto">
        {activeTab === 'tabla' ? (
          <div className="py-4 flex flex-col gap-2">
            <ChampionProdeaBanner leaderboard={leaderboard} matches={matches} currentUserId={user?.id} />
            {showPushBanner && (
              <div className="mx-4 flex items-center gap-3 p-3 rounded-2xl bg-[#1A1A2E] border border-[#00FF87]/20">
                <Bell size={16} className="text-[#00FF87] shrink-0" />
                <p className="flex-1 text-[#8A8A9A] text-xs leading-snug">Activá las notificaciones para saber cuándo termina cada jornada</p>
                <button
                  onClick={() => { subscribePush(); dismissPush() }}
                  className="shrink-0 px-3 py-1.5 rounded-lg bg-[#00FF87] text-black text-xs font-bold active:scale-95"
                >
                  Activar
                </button>
                <button onClick={dismissPush} className="text-[#8A8A9A] active:opacity-60">
                  <X size={14} />
                </button>
              </div>
            )}
            <p className="text-[#8A8A9A] text-xs uppercase tracking-widest font-semibold px-4 mb-1">
              Tabla de posiciones
            </p>
            <div className="px-2 flex flex-col gap-1.5">
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

            {matchdayWinners.length > 0 && (
              <div className="px-4 mt-5">
                <p className="text-[#8A8A9A] text-xs uppercase tracking-widest font-semibold mb-3">
                  Ganadores por fecha
                </p>
                <div className="flex flex-col gap-2">
                  {matchdayWinners.map((w) => (
                    <div
                      key={`${w.phase}-${w.matchday}`}
                      onClick={() => navigate(`/torneos/${id}/perfil/${w.userId}`)}
                      className="flex items-center gap-3 px-4 py-3 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] cursor-pointer active:border-[#00FF87] transition-colors"
                    >
                      <span className="text-lg">🏆</span>
                      <div className="flex-1 min-w-0">
                        <p className="text-white text-sm font-semibold truncate">
                          {w.fullName ?? w.username}
                        </p>
                        <p className="text-[#8A8A9A] text-xs">{w.label}</p>
                      </div>
                      <span className="text-lg font-bold text-[#F59E0B]" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                        {w.points}<span className="text-xs text-[#8A8A9A] ml-0.5">pts</span>
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}
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
                className="px-4 pt-4 pb-[calc(3.5rem+env(safe-area-inset-bottom))] flex flex-col gap-2"
              >
                {phaseTab === 'Final' && (
                  <ChampionPickBanner tournamentId={id} currentUserId={user?.id} readOnly />
                )}
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

      {/* Leave tournament confirmation */}
      <AnimatePresence>
        {showLeaveConfirm && (
          <div
            className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-6"
            onClick={() => !leaving && setShowLeaveConfirm(false)}
          >
            <motion.div
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.95 }}
              transition={{ duration: 0.15 }}
              onClick={(e) => e.stopPropagation()}
              className="w-full max-w-sm bg-[#1A1A2E] border border-[#2A2A3E] rounded-2xl p-5"
            >
              <h3 className="text-white font-bold text-lg mb-2">¿Salir del torneo?</h3>
              <p className="text-[#8A8A9A] text-sm mb-5">
                Vas a dejar de participar en "{tournament?.name}". Tus predicciones y motes en este torneo se van a perder y no vas a poder volver a entrar salvo que te inviten de nuevo.
                {tournament?.adminUserId === user?.id && ' Como sos el admin, el rol pasará a otro participante (o el torneo se eliminará si sos el único).'}
              </p>
              <div className="flex gap-2">
                <button
                  onClick={() => setShowLeaveConfirm(false)}
                  disabled={leaving}
                  className="flex-1 py-2.5 rounded-xl bg-[#2A2A3E] text-white text-sm font-semibold disabled:opacity-50"
                >
                  Cancelar
                </button>
                <button
                  onClick={handleLeaveTournament}
                  disabled={leaving}
                  className="flex-1 py-2.5 rounded-xl bg-[#FF6B35] text-white text-sm font-bold disabled:opacity-50"
                >
                  {leaving ? 'Saliendo...' : 'Salir'}
                </button>
              </div>
            </motion.div>
          </div>
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
      className={`flex items-center gap-2 p-2.5 rounded-2xl border cursor-pointer active:border-[#00FF87] transition-colors ${
        isMe ? 'bg-[#00FF87]/5 border-[#00FF87]/30' : 'bg-[#1A1A2E] border-[#2A2A3E]'
      }`}
    >
      <span className={`w-6 text-center font-bold text-sm ${rankColors[index] || 'text-[#8A8A9A]'}`}>{entry.rank}</span>
      <div className="w-8 h-8 rounded-full bg-[#2A2A3E] flex items-center justify-center text-white font-bold text-sm shrink-0">
        {(entry.fullName ?? entry.username)[0].toUpperCase()}
      </div>
      <div className="flex-1 min-w-0">
        <p className={`font-semibold text-sm truncate ${isMe ? 'text-[#00FF87]' : 'text-white'}`}>
          {entry.fullName ?? entry.username} {isMe && <span className="text-xs font-normal">(vos)</span>}
        </p>
        {entry.currentBadge && <BadgePill type={entry.currentBadge} className="mt-0.5 text-[10px]" />}
      </div>
      <div className="flex items-center gap-1.5 shrink-0">
        <span className="text-xl font-bold text-white" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
          {entry.totalPoints}<span className="text-xs text-[#8A8A9A] ml-0.5">pts</span>
        </span>
        <ChevronRight size={14} className="text-[#8A8A9A]" />
      </div>
    </motion.div>
  )
}
