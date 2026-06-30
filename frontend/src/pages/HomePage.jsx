import { useEffect, useState, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Zap } from 'lucide-react'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'
import InstallBanner from '../components/InstallBanner'
import { usePushNotifications } from '../hooks/usePushNotifications'
import PushPermissionModal from '../components/PushPermissionModal'
import { joinTournament, leaveTournament, onMatchUpdated } from '../services/signalr'
import { LiveCard, FinishedCard, PlaceholderCard, UpcomingCard } from '../components/HomeMatchCards'

const LIVE_POLL_MS = 60_000
const IS_TESTING = import.meta.env.VITE_APP_ENV === 'testing'

export default function HomePage() {
  const { t } = useTranslation()
  const user = useAuthStore((s) => s.user)
  const [matches, setMatches] = useState([])
  const [tournaments, setTournaments] = useState([])
  const navigate = useNavigate()
  const pollRef = useRef(null)
  const { showModal, subscribe, dismiss } = usePushNotifications()

  useEffect(() => {
    api.getMyPredictions().then(setMatches)
    api.getTournaments().then(setTournaments).catch(() => {})
  }, [])

  useEffect(() => {
    async function refresh() {
      const updated = await api.getMyPredictions().catch(() => null)
      if (updated) setMatches(updated)
    }

    function scheduleNext() {
      pollRef.current = setTimeout(async () => {
        await refresh()
        scheduleNext()
      }, LIVE_POLL_MS)
    }

    function onVisible() {
      if (document.visibilityState === 'visible') {
        clearTimeout(pollRef.current)
        refresh().then(scheduleNext)
      }
    }
    function onPageShow(e) {
      if (e.persisted) {
        clearTimeout(pollRef.current)
        refresh().then(scheduleNext)
      }
    }

    document.addEventListener('visibilitychange', onVisible)
    window.addEventListener('pageshow', onPageShow)
    scheduleNext()
    return () => {
      clearTimeout(pollRef.current)
      document.removeEventListener('visibilitychange', onVisible)
      window.removeEventListener('pageshow', onPageShow)
    }
  }, [])

  useEffect(() => {
    if (tournaments.length === 0) return
    tournaments.forEach((t) => joinTournament(t.id).catch(() => {}))
    const unsub = onMatchUpdated((update) => {
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
      if (update.status === 'Finished') {
        setTimeout(() => api.getMyPredictions().then(setMatches).catch(() => {}), 2000)
      }
    })
    return () => {
      unsub()
      tournaments.forEach((t) => leaveTournament(t.id).catch(() => {}))
    }
  }, [tournaments])

  const today = new Date().toDateString()
  const tomorrow = new Date(new Date().getTime() + 86400000).toDateString()

  const liveMatches = matches.filter((m) => m.status === 'InProgress')
  const recentFinished = matches
    .filter((m) => m.status === 'Finished')
    .sort((a, b) => new Date(b.matchDate) - new Date(a.matchDate))
    .slice(0, 4)
  const allUpcoming = matches
    .filter((m) => m.status === 'Scheduled')
    .sort((a, b) => new Date(a.matchDate) - new Date(b.matchDate))
  const todayTomorrowMatches = allUpcoming.filter((m) => {
    const d = new Date(m.matchDate).toDateString()
    return d === today || d === tomorrow
  })
  const upcomingMatches = todayTomorrowMatches.length > 0 ? todayTomorrowMatches : allUpcoming.slice(0, 3)

  const isEmpty = matches.length === 0 && recentFinished.length === 0 && upcomingMatches.length === 0
  const isFinalFinished = matches.some(m => m.phase === 'Final' && m.status === 'Finished')

  const avatar = user?.username?.[0]?.toUpperCase() || '?'

  function handlePushActivate() {
    dismiss()
    subscribe()
  }

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D] pb-4">
      {showModal && (
        <PushPermissionModal onActivate={handlePushActivate} onDismiss={dismiss} />
      )}

      <div className="w-full bg-gradient-to-b from-[#1A1A2E] to-[#0D0D0D] px-5 md:px-16 pt-12 md:pt-8 pb-6">
        <div className="flex items-center justify-between mb-3 md:hidden">
          <div className="flex flex-col gap-1">
            <img src="/logo-wordmark.png" alt="Prodea" className="h-12 object-contain object-left -ml-[23px]" />
            {IS_TESTING && (
              <span className="ml-0.5 self-start px-2 py-0.5 rounded-full text-[10px] font-bold tracking-widest uppercase bg-[#FF6B35]/20 text-[#FF6B35] border border-[#FF6B35]/40">
                Testing
              </span>
            )}
          </div>
          <div className="w-11 h-11 rounded-full bg-[#00FF87] flex items-center justify-center text-black font-bold text-lg">
            {avatar}
          </div>
        </div>
        <div className="md:flex md:items-end md:justify-between">
          <div>
            <p className="text-[#8A8A9A] text-sm md:text-base md:tracking-widest md:uppercase md:mb-1">{t('home.hello')}</p>
            <h2 className="text-2xl md:text-3xl font-bold text-white">
              {user?.firstName ?? user?.username}
            </h2>
          </div>
          {IS_TESTING && (
            <span className="hidden md:inline px-3 py-1 rounded-full text-xs font-bold tracking-widest uppercase bg-[#FF6B35]/20 text-[#FF6B35] border border-[#FF6B35]/40">
              Testing
            </span>
          )}
        </div>
      </div>

      <div className="md:px-16 w-full">
        <InstallBanner />

        {isFinalFinished && tournaments.length > 0 && (
          <div className="px-5 md:px-0 mb-4">
            <div
              onClick={() => navigate(tournaments.length === 1 ? `/torneos/${tournaments[0].id}` : '/torneos')}
              className="flex items-center gap-3 p-4 rounded-2xl bg-gradient-to-r from-[#F59E0B]/20 to-[#FF6B35]/10 border border-[#F59E0B]/40 cursor-pointer active:opacity-80"
            >
              <img src="/trophy.svg" alt="" className="h-9 w-auto shrink-0" />
              <div className="flex-1 min-w-0">
                <p className="text-[#F59E0B] text-xs font-bold uppercase tracking-wider">{t('home.worldCupEnded')}</p>
                <p className="text-white font-semibold text-sm">{t('home.seeWinner')}</p>
              </div>
            </div>
          </div>
        )}

        {isEmpty && (
          <div className="flex flex-col items-center justify-center gap-3 py-24 px-5 text-center">
            <Zap size={40} className="text-[#2A2A3E]" />
            <p className="text-[#8A8A9A] text-sm">{t('home.noMatches')}</p>
          </div>
        )}

        <div className="px-5 md:px-0 mb-5">
          <h3 className="text-[#FF6B35] text-xs uppercase tracking-widest mb-2 font-semibold flex items-center gap-1.5">
            <span className={`w-1.5 h-1.5 rounded-full bg-[#FF6B35] ${liveMatches.length > 0 ? 'animate-pulse' : 'opacity-30'}`} />
            {t('home.live')}
          </h3>
          {liveMatches.length === 0 ? (
            <div className="p-4 rounded-2xl border border-[#FF6B35]/20 border-dashed flex items-center gap-3">
              <span className="text-xl">📡</span>
              <p className="text-sm font-semibold text-[#8A8A9A]">{t('home.noLive')}</p>
            </div>
          ) : liveMatches.length === 1
            ? liveMatches.map((m) => <LiveCard key={m.id} match={m} />)
            : (
              <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
                {liveMatches.map((m) => <LiveCard key={m.id} match={m} compact />)}
              </div>
            )
          }
        </div>

        <div className="mb-5 px-5 md:px-0">
          <h3 className="text-[#8A8A9A] text-xs uppercase tracking-widest mb-2 font-semibold">{t('home.latestResults')}</h3>
          <div
            className="md:hidden flex gap-2 overflow-x-auto pb-1 snap-x snap-mandatory"
            style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
            onWheel={(e) => { e.preventDefault(); e.currentTarget.scrollLeft += e.deltaY }}
          >
            {recentFinished.length > 0
              ? recentFinished.map((m) => (
                  <div key={m.id} className="flex-shrink-0 w-[44%] snap-start">
                    <FinishedCard match={m} />
                  </div>
                ))
              : allUpcoming.slice(0, 4).map((m) => (
                  <div key={m.id} className="flex-shrink-0 w-[44%] snap-start">
                    <PlaceholderCard match={m} />
                  </div>
                ))
            }
          </div>
          <div className="hidden md:grid grid-cols-4 gap-3">
            {recentFinished.length > 0
              ? recentFinished.map((m) => <FinishedCard key={m.id} match={m} />)
              : allUpcoming.slice(0, 4).map((m) => <PlaceholderCard key={m.id} match={m} />)
            }
          </div>
        </div>

        {upcomingMatches.length > 0 && (
          <div className="px-5 md:px-0">
            <h3 className="text-[#8A8A9A] text-xs uppercase tracking-widest mb-2 font-semibold">{t('home.upcoming')}</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
              {upcomingMatches.map((m) => <UpcomingCard key={m.id} match={m} navigate={navigate} />)}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
