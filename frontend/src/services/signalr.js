import * as signalR from '@microsoft/signalr'

const HUB_URL = import.meta.env.VITE_API_URL
  ? `${import.meta.env.VITE_API_URL}/hubs/tournament`
  : '/hubs/tournament'

let connection = null
const joinedTournaments = new Set()

export function getConnection() {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => localStorage.getItem('prodea_token') || '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    // El ConnectionId cambia al reconectar, y los grupos de SignalR están atados
    // al ConnectionId viejo en el server — hay que volver a unirse a cada torneo.
    connection.onreconnected(async () => {
      for (const tournamentId of joinedTournaments) {
        try { await connection.invoke('JoinTournament', String(tournamentId)) } catch { /* reintentará en el próximo reconnect */ }
      }
    })
  }
  return connection
}

export async function startConnection() {
  const conn = getConnection()
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    await conn.start()
  }
  return conn
}

export async function joinTournament(tournamentId) {
  const conn = await startConnection()
  await conn.invoke('JoinTournament', String(tournamentId))
  joinedTournaments.add(String(tournamentId))
}

export async function leaveTournament(tournamentId) {
  joinedTournaments.delete(String(tournamentId))
  const conn = getConnection()
  if (conn.state === signalR.HubConnectionState.Connected) {
    await conn.invoke('LeaveTournament', String(tournamentId))
  }
}

export function onMatchUpdated(handler) {
  const conn = getConnection()
  conn.on('MatchUpdated', handler)
  return () => conn.off('MatchUpdated', handler)
}
