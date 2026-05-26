# Prodeá — Brief para Claude Code

## Descripción general

Prodeá es una app mobile-first de prode futbolero entre amigos, pensada para el Mundial 2026. El diferencial no es solo el prode en sí, sino la capa de humor y viralidad: cada jugador recibe un mote por fecha ("El Mufa", "El Crack", etc.) que se muestra en cards compartibles estilo figurita, diseñadas para compartir en WhatsApp e Instagram.

El objetivo es que sea divertida, visualmente llamativa, y que se viralice orgánicamente entre grupos de amigos.

---

## Stack tecnológico

- **Backend:** .NET 9 — ASP.NET Core Web API
- **Base de datos:** PostgreSQL (via Npgsql + Entity Framework Core)
- **Tiempo real:** SignalR
- **Frontend:** React + Tailwind CSS (mobile-first) — PWA (Progressive Web App)
- **Hosteo backend + DB:** Railway
- **Hosteo frontend:** Vercel
- **API de fútbol:** football-data.org (tier gratuito)

---

## Funcionalidades principales

### Autenticación
- Registro y login con usuario y contraseña (JWT)
- Perfil de usuario con nombre y avatar (inicial del nombre como fallback)

### Torneos
- Crear un torneo (el creador es el admin)
- Unirse a un torneo mediante **link de invitación** o **código de 6 caracteres**
- Cada torneo tiene un nombre, una lista de participantes y un admin
- El admin puede cargar resultados manualmente (fallback si la API falla)

### Partidos y predicciones
- Fixture completo del Mundial 2026 cargado en la base de datos
- Cada usuario carga su predicción (marcador exacto) antes del inicio del partido
- Las predicciones se bloquean automáticamente al inicio del partido (kickoff)
- Si un usuario no cargó predicción antes del cierre, recibe 0 puntos y el mote "El Dormido"

### Sistema de puntuación
- **3 puntos** — resultado exacto
- **1 punto** — acertó el ganador o el empate (sin exacto)
- **0 puntos** — falló

### Resultados en tiempo real
- El backend consulta football-data.org cada **10 minutos** mientras un partido está en curso
- Se detecta el inicio y fin del partido para activar/desactivar el polling
- Los resultados se propagan via **SignalR** a todos los clientes conectados al torneo
- La tabla de posiciones se actualiza en tiempo real durante el partido
- Fallback manual: el admin del torneo puede cargar/corregir el resultado desde un panel

### Motes por fecha

Se asigna **un solo mote por jugador por fecha**, siguiendo esta jerarquía de prioridad:

| Prioridad | Mote | Condición |
|-----------|------|-----------|
| 1 | 🏆 El Crack | Mayor puntaje de la fecha |
| 2 | 💀 El Mufa | Menor puntaje de la fecha |
| 4 | 🎯 El Francotirador | Acertó al menos 1 resultado exacto |
| 3 | 🔮 El Adivino | Acertó 3 o más resultados exactos en la fecha |
| 5 | 🤡 El Payaso | No acertó ningún ganador |
| 6 | 😴 El Dormido | No cargó ninguna predicción |

**Motes acumulativos** (se muestran en el perfil, no en la card de fecha):
- 📉 **En caída libre** — 3 fechas consecutivas bajando en la tabla
- 🔥 **Racha infernal** — 3 fechas consecutivas siendo El Crack
- 🧱 **El Muro** — nunca fue último en toda la competencia
- 👻 **El Fantasma** — olvidó cargar predicciones más de 3 veces

### Cards compartibles (feature central)

Al terminar cada fecha, se genera automáticamente una **card estilo figurita de jugador** para cada participante con:
- Nombre del jugador
- Mote de la fecha con emoji y color característico
- Puntos obtenidos en la fecha
- Posición actual en la tabla del torneo
- Frase graciosa según el mote (ver listado abajo)
- Nombre del torneo y fecha
- Branding de Prodeá

**Cards especiales:**
- 🏆 **Card Campeón del Torneo** — al terminar el mundial, card especial con trofeo para el ganador del grupo
- 🔮 **Card Profecía Cumplida** — si alguien acertó el resultado exacto de una final o semifinal

Las cards se generan como imagen PNG descargable/compartible (usar `html2canvas` o similar en el frontend, o generación server-side con SkiaSharp).

**Frases por mote:**

| Mote | Frases |
|------|--------|
| 🏆 El Crack | "Clarividencia pura", "¿Sos DT o qué?", "Messi te manda saludos" |
| 💀 El Mufa | "Apostaste con el corazón, no con el cerebro", "Tus predicciones son una obra de arte... abstracto", "El VAR te hubiera dado la razón... en otro universo" |
| 🎯 El Francotirador | "Un tiro, un gol", "Cuando apuntás, no fallás", "La mira calibrada" |
| 🔮 El Adivino | "¿Bola de cristal o qué?", "Todos los ganadores. Todos.", "Brujo confirmado" |
| 🤡 El Payaso | "Ni uno. Increíble.", "El fútbol te debe una explicación", "Arte del error" |
| 😴 El Dormido | "El partido arrancó. Vos, dormido", "Gran estrategia: no jugaste", "Apareciste menos que el árbitro en el descuento" |
| 📉 En caída libre | "Lo que sube... bueno, vos solo bajás", "Tres fechas en picada. Sos un avión sin combustible" |
| 🔥 Racha infernal | "Nadie te para", "Tres fechas dominando. Humano o algoritmo, no se sabe" |
| 👻 El Fantasma | "¿Seguís en el grupo? Nadie lo sabe", "Apareciste menos que el VAR a favor tuyo" |

---

## Modelo de datos (entidades principales)

```
User
- Id, Username, Email, PasswordHash, AvatarUrl, CreatedAt

Tournament
- Id, Name, Code (6 chars), InviteLink, AdminUserId, CreatedAt

TournamentParticipant
- Id, TournamentId, UserId, JoinedAt

Match
- Id, HomeTeam, AwayTeam, MatchDate, Phase (Group/R16/QF/SF/Final)
- HomeScore (nullable), AwayScore (nullable)
- Status (Scheduled/InProgress/Finished)
- ExternalId (football-data.org id)

Prediction
- Id, UserId, TournamentId, MatchId
- PredictedHomeScore, PredictedAwayScore
- PointsEarned (calculado al terminar el partido)
- CreatedAt, UpdatedAt

MatchdayBadge (mote por fecha)
- Id, UserId, TournamentId, Matchday
- BadgeType (enum: Crack, Mufa, Francotirador, Adivino, Payaso, Dormido)
- PointsInMatchday

AccumulativeBadge
- Id, UserId, TournamentId
- BadgeType (enum: EnCaidaLibre, RachaInfernal, ElMuro, ElFantasma)
- AwardedAt
```

---

## PWA — configuración requerida

- Archivo  con nombre, íconos y colores de la app
- Service Worker para funcionamiento offline básico y cacheo
- Soporte de **push notifications** via Web Push API + VAPID keys
- Meta tags para que en Android se muestre el banner "Agregar a pantalla de inicio"
- Pantalla completa sin barra del navegador ()
- Ícono de la app en la pantalla de inicio del celu

## Pantallas del frontend

### 1. Home
- Lista de torneos activos del usuario
- Banner destacado si hay un partido en curso (con marcador en vivo)
- Botón "Crear torneo" y "Unirse a torneo"

### 2. Torneo — Tab Fixture
- Lista de partidos por fecha/fase
- Estado de cada partido (Próximo / En curso / Terminado)
- Predicción cargada por el usuario para cada partido
- Resultado real (cuando esté disponible)

### 3. Torneo — Tab Tabla
- Ranking de participantes con puntos totales
- Mote actual de cada jugador (el de la última fecha)
- Se actualiza en tiempo real via SignalR durante partidos en curso

### 4. Cargar predicción
- Escudos grandes de los equipos a cada lado
- **Picker de goles con ruedas animadas** — dos controles verticales (uno por equipo) con flechas arriba/abajo. El número entra animado desde arriba o abajo según la dirección, se siente físico y táctil
- Soporta tres formas de interacción: tocar las flechas, deslizar el dedo verticalmente sobre el número (swipe), y scroll con mouse en desktop
- Rango válido: 0 a 9 goles por equipo
- **Resumen dinámico** debajo de los pickers: muestra la predicción en texto ("Argentina 2 - 1 Francia") y los puntos que ganaría el usuario si acierta exacto (+3 pts)
- **Cuenta regresiva** hasta el cierre de predicciones (kickoff)
- Botón "Confirmar predicción" que cambia de estado visualmente al confirmar (texto "Predicción guardada", fondo oscuro con borde verde)
- Toda la pantalla se deshabilita después del kickoff — no se puede modificar

### 5. Perfil del jugador
- Estadísticas del torneo
- Historial de motes por fecha
- Motes acumulativos obtenidos
- Botón para ver/compartir cards

### 6. Panel del admin
- Cargar/editar resultado de partido manualmente
- Ver estado del polling de la API externa

---

## Diseño visual

**Estética:** Oscura, moderna, energética. Inspirada en apps de fantasy football (Sorare, ESPN Fantasy) pero con personalidad propia argentina/memeable.

**Paleta de colores:**
- Fondo: `#0D0D0D` (casi negro)
- Superficie: `#1A1A2E` (azul oscuro profundo)
- Acento primario: `#00FF87` (verde neón — energía, goles)
- Acento secundario: `#FF6B35` (naranja — fuego, racha)
- Texto principal: `#FFFFFF`
- Texto secundario: `#8A8A9A`

**Tipografía:**
- Display / títulos: `Bebas Neue` o `Barlow Condensed` — potente, deportivo
- Cuerpo: `DM Sans` — legible, moderno

**Elementos distintivos:**
- Animaciones suaves cuando cambia la tabla de posiciones (los items se mueven con transición)
- El mote de cada jugador resaltado con el color del badge (dorado para Crack, rojo para Mufa, etc.)
- Cards de figurita con diseño retro + moderno: borde degradado, fondo con textura sutil, tipografía bold

**Cards compartibles — diseño figurita:**
- Formato vertical (como figurita Panini)
- Fondo con gradiente según el mote (dorado para Crack, rojo oscuro para Mufa, etc.)
- Nombre del jugador en tipografía grande bold
- Emoji + nombre del mote centrado
- Puntos de la fecha y posición en tabla
- Frase graciosa en itálica
- Logo Prodeá en el footer
- Borde brillante animado (en app) o estático (en la imagen exportada)

---

## Monetización (fase 1)

- Torneos ilimitados gratis hasta 10 participantes; premium para más (fase 2)
- Temas visuales premium para las cards (fase 2)
- **Ads interstitials (pantalla completa) — implementar en fase 1:**
  - Usar **Google AdSense** interstitials
  - Mostrar en momentos naturales para no interrumpir la experiencia:
    - Al abrir la tabla de posiciones después de que terminó un partido
    - Al ver la card de mote de un compañero
    - Al entrar al fixture (cada 3 veces, no siempre — usar contador en localStorage)
  - Nunca mostrar durante un partido en curso
  - Frecuencia máxima: 1 interstitial cada 5 minutos por sesión

---

## API externa — football-data.org

- Endpoint para obtener partidos del Mundial: `GET /v4/competitions/WC/matches`
- Polling cada 10 minutos solo cuando `status == IN_PLAY`
- Mapear campos: `homeTeam.name`, `awayTeam.name`, `score.fullTime.home/away`, `status`
- En caso de error (rate limit, caída), loguear y activar modo manual sin romper la app

---

## Notas para el desarrollo

- Arrancar con el backend primero: modelos, migrations EF Core, endpoints REST
- Segundo: lógica de puntuación y asignación de motes
- Tercero: integración football-data.org + SignalR
- Cuarto: frontend React, mobile-first, Tailwind
- Quinto: generación de cards compartibles
- El proyecto debe tener un `README.md` con instrucciones de setup local y deploy en Railway/Vercel
- Variables de entorno: connection string PostgreSQL, JWT secret, API key de football-data.org
