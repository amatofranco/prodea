import { getTeam, getFlagUrl } from '../data/teamsData'

export default function FlagImg({ name, label, size = 40 }) {
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
