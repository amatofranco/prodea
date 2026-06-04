import { Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'

export default function PrivacyPage() {
  return (
    <div className="min-h-dvh bg-[#0D0D0D] text-white px-5 py-6 max-w-2xl mx-auto">
      <div className="flex items-center gap-3 mb-8">
        <Link to="/login" className="text-[#8A8A9A] active:text-white transition-colors">
          <ArrowLeft size={20} />
        </Link>
        <h1 className="text-xl font-bold">Política de Privacidad</h1>
      </div>

      <p className="text-[#8A8A9A] text-sm mb-8">Última actualización: junio 2026</p>

      <div className="flex flex-col gap-8 text-sm leading-relaxed text-[#C0C0D0]">

        <section>
          <h2 className="text-white font-bold text-base mb-2">1. Información que recopilamos</h2>
          <p>Al registrarte en Prodea recopilamos:</p>
          <ul className="list-disc list-inside mt-2 flex flex-col gap-1 text-[#8A8A9A]">
            <li>Nombre de usuario y dirección de email</li>
            <li>Contraseña (almacenada con hash, nunca en texto plano)</li>
            <li>Nombre y apellido opcionales</li>
            <li>URL de foto de perfil (si usás Google para registrarte)</li>
          </ul>
          <p className="mt-2">También registramos las predicciones que cargás y tu actividad dentro de los torneos.</p>
        </section>

        <section>
          <h2 className="text-white font-bold text-base mb-2">2. Cómo usamos tu información</h2>
          <p>Usamos tu información exclusivamente para:</p>
          <ul className="list-disc list-inside mt-2 flex flex-col gap-1 text-[#8A8A9A]">
            <li>Identificarte dentro de la app y los torneos en los que participás</li>
            <li>Calcular tu puntaje y posición en la tabla</li>
            <li>Enviarte notificaciones push sobre resultados y motes (solo si lo autorizás)</li>
            <li>Recuperación de contraseña por email</li>
          </ul>
        </section>

        <section>
          <h2 className="text-white font-bold text-base mb-2">3. Cookies y publicidad</h2>
          <p>
            Prodea utiliza <strong className="text-white">Google AdSense</strong> para mostrar anuncios. Google puede usar cookies para personalizar los anuncios según tu historial de navegación. Esta personalización está regulada por la{' '}
            <a
              href="https://policies.google.com/privacy"
              target="_blank"
              rel="noopener noreferrer"
              className="text-[#00FF87] underline"
            >
              Política de Privacidad de Google
            </a>.
          </p>
          <p className="mt-2">
            Podés optar por no recibir anuncios personalizados visitando{' '}
            <a
              href="https://www.google.com/settings/ads"
              target="_blank"
              rel="noopener noreferrer"
              className="text-[#00FF87] underline"
            >
              Configuración de anuncios de Google
            </a>.
          </p>
          <p className="mt-2">
            También usamos <code className="text-[#00FF87] text-xs bg-[#1A1A2E] px-1 py-0.5 rounded">localStorage</code> para guardar tu sesión (token JWT) y preferencias de la app. No usamos cookies propias de seguimiento.
          </p>
        </section>

        <section>
          <h2 className="text-white font-bold text-base mb-2">4. Compartición de datos</h2>
          <p>
            No vendemos ni compartimos tu información personal con terceros, salvo lo estrictamente necesario para el funcionamiento de la app (alojamiento en la nube, autenticación con Google). Nunca vendemos tus datos.
          </p>
        </section>

        <section>
          <h2 className="text-white font-bold text-base mb-2">5. Seguridad</h2>
          <p>
            Las contraseñas se almacenan con hashing seguro (bcrypt). La comunicación entre la app y el servidor utiliza HTTPS. Tu token de sesión se guarda localmente en tu dispositivo y no se transmite innecesariamente.
          </p>
        </section>

        <section>
          <h2 className="text-white font-bold text-base mb-2">6. Tus derechos</h2>
          <p>Podés:</p>
          <ul className="list-disc list-inside mt-2 flex flex-col gap-1 text-[#8A8A9A]">
            <li>Actualizar tu nombre o email desde la configuración de la app</li>
            <li>Solicitar la eliminación de tu cuenta y todos tus datos</li>
            <li>Revocar el acceso de notificaciones push desde la configuración de tu dispositivo</li>
          </ul>
        </section>

        <section>
          <h2 className="text-white font-bold text-base mb-2">7. Contacto</h2>
          <p>
            Para solicitar la eliminación de tu cuenta o cualquier consulta sobre privacidad, escribinos a través del formulario de contacto disponible en la sección <strong className="text-white">Ajustes</strong> de la app.
          </p>
        </section>

        <section>
          <h2 className="text-white font-bold text-base mb-2">8. Cambios en esta política</h2>
          <p>
            Podemos actualizar esta política ocasionalmente. Te notificaremos de cambios relevantes dentro de la app. El uso continuado de Prodea después de los cambios implica tu aceptación.
          </p>
        </section>

      </div>

      <div className="mt-10 pb-8 text-center">
        <Link to="/login" className="text-[#8A8A9A] text-sm hover:text-white transition-colors">
          ← Volver al inicio
        </Link>
      </div>
    </div>
  )
}
