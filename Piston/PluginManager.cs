using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Piston.API;
using Sandbox;
using VRage.Plugins;
using VRage.Collections;
using VRage.Library.Collections;

namespace Piston
{
    public class PluginManager
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

        private void GetPluginList()
        {
            _plugins = typeof(MyPlugins).GetField("m_plugins", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as List<IPlugin>;
        }

        /// <summary>
        /// Load a plugin into the game.
        /// </summary>
        /// <param name="plugin"></param>
        public void LoadPlugin(IPlugin plugin)
        {
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
                throw new FileNotFoundException($"Plugin {folderName} does not exist in the Plugins folder.");

            var fileNames = Directory.GetFiles(relativeDir, "*.dll");

            foreach (var fileName in fileNames)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
                var asm = Assembly.LoadFrom(fullPath);

                foreach (var type in asm.GetTypes())
                {
                    if (type.GetInterfaces().Contains(typeof(IPlugin)))
                    {
                        var inst = Activator.CreateInstance(type);
                        LoadPlugin((IPlugin)inst);
                    }
                }
            }
        }

        /// <summary>
        /// Unload a plugin from the game.
        /// </summary>
        /// <param name="plugin"></param>
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
            var p = plugin as IPistonPlugin;
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
                var p = plugin as IPistonPlugin;
                p?.Reload();
            }
        }
    }
}
