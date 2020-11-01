using System.IO;
using System.Xml.Serialization;
using NLog;

namespace Torch.Server.InfluxDb
{
    /// <summary>
    /// Helper functions to serialize/deserialize config files
    /// </summary>
    internal static class ConfigUtils
    {
        static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Load a config file at given location.
        /// </summary>
        /// <remarks>Search path will be relative to the application bin directory.</remarks>
        /// <remarks>If a config file doesn't exist at specified path,
        /// a new config file will be created at the path using given default config,
        /// and the function will return the default config.</remarks>
        /// <param name="fileName">Name of the config file to deserialize.</param>
        /// <param name="defaultConfig">Default config to save in the disk if a config is not found at specified path.</param>
        /// <typeparam name="T">Type of the config.</typeparam>
        /// <returns>Config deserialized from specified config file. If file is not found, `defaultConfig` will be returned.</returns>
        public static T LoadConfigFile<T>(string fileName, T defaultConfig)
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            var configSerializer = new XmlSerializer(typeof(T));

            if (!File.Exists(fileName))
            {
                _logger.Info($"Generating default config at {configPath}");

                using (var file = File.Create(configPath))
                {
                    configSerializer.Serialize(file, defaultConfig);
                }

                return defaultConfig;
            }

            _logger.Info($"Loading config {configPath}");

            using (var file = File.OpenRead(configPath))
            {
                return (T) configSerializer.Deserialize(file);
            }
        }
    }
}