using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PulseVpn
{
    public class PulseService : BackgroundService
    {
        private PulseVpnSettings vpnSettings;
        public PulseService(IOptions<PulseVpnSettings> options)
        {
            if (options.Value?.VpnName == null)
                throw new ArgumentNullException(nameof(options.Value.VpnName));

            vpnSettings = options.Value;
        }

        private ManualResetEvent WorkerCancelled = new ManualResetEvent(false);

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            WorkerCancelled.Set();
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Run(PulseAsync);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                finally
                {
                    await Task.Delay(2000);
                    // Thread.Sleep(1000);
                }

            }
        }

        private async Task PulseAsync()
        {
            var chech = CheckForVPNInterface();
            if (!chech)
                StartVpn();
        }

        private void StartVpn()
        {
            Console.WriteLine($"Reconnect VPN: {vpnSettings.VpnName}...");
            var proc = new ProcessStartInfo()
            {
                UseShellExecute = true,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                FileName = Path.Combine(Directory.GetCurrentDirectory(), "vpn_route.cmd"),
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process.Start(proc).WaitForExit();

            Console.WriteLine($"Reconnect VPN: {vpnSettings.VpnName} - OK");
        }

        public bool CheckForVPNInterface()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

                return interfaces.Any(x => x.Description.Contains(vpnSettings.VpnName) && x.OperationalStatus == OperationalStatus.Up);
            }

            return false;
        }
    }
}
