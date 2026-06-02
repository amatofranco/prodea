import { NavLink, Outlet } from 'react-router-dom'
import { Home, Trophy, Target } from 'lucide-react'

export default function Layout() {
  return (
    <div className="flex flex-col flex-1">
      <main className="flex-1 overflow-y-auto pb-[calc(3.5rem+env(safe-area-inset-bottom))]">
        <Outlet />
      </main>

      <nav className="fixed bottom-0 left-1/2 -translate-x-1/2 w-full max-w-[480px] bg-[#1A1A2E] border-t border-[#2A2A3E] z-50" style={{ paddingBottom: 'env(safe-area-inset-bottom)' }}>
        <div className="flex justify-around items-center h-14">
          <NavLink to="/" end className={({ isActive }) => `flex flex-col items-center gap-0.5 px-5 py-1 text-xs transition-colors ${isActive ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
            <Home size={20} />
            <span>Inicio</span>
          </NavLink>

          <NavLink to="/torneos" className={({ isActive }) => `flex flex-col items-center gap-0.5 px-5 py-1 text-xs transition-colors ${isActive ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
            <Trophy size={20} />
            <span>Torneos</span>
          </NavLink>

          <NavLink to="/predicciones" className={({ isActive }) => `flex flex-col items-center gap-0.5 px-5 py-1 text-xs transition-colors ${isActive ? 'text-[#00FF87]' : 'text-[#8A8A9A]'}`}>
            <Target size={20} />
            <span>Predicciones</span>
          </NavLink>

        </div>
      </nav>
    </div>
  )
}
