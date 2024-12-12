using System;
using System.Collections.Generic;

namespace Torch.Collections
{
    /// <summary>
    /// Comparer that uses a delegate to select the key to compare on.
    /// </summary>
    /// <typeparam name="TIn">Input to this comparer</typeparam>
    /// <typeparam name="TCompare">Type of comparison key</typeparam>
    public class TransformComparer<TIn, TCompare> : IComparer<TIn>
    {
        private readonly IComparer<TCompare> _comparer;
        private readonly Func<TIn, TCompare> _selector;

        /// <summary>
        /// Creates a new transforming comparer that uses the given key selector, and the given key comparer.
        /// </summary>
        /// <param name="transform">Key selector</param>
        /// <param name="comparer">Key comparer</param>
        public TransformComparer(Func<TIn, TCompare> transform, IComparer<TCompare> comparer = null)
        {
            _selector = transform;
            _comparer = comparer ?? Comparer<TCompare>.Default;
        }

        /// <inheritdoc/>
        public int Compare(TIn x, TIn y)
        {
            return _comparer.Compare(_selector(x), _selector(y));
        }
    }
}
