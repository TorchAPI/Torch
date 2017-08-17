using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Havok;
using NLog;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Server.ViewModels;
using VRage.FileSystem;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Torch.Server.Managers
{
    public class InstanceManager : Manager
    {
        private const string CONFIG_NAME = "SpaceEngineers-Dedicated.cfg";
        public ConfigDedicatedViewModel DedicatedConfig { get; set; }
        private static readonly Logger Log = LogManager.GetLogger(nameof(InstanceManager));

        public InstanceManager(ITorchBase torchInstance) : base(torchInstance)
        {
            
        }

        /// <inheritdoc />
        public override void Init()
        {
            MyFileSystem.ExePath = Path.Combine(Torch.GetManager<FilesystemManager>().TorchDirectory, "DedicatedServer64");
            MyFileSystem.Init("Content", Torch.Config.InstancePath);
            //Initializes saves path. Why this isn't in Init() we may never know.
            MyFileSystem.InitUserSpecific(null);
        }

        public void LoadInstance(string path, bool validate = true)
        {
            if (validate)
                ValidateInstance(path);

            MyFileSystem.Reset();
            MyFileSystem.ExePath = Path.Combine(Torch.GetManager<FilesystemManager>().TorchDirectory, "DedicatedServer64");
            MyFileSystem.Init("Content", path);
            //Initializes saves path. Why this isn't in Init() we may never know.
            MyFileSystem.InitUserSpecific(null);

            var configPath = Path.Combine(path, CONFIG_NAME);
            if (!File.Exists(configPath))
            {
                Log.Error($"Failed to load dedicated config at {path}");
                return;
            }

            var config = new MyConfigDedicated<MyObjectBuilder_SessionSettings>(configPath);
            config.Load(configPath);

            DedicatedConfig = new ConfigDedicatedViewModel(config);
            var worldFolders = Directory.EnumerateDirectories(Path.Combine(Torch.Config.InstancePath, "Saves"));

            foreach (var f in worldFolders)
                DedicatedConfig.WorldPaths.Add(f);

            if (DedicatedConfig.WorldPaths.Count == 0)
            {
                Log.Warn($"No worlds found in the current instance {path}.");
                return;
            }

            ImportWorldConfig();

            /*
            if (string.IsNullOrEmpty(DedicatedConfig.LoadWorld))
            {
                Log.Warn("No world specified, importing first available world.");
                SelectWorld(DedicatedConfig.WorldPaths[0], false);
            }*/
        }

        public void SelectWorld(string worldPath, bool modsOnly = true)
        {
            DedicatedConfig.LoadWorld = worldPath;
            ImportWorldConfig(modsOnly);
        }


        private void ImportWorldConfig(bool modsOnly = true)
        {
            if (string.IsNullOrEmpty(DedicatedConfig.LoadWorld))
                return;

            var sandboxPath = Path.Combine(DedicatedConfig.LoadWorld, "Sandbox.sbc");

            if (!File.Exists(sandboxPath))
                return;

            try
            {
                MyObjectBuilderSerializer.DeserializeXML(sandboxPath, out MyObjectBuilder_Checkpoint checkpoint, out ulong sizeInBytes);
                if (checkpoint == null)
                {
                    Log.Error($"Failed to load {DedicatedConfig.LoadWorld}, checkpoint null ({sizeInBytes} bytes, instance {TorchBase.Instance.Config.InstancePath})");
                    return;
                }

                var sb = new StringBuilder();
                foreach (var mod in checkpoint.Mods)
                    sb.AppendLine(mod.PublishedFileId.ToString());

                DedicatedConfig.Mods = sb.ToString();

                Log.Debug("Loaded mod list from world");

                if (!modsOnly)
                    DedicatedConfig.SessionSettings = new SessionSettingsViewModel(checkpoint.Settings);
            }
            catch (Exception e)
            {
                Log.Error($"Error loading mod list from world, verify that your mod list is accurate. '{DedicatedConfig.LoadWorld}'.");
                Log.Error(e);
            }
        }

        public void SaveConfig()
        {
            DedicatedConfig.Save();
            Log.Info("Saved dedicated config.");

            try
            {
                MyObjectBuilderSerializer.DeserializeXML(Path.Combine(DedicatedConfig.LoadWorld, "Sandbox.sbc"), out MyObjectBuilder_Checkpoint checkpoint, out ulong sizeInBytes);
                if (checkpoint == null)
                {
                    Log.Error($"Failed to load {DedicatedConfig.LoadWorld}, checkpoint null ({sizeInBytes} bytes, instance {TorchBase.Instance.Config.InstancePath})");
                    return;
                }
                checkpoint.Settings = DedicatedConfig.SessionSettings;
                checkpoint.Mods.Clear();
                foreach (var modId in DedicatedConfig.Model.Mods)
                    checkpoint.Mods.Add(new MyObjectBuilder_Checkpoint.ModItem(modId));

                MyLocalCache.SaveCheckpoint(checkpoint, DedicatedConfig.LoadWorld);
                Log.Info("Saved world config.");
            }
            catch (Exception e)
            {
                Log.Error("Failed to write sandbox config, changes will not appear on server");
                Log.Error(e);
            }
        }

        /// <summary>
        /// Ensures that the given path is a valid server instance.
        /// </summary>
        private void ValidateInstance(string path)
        {
            Directory.CreateDirectory(Path.Combine(path, "Saves"));
            Directory.CreateDirectory(Path.Combine(path, "Mods"));
            var configPath = Path.Combine(path, CONFIG_NAME);
            if (File.Exists(configPath))
                return;

            var config = new MyConfigDedicated<MyObjectBuilder_SessionSettings>(configPath);
            config.Save(configPath);
        }
    }
}
