using ModbusServers.Servers;
using NModbus;
using ProductMonitor.Models;
using ProductMonitor.Usercontrol;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ProductMonitor.ViewModels
{
    public static class TaskExtensions
    {
        public static async Task<T> WaitAsync<T>(this Task<T> task, TimeSpan timeout, CancellationToken ct)
        {
            var delayTask = Task.Delay(timeout, ct);
            var completedTask = await Task.WhenAny(task, delayTask);
            return completedTask == delayTask
                ? throw new TimeoutException("操作超时")
                : await task;
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        public RelayCommand(Action<T> execute) => _execute = execute;
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute((T)parameter);
    }

    internal class MainWindowVM : INotifyPropertyChanged, IDisposable
    {
        private bool _disposed;
        private readonly CancellationTokenSource _cts = new();
        private IModbusMaster _modbusMaster;
        private TcpClient _tcpClient;
        private readonly byte _slaveId = 1;
        private readonly DispatcherTimer _timer;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<JiTaiShow> JiTailist { get; } = new ObservableCollection<JiTaiShow>();

        // 过滤属性，与服务端状态对应
        private bool _filterWorking = true;
        public bool FilterWorking
        {
            get => _filterWorking;
            set { _filterWorking = value; OnPropertyChanged(); UpdateFilter(); }
        }

        private bool _filterWaiting = true;
        public bool FilterWaiting
        {
            get => _filterWaiting;
            set { _filterWaiting = value; OnPropertyChanged(); UpdateFilter(); }
        }

        private bool _filterError = true;
        public bool FilterError
        {
            get => _filterError;
            set { _filterError = value; OnPropertyChanged(); UpdateFilter(); }
        }

        private bool _filterStopped = true;
        public bool FilterStopped
        {
            get => _filterStopped;
            set { _filterStopped = value; OnPropertyChanged(); UpdateFilter(); }
        }

        public ICommand UpdateFilterCommand { get; }
        // 添加查看状态日志命令
        public ICommand ShowStatusLogCommand { get; }
        public MainWindowVM()
        {
            UpdateFilterCommand = new RelayCommand<string>(param => UpdateFilter());
            SearchStaffCommand = new RelayCommand<string>(async param => await SearchStaffAsync());

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
                IsEnabled = false
            };
            _timer.Tick += async (s, e) => await SafeUpdateDataAsync();

            // 初始化 Stafflist
            Stafflist = new ObservableCollection<StaffOut>();
            LoadStaffDataAsync();
            Task.Run(async () =>
            {
                try
                {
                    await InitializeConnectionAsync();
                    _timer.Start();
                    await SafeUpdateDataAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"初始化失败: {ex.Message} - {ex.StackTrace}");
                }
            });
        }
      

        private async Task InitializeConnectionAsync()
        {
            try
            {
                await CreateNewConnection();
                Debug.WriteLine("连接成功，开始数据更新");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化失败: {ex.Message} - {ex.StackTrace}");
                _timer.Stop();
                throw;
            }
        }

        private async Task CreateNewConnection()
        {
            try
            {
                DisposeConnection();
                _tcpClient = new TcpClient { ReceiveTimeout = 5000, SendTimeout = 5000 };
                Debug.WriteLine("正在建立TCP连接...");
                await _tcpClient.ConnectAsync("127.0.0.1", 502).WaitAsync(TimeSpan.FromSeconds(5), _cts.Token);

                if (!_tcpClient.Connected)
                    throw new InvalidOperationException("TCP连接未成功");

                _modbusMaster = new ModbusFactory().CreateMaster(_tcpClient);
                if (_modbusMaster == null)
                    throw new InvalidOperationException("无法创建ModbusMaster");

                Debug.WriteLine("Modbus连接已建立");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"连接失败: {ex.Message} - {ex.StackTrace}");
                DisposeConnection();
                throw;
            }
        }

        private async Task SafeUpdateDataAsync()
        {
            if (!IsConnected || _modbusMaster == null)
            {
                Debug.WriteLine("连接丢失或未初始化，尝试重新连接...");
                try
                {
                    await CreateNewConnection();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"重连失败: {ex.Message} - {ex.StackTrace}");
                    _timer.Stop();
                    return;
                }
            }

            try
            {
                Debug.WriteLine("开始读取机器数据...");
                var newData = await ReadMachinesDataAsync(100); // 读取 100 台机器
                Debug.WriteLine($"读取到 {newData.Count} 台机器数据");
                UpdateUiData(newData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"数据更新失败: {ex.Message} - {ex.StackTrace}");
            }
        }

        private async Task<List<JiTaiShow>> ReadMachinesDataAsync(int machineCount)
        {
            var result = new List<JiTaiShow>();
            for (int i = 0; i < machineCount; i++)
            {
                await ReadSingleMachineAsync(i, result, _cts.Token);
            }
            return result;
        }

        private async Task ReadSingleMachineAsync(int index, List<JiTaiShow> result, CancellationToken ct)
        {
            try
            {
                ushort baseAddress = (ushort)(index * 10);
                if (!IsConnected || _modbusMaster == null)
                {
                    Debug.WriteLine($"机器{index} - 连接已丢失或未初始化");
                    return;
                }

                Debug.WriteLine($"尝试读取机器{index}，地址={baseAddress}");
                var registers = await _modbusMaster.ReadHoldingRegistersAsync(_slaveId, baseAddress, 5)
                    .WaitAsync(TimeSpan.FromSeconds(2), ct);

                if (registers?.Length >= 5)
                {
                    var data = ParseRegisters(index, registers);
                    lock (result)
                    {
                        result.Add(data);
                    }
                    Debug.WriteLine($"机器{index}读取成功：{data.FinishedCount}/{data.PlanCount}, 状态: {data.Status}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"机器{index}读取失败：{ex.Message} - {ex.StackTrace}");
            }
        }

        private JiTaiShow ParseRegisters(int index, ushort[] registers)
        {
            return new JiTaiShow
            {
                Machinename = $"焊接机-{index + 1}",
                Status = registers[0] switch
                {
                    0 => "停机",
                    1 => "作业中",
                    2 => "等待",
                    3 => "错误",
                    _ => "未知"
                },
                PlanCount = (registers[1] << 16) | registers[2],
                FinishedCount = (registers[3] << 16) | registers[4],
                OrderNo = "H20242286666"
            };
        }

        private void UpdateUiData(List<JiTaiShow> newData)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                JiTailist.Clear();
                foreach (var item in newData.OrderBy(x => x.Machinename))
                {
                    JiTailist.Add(item);
                }
                UpdateFilter();
            });
        }

        private void UpdateFilter()
        {
            var view = CollectionViewSource.GetDefaultView(JiTailist);
            if (view != null)
            {
                view.Filter = item =>
                {
                    var machine = item as JiTaiShow;
                    if (machine == null) return false;

                    return (machine.Status == "作业中" && FilterWorking) ||
                           (machine.Status == "等待" && FilterWaiting) ||
                           (machine.Status == "错误" && FilterError) ||
                           (machine.Status == "停机" && FilterStopped);
                };
                view.Refresh();
                Debug.WriteLine($"过滤应用: Working={FilterWorking}, Waiting={FilterWaiting}, Error={FilterError}, Stopped={FilterStopped}, 显示数量={view.Cast<object>().Count()}");
            }
        }

        public bool IsConnected
        {
            get
            {
                try
                {
                    return _tcpClient?.Client != null &&
                           _tcpClient.Connected &&
                           _modbusMaster != null;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
        }

        private void DisposeConnection()
        {
            try
            {
                _modbusMaster?.Dispose();
                _tcpClient?.Close();
                _tcpClient?.Dispose();
            }
            catch { }
            finally
            {
                _modbusMaster = null;
                _tcpClient = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts.Cancel();
            _timer.Stop();
            DisposeConnection();
            _cts.Dispose();
        }

        private UserControl _monitorUC1;
        public UserControl MonitorUC1
        {
            get => _monitorUC1 ??= new MonitorUC1();
            set { _monitorUC1 = value; OnPropertyChanged(); }
        }

        public string TimeStr => DateTime.Now.ToString("HH:mm");
        public string DateStr => DateTime.Now.ToString("yyyy-MM-dd");
        public string Weekday => DateTime.Now.DayOfWeek switch
        {
            DayOfWeek.Sunday => "星期日",
            DayOfWeek.Monday => "星期一",
            DayOfWeek.Tuesday => "星期二",
            DayOfWeek.Wednesday => "星期三",
            DayOfWeek.Thursday => "星期四",
            DayOfWeek.Friday => "星期五",
            DayOfWeek.Saturday => "星期六",
            _ => "未知"
        };

        private string _machineCount = "0168";
        public string Machinecount
        {
            get => _machineCount;
            set { _machineCount = value; OnPropertyChanged(); }
        }

        private string _productCount = "2389";
        public string Productcount
        {
            get => _productCount;
            set { _productCount = value; OnPropertyChanged(); }
        }

        private string _badCount = "0789";
        public string Badcount
        {
            get => _badCount;
            set { _badCount = value; OnPropertyChanged(); }
        }

        private List<EnviromentModel> _enlist = new List<EnviromentModel>
        {
            new EnviromentModel { EnName = "光照(LUX)", EnValue = 123 },
            new EnviromentModel { EnName = "噪音(DB)", EnValue = 55 },
            new EnviromentModel { EnName = "温度(℃)", EnValue = 80 },
            new EnviromentModel { EnName = "湿度(%)", EnValue = 43 },
            new EnviromentModel { EnName = "PM2.5(m³)", EnValue = 20 },
            new EnviromentModel { EnName = "硫化氢(PPM)", EnValue = 15 },
            new EnviromentModel { EnName = "氮气(PPM)", EnValue = 18 }
        };
        public List<EnviromentModel> Enlist
        {
            get => _enlist;
            set { _enlist = value; OnPropertyChanged(); }
        }

        private List<BaoJingModel> _baojing = new List<BaoJingModel>
        {
            new BaoJingModel { Num = "01", Msg = "设备温度过高", Date = "2025-2-21 18:12:56", Time = "7" },
            new BaoJingModel { Num = "02", Msg = "车间温度过高", Date = "2025-2-22 19:02:36", Time = "17" },
            new BaoJingModel { Num = "03", Msg = "设备转速过快", Date = "2025-2-23 13:47:59", Time = "13" },
            new BaoJingModel { Num = "04", Msg = "设备气压偏低", Date = "2025-2-24 15:56:04", Time = "57" }
        };
        public List<BaoJingModel> Baojing
        {
            get => _baojing;
            set { _baojing = value; OnPropertyChanged(); }
        }

        private List<SheBeiModel> _sheBeilist = new List<SheBeiModel>
        {
            new SheBeiModel { SheBeiName = "电能（Kw.h)", SheBeiValue = 70.8 },
            new SheBeiModel { SheBeiName = "电压(V)", SheBeiValue = 220 },
            new SheBeiModel { SheBeiName = "电流(A)", SheBeiValue = 5 },
            new SheBeiModel { SheBeiName = "压差(压差)", SheBeiValue = 13 },
            new SheBeiModel { SheBeiName = "温度(℃)", SheBeiValue = 37 },
            new SheBeiModel { SheBeiName = "振动(mm/s)", SheBeiValue = 5.1 },
            new SheBeiModel { SheBeiName = "转速(r/min)", SheBeiValue = 2600 },
            new SheBeiModel { SheBeiName = "气压(kPa)", SheBeiValue = 0.5 }
        };
        public List<SheBeiModel> SheBeilist
        {
            get => _sheBeilist;
            set { _sheBeilist = value; OnPropertyChanged(); }
        }

        private List<RaderModel> _raderlist = new List<RaderModel>
        {
            new RaderModel { RaderName = "排烟风机", RaderValue = 90 },
            new RaderModel { RaderName = "客梯", RaderValue = 37 },
            new RaderModel { RaderName = "供水机", RaderValue = 69 },
            new RaderModel { RaderName = "喷淋水泵", RaderValue = 34.2 },
            new RaderModel { RaderName = "稳压设备", RaderValue = 14 }
        };
        public List<RaderModel> Raderlist
        {
            get => _raderlist;
            set { _raderlist = value; OnPropertyChanged(); }
        }

        //人力显示查询模块
        // 数据库连接字符串
        private readonly string _connectionString = "Server=DESKTOP-G2PTIM2\\MSSQLSERVER01;Database=TestDB;Trusted_Connection=True;";
        // 修改 Stafflist 为 ObservableCollection
        private ObservableCollection<StaffOut> _stafflist;
        public ObservableCollection<StaffOut> Stafflist
        {
            get => _stafflist;
            set { _stafflist = value; OnPropertyChanged(); }
        }
        // 查询相关属性
        private string _searchName;
        public string SearchName
        {
            get => _searchName;
            set { _searchName = value; OnPropertyChanged(); }
        }

        public ICommand SearchStaffCommand { get; }
        // 从数据库加载员工数据
        private async Task LoadStaffDataAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT Id, StaffName, JobTitle, OutWorkValue FROM Staff", connection);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        Stafflist.Clear();
                        while (await reader.ReadAsync())
                        {
                            Stafflist.Add(new StaffOut
                            {
                                Id = reader.GetInt32(0),
                                Staffname = reader.GetString(1),
                                JT = reader.GetString(2),
                                Outworkvalue = reader.GetInt32(3)
                            });
                        }
                    }
                }
                Debug.WriteLine($"从数据库加载了 {Stafflist.Count} 条员工数据");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载员工数据失败: {ex.Message}");
                MessageBox.Show($"加载员工数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // 根据名字查询员工
        private async Task SearchStaffAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchName))
            {
                await LoadStaffDataAsync(); // 如果搜索框为空，加载所有数据
                return;
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT Id, StaffName, JobTitle, OutWorkValue FROM Staff WHERE StaffName = @StaffName", connection);
                    command.Parameters.AddWithValue("@StaffName", SearchName);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        Stafflist.Clear();
                        if (await reader.ReadAsync())
                        {
                            Stafflist.Add(new StaffOut
                            {
                                Id = reader.GetInt32(0),
                                Staffname = reader.GetString(1),
                                JT = reader.GetString(2),
                                Outworkvalue = reader.GetInt32(3)
                            });
                        }
                        else
                        {
                            MessageBox.Show($"未找到名为 {SearchName} 的员工", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                Debug.WriteLine($"查询员工: {SearchName}, 结果数量: {Stafflist.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"查询员工失败: {ex.Message}");
                MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<WorkShop> _workShoplist = new List<WorkShop>
        {
            new WorkShop { WorkshopName = "贴片车间", WorkingCount = 14, WaitingCount = 14, StopCount = 14, WrongCount = 16 },
            new WorkShop { WorkshopName = "封装车间", WorkingCount = 17, WaitingCount = 24, StopCount = 4, WrongCount = 16 },
            new WorkShop { WorkshopName = "焊接车间", WorkingCount = 24, WaitingCount = 8, StopCount = 9, WrongCount = 11 },
            new WorkShop { WorkshopName = "装配车间", WorkingCount = 11, WaitingCount = 12, StopCount = 2, WrongCount = 1 }
        };
        public List<WorkShop> WorkShoplist
        {
            get => _workShoplist;
            set { _workShoplist = value; OnPropertyChanged(); }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}