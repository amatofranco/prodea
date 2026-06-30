import { motion } from 'framer-motion'
import { X } from 'lucide-react'
import FlagImg from './FlagImg'

export default function MatchPredictionsSheet({ match, predictions, loading, onClose }) {
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
            <div className="flex flex-col items-center">
              <span className="text-white font-bold text-lg" style={{ fontFamily: 'Bebas Neue, sans-serif' }}>
                {match.homeScore} – {match.awayScore}
              </span>
              {match.homePenaltyScore != null && match.awayPenaltyScore != null && (
                <span className="text-[9px] text-[#F59E0B] font-semibold -mt-1">({match.homePenaltyScore}-{match.awayPenaltyScore})</span>
              )}
            </div>
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
              <div className="flex-1 min-w-0">
                <span className="text-white text-sm font-medium truncate block">{p.fullName ?? p.username}</span>
                {match.phase === 'Final' && (
                  <span className="text-[9px] text-[#8A8A9A] truncate block">
                    🏆 {p.championPick ?? 'Sin elegir'}
                  </span>
                )}
              </div>
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
              <div className="flex flex-col items-end w-16">
                <span className={`text-sm font-bold text-right ${pointColor(p.pointsEarned)}`}>
                  +{p.pointsEarned} pts
                </span>
                {match.phase === 'Final' && p.championPick && (
                  <span className={`text-[9px] font-semibold ${p.championPickPoints > 0 ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
                    +{p.championPickPoints ?? 0} camp.
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
      </motion.div>
    </div>
  )
}
