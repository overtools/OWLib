using System;
using System.ComponentModel;

namespace TankView {
    /// <summary>Reports on progress</summary>
    public class ProgressSlave {
        private readonly object _lock = new object();

        public event Action<object, ProgressChangedEventArgs> OnProgress;

        public void ReportProgress(int percentProgress) {
            lock (_lock) {
                OnProgress?.Invoke(this, new ProgressChangedEventArgs(percentProgress, null));
            }
        }

        public void ReportProgress(int percentProgress, object userState) {
            lock (_lock) {
                OnProgress?.Invoke(this, new ProgressChangedEventArgs(percentProgress, userState));
            }
        }
    }
}
