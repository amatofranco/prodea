using Prodea.Api.Services;

namespace Prodea.Api.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddProdeaCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:3000"];

        var allowedOriginSuffixes = configuration.GetSection("Cors:AllowedOriginSuffixes").Get<string[]>()
            ?? [".vercel.app"];

        services.AddCors(options =>
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

        return services;
    }

    public static IServiceCollection AddProdeaHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("FootballData", client =>
        {
            client.BaseAddress = new Uri("https://api.football-data.org");
            client.DefaultRequestHeaders.Add("X-Auth-Token", configuration["FootballData:ApiKey"] ?? "");
        });

        services.AddHttpClient("Espn", client =>
        {
            client.BaseAddress = new Uri("https://site.api.espn.com");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }

    public static IServiceCollection AddProdeaServices(this IServiceCollection services)
    {
        services.AddScoped<JwtService>();
        services.AddScoped<BadgeService>();
        services.AddScoped<EmailService>();
        services.AddScoped<EspnBracketService>();
        services.AddScoped<FixtureService>();
        services.AddScoped<MatchFinalizationService>();
        services.AddSingleton<EspnApiClient>();
        services.AddSingleton<PollingStatusService>();
        services.AddHostedService<FootballDataService>();
        services.AddHostedService<BackupService>();
        services.AddScoped<PushNotificationService>();
        services.AddHostedService<MatchNotificationJob>();

        return services;
    }
}
