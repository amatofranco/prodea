using Microsoft.EntityFrameworkCore;
using Prodea.Api.Data;
using Prodea.Api.Services;

namespace Prodea.Api.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddProdeaDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);
        services.AddDbContext<ProdeaDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProdeaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await db.Database.MigrateAsync();

        await SeedFixtureAsync(app, scope, db, logger);
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var raw = configuration.GetConnectionString("DefaultConnection");
        var connectionString = (!string.IsNullOrEmpty(raw) ? raw : null)
            ?? Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? throw new InvalidOperationException("No database connection string configured");

        if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':');
            var port = uri.Port == -1 ? 5432 : uri.Port;
            connectionString = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
        }

        return connectionString;
    }

    private static async Task SeedFixtureAsync(WebApplication app, IServiceScope scope, ProdeaDbContext db, ILogger logger)
    {
        var apiKey = app.Configuration["FootballData:ApiKey"];

        bool noMatches = !await db.Matches.AnyAsync();
        bool hasLocalSeedOnly = !noMatches && !await db.Matches.AnyAsync(m => m.ExternalId != null);

        bool badFixture = false;
        if (!noMatches && !hasLocalSeedOnly && !string.IsNullOrEmpty(apiKey))
        {
            var groupMatchdays = await db.Matches
                .Where(m => m.Phase == Prodea.Api.Models.MatchPhase.Group)
                .Select(m => m.Matchday)
                .Distinct()
                .CountAsync();
            bool badMatchdays = groupMatchdays <= 1;

            bool noKnockouts = !await db.Matches.AnyAsync(m => m.Phase != Prodea.Api.Models.MatchPhase.Group);

            const int Wc2026GroupMatchCount = 72;
            var groupCount = await db.Matches.CountAsync(m => m.Phase == Prodea.Api.Models.MatchPhase.Group);
            bool tooManyGroups = groupCount > Wc2026GroupMatchCount;

            bool knockoutsNeedLabels = await db.Matches.AnyAsync(
                m => m.Phase != Prodea.Api.Models.MatchPhase.Group &&
                     m.HomeTeam == "TBD" &&
                     m.HomeTeamLabel == null);

            bool r32Duplicates = await db.Matches
                .Where(m => m.Phase == Prodea.Api.Models.MatchPhase.R32 && m.HomeTeam != "TBD")
                .GroupBy(m => m.HomeTeam)
                .AnyAsync(g => g.Count() > 1);

            badFixture = badMatchdays || noKnockouts || tooManyGroups || knockoutsNeedLabels || r32Duplicates;
            logger.LogInformation(
                "Check fixture: groupMatchdays={MD} noKnockouts={NK} groupCount={GC} tooManyGroups={TMG} knockoutsNeedLabels={KNL} r32Dup={DUP} → reimport={RI}",
                groupMatchdays, noKnockouts, groupCount, tooManyGroups, knockoutsNeedLabels, r32Duplicates, badFixture);
        }

        bool forceReimport = app.Configuration.GetValue<bool>("ForceFixtureReimport");
        bool shouldImport = noMatches || (hasLocalSeedOnly && !string.IsNullOrEmpty(apiKey)) || badFixture || forceReimport;

        if (shouldImport)
        {
            var fixtureService = scope.ServiceProvider.GetRequiredService<FixtureService>();
            var (count, source) = await fixtureService.ImportAsync(force: !noMatches);
            logger.LogInformation("Fixture importado al arrancar: {Count} partidos desde {Source}", count, source);
        }
    }
}
