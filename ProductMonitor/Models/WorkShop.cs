using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductMonitor.Models
{
    class WorkShop
    {
        public string WorkshopName { get; set; }
        public int WorkingCount { get; set; }
        public int WaitingCount { get; set; }
        public int StopCount { get; set; }
        public int WrongCount { get; set; }
        public int TotalCount 
        {
            get
            {
                return WorkingCount + WaitingCount + StopCount+WrongCount;
            }
 }
    }
}
