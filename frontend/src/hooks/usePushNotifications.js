import { useEffect, useState } from 'react'
import { api } from '../services/api'

function urlBase64ToUint8Array(base64String) {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const raw = atob(base64)
  return new Uint8Array([...raw].map((c) => c.charCodeAt(0)))
}

export function usePushNotifications() {
  const [permission, setPermission] = useState(Notification.permission)
  const [subscribed, setSubscribed] = useState(false)

  useEffect(() => {
    checkSubscription()
  }, [])

  async function checkSubscription() {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return
    const reg = await navigator.serviceWorker.ready
    const sub = await reg.pushManager.getSubscription()
    setSubscribed(!!sub)
  }

  async function subscribe() {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return false

    const perm = await Notification.requestPermission()
    setPermission(perm)
    if (perm !== 'granted') return false

    try {
      const { publicKey } = await api.getPushPublicKey()
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

  const supported = 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window

  return { supported, permission, subscribed, subscribe, unsubscribe }
}
