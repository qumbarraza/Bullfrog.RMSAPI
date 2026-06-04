using System.Text.Json;
using Bullfrog;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    app     = "Bullfrog.RMSAPI",
    status  = "running",
    message = "Mock RMS API is live - final testing"
}));

app.MapGet("/info", () =>
{
    var versionFile = Path.Combine(AppContext.BaseDirectory, "version.json");
    object? versionInfo = null;

    if (File.Exists(versionFile))
    {
        var raw = File.ReadAllText(versionFile);
        versionInfo = JsonSerializer.Deserialize<object>(raw);
    }

    return Results.Ok(new
    {
        app         = "Bullfrog.RMSAPI",
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        server      = Environment.MachineName,
        startedAt   = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToString("o"),
        version     = versionInfo
    });
});

// Served from Bullfrog class library — proves the shared lib change is picked up
app.MapGet("/spa", () => Results.Ok(SpaInfo.GetIdentity()));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
