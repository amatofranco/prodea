using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prodea.Api.Data;
using Prodea.Api.Hubs;
using Prodea.Api.Services;
using Resend;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
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

// Resend email
var resendApiKey = builder.Configuration["Resend:ApiKey"] ?? "";
builder.Services.AddResend(opts => opts.ApiToken = resendApiKey);

var app = builder.Build();

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
            """);
    }
    catch { /* columnas ya existen o la tabla aún no existe — EnsureCreated se encarga */ }

    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
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

        badFixture = badMatchdays || noKnockouts || tooManyGroups;
        startupLogger.LogInformation(
            "Check fixture: groupMatchdays={MD} noKnockouts={NK} groupCount={GC} tooManyGroups={TMG} → reimport={RI}",
            groupMatchdays, noKnockouts, groupCount, tooManyGroups, badFixture);
    }

    bool shouldImport = noMatches || (hasLocalSeedOnly && !string.IsNullOrEmpty(apiKey)) || badFixture;

    if (shouldImport)
    {
        var fixtureService = scope.ServiceProvider.GetRequiredService<FixtureService>();
        var (count, source) = await fixtureService.ImportAsync(force: !noMatches);
        startupLogger.LogInformation("Fixture importado al arrancar: {Count} partidos desde {Source}", count, source);
    }
}

app.Run();
