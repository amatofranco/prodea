import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import UpdateBanner from './components/UpdateBanner.jsx'
import OfflineBanner from './components/OfflineBanner.jsx'

// Llegamos hasta aquí: el módulo principal cargó bien, se puede reintentar
// el auto-reload de recuperación si vuelve a fallar más adelante.
sessionStorage.removeItem('prodea-reload-once')

// Captura el evento antes de que cualquier componente monte
window.addEventListener('beforeinstallprompt', (e) => {
  e.preventDefault()
  window.__pwaInstallPrompt = e
})

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <OfflineBanner />
    <App />
    <UpdateBanner />
  </StrictMode>,
)
