import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import UpdateBanner from './components/UpdateBanner.jsx'
import OfflineBanner from './components/OfflineBanner.jsx'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <OfflineBanner />
    <App />
    <UpdateBanner />
  </StrictMode>,
)
