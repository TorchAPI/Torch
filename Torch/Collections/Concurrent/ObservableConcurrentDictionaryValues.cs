#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Torch.Collections.Concurrent
{
    /// <summary>
    /// Observable values view for <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// Use the dictionary's Values property for UI binding when value updates must raise change events.
    /// Serialization methods operate on a snapshot of the current values.
    /// </remarks>
    public sealed class ObservableConcurrentDictionaryValues<TKey, TValue> :
        ICollection<TValue>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
        where TKey : notnull
    {
        private readonly ObservableConcurrentDictionary<TKey, TValue> _owner;
        private readonly DispatcherObservableEvent<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventHandler?> _collectionChangedEvent = new();
        private readonly DispatcherObservableEvent<PropertyChangedEventArgs, PropertyChangedEventHandler?> _propertyChangedEvent = new();

        internal ObservableConcurrentDictionaryValues(ObservableConcurrentDictionary<TKey, TValue> owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _owner.CollectionChanged += OwnerCollectionChanged;
            _owner.PropertyChanged += OwnerPropertyChanged;
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => _collectionChangedEvent.Add(value);
            remove => _collectionChangedEvent.Remove(value);
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => _propertyChangedEvent.Add(value);
            remove => _propertyChangedEvent.Remove(value);
        }

        public int Count => _owner.Count;

        public bool IsReadOnly => true;

        public bool Contains(TValue item)
        {
            EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;
            foreach (var value in _owner.Items.Values)
            {
                if (comparer.Equals(value, item))
                    return true;
            }
            return false;
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            foreach (var value in _owner.Items.Values)
            {
                array[arrayIndex++] = value;
            }
        }

        public IEnumerator<TValue> GetEnumerator() => _owner.Items.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

        bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

        void ICollection<TValue>.Clear() => throw new NotSupportedException();

        private void OwnerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                _propertyChangedEvent.Raise(this, e);
            }
            catch
            {
                _propertyChangedEvent.Raise(this, new(nameof(Count)));
            }
        }

        private void OwnerCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (TryTranslateEvent(e, out NotifyCollectionChangedEventArgs? translated))
                {
                    if (translated != null) _collectionChangedEvent.Raise(this, translated);
                    return;
                }
            }
            catch
            {
                // Fall through to reset.
            }

            _collectionChangedEvent.Raise(this, new(NotifyCollectionChangedAction.Reset));
        }

        private static bool TryTranslateEvent(NotifyCollectionChangedEventArgs e, out NotifyCollectionChangedEventArgs? translated)
        {
            translated = null;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (TryGetSingleValue(e.NewItems, out var added))
                    {
                        translated = new(NotifyCollectionChangedAction.Add, added);
                        return true;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (TryGetSingleValue(e.OldItems, out var removed))
                    {
                        translated = new(NotifyCollectionChangedAction.Remove, removed);
                        return true;
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (TryGetSingleValue(e.NewItems, out var newValue) && TryGetSingleValue(e.OldItems, out var oldValue))
                    {
                        translated = new(NotifyCollectionChangedAction.Replace, newValue, oldValue);
                        return true;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    translated = new(NotifyCollectionChangedAction.Reset);
                    return true;
            }

            return false;
        }

        private static bool TryGetSingleValue(IList? items, out TValue? value)
        {
            value = default;
            if (items is not { Count: 1 })
                return false;

            object item = items[0];
            if (item is TValue direct)
            {
                value = direct;
                return true;
            }

            if (item is KeyValuePair<TKey, TValue> kvp)
            {
                value = kvp.Value;
                return true;
            }

            return false;
        }

        private List<TValue> SnapshotItems()
        {
            return new(_owner.Items.Values);
        }

        /// <summary>
        /// Serializes a snapshot of current values to XML.
        /// </summary>
        public string SerializeToXml()
        {
            XmlSerializer serializer = new(typeof(List<TValue>));
            using StringWriter sw = new();
            serializer.Serialize(sw, SnapshotItems());
            return sw.ToString();
        }

        /// <summary>
        /// Deserializes an XML list snapshot.
        /// </summary>
        public static List<TValue>? DeserializeItemsFromXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return null;
            try
            {
                XmlSerializer serializer = new(typeof(List<TValue>));
                using StringReader sr = new(xml);
                return serializer.Deserialize(sr) as List<TValue>;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"XML deserialization failed for {typeof(TValue)}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Serializes a snapshot of current values to JSON.
        /// </summary>
        public string SerializeToJson()
        {
            return JsonConvert.SerializeObject(SnapshotItems());
        }

        /// <summary>
        /// Deserializes a JSON array into a list snapshot.
        /// </summary>
        public static List<TValue>? DeserializeItemsFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<List<TValue>>(json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"JSON deserialization failed for {typeof(TValue)}: {ex.Message}");
                return null;
            }
        }
    }
}
