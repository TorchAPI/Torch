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
using Torch.Commands;
using VRage.Plugins;
using VRage.Collections;
using VRage.Library.Collections;

namespace Torch
{
    public class PluginManager : IPluginManager
    {
        private readonly ITorchBase _torch;
        private static Logger _log = LogManager.GetLogger(nameof(PluginManager));
        public const string PluginDir = "Plugins";

        private readonly List<ITorchPlugin> _plugins = new List<ITorchPlugin>();
        private readonly PluginUpdater _updater;
        private readonly CommandManager _commands;

        public PluginManager(ITorchBase torch)
        {
            _torch = torch;
            _updater = new PluginUpdater(this);
            _commands = new CommandManager(_torch);

            if (!Directory.Exists(PluginDir))
                Directory.CreateDirectory(PluginDir);

            InitUpdater();
        }

        private void InitUpdater()
        {
            var fieldName = "m_plugins";
            var pluginList = typeof(MyPlugins).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as List<IPlugin>;
            if (pluginList == null)
                throw new TypeLoadException($"{fieldName} field not found in {nameof(MyPlugins)}");

            pluginList.Add(_updater);
        }

        public void UpdatePlugins()
        {
            Parallel.ForEach(_plugins, p => p.Update());
        }

        public void UnloadPlugins()
        {
            foreach (var plugin in _plugins)
                plugin.Unload();

            _plugins.Clear();
        }

        /// <summary>
        /// Load and create instances of all plugins in the <see cref="PluginDir"/> folder.
        /// </summary>
        public void LoadPlugins()
        {
            _log.Info("Loading plugins");
            var pluginsPath = Path.Combine(Directory.GetCurrentDirectory(), PluginDir);
            var dlls = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);
            foreach (var dllPath in dlls)
            {
                UnblockDll(dllPath);
                var asm = Assembly.LoadFrom(dllPath);

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
                            _plugins.Add(plugin);

                            _commands.RegisterPluginCommands(plugin);
                        }
                        catch (Exception e)
                        {
                            _log.Error($"Error loading plugin '{type.FullName}'");
                            throw;
                        }
                    }
                }
            }
        }

        public IEnumerator<ITorchPlugin> GetEnumerator()
        {
            return _plugins.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Removes the lock on a DLL downloaded from the internet.
        /// </summary>
        /// <returns></returns>
        public bool UnblockDll(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        /// <summary>
        /// Tiny "plugin" to call <see cref="PluginManager"/>'s update method after each game tick.
        /// </summary>
        private class PluginUpdater : IPlugin
        {
            private readonly IPluginManager _manager;
            public PluginUpdater(IPluginManager manager)
            {
                _manager = manager;
            }

            public void Init(object obj)
            {
                _manager.LoadPlugins();
            }

            public void Update()
            {
                _manager.UpdatePlugins();
            }

            public void Dispose()
            {
                _manager.UnloadPlugins();
            }
        }
    }
}
