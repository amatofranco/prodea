using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prodea.Api.Data;
using Prodea.Api.Hubs;
using Prodea.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var rawConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = (!string.IsNullOrEmpty(rawConnection) ? rawConnection : null)
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("No database connection string configured");

if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    var port = uri.Port == -1 ? 5432 : uri.Port;
    connectionString = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddDbContext<ProdeaDbContext>(options =>
    options.UseNpgsql(connectionString));

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // Support JWT in SignalR query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// SignalR
builder.Services.AddSignalR();

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:3000"];

var allowedOriginSuffixes = builder.Configuration.GetSection("Cors:AllowedOriginSuffixes").Get<string[]>()
    ?? [".vercel.app"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            allowedOrigins.Contains(origin) ||
            allowedOriginSuffixes.Any(suffix => new Uri(origin).Host.EndsWith(suffix)))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// HTTP client for football-data.org
builder.Services.AddHttpClient("FootballData", client =>
{
    client.BaseAddress = new Uri("https://api.football-data.org");
    client.DefaultRequestHeaders.Add("X-Auth-Token", builder.Configuration["FootballData:ApiKey"] ?? "");
});

// App services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<BadgeService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<FixtureService>();
builder.Services.AddSingleton<PollingStatusService>();
builder.Services.AddHostedService<FootballDataService>();
builder.Services.AddHostedService<BackupService>();
builder.Services.AddScoped<PushNotificationService>();
builder.Services.AddHostedService<MatchNotificationJob>();

var app = builder.Build();

app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    ctx.Response.StatusCode = 500;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsync("{\"message\":\"Error interno del servidor\"}");
}));

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TournamentHub>("/hubs/tournament");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Auto-migrate y seed del fixture al arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProdeaDbContext>();

    // EnsureCreated crea el esquema completo en DBs nuevas; en DBs existentes es no-op
    await db.Database.EnsureCreatedAsync();

    // Agrega columnas nuevas si no existen (para DBs creadas antes de este cambio)
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "Matches" ADD COLUMN IF NOT EXISTS "HomeTeamLabel" varchar(200);
            ALTER TABLE "Matches" ADD COLUMN IF NOT EXISTS "AwayTeamLabel" varchar(200);
            ALTER TABLE "Matches" ADD COLUMN IF NOT EXISTS "Minute" int;
            ALTER TABLE "Matches" ADD COLUMN IF NOT EXISTS "Winner" varchar(100);
            ALTER TABLE "Matches" ADD COLUMN IF NOT EXISTS "FinishedAt" timestamptz;
            ALTER TABLE "Matches" ADD COLUMN IF NOT EXISTS "LastUpdatedAt" timestamptz;
            ALTER TABLE "Predictions" ADD COLUMN IF NOT EXISTS "PredictedPenaltyWinner" varchar(10);
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "FirstName" varchar(100);
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "LastName" varchar(100);
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsPwa" boolean NOT NULL DEFAULT false;
            CREATE TABLE IF NOT EXISTS "PredictionBackups" (
                "Id"        serial PRIMARY KEY,
                "CreatedAt" timestamptz NOT NULL,
                "Count"     int         NOT NULL,
                "JsonData"  text        NOT NULL
            );
            """);
    }
    catch { /* columnas ya existen o la tabla aún no existe — EnsureCreated se encarga */ }

    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Migración BadgesByMatchday: reemplaza Date por Phase+Matchday en MatchdayBadges
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            DROP INDEX IF EXISTS "IX_MatchdayBadges_UserId_TournamentId_Date";
            ALTER TABLE "MatchdayBadges" DROP COLUMN IF EXISTS "Date";
            ALTER TABLE "MatchdayBadges" ADD COLUMN IF NOT EXISTS "Phase" text NOT NULL DEFAULT '';
            ALTER TABLE "MatchdayBadges" ADD COLUMN IF NOT EXISTS "Matchday" int NOT NULL DEFAULT 0;
            DELETE FROM "MatchdayBadges" WHERE "Phase" = '';
            """);

        // Índice único: crear solo si no existe
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_indexes
                    WHERE indexname = 'IX_MatchdayBadges_UserId_TournamentId_Phase_Matchday'
                ) THEN
                    CREATE UNIQUE INDEX "IX_MatchdayBadges_UserId_TournamentId_Phase_Matchday"
                        ON "MatchdayBadges" ("UserId", "TournamentId", "Phase", "Matchday");
                END IF;
            END$$;
            """);
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "Migración BadgesByMatchday: algunos pasos ya aplicados");
    }
    // Migración ChampionPick global: crea la tabla sin TournamentId (deploys nuevos)
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ChampionPicks" (
                "Id"           serial PRIMARY KEY,
                "UserId"       int NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
                "CountryName"  varchar(100) NOT NULL DEFAULT '',
                "PickedAt"     timestamptz NOT NULL DEFAULT now(),
                "PointsEarned" int NOT NULL DEFAULT 0
            );
            """);
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "Migración ChampionPicks (create): {Msg}", ex.Message);
    }
    // Migración ChampionPick: eliminar TournamentId si la tabla ya existía con esa columna.
    // Ejecutamos cada paso por separado para que un fallo no bloquee los demás.
    try
    {
        // Paso 1: deduplicar registros por UserId (conservar el de menor Id)
        await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM "ChampionPicks"
            WHERE "Id" NOT IN (
                SELECT MIN("Id") FROM "ChampionPicks" GROUP BY "UserId"
            );
            """);
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "Migración ChampionPicks (dedup): {Msg}", ex.Message);
    }
    try
    {
        // Paso 2: eliminar FK y columna TournamentId si existe
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "ChampionPicks"
                DROP CONSTRAINT IF EXISTS "FK_ChampionPicks_Tournaments_TournamentId";
            """);
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "Migración ChampionPicks (drop FK): {Msg}", ex.Message);
    }
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            DROP INDEX IF EXISTS "IX_ChampionPicks_TournamentId_UserId";
            """);
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "Migración ChampionPicks (drop idx TournamentId): {Msg}", ex.Message);
    }
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "ChampionPicks" DROP COLUMN IF EXISTS "TournamentId";
            """);
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "Migración ChampionPicks (drop col TournamentId): {Msg}", ex.Message);
    }
    try
    {
        // Paso 3: índice único por UserId (solo si no existen duplicados)
        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ChampionPicks_UserId" ON "ChampionPicks"("UserId");
            """);
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "Migración ChampionPicks (unique idx UserId): {Msg}", ex.Message);
    }

    var apiKey = app.Configuration["FootballData:ApiKey"];

    bool noMatches = !await db.Matches.AnyAsync();
    bool hasLocalSeedOnly = !noMatches && !await db.Matches.AnyAsync(m => m.ExternalId != null);

    // Detecta fixture corrupto
    bool badFixture = false;
    if (!noMatches && !hasLocalSeedOnly && !string.IsNullOrEmpty(apiKey))
    {
        var groupMatchdays = await db.Matches
            .Where(m => m.Phase == Prodea.Api.Models.MatchPhase.Group)
            .Select(m => m.Matchday)
            .Distinct()
            .CountAsync();
        bool badMatchdays = groupMatchdays <= 1;

        // Sin ningún partido eliminatorio
        bool noKnockouts = !await db.Matches.AnyAsync(m => m.Phase != Prodea.Api.Models.MatchPhase.Group);

        // WC 2026: 48 equipos, 12 grupos de 4, cada equipo juega 3 partidos → 48×3/2 = 72
        const int Wc2026GroupMatchCount = 72;
        var groupCount = await db.Matches.CountAsync(m => m.Phase == Prodea.Api.Models.MatchPhase.Group);
        bool tooManyGroups = groupCount > Wc2026GroupMatchCount;

        // Knockout sin labels del bracket = primera vez que se corre el código con Wc2026Bracket
        bool knockoutsNeedLabels = await db.Matches.AnyAsync(
            m => m.Phase != Prodea.Api.Models.MatchPhase.Group &&
                 m.HomeTeam == "TBD" &&
                 m.HomeTeamLabel == null);

        badFixture = badMatchdays || noKnockouts || tooManyGroups || knockoutsNeedLabels;
        startupLogger.LogInformation(
            "Check fixture: groupMatchdays={MD} noKnockouts={NK} groupCount={GC} tooManyGroups={TMG} knockoutsNeedLabels={KNL} → reimport={RI}",
            groupMatchdays, noKnockouts, groupCount, tooManyGroups, knockoutsNeedLabels, badFixture);
    }

    bool forceReimport = app.Configuration.GetValue<bool>("ForceFixtureReimport");
    bool shouldImport = noMatches || (hasLocalSeedOnly && !string.IsNullOrEmpty(apiKey)) || badFixture || forceReimport;

    if (shouldImport)
    {
        var fixtureService = scope.ServiceProvider.GetRequiredService<FixtureService>();
        var (count, source) = await fixtureService.ImportAsync(force: !noMatches);
        startupLogger.LogInformation("Fixture importado al arrancar: {Count} partidos desde {Source}", count, source);
    }
}

app.Run();
