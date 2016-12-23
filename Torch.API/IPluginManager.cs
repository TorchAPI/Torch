using System;
using VRage.Collections;
using VRage.Plugins;

namespace Torch.API
{
    public interface IPluginManager
    {
        ListReader<IPlugin> Plugins { get; }

        string[] GetPluginFolders();
        string GetPluginName(Type pluginType);
        void LoadAllPlugins();
        void LoadPlugin(IPlugin plugin);
        void LoadPluginFolder(string folderName);
        void ReloadAll();
        void ReloadPlugin(IPlugin plugin, bool forceNonPiston = false);
        bool UnblockDll(string fileName);
        void UnloadPlugin(IPlugin plugin);
    }
}