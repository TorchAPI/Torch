using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NLog;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.API.WebAPI;
using Torch.Collections;
using Torch.Commands;
using Torch.Utils;

namespace Torch.Managers
{
    /// <inheritdoc />
    public class PluginManager : Manager, IPluginManager
    {
        private class PluginItem
        {
            public string Filename { get; set; }
            public string Path { get; set; }
            public PluginManifest Manifest { get; set; }
            public bool IsZip { get; set; }
            public List<PluginItem> ResolvedDependencies { get; set; }
        }
        
        private static Logger _log = LogManager.GetCurrentClassLogger();
        
        private const string MANIFEST_NAME = "manifest.xml";
        
        public readonly string PluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        private readonly MtObservableSortedDictionary<Guid, ITorchPlugin> _plugins = new MtObservableSortedDictionary<Guid, ITorchPlugin>();
        private CommandManager _mgr;
        
#pragma warning disable 649
        [Dependency]
        private ITorchSessionManager _sessionManager;
#pragma warning restore 649
        
        /// <inheritdoc />
        public IReadOnlyDictionary<Guid, ITorchPlugin> Plugins => _plugins.AsReadOnlyObservable();
        public bool CanUsePrivatePlugins = false;

        public event Action<IReadOnlyCollection<ITorchPlugin>> PluginsLoaded;
        
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
            foreach (var plugin in _plugins.Values)
            {
                try
                {
                    plugin.Update();
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Plugin {plugin.Name} threw an exception during update!");
                }
            }
        }
        
        /// <inheritdoc/>
        public override void Attach()
        {
            base.Attach();
            _sessionManager.SessionStateChanged += SessionManagerOnSessionStateChanged;
        }

        private void SessionManagerOnSessionStateChanged(ITorchSession session, TorchSessionState newState)
        {
            _mgr = session.Managers.GetManager<CommandManager>();
            if (_mgr == null)
                return;
            switch (newState)
            {
                case TorchSessionState.Loaded:
                    foreach (ITorchPlugin plugin in _plugins.Values)
                        _mgr.RegisterPluginCommands(plugin);
                    return;
                case TorchSessionState.Unloading:
                    foreach (ITorchPlugin plugin in _plugins.Values)
                        _mgr.UnregisterPluginCommands(plugin);
                    return;
                case TorchSessionState.Loading:
                case TorchSessionState.Unloaded:
                default:
                    return;
            }
        }
        
        /// <summary>
        /// Unloads all plugins.
        /// </summary>
        public override void Detach()
        {
            _sessionManager.SessionStateChanged -= SessionManagerOnSessionStateChanged;
            foreach (var plugin in _plugins.Values)
                plugin.Dispose();

            _plugins.Clear();
        }

        public void LoadPlugins()
        {
            _log.Info("Loading plugins...");

            //check for usage of private plugins
            if(Torch.Config.DataSharing){
                CanUsePrivatePlugins = true;
            }

            if (!string.IsNullOrEmpty(Torch.Config.TestPlugin))
            {
                _log.Info($"Loading plugin for debug at {Torch.Config.TestPlugin}");

                foreach (var item in GetLocalPlugins(Torch.Config.TestPlugin, true))
                {
                    _log.Info(item.Path);
                    LoadPlugin(item);
                }

                foreach (var plugin in _plugins.Values)
                {
                    plugin.Init(Torch);
                }
                _log.Info($"Loaded {_plugins.Count} plugins.");
                PluginsLoaded?.Invoke(_plugins.Values.AsReadOnly());
                return;
            }

            var pluginItems = GetLocalPlugins(PluginDir);
            var pluginsToLoad = new List<PluginItem>();
            foreach (var item in pluginItems)
            {
                var pluginItem = item;
                if (!TryValidatePluginDependencies(pluginItems, ref pluginItem, out var missingPlugins))
                {
                    // We have some missing dependencies.
                    // Future fix would be to download them, but instead for now let's
                    // just warn the user it's missing
                    foreach(var missingPlugin in missingPlugins)
                        _log.Warn($"{item.Manifest.Name} is missing dependency {missingPlugin}. Skipping plugin.");
                    continue;
                }
                
                pluginsToLoad.Add(pluginItem);
            }


            if (Torch.Config.ShouldUpdatePlugins)
            {
                List<PluginItem> updatedPluginList = new List<PluginItem>();
                if (DownloadPluginUpdates(pluginsToLoad, out updatedPluginList))
                {
                    // Resort the plugins just in case updates changed load hints.
                    pluginItems = GetLocalPlugins(PluginDir);
                    pluginsToLoad.Clear();
                    foreach (var item in pluginItems)
                    {
                        var pluginItem = item;
                        if (!TryValidatePluginDependencies(pluginItems, ref pluginItem, out var missingPlugins))
                        {
                            foreach (var missingPlugin in missingPlugins)
                                _log.Warn($"{item.Manifest.Name} is missing dependency {missingPlugin}. Skipping plugin.");
                            continue;
                        }

                        pluginsToLoad.Add(pluginItem);
                    }
                }
                pluginsToLoad = pluginItems;
            }

            // Sort based on dependencies.
            try
            {
                pluginsToLoad = pluginsToLoad.TSort(item => item.ResolvedDependencies)
                    .ToList();
            }
            catch (Exception e)
            {
                // This will happen on cylic dependencies.
                _log.Error(e);
            }
            
            // Actually load the plugins now.
            foreach (var item in pluginsToLoad)
            {
                LoadPlugin(item);
            } 
            
            foreach (var plugin in _plugins.Values)
            {
                plugin.Init(Torch);
            }
            _log.Info($"Loaded {_plugins.Count} plugins.");
            PluginsLoaded?.Invoke(_plugins.Values.AsReadOnly());
        }

        //debug flag is set when the user asks us to run with a specific plugin for plugin development debug
        //please do not change references to this arg unless you are very sure you know what you're doing
        private List<PluginItem> GetLocalPlugins(string pluginDir, bool debug = false)
        {
            var firstLoad = Torch.Config.Plugins.Count == 0;
            
            var pluginItems = Directory.EnumerateFiles(pluginDir, "*.zip")
                .Union(Directory.EnumerateDirectories(pluginDir));
            if (debug)
                pluginItems = pluginItems.Union(new List<string> {pluginDir});
            var results = new List<PluginItem>();

            foreach (var item in pluginItems)
            {
                var path = Path.Combine(pluginDir, item);
                var isZip = item.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase);
                var manifest = isZip ? GetManifestFromZip(path) : GetManifestFromDirectory(path);

                if (manifest == null)
                {
                    if (!debug)
                    {
                        _log.Warn($"Item '{item}' is missing a manifest, skipping.");
                        continue;
                    }
                    manifest = new PluginManifest()
                               {
                                   Guid = new Guid(),
                                   Version = "0",
                                   Name = "TEST"
                               };
                }

                var duplicatePlugin = results.FirstOrDefault(r => r.Manifest.Guid == manifest.Guid);
                if (duplicatePlugin != null)
                {
                    _log.Warn(
                        $"The GUID provided by {manifest.Name} ({item}) is already in use by {duplicatePlugin.Manifest.Name}.");
                    continue;
                }
                
                if (!Torch.Config.LocalPlugins && !debug)
                {
                    if (isZip && !Torch.Config.Plugins.Contains(manifest.Guid))
                    {
                        if (!firstLoad)
                        {
                            _log.Warn($"Plugin {manifest.Name} ({item}) exists in the plugin directory, but is not listed in torch.cfg. Skipping load!");
                            continue;
                        }
                        _log.Info($"First-time load: Plugin {manifest.Name} added to torch.cfg.");
                        Torch.Config.Plugins.Add(manifest.Guid);
                    }
                }
                
                var pluginFullItem = Task.Run(async () => await PluginQuery.Instance.QueryOneOnly(manifest.Guid.ToString())).Result;
                if(pluginFullItem.IsPrivate && (pluginFullItem != null))
                {
                    _log.Warn($"Plugin {manifest.Name} ({item}) is private and cannot be loaded as local. Skipping load!");
                    continue;
                }

                results.Add(new PluginItem
                {
                    Filename = item,
                    IsZip = isZip,
                    Manifest = manifest,
                    Path = path
                });
            }

            if (!Torch.Config.LocalPlugins && firstLoad)
                Torch.Config.Save();
            
            return results;
        } 
        
        private bool DownloadPluginUpdates(List<PluginItem> plugins, out List<PluginItem> returnedPlugins)
        {
            _log.Info("Checking for plugin updates...");
            var count = 0;
            List<PluginItem> pluginsToUpdate = plugins;
            Task.WaitAll(plugins.Select(async item =>
            {
                try
                {
                    if (!item.IsZip)
                    {
                        _log.Warn($"Unzipped plugins cannot be auto-updated. Skipping plugin {item}");
                        return;
                    }
                    item.Manifest.Version.TryExtractVersion(out Version currentVersion);
                    var latest = await PluginQuery.Instance.QueryOneOnly(item.Manifest.Guid.ToString(), false, TorchBase.Instance.Identifier);

                    if (latest?.LatestVersion == null)
                    {
                        _log.Warn($"Plugin {item.Manifest.Name} does not have any releases on torchapi.com. Cannot update.");
                        return;
                    }

                    if (latest.IsPrivate && !CanUsePrivatePlugins)
                    {
                        _log.Warn($"You cannot use {item.Manifest.Name} as data sharing is disabled in config");
                        pluginsToUpdate.Remove(item);
                        return;
                    }

                    latest.LatestVersion.TryExtractVersion(out Version newVersion);

                    if (currentVersion == null || newVersion == null)
                    {
                        _log.Error($"Error parsing version from manifest or website for plugin '{item.Manifest.Name}.'");
                        return;
                    }

                    if (newVersion <= currentVersion)
                    {
                        _log.Debug($"{item.Manifest.Name} {item.Manifest.Version} is up to date.");
                        return;
                    }

                    _log.Info($"Updating plugin '{item.Manifest.Name}' from {currentVersion} to {newVersion}.");
                    if (latest.IsPrivate)
                    {
                        await PluginQuery.Instance.DownloadPrivatePlugin(latest.ID, TorchBase.Instance.Identifier, TorchBase.IPAddress);
                    }
                    else
                    {
                        await PluginQuery.Instance.DownloadPlugin(latest, item.Path);
                    }

                    Interlocked.Increment(ref count);
                }
                catch (Exception e)
                {
                    _log.Warn($"An error occurred updating the plugin {item.Manifest.Name}.");
                    _log.Warn(e);
                }
            }).ToArray());

            _log.Info($"Updated {count} plugins.");
            returnedPlugins = pluginsToUpdate;
            return count > 0;
        }
        
        private void LoadPlugin(PluginItem item)
        {
            var assemblies = new List<Assembly>();

            var loaded = AppDomain.CurrentDomain.GetAssemblies();
            
            if (item.IsZip)
            {
                using (var zipFile = ZipFile.OpenRead(item.Path))
                {
                    foreach (var entry in zipFile.Entries)
                    {
                        if (!entry.Name.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase))
                            continue;

                        //if (loaded.Any(a => entry.Name.Contains(a.GetName().Name)))
                        //    continue;


                        using (var stream = entry.Open())
                        {
                            var data = stream.ReadToEnd((int) entry.Length);
                            byte[] symbol = null;
                            var symbolEntryName =
                                entry.FullName.Substring(0, entry.FullName.Length - "dll".Length) + "pdb";
                            var symbolEntry = zipFile.GetEntry(symbolEntryName);
                            if (symbolEntry != null)
                                try
                                {
                                    using (var symbolStream = symbolEntry.Open())
                                        symbol = symbolStream.ReadToEnd((int) symbolEntry.Length);
                                }
                                catch (Exception e)
                                {
                                    _log.Warn(e, $"Failed to read debugging symbols from {item.Filename}:{symbolEntryName}");
                                }

                            assemblies.Add(symbol != null ? Assembly.Load(data, symbol) : Assembly.Load(data));
                        }
                    }
                }
            }
            else
            {
                var files = Directory
                    .EnumerateFiles(item.Path, "*.*", SearchOption.AllDirectories)
                    .ToList();
                
                foreach (var file in files)
                {
                    if (!file.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    //if (loaded.Any(a => file.Contains(a.GetName().Name)))
                    //    continue;

                    using (var stream = File.OpenRead(file))
                    {
                        var data = stream.ReadToEnd();
                        byte[] symbol = null;
                        var symbolPath = Path.Combine(Path.GetDirectoryName(file) ?? ".",
                            Path.GetFileNameWithoutExtension(file) + ".pdb");
                        if (File.Exists(symbolPath))
                            try
                            {
                                using (var symbolStream = File.OpenRead(symbolPath))
                                    symbol = symbolStream.ReadToEnd();
                            }
                            catch (Exception e)
                            {
                                _log.Warn(e, $"Failed to read debugging symbols from {symbolPath}");
                            }
                        
                        assemblies.Add(symbol != null ? Assembly.Load(data, symbol) : Assembly.Load(data));
                    }
                }

                
            }
            
            RegisterAllAssemblies(assemblies);
            InstantiatePlugin(item.Manifest, assemblies);
        }
        
        private void RegisterAllAssemblies(IReadOnlyCollection<Assembly> assemblies)
        {
            Assembly ResolveDependentAssembly(object sender, ResolveEventArgs args)
            {
                var requiredAssemblyName = new AssemblyName(args.Name);
                foreach (Assembly asm in assemblies)
                {
                    if (IsAssemblyCompatible(requiredAssemblyName, asm.GetName()))
                        return asm;
                }
                if (requiredAssemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                    return null;
                foreach (var asm in assemblies)
                    if (asm == args.RequestingAssembly)
                    {
                        _log.Warn($"Couldn't find dependency! {args.RequestingAssembly} depends on {requiredAssemblyName}.");
                        break;
                    }
                return null;
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveDependentAssembly;
            foreach (Assembly asm in assemblies)
            {
                TorchBase.RegisterAuxAssembly(asm);
            }
        }
        
        private static bool IsAssemblyCompatible(AssemblyName a, AssemblyName b)
        {
            return a.Name == b.Name && a.Version.Major == b.Version.Major && a.Version.Minor == b.Version.Minor;
        }
        
        private void InstantiatePlugin(PluginManifest manifest, IEnumerable<Assembly> assemblies)
        {
            Type pluginType = null;
            bool mult = false;
            foreach (var asm in assemblies)
            {
                foreach (var type in asm.GetExportedTypes())
                {
                    if (!type.GetInterfaces().Contains(typeof(ITorchPlugin)))
                        continue;

                    if (type.IsAbstract)
                        continue;

                    _log.Info($"Loading plugin at {type.FullName}");

                    if (pluginType != null)
                    {
                        //_log.Error($"The plugin '{manifest.Name}' has multiple implementations of {nameof(ITorchPlugin)}, not loading.");
                        //return;
                        mult = true;
                        continue;
                    }

                    pluginType = type;
                }
            }

            if (mult)
            {
                _log.Error($"The plugin '{manifest.Name}' has multiple implementations of {nameof(ITorchPlugin)}, not loading.");
                return;
            }

            if (pluginType == null)
            {
                _log.Error($"The plugin '{manifest.Name}' does not have an implementation of {nameof(ITorchPlugin)}, not loading.");
                return;
            }

            // Backwards compatibility for PluginAttribute.
            var pluginAttr = pluginType.GetCustomAttribute<PluginAttribute>();
            if (pluginAttr != null)
            {
                _log.Warn($"Plugin '{manifest.Name}' is using the obsolete {nameof(PluginAttribute)}, using info from attribute if necessary.");
                manifest.Version = manifest.Version ?? pluginAttr.Version.ToString();
                manifest.Name = manifest.Name ?? pluginAttr.Name;
                if (manifest.Guid == default(Guid))
                    manifest.Guid = pluginAttr.Guid;
            }

            _log.Info($"Loading plugin '{manifest.Name}' ({manifest.Version})");

            TorchPluginBase plugin;
            try
            {
                plugin = (TorchPluginBase)Activator.CreateInstance(pluginType);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Plugin {manifest.Name} threw an exception during instantiation! Not loading!");
                return;
            }
            plugin.Manifest = manifest;
            plugin.StoragePath = Torch.Config.InstancePath;
            plugin.Torch = Torch;
            _plugins.Add(manifest.Guid, plugin);
        }
        
        private PluginManifest GetManifestFromZip(string path)
        {
            try
            {
                using (var zipFile = ZipFile.OpenRead(path))
                {
                    foreach (var entry in zipFile.Entries)
                    {
                        if (!entry.Name.Equals(MANIFEST_NAME, StringComparison.CurrentCultureIgnoreCase))
                            continue;

                        using (var stream = new StreamReader(entry.Open()))
                        {
                            return PluginManifest.Load(stream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error opening zip! File is likely corrupt. File at {path} will be deleted and re-acquired on the next restart!");
                File.Delete(path);
            }

            return null;
        }

        private bool TryValidatePluginDependencies(List<PluginItem> items, ref PluginItem item, out List<Guid> missingDependencies)
        {
            var dependencies = new List<PluginItem>();
            missingDependencies = new List<Guid>();
            
            foreach (var pluginDependency in item.Manifest.Dependencies)
            {
                var dependency = items
                    .FirstOrDefault(pi => pi?.Manifest.Guid == pluginDependency.Plugin);
                if (dependency == null)
                {
                    missingDependencies.Add(pluginDependency.Plugin);
                    continue;
                }

                if (!string.IsNullOrEmpty(pluginDependency.MinVersion)
                    && dependency.Manifest.Version.TryExtractVersion(out var dependencyVersion)
                    && pluginDependency.MinVersion.TryExtractVersion(out var minVersion))
                {
                    // really only care about version if it is defined.
                    if (dependencyVersion < minVersion)
                    {
                        // If dependency version is too low, we can try to update. Otherwise
                        // it's a missing dependency.
                        
                        // For now let's just warn the user. bitMuse is lazy.
                        _log.Warn($"{dependency.Manifest.Name} is below the requested version for {item.Manifest.Name}."
                        + Environment.NewLine
                        + $" Desired version: {pluginDependency.MinVersion}, Available version: {dependency.Manifest.Version}");
                        missingDependencies.Add(pluginDependency.Plugin);
                        continue;
                    }
                }

                dependencies.Add(dependency);
            }

            item.ResolvedDependencies = dependencies;
            if (missingDependencies.Count > 0)
                return false;
            return true;
        }

        private PluginManifest GetManifestFromDirectory(string directory)
        {
            var path = Path.Combine(directory, MANIFEST_NAME);
            return !File.Exists(path) ? null : PluginManifest.Load(path);
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<ITorchPlugin> GetEnumerator()
        {
            return _plugins.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
