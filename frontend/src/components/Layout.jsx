import { NavLink, Outlet } from 'react-router-dom'
import { Home, Trophy, Target, Settings } from 'lucide-react'
import { useAuthStore } from '../store/authStore'

const navItems = [
  { to: '/', icon: Home, label: 'Inicio', end: true },
  { to: '/predicciones', icon: Target, label: 'Predicciones' },
  { to: '/torneos', icon: Trophy, label: 'Torneos' },
  { to: '/ajustes', icon: Settings, label: 'Ajustes' },
]

export default function Layout() {
  const user = useAuthStore((s) => s.user)
  const avatar = user?.username?.[0]?.toUpperCase() || '?'

  return (
    <div className="flex min-h-dvh bg-[#0D0D0D]">

      {/* Sidebar — desktop only */}
      <aside className="hidden md:flex flex-col w-56 shrink-0 sticky top-0 h-dvh bg-[#1A1A2E] border-r border-[#2A2A3E] z-50">
        <div className="px-5 pt-6 pb-4">
          <img src="/logo-wordmark.png" alt="Prodea" className="h-10 object-contain object-left -ml-4" />
        </div>

        <nav className="flex flex-col px-3 gap-1 flex-1">
          {navItems.map(({ to, icon: Icon, label, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-semibold transition-colors ${
                  isActive
                    ? 'bg-[#00FF87]/10 text-[#00FF87]'
                    : 'text-[#8A8A9A] hover:text-white hover:bg-[#2A2A3E]'
                }`
              }
            >
              <Icon size={18} />
              {label}
            </NavLink>
          ))}
        </nav>

        <div className="px-4 py-5 border-t border-[#2A2A3E] flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-[#00FF87] flex items-center justify-center text-black font-bold text-sm shrink-0">
            {avatar}
          </div>
          <span className="text-sm text-white font-semibold truncate">{user?.firstName ?? user?.username}</span>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 min-w-0 overflow-y-auto md:pb-0 pb-[calc(3.5rem+env(safe-area-inset-bottom))]">
        <Outlet />
      </main>

      {/* Bottom nav — mobile only */}
      <nav
        className="md:hidden fixed bottom-0 left-1/2 -translate-x-1/2 w-full max-w-[480px] bg-[#1A1A2E] border-t border-[#2A2A3E] z-50"
        style={{ paddingBottom: 'env(safe-area-inset-bottom)' }}
      >
        <div className="flex justify-around items-center h-14">
          {navItems.map(({ to, icon: Icon, label, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `flex flex-col items-center gap-0.5 px-5 py-1 text-xs transition-colors ${
                  isActive ? 'text-[#00FF87]' : 'text-[#8A8A9A]'
                }`
              }
            >
              <Icon size={20} />
              <span>{label}</span>
            </NavLink>
          ))}
        </div>
      </nav>

    </div>
  )
}
