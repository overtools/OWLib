using System;
using System.ComponentModel;

namespace TankLib.CASC.Helpers {
    /// <summary>Extended BackgroundWorker</summary>
    public class BackgroundWorkerEx : BackgroundWorker {
        private int _lastProgressPercentage;

        public BackgroundWorkerEx() {
            WorkerReportsProgress = true;
            WorkerSupportsCancellation = true;
        }

        protected override void OnDoWork(DoWorkEventArgs e) {
            try {
                base.OnDoWork(e);
            } catch (OperationCanceledException) {
                e.Cancel = true;
            }
        }

        public new void ReportProgress(int percentProgress) {
            if (CancellationPending)
                throw new OperationCanceledException();

            if (IsBusy && percentProgress > _lastProgressPercentage)
                base.ReportProgress(percentProgress);

            _lastProgressPercentage = percentProgress;
        }

        public new void ReportProgress(int percentProgress, object userState) {
            if (CancellationPending)
                throw new OperationCanceledException();

            if (IsBusy)
                base.ReportProgress(percentProgress, userState);

            _lastProgressPercentage = percentProgress;
        }
    }
}