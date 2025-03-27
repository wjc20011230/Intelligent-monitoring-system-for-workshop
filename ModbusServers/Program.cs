
using ModbusServers.Servers;
using System;

namespace ModbusServers
{

    class Program
    {
        static void Main(string[] args)
        {
            var server = new ModbusServerSimulator();
            try
            {
                server.Start();
                Console.WriteLine($"服务端已在端口 502 启动，监听状态：{server.IsRunning}");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动失败: {ex.Message}");
            }
            finally
            {
                server.Dispose();
            }
        }


    }
}