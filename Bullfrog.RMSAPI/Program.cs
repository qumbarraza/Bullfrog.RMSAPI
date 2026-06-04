using System.Text.Json;

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

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
