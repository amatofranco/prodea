import { Link } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

const STEPS = [
  {
    title: 'Armá tu torneo o sumate a uno',
    text: 'Creá un grupo con tus amigos en segundos, o unite con un código de 6 caracteres o un link de invitación que te pase alguien del grupo.',
  },
  {
    title: 'Cargá tu predicción antes de que arranque cada partido',
    text: 'Elegí el marcador exacto con un picker animado, tipo ruleta — tocás, deslizás o usás el scroll del mouse, como más cómodo te resulte. Una vez que arranca el partido, la predicción queda bloqueada.',
  },
  {
    title: 'Sumá puntos según lo bien (o mal) que la viste venir',
    text: '3 puntos si acertás el resultado exacto, 1 punto si acertás el ganador o el empate, y 0 si la pifiaste. La tabla se actualiza en vivo mientras se juega.',
  },
  {
    title: 'Recibí tu mote de la fecha y compartilo',
    text: 'Cada jornada, Prodea te asigna un personaje según cómo jugaste: "El Crack" si fuiste el mejor, "El Mufa" si fuiste el peor, "El Dormido" si te olvidaste de cargar... y varios más. Se genera automáticamente una carta tipo figurita con tu mote, lista para mandar al grupo de WhatsApp.',
  },
]

const FEATURES = [
  {
    title: 'Más que un prode: una excusa para cargar al grupo',
    text: 'A diferencia de las planillas de Excel o los grupos improvisados, Prodea le pone nombre, cara y humor a cada fecha. No solo importa quién va primero en la tabla — importa quién fue "El Payaso" que no acertó ni un resultado, o quién se ganó el título de "El Adivino" por clarividente.',
  },
  {
    title: 'Predicciones con cierre automático',
    text: 'Las predicciones se bloquean solas apenas arranca el partido, así nadie puede "ajustar" su pronóstico después de ver cómo va el resultado. Es justo para todos, y no depende de que el admin esté pendiente.',
  },
  {
    title: 'Resultados y tabla en tiempo real',
    text: 'Mientras se juega, Prodea consulta los resultados automáticamente y actualiza la tabla de posiciones al instante — sin recargar la página, sin esperar a que alguien cargue el resultado a mano (aunque el admin del torneo siempre puede corregirlo si hace falta).',
  },
  {
    title: 'Cartas para compartir, hechas para viralizarse',
    text: 'Al cerrar cada fecha, cada jugador recibe una carta estilo figurita con su mote, su frase y su posición — pensada para mandar directo a Instagram o WhatsApp y que el grupo se cague de risa (o se enoje, según el caso).',
  },
]

const FAQ = [
  {
    q: '¿Prodea es gratis?',
    a: 'Sí, totalmente gratis. Podés crear o unirte a torneos de hasta 100 participantes sin costo.',
  },
  {
    q: '¿Cómo me uno al torneo de mis amigos?',
    a: 'Pedile al admin del torneo el código de 6 caracteres o el link de invitación, y entrás directo desde la app.',
  },
  {
    q: '¿Qué pasa si no llego a cargar mi predicción a tiempo?',
    a: 'Las predicciones se cierran automáticamente cuando arranca el partido. Si no llegaste a cargar la tuya, no sumás puntos esa fecha — y probablemente te lleves el mote de "El Dormido" 😴.',
  },
  {
    q: '¿Cómo se calculan los puntos?',
    a: '3 puntos por acertar el resultado exacto, 1 punto por acertar el ganador o el empate (sin el marcador exacto), y 0 si no acertaste nada.',
  },
  {
    q: '¿Puedo jugar desde el celular?',
    a: 'Sí, Prodea es una PWA — la podés instalar directo desde el navegador en tu pantalla de inicio, como si fuera una app nativa, sin pasar por ninguna tienda de aplicaciones.',
  },
  {
    q: '¿Qué son los "motes"?',
    a: 'Son personajes que la app le asigna a cada jugador en cada fecha, según cómo le fue: "El Crack" para el mejor puntaje, "El Mufa" para el peor, "El Francotirador" para quien clavó un resultado exacto, y varios más. Cada uno viene con su propia carta para compartir.',
  },
]

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
  const isLoggedIn = Boolean(useAuthStore((s) => s.token))

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
          Mundial 2026
        </p>

        <h1 className="text-3xl md:text-4xl font-bold leading-tight mb-3">
          El prode del Mundial 2026, jugado con tus amigos
        </h1>
        <p className="text-[#C0C0D0] text-base leading-relaxed mb-8">
          Predecí los resultados, ganá motes como <span className="text-[#00FF87] font-semibold">"El Crack"</span> o{' '}
          <span className="text-[#FF6B35] font-semibold">"El Mufa"</span>, y compartí tu carta de jugador con el grupo.
          Gratis, sin vueltas, y con la cuota justa de cargada.
        </p>

        {!isLoggedIn && (
          <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
            <Link
              to="/register"
              className="px-6 py-3 rounded-xl bg-[#00FF87] text-[#0D0D0D] font-bold text-center active:scale-95 transition-transform"
            >
              Crear mi torneo gratis
            </Link>
            <Link
              to="/login"
              className="px-6 py-3 rounded-xl bg-[#1A1A2E] border border-[#2A2A3E] text-white font-semibold text-center active:scale-95 transition-transform"
            >
              Ya tengo una cuenta
            </Link>
          </div>
        )}
      </header>

      <main className="max-w-2xl mx-auto px-6 flex flex-col gap-14 pb-16">

        {/* Cómo funciona */}
        <section>
          <SectionTitle>Cómo se juega</SectionTitle>
          <div className="flex flex-col gap-5">
            {STEPS.map((step, i) => (
              <div key={step.title} className="flex gap-4">
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

        {/* Diferenciales */}
        <section>
          <SectionTitle>Lo que hace diferente a Prodea</SectionTitle>
          <div className="flex flex-col gap-6">
            {FEATURES.map((f) => (
              <div key={f.title} className="bg-[#1A1A2E] border border-[#2A2A3E] rounded-2xl p-5">
                <h3 className="font-bold text-white mb-2">{f.title}</h3>
                <p className="text-[#8A8A9A] text-sm leading-relaxed">{f.text}</p>
              </div>
            ))}
          </div>
        </section>

        {/* Capturas */}
        <section>
          <SectionTitle>Así se juega Prodea</SectionTitle>
          <div className="flex flex-col gap-6">
            <figure>
              <img
                src="/screens/prediccion.png"
                alt="Pantalla para cargar tu predicción con un picker animado de goles"
                className="w-full max-w-[280px] mx-auto rounded-2xl border border-[#2A2A3E]"
                loading="lazy"
              />
              <figcaption className="text-[#8A8A9A] text-sm mt-2">
                <span className="text-white font-semibold">Cargá tu predicción con un picker animado</span> — elegí el
                marcador con ruedas que giran como las de una máquina tragamonedas, tocando, deslizando o con el scroll
                del mouse.
              </figcaption>
            </figure>
            <figure>
              <img
                src="/screens/tabla.png"
                alt="Tabla de posiciones del torneo actualizándose en tiempo real"
                className="w-full max-w-[280px] mx-auto rounded-2xl border border-[#2A2A3E]"
                loading="lazy"
              />
              <figcaption className="text-[#8A8A9A] text-sm mt-2">
                <span className="text-white font-semibold">Mirá la tabla actualizarse en vivo</span> — mientras se juega
                el partido, vas viendo cómo se mueve tu posición en tiempo real.
              </figcaption>
            </figure>
            <figure>
              <img
                src="/screens/carta.png"
                alt="Carta tipo figurita con el mote de la fecha de un jugador"
                className="w-full max-w-[280px] mx-auto rounded-2xl border border-[#2A2A3E]"
                loading="lazy"
              />
              <figcaption className="text-[#8A8A9A] text-sm mt-2">
                <span className="text-white font-semibold">Recibí tu mote y compartilo</span> — cada fecha te espera una
                carta nueva. ¿Vas a ser "El Crack" o "El Mufa" esta vez?
              </figcaption>
            </figure>
          </div>
        </section>

        {/* FAQ */}
        <section>
          <SectionTitle>Preguntas frecuentes</SectionTitle>
          <div className="flex flex-col gap-5">
            {FAQ.map((item) => (
              <div key={item.q}>
                <h3 className="font-bold text-white mb-1">{item.q}</h3>
                <p className="text-[#8A8A9A] text-sm leading-relaxed">{item.a}</p>
              </div>
            ))}
          </div>
        </section>

        {/* CTA final */}
        {!isLoggedIn && (
          <section className="text-center bg-[#1A1A2E] border border-[#2A2A3E] rounded-2xl p-8">
            <h2 className="text-xl font-bold mb-2">¿Listo para descubrir tu mote?</h2>
            <p className="text-[#8A8A9A] text-sm mb-5">
              Armá tu torneo, invitá a tus amigos, y que empiece la cargada.
            </p>
            <Link
              to="/register"
              className="inline-block px-6 py-3 rounded-xl bg-[#00FF87] text-[#0D0D0D] font-bold active:scale-95 transition-transform"
            >
              Crear mi torneo gratis
            </Link>
          </section>
        )}
      </main>

      {/* Footer */}
      <footer className="border-t border-[#2A2A3E] px-6 py-8 text-center">
        <p className="text-[#8A8A9A] text-xs mb-2">
          Prodea — El prode del Mundial 2026 con tus amigos
        </p>
        <div className="flex items-center justify-center gap-4 text-xs">
          <Link to="/privacidad" className="text-[#8A8A9A] hover:text-white transition-colors">
            Política de privacidad
          </Link>
          {!isLoggedIn && (
            <>
              <span className="text-[#2A2A3E]">·</span>
              <Link to="/login" className="text-[#8A8A9A] hover:text-white transition-colors">
                Iniciar sesión
              </Link>
            </>
          )}
        </div>
      </footer>
    </div>
  )
}
