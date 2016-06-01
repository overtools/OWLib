using System;
using System.Diagnostics;

namespace CASCExplorer
{
    public class PerfCounter : IDisposable
    {
        private Stopwatch _sw;
        private string _name;

        public PerfCounter(string name)
        {
            _name = name;
            _sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _sw.Stop();

            Logger.WriteLine("{0} completed in {1}", _name, _sw.Elapsed);
        }
    }
}
