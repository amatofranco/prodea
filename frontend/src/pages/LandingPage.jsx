import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuthStore } from '../store/authStore'

function SectionTitle({ children }) {
  return (
    <h2
      className="text-2xl md:text-3xl font-bold text-white mb-6 tracking-wide"
      style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}
    >
      {children}
    </h2>
  )
}

export default function LandingPage() {
  const { t } = useTranslation()
  const isLoggedIn = Boolean(useAuthStore((s) => s.token))

  const steps = [
    { title: t('landing.step1Title'), text: t('landing.step1Text') },
    { title: t('landing.step2Title'), text: t('landing.step2Text') },
    { title: t('landing.step3Title'), text: t('landing.step3Text') },
    { title: t('landing.step4Title'), text: t('landing.step4Text') },
  ]

  const features = [
    { title: t('landing.feat1Title'), text: t('landing.feat1Text') },
    { title: t('landing.feat2Title'), text: t('landing.feat2Text') },
    { title: t('landing.feat3Title'), text: t('landing.feat3Text') },
    { title: t('landing.feat4Title'), text: t('landing.feat4Text') },
  ]

  const faq = [
    { q: t('landing.faq1q'), a: t('landing.faq1a') },
    { q: t('landing.faq2q'), a: t('landing.faq2a') },
    { q: t('landing.faq3q'), a: t('landing.faq3a') },
    { q: t('landing.faq4q'), a: t('landing.faq4a') },
    { q: t('landing.faq5q'), a: t('landing.faq5a') },
    { q: t('landing.faq6q'), a: t('landing.faq6a') },
  ]

  return (
    <div className="min-h-dvh bg-[#0D0D0D] text-white">

      {/* Hero */}
      <header className="flex flex-col items-center text-center px-6 pt-12 pb-10 max-w-2xl mx-auto">
        <img src="/logo-icon.png" alt="Prodea" className="w-[140px] h-[140px] object-contain" />
        <img src="/logo-wordmark.png" alt="Prodea" className="h-[44px] object-contain -mt-3" />
        <p
          className="text-[#8A8A9A] text-sm font-semibold tracking-widest uppercase -mt-1 mb-6"
          style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}
        >
          {t('common.worldCup')}
        </p>

        <h1 className="text-3xl md:text-4xl font-bold leading-tight mb-3">
          {t('landing.heroTitle')}
        </h1>
        <p className="text-[#C0C0D0] text-base leading-relaxed mb-8">
          {t('landing.heroDesc', { defaultValue: '' }).replace(/<\/?[12]>/g, '')}
        </p>

        {!isLoggedIn && (
          <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
            <Link
              to="/register"
              className="px-6 py-3 rounded-xl bg-[#00FF87] text-[#0D0D0D] font-bold text-center active:scale-95 transition-transform"
            >
              {t('landing.createFree')}
            </Link>
            <Link
              to="/login"
              className="px-6 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white font-semibold text-center active:scale-95 transition-transform"
            >
              {t('landing.haveAccount')}
            </Link>
          </div>
        )}
      </header>

      <main className="max-w-2xl mx-auto px-6 flex flex-col gap-14 pb-16">

        <section>
          <SectionTitle>{t('landing.howToPlay')}</SectionTitle>
          <div className="flex flex-col gap-5">
            {steps.map((step, i) => (
              <div key={i} className="flex gap-4">
                <div className="shrink-0 w-9 h-9 rounded-full bg-[#1A1A2E] border border-[#2A2A3E] flex items-center justify-center font-bold text-[#00FF87]">
                  {i + 1}
                </div>
                <div>
                  <h3 className="font-bold text-white mb-1">{step.title}</h3>
                  <p className="text-[#8A8A9A] text-sm leading-relaxed">{step.text}</p>
                </div>
              </div>
            ))}
          </div>
        </section>

        <section>
          <SectionTitle>{t('landing.whatMakesDifferent')}</SectionTitle>
          <div className="flex flex-col gap-6">
            {features.map((f, i) => (
              <div key={i} className="bg-[#1A1A2E] border border-[#2A2A3E] rounded-2xl p-5">
                <h3 className="font-bold text-white mb-2">{f.title}</h3>
                <p className="text-[#8A8A9A] text-sm leading-relaxed">{f.text}</p>
              </div>
            ))}
          </div>
        </section>

        <section>
          <SectionTitle>{t('landing.howItLooks')}</SectionTitle>
          <div className="flex flex-col gap-6">
            <figure>
              <img src="/screens/prediccion.png" alt={t('landing.screen1Alt')} className="w-full max-w-[280px] mx-auto rounded-2xl border border-[#2A2A3E]" loading="lazy" />
              <figcaption className="text-[#8A8A9A] text-sm mt-2">
                <span className="text-white font-semibold">{t('landing.screen1Caption')}</span> — {t('landing.screen1Text')}
              </figcaption>
            </figure>
            <figure>
              <img src="/screens/tabla.png" alt={t('landing.screen2Alt')} className="w-full max-w-[280px] mx-auto rounded-2xl border border-[#2A2A3E]" loading="lazy" />
              <figcaption className="text-[#8A8A9A] text-sm mt-2">
                <span className="text-white font-semibold">{t('landing.screen2Caption')}</span> — {t('landing.screen2Text')}
              </figcaption>
            </figure>
            <figure>
              <img src="/screens/carta.png" alt={t('landing.screen3Alt')} className="w-full max-w-[280px] mx-auto rounded-2xl border border-[#2A2A3E]" loading="lazy" />
              <figcaption className="text-[#8A8A9A] text-sm mt-2">
                <span className="text-white font-semibold">{t('landing.screen3Caption')}</span> — {t('landing.screen3Text')}
              </figcaption>
            </figure>
          </div>
        </section>

        <section>
          <SectionTitle>{t('landing.faq')}</SectionTitle>
          <div className="flex flex-col gap-5">
            {faq.map((item, i) => (
              <div key={i}>
                <h3 className="font-bold text-white mb-1">{item.q}</h3>
                <p className="text-[#8A8A9A] text-sm leading-relaxed">{item.a}</p>
              </div>
            ))}
          </div>
        </section>

        {!isLoggedIn && (
          <section className="text-center bg-[#1A1A2E] border border-[#2A2A3E] rounded-2xl p-8">
            <h2 className="text-xl font-bold mb-2">{t('landing.ctaTitle')}</h2>
            <p className="text-[#8A8A9A] text-sm mb-5">{t('landing.ctaDesc')}</p>
            <Link
              to="/register"
              className="inline-block px-6 py-3 rounded-xl bg-[#00FF87] text-[#0D0D0D] font-bold active:scale-95 transition-transform"
            >
              {t('landing.createFree')}
            </Link>
          </section>
        )}
      </main>

      <footer className="border-t border-[#2A2A3E] px-6 py-8 text-center">
        <p className="text-[#8A8A9A] text-xs mb-2">{t('landing.footer')}</p>
        <div className="flex items-center justify-center gap-4 text-xs">
          <Link to="/privacy" className="text-[#8A8A9A] hover:text-white transition-colors">
            {t('common.privacyPolicy')}
          </Link>
          {!isLoggedIn && (
            <>
              <span className="text-[#2A2A3E]">·</span>
              <Link to="/login" className="text-[#8A8A9A] hover:text-white transition-colors">
                {t('auth.signIn')}
              </Link>
            </>
          )}
        </div>
      </footer>
    </div>
  )
}
