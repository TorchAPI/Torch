using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Torch
{
    /// <summary>
    /// Provides a method to notify an observer of changes to an object's properties.
    /// </summary>
    public abstract class ViewModel : INotifyPropertyChanged, INotifyCollectionChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler collectionChanged = CollectionChanged;
            if (collectionChanged != null)
                foreach (NotifyCollectionChangedEventHandler nh in collectionChanged.GetInvocationList())
                {
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

        protected virtual void SetValue<T>(ref T backingField, T value, [CallerMemberName] string propName = "")
        {
            if (backingField != null && backingField.Equals(value))
                return;

            backingField = value;
            // ReSharper disable once ExplicitCallerInfoArgument
            OnPropertyChanged(propName);
        }

        /// <summary>
        /// Fires PropertyChanged for all properties.
        /// </summary>
        public void RefreshModel()
        {
            foreach (var propName in GetType().GetProperties().Select(x => x.Name))
                // ReSharper disable once ExplicitCallerInfoArgument
                OnPropertyChanged(propName);
        }
    }
}
