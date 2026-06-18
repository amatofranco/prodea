import { cleanupOutdatedCaches, precacheAndRoute } from 'workbox-precaching'
import { registerRoute } from 'workbox-routing'
import { NetworkFirst, CacheFirst, StaleWhileRevalidate } from 'workbox-strategies'
import { ExpirationPlugin } from 'workbox-expiration'
import { CacheableResponsePlugin } from 'workbox-cacheable-response'

cleanupOutdatedCaches()
precacheAndRoute(self.__WB_MANIFEST)

self.addEventListener('install', () => self.skipWaiting())
self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()))

// index.html siempre se pide a la red primero (no se precachea) para evitar
// que quede referenciando assets hasheados de un deploy anterior que ya no existen.
registerRoute(
  ({ request }) => request.mode === 'navigate',
  new NetworkFirst({
    cacheName: 'html-cache',
    networkTimeoutSeconds: 3,
    plugins: [
      new CacheableResponsePlugin({ statuses: [200] }),
    ],
  })
)

// Fuentes de Google
registerRoute(
  /^https:\/\/fonts\.(googleapis|gstatic)\.com\/.*/i,
  new CacheFirst({
    cacheName: 'google-fonts',
    plugins: [
      new ExpirationPlugin({ maxEntries: 10, maxAgeSeconds: 60 * 60 * 24 * 365 }),
      new CacheableResponsePlugin({ statuses: [0, 200] }),
    ],
  })
)

// Fixture y partidos
registerRoute(
  /\/api\/tournaments\/\d+\/matches/,
  new NetworkFirst({
    cacheName: 'matches-cache',
    networkTimeoutSeconds: 5,
    plugins: [
      new ExpirationPlugin({ maxEntries: 50, maxAgeSeconds: 60 * 10 }),
      new CacheableResponsePlugin({ statuses: [200] }),
    ],
  })
)

// Mis predicciones (Home y pantalla de Predicciones) — NetworkFirst para siempre mostrar estado actual
registerRoute(
  /\/api\/predictions$/,
  new NetworkFirst({
    cacheName: 'predictions-cache',
    networkTimeoutSeconds: 4,
    plugins: [
      new ExpirationPlugin({ maxEntries: 10, maxAgeSeconds: 60 * 10 }),
      new CacheableResponsePlugin({ statuses: [200] }),
    ],
  })
)

// Banderas de equipos
registerRoute(
  /^https:\/\/flagcdn\.com\/.*/i,
  new CacheFirst({
    cacheName: 'flags-cache',
    plugins: [
      new ExpirationPlugin({ maxEntries: 250, maxAgeSeconds: 60 * 60 * 24 * 30 }),
      new CacheableResponsePlugin({ statuses: [0, 200] }),
    ],
  })
)

// Tabla de posiciones
registerRoute(
  /\/api\/tournaments\/\d+\/leaderboard/,
  new NetworkFirst({
    cacheName: 'leaderboard-cache',
    networkTimeoutSeconds: 5,
    plugins: [
      new ExpirationPlugin({ maxEntries: 20, maxAgeSeconds: 60 * 5 }),
      new CacheableResponsePlugin({ statuses: [200] }),
    ],
  })
)

// Lista de torneos
registerRoute(
  /\/api\/tournaments$/,
  new NetworkFirst({
    cacheName: 'tournaments-cache',
    networkTimeoutSeconds: 5,
    plugins: [
      new ExpirationPlugin({ maxEntries: 10, maxAgeSeconds: 60 * 60 }),
      new CacheableResponsePlugin({ statuses: [200] }),
    ],
  })
)

// Perfil
registerRoute(
  /\/api\/tournaments\/\d+\/profile\/.*/,
  new StaleWhileRevalidate({
    cacheName: 'profile-cache',
    plugins: [
      new ExpirationPlugin({ maxEntries: 30, maxAgeSeconds: 60 * 30 }),
      new CacheableResponsePlugin({ statuses: [200] }),
    ],
  })
)

// Push notifications
self.addEventListener('push', (event) => {
  const data = event.data?.json() ?? {}
  const title = data.title ?? 'Prodea'
  const options = {
    body: data.body ?? '',
    icon: '/pwa-192x192.png',
    badge: '/pwa-192x192.png',
    data: { url: data.url ?? '/' },
    vibrate: [200, 100, 200],
  }
  event.waitUntil(self.registration.showNotification(title, options))
})

self.addEventListener('notificationclick', (event) => {
  event.notification.close()
  const url = event.notification.data?.url ?? '/'
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
      const existing = clientList.find((c) => c.url.includes(self.location.origin))
      if (existing) {
        existing.focus()
        existing.navigate(url)
      } else {
        clients.openWindow(url)
      }
    })
  )
})
