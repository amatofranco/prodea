import { useEffect, useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { api } from '../services/api'
import { useAuthStore } from '../store/authStore'
import { useTournamentStore } from '../store/tournamentStore'

export default function JoinPage() {
  const { t } = useTranslation()
  const { code } = useParams()
  const navigate = useNavigate()
  const token = useAuthStore((s) => s.token)
  const { tournaments, setTournaments } = useTournamentStore()
  const [status, setStatus] = useState('joining') // joining | error | already
  const [errorMsg, setErrorMsg] = useState('')

  useEffect(() => {
    if (!token) return

    // Ya es participante
    const existing = tournaments.find(
      (t) => t.code === code || t.inviteLink === code
    )
    if (existing) {
      navigate(`/tournaments/${existing.id}`, { replace: true })
      return
    }

    api.joinTournament({ codeOrInviteLink: code })
      .then((t) => {
        setTournaments([...tournaments, t])
        window.fbq?.('trackCustom', 'JoinTournament')
        navigate(`/tournaments/${t.id}`, { replace: true })
      })
      .catch((err) => {
        if (err.message === 'already_participant') {
          setStatus('already')
        } else {
          setStatus('error')
          setErrorMsg(t('errors.' + err.message, { ...err.data, defaultValue: t('errors.generic') }))
        }
      })
  }, [token])

  // No logueado → mandarlo al login con redirect de vuelta
  if (!token) {
    return (
      <div className="min-h-dvh flex flex-col items-center justify-center px-6 bg-[#0D0D0D] gap-6 text-center">
        <div className="flex flex-col items-center gap-0">
          <img src="/logo-icon.png" alt="Prodea" className="w-40 h-40 object-contain" />
          <img src="/logo-wordmark.png" alt="Prodea" className="h-10 object-contain -mt-3" />
        </div>
        <div className="space-y-2">
          <p className="text-white font-bold text-lg">{t('join.invited')}</p>
          <p className="text-[#8A8A9A] text-sm">{t('join.loginOrRegister')}</p>
        </div>
        <div className="flex flex-col gap-3 w-full max-w-xs">
          <Link
            to={`/login?redirect=/join/${code}`}
            className="w-full py-3 rounded-xl bg-[#00FF87] text-black font-bold text-center"
          >
            {t('join.loginBtn')}
          </Link>
          <Link
            to={`/register?redirect=/join/${code}`}
            className="w-full py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white font-semibold text-center"
          >
            {t('join.registerBtn')}
          </Link>
        </div>
      </div>
    )
  }

  if (status === 'error') {
    return (
      <div className="min-h-dvh flex flex-col items-center justify-center px-6 bg-[#0D0D0D] gap-4 text-center">
        <p className="text-red-400 font-semibold">{t('join.errorTitle')}</p>
        <p className="text-[#8A8A9A] text-sm">{errorMsg || t('join.errorDefault')}</p>
        <Link to="/tournaments" className="text-[#00FF87] font-semibold text-sm">{t('join.goToTournaments')}</Link>
      </div>
    )
  }

  if (status === 'already') {
    return (
      <div className="min-h-dvh flex flex-col items-center justify-center px-6 bg-[#0D0D0D] gap-4 text-center">
        <p className="text-white font-semibold">{t('join.alreadyIn')}</p>
        <Link to="/tournaments" className="text-[#00FF87] font-semibold text-sm">{t('join.viewTournaments')}</Link>
      </div>
    )
  }

  return (
    <div className="min-h-dvh flex flex-col items-center justify-center bg-[#0D0D0D] gap-3">
      <div className="w-8 h-8 border-2 border-[#00FF87] border-t-transparent rounded-full animate-spin" />
      <p className="text-[#8A8A9A] text-sm">{t('join.joining')}</p>
    </div>
  )
}
