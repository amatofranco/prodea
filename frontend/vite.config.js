import { defineConfig, loadEnv } from 'vite'
import { execSync } from 'node:child_process'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'

function getAppVersion() {
  // Cantidad de commits en main: sube de 1 en 1 en cada deploy. Requiere historia
  // completa de git (Vercel clona el repo entero, no en modo shallow).
  try { return execSync('git rev-list --count HEAD').toString().trim() } catch { return '?' }
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const isTesting = env.VITE_APP_ENV === 'testing'

  return {
  define: {
    __APP_VERSION__: JSON.stringify(getAppVersion()),
  },
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      strategies: 'injectManifest',
      srcDir: 'src',
      filename: 'sw.js',
      registerType: 'autoUpdate',
      devOptions: { enabled: true, type: 'module' },

      manifest: {
        name: isTesting ? 'Prodea — Testing' : 'Prodea — Mundial 2026',
        short_name: isTesting ? 'Prodea Test' : 'Prodea',
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
            label: 'Prodea — Fixture',
          },
        ],
      },

      injectManifest: {
        maximumFileSizeToCacheInBytes: 5 * 1024 * 1024,
        // index.html no se precachea: se sirve siempre con NetworkFirst (ver sw.js)
        // para evitar que quede referenciando assets hasheados de un deploy anterior.
        globPatterns: ['**/*.{js,css,svg,png,ico,woff,woff2}'],
      },
    }),
  ],

  server: {
    proxy: {
      '/api': 'http://localhost:5000',
      '/hubs': { target: 'http://localhost:5000', ws: true },
    },
  },
  }
})
