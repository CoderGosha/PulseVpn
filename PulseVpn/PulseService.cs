using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PulseVpn
{
    public class PulseService : BackgroundService
    {
        private PulseVpnSettings vpnSettings;
        private readonly string scriptName = "vpn_route";
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
                    PulseAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                finally
                {
                    await Task.Delay(5000);
                    // Thread.Sleep(1000);
                }

            }
        }

        private void PulseAsync()
        {
            var chech = CheckForVPNInterface();
            if (!chech)
                StartVpn();
        }

        private void StartVpn()
        {
            Console.WriteLine($"Reconnect VPN: {vpnSettings.VpnName}...");
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo("rasdial.exe", vpnSettings.VpnName)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                ErrorDialog = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            };
            process.Start();

            var result = process.WaitForExit(15000);
            if (result)
                Console.WriteLine($"Reconnect VPN: {vpnSettings.VpnName} - OK");
            else
            {
                Console.WriteLine($"Reconnect VPN: {vpnSettings.VpnName} - fail");
                KillProcessAndChildrens(process.Id);
                return;
            }

            Console.WriteLine($"VPN routing: {vpnSettings.VpnName}");

            var proc = new ProcessStartInfo()
            {
                UseShellExecute = true,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                FileName = Path.Combine(Directory.GetCurrentDirectory(), scriptName + ".cmd"),
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process.Start(proc).WaitForExit();

        }

        public bool CheckForVPNInterface()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            return interfaces.Any(x => x.Description.Contains(vpnSettings.VpnName) && x.OperationalStatus == OperationalStatus.Up);
        }

        private static void KillProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            // We must kill child processes first!
            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }

            // Then kill parents.
            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
    }
}
