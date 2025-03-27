//using System;
//using System.Threading.Tasks;

//namespace ProductMonitor.ViewModels
//{
//    public static class TaskExtensions
//    {
//        public static async Task<T> TimeoutAfter<T>(this Task<T> task, int millisecondsTimeout)
//        {
//            if (await Task.WhenAny(task, Task.Delay(millisecondsTimeout)) == task)
//            {
//                return await task;
//            }
//            throw new TimeoutException("The operation has timed out.");
//        }
//    }
//}