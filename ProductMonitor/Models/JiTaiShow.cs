using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProductMonitor.Models
{
    public class JiTaiShow : INotifyPropertyChanged, ICloneable
    {
        private string _machinename;
        private string _status;
        private int _planCount;
        private int _finishedCount;
        private string _orderNo;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Machinename
        {
            get => _machinename;
            set { _machinename = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public int PlanCount
        {
            get => _planCount;
            set
            {
                _planCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Percent)); // 更新 Percent
            }
        }

        public int FinishedCount
        {
            get => _finishedCount;
            set
            {
                _finishedCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Percent)); // 更新 Percent
            }
        }

        public string OrderNo
        {
            get => _orderNo;
            set { _orderNo = value; OnPropertyChanged(); }
        }

        // 添加 Percent 属性，计算任务完成百分比
        public double Percent
        {
            get => PlanCount > 0 ? (double)FinishedCount / PlanCount * 100 : 0;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}