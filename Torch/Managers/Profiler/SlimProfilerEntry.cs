using System.Diagnostics;
using System.Threading;
using NLog;

namespace Torch.Managers.Profiler
{
    public class SlimProfilerEntry
    {
        private readonly FatProfilerEntry _fat;
        private readonly Stopwatch _updateWatch = new Stopwatch();
        
        public double UpdateTime { get; private set; } = 0;

        private int _watchStarts;

        internal SlimProfilerEntry() : this(null)
        {
        }

        internal SlimProfilerEntry(FatProfilerEntry fat)
        {
            _fat = fat;
        }

        internal void Start()
        {
            if (Interlocked.Add(ref _watchStarts, 1) == 1)
            {
                _fat?.Start();
                _updateWatch.Start();
            }
        }

        internal void Stop()
        {
            if (Interlocked.Add(ref _watchStarts, -1) == 0)
            {
                _updateWatch.Stop();
                _fat?.Stop();
            }
        }

        internal void Rotate()
        {
            UpdateTime = _updateWatch.Elapsed.TotalSeconds / ProfilerData.RotateInterval;
            _updateWatch.Reset();
            Debug.Assert(_watchStarts == 0, "A watch wasn't stopped");
            _watchStarts = 0;
        }
    }
}