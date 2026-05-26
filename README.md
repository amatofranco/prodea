# Prodeá

App mobile-first de prode futbolero entre amigos para el Mundial 2026, con motes virales (El Crack, El Mufa, etc.) y cards compartibles estilo figurita.

## Stack

- **Backend:** .NET 9 — ASP.NET Core Web API + SignalR
- **Base de datos:** PostgreSQL (EF Core + Npgsql)
- **Frontend:** React + Tailwind CSS (PWA) — próximamente
- **Hosting:** Railway (backend + DB) / Vercel (frontend)

## Setup local

### Requisitos

- .NET 9 SDK
- PostgreSQL 14+
- Node.js 20+ (para el frontend)

### Backend

1. Clonar el repo y entrar al directorio:
   ```bash
   cd backend/src/Prodea.Api
   ```

2. Crear `appsettings.Development.json` (ya incluido con valores de ejemplo) y ajustar:
   - `ConnectionStrings:DefaultConnection` — tu string de conexión a PostgreSQL local
   - `Jwt:Secret` — mínimo 32 caracteres
   - `FootballData:ApiKey` — clave de [football-data.org](https://www.football-data.org/)

3. Aplicar migraciones y levantar:
   ```bash
   dotnet ef database update
   dotnet run
   ```

4. Cargar el fixture del Mundial (solo en desarrollo):
   ```bash
   curl -X POST http://localhost:5000/api/admin/seed-fixture
   ```

### Variables de entorno (Railway)

| Variable | Descripción |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Secret` | Clave secreta JWT (mínimo 32 chars) |
| `Jwt__Issuer` | `Prodea` |
| `Jwt__Audience` | `ProdeaApp` |
| `FootballData__ApiKey` | API key de football-data.org |
| `Cors__AllowedOrigins__0` | URL del frontend en Vercel |

## Estructura del proyecto

```
backend/
  src/
    Prodea.Api/
      Controllers/     — Auth, Tournaments, Matches, Profile, Admin
      Data/            — DbContext, Migrations, WorldCup2026Seed
      DTOs/            — Request/Response records
      Hubs/            — SignalR TournamentHub
      Models/          — Entidades EF Core
      Services/        — JwtService, ScoringService, BadgeService, FootballDataService
```

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

## WebSocket (SignalR)

Conectar a `/hubs/tournament?access_token=<JWT>` y unirse al grupo:

```js
connection.invoke("JoinTournament", tournamentId)
// Recibe: "MatchUpdated" { matchId, homeScore, awayScore, status }
```

## Lógica de motes

| Prioridad | Mote | Condición |
|-----------|------|-----------|
| 1 | El Crack | Mayor puntaje de la fecha |
| 2 | El Mufa | Menor puntaje de la fecha |
| 3 | El Adivino | 3+ resultados exactos |
| 4 | El Francotirador | 1+ resultado exacto |
| 5 | El Payaso | Ningún ganador acertado |
| 6 | El Dormido | Sin predicciones cargadas |
