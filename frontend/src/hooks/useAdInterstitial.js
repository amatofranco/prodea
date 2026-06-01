const AD_LAST_SHOWN_KEY = 'ad_last_shown'
const FIXTURE_TAB_COUNT_KEY = 'ad_fixture_count'
const TABLA_TAB_COUNT_KEY = 'ad_tabla_count'
const MIN_INTERVAL_MS = 3 * 60 * 1000 // 3 min

export function useAdInterstitial(hasLiveMatch = false) {
  function canShow() {
    if (hasLiveMatch) return false
    const last = parseInt(localStorage.getItem(AD_LAST_SHOWN_KEY) || '0')
    return Date.now() - last >= MIN_INTERVAL_MS
  }

  function markShown() {
    localStorage.setItem(AD_LAST_SHOWN_KEY, Date.now().toString())
  }

  // Devuelve true cada 3 visitas al tab fixture
  function checkFixtureTab() {
    const count = parseInt(localStorage.getItem(FIXTURE_TAB_COUNT_KEY) || '0') + 1
    localStorage.setItem(FIXTURE_TAB_COUNT_KEY, count.toString())
    return count % 3 === 0 && canShow()
  }

  // Devuelve true cada 3 visitas al tab tabla
  function checkTablaTab() {
    const count = parseInt(localStorage.getItem(TABLA_TAB_COUNT_KEY) || '0') + 1
    localStorage.setItem(TABLA_TAB_COUNT_KEY, count.toString())
    return count % 3 === 0 && canShow()
  }

  return { canShow, markShown, checkFixtureTab, checkTablaTab }
}
