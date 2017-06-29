using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using NLog;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Torch.Server.ViewModels;
using Torch.Views;
using VRage;
using VRage.Dedicated;
using VRage.Game;
using VRage.ObjectBuilders;
using Path = System.IO.Path;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for ConfigControl.xaml
    /// </summary>
    public partial class ConfigControl : UserControl
    {
        private readonly Logger Log = LogManager.GetLogger("Config");
        public MyConfigDedicated<MyObjectBuilder_SessionSettings> Config { get; set; }
        private ConfigDedicatedViewModel _viewModel;
        private string _configPath;
        private TorchConfig _torchConfig;

        public ConfigControl()
        {
            InitializeComponent();
        }

        public void SaveConfig()
        {
            _viewModel.Save(_configPath);
            Log.Info("Saved DS config.");
            try
            {
                //var checkpoint = MyLocalCache.LoadCheckpoint(Config.LoadWorld, out _);
                MyObjectBuilderSerializer.DeserializeXML(Path.Combine(Config.LoadWorld, "Sandbox.sbc"), out MyObjectBuilder_Checkpoint checkpoint, out ulong sizeInBytes);
                if (checkpoint == null)
                {
                    Log.Error($"Failed to load {Config.LoadWorld}, checkpoint null ({sizeInBytes} bytes, instance {TorchBase.Instance.Config.InstancePath})");
                    return;
                }
                checkpoint.Settings = Config.SessionSettings;
                checkpoint.Mods.Clear();
                foreach (var modId in Config.Mods)
                    checkpoint.Mods.Add(new MyObjectBuilder_Checkpoint.ModItem(modId));

                MyLocalCache.SaveCheckpoint(checkpoint, Config.LoadWorld);
                Log.Info("Saved world config.");
            }
            catch (Exception e)
            {
                Log.Error("Failed to write sandbox config, changes will not appear on server");
                Log.Error(e);
            }
        }

        public void LoadDedicatedConfig(TorchConfig torchConfig)
        {
            _torchConfig = torchConfig;
            DataContext = null;
            MySandboxGame.Config = new MyConfig(MyPerServerSettings.GameNameSafe + ".cfg");
            var path = Path.Combine(torchConfig.InstancePath, "SpaceEngineers-Dedicated.cfg");

            if (!File.Exists(path))
            {
                Log.Error($"Failed to load dedicated config at {path}");
                DataContext = null;
                return;
            }

            Config = new MyConfigDedicated<MyObjectBuilder_SessionSettings>(path);
            Config.Load(path);
            _configPath = path;

            _viewModel = new ConfigDedicatedViewModel(Config);
            var worldFolders = Directory.EnumerateDirectories(Path.Combine(torchConfig.InstancePath, "Saves"));
            
            foreach (var f in worldFolders)
                _viewModel.WorldPaths.Add(f);

            LoadWorldMods();
            DataContext = _viewModel;
        }

        private void LoadWorldMods()
        {
            var sandboxPath = Path.Combine(Config.LoadWorld, "Sandbox.sbc");

            if (!File.Exists(sandboxPath))
                return;

            MyObjectBuilderSerializer.DeserializeXML(sandboxPath, out MyObjectBuilder_Checkpoint checkpoint, out ulong sizeInBytes);
            if (checkpoint == null)
            {
                Log.Error($"Failed to load {Config.LoadWorld}, checkpoint null ({sizeInBytes} bytes, instance {TorchBase.Instance.Config.InstancePath})");
                return;
            }

            var sb = new StringBuilder();
            foreach (var mod in checkpoint.Mods)
                sb.AppendLine(mod.PublishedFileId.ToString());

            _viewModel.Mods = sb.ToString();

            Log.Info("Loaded mod list from world");
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }

        private void RemoveLimit_OnClick(object sender, RoutedEventArgs e)
        {
            var vm = (BlockLimitViewModel)((Button)sender).DataContext;
            _viewModel.SessionSettings.BlockLimits.Remove(vm);
        }

        private void AddLimit_OnClick(object sender, RoutedEventArgs e)
        {
            _viewModel.SessionSettings.BlockLimits.Add(new BlockLimitViewModel(_viewModel.SessionSettings, "", 0));
        }

        private void NewWorld_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Feature coming soon :)");
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //The control doesn't update the binding before firing the event.
            if (e.AddedItems.Count > 0)
            {
                Config.LoadWorld = (string)e.AddedItems[0];
                LoadWorldMods();
            }
        }
    }
}
