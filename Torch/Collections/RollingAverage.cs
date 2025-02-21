using System.Linq;

namespace Torch.Collections
{
    public class RollingAverage
    {
        private readonly double[] _array;
        private int _idx;
        private bool _full;

        public RollingAverage(int size)
        {
            _array = new double[size];
        }

        /// <summary>
        /// Adds a new value and removes the oldest if necessary.
        /// </summary>
        /// <param name="value"></param>
        public void Add(double value)
        {
            if (_idx >= _array.Length - 1)
            {
                _full = true;
                _idx = 0;
            }

            _array[_idx] = value;
            _idx++;
        }

        public double GetAverage()
        {
            return _array.Sum() / (_full ? _array.Length : (_idx + 1));
        }

        /// <summary>
        /// Resets the rolling average.
        /// </summary>
        public void Clear()
        {
            _idx = 0;
            _full = false;
        }

        public static implicit operator double(RollingAverage avg)
        {
            return avg.GetAverage();
        }
    }
}
