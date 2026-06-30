import { useEffect, useState } from 'react'
import { Trophy } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'

const MEDAL = { 1: '🥇', 2: '🥈', 3: '🥉' }

export default function RankingPage() {
  const { t } = useTranslation()
  const [ranking, setRanking] = useState([])
  const [loading, setLoading] = useState(true)
  const currentUserId = useAuthStore((s) => s.user?.id)

  useEffect(() => {
    api.getGlobalRanking().then(setRanking).finally(() => setLoading(false))
  }, [])

  const myEntry = ranking.find((e) => e.userId === currentUserId)

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D]">
      {/* Header */}
      <div className="px-5 pt-12 md:pt-6 pb-5 bg-gradient-to-b from-[#1A1A2E] to-[#0D0D0D]">
        <div className="flex items-center gap-3 mb-1">
          <Trophy size={22} className="text-[#F59E0B]" />
          <h2 className="text-2xl font-bold text-white">{t('ranking.title')}</h2>
        </div>
        <p className="text-[#8A8A9A] text-xs">{t('ranking.subtitle')}</p>
      </div>

      {/* Mi posición sticky */}
      {myEntry && !loading && (
        <div className="mx-5 mb-3 p-3 rounded-2xl bg-[#00FF87]/10 border border-[#00FF87]/30 flex items-center gap-3">
          <span className="text-lg font-black text-[#00FF87] w-8 text-center">#{myEntry.rank}</span>
          <div className="flex-1 min-w-0">
            <p className="text-white font-bold text-sm truncate">{myEntry.fullName ?? myEntry.username}</p>
            <p className="text-[#8A8A9A] text-xs">{t('ranking.yourPosition')}</p>
          </div>
          <span className="text-[#00FF87] font-black text-lg">{myEntry.totalPoints} pts</span>
        </div>
      )}

      {/* Lista */}
      <div className="px-5 flex flex-col gap-2 pb-4">
        {loading ? (
          Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-14 rounded-2xl bg-[#1A1A2E] animate-pulse" />
          ))
        ) : ranking.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 gap-3 text-center">
            <Trophy size={40} className="text-[#2A2A3E]" />
            <p className="text-[#8A8A9A] text-sm">{t('ranking.noData')}</p>
          </div>
        ) : (
          ranking.map((entry) => {
            const isMe = entry.userId === currentUserId
            const medal = MEDAL[entry.rank]
            const initials = (entry.fullName ?? entry.username).slice(0, 2).toUpperCase()

            return (
              <div
                key={entry.userId}
                className={`flex items-center gap-3 px-4 py-3 rounded-2xl border transition-colors ${
                  isMe
                    ? 'bg-[#00FF87]/10 border-[#00FF87]/30'
                    : 'bg-[#1A1A2E] border-[#2A2A3E]'
                }`}
              >
                {/* Rank */}
                <div className="w-8 flex-shrink-0 text-center">
                  {medal ? (
                    <span className="text-xl">{medal}</span>
                  ) : (
                    <span className={`text-sm font-bold ${isMe ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
                      {entry.rank}
                    </span>
                  )}
                </div>

                {/* Avatar */}
                <div className={`w-9 h-9 rounded-full flex-shrink-0 flex items-center justify-center text-sm font-bold ${
                  isMe ? 'bg-[#00FF87] text-black' : 'bg-[#2A2A3E] text-white'
                }`}>
                  {entry.avatarUrl
                    ? <img src={entry.avatarUrl} className="w-full h-full rounded-full object-cover" alt="" />
                    : initials}
                </div>

                {/* Nombre */}
                <div className="flex-1 min-w-0">
                  <p className={`font-semibold text-sm truncate ${isMe ? 'text-[#00FF87]' : 'text-white'}`}>
                    {entry.fullName ?? entry.username}
                    {isMe && <span className="text-[10px] text-[#00FF87]/70 ml-1">({t('tournaments.you')})</span>}
                  </p>
                  {entry.fullName && (
                    <p className="text-[#8A8A9A] text-[10px] truncate">@{entry.username}</p>
                  )}
                </div>

                {/* Puntos */}
                <span className={`font-black text-base flex-shrink-0 ${isMe ? 'text-[#00FF87]' : 'text-white'}`}>
                  {entry.totalPoints} pts
                </span>
              </div>
            )
          })
        )}
      </div>
    </div>
  )
}
