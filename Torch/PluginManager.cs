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
using Sandbox;
using Torch.API;
using VRage.Plugins;
using VRage.Collections;
using VRage.Library.Collections;

namespace Torch
{
    public class PluginManager : IPluginManager
    {
        private readonly ITorchBase _torch;
        public const string PluginDir = "Plugins";

        private readonly List<TorchPluginBase> _plugins = new List<TorchPluginBase>();

        public PluginManager(ITorchBase torch)
        {
            _torch = torch;

            if (!Directory.Exists(PluginDir))
                Directory.CreateDirectory(PluginDir);
        }

        /// <summary>
        /// Load all plugins in the <see cref="PluginDir"/> folder.
        /// </summary>
        public void LoadPlugins()
        {
            var pluginsPath = Path.Combine(Directory.GetCurrentDirectory(), PluginDir);
            var dlls = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);
            foreach (var dllPath in dlls)
            {
                UnblockDll(dllPath);
                var asm = Assembly.LoadFrom(dllPath);

                foreach (var type in asm.GetExportedTypes())
                {
                    if (type.IsSubclassOf(typeof(TorchPluginBase)))
                        _plugins.Add((TorchPluginBase)Activator.CreateInstance(type));
                }
            }
        }

        public async void ReloadPluginAsync(ITorchPlugin plugin)
        {
            var p = plugin as TorchPluginBase;
            if (p == null)
                return;

            var newPlugin = (TorchPluginBase)Activator.CreateInstance(p.GetType());
            _plugins.Add(newPlugin);

            await p.StopAsync();
            _plugins.Remove(p);

            newPlugin.Run(_torch, true);
        }

        public void StartEnabledPlugins()
        {
            foreach (var plugin in _plugins)
            {
                if (plugin.Enabled)
                    plugin.Run(_torch);
            }
        }

        public bool UnblockDll(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }

        public IEnumerator<ITorchPlugin> GetEnumerator()
        {
            return _plugins.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);
    }
}
