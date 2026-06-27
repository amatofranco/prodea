import AdBanner, { adsEnabled } from './AdBanner'

export default function StickyBannerAd({ hidden = false }) {
  if (hidden || !adsEnabled()) return null

  return (
    <div
      className="md:hidden fixed left-0 right-0 z-40 flex justify-center py-1 bg-[#0D0D0D]/95"
      style={{ bottom: 'calc(3.5rem + env(safe-area-inset-bottom))' }}
    >
      <AdBanner format="banner" />
    </div>
  )
}
