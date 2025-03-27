using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ProductMonitor.Usercontrol
{
    public partial class MonitorUC1 : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<ImageItem> Images { get; set; } = new ObservableCollection<ImageItem>();
        private ImageItem _selectedImage;
        public ImageItem SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                OnPropertyChanged(nameof(SelectedImage));
            }
        }

        private DispatcherTimer _timer;
        private int _currentIndex = 0;

        public MonitorUC1()
        {
            InitializeComponent();

            Images.Add(new ImageItem { Source = new Uri("pack://application:,,,/Resource/images/show1.png"), IsSelected = true });
            Images.Add(new ImageItem { Source = new Uri("pack://application:,,,/Resource/images/show2.jpg") });
            Images.Add(new ImageItem { Source = new Uri("pack://application:,,,/Resource/images/xiaoyan.png") });
            SelectedImage = Images[0];

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _currentIndex = (_currentIndex + 1) % Images.Count;
            UpdateSelectedImage();
        }

        private void UpdateSelectedImage()
        {
            foreach (var item in Images) item.IsSelected = false;
            Images[_currentIndex].IsSelected = true;
            SelectedImage = Images[_currentIndex];
        }

        private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ImageItem item)
            {
                _currentIndex = Images.IndexOf(item);
                UpdateSelectedImage();
                _timer.Stop();
                _timer.Start();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ImageItem : INotifyPropertyChanged
    {
        private Uri _source;
        private bool _isSelected;

        public Uri Source
        {
            get => _source;
            set { _source = value; OnPropertyChanged(nameof(Source)); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Brushes.White : Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}