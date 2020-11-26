using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NLog;
using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Game.World;
using Torch.Server.Managers;
using Torch.Server.ViewModels;
using Torch.Utils;
using VRage;
using VRage.Dedicated;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Localization;
using VRage.Utils;

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for WorldGeneratorDialog.xaml
    /// </summary>
    public partial class WorldGeneratorDialog : Window
    {
        private InstanceManager _instanceManager;
        private List<PremadeCheckpointItem> _checkpoints = new List<PremadeCheckpointItem>();
        private PremadeCheckpointItem _currentItem;

        [ReflectedStaticMethod(Type = typeof(ConfigForm), Name = "LoadLocalization")]
        private static Action _loadLocalization;

        public WorldGeneratorDialog(InstanceManager instanceManager)
        {
            _instanceManager = instanceManager;
            InitializeComponent();
            _loadLocalization();
            var scenarios = MyLocalCache.GetAvailableWorldInfos(new List<string> {Path.Combine(MyFileSystem.ContentPath, "CustomWorlds")});
            foreach (var tup in scenarios)
            {
                if (tup.Item2 == null)
                    continue;
                
                string directory = tup.Item1;
                MyWorldInfo info = tup.Item2;
                var sessionNameId = MyStringId.GetOrCompute(info.SessionName);
                string localizedName = MyTexts.GetString(sessionNameId);
                var checkpoint = MyLocalCache.LoadCheckpoint(directory, out _);
                checkpoint.OnlineMode = MyOnlineModeEnum.PUBLIC;
                // Keen, why do random checkpoints point to the SBC and not the folder!
                directory = directory.Replace("Sandbox.sbc", "");
                _checkpoints.Add(new PremadeCheckpointItem { Name = localizedName, Icon = Path.Combine(directory, "thumb.jpg"), Path = directory, Checkpoint = checkpoint});
            }

            /*
            var premadeCheckpoints = Directory.EnumerateDirectories(Path.Combine("Content", "CustomWorlds"));
            foreach (var path in premadeCheckpoints)
            {
                var thumbPath = Path.GetFullPath(Directory.EnumerateFiles(path).First(x => x.Contains("thumb")));

                _checkpoints.Add(new PremadeCheckpointItem
                {
                    Path = path,
                    Icon = thumbPath,
                    Name = Path.GetFileName(path)
                });
            }*/
            PremadeCheckpoints.ItemsSource = _checkpoints;
        }
        
        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            string worldName = string.IsNullOrEmpty(WorldName.Text) ? _currentItem.Name : WorldName.Text;
            
            var worldPath = Path.Combine(TorchBase.Instance.Config.InstancePath, "Saves", worldName);
            var checkpoint = _currentItem.Checkpoint;
            if (Directory.Exists(worldPath))
            {
                MessageBox.Show("World already exists with that name.");
                return;
            }
            Directory.CreateDirectory(worldPath);
            foreach (var file in Directory.EnumerateFiles(_currentItem.Path, "*", SearchOption.AllDirectories))
            {
                File.Copy(file, Path.Combine(worldPath, file.Replace($"{_currentItem.Path}\\", "")));
            }

            checkpoint.SessionName = worldName;

            MyLocalCache.SaveCheckpoint(checkpoint, worldPath);


            _instanceManager.SelectWorld(worldPath, false);
            _instanceManager.ImportSelectedWorldConfig();
            Close();
        }

        private void PremadeCheckpoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (PremadeCheckpointItem)PremadeCheckpoints.SelectedItem;
            _currentItem = selected;
            SettingsView.DataContext = new SessionSettingsViewModel(_currentItem.Checkpoint.Settings);
        }
    }

    public class PremadeCheckpointItem
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public MyObjectBuilder_Checkpoint Checkpoint { get; set; }
    }
}
