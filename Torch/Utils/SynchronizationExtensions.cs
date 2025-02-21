using System;
using System.Threading;

namespace Torch.Utils
{
    /// <summary>
    /// Extension functions related to synchronization
    /// </summary>
    public static class SynchronizationExtensions
    {

        /// <summary>
        /// Acquires a RAII view of the lock in read mode.
        /// </summary>
        /// <param name="lck">Lock</param>
        /// <returns>RAII token</returns>
        public static IDisposable ReadUsing(this ReaderWriterLockSlim lck)
        {
            return new ReaderWriterLockSlimReadToken(lck);
        }

        /// <summary>
        /// Acquires a RAII view of the lock in upgradable read mode.
        /// </summary>
        /// <param name="lck">Lock</param>
        /// <returns>RAII token</returns>
        public static IDisposable UpgradableReadUsing(this ReaderWriterLockSlim lck)
        {
            return new ReaderWriterLockSlimUpgradableToken(lck);
        }

        /// <summary>
        /// Acquires a RAII view of the lock in write mode.
        /// </summary>
        /// <param name="lck">Lock</param>
        /// <returns>RAII token</returns>
        public static IDisposable WriteUsing(this ReaderWriterLockSlim lck)
        {
            return new ReaderWriterLockSlimWriteToken(lck);
        }

        #region Support Structs
        private struct ReaderWriterLockSlimUpgradableToken : IDisposable
        {
            private ReaderWriterLockSlim _lock;

            public ReaderWriterLockSlimUpgradableToken(ReaderWriterLockSlim lc)
            {
                _lock = lc;
                lc.EnterUpgradeableReadLock();
            }

            public void Dispose()
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        private struct ReaderWriterLockSlimWriteToken : IDisposable
        {
            private ReaderWriterLockSlim _lock;

            public ReaderWriterLockSlimWriteToken(ReaderWriterLockSlim lc)
            {
                _lock = lc;
                lc.EnterWriteLock();
            }

            public void Dispose()
            {
                _lock.ExitWriteLock();
            }
        }

        private struct ReaderWriterLockSlimReadToken : IDisposable
        {
            private ReaderWriterLockSlim _lock;

            public ReaderWriterLockSlimReadToken(ReaderWriterLockSlim lc)
            {
                _lock = lc;
                lc.EnterReadLock();
            }

            public void Dispose()
            {
                _lock.ExitReadLock();
            }
        }
        #endregion
    }
}
