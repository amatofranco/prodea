import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import UpdateBanner from './components/UpdateBanner.jsx'
import OfflineBanner from './components/OfflineBanner.jsx'

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
