import * as signalR from '@microsoft/signalr'

const HUB_URL = import.meta.env.VITE_API_URL
  ? `${import.meta.env.VITE_API_URL}/hubs/tournament`
  : '/hubs/tournament'

let connection = null

export function getConnection() {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => localStorage.getItem('prodea_token') || '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()
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
}

export async function leaveTournament(tournamentId) {
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
