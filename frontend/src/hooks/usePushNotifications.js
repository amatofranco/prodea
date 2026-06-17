import { useEffect, useState } from 'react'
import { api } from '../services/api'

const DISMISSED_KEY = 'push_modal_dismissed_at'
const ONE_DAY_MS = 24 * 60 * 60 * 1000

function urlBase64ToUint8Array(base64String) {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const raw = atob(base64)
  return new Uint8Array([...raw].map((c) => c.charCodeAt(0)))
}

export function usePushNotifications() {
  const [permission, setPermission] = useState(
    typeof Notification !== 'undefined' ? Notification.permission : 'denied'
  )
  const [subscribed, setSubscribed] = useState(false)
  const [checked, setChecked] = useState(false)
  const [dismissed, setDismissed] = useState(() => {
    const ts = localStorage.getItem(DISMISSED_KEY)
    return ts ? Date.now() - parseInt(ts) < ONE_DAY_MS : false
  })

  useEffect(() => {
    checkSubscription()
  }, [])

  async function checkSubscription() {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
      setChecked(true)
      return
    }
    const reg = await navigator.serviceWorker.ready
    const sub = await reg.pushManager.getSubscription()
    setSubscribed(!!sub)
    setChecked(true)
  }

  async function subscribe() {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return false

    const perm = await Notification.requestPermission()
    setPermission(perm)
    if (perm !== 'granted') return false

    try {
      const { publicKey } = await api.getPushPublicKey()
      if (!publicKey) {
        console.warn('VAPID public key no configurada en el servidor')
        return false
      }
      const reg = await navigator.serviceWorker.ready
      const sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(publicKey),
      })
      const json = sub.toJSON()
      setSubscribed(true)
      try {
        await api.subscribePush({
          endpoint: json.endpoint,
          p256dh: json.keys.p256dh,
          auth: json.keys.auth,
        })
      } catch (err) {
        console.error('Error guardando suscripción en backend:', err)
      }
      return true
    } catch (err) {
      console.error('Error suscribiendo push:', err)
      return false
    }
  }

  async function unsubscribe() {
    const reg = await navigator.serviceWorker.ready
    const sub = await reg.pushManager.getSubscription()
    if (sub) {
      await api.unsubscribePush({ endpoint: sub.endpoint })
      await sub.unsubscribe()
    }
    setSubscribed(false)
  }

  function dismiss() {
    localStorage.setItem(DISMISSED_KEY, Date.now().toString())
    setDismissed(true)
  }

  const supported = 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window
  const isStandalone = window.matchMedia('(display-mode: standalone)').matches
  const showModal = checked && supported && isStandalone && !subscribed && !dismissed && permission !== 'denied'

  return { supported, isStandalone, permission, subscribed, subscribe, unsubscribe, dismiss, showModal }
}
