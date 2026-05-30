const BASE = import.meta.env.VITE_API_URL
  ? `${import.meta.env.VITE_API_URL}/api`
  : '/api'

function getToken() {
  return localStorage.getItem('prodea_token')
}

async function request(path, options = {}) {
  const token = getToken()
  const res = await fetch(`${BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
    ...options,
  })

  if (res.status === 204) return null

  const data = await res.json().catch(() => null)

  if (!res.ok) {
    const message = data?.message || `Error ${res.status}`
    throw new Error(message)
  }

  return data
}

export const api = {
  register: (body) => request('/auth/register', { method: 'POST', body: JSON.stringify(body) }),
  login: (body) => request('/auth/login', { method: 'POST', body: JSON.stringify(body) }),
  googleLogin: (idToken) => request('/auth/google', { method: 'POST', body: JSON.stringify({ idToken }) }),
  forgotPassword: (email) => request('/auth/forgot-password', { method: 'POST', body: JSON.stringify({ email }) }),
  resetPassword: (token, newPassword) => request('/auth/reset-password', { method: 'POST', body: JSON.stringify({ token, newPassword }) }),

  getTournaments: () => request('/tournaments'),
  getTournament: (id) => request(`/tournaments/${id}`),
  createTournament: (body) => request('/tournaments', { method: 'POST', body: JSON.stringify(body) }),
  joinTournament: (body) => request('/tournaments/join', { method: 'POST', body: JSON.stringify(body) }),
  getLeaderboard: (id) => request(`/tournaments/${id}/leaderboard`),

  getMatches: (tournamentId) => request(`/tournaments/${tournamentId}/matches`),
  updateResult: (tournamentId, matchId, body) =>
    request(`/tournaments/${tournamentId}/matches/${matchId}/result`, { method: 'POST', body: JSON.stringify(body) }),

  getMyPredictions: () => request('/predictions'),
  submitPrediction: (matchId, body) =>
    request(`/predictions/${matchId}`, { method: 'POST', body: JSON.stringify(body) }),

  getProfile: (tournamentId, userId) => request(`/tournaments/${tournamentId}/profile/${userId}`),

  getChampionPick: (tournamentId) => request(`/tournaments/${tournamentId}/champion-pick`),
  submitChampionPick: (tournamentId, countryName) =>
    request(`/tournaments/${tournamentId}/champion-pick`, { method: 'POST', body: JSON.stringify({ countryName }) }),

  getPollingStatus: () => request('/admin/polling-status'),
}
