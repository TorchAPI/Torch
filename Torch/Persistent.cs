using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using NLog;

namespace Torch
{
    /// <summary>
    /// Simple class that manages saving <see cref="Persistent{T}.Data"/> to disk using XML serialization.
    /// Can automatically save on changes by implementing <see cref="INotifyPropertyChanged"/> in the data class.
    /// </summary>
    /// <typeparam name="T">Data class type</typeparam>
    public sealed class Persistent<T> : IDisposable where T : new()
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        public string Path { get; set; }
        private T _data;
        public T Data
        {
            get => _data;
            private set
            {
                if (_data is INotifyPropertyChanged npc1)
                    npc1.PropertyChanged -= OnPropertyChanged;
                _data = value;
                if (_data is INotifyPropertyChanged npc2)
                    npc2.PropertyChanged += OnPropertyChanged;
            }
        }

        ~Persistent()
        {
            Dispose();
        }

        public Persistent(string path, T data = default(T))
        {
            Path = path;
            Data = data;
        }
        
        private Timer _saveConfigTimer;

        private void SaveAsync()
        {
            if (_saveConfigTimer == null)
            {
                _saveConfigTimer = new Timer((x) => Save());
            }

            _saveConfigTimer.Change(1000, -1);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SaveAsync();
        }

        public void Save(string path = null)
        {
            if (path == null)
                path = Path;

            var ser = new XmlSerializer(typeof(T));
            using (var f = File.CreateText(path))
            {
                ser.Serialize(f, Data);
            }
        }

        public static Persistent<T> Load(string path, bool saveIfNew = true)
        {
            Persistent<T> config = null;

            if (File.Exists(path))
            {
                try
                {
                    var ser = new XmlSerializer(typeof(T));
                    using (var f = File.OpenText(path))
                    {
                        config = new Persistent<T>(path, (T)ser.Deserialize(f));
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    config = null;
                }
            }
            if (config == null)
                config = new Persistent<T>(path, new T());
            if (!File.Exists(path) && saveIfNew)
                config.Save();

            return config;
        }

        public void Dispose()
        {
            try
            {
                if (Data is INotifyPropertyChanged npc)
                    npc.PropertyChanged -= OnPropertyChanged;
                _saveConfigTimer?.Dispose();
                Save();
            }
            catch
            {
                // ignored
            }
        }
    }

}
