using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Client.Threading
{
    public class ApplicationThread
    {
        public System.Threading.Thread Thread;

        private bool StopThread = false;
        public int Interval = 100;
        public event Action<object> OnRun;

        public ApplicationThread(int Interval)
        {
            this.Interval = Interval;
            Thread = new System.Threading.Thread(Run);
        }
        public void Start(object parameter)
        {
            Thread.Start(parameter);
        }
        public void Join()
        {
            Thread.Join();
        }
        public void Stop()
        {
            StopThread = true;
        }
        private void Run(object parameter)
        {
            while (!StopThread)
            {
                if (OnRun != null)
                    OnRun(parameter);

                System.Threading.Thread.Sleep(Interval);
            }
        }
    }
}
