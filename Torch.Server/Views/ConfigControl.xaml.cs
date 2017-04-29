using System;
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
using NLog;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Torch.Server.ViewModels;
using VRage.Dedicated;
using VRage.Game;
using Path = System.IO.Path;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for ConfigControl.xaml
    /// </summary>
    public partial class ConfigControl : UserControl
    {
        public MyConfigDedicated<MyObjectBuilder_SessionSettings> Config { get; set; }
        private ConfigDedicatedViewModel _viewModel;
        private string _configPath;

        public ConfigControl()
        {
            InitializeComponent();
        }

        public void SaveConfig()
        {
            Config.Save(_configPath);
            //TODO: make this work
            try
            {
                var checkpoint = MyLocalCache.LoadCheckpoint(_viewModel.LoadWorld, out ulong size);
                checkpoint.SessionName = _viewModel.WorldName;
                checkpoint.Settings = _viewModel.SessionSettings;
                checkpoint.Mods.Clear();
                foreach (var modId in _viewModel.Mods)
                    checkpoint.Mods.Add(new MyObjectBuilder_Checkpoint.ModItem(modId));

                Debug.Assert(checkpoint != null);
                Debug.Assert(_viewModel.LoadWorld != null);
                MyLocalCache.SaveCheckpoint(checkpoint, _viewModel.LoadWorld);
            }
            catch (Exception e)
            {
                var log = LogManager.GetLogger("Torch");
                log.Error("Failed to overwrite sandbox config, changes will not appear on server");
                log.Error(e);
            }
        }

        public void LoadDedicatedConfig(TorchConfig torchConfig)
        {
            MySandboxGame.Config = new MyConfig(MyPerServerSettings.GameNameSafe + ".cfg");
            var path = Path.Combine(torchConfig.InstancePath, "SpaceEngineers-Dedicated.cfg");

            if (!File.Exists(path))
            {
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

            DataContext = _viewModel;
        }

        private void Banned_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var editor = new CollectionEditor {Owner = Window.GetWindow(this)};
            editor.Edit(_viewModel.Banned, "Banned Players");
        }

        private void Administrators_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var editor = new CollectionEditor { Owner = Window.GetWindow(this) };
            editor.Edit(_viewModel.Administrators, "Administrators");
        }

        private void Mods_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var editor = new CollectionEditor { Owner = Window.GetWindow(this) };
            editor.Edit(_viewModel.Mods, "Mods");
        }
    }
}
