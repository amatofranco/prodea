import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate',
      devOptions: { enabled: true },

      manifest: {
        name: 'Prodeá — Mundial 2026',
        short_name: 'Prodeá',
        description: 'El prode del Mundial 2026 con tus amigos',
        theme_color: '#0D0D0D',
        background_color: '#0D0D0D',
        display: 'standalone',
        orientation: 'portrait',
        start_url: '/',
        scope: '/',
        lang: 'es',
        icons: [
          { src: 'pwa-192x192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'any maskable' },
        ],
        screenshots: [
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            form_factor: 'narrow',
            label: 'Prodeá — Fixture',
          },
        ],
      },

      workbox: {
        // Precachea todos los assets del build (JS, CSS, HTML)
        globPatterns: ['**/*.{js,css,html,svg,png,ico,woff,woff2}'],

        runtimeCaching: [
          // Fuentes de Google: caché larga, network-first la primera vez
          {
            urlPattern: /^https:\/\/fonts\.(googleapis|gstatic)\.com\/.*/i,
            handler: 'CacheFirst',
            options: {
              cacheName: 'google-fonts',
              expiration: { maxEntries: 10, maxAgeSeconds: 60 * 60 * 24 * 365 },
              cacheableResponse: { statuses: [0, 200] },
            },
          },

          // Fixture y partidos: NetworkFirst con fallback a caché
          // Si no hay internet, muestra los datos que tenía guardados
          {
            urlPattern: /\/api\/tournaments\/\d+\/matches/,
            handler: 'NetworkFirst',
            options: {
              cacheName: 'matches-cache',
              expiration: { maxEntries: 50, maxAgeSeconds: 60 * 10 }, // 10 min
              networkTimeoutSeconds: 5,
              cacheableResponse: { statuses: [200] },
            },
          },

          // Tabla de posiciones: NetworkFirst con fallback
          {
            urlPattern: /\/api\/tournaments\/\d+\/leaderboard/,
            handler: 'NetworkFirst',
            options: {
              cacheName: 'leaderboard-cache',
              expiration: { maxEntries: 20, maxAgeSeconds: 60 * 5 }, // 5 min
              networkTimeoutSeconds: 5,
              cacheableResponse: { statuses: [200] },
            },
          },

          // Lista de torneos del usuario: NetworkFirst
          {
            urlPattern: /\/api\/tournaments$/,
            handler: 'NetworkFirst',
            options: {
              cacheName: 'tournaments-cache',
              expiration: { maxEntries: 10, maxAgeSeconds: 60 * 60 }, // 1 hora
              networkTimeoutSeconds: 5,
              cacheableResponse: { statuses: [200] },
            },
          },

          // Perfil: StaleWhileRevalidate (muestra lo que tiene y actualiza en segundo plano)
          {
            urlPattern: /\/api\/tournaments\/\d+\/profile\/.*/,
            handler: 'StaleWhileRevalidate',
            options: {
              cacheName: 'profile-cache',
              expiration: { maxEntries: 30, maxAgeSeconds: 60 * 30 },
              cacheableResponse: { statuses: [200] },
            },
          },
        ],
      },
    }),
  ],

  server: {
    proxy: {
      '/api': 'http://localhost:5000',
      '/hubs': { target: 'http://localhost:5000', ws: true },
    },
  },
})
