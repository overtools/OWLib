using System;
using System.ComponentModel;

namespace TankLib.CASC.Helpers {
    /// <summary>Reports on progress</summary>
    public class ProgressReportSlave {
        private int _lastProgressPercentage;

        private object @lock = new object();

        public event Action<object, ProgressChangedEventArgs> OnProgress;

        public ProgressReportSlave() {
        }

        public void ReportProgress(int percentProgress)
        {
            lock (@lock)
            {
                OnProgress.Invoke(this, new ProgressChangedEventArgs(percentProgress, null));
            }
        }

        public void ReportProgress(int percentProgress, object userState)
        {
            lock (@lock)
            {
                OnProgress.Invoke(this, new ProgressChangedEventArgs(percentProgress, userState));
            }
        }
    }
}
