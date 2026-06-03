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

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/join/:code" element={<JoinPage />} />

        {/* Protected */}
        <Route element={<PrivateRoute />}>
          <Route element={<Layout />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/torneos" element={<TorneosPage />} />
            <Route path="/predicciones" element={<PredictionsPage />} />
            <Route path="/predicciones/:matchId" element={<PredictionPage />} />
            <Route path="/predicciones/campeon" element={<ChampionPickPage />} />
            <Route path="/torneos/:id" element={<TournamentPage />} />
            <Route path="/torneos/:tournamentId/perfil/:userId" element={<ProfilePage />} />
            <Route path="/ajustes" element={<AjustesPage />} />
          </Route>
        </Route>

        {/* Fallback */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
