using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Sandbox;
using Sandbox.ModAPI;
using Torch.API;
using Torch.API.Plugins;
using Torch.Commands;
using Torch.Managers;
using Torch.Updater;
using VRage.Plugins;
using VRage.Collections;
using VRage.Library.Collections;

namespace Torch.Managers
{
    public class PluginManager : IPluginManager
    {
        private readonly ITorchBase _torch;
        private static Logger _log = LogManager.GetLogger(nameof(PluginManager));
        public readonly string PluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        public List<ITorchPlugin> Plugins { get; } = new List<ITorchPlugin>();

        public float LastUpdateMs => _lastUpdateMs;
        private volatile float _lastUpdateMs;

        public event Action<List<ITorchPlugin>> PluginsLoaded;

        public PluginManager(ITorchBase torch)
        {
            _torch = torch;

            if (!Directory.Exists(PluginDir))
                Directory.CreateDirectory(PluginDir);
        }

        /// <summary>
        /// Updates loaded plugins in parallel.
        /// </summary>
        public void UpdatePlugins()
        {
            var s = Stopwatch.StartNew();
            foreach (var plugin in Plugins)
                plugin.Update();
            s.Stop();
            _lastUpdateMs = (float)s.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// Unloads all plugins.
        /// </summary>
        public void DisposePlugins()
        {
            foreach (var plugin in Plugins)
                plugin.Dispose();

            Plugins.Clear();
        }

        private void DownloadPlugins()
        {
            _log.Info("Downloading plugins");
            var updater = new PluginUpdater(this);

            var folders = Directory.GetDirectories(PluginDir);
            var taskList = new List<Task>();
            if (_torch.Config.RedownloadPlugins)
                _log.Warn("Force downloading all plugins because the RedownloadPlugins flag is set in the config");

            foreach (var folder in folders)
            {
                var manifestPath = Path.Combine(folder, "manifest.xml");
                if (!File.Exists(manifestPath))
                {
                    _log.Info($"No manifest in {folder}, skipping");
                    continue;
                }

                _log.Info($"Checking for updates for {folder}");
                var manifest = PluginManifest.Load(manifestPath);
                taskList.Add(updater.CheckAndUpdate(manifest, _torch.Config.RedownloadPlugins));
            }

            Task.WaitAll(taskList.ToArray());
            _torch.Config.RedownloadPlugins = false;
        }

        /// <summary>
        /// Loads and creates instances of all plugins in the <see cref="PluginDir"/> folder.
        /// </summary>
        public void Init()
        {
            var commands = ((TorchBase)_torch).Commands;

            if (_torch.Config.AutomaticUpdates)
                DownloadPlugins();
            else
                _log.Warn("Automatic plugin updates are disabled.");

            _log.Info("Loading plugins");
            var dlls = Directory.GetFiles(PluginDir, "*.dll", SearchOption.AllDirectories);
            foreach (var dllPath in dlls)
            {
                _log.Debug($"Loading plugin {dllPath}");
                var asm = Assembly.UnsafeLoadFrom(dllPath);

                foreach (var type in asm.GetExportedTypes())
                {
                    if (type.GetInterfaces().Contains(typeof(ITorchPlugin)))
                    {
                        try
                        {
                            var plugin = (TorchPluginBase)Activator.CreateInstance(type);
                            if (plugin.Id == default(Guid))
                                throw new TypeLoadException($"Plugin '{type.FullName}' is missing a {nameof(PluginAttribute)}");

                            _log.Info($"Loading plugin {plugin.Name} ({plugin.Version})");
                            plugin.StoragePath = new FileInfo(asm.Location).Directory.FullName;
                            Plugins.Add(plugin);

                            commands.RegisterPluginCommands(plugin);
                        }
                        catch (Exception e)
                        {
                            _log.Error($"Error loading plugin '{type.FullName}'");
                            _log.Error(e);
                            throw;
                        }
                    }
                }
            }

            Plugins.ForEach(p => p.Init(_torch));
            PluginsLoaded?.Invoke(Plugins);
        }

        public IEnumerator<ITorchPlugin> GetEnumerator()
        {
            return Plugins.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
