const COOLDOWN_MS = 5 * 60 * 1000
const LAST_AD_KEY = 'prodea_last_ad'

export function useAdGate() {
  function shouldShowAd() {
    const last = localStorage.getItem(LAST_AD_KEY)
    if (!last) return true
    return Date.now() - parseInt(last, 10) >= COOLDOWN_MS
  }

  function recordAdShown() {
    localStorage.setItem(LAST_AD_KEY, Date.now().toString())
  }

  function shouldShowOnNthVisit(key, n) {
    const storageKey = `prodea_visit_${key}`
    const count = parseInt(localStorage.getItem(storageKey) || '0', 10) + 1
    localStorage.setItem(storageKey, count.toString())
    return count % n === 0
  }

  return { shouldShowAd, recordAdShown, shouldShowOnNthVisit }
}
