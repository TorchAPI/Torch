using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Newtonsoft.Json;

#nullable enable

namespace Torch.Collections.Concurrent
{
    /// <summary>
    /// Thread-safe observable dictionary for UI binding and background updates.
    /// Replaces MtObservableSortedDictionary for unsorted dictionary needs.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    [XmlRoot("ObservableConcurrentDictionary")]
    public class ObservableConcurrentDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dict = new();
        private readonly ObservableConcurrentDictionaryValues<TKey, TValue> _valuesView;
        private readonly DispatcherObservableEvent<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventHandler?> _collectionChangedEvent = new();
        private readonly DispatcherObservableEvent<PropertyChangedEventArgs, PropertyChangedEventHandler?> _propertyChangedEvent = new();

        public ObservableConcurrentDictionary()
        {
            _valuesView = new(this);
        }

        // Used for serializing since XmlSerializer can't handle ConcurrentDictionary
        [XmlIgnore]
        public IReadOnlyDictionary<TKey, TValue> Items => _dict;

        [XmlArray("Entries")]
        [XmlArrayItem("Entry")]
        public List<SerializableKeyValuePair<TKey, TValue>>? XmlEntries
        {
            get
            {
                // ConcurrentDictionary is already thread-safe for enumeration, no lock needed
                List<SerializableKeyValuePair<TKey, TValue>> list = new();
                list.AddRange(_dict.Select(kvp => new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value)));
                return list;
            }
            set
            {
                _dict.Clear();
                if (value == null)
                    return;
                foreach (SerializableKeyValuePair<TKey, TValue> kvp in value)
                    _dict[kvp.Key] = kvp.Value;
            }
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

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            _propertyChangedEvent.Raise(this, new(propertyName));
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            _collectionChangedEvent.Raise(this, e);
        }

        /// <summary>
        /// Attempts to add the specified key and value to the dictionary.
        /// </summary>
        public bool TryAdd(TKey key, TValue value)
        {
            if (!_dict.TryAdd(key, value)) return false;

            OnCollectionChanged(new(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
            OnPropertyChanged(nameof(Count));
            return true;
        }

        /// <summary>
        /// Adds a key/value pair to the dictionary by generating a value if the key does not exist,
        /// or updates an existing key with a new value.
        /// </summary>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            bool wasAdded = false;
            TValue? capturedOldValue = default;
            bool hadOldValue = false;

            TValue newValue = _dict.AddOrUpdate(
                key,
                k =>
                {
                    wasAdded = true;
                    return addValueFactory(k);
                },
                (k, oldVal) =>
                {
                    capturedOldValue = oldVal;
                    hadOldValue = true;
                    return updateValueFactory(k, oldVal);
                });

            if (wasAdded)
            {
                OnCollectionChanged(new(NotifyCollectionChangedAction.Add, new List<KeyValuePair<TKey, TValue>> { new(key, newValue) }));
                OnPropertyChanged(nameof(Count));
            }
            else if (hadOldValue)
            {
                OnCollectionChanged(new(NotifyCollectionChangedAction.Replace,
                    new List<KeyValuePair<TKey, TValue>> { new(key, newValue) },
                    new List<KeyValuePair<TKey, TValue>> { new(key, capturedOldValue!) }));
            }

            return newValue;
        }

        /// <summary>
        /// Adds a key/value pair to the dictionary if the key does not already exist,
        /// or updates a key/value pair in the dictionary if the key already exists.
        /// </summary>
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            bool wasAdded = false;
            TValue? capturedOldValue = default;
            bool hadOldValue = false;

            TValue newValue = _dict.AddOrUpdate(
                key,
                k =>
                {
                    wasAdded = true;
                    return addValue;
                },
                (k, oldVal) =>
                {
                    capturedOldValue = oldVal;
                    hadOldValue = true;
                    return updateValueFactory(k, oldVal);
                });

            if (wasAdded)
            {
                OnCollectionChanged(new(NotifyCollectionChangedAction.Add, new List<KeyValuePair<TKey, TValue>> { new(key, newValue) }));
                OnPropertyChanged(nameof(Count));
            }
            else if (hadOldValue)
            {
                OnCollectionChanged(new(NotifyCollectionChangedAction.Replace,
                    new List<KeyValuePair<TKey, TValue>> { new(key, newValue) },
                    new List<KeyValuePair<TKey, TValue>> { new(key, capturedOldValue!) }));
            }

            return newValue;
        }

        /// <summary>
        /// Attempts to remove the value with the specified key from the dictionary.
        /// </summary>
        public bool TryRemove(TKey key, out TValue value)
        {
            if (!_dict.TryRemove(key, out value)) return false;

            // Raise proper Remove event
            if (value != null)
                OnCollectionChanged(new(NotifyCollectionChangedAction.Remove, value));

            OnPropertyChanged(nameof(Count));
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

        /// <summary>
        /// Clears all the elements from the dictionary.
        /// </summary>
        public void Clear()
        {
            _dict.Clear();
            OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(nameof(Count));
        }

        /// <summary>
        /// Attempts to update the value associated with the specified key in the dictionary.
        /// </summary>
        public bool TryUpdate(TKey key, TValue newValue)
        {
            if (!_dict.TryGetValue(key, out TValue oldValue) || !_dict.TryUpdate(key, newValue, oldValue)) return false;
            OnCollectionChanged(new(NotifyCollectionChangedAction.Replace,
                new KeyValuePair<TKey, TValue>(key, newValue),
                new KeyValuePair<TKey, TValue>(key, oldValue)));
            return true;
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key in the dictionary.
        /// </summary>
        public TValue this[TKey key]
        {
            get => _dict[key];
            set
            {
                _dict[key] = value;
                OnCollectionChanged(new(NotifyCollectionChangedAction.Replace,
                    new KeyValuePair<TKey, TValue>(key, value)));
            }
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

        /// <summary>
        /// Gets the number of key/value pairs contained in the dictionary.
        /// </summary>
        public int Count => _dict.Count;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // --- IDictionary<TKey, TValue> implementations ---
        public void Add(TKey key, TValue value)
        {
            if (!TryAdd(key, value))
                throw new ArgumentException($"Key {key} already exists", nameof(key));
        }

        public bool Remove(TKey key)
        {
            return TryRemove(key, out _);
        }

        public ICollection<TKey> Keys => _dict.Keys;
        public ICollection<TValue> Values => _valuesView;

        // --- ICollection<KeyValuePair<TKey, TValue>> explicit implementations ---
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)this).Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            int i = arrayIndex;
            foreach (var kvp in _dict)
            {
                array[i++] = kvp;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (((ICollection<KeyValuePair<TKey, TValue>>)this).Contains(item))
                return ((IDictionary<TKey, TValue>)this).Remove(item.Key);
            return false;
        }

        // --- XML Serialization (optional) ---
        /// <summary>
        /// Serializes the current instance of the dictionary to an XML string representation.
        /// </summary>
        public string SerializeToXml()
        {
            XmlSerializer serializer = new(typeof(ObservableConcurrentDictionary<TKey, TValue>));
            using StringWriter sw = new();
            serializer.Serialize(sw, this);
            return sw.ToString();
        }

        /// <summary>
        /// Deserializes an XML string into an instance of <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        public static ObservableConcurrentDictionary<TKey, TValue>? DeserializeFromXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return null;
            try
            {
                XmlSerializer serializer = new(typeof(ObservableConcurrentDictionary<TKey, TValue>));
                using StringReader sr = new(xml);
                return (ObservableConcurrentDictionary<TKey, TValue>?)serializer.Deserialize(sr);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"XML deserialization failed for {typeof(TKey)}→{typeof(TValue)}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Serializes the current instance of the dictionary to a JSON string representation.
        /// </summary>
        public string SerializeToJson()
        {
            return JsonConvert.SerializeObject(_dict);
        }

        /// <summary>
        /// Deserializes a JSON string into an ObservableConcurrentDictionary.
        /// </summary>
        public static ObservableConcurrentDictionary<TKey, TValue>? DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                Dictionary<TKey, TValue> items = JsonConvert.DeserializeObject<Dictionary<TKey, TValue>>(json);
                if (items == null)
                    return null;
                var result = new ObservableConcurrentDictionary<TKey, TValue>();
                foreach (var kvp in items)
                    result._dict[kvp.Key] = kvp.Value;
                return result;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"JSON deserialization failed for {typeof(TKey)}→{typeof(TValue)}: {ex.Message}");
                return null;
            }
        }

        // ---- Nested type for XML ----
        public class SerializableKeyValuePair<K, V>
        {
            public K Key { get; }
            public V Value { get; }

            public SerializableKeyValuePair() : this(default!, default!) { }

            public SerializableKeyValuePair(K key, V value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}
