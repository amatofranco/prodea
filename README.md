# Prodeá

App mobile-first de prode futbolero entre amigos para el Mundial 2026, con motes virales (El Crack, El Mufa, etc.) y cards compartibles estilo figurita.

## Stack

- **Backend:** .NET 9 — ASP.NET Core Web API + SignalR
- **Base de datos:** PostgreSQL (EF Core + Npgsql)
- **Frontend:** React + Tailwind CSS (PWA)
- **Hosting:** Railway (backend + DB) / Vercel (frontend)

---

## Flujo de trabajo con Git

### Branches

| Branch | Propósito |
|--------|-----------|
| `main` | Producción — código deployado en Railway/Vercel. **Nunca commitear directo.** |
| `dev` | Desarrollo — branch base para todo el trabajo nuevo. |

### Cómo trabajar

```bash
# Siempre partir de dev actualizado
git checkout dev
git pull origin dev

# Crear un branch para la feature
git checkout -b feat/nombre-de-la-feature

# ... trabajar, commitear ...

# Pushear y abrir PR hacia dev
git push -u origin feat/nombre-de-la-feature
gh pr create --base dev --title "feat: descripción"
```

### Pasar dev a producción (main)

```bash
# Desde GitHub: abrir un PR de dev → main
gh pr create --base main --head dev --title "release: vX.X"
# Revisar, aprobar y mergear
```

Railway y Vercel deployán automáticamente cuando se mergea a `main`.

---

## Setup local

### Requisitos

- .NET 9 SDK
- PostgreSQL 14+
- Node.js 20+

### Backend

```bash
cd backend/src/Prodea.Api
```

Ajustar `appsettings.Development.json`:
- `ConnectionStrings:DefaultConnection` — PostgreSQL local
- `Jwt:Secret` — mínimo 32 caracteres
- `FootballData:ApiKey` — clave de [football-data.org](https://www.football-data.org/)

```bash
dotnet ef database update
dotnet run
```

Cargar el fixture (una vez):
```bash
curl -X POST http://localhost:5000/api/admin/seed-fixture
```

### Frontend

```bash
cd frontend
npm install
npm run dev   # → http://localhost:5173
```

### Variables de entorno (Railway — producción)

| Variable | Descripción |
|----------|-------------|
| `DATABASE_URL` | Inyectada automáticamente por Railway al agregar PostgreSQL |
| `Jwt__Secret` | Clave secreta JWT (mínimo 32 chars) |
| `Jwt__Issuer` | `Prodea` |
| `Jwt__Audience` | `ProdeaApp` |
| `FootballData__ApiKey` | API key de football-data.org |
| `Cors__AllowedOrigins__0` | URL del frontend en Vercel |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

### Variables de entorno (Vercel — producción)

| Variable | Descripción |
|----------|-------------|
| `VITE_API_URL` | URL del backend en Railway (sin slash final) |

---

## Estructura del proyecto

```
prodea/
├── backend/
│   └── src/Prodea.Api/
│       ├── Controllers/   — Auth, Tournaments, Matches, Profile, Admin
│       ├── Data/          — DbContext, Migrations, WorldCup2026Seed
│       ├── DTOs/          — Request/Response records
│       ├── Hubs/          — SignalR TournamentHub
│       ├── Models/        — Entidades EF Core
│       └── Services/      — JwtService, ScoringService, BadgeService, FootballDataService
├── frontend/
│   └── src/
│       ├── components/    — Layout, GoalPicker, FigurineCard, BadgePill
│       ├── pages/         — Login, Register, Home, Tournament, Prediction, Profile
│       ├── services/      — api.js, signalr.js
│       └── store/         — authStore, tournamentStore (Zustand)
├── Dockerfile             — Build del backend para Railway
└── railway.toml           — Configuración de deploy
```

---

## API Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/auth/register` | Registro de usuario |
| POST | `/api/auth/login` | Login, devuelve JWT |
| GET | `/api/tournaments` | Torneos del usuario autenticado |
| POST | `/api/tournaments` | Crear torneo |
| POST | `/api/tournaments/join` | Unirse por código o link |
| GET | `/api/tournaments/{id}` | Detalle del torneo |
| GET | `/api/tournaments/{id}/leaderboard` | Tabla de posiciones |
| GET | `/api/tournaments/{id}/matches` | Fixture con predicciones del usuario |
| POST | `/api/tournaments/{id}/matches/{matchId}/predictions` | Cargar/actualizar predicción |
| POST | `/api/tournaments/{id}/matches/{matchId}/result` | Cargar resultado (admin) |
| GET | `/api/tournaments/{id}/profile/{userId}` | Perfil y motes del jugador |
| GET | `/health` | Health check |

## WebSocket (SignalR)

```js
// Conectar a /hubs/tournament?access_token=<JWT>
connection.invoke("JoinTournament", tournamentId)
// Recibe: "MatchUpdated" { matchId, homeScore, awayScore, status }
```

## Lógica de motes

| Prioridad | Mote | Condición |
|-----------|------|-----------|
| 1 | 🏆 El Crack | Mayor puntaje de la fecha |
| 2 | 💀 El Mufa | Menor puntaje de la fecha |
| 3 | 🔮 El Adivino | 3+ resultados exactos |
| 4 | 🎯 El Francotirador | 1+ resultado exacto |
| 5 | 🤡 El Payaso | Ningún ganador acertado |
| 6 | 😴 El Dormido | Sin predicciones cargadas |
