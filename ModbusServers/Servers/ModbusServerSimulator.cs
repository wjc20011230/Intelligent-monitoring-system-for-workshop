using NModbus;
using NModbus.Data;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
namespace ModbusServers.Servers
{
    public class ModbusServerSimulator : IDisposable
    {
        private TcpListener _tcpListener;
        private const byte SlaveId = 1;
        private readonly ModbusFactory _factory = new();
        private SlaveDataStore _dataStore;
        private bool _isRunning;
        private readonly object _lock = new();
        private Timer _dataUpdateTimer;

        public event EventHandler<string> StatusChanged;
        public bool IsRunning { get; private set; }

        public ModbusServerSimulator()
        {
            _dataStore = new SlaveDataStore();
            InitializeMachineData(_dataStore);
            StartDataUpdateTimer();
        }

        public void Start()
        {
            try
            {
                _tcpListener = new TcpListener(IPAddress.Any, 502);
                _tcpListener.Start();
                _isRunning = true;
                IsRunning = true;
                OnStatusChanged("服务器已启动");
                Debug.WriteLine("服务器启动成功，监听端口 502");
                _ = Task.Run(ListenAsync);
            }
            catch (Exception ex)
            {
                IsRunning = false;
                OnStatusChanged($"启动失败: {ex.Message}");
                Debug.WriteLine($"启动失败: {ex.Message}");
                throw;
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
                _tcpListener?.Stop();
                _dataUpdateTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                IsRunning = false;
                OnStatusChanged("服务器已停止");
                Debug.WriteLine("服务器已停止");
            }
        }

        private async Task ListenAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _tcpListener.AcceptTcpClientAsync();
                    Debug.WriteLine($"客户端连接: {client.Client.RemoteEndPoint}");
                    _ = HandleClientAsync(client);
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    Debug.WriteLine($"监听错误: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    using var stream = client.GetStream();
                    var buffer = new byte[4096];
                    while (_isRunning && client.Connected)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;
                        Debug.WriteLine($"收到 {bytesRead} 字节: {BitConverter.ToString(buffer, 0, bytesRead)}");
                        await ProcessFrameAsync(stream, buffer, bytesRead);
                    }
                    Debug.WriteLine($"客户端断开: {client.Client.RemoteEndPoint}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"客户端处理异常: {ex.Message}");
                }
            }
        }

        private async Task ProcessFrameAsync(NetworkStream stream, byte[] buffer, int bytesRead)
        {
            try
            {
                if (bytesRead < 6)
                {
                    Debug.WriteLine($"数据帧长度无效: {bytesRead}");
                    return;
                }

                ushort transactionId = (ushort)((buffer[0] << 8) | buffer[1]);
                ushort protocolId = (ushort)((buffer[2] << 8) | buffer[3]);
                ushort length = (ushort)((buffer[4] << 8) | buffer[5]);
                byte unitId = buffer[6];

                if (protocolId != 0)
                {
                    Debug.WriteLine("协议标识符错误");
                    SendErrorResponse(stream, transactionId, protocolId, unitId, 0, 0x01);
                    return;
                }

                if (bytesRead < length + 6)
                {
                    Debug.WriteLine($"数据帧不完整: 预期 {length + 6}, 收到 {bytesRead}");
                    return;
                }

                byte functionCode = buffer[7];
                Debug.WriteLine($"收到请求: FunctionCode={functionCode}, UnitId={unitId}");

                if (functionCode == 3 && unitId == SlaveId)
                {
                    ushort startAddress = (ushort)((buffer[8] << 8) | buffer[9]);
                    ushort quantity = (ushort)((buffer[10] << 8) | buffer[11]);
                    Debug.WriteLine($"读取寄存器: StartAddress={startAddress}, Quantity={quantity}");

                    if (startAddress + quantity > 10000)
                    {
                        Debug.WriteLine("地址超出范围");
                        SendErrorResponse(stream, transactionId, protocolId, unitId, functionCode, 0x02);
                        return;
                    }

                    ushort[] registers = _dataStore.HoldingRegisters.ReadPoints(startAddress, quantity);
                    Debug.WriteLine($"读取数据: {string.Join(",", registers)}");
                    SendReadResponse(stream, transactionId, protocolId, unitId, functionCode, registers);
                }
                else
                {
                    Debug.WriteLine("不支持的功能码或错误的 Slave ID");
                    SendErrorResponse(stream, transactionId, protocolId, unitId, functionCode, 0x01);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理错误: {ex.Message}");
            }
        }

        private void SendReadResponse(NetworkStream stream, ushort transactionId, ushort protocolId, byte unitId, byte functionCode, ushort[] registers)
        {
            byte[] pdu = new byte[2 + registers.Length * 2];
            pdu[0] = functionCode;
            pdu[1] = (byte)(registers.Length * 2);
            for (int i = 0; i < registers.Length; i++)
            {
                pdu[2 + i * 2] = (byte)(registers[i] >> 8);
                pdu[3 + i * 2] = (byte)(registers[i] & 0xFF);
            }
            SendResponse(stream, transactionId, protocolId, unitId, pdu);
        }

        private void SendErrorResponse(NetworkStream stream, ushort transactionId, ushort protocolId, byte unitId, byte functionCode, byte errorCode)
        {
            byte[] pdu = { (byte)(functionCode | 0x80), errorCode };
            SendResponse(stream, transactionId, protocolId, unitId, pdu);
        }

        private void SendResponse(NetworkStream stream, ushort transactionId, ushort protocolId, byte unitId, byte[] pdu)
        {
            byte[] frame = new byte[6 + pdu.Length + 1];
            frame[0] = (byte)(transactionId >> 8);
            frame[1] = (byte)(transactionId & 0xFF);
            frame[2] = (byte)(protocolId >> 8);
            frame[3] = (byte)(protocolId & 0xFF);
            frame[4] = (byte)((pdu.Length + 1) >> 8);
            frame[5] = (byte)((pdu.Length + 1) & 0xFF);
            frame[6] = unitId;
            Array.Copy(pdu, 0, frame, 7, pdu.Length);
            stream.Write(frame, 0, frame.Length);
            Debug.WriteLine($"发送响应: {BitConverter.ToString(frame)}");
        }

        private void InitializeMachineData(SlaveDataStore dataStore)
        {
            Random rand = new Random();
            for (int i = 0; i < 100; i++)
            {
                ushort baseAddr = (ushort)(i * 10);
                int plan = rand.Next(100, 1000);
                int finished = rand.Next(0, plan);
                dataStore.HoldingRegisters.WritePoints(baseAddr, new ushort[]
                {
                    (ushort)rand.Next(0, 4), // 状态: 0=停机, 1=工作中, 2=等待, 3=故障
                    (ushort)(plan >> 16),
                    (ushort)(plan & 0xFFFF),
                    (ushort)(finished >> 16),
                    (ushort)(finished & 0xFFFF)
                });
            }
            Debug.WriteLine("已初始化 100 台机器的寄存器数据");
        }

        private void StartDataUpdateTimer()
        {
            _dataUpdateTimer = new Timer(UpdateMachineData, null, 0, 1000); // 每秒更新一次
        }

      

        private void UpdateMachineData(object state)
        {
            Random rand = new Random();
            for (int i = 0; i < 100; i++)
            {
                ushort baseAddr = (ushort)(i * 10);
                var registers = _dataStore.HoldingRegisters.ReadPoints(baseAddr, 5);
                ushort status = registers[0];
                int plan = (registers[1] << 16) | registers[2];
                int finished = (registers[3] << 16) | registers[4];

                // 状态逻辑
                switch (status)
                {
                    case 0: // 停机
                        if (finished < plan && rand.Next(0, 10) == 0) // 10% 概率转为等待
                        {
                            status = 2;
                        }
                        break;

                    case 1: // 工作中
                        if (finished < plan)
                        {
                            finished += rand.Next(1, 10); // 随机增加完成数
                            if (finished >= plan)
                            {
                                finished = plan;
                                status = 0; // 完成任务后停机
                            }
                            else if (rand.Next(0, 20) == 0) // 5% 概率转为故障
                            {
                                status = 3;
                            }
                        }
                        break;

                    case 2: // 等待
                        if (rand.Next(0, 5) == 0) // 20% 概率转为工作中
                        {
                            status = 1;
                        }
                        else if (rand.Next(0, 20) == 0) // 5% 概率转为故障
                        {
                            status = 3;
                        }
                        break;

                    case 3: // 故障
                        if (rand.Next(0, 10) == 0) // 10% 概率修复为停机
                        {
                            status = 0;
                        }
                        break;
                }

                _dataStore.HoldingRegisters.WritePoints(baseAddr, new ushort[]
                {
                    status,
                    (ushort)(plan >> 16),
                    (ushort)(plan & 0xFFFF),
                    (ushort)(finished >> 16),
                    (ushort)(finished & 0xFFFF)
                });
              
            }
        }

      
        private void OnStatusChanged(string message)
        {
            StatusChanged?.Invoke(this, message);
        }

        public void Dispose()
        {
            Stop();
            _dataUpdateTimer?.Dispose();
        }
    }
}