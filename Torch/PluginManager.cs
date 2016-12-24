using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PistonAPI;
using Sandbox;
using Torch.API;
using VRage.Plugins;
using VRage.Collections;
using VRage.Library.Collections;

namespace Torch
{
    public class PluginManager : IPluginManager
    {
        //TODO: Disable reloading if the plugin has static elements because they prevent a full reload.

        public ListReader<IPlugin> Plugins => MyPlugins.Plugins;

        private List<IPlugin> _plugins;
        public const string PluginDir = "Plugins";

        public PluginManager()
        {
            if (!Directory.Exists(PluginDir))
                Directory.CreateDirectory(PluginDir);

            GetPluginList();
        }

        /// <summary>
        /// Get a reference to the internal VRage plugin list.
        /// </summary>
        private void GetPluginList()
        {
            _plugins = typeof(MyPlugins).GetField("m_plugins", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as List<IPlugin>;
        }

        /// <summary>
        /// Get a plugin's name from its <see cref="PluginAttribute"/> or its type name.
        /// </summary>
        public string GetPluginName(Type pluginType)
        {
            var attr = pluginType.GetCustomAttribute<PluginAttribute>();
            return attr?.Name ?? pluginType.Name;
        }

        /// <summary>
        /// Load all plugins in the <see cref="PluginDir"/> folder.
        /// </summary>
        public void LoadAllPlugins()
        {
            var pluginFolders = GetPluginFolders();
            foreach (var folder in pluginFolders)
            {
                LoadPluginFolder(folder);
            }
        }

        /// <summary>
        /// Load a plugin into the game.
        /// </summary>
        /// <param name="plugin"></param>
        public void LoadPlugin(IPlugin plugin)
        {
            Logger.Write($"Loading plugin: {GetPluginName(plugin.GetType())}");
            plugin.Init(MySandboxGame.Static);
            _plugins.Add(plugin);
        }

        /// <summary>
        /// Get the names of all the subfolders in the Plugins directory.
        /// </summary>
        /// <returns></returns>
        public string[] GetPluginFolders()
        {
            var dirs = Directory.GetDirectories(PluginDir);
            for (var i = 0; i < dirs.Length; i++)
            {
                dirs[i] = dirs[i].Substring(PluginDir.Length + 1);
            }

            return dirs;
        }

        /// <summary>
        /// Load all plugins in the specified folder.
        /// </summary>
        /// <param name="folderName">Folder in the <see cref="PluginDir"/> directory</param>
        public void LoadPluginFolder(string folderName)
        {
            var relativeDir = Path.Combine(PluginDir, folderName);
            if (!Directory.Exists(relativeDir))
            {
                Logger.Write($"Plugin {folderName} does not exist in the Plugins folder.");
                return;
            }

            var fileNames = Directory.GetFiles(relativeDir, "*.dll");

            foreach (var fileName in fileNames)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
                UnblockDll(fullPath);
                var asm = Assembly.LoadFrom(fullPath);

                foreach (var type in asm.GetTypes())
                {
                    if (type.GetInterfaces().Contains(typeof(IPlugin)))
                    {
                        var inst = (IPlugin)Activator.CreateInstance(type);
                        MySandboxGame.Static.Invoke(() => LoadPlugin(inst));
                    }
                }
            }
        }

        /// <summary>
        /// Unload a plugin from the game.
        /// </summary>
        public void UnloadPlugin(IPlugin plugin)
        {
            _plugins.Remove(plugin);
            plugin.Dispose();
        }

        /// <summary>
        /// Reload a plugin.
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="forceNonPiston">Reload a non-Piston plugin</param>
        public void ReloadPlugin(IPlugin plugin, bool forceNonPiston = false)
        {
            var p = plugin as ITorchPlugin;
            if (p == null && forceNonPiston)
            {
                plugin.Dispose();
                plugin.Init(MySandboxGame.Static);
            }
            else
            {
                p?.Reload();
            }
        }

        public void ReloadAll()
        {
            foreach (var plugin in _plugins)
            {
                var p = plugin as ITorchPlugin;
                p?.Reload();
            }
        }

        public bool UnblockDll(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);
    }
}
