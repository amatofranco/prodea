import { useEffect, useState, useCallback } from 'react'
import { Trophy, Lock, Search, X, ChevronRight } from 'lucide-react'
import { api } from '../services/api'
import { getTeam, getFlagUrl } from '../data/teamsData'

function FlagImg({ country, size = 28 }) {
  const { flag } = getTeam(country)
  const url = getFlagUrl(flag)
  return (
    <div
      className="rounded overflow-hidden bg-[#2A2A3E] shrink-0"
      style={{ width: size, height: Math.round(size * 0.67) }}
    >
      {url
        ? <img src={url} alt={country} className="w-full h-full object-cover" />
        : <span className="text-[10px] text-[#8A8A9A]">?</span>
      }
    </div>
  )
}

function Countdown({ lockTime }) {
  const [diff, setDiff] = useState(new Date(lockTime) - Date.now())

  useEffect(() => {
    const id = setInterval(() => setDiff(new Date(lockTime) - Date.now()), 1000)
    return () => clearInterval(id)
  }, [lockTime])

  if (diff <= 0) return null

  const h = Math.floor(diff / 3600000)
  const m = Math.floor((diff % 3600000) / 60000)
  const s = Math.floor((diff % 60000) / 1000)

  if (h > 48) {
    const d = Math.floor(h / 24)
    return <span>Cierra en {d}d {h % 24}h</span>
  }
  if (h > 0) return <span>Cierra en {h}h {m}m</span>
  return <span>Cierra en {m}m {s}s</span>
}

function TeamPickerModal({ teams, onSelect, onClose }) {
  const [query, setQuery] = useState('')
  const filtered = teams.filter((t) =>
    t.toLowerCase().includes(query.toLowerCase())
  )

  return (
    <div className="fixed inset-0 z-50 flex flex-col bg-[#0D0D0D]">
      {/* Header */}
      <div className="flex items-center gap-3 px-4 pt-12 pb-3 border-b border-[#2A2A3E]">
        <button onClick={onClose} className="text-[#8A8A9A] active:text-white">
          <X size={22} />
        </button>
        <h2 className="text-white font-bold flex-1">Elegir campeón</h2>
      </div>

      {/* Search */}
      <div className="px-4 py-3">
        <div className="flex items-center gap-2 bg-[#1A1A2E] rounded-xl px-3 py-2 border border-[#2A2A3E]">
          <Search size={16} className="text-[#8A8A9A] shrink-0" />
          <input
            autoFocus
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Buscar selección..."
            className="bg-transparent text-white text-sm outline-none flex-1 placeholder:text-[#8A8A9A]"
          />
        </div>
      </div>

      {/* Team list */}
      <div className="flex-1 overflow-y-auto px-4 pb-8">
        {filtered.map((team) => (
          <button
            key={team}
            onClick={() => onSelect(team)}
            className="w-full flex items-center gap-3 py-3 border-b border-[#1A1A2E] active:bg-[#1A1A2E] rounded-lg px-2 transition-colors"
          >
            <FlagImg country={team} size={36} />
            <span className="text-white text-sm font-medium flex-1 text-left">{team}</span>
            <ChevronRight size={16} className="text-[#8A8A9A]" />
          </button>
        ))}
        {filtered.length === 0 && (
          <p className="text-[#8A8A9A] text-sm text-center py-8">Sin resultados</p>
        )}
      </div>
    </div>
  )
}

export default function ChampionPickBanner({ tournamentId, currentUserId }) {
  const [status, setStatus] = useState(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [showPicker, setShowPicker] = useState(false)

  const fetchStatus = useCallback(() => {
    api.getChampionPick(tournamentId)
      .then(setStatus)
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [tournamentId])

  useEffect(() => { fetchStatus() }, [fetchStatus])

  async function handleSelect(countryName) {
    setShowPicker(false)
    setSaving(true)
    try {
      await api.submitChampionPick(tournamentId, countryName)
      await fetchStatus()
    } catch (err) {
      console.error(err)
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return <div className="mx-4 h-20 rounded-2xl bg-[#1A1A2E] animate-pulse mb-2" />
  }
  if (!status) return null

  const { myPick, isLocked, lockTime, champion, allPicks, availableTeams } = status

  return (
    <>
      {showPicker && (
        <TeamPickerModal
          teams={availableTeams}
          onSelect={handleSelect}
          onClose={() => setShowPicker(false)}
        />
      )}

      <div className="mx-4 mb-3 rounded-2xl border border-[#F59E0B]/30 bg-[#1A1A2E] overflow-hidden">
        {/* Title row */}
        <div className="flex items-center gap-2 px-4 pt-3 pb-2">
          <Trophy size={16} className="text-[#F59E0B] shrink-0" />
          <span className="text-white font-bold text-sm flex-1">Campeón del Mundial</span>
          {isLocked && (
            <span className="flex items-center gap-1 text-[10px] text-[#8A8A9A] font-semibold uppercase">
              <Lock size={10} /> Cerrado
            </span>
          )}
        </div>

        {/* ── NOT LOCKED ── */}
        {!isLocked && (
          <div className="px-4 pb-3">
            {myPick ? (
              <div className="flex items-center gap-3">
                <FlagImg country={myPick} size={32} />
                <div className="flex-1 min-w-0">
                  <p className="text-white font-semibold text-sm truncate">{myPick}</p>
                  <p className="text-[#8A8A9A] text-[10px]">
                    <Countdown lockTime={lockTime} />
                  </p>
                </div>
                <button
                  onClick={() => setShowPicker(true)}
                  disabled={saving}
                  className="text-[#00FF87] text-xs font-semibold shrink-0"
                >
                  Cambiar
                </button>
              </div>
            ) : (
              <div className="flex items-center gap-3">
                <div className="flex-1 min-w-0">
                  <p className="text-[#8A8A9A] text-xs">
                    <Countdown lockTime={lockTime} />
                  </p>
                </div>
                <button
                  onClick={() => setShowPicker(true)}
                  disabled={saving}
                  className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-[#F59E0B] text-black text-xs font-bold active:opacity-80 shrink-0"
                >
                  {saving ? 'Guardando...' : '🏆 Elegir'}
                </button>
              </div>
            )}
          </div>
        )}

        {/* ── LOCKED: all picks grid ── */}
        {isLocked && (
          <div className="px-4 pb-3">
            {champion && (
              <div className="flex items-center gap-2 mb-3 px-3 py-2 rounded-xl bg-[#F59E0B]/10 border border-[#F59E0B]/30">
                <FlagImg country={champion} size={28} />
                <div>
                  <p className="text-[#F59E0B] font-bold text-xs">¡Campeón del mundo!</p>
                  <p className="text-white font-semibold text-sm">{champion}</p>
                </div>
              </div>
            )}

            <div className="grid grid-cols-3 gap-2">
              {allPicks.map((p) => {
                const isMe = p.userId === currentUserId
                const hit = p.correctPick
                return (
                  <div
                    key={p.userId}
                    className={`flex flex-col items-center gap-1.5 p-2 rounded-xl border transition-colors ${
                      hit
                        ? 'bg-[#00FF87]/10 border-[#00FF87]/40'
                        : isMe
                        ? 'bg-[#F59E0B]/5 border-[#F59E0B]/30'
                        : 'bg-[#0D0D0D] border-[#2A2A3E]'
                    }`}
                  >
                    <div className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold ${
                      isMe ? 'bg-[#F59E0B] text-black' : 'bg-[#2A2A3E] text-white'
                    }`}>
                      {p.username[0].toUpperCase()}
                    </div>
                    <p className="text-[9px] text-[#8A8A9A] truncate w-full text-center">{p.username}</p>
                    {p.countryName ? (
                      <>
                        <FlagImg country={p.countryName} size={32} />
                        <p className={`text-[9px] font-semibold truncate w-full text-center ${hit ? 'text-[#00FF87]' : 'text-white'}`}>
                          {p.countryName}
                        </p>
                        {hit && <span className="text-[9px] text-[#00FF87] font-bold">+10 pts</span>}
                      </>
                    ) : (
                      <p className="text-[9px] text-[#8A8A9A] italic">Sin pick</p>
                    )}
                  </div>
                )
              })}
            </div>
          </div>
        )}
      </div>
    </>
  )
}
