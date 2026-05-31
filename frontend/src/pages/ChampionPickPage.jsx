import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft, Search, Lock, Check } from 'lucide-react'
import { api } from '../services/api'
import { getTeam, getFlagUrl } from '../data/teamsData'

function FlagImg({ country, size = 40 }) {
  const { flag } = getTeam(country)
  const url = getFlagUrl(flag)
  return (
    <div className="rounded-lg overflow-hidden bg-[#2A2A3E] shrink-0" style={{ width: size, height: Math.round(size * 0.67) }}>
      {url
        ? <img src={url} alt={country} className="w-full h-full object-cover opacity-90" />
        : <div className="w-full h-full flex items-center justify-center text-[#8A8A9A] text-xs">?</div>
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

  if (diff <= 0) return <span className="text-red-400 font-semibold text-xs">Cerrado</span>
  const h = Math.floor(diff / 3600000)
  const m = Math.floor((diff % 3600000) / 60000)
  const s = Math.floor((diff % 60000) / 1000)
  const isUrgent = diff < 30 * 60 * 1000
  const label = h > 48
    ? `Cierra en ${Math.floor(h / 24)}d ${h % 24}h`
    : h > 0 ? `Cierra en ${h}h ${m}m`
    : `Cierra en ${m}m ${s}s`
  return <span className={`text-xs font-semibold ${isUrgent ? 'text-[#FF6B35]' : 'text-[#8A8A9A]'}`}>{label}</span>
}

export default function ChampionPickPage() {
  const navigate = useNavigate()
  const [status, setStatus] = useState(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [query, setQuery] = useState('')
  const [changing, setChanging] = useState(false)

  useEffect(() => {
    api.getChampionPick()
      .then(setStatus)
      .finally(() => setLoading(false))
  }, [])

  async function handleSelect(countryName) {
    setSaving(true)
    setSaved(false)
    try {
      await api.submitChampionPick(countryName)
      const updated = await api.getChampionPick()
      setStatus(updated)
      setSaved(true)
      setChanging(false)
      setQuery('')
      setTimeout(() => setSaved(false), 2500)
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <div className="flex flex-col min-h-full bg-[#0D0D0D]">
        <div className="h-24 bg-[#1A1A2E]" />
        <div className="p-4 flex flex-col gap-3">
          {[1, 2, 3].map(i => <div key={i} className="h-14 rounded-2xl bg-[#1A1A2E] animate-pulse" />)}
        </div>
      </div>
    )
  }

  const { myPick, isLocked, lockTime, availableTeams, champion } = status ?? {}
  const filtered = (availableTeams ?? []).filter(t => t.toLowerCase().includes(query.toLowerCase()))
  const showPicker = !isLocked && (!myPick || changing)

  return (
    <div className="flex flex-col min-h-full bg-[#0D0D0D]">
      {/* Header */}
      <div className="px-4 pt-12 pb-4 bg-[#1A1A2E]">
        <div className="flex items-center gap-3">
          <button onClick={() => navigate(-1)} className="text-[#8A8A9A] active:text-white">
            <ChevronLeft size={24} />
          </button>
          <div className="flex-1 min-w-0">
            <h1 className="text-xl font-bold text-white">🏆 Campeón del Mundial</h1>
            <div className="mt-0.5">
              {isLocked
                ? <span className="flex items-center gap-1 text-xs text-[#8A8A9A]"><Lock size={11} /> Elección cerrada</span>
                : <Countdown lockTime={lockTime} />
              }
            </div>
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto">

        {/* Current pick (if has one and not in change mode) */}
        {myPick && !changing && (
          <div className="mx-4 mt-4 p-4 rounded-2xl bg-[#1A1A2E] border border-[#F59E0B]/30 flex items-center gap-4">
            <FlagImg country={myPick} size={56} />
            <div className="flex-1 min-w-0">
              <p className="text-[#8A8A9A] text-xs uppercase tracking-wider mb-0.5">Tu candidato</p>
              <p className="text-white font-bold text-lg leading-tight truncate">{myPick}</p>
              {saved && (
                <p className="flex items-center gap-1 text-[#00FF87] text-xs font-semibold mt-0.5">
                  <Check size={12} /> Guardado
                </p>
              )}
            </div>
            {!isLocked && (
              <button
                onClick={() => setChanging(true)}
                className="text-[#00FF87] text-sm font-semibold shrink-0"
              >
                Cambiar
              </button>
            )}
          </div>
        )}

        {/* Locked: no pick */}
        {isLocked && !myPick && (
          <div className="mx-4 mt-4 p-4 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E] text-center">
            <p className="text-[#8A8A9A] text-sm">No elegiste un campeón antes del cierre.</p>
          </div>
        )}

        {/* Champion revealed */}
        {champion && (
          <div className="mx-4 mt-3 p-4 rounded-2xl bg-[#F59E0B]/10 border border-[#F59E0B]/40 flex items-center gap-3">
            <FlagImg country={champion} size={40} />
            <div>
              <p className="text-[#F59E0B] text-xs font-bold uppercase tracking-wider">¡Campeón del mundo!</p>
              <p className="text-white font-bold">{champion}</p>
            </div>
          </div>
        )}

        {/* Picker (no pick yet OR changing) */}
        {showPicker && (
          <div className="mt-4">
            {!myPick && (
              <p className="text-center text-[#8A8A9A] text-sm px-4 mb-3">
                Elegí el campeón del Mundial 2026
              </p>
            )}
            {/* Search */}
            <div className="mx-4 mb-3">
              <div className="flex items-center gap-2 bg-[#1A1A2E] rounded-xl px-3 py-2.5 border border-[#2A2A3E]">
                <Search size={16} className="text-[#8A8A9A] shrink-0" />
                <input
                  autoFocus
                  value={query}
                  onChange={e => setQuery(e.target.value)}
                  placeholder="Buscar selección..."
                  className="bg-transparent text-white text-sm outline-none flex-1 placeholder:text-[#8A8A9A]"
                />
              </div>
            </div>
            {/* Team list */}
            <div className="px-4 flex flex-col gap-1 pb-8">
              {filtered.map(team => (
                <button
                  key={team}
                  onClick={() => !saving && handleSelect(team)}
                  disabled={saving}
                  className={`flex items-center gap-3 px-3 py-3 rounded-xl border transition-colors active:bg-[#2A2A3E] ${
                    team === myPick
                      ? 'bg-[#F59E0B]/10 border-[#F59E0B]/40'
                      : 'bg-[#1A1A2E] border-[#1A1A2E]'
                  }`}
                >
                  <FlagImg country={team} size={36} />
                  <span className="text-white text-sm font-medium flex-1 text-left">{team}</span>
                  {team === myPick && <Check size={16} className="text-[#F59E0B] shrink-0" />}
                </button>
              ))}
              {filtered.length === 0 && (
                <p className="text-[#8A8A9A] text-sm text-center py-8">Sin resultados</p>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
