import { useEffect, useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { motion, AnimatePresence } from 'framer-motion'
import { Share2, ChevronLeft, ChevronRight, Wifi, Lock, X, Pencil, ImageDown, LogOut, MoreVertical, Bell, Calendar } from 'lucide-react'
import { api } from '../services/api'
import { useTournamentStore } from '../store/tournamentStore'
import { useAuthStore } from '../store/authStore'
import { joinTournament, leaveTournament, onMatchUpdated } from '../services/signalr'
import { BadgePill } from '../components/BadgePill'
import ApiStatusBanner from '../components/ApiStatusBanner'
import ChampionPickBanner from '../components/ChampionPickBanner'
import InstallBanner from '../components/InstallBanner'
import TournamentMatchCard from '../components/TournamentMatchCard'
import MatchPredictionsSheet from '../components/MatchPredictionsSheet'
import { shareInviteImage } from '../utils/shareInviteImage'
import { usePushNotifications } from '../hooks/usePushNotifications'

const MAX_DESC = 150

function todayStr() {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}
const PHASE_ORDER = ['group-1', 'group-2', 'group-3', 'R32', 'R16', 'QF', 'SF', 'ThirdPlace', 'Final']
function useTabLabels() {
  const { t } = useTranslation()
  return {
    'group-1': t('phases.Group', { matchday: 1 }), 'group-2': t('phases.Group', { matchday: 2 }), 'group-3': t('phases.Group', { matchday: 3 }),
    R32: t('phases.R32'), R16: t('phases.R16'), QF: t('phases.QF'), SF: t('phases.SF'),
    ThirdPlace: t('phases.ThirdPlace'), Final: t('phases.Final'),
  }
}
function getPhaseKey(m) {
  return m.phase === 'Group' ? `group-${m.matchday ?? 1}` : m.phase
}

function ChampionProdeaBanner({ leaderboard, matches, currentUserId }) {
  const { t } = useTranslation()
  const isTournamentFinished = matches.some(m => m.phase === 'Final' && m.status === 'Finished')
  if (!isTournamentFinished || leaderboard.length === 0) return null

  const winner = leaderboard[0]
  const isMe = winner.userId === currentUserId

  return (
    <div className="mx-4 mb-1 p-4 rounded-2xl bg-gradient-to-br from-[#F59E0B]/20 to-[#FF6B35]/10 border border-[#F59E0B]/50">
      <p className="text-[#F59E0B] text-[10px] font-bold uppercase tracking-widest mb-3 flex items-center gap-1.5">
        <img src="/trophy.svg" alt="" className="h-3 w-auto" /> {t('tournaments.champion')}
      </p>
      <div className="flex items-center gap-3">
        <div className="w-14 h-14 rounded-full bg-[#F59E0B] flex items-center justify-center text-black font-bold text-2xl shrink-0">
          {(winner.fullName ?? winner.username)[0].toUpperCase()}
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-white font-bold text-xl leading-tight truncate">{winner.fullName ?? winner.username}</p>
          <p className="text-[#F59E0B] text-sm font-semibold">{winner.totalPoints} {t('common.points')}</p>
        </div>
        <img src="/trophy.svg" alt="" className="h-12 w-auto" />
      </div>
      {isMe && (
        <p className="mt-3 text-center text-[#00FF87] text-sm font-bold">
          {t('tournaments.youWon')}
        </p>
      )}
    </div>
  )
}

export default function TournamentPage() {
  const { t } = useTranslation()
  const tabLabels = useTabLabels()
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
  const [predSheet, setPredSheet] = useState(null)
  const [editingDesc, setEditingDesc] = useState(false)
  const [descDraft, setDescDraft] = useState('')
  const [showShareMenu, setShowShareMenu] = useState(false)
  const [showOptionsMenu, setShowOptionsMenu] = useState(false)
  const [matchdayWinners, setMatchdayWinners] = useState([])
  const [showLeaveConfirm, setShowLeaveConfirm] = useState(false)
  const [leaving, setLeaving] = useState(false)
  const [showStartDateModal, setShowStartDateModal] = useState(false)
  const [startDateDraft, setStartDateDraft] = useState('')
  const [savingStartDate, setSavingStartDate] = useState(false)
  const { showModal: showPushBanner, subscribe: subscribePush, dismiss: dismissPush } = usePushNotifications()
  const phaseBarRef = useRef(null)

  function getInviteLink() {
    return `${window.location.origin}/join/${tournament?.inviteLink}`
  }

  async function saveDescription() {
    const updated = await api.updateTournament(id, { description: descDraft.trim() || null })
    setTournament(t => ({ ...t, description: updated.description }))
    setEditingDesc(false)
  }

  async function saveStartingMatchDate() {
    if (!startDateDraft) return
    setSavingStartDate(true)
    try {
      const dateToSend = startDateDraft === todayStr() ? new Date().toISOString() : startDateDraft
      const updated = await api.updateTournament(id, { description: tournament?.description ?? null, startingMatchDate: dateToSend })
      setTournament(t => ({ ...t, startingMatchDate: updated.startingMatchDate }))
      setShowStartDateModal(false)
      await Promise.all([
        api.getLeaderboard(id).then(setLeaderboard),
        api.getMatchdayWinners(id).then(setMatchdayWinners),
      ])
    } finally {
      setSavingStartDate(false)
    }
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
    }).catch(() => {})
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
      setMatches((prev) =>
        prev.map((m) =>
          m.id === update.matchId
            ? {
                ...m,
                homeScore: update.homeScore,
                awayScore: update.awayScore,
                status: update.status,
                minute: update.minute ?? m.minute,
                goals: update.goals !== undefined ? update.goals : m.goals,
                livePhase: update.livePhase !== undefined ? update.livePhase : m.livePhase,
                minuteDisplay: update.minuteDisplay !== undefined ? update.minuteDisplay : m.minuteDisplay,
                homePenaltyScore: update.homePenaltyScore !== undefined ? update.homePenaltyScore : m.homePenaltyScore,
                awayPenaltyScore: update.awayPenaltyScore !== undefined ? update.awayPenaltyScore : m.awayPenaltyScore,
              }
            : m
        )
      )
      setLiveCount((prev) => {
        const isNowLive = update.status === 'InProgress'
        const wasLive = prev > 0
        return isNowLive ? Math.max(prev, 1) : wasLive ? Math.max(0, prev - 1) : prev
      })
      api.getLeaderboard(id).then(setLeaderboard)
      api.getMatchdayWinners(id).then(setMatchdayWinners).catch(() => {})
      fetchMatches()
    })

    function onVisible() {
      if (document.visibilityState === 'visible') {
        fetchMatches()
        api.getLeaderboard(id).then(setLeaderboard)
      }
    }
    document.addEventListener('visibilitychange', onVisible)

    return () => {
      off()
      leaveTournament(id)
      document.removeEventListener('visibilitychange', onVisible)
    }
  }, [id])

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

  function shareText() {
    const link = getInviteLink()
    navigator.share({
      title: 'Prodea',
      text: t('tournaments.inviteText', { name: tournament?.name }),
      url: link,
    }).catch(() => {})
    setShowShareMenu(false)
  }

  async function handleShareImage() {
    setShowShareMenu(false)
    await shareInviteImage(tournament?.name, getInviteLink())
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
            <p className="text-[#8A8A9A] text-xs">{leaderboard.length} {t('common.participants')}</p>
          </div>
          <div className="relative">
            <button
              onClick={() => setShowShareMenu((v) => !v)}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[#00FF87]/10 text-[#00FF87] text-xs font-semibold"
            >
              <Share2 size={14} /> {t('tournaments.invite')}
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
                    <span className="flex-1 text-center">{t('tournaments.shareInvite')}</span>
                  </button>
                  <div className="h-px bg-[#2A2A3E]" />
                  <button
                    onClick={handleShareImage}
                    className="flex items-center w-full px-4 py-3 text-sm text-white font-semibold active:bg-[#2A2A3E]"
                  >
                    <ImageDown size={16} className="text-[#00FF87] shrink-0" />
                    <span className="flex-1 text-center leading-tight">{t('tournaments.shareQR')}</span>
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
                  {tournament?.adminUserId === user?.id && (
                    <button
                      onClick={() => {
                        setShowOptionsMenu(false)
                        setStartDateDraft(tournament?.startingMatchDate ? tournament.startingMatchDate.slice(0, 10) : '')
                        setShowStartDateModal(true)
                      }}
                      className="flex items-center w-full px-4 py-3 text-sm text-white font-semibold active:bg-[#2A2A3E] border-b border-[#2A2A3E]"
                    >
                      <Calendar size={16} className="shrink-0" />
                      <span className="flex-1 text-center">{t('tournaments.startDateTitle')}</span>
                    </button>
                  )}
                  <button
                    onClick={() => { setShowOptionsMenu(false); setShowLeaveConfirm(true) }}
                    className="flex items-center w-full px-4 py-3 text-sm text-[#FF6B35] font-semibold active:bg-[#2A2A3E]"
                  >
                    <LogOut size={16} className="shrink-0" />
                    <span className="flex-1 text-center">{t('tournaments.leaveTournament')}</span>
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
                placeholder={t('tournaments.editDescPlaceholder')}
              />
              <p className="text-right text-xs text-[#8A8A9A] mt-0.5">{descDraft.length}/{MAX_DESC}</p>
            </div>
            <div className="flex gap-2">
              <button onClick={saveDescription} className="flex-1 py-2 rounded-xl bg-[#00FF87] text-black text-sm font-bold">{t('common.save')}</button>
              <button onClick={() => setEditingDesc(false)} className="flex-1 py-2 rounded-xl bg-[#2A2A3E] text-[#8A8A9A] text-sm">{t('common.cancel')}</button>
            </div>
          </div>
        ) : (
          <div className="mb-3 flex items-start gap-2 group">
            {(tournament?.description || tournament?.adminUserId === user?.id) && (
              <p className={`flex-1 text-sm leading-relaxed whitespace-pre-wrap ${tournament?.description ? 'text-[#8A8A9A]' : 'text-[#2A2A3E] italic'}`}>
                {tournament?.description || (tournament?.adminUserId === user?.id ? t('tournaments.descPlaceholder') : '')}
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
              {t('tournaments.liveMatchesBanner', { count: liveCount })}
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
              {tab === 'tabla' ? t('tournaments.table') : t('tournaments.fixture')}
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
                <p className="flex-1 text-[#8A8A9A] text-xs leading-snug">{t('tournaments.pushBanner')}</p>
                <button
                  onClick={() => { subscribePush(); dismissPush() }}
                  className="shrink-0 px-3 py-1.5 rounded-lg bg-[#00FF87] text-black text-xs font-bold active:scale-95"
                >
                  {t('tournaments.activate')}
                </button>
                <button onClick={dismissPush} className="text-[#8A8A9A] active:opacity-60">
                  <X size={14} />
                </button>
              </div>
            )}
            <p className="text-[#8A8A9A] text-xs uppercase tracking-widest font-semibold px-4 mb-1">
              {t('tournaments.leaderboard')}
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
                  {t('tournaments.matchdayWinners')}
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
                        <p className="text-[#8A8A9A] text-xs">
                          {w.phase === 'Group' ? t('phases.Group', { matchday: w.matchday }) : t(`phases.${w.phase}`, w.label)}
                        </p>
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
                    {tabLabels[tab] ?? tab}
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
              <h3 className="text-white font-bold text-lg mb-2">{t('tournaments.leaveConfirmTitle')}</h3>
              <p className="text-[#8A8A9A] text-sm mb-5">
                {t('tournaments.leaveConfirmBody', { name: tournament?.name })}
                {tournament?.adminUserId === user?.id && ` ${t('tournaments.leaveConfirmAdmin')}`}
              </p>
              <div className="flex gap-2">
                <button
                  onClick={() => setShowLeaveConfirm(false)}
                  disabled={leaving}
                  className="flex-1 py-2.5 rounded-xl bg-[#2A2A3E] text-white text-sm font-semibold disabled:opacity-50"
                >
                  {t('common.cancel')}
                </button>
                <button
                  onClick={handleLeaveTournament}
                  disabled={leaving}
                  className="flex-1 py-2.5 rounded-xl bg-[#FF6B35] text-white text-sm font-bold disabled:opacity-50"
                >
                  {leaving ? t('tournaments.leaving') : t('tournaments.leave')}
                </button>
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>

      {/* Editar fecha de arranque del torneo (solo admin) */}
      <AnimatePresence>
        {showStartDateModal && (
          <div
            className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-6"
            onClick={() => !savingStartDate && setShowStartDateModal(false)}
          >
            <motion.div
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.95 }}
              transition={{ duration: 0.15 }}
              onClick={(e) => e.stopPropagation()}
              className="w-full max-w-sm bg-[#1A1A2E] border border-[#2A2A3E] rounded-2xl p-5"
            >
              <h3 className="text-white font-bold text-lg mb-2">{t('tournaments.startDateTitle')}</h3>
              <p className="text-[#8A8A9A] text-sm mb-4">
                {t('tournaments.startDateDesc')}
              </p>
              <input
                type="date"
                value={startDateDraft}
                onChange={(e) => setStartDateDraft(e.target.value)}
                className="w-full px-4 py-3 rounded-xl bg-[#0D0D0D] border border-[#2A2A3E] text-white focus:outline-none focus:border-[#00FF87] text-sm mb-5"
              />
              <div className="flex gap-2">
                <button
                  onClick={() => setShowStartDateModal(false)}
                  disabled={savingStartDate}
                  className="flex-1 py-2.5 rounded-xl bg-[#2A2A3E] text-white text-sm font-semibold disabled:opacity-50"
                >
                  {t('common.cancel')}
                </button>
                <button
                  onClick={saveStartingMatchDate}
                  disabled={savingStartDate || !startDateDraft}
                  className="flex-1 py-2.5 rounded-xl bg-[#00FF87] text-black text-sm font-bold disabled:opacity-50"
                >
                  {savingStartDate ? t('tournaments.saving') : t('common.save')}
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
  const { t } = useTranslation()
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
          {entry.fullName ?? entry.username} {isMe && <span className="text-xs font-normal">({t('tournaments.you')})</span>}
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
