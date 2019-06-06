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
        private static Logger _log = LogManager.GetCurrentClassLogger();
        private const string MANIFEST_NAME = "manifest.xml";
        public readonly string PluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        private readonly MtObservableSortedDictionary<Guid, ITorchPlugin> _plugins = new MtObservableSortedDictionary<Guid, ITorchPlugin>();
#pragma warning disable 649
        [Dependency]
        private ITorchSessionManager _sessionManager;
#pragma warning restore 649

        /// <inheritdoc />
        public IReadOnlyDictionary<Guid, ITorchPlugin> Plugins => _plugins.AsReadOnlyObservable();

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

        private CommandManager _mgr;

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
            bool firstLoad = Torch.Config.Plugins.Count == 0;
            List<Guid> foundPlugins = new List<Guid>();
            if (Torch.Config.ShouldUpdatePlugins)
                DownloadPluginUpdates();

            _log.Info("Loading plugins...");
            var pluginItems = Directory.EnumerateFiles(PluginDir, "*.zip").Union(Directory.EnumerateDirectories(PluginDir));
            foreach (var item in pluginItems)
            {
                var path = Path.Combine(PluginDir, item);
                var isZip = item.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase);
                var manifest = isZip ? GetManifestFromZip(path) : GetManifestFromDirectory(path);
                if (!Torch.Config.LocalPlugins)
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
                if(isZip)
                    foundPlugins.Add(manifest.Guid);
                }

                LoadPlugin(item);
            }
            if (!Torch.Config.LocalPlugins && firstLoad)
                Torch.Config.Save();
            
            if (!Torch.Config.LocalPlugins)
            {
                List<string> toLoad = new List<string>();

                //This is actually the easiest way to batch process async tasks and block until completion (????)
                Task.WhenAll(Torch.Config.Plugins.Select(async g =>
                                                         {
                                                             try
                                                             {
                                                                 if (foundPlugins.Contains(g))
                                                                 {
                                                                     return;
                                                                 }
                                                                 var item = await PluginQuery.Instance.QueryOne(g);
                                                                 string s = Path.Combine(PluginDir, item.Name + ".zip");
                                                                 await PluginQuery.Instance.DownloadPlugin(item, s);
                                                                 lock (toLoad)
                                                                     toLoad.Add(s);
                                                             }
                                                             catch (Exception ex)
                                                             {
                                                                 _log.Error(ex);
                                                             }
                                                         }));

                foreach (var l in toLoad)
                {
                    LoadPlugin(l);
                }
            }

            //just reuse the list from earlier
            foundPlugins.Clear();
            foreach (var plugin in _plugins.Values)
            {
                try
                {
                    plugin.Init(Torch);
                }
                catch (Exception e)
                {
                    _log.Error(e, $"Plugin {plugin.Name} threw an exception during init! Unloading plugin!");
                    foundPlugins.Add(plugin.Id);
                }
            }

            foreach (var id in foundPlugins)
            {
                var p = _plugins[id];
                _plugins.Remove(id);
                _mgr.UnregisterPluginCommands(p);
                p.Dispose();
            }

            _log.Info($"Loaded {_plugins.Count} plugins.");
            PluginsLoaded?.Invoke(_plugins.Values.AsReadOnly());
        }

        private void LoadPlugin(string item)
        {
            var path = Path.Combine(PluginDir, item);
            var isZip = item.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase);
            var manifest = isZip ? GetManifestFromZip(path) : GetManifestFromDirectory(path);
            if (manifest == null)
            {
                _log.Warn($"Item '{item}' is missing a manifest, skipping.");
                return;
            }

            if (_plugins.ContainsKey(manifest.Guid))
            {
                _log.Error($"The GUID provided by {manifest.Name} ({item}) is already in use by {_plugins[manifest.Guid].Name}");
                return;
            }

            if (isZip)
                LoadPluginFromZip(path);
            else
                LoadPluginFromFolder(path);
        }

        private void DownloadPluginUpdates()
        {
            _log.Info("Checking for plugin updates...");
            var count = 0;
            var pluginItems = Directory.EnumerateFiles(PluginDir, "*.zip");
            Task.WhenAll(pluginItems.Select(async item =>
            {
                PluginManifest manifest = null;
                try
                {
                    var path = Path.Combine(PluginDir, item);
                    var isZip = item.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase);
                    if (!isZip)
                    {
                        _log.Warn($"Unzipped plugins cannot be auto-updated. Skipping plugin {item}");
                        return;
                    }
                    manifest = GetManifestFromZip(path);
                    if (manifest == null)
                    {
                        _log.Warn($"Item '{item}' is missing a manifest, skipping update check.");
                        return;
                    }

                    manifest.Version.TryExtractVersion(out Version currentVersion);
                    var latest = await PluginQuery.Instance.QueryOne(manifest.Guid);

                    if (latest?.LatestVersion == null)
                    {
                        _log.Warn($"Plugin {manifest.Name} does not have any releases on torchapi.net. Cannot update.");
                        return;
                    }

                    latest.LatestVersion.TryExtractVersion(out Version newVersion);

                    if (currentVersion == null || newVersion == null)
                    {
                        _log.Error($"Error parsing version from manifest or website for plugin '{manifest.Name}.'");
                        return;
                    }

                    if (newVersion <= currentVersion)
                    {
                        _log.Debug($"{manifest.Name} {manifest.Version} is up to date.");
                        return;
                    }

                    _log.Info($"Updating plugin '{manifest.Name}' from {currentVersion} to {newVersion}.");
                    await PluginQuery.Instance.DownloadPlugin(latest, path);
                    Interlocked.Increment(ref count);
                }
                catch (Exception e)
                {
                    _log.Warn($"An error occurred updating the plugin {manifest?.Name ?? item}.");
                    _log.Warn(e);
                }
            }));

            _log.Info($"Updated {count} plugins.");
        }
        
        private void LoadPluginFromFolder(string directory)
        {
            var assemblies = new List<Assembly>();
            var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories).ToList();

            var manifest = GetManifestFromDirectory(directory);
            if (manifest == null)
            {
                _log.Warn($"Directory {directory} is missing a manifest, skipping load.");
                return;
            }

            foreach (var file in files)
            {
                if (!file.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase))
                    continue;

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

            RegisterAllAssemblies(assemblies);
            InstantiatePlugin(manifest, assemblies);
        }

        private void LoadPluginFromZip(string path)
        {
            PluginManifest manifest;
            var assemblies = new List<Assembly>();
            using (var zipFile = ZipFile.OpenRead(path))
            {
                manifest = GetManifestFromZip(path);
                if (manifest == null)
                {
                    _log.Warn($"Zip file {path} is missing a manifest, skipping.");
                    return;
                }

                foreach (var entry in zipFile.Entries)
                {
                    if (!entry.Name.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase))
                        continue;


                    using (var stream = entry.Open())
                    {
                        var data = stream.ReadToEnd((int)entry.Length);
                        byte[] symbol = null;
                        var symbolEntryName = entry.FullName.Substring(0, entry.FullName.Length - "dll".Length) + "pdb";
                        var symbolEntry = zipFile.GetEntry(symbolEntryName);
                        if (symbolEntry != null)
                            try
                            {
                                using (var symbolStream = symbolEntry.Open())
                                    symbol = symbolStream.ReadToEnd((int)symbolEntry.Length);
                            }
                            catch (Exception e)
                            {
                                _log.Warn(e, $"Failed to read debugging symbols from {path}:{symbolEntryName}");
                            }
                        assemblies.Add(symbol != null ? Assembly.Load(data, symbol) : Assembly.Load(data));
                    }
                }
            }

            RegisterAllAssemblies(assemblies);
            InstantiatePlugin(manifest, assemblies);
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


        private PluginManifest GetManifestFromZip(string path)
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

            return null;
        }

        private PluginManifest GetManifestFromDirectory(string directory)
        {
            var path = Path.Combine(directory, MANIFEST_NAME);
            return !File.Exists(path) ? null : PluginManifest.Load(path);
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
