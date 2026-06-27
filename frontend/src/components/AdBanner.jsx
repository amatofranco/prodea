import { useMemo } from 'react'

const ADSTERRA_BANNER_KEY = import.meta.env.VITE_ADSTERRA_BANNER_KEY
const ADSTERRA_RECT_KEY = import.meta.env.VITE_ADSTERRA_RECT_KEY
const SHOW_AD_PLACEHOLDERS = import.meta.env.VITE_SHOW_AD_PLACEHOLDERS === 'true'

const SIZES = {
  banner: { width: 320, height: 50, key: ADSTERRA_BANNER_KEY },
  rectangle: { width: 300, height: 250, key: ADSTERRA_RECT_KEY },
}

export function adsEnabled() {
  return !!(ADSTERRA_BANNER_KEY || ADSTERRA_RECT_KEY || SHOW_AD_PLACEHOLDERS)
}

export default function AdBanner({ format = 'banner', className = '' }) {
  const { width, height, key } = SIZES[format] || SIZES.banner

  const srcDoc = useMemo(() => {
    if (!key) return null
    return `<!DOCTYPE html>
<html><head><style>body{margin:0;padding:0;overflow:hidden;background:transparent;}</style></head>
<body>
<script type="text/javascript">
atOptions={'key':'${key}','format':'iframe','height':${height},'width':${width},'params':{}};
</script>
<script src="//www.highperformanceformat.com/${key}/invoke.js"></script>
</body></html>`
  }, [key, width, height])

  if (!key && !SHOW_AD_PLACEHOLDERS) return null

  if (!key) {
    return (
      <div
        className={`flex items-center justify-center border border-dashed border-[#2A2A3E] rounded-lg bg-[#1A1A2E]/50 ${className}`}
        style={{ width, height }}
      >
        <span className="text-[10px] text-[#8A8A9A] uppercase tracking-wider">
          Ad {width}x{height}
        </span>
      </div>
    )
  }

  return (
    <iframe
      srcDoc={srcDoc}
      className={className}
      style={{ width, height, border: 'none', overflow: 'hidden' }}
      scrolling="no"
      title="Publicidad"
    />
  )
}
