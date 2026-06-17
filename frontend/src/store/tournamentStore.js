import { create } from 'zustand'

export const useTournamentStore = create((set, get) => ({
  tournaments: [],
  currentTournament: null,
  leaderboard: [],
  matches: [],

  setTournaments: (tournaments) => set({ tournaments }),
  setCurrentTournament: (t) => set({ currentTournament: t }),
  setLeaderboard: (leaderboard) => set({ leaderboard }),
  setMatches: (matches) => set({ matches }),

  updateMatchLive: (update) => {
    set((state) => ({
      matches: state.matches.map((m) =>
        m.id === update.matchId
          ? {
              ...m,
              homeScore: update.homeScore,
              awayScore: update.awayScore,
              status: update.status,
              minute: update.minute ?? m.minute,
              goals: update.goals !== undefined ? update.goals : m.goals,
              livePhase: update.livePhase !== undefined ? update.livePhase : m.livePhase,
              minuteDisplay: update.minuteDisplay !== undefined ? update.minuteDisplay : m.minuteDisplay,
            }
          : m
      ),
    }))
  },
}))
