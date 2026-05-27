import { useEffect, useState } from 'react'
import { api } from '../services/api'

export default function ApiStatusBanner({ hasLiveMatches }) {
  const [stale, setStale] = useState(false)

  useEffect(() => {
    if (!hasLiveMatches) { setStale(false); return }

    async function check() {
      try {
        const status = await api.getPollingStatus()
        setStale(status.isStale)
      } catch {
        // si falla el chequeo, no mostramos nada
      }
    }

    check()
    const interval = setInterval(check, 2 * 60 * 1000) // cada 2 minutos
    return () => clearInterval(interval)
  }, [hasLiveMatches])

  if (!stale) return null

  return (
    <div className="flex items-center gap-2 px-4 py-2 bg-yellow-500/10 border border-yellow-500/30 rounded-xl text-yellow-400 text-sm">
      <span>⚠</span>
      <span>Los marcadores pueden demorar en actualizarse</span>
    </div>
  )
}
