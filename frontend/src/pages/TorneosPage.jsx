import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Plus, Users, Zap, Calendar } from 'lucide-react'
import { api } from '../services/api'
import { useTournamentStore } from '../store/tournamentStore'

const MAX_DESC = 150

function todayStr() {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

export default function TorneosPage() {
  const { t } = useTranslation()
  const { tournaments, setTournaments } = useTournamentStore()
  const [loading, setLoading] = useState(true)
  const [showJoin, setShowJoin] = useState(false)
  const [showCreate, setShowCreate] = useState(false)
  const [joinCode, setJoinCode] = useState('')
  const [createName, setCreateName] = useState('')
  const [createDesc, setCreateDesc] = useState('')
  const [createStartingDate, setCreateStartingDate] = useState(todayStr)
  const [error, setError] = useState('')
  const navigate = useNavigate()

  useEffect(() => {
    api.getTournaments().then(setTournaments).finally(() => setLoading(false))
  }, [])

  async function handleCreate(e) {
    e.preventDefault()
    setError('')
    try {
      const tn = await api.createTournament({
        name: createName,
        description: createDesc.trim() || null,
        startingMatchDate: createStartingDate !== todayStr() ? createStartingDate : undefined,
      })
      setTournaments([...tournaments, tn])
      setShowCreate(false)
      setCreateName('')
      setCreateDesc('')
      setCreateStartingDate(todayStr())
      window.fbq?.('trackCustom', 'JoinTournament')
      navigate(`/tournaments/${tn.id}`)
    } catch (err) { setError(err.message) }
  }

  async function handleJoin(e) {
    e.preventDefault()
    setError('')
    try {
      const tn = await api.joinTournament({ codeOrInviteLink: joinCode.trim() })
      setTournaments([...tournaments, tn])
      setShowJoin(false)
      setJoinCode('')
      window.fbq?.('trackCustom', 'JoinTournament')
      navigate(`/tournaments/${tn.id}`)
    } catch (err) { setError(err.message) }
  }

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D] pb-4">
      {/* Header */}
      <div className="px-5 pt-12 md:pt-6 pb-5 bg-gradient-to-b from-[#1A1A2E] to-[#0D0D0D]">
        <h2 className="text-2xl font-bold text-white mb-5">{t('tournaments.myTournaments')}</h2>

        <div className="flex gap-3">
          <button
            onClick={() => { setShowCreate(true); setError('') }}
            className="flex-1 flex items-center justify-center gap-2 py-3 rounded-xl bg-[#00FF87] text-black font-bold text-sm active:scale-95 transition-transform"
          >
            <Plus size={18} /> {t('tournaments.createTournament')}
          </button>
          <button
            onClick={() => { setShowJoin(true); setError('') }}
            className="flex-1 flex items-center justify-center gap-2 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white font-semibold text-sm active:scale-95 transition-transform"
          >
            <Users size={18} /> {t('tournaments.join')}
          </button>
        </div>
      </div>

      {/* Lista de torneos */}
      <div className="px-5 flex-1 mt-5">

        {loading ? (
          <div className="flex flex-col gap-3">
            {[1, 2].map((i) => (
              <div key={i} className="h-24 rounded-2xl bg-[#1A1A2E] animate-pulse" />
            ))}
          </div>
        ) : tournaments.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
            <Zap size={40} className="text-[#2A2A3E]" />
            <p className="text-[#8A8A9A] text-sm">{t('tournaments.noTournaments')}<br />{t('tournaments.noTournamentsHint')}</p>
          </div>
        ) : (
          <div className="flex flex-col gap-3">
            {tournaments.map((tn) => (
              <button
                key={tn.id}
                onClick={() => navigate(`/tournaments/${tn.id}`)}
                className="w-full text-left p-4 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] active:border-[#00FF87] transition-colors"
              >
                <div className="flex items-start justify-between">
                  <div>
                    <p className="font-bold text-white text-base">{tn.name}</p>
                    <p className="text-[#8A8A9A] text-xs mt-0.5">{tn.participantCount} {t('common.participants')}</p>
                  </div>
                  <span className="text-xs bg-[#0D0D0D] px-2 py-1 rounded-lg font-mono text-[#00FF87] tracking-widest">
                    {tn.code}
                  </span>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Modal: Crear torneo */}
      {showCreate && (
        <Modal title={t('tournaments.createTournament')} onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate} className="flex flex-col gap-4">
            <input
              type="text"
              placeholder={t('tournaments.tournamentName')}
              value={createName}
              onChange={(e) => setCreateName(e.target.value)}
              required minLength={3} maxLength={100}
              autoFocus
              className="w-full px-4 py-3 rounded-xl bg-[#0D0D0D] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87]"
            />
            <div className="flex flex-col gap-1">
              <textarea
                placeholder={t('tournaments.tournamentDesc')}
                value={createDesc}
                onChange={(e) => setCreateDesc(e.target.value.slice(0, MAX_DESC))}
                rows={3}
                className="w-full px-4 py-3 rounded-xl bg-[#0D0D0D] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] resize-none text-sm"
              />
              <p className="text-right text-xs text-[#8A8A9A] pr-1">{createDesc.length}/{MAX_DESC}</p>
            </div>

            <div className="flex flex-col gap-1 p-3 rounded-xl bg-[#0D0D0D] border border-[#2A2A3E]">
              <label className="flex items-center gap-1.5 text-sm font-bold text-white">
                <Calendar size={15} className="text-[#00FF87]" />
                {t('tournaments.startDate')}
              </label>
              <p className="text-xs text-[#8A8A9A] mb-1">
                {t('tournaments.startDateHint')}
              </p>
              <input
                type="date"
                value={createStartingDate}
                onChange={(e) => setCreateStartingDate(e.target.value)}
                className="w-full px-4 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white focus:outline-none focus:border-[#00FF87] text-sm"
              />
            </div>

            {error && <p className="text-red-400 text-sm">{error}</p>}
            <button type="submit" className="w-full py-3 rounded-xl bg-[#00FF87] text-black font-bold">{t('common.create')}</button>
          </form>
        </Modal>
      )}

      {/* Modal: Unirse */}
      {showJoin && (
        <Modal title={t('tournaments.joinTournament')} onClose={() => setShowJoin(false)}>
          <form onSubmit={handleJoin} className="flex flex-col gap-4">
            <input
              type="text"
              placeholder={t('tournaments.joinCodePlaceholder')}
              value={joinCode}
              onChange={(e) => setJoinCode(e.target.value)}
              required
              autoFocus
              className="w-full px-4 py-3 rounded-xl bg-[#0D0D0D] border border-[#2A2A3E] text-white placeholder-[#8A8A9A] focus:outline-none focus:border-[#00FF87] font-mono tracking-widest uppercase"
            />
            {error && <p className="text-red-400 text-sm">{error}</p>}
            <button type="submit" className="w-full py-3 rounded-xl bg-[#00FF87] text-black font-bold">{t('tournaments.join')}</button>
          </form>
        </Modal>
      )}
    </div>
  )
}

function Modal({ title, onClose, children }) {
  return (
    <div className="fixed inset-0 z-[60] flex items-end" onClick={onClose}>
      <div
        className="w-full max-w-[480px] mx-auto bg-[#1A1A2E] rounded-t-3xl p-6 pb-[calc(2.5rem+env(safe-area-inset-bottom))] flex flex-col gap-4"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-bold text-white">{title}</h3>
          <button onClick={onClose} className="text-[#8A8A9A] text-2xl leading-none">×</button>
        </div>
        {children}
      </div>
    </div>
  )
}
