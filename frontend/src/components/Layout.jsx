import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { Home, Trophy, Target, LogOut } from 'lucide-react'
import { useAuthStore } from '../store/authStore'

export default function Layout() {
  const logout = useAuthStore((s) => s.logout)
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <div className="flex flex-col flex-1">
      <main className="flex-1 overflow-y-auto pb-[calc(3.5rem+env(safe-area-inset-bottom))]">
        <Outlet />
      </main>

      <nav className="fixed bottom-0 left-1/2 -translate-x-1/2 w-full max-w-[480px] bg-[#1A1A2E] border-t border-[#2A2A3E] z-50" style={{ paddingBottom: 'env(safe-area-inset-bottom)' }}>
        <div className="flex justify-around items-center h-14">
          <NavLink
            to="/"
            end
            className={({ isActive }) =>
              `flex flex-col items-center gap-0.5 px-5 py-1 text-xs transition-colors ${
                isActive ? 'text-[#00FF87]' : 'text-[#8A8A9A]'
              }`
            }
          >
            <Home size={20} />
            <span>Inicio</span>
          </NavLink>

          <NavLink
            to="/torneos"
            className={({ isActive }) =>
              `flex flex-col items-center gap-0.5 px-5 py-1 text-xs transition-colors ${
                isActive ? 'text-[#00FF87]' : 'text-[#8A8A9A]'
              }`
            }
          >
            <Trophy size={20} />
            <span>Torneos</span>
          </NavLink>

          <NavLink
            to="/predicciones"
            className={({ isActive }) =>
              `flex flex-col items-center gap-0.5 px-5 py-1 text-xs transition-colors ${
                isActive ? 'text-[#00FF87]' : 'text-[#8A8A9A]'
              }`
            }
          >
            <Target size={20} />
            <span>Predicciones</span>
          </NavLink>

          <button
            onClick={handleLogout}
            className="flex flex-col items-center gap-0.5 px-5 py-1 text-xs text-[#8A8A9A] active:text-red-400 transition-colors"
          >
            <LogOut size={20} />
            <span>Salir</span>
          </button>
        </div>
      </nav>
    </div>
  )
}
