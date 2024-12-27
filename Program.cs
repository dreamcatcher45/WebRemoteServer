using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using NAudio.CoreAudioApi;
using NLog;
using System.Management;

namespace WebSocketServerApp
{
    public static class BrightnessControl
    {
        public static void SetBrightness(int brightnessPercentage)
        {
            if (brightnessPercentage < 0 || brightnessPercentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(brightnessPercentage), "Brightness percentage must be between 0 and 100.");
            }

            using (var mclass = new ManagementClass("WmiMonitorBrightnessMethods"))
            {
                mclass.Scope = new ManagementScope(@"\\.\root\wmi");
                using (var instances = mclass.GetInstances())
                {
                    foreach (ManagementObject instance in instances)
                    {
                        instance.InvokeMethod("WmiSetBrightness", new object[] { uint.MaxValue, brightnessPercentage });
                    }
                }
            }
        }
    }

    public class WebSocketHandler : WebSocketBehavior
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected override void OnOpen()
        {
            string clientIp = Context.UserEndPoint?.Address?.ToString() ?? "Unknown IP";
            Logger.Info($"Device connected: {clientIp}");
            Console.WriteLine($"Device connected: {clientIp}");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Logger.Info($"Received message: {e.Data}");
            Console.WriteLine($"Received message: {e.Data}");

            string response = "";
            var audioMatch = Regex.Match(e.Data, @"a_(\d+)");
            var brightnessMatch = Regex.Match(e.Data, @"b_(\d+)");

            if (audioMatch.Success)
            {
                int level = int.Parse(audioMatch.Groups[1].Value);
                if (level >= 1 && level <= 10)
                {
                    response = SetVolume(level);
                }
                else
                {
                    response = "Invalid audio level. Please use 1-10.";
                }
            }
            else if (brightnessMatch.Success)
            {
                int level = int.Parse(brightnessMatch.Groups[1].Value);
                if (level >= 1 && level <= 10)
                {
                    response = SetMonitorBrightness(level);
                }
                else
                {
                    response = "Invalid brightness level. Please use 1-10.";
                }
            }
            else
            {
                response = "Invalid message format. Use 'a_X' for audio or 'b_X' for brightness (X: 1-10)";
            }

            Logger.Info($"Action: {response}");
            Console.WriteLine($"Action: {response}");
            Send(response);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            string clientIp = Context.UserEndPoint?.Address?.ToString() ?? "Unknown IP";
            Logger.Info($"Connection lost: {clientIp}");
            Console.WriteLine($"Connection lost: {clientIp}");
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Logger.Error($"Error handling message: {e.Message}");
            Console.WriteLine($"Error handling message: {e.Message}");
        }

        private string SetVolume(int level)
        {
            try
            {
                var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                float volumePercentage = level * 10;
                device.AudioEndpointVolume.MasterVolumeLevelScalar = volumePercentage / 100.0f;
                return $"Volume set to {volumePercentage}%";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting volume: {ex.Message}");
                return $"Error setting volume: {ex.Message}";
            }
        }

        private string SetMonitorBrightness(int level)
        {
            try
            {
                int brightnessPercentage = level * 10;
                BrightnessControl.SetBrightness(brightnessPercentage);
                return $"Brightness set to {brightnessPercentage}%";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting brightness: {ex.Message}");
                return $"Error setting brightness: {ex.Message}";
            }
        }
    }

    public class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static bool serverRunning = true;

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Task.Run(() => MaintainServer()).Wait();
        }

        private static async Task MaintainServer()
        {
            while (serverRunning)
            {
                try
                {
                    string ipAddress = GetLocalIPAddress();
                    Logger.Info("Starting WebSocket server...");
                    Logger.Info($"Server listening on ws://{ipAddress}:8765");

                    Console.WriteLine("Starting WebSocket server...");
                    Console.WriteLine($"Server listening on ws://{ipAddress}:8765");

                    var server = new WebSocketServer($"ws://{ipAddress}:8765");
                    server.AddWebSocketService<WebSocketHandler>("/");
                    server.Start();

                    while (serverRunning)
                    {
                        await Task.Delay(1000); // Keep the server running
                    }

                    server.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Server error: {ex.Message}");
                    Console.WriteLine($"Server error: {ex.Message}");
                    if (serverRunning)
                    {
                        Logger.Info("Attempting to restart server in 5 seconds...");
                        Console.WriteLine("Attempting to restart server in 5 seconds...");
                        await Task.Delay(5000); // Wait before trying to restart
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Logger.Info("Received shutdown signal. Closing server gracefully...");
            Console.WriteLine("Received shutdown signal. Closing server gracefully...");
            serverRunning = false;
        }
    }
}