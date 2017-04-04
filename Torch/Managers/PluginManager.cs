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
using VRage.Plugins;
using VRage.Collections;
using VRage.Library.Collections;

namespace Torch.Managers
{
    public class PluginManager : IPluginManager, IPlugin
    {
        private readonly ITorchBase _torch;
        private static Logger _log = LogManager.GetLogger(nameof(PluginManager));
        public const string PluginDir = "Plugins";

        public List<ITorchPlugin> Plugins { get; } = new List<ITorchPlugin>();
        public CommandManager Commands { get; private set; }

        public float LastUpdateMs => _lastUpdateMs;
        private volatile float _lastUpdateMs;

        public event Action<List<ITorchPlugin>> PluginsLoaded;

        public PluginManager(ITorchBase torch)
        {
            _torch = torch;

            if (!Directory.Exists(PluginDir))
                Directory.CreateDirectory(PluginDir);

            InitUpdater();
        }

        /// <summary>
        /// Adds the plugin manager "plugin" to VRage's plugin system.
        /// </summary>
        private void InitUpdater()
        {
            var fieldName = "m_plugins";
            var pluginList = typeof(MyPlugins).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as List<IPlugin>;
            if (pluginList == null)
                throw new TypeLoadException($"{fieldName} field not found in {nameof(MyPlugins)}");

            pluginList.Add(this);
        }

        /// <summary>
        /// Updates loaded plugins in parallel.
        /// </summary>
        public void UpdatePlugins()
        {
            var s = Stopwatch.StartNew();
            Parallel.ForEach(Plugins, p => p.Update());
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

        /// <summary>
        /// Loads and creates instances of all plugins in the <see cref="PluginDir"/> folder.
        /// </summary>
        public void Init()
        {
            ((TorchBase)_torch).Network.Init();
            ChatManager.Instance.Init();
            Commands = new CommandManager(_torch);

            _log.Info("Loading plugins");
            var pluginsPath = Path.Combine(Directory.GetCurrentDirectory(), PluginDir);
            var dlls = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);
            foreach (var dllPath in dlls)
            {
                var asm = Assembly.UnsafeLoadFrom(dllPath);

                foreach (var type in asm.GetExportedTypes())
                {
                    if (type.GetInterfaces().Contains(typeof(ITorchPlugin)))
                    {
                        try
                        {
                            var plugin = (ITorchPlugin)Activator.CreateInstance(type);
                            if (plugin.Id == default(Guid))
                                throw new TypeLoadException($"Plugin '{type.FullName}' is missing a {nameof(PluginAttribute)}");

                            _log.Info($"Loading plugin {plugin.Name} ({plugin.Version})");
                            Plugins.Add(plugin);

                            Commands.RegisterPluginCommands(plugin);
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

        void IPlugin.Init(object obj)
        {
            Init();
        }

        void IPlugin.Update()
        {
            UpdatePlugins();
        }

        public void Dispose()
        {
            DisposePlugins();
        }
    }
}
