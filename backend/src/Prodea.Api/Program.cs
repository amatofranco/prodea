using Prodea.Api.Extensions;
using Prodea.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

builder.Services.AddProdeaDatabase(builder.Configuration);
builder.Services.AddProdeaAuth(builder.Configuration);
builder.Services.AddProdeaCors(builder.Configuration);
builder.Services.AddProdeaHttpClients(builder.Configuration);
builder.Services.AddProdeaServices();

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

await app.InitializeDatabaseAsync();

app.Run();
