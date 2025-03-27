using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ProductMonitor.ViewModels;

namespace ProductMonitor.Usercontrol
{
    /// <summary>
    /// WorkShopDT.xaml 的交互逻辑
    /// </summary>
    public partial class WorkShopDT : UserControl
    {
        public WorkShopDT()
        {
            InitializeComponent();
            Loaded += WorkShopDT_Loaded;
        }

        private void WorkShopDT_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowVM vm)
            {
                // 正确通过ViewModel访问JiTailist
                var view = CollectionViewSource.GetDefaultView(vm.JiTailist);
                view?.Refresh();

                // 强制重绘
                ItemsControl.UpdateLayout();
                var scrollViewer = Template.FindName("ScrollViewer", this) as ScrollViewer;
                scrollViewer?.InvalidateVisual();

                Debug.WriteLine($"WorkShopDT Loaded, JiTailist数量: {vm.JiTailist.Count}");
                if (vm.JiTailist.Count > 0)
                {
                    Debug.WriteLine($"数据示例: {vm.JiTailist[0].Machinename}, {vm.JiTailist[0].Status}");
                }
            }
            else
            {
                Debug.WriteLine($"DataContext类型: {DataContext?.GetType()?.Name ?? "null"}");
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            detail.Visibility = Visibility.Visible;
            var thicknessAnim = new ThicknessAnimation(
                new Thickness(0, 50, 0, -50),
                new Thickness(0, 0, 0, 0),
                TimeSpan.FromMilliseconds(400));
            var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400));

            Storyboard.SetTarget(thicknessAnim, detail2);
            Storyboard.SetTarget(opacityAnim, detail2);
            Storyboard.SetTargetProperty(thicknessAnim, new PropertyPath("Margin"));
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

            new Storyboard { Children = { thicknessAnim, opacityAnim } }.Begin();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var thicknessAnim = new ThicknessAnimation(
                new Thickness(0, 0, 0, 0),
                new Thickness(0, 50, 0, -50),
                TimeSpan.FromMilliseconds(400));
            var opacityAnim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));

            Storyboard.SetTarget(thicknessAnim, detail2);
            Storyboard.SetTarget(opacityAnim, detail2);
            Storyboard.SetTargetProperty(thicknessAnim, new PropertyPath("Margin"));
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

            var storyboard = new Storyboard { Children = { thicknessAnim, opacityAnim } };
            storyboard.Completed += (s, args) => detail.Visibility = Visibility.Hidden;
            storyboard.Begin();
        }
    }
}