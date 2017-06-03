using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Torch
{
    /// <summary>
    /// Class that manages saving <see cref="T"/> to disk using JSON serialization.
    /// </summary>
    /// <typeparam name="T">Data class</typeparam>
    public class Persistent<T> : IDisposable where T : new()
    {
        [JsonIgnore]
        public string Path { get; set; }
        public T Data { get; private set; } = new T();

        ~Persistent()
        {
            Dispose();
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
            var config = new Persistent<T> { Path = path };

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
            Save();
        }
    }

}
