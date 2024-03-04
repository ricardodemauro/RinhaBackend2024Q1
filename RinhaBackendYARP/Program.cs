var builder = WebApplication.CreateBuilder(args);

var secretsPath = Path.Combine(Directory.GetCurrentDirectory(), "secrets.json");
builder.Configuration.AddJsonFile(secretsPath, optional: false);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.MapReverseProxy();
app.Run();