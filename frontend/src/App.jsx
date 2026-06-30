import { useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/Layout'
import PrivateRoute from './components/PrivateRoute'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ForgotPasswordPage from './pages/ForgotPasswordPage'
import ResetPasswordPage from './pages/ResetPasswordPage'
import JoinPage from './pages/JoinPage'
import HomePage from './pages/HomePage'
import TorneosPage from './pages/TorneosPage'
import TournamentPage from './pages/TournamentPage'
import PredictionsPage from './pages/PredictionsPage'
import PredictionPage from './pages/PredictionPage'
import ProfilePage from './pages/ProfilePage'
import ChampionPickPage from './pages/ChampionPickPage'
import AjustesPage from './pages/AjustesPage'
import PrivacyPage from './pages/PrivacyPage'
import LandingPage from './pages/LandingPage'
import { useAuthStore } from './store/authStore'
import { api } from './services/api'

export default function App() {
  const token = useAuthStore((s) => s.token)

  useEffect(() => {
    if (!token) return
    const isStandalone = window.matchMedia('(display-mode: standalone)').matches
      || window.navigator.standalone === true
    if (isStandalone) api.markAsPwa().catch(() => {})
  }, [token])

  return (
    <BrowserRouter>
      <Routes>
        {/* Public */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/join/:code" element={<JoinPage />} />
        <Route path="/privacy" element={<PrivacyPage />} />
        <Route path="/how-to-play" element={<LandingPage />} />

        {/* Protected */}
        <Route element={<PrivateRoute />}>
          <Route element={<Layout />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/tournaments" element={<TorneosPage />} />
            <Route path="/predictions" element={<PredictionsPage />} />
            <Route path="/predictions/:matchId" element={<PredictionPage />} />
            <Route path="/predictions/champion" element={<ChampionPickPage />} />
            <Route path="/tournaments/:id" element={<TournamentPage />} />
            <Route path="/tournaments/:tournamentId/profile/:userId" element={<ProfilePage />} />
            <Route path="/settings" element={<AjustesPage />} />
          </Route>
        </Route>

        {/* Fallback */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
