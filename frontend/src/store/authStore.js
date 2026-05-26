import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export const useAuthStore = create(
  persist(
    (set) => ({
      token: null,
      user: null,
      setAuth: (token, user) => {
        localStorage.setItem('prodea_token', token)
        set({ token, user })
      },
      logout: () => {
        localStorage.removeItem('prodea_token')
        set({ token: null, user: null })
      },
    }),
    { name: 'prodea-auth', partialize: (s) => ({ token: s.token, user: s.user }) }
  )
)
