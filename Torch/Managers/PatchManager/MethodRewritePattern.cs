using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Torch.Managers.PatchManager
{
    /// <summary>
    /// Defines the different components used to rewrite a method.
    /// </summary>
    public class MethodRewritePattern
    {
        /// <summary>
        /// Sorts methods so that their <see cref="PatchPriorityAttribute"/> priority is in descending order.  Assumes priority zero if no attribute exists.
        /// </summary>
        private class MethodPriorityCompare : Comparer<MethodInfo>
        {
            internal static readonly MethodPriorityCompare Instance = new MethodPriorityCompare();

            public override int Compare(MethodInfo x, MethodInfo y)
            {
                return -(x?.GetCustomAttribute<PatchPriorityAttribute>()?.Priority ?? 0).CompareTo(
                    y?.GetCustomAttribute<PatchPriorityAttribute>()?.Priority ?? 0);
            }
        }

        /// <summary>
        /// Stores an set of methods according to a certain order.
        /// </summary>
        public class MethodRewriteSet : IEnumerable<MethodInfo>
        {
            private readonly MethodRewriteSet _backingSet;
            private bool _sortDirty = false;
            private readonly List<MethodInfo> _backingList = new List<MethodInfo>();

            private int _hasChanges = 0;

            internal bool HasChanges(bool reset = false)
            {
                if (reset)
                    return Interlocked.Exchange(ref _hasChanges, 0) != 0;
                return _hasChanges != 0;
            }

            /// <summary>
            /// </summary>
            /// <param name="backingSet">The set to track changes on</param>
            internal MethodRewriteSet(MethodRewriteSet backingSet)
            {
                _backingSet = backingSet;
            }

            /// <summary>
            /// Adds the given method to this set if it doesn't already exist in the tracked set and this set.
            /// </summary>
            /// <param name="m">Method to add</param>
            /// <returns>true if added</returns>
            public bool Add(MethodInfo m)
            {
                if (!m.IsStatic)
                    throw new ArgumentException("Patch methods must be static");
                if (_backingSet != null && !_backingSet.Add(m))
                    return false;
                if (_backingList.Contains(m))
                    return false;
                _sortDirty = true;
                Interlocked.Exchange(ref _hasChanges, 1);
                _backingList.Add(m);
                return true;
            }

            /// <summary>
            /// Removes the given method from this set, and from the tracked set if it existed in this set.
            /// </summary>
            /// <param name="m">Method to remove</param>
            /// <returns>true if removed</returns>
            public bool Remove(MethodInfo m)
            {
                if (_backingList.Remove(m))
                {
                    _sortDirty = true;
                    Interlocked.Exchange(ref _hasChanges, 1);
                    return _backingSet == null || _backingSet.Remove(m);
                }
                return false;
            }

            /// <summary>
            /// Removes all methods from this set, and their matches in the tracked set.
            /// </summary>
            public void RemoveAll()
            {
                foreach (var k in _backingList)
                    _backingSet?.Remove(k);
                _backingList.Clear();
                _sortDirty = true;
                Interlocked.Exchange(ref _hasChanges, 1);
            }

            /// <summary>
            /// Gets the number of methods stored in this set.
            /// </summary>
            public int Count => _backingList.Count;

            /// <summary>
            /// Gets an ordered enumerator over this set
            /// </summary>
            /// <returns></returns>
            public IEnumerator<MethodInfo> GetEnumerator()
            {
                CheckSort();
                return _backingList.GetEnumerator();
            }

            /// <summary>
            /// Gets an ordered enumerator over this set
            /// </summary>
            /// <returns></returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                CheckSort();
                return _backingList.GetEnumerator();
            }

            private void CheckSort()
            {
                if (!_sortDirty)
                    return;
                var tmp = _backingList.ToArray();
                MergeSort(tmp, _backingList, MethodPriorityCompare.Instance, 0, _backingList.Count);
                _sortDirty = false;
            }

            private static void MergeSort<T>(IList<T> src, IList<T> dst, Comparer<T> comparer, int left, int right)
            {
                if (left + 1 >= right)
                    return;
                var mid = (left + right) / 2;
                MergeSort<T>(dst, src, comparer, left, mid);
                MergeSort<T>(dst, src, comparer, mid, right);
                for (int i = left, j = left, k = mid; i < right; i++)
                    if ((k >= right || j < mid) && comparer.Compare(src[j], src[k]) <= 0)
                        dst[i] = src[j++];
                    else
                        dst[i] = src[k++];
            }
        }

        /// <summary>
        /// Methods run before the original method is run.  If they return false the original method is skipped.
        /// </summary>
        public MethodRewriteSet Prefixes { get; }
        /// <summary>
        /// Methods capable of accepting one <see cref="IEnumerable{MsilInstruction}"/> and returing another, modified.
        /// </summary>
        public MethodRewriteSet Transpilers { get; }
        /// <summary>
        /// Methods capable of accepting one <see cref="IEnumerable{MsilInstruction}"/> and returing another, modified.
        /// Runs after prefixes, suffixes, and normal transpilers are applied.
        /// </summary>
        public MethodRewriteSet PostTranspilers { get; }
        /// <summary>
        /// Methods run after the original method has run.
        /// </summary>
        public MethodRewriteSet Suffixes { get; }

        /// <summary>
        /// Should the resulting MSIL of the transpile operation be printed.
        /// </summary>
        [Obsolete]
        public bool PrintMsil
        {
            get => PrintMode != 0;
            set => PrintMode = PrintModeEnum.Emitted;
        }
        
        private PrintModeEnum _printMsilBacking;
        
        /// <summary>
        /// Types of IL to print to log
        /// </summary>
        public PrintModeEnum PrintMode
        {
            get => _parent?.PrintMode ?? _printMsilBacking;
            set
            {
                if (_parent != null)
                    _parent.PrintMode = value;
                else
                    _printMsilBacking = value;
            }
        }

        [Flags]
        public enum PrintModeEnum
        {
            Original = 1,
            Emitted = 2,
            Patched = 4,
            EmittedReflection = 8
        }
        
        /// <summary>
        /// File to dump the emitted MSIL to.
        /// </summary>
        public string DumpTarget
        {
            get => _parent?.DumpTarget ?? _dumpTargetBacking;
            set
            {
                if (_parent != null)
                    _parent.DumpTarget = value;
                else
                    _dumpTargetBacking = value;
            }
        }
        
        /// <summary>
        /// Types of IL to dump to file
        /// </summary>
        public PrintModeEnum DumpMode
        {
            get => _parent?.DumpMode ?? _dumpTargetMode;
            set
            {
                if (_parent != null)
                    _parent.DumpMode = value;
                else
                    _dumpTargetMode = value;
            }
        }

        private PrintModeEnum _dumpTargetMode;
        private string _dumpTargetBacking;

        private readonly MethodRewritePattern _parent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentPattern">The pattern to track changes on, or null</param>
        public MethodRewritePattern(MethodRewritePattern parentPattern)
        {
            Prefixes = new MethodRewriteSet(parentPattern?.Prefixes);
            Transpilers = new MethodRewriteSet(parentPattern?.Transpilers);
            PostTranspilers = new MethodRewriteSet(parentPattern?.PostTranspilers);
            Suffixes = new MethodRewriteSet(parentPattern?.Suffixes);
            _parent = parentPattern;
        }
    }
}
