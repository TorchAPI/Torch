using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NLog;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Commands;
using VRage.Collections;

namespace Torch.Managers
{
    /// <inheritdoc />
    public class PluginManager : Manager, IPluginManager
    {
        private static Logger _log = LogManager.GetLogger(nameof(PluginManager));
        public readonly string PluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        [Dependency]
        private UpdateManager _updateManager;
        [Dependency(Optional = true)]
        private ITorchSessionManager _sessionManager;

        /// <inheritdoc />
        public IList<ITorchPlugin> Plugins { get; } = new ObservableList<ITorchPlugin>();

        public event Action<ITorchPlugin> PluginLoaded;
        public event Action<IList<ITorchPlugin>> PluginsLoaded;

        public PluginManager(ITorchBase torchInstance) : base(torchInstance)
        {
            if (!Directory.Exists(PluginDir))
                Directory.CreateDirectory(PluginDir);
        }

        /// <summary>
        /// Updates loaded plugins in parallel.
        /// </summary>
        public void UpdatePlugins()
        {
            foreach (var plugin in Plugins)
                plugin.Update();
        }

        private Action<ITorchPlugin> _attachCommandsHandler = null;

        private void SessionStateChanged(ITorchSession session, TorchSessionState newState)
        {
            var cmdManager = session.Managers.GetManager<CommandManager>();
            if (cmdManager == null)
                return;
            switch (newState)
            {
                case TorchSessionState.Loaded:
                    if (_attachCommandsHandler != null)
                        PluginLoaded -= _attachCommandsHandler;
                    _attachCommandsHandler = (x) => cmdManager.RegisterPluginCommands(x);
                    PluginLoaded += _attachCommandsHandler;
                    foreach (ITorchPlugin plugin in Plugins)
                        cmdManager.RegisterPluginCommands(plugin);
                    break;
                case TorchSessionState.Unloading:
                    if (_attachCommandsHandler != null)
                    {
                        PluginLoaded -= _attachCommandsHandler;
                        _attachCommandsHandler = null;
                    }
                    foreach (ITorchPlugin plugin in Plugins)
                    {
                        // cmdMgr?.UnregisterPluginCommands(plugin);
                    }
                    break;
                case TorchSessionState.Loading:
                case TorchSessionState.Unloaded:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }
        }

        /// <summary>
        /// Prepares the plugin manager for loading.
        /// </summary>
        public override void Attach()
        {
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += SessionStateChanged;
        }

        /// <summary>
        /// Unloads all plugins.
        /// </summary>
        public override void Detach()
        {
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged -= SessionStateChanged;
            foreach (var plugin in Plugins)
                plugin.Dispose();

            Plugins.Clear();
        }

        private void DownloadPlugins()
        {
            var folders = Directory.GetDirectories(PluginDir);
            var taskList = new List<Task>();

            //Copy list because we don't want to modify the config.
            var toDownload = Torch.Config.Plugins.ToList();

            foreach (var folder in folders)
            {
                var manifestPath = Path.Combine(folder, "manifest.xml");
                if (!File.Exists(manifestPath))
                {
                    _log.Debug($"No manifest in {folder}, skipping");
                    continue;
                }

                var manifest = PluginManifest.Load(manifestPath);
                toDownload.RemoveAll(x => string.Compare(manifest.Repository, x, StringComparison.InvariantCultureIgnoreCase) == 0);
                taskList.Add(_updateManager.CheckAndUpdatePlugin(manifest));
            }

            foreach (var repository in toDownload)
            {
                var manifest = new PluginManifest { Repository = repository, Version = "0.0" };
                taskList.Add(_updateManager.CheckAndUpdatePlugin(manifest));
            }

            Task.WaitAll(taskList.ToArray());
        }

        /// <inheritdoc />
        public void LoadPlugins()
        {
            if (Torch.Config.ShouldUpdatePlugins)
                DownloadPlugins();
            else
                _log.Warn("Automatic plugin updates are disabled.");

            _log.Info("Loading plugins");
            var dlls = Directory.GetFiles(PluginDir, "*.dll", SearchOption.AllDirectories);
            foreach (var dllPath in dlls)
            {
                _log.Info($"Loading plugin {dllPath}");
                var asm = Assembly.UnsafeLoadFrom(dllPath);

                foreach (var type in asm.GetExportedTypes())
                {
                    if (type.GetInterfaces().Contains(typeof(ITorchPlugin)))
                    {
                        if (type.GetCustomAttribute<PluginAttribute>() == null)
                            continue;

                        try
                        {
                            var plugin = (TorchPluginBase)Activator.CreateInstance(type);
                            if (plugin.Id == default(Guid))
                                throw new TypeLoadException($"Plugin '{type.FullName}' is missing a {nameof(PluginAttribute)}");

                            _log.Info($"Loading plugin {plugin.Name} ({plugin.Version})");
                            plugin.StoragePath = Torch.Config.InstancePath;
                            Plugins.Add(plugin);
                            PluginLoaded?.Invoke(plugin);
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

            Plugins.ForEach(p => p.Init(Torch));
            PluginsLoaded?.Invoke(Plugins.ToList());
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
