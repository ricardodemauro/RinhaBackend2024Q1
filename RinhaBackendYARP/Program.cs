using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RinhaBackendYARP;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

var debugEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

var secretsPath = Path.Combine(Directory.GetCurrentDirectory(), "secrets.json");
builder.Configuration.AddJsonFile(secretsPath, optional: debugEnv);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((context, handler) =>
    {
        if (context.NewMetadata.TryGetValue("Socket.Address", out var unixSocket))
        {
            handler.ConnectCallback = async (context, cancellation) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(new UnixDomainSocketEndPoint(unixSocket), cancellation);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            };
        }
    });

var descriptor = new ServiceDescriptor(
    typeof(IForwarderHttpClientFactory),
    typeof(SocketForwarderHttpClientFactory),
    ServiceLifetime.Transient);

builder.Services.Replace(descriptor);

var app = builder.Build();
app.MapReverseProxy();
app.Run();