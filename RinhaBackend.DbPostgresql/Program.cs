using RinhaBackend.DbPostgresql;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

//https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0#bind-to-a-unix-socket
if (string.IsNullOrEmpty(builder.Configuration["SOCKET_ADDRESS"]) == false)
{
    var socketAddress = builder.Configuration["SOCKET_ADDRESS"];
    
    if(File.Exists(socketAddress)) File.Delete(socketAddress);

    Console.WriteLine("Listening to socket address " + socketAddress);

    builder.WebHost.ConfigureKestrel((ctx, opts) =>
    {
        opts.ListenUnixSocket(socketAddress);

        //if (builder.Environment.IsDevelopment()) opts.ListenLocalhost(9888);
    });
}

builder.Services.AddAndConfigureHealthChecks(builder.Configuration);
builder.Services.AddAndConfigureRequestTimeout(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

app.IncludeRequestTimeout(builder.Configuration);
app.IncludeHealhCheck(builder.Configuration);

DbPostgreSql db = new DbPostgreSql(builder.Configuration.GetConnectionString("Default"));

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

app.MapGet("/", () => $"Hello from {builder.Configuration["TRAIL"]}");

app.Run();
