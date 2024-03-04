using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace RinhaBackendYARP;

public class SocketForwarderHttpClientFactory : IForwarderHttpClientFactory
{
    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {
        if (context.OldClient is not null && context.NewConfig == context.OldConfig)
            return context.OldClient;

        string unixAddress = string.Empty;
        if (context?.NewMetadata?.TryGetValue("Socket.Address", out unixAddress) == false
            || string.IsNullOrEmpty(unixAddress))

            throw new Exception("Could not load unix config");

        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15.0),
            ConnectCallback = async (context, cancellation) =>
            {
                // var msg = context.InitialRequestMessage;
                // var unixUri = msg.RequestUri.AbsoluteUri.Replace("http://", "unix:/").TrimEnd('/');

                // msg.RequestUri = new Uri("http://localhost:5113");

                var isPrimary = context.InitialRequestMessage.RequestUri.AbsolutePath.IndexOf("http://localhost:9998") > -1;

                var dockerEngineUri = isPrimary ? new Uri("unix:/tmp/kestrel-api3.sock") : new Uri("unix:/tmp/kestrel-api4.sock");

                Console.WriteLine("Connecting on sock: {0}", dockerEngineUri.AbsoluteUri);

                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);

                var endpoint = new UnixDomainSocketEndPoint(dockerEngineUri.AbsolutePath);
                try
                {
                    await socket.ConnectAsync(endpoint, cancellation);
                    return new NetworkStream(socket);
                }
                catch (Exception)
                {
                    socket.Dispose();
                    throw;
                }

            }
        };

        ConfigureHandler(in context, ref handler);

        return new HttpMessageInvoker(handler, disposeHandler: true);
    }

    void ConfigureHandler(in ForwarderHttpClientContext context, ref SocketsHttpHandler handler)
    {
        HttpClientConfig newConfig = context.NewConfig;
        if (newConfig.SslProtocols.HasValue)
        {
            handler.SslOptions.EnabledSslProtocols = newConfig.SslProtocols.Value;
        }

        if (newConfig.MaxConnectionsPerServer.HasValue)
        {
            handler.MaxConnectionsPerServer = newConfig.MaxConnectionsPerServer.Value;
        }

        if (newConfig.DangerousAcceptAnyServerCertificate.GetValueOrDefault())
        {
            handler.SslOptions.RemoteCertificateValidationCallback = (
                object _003Cp0_003E,
                X509Certificate _003Cp1_003E,
                X509Chain _003Cp2_003E,
                SslPolicyErrors _003Cp3_003E) => true;
        }

        handler.EnableMultipleHttp2Connections = newConfig.EnableMultipleHttp2Connections.GetValueOrDefault(true);
        if (newConfig.RequestHeaderEncoding != null)
        {
            Encoding encoding2 = Encoding.GetEncoding(newConfig.RequestHeaderEncoding);
            handler.RequestHeaderEncodingSelector = (string _, HttpRequestMessage _) => encoding2;
        }

        if (newConfig.ResponseHeaderEncoding != null)
        {
            Encoding encoding = Encoding.GetEncoding(newConfig.ResponseHeaderEncoding);
            handler.ResponseHeaderEncodingSelector = (string _, HttpRequestMessage _) => encoding;
        }
    }
}
