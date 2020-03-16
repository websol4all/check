using System.Threading;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Xyzies.Devices.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            const int port = 8085;

            ThreadPool.SetMinThreads(int.MaxValue, int.MaxValue);

            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel()
                .ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ListenAnyIP(port, listenOptions =>
                    {
                        // TODO: Setup certificate for HTTPS and HTTP2.0
                        //listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        //listenOptions.UseHttps("localhost.pfx", "Secret001");
                    });
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();
        }
    }
}
