using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace PulseVpn
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("PulseVpn  starting...");
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                // NLog: catch any exception and log it.
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            }


        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<PulseVpnSettings>(hostContext.Configuration.GetSection("PulseVpnSettings"));
                    services.AddHostedService<PulseService>();

                })
                .ConfigureLogging(logging =>
                {
                    // logging.ClearProviders();
                    // logging.SetMinimumLevel(LogLevel.Trace);
                });
        }
    }
}
