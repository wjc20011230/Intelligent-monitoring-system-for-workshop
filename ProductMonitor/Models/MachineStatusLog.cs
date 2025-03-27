using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProductMonitor.Models
{
    public class MachineStatusLog : INotifyPropertyChanged
    {
        private string _machineName;
        private string _status;
        private DateTime _timestamp;
        private int _planCount;
        private int _finishedCount;

        public event PropertyChangedEventHandler PropertyChanged;

        public string MachineName
        {
            get => _machineName;
            set { _machineName = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public int PlanCount
        {
            get => _planCount;
            set { _planCount = value; OnPropertyChanged(); }
        }

        public int FinishedCount
        {
            get => _finishedCount;
            set { _finishedCount = value; OnPropertyChanged(); }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}