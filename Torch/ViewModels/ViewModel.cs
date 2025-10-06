using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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


        /// <summary>
        /// Assign a value to the given field and raise PropertyChanged for the caller.
        /// </summary>
        protected virtual void SetValue<T>(ref T field, T value, [CallerMemberName] string propName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            OnPropertyChanged(propName);
        }

        /// <summary>
        /// Assign a value using the given setter and raise PropertyChanged for the caller.
        /// </summary>
        protected virtual void SetValue<T>(Action<T> setter, T value, [CallerMemberName] string propName = "")
        {
            if (setter == null)
                throw new ArgumentNullException(nameof(setter));

            setter.Invoke(value);
            OnPropertyChanged(propName);
        }

        /// <summary>
        /// Fires PropertyChanged for all properties.
        /// </summary>
        public void RefreshModel()
        {
            foreach (var property in GetType().GetProperties())
                OnPropertyChanged(property.Name);
        }
    }
}
