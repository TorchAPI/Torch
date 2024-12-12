using System;
using System.Collections;
using System.Collections.Generic;

namespace Torch.Collections
{
    /// <summary>
    /// Enumerator that transforms from one enumeration into another.
    /// </summary>
    /// <typeparam name="TIn">Input type</typeparam>
    /// <typeparam name="TOut">Output type</typeparam>
    public class TransformEnumerator<TIn,TOut> : IEnumerator<TOut>
    {
        private readonly IEnumerator<TIn> _input;
        private readonly Func<TIn, TOut> _transform;

        /// <summary>
        /// Creates a new transform enumerator with the given transform function
        /// </summary>
        /// <param name="input">Input to proxy enumerator</param>
        /// <param name="transform">Transform function</param>
        public TransformEnumerator(IEnumerator<TIn> input, Func<TIn, TOut> transform)
        {
            _input = input;
            _transform = transform;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _input.Dispose();
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            return _input.MoveNext();
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _input.Reset();
        }

        /// <inheritdoc/>
        public TOut Current => _transform(_input.Current);

        /// <inheritdoc/>
        object IEnumerator.Current => Current;
    }
}
