using System;
using System.Diagnostics;

namespace TankLib.CASC.Helpers {
    /// <summary>Performance tracker</summary>
    public sealed class PerfCounter : IDisposable {
        private readonly Stopwatch _sw;
        private readonly string _name;

        public PerfCounter(string name) {
            _name = name;
            _sw = Stopwatch.StartNew();
        }

        public void Dispose() {
            _sw.Stop();
        }
    }
}