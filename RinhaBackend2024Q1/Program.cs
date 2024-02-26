using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RinhaBackend2024Q1;
using System.Net.Mime;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddMySql(connectionString: builder.Configuration.GetConnectionString("Default"));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

DbService db = new DbService(builder.Configuration.GetConnectionString("Default"));

app.MapGet("/", () => "Hello World!");

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

app.MapPost("/clientes/{id}/transacoes",
    async ([FromRoute] int id, [FromBody] TransacaoRequest transacaoRequest) =>
    {
        if (transacaoRequest?.IsValid() == true)
        {
            return await db.Transacao(id, transacaoRequest);
        }
        return Results.UnprocessableEntity();
    });

app.MapGet("/clientes/{id}/extrato",
    async ([FromRoute] int id, CancellationToken cancellationToken) =>
    {
        return await db.Extrato(id, cancellationToken);
    });

app.Run();
