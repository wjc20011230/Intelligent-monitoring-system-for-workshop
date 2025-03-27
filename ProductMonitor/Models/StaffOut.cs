using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProductMonitor.Models
{
    public class StaffOut : INotifyPropertyChanged
    {
        private string _staffname;
        private string _jt;
        private int _outworkvalue;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Id { get; set; }

        public string Staffname
        {
            get => _staffname;
            set { _staffname = value; OnPropertyChanged(); }
        }

        public string JT
        {
            get => _jt;
            set { _jt = value; OnPropertyChanged(); }
        }

        public int Outworkvalue
        {
            get => _outworkvalue;
            set { _outworkvalue = value; OnPropertyChanged(); OnPropertyChanged(nameof(OT)); }
        }

        public int OT
        {
            get => Outworkvalue * 40 / 100;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}