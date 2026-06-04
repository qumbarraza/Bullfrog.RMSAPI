var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "Mock Bullfrog.RMSAPI");

app.Run();
