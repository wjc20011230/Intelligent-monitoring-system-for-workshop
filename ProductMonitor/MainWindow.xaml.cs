using ProductMonitor.Models;
using ProductMonitor.OpCommand;
using ProductMonitor.Usercontrol;
using ProductMonitor.ViewModels;
using ProductMonitor.Views;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ProductMonitor
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowVM _viewModel;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _viewModel = new MainWindowVM();
                DataContext = _viewModel;
            }
            catch (Exception ex)
            {
                HandleFatalError("初始化失败", ex);
            }
        }

        // 移除 OnContentRendered，因为初始化已移到 MainWindowVM 构造函数
        /*
        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            try
            {
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                ShowWarning("数据加载失败", ex.Message);
            }
        }
        */

        #region 事件处理方法
        private void BtnMin_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion

        #region 命令方法
        private void Goback()
        {
            AnimateControlTransition(new MonitorUC1(), 50, 0);
            _viewModel.MonitorUC1 = new MonitorUC1();
        }

        private async Task ShowDTAsync()
        {
            const int timeoutSeconds = 10;
            var sw = Stopwatch.StartNew();

            while (_viewModel.JiTailist.Count == 0)
            {
                if (sw.Elapsed.TotalSeconds > timeoutSeconds)
                {
                    ShowWarning("超时", "未检测到机器数据");
                    return;
                }
                await Task.Delay(500);
            }

            var workshopDetail = new WorkShopDT();
            AnimateControlTransition(workshopDetail, 50, 0);
            _viewModel.MonitorUC1 = workshopDetail;
        }
        #endregion

        #region UI辅助方法
        private void AnimateControlTransition(UserControl control, double fromY, double toY)
        {
            control.Margin = new Thickness(0, fromY, 0, -fromY);
            control.Opacity = fromY == 0 ? 0 : 1;

            var thicknessAnim = new ThicknessAnimation(
                new Thickness(0, fromY, 0, -fromY),
                new Thickness(0, toY, 0, 0),
                TimeSpan.FromMilliseconds(400))
            {
                AccelerationRatio = 0.3
            };

            var opacityAnim = new DoubleAnimation(
                fromY == 0 ? 0 : 1,
                toY == 0 ? 1 : 0,
                TimeSpan.FromMilliseconds(400));

            Storyboard.SetTarget(thicknessAnim, control);
            Storyboard.SetTarget(opacityAnim, control);
            Storyboard.SetTargetProperty(thicknessAnim, new PropertyPath("Margin"));
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

            new Storyboard { Children = { thicknessAnim, opacityAnim } }.Begin();
        }

        private void HandleFatalError(string title, Exception ex)
        {
            Debug.WriteLine($"致命错误: {ex}");
            MessageBox.Show($"{title}: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }

        private void ShowWarning(string title, string message)
        {
            Debug.WriteLine($"{title}: {message}");
            MessageBox.Show(message, title,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        #endregion

        #region 命令属性
        public Command ShowDTCommand => new Command(async () => await ShowDTAsync());
        public Command GobackCommand => new Command(Goback);
        public Command ShowSettingsCommand => new Command(ShowSettings);

        private void ShowSettings()
        {
            new SettingWin { Owner = this }.ShowDialog();
        }
        #endregion
    }
}