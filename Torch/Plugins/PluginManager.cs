using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NLog;
using Octokit;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Collections;
using Torch.Commands;

namespace Torch.Managers
{
    /// <inheritdoc />
    public class PluginManager : Manager, IPluginManager
    {
        private GitHubClient _gitClient = new GitHubClient(new ProductHeaderValue("Torch"));
        private static Logger _log = LogManager.GetLogger(nameof(PluginManager));
        private const string MANIFEST_NAME = "manifest.xml";
        public readonly string PluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        private readonly ObservableDictionary<Guid, ITorchPlugin> _plugins = new ObservableDictionary<Guid, ITorchPlugin>();
#pragma warning disable 649
        [Dependency]
        private ITorchSessionManager _sessionManager;
#pragma warning restore 649

        /// <inheritdoc />
        public IReadOnlyDictionary<Guid, ITorchPlugin> Plugins => _plugins.AsReadOnly();

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
                plugin.Update();
        }

        /// <inheritdoc/>
        public override void Attach()
        {
            base.Attach();
            _sessionManager.SessionStateChanged += SessionManagerOnSessionStateChanged;
        }

        private void SessionManagerOnSessionStateChanged(ITorchSession session, TorchSessionState newState)
        {
            var mgr = session.Managers.GetManager<CommandManager>();
            if (mgr == null)
                return;
            switch (newState)
            {
                case TorchSessionState.Loaded:
                    foreach (ITorchPlugin plugin in _plugins.Values)
                        mgr.RegisterPluginCommands(plugin);
                    return;
                case TorchSessionState.Unloading:
                    foreach (ITorchPlugin plugin in _plugins.Values)
                        mgr.UnregisterPluginCommands(plugin);
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
            if (Torch.Config.ShouldUpdatePlugins)
                DownloadPluginUpdates();

            _log.Info("Loading plugins...");
            var pluginItems = Directory.EnumerateFiles(PluginDir, "*.zip").Union(Directory.EnumerateDirectories(PluginDir));
            foreach (var item in pluginItems)
            {
                var path = Path.Combine(PluginDir, item);
                var isZip = item.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase);
                var manifest = isZip ? GetManifestFromZip(path) : GetManifestFromDirectory(path);
                if (manifest == null)
                {
                    _log.Warn($"Item '{item}' is missing a manifest, skipping.");
                    continue;
                }

                if (_plugins.ContainsKey(manifest.Guid))
                {
                    _log.Error($"The GUID provided by {manifest.Name} ({item}) is already in use by {_plugins[manifest.Guid].Name}");
                    continue;
                }

                if (isZip)
                    LoadPluginFromZip(path);
                else
                    LoadPluginFromFolder(path);
            }

            _plugins.ForEach(x => x.Value.Init(Torch));
            _log.Info($"Loaded {_plugins.Count} plugins.");
            PluginsLoaded?.Invoke(_plugins.Values.AsReadOnly());
        }

        private void DownloadPluginUpdates()
        {
            _log.Info("Checking for plugin updates...");
            var count = 0;
            var pluginItems = Directory.EnumerateFiles(PluginDir, "*.zip").Union(Directory.EnumerateDirectories(PluginDir));
            Parallel.ForEach(pluginItems, async item =>
            {
                PluginManifest manifest = null;
                try
                {
                    var path = Path.Combine(PluginDir, item);
                    var isZip = item.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase);
                    manifest = isZip ? GetManifestFromZip(path) : GetManifestFromDirectory(path);
                    if (manifest == null)
                    {
                        _log.Warn($"Item '{item}' is missing a manifest, skipping update check.");
                        return;
                    }

                    manifest.Version.TryExtractVersion(out Version currentVersion);
                    var latest = await GetLatestArchiveAsync(manifest.Repository).ConfigureAwait(false);

                    if (currentVersion == null || latest.Item1 == null)
                    {
                        _log.Error($"Error parsing version from manifest or GitHub for plugin '{manifest.Name}.'");
                        return;
                    }

                    if (latest.Item1 <= currentVersion)
                    {
                        _log.Debug($"{manifest.Name} {manifest.Version} is up to date.");
                        return;
                    }

                    _log.Info($"Updating plugin '{manifest.Name}' from {currentVersion} to {latest.Item1}.");
                    await UpdatePluginAsync(path, latest.Item2).ConfigureAwait(false);
                    count++;
                }
                catch (Exception e)
                {
                    _log.Error($"An error occurred updating the plugin {manifest.Name}.");
                    _log.Error(e);
                }
            });

            _log.Info($"Updated {count} plugins.");
        }

        private async Task<Tuple<Version, string>> GetLatestArchiveAsync(string repository)
        {
            try
            {
                var split = repository.Split('/');
                var latest = await _gitClient.Repository.Release.GetLatest(split[0], split[1]).ConfigureAwait(false);
                if (!latest.TagName.TryExtractVersion(out Version latestVersion))
                {
                    _log.Error($"Unable to parse version tag for the latest release of '{repository}.'");
                }

                var zipAsset = latest.Assets.FirstOrDefault(x => x.Name.Contains(".zip", StringComparison.CurrentCultureIgnoreCase));
                if (zipAsset == null)
                {
                    _log.Error($"Unable to find archive for the latest release of '{repository}.'");
                }

                return new Tuple<Version, string>(latestVersion, zipAsset?.BrowserDownloadUrl);
            }
            catch (Exception e)
            {
                _log.Error($"Unable to get the latest release of '{repository}.'");
                _log.Error(e);
                return default(Tuple<Version, string>);
            }
        }

        private Task UpdatePluginAsync(string localPath, string downloadUrl)
        {
            if (File.Exists(localPath))
                File.Delete(localPath);

            if (Directory.Exists(localPath))
                Directory.Delete(localPath, true);

            var fileName = downloadUrl.Split('/').Last();
            var filePath = Path.Combine(PluginDir, fileName);

            return new WebClient().DownloadFileTaskAsync(downloadUrl, filePath);
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
                if (!file.Contains(".dll", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                using (var stream = File.OpenRead(file))
                {
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    Assembly asm = Assembly.Load(data);
                    assemblies.Add(asm);
                    TorchBase.RegisterAuxAssembly(asm);
                }
            }

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
                    if (!entry.Name.Contains(".dll", StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    using (var stream = entry.Open())
                    {
                        var data = new byte[entry.Length];
                        stream.Read(data, 0, data.Length);
                        Assembly asm = Assembly.Load(data);
                        TorchBase.RegisterAuxAssembly(asm);
                    }
                }
            }

            InstantiatePlugin(manifest, assemblies);
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
            foreach (var asm in assemblies)
            {
                foreach (var type in asm.GetExportedTypes())
                {
                    if (!type.GetInterfaces().Contains(typeof(ITorchPlugin)))
                        continue;

                    if (pluginType != null)
                    {
                        _log.Error($"The plugin '{manifest.Name}' has multiple implementations of {nameof(ITorchPlugin)}, not loading.");
                        return;
                    }

                    pluginType = type;
                }
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
            var plugin = (TorchPluginBase)Activator.CreateInstance(pluginType);

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
