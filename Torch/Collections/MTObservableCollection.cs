using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Torch
{
    [Obsolete("Use ObservableList<T>.")]
    public class MTObservableCollection<T> : ObservableCollection<T>
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler collectionChanged = CollectionChanged;
            if (collectionChanged != null)
                foreach (var del in collectionChanged.GetInvocationList())
                {
                    var nh = (NotifyCollectionChangedEventHandler)del;
                    var dispObj = nh.Target as DispatcherObject;

                    var dispatcher = dispObj?.Dispatcher;
                    if (dispatcher != null && !dispatcher.CheckAccess())
                    {
                        dispatcher.BeginInvoke(
                            (Action)(() => nh.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))),
                            DispatcherPriority.DataBind);
                        continue;
                    }

                    nh.Invoke(this, e);
                }
        }

        public void Insert<TKey>(T item, Func<T, TKey> selector, IComparer<TKey> comparer)
        {
            var key = selector(item);
            for (var i = 0; i < Count; i++)
            {
                var key2 = selector(Items[i]);
                if (comparer.Compare(key, key2) < 1)
                    continue;

                Insert(i + 1, item);
                return;
            }

            Add(item);
        }

        public void Sort<TKey>(Func<T, TKey> selector, IComparer<TKey> comparer = null)
        {
            List<T> sortedItems;
            if (comparer != null)
                sortedItems = Items.OrderBy(selector, comparer).ToList();
            else
                sortedItems = Items.OrderBy(selector).ToList();

            Items.Clear();
            foreach (var item in sortedItems)
                Add(item);
        }

        public void RemoveWhere(Func<T, bool> condition)
        {
            for (var i = Items.Count - 1; i > 0; i--)
            {
                if (condition(Items[i]))
                    RemoveAt(i);
            }
        }
    }
}
