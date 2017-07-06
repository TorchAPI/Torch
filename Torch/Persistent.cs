using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Torch
{
    /// <summary>
    /// Simple class that manages saving <see cref="Persistent{T}.Data"/> to disk using JSON serialization.
    /// Can automatically save on changes by implementing <see cref="INotifyPropertyChanged"/> in the data class.
    /// </summary>
    /// <typeparam name="T">Data class type</typeparam>
    public sealed class Persistent<T> : IDisposable where T : new()
    {
        public string Path { get; set; }
        public T Data { get; private set; }
        private Timer _saveTimer;

        ~Persistent()
        {
            Dispose();
        }

        public Persistent(string path, T data = default(T))
        {
            _saveTimer = new Timer(Callback);
            Path = path;
            Data = data;
            if (Data is INotifyPropertyChanged npc)
                npc.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _saveTimer.Change(5000, -1);
        }

        private void Callback(object state)
        {
            Save();
        }

        public void Save(string path = null)
        {
            if (path == null)
                path = Path;

            using (var f = File.Create(path))
            {
                var writer = new StreamWriter(f);
                writer.Write(JsonConvert.SerializeObject(Data, Formatting.Indented));
                writer.Flush();
            }
        }

        public static Persistent<T> Load(string path, bool saveIfNew = true)
        {
            var config = new Persistent<T>(path, new T());

            if (File.Exists(path))
            {
                using (var f = File.OpenRead(path))
                {
                    var reader = new StreamReader(f);
                    config.Data = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                }
            }
            else if (saveIfNew)
            {
                config.Save(path);
            }

            return config;
        }

        public void Dispose()
        {
            try
            {
                if (Data is INotifyPropertyChanged npc)
                    npc.PropertyChanged -= OnPropertyChanged;
                _saveTimer.Dispose();
                Save();
            }
            catch
            {
                // ignored
            }
        }
    }

}
