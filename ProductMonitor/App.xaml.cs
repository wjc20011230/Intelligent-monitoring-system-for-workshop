using ModbusServers.Servers;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using System.Net.Sockets;

namespace ProductMonitor
{
    public partial class App : Application
    {
        private ModbusServerSimulator _modbusServer;
        private DispatcherTimer _serverStatusTimer;



        private void OnServerStatusChanged(object sender, string message)
        {
            Debug.WriteLine($"[Server] {message}");
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (message.Contains("失败"))
                {
                    MessageBox.Show(message, "服务端错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private void HandleCriticalError(string message)
        {
            Debug.WriteLine(message);
            MessageBox.Show(message, "致命错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _modbusServer?.Stop();
            Debug.WriteLine("Application exiting...");
            base.OnExit(e);
        }
    }
}