using ProductMonitor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProductMonitor.Usercontrol
{
    /// <summary>
    /// RaderUC.xaml 的交互逻辑
    /// </summary>
    public partial class RaderUC : UserControl
    {
        public RaderUC()
        {
            InitializeComponent();
            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            draw();
        }

        //依赖


        public List<RaderModel> ItemSource
        {
            get { return (List<RaderModel>)GetValue(ItemSourceProperty); }
            set { SetValue(ItemSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemSourceProperty =
            DependencyProperty.Register("ItemSource", typeof(List<RaderModel>), typeof(RaderUC));
        //画图方法
        public void draw()
        {
            //判断是否为空
            if (ItemSource == null || ItemSource.Count == 0)
            {
                return;
            }
            //清除画布
            Maincanvas.Children.Clear();
            p1.Points.Clear();
            p2.Points.Clear();
            p3.Points.Clear();
            p4.Points.Clear();
            p5.Points.Clear();

            //调整大小
            double size = Math.Min(RenderSize.Width, RenderSize.Height);
            Rader.Width = size;
            Rader.Height = size;
            //半径
            double BJ = size / 2;
            double step = 360.0 / ItemSource.Count;
            //xy坐标
           
            for (int i = 0; i < ItemSource.Count; i++)
            {
                double x =  (BJ - 20) * Math.Cos(((step * i) - 90) * Math.PI / 180);
                double y =  (BJ - 20) * Math.Sin(((step * i) - 90) * Math.PI / 180);
                p1.Points.Add(new Point (BJ+x, BJ+y));
                p2.Points.Add(new Point(BJ+x*0.75, BJ+y*0.75));
                p3.Points.Add(new Point(BJ+x*0.5, BJ+y*0.5));
                p4.Points.Add(new Point(BJ+x*0.25, BJ+y*0.25));
                //数据多边形
                p5.Points.Add(new Point(BJ + x * ItemSource[i].RaderValue * 0.01, BJ + y * ItemSource[i].RaderValue * 0.01));
                //文字处理
                TextBlock txt = new TextBlock();
                txt.Width = 60;
                txt.FontSize = 9;
                txt.TextAlignment=TextAlignment.Center;
                txt.Text = ItemSource[i].RaderName;
                txt.Foreground = new SolidColorBrush(Color.FromArgb(100,255, 255, 255));
                txt.SetValue(Canvas.LeftProperty, BJ + (BJ - 15) * Math.Cos(((step * i) - 90) * Math.PI / 180) - 30);
                txt.SetValue(Canvas.TopProperty, BJ + (BJ - 10) * Math.Sin(((step * i) - 90) * Math.PI / 180) - 7);
                Maincanvas.Children.Add(txt);
            }
        }
    }
}
