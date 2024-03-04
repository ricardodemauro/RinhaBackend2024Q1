using RinhaBackend.DbMysql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Mime;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

//https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0#bind-to-a-unix-socket
if (string.IsNullOrEmpty(builder.Configuration["SOCKET_ADDRESS"]) == false)
{
    var socketAddress = $"/tmp/{builder.Configuration["SOCKET_ADDRESS"]}";
    Console.WriteLine("Listening on socket address " + socketAddress);

    builder.WebHost.ConfigureKestrel((ctx, opts) =>
    {
        opts.ListenUnixSocket(socketAddress);
    });
}

builder.Services.AddAndConfigureHealthChecks(builder.Configuration);
builder.Services.AddAndConfigureRequestTimeout(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

var db = new DbMySql(builder.Configuration.GetConnectionString("Default"));

if ((builder.Configuration["USE_TIMEOUT_MID"] ?? bool.FalseString) == bool.TrueString)
{
    app.UseRequestTimeouts();
}

if ((builder.Configuration["USE_HEALTHZ"] ?? bool.FalseString) == bool.TrueString)
{
    app.UseHealthChecks("/healthz", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = async (context, report) =>
        {
            var result = JsonSerializer.Serialize(
                new
                {
                    status = report.Status.ToString(),
                    monitors = report.Entries.Select(e => new { key = e.Key, value = Enum.GetName(typeof(HealthStatus), e.Value.Status) })
                });
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsync(result);
        }
    });
}

app.MapPost("/clientes/{id}/transacoes",
    [RequestTimeout(milliseconds: 2000)] async ([FromRoute] int id,
    [FromBody] TransacaoRequest transacaoRequest,
    CancellationToken cancellationToken) =>
    {
        if (transacaoRequest?.IsValid() == true)
        {
            return await db.Transacao(id, transacaoRequest, cancellationToken);
        }
        return Results.UnprocessableEntity();
    });

app.MapGet("/clientes/{id}/extrato",
   [RequestTimeout(milliseconds: 2000)] async ([FromRoute] int id, CancellationToken cancellationToken) =>
    {
        return await db.Extrato(id, cancellationToken);
    });

app.MapGet("/", () => $"Hello from {Environment.GetEnvironmentVariable("TRAIL")}");

app.Run();
