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
        private List<string> _worldNames = new List<string>();
        private PremadeCheckpointItem _currentItem;

        [ReflectedStaticMethod(Type = typeof(ConfigForm), Name = "LoadLocalization")]
        private static Action _loadLocalization;

        public WorldGeneratorDialog(InstanceManager instanceManager)
        {
            _instanceManager = instanceManager;
            InitializeComponent();
            _loadLocalization();

            string worldsDir = Path.Combine(MyFileSystem.ContentPath, "CustomWorlds");
            var result = new List<Tuple<string,MyWorldInfo>>();
            
            GetWorldInfo(worldsDir, result);
            
            foreach (var tup in result)
            {
                string directory = tup.Item1;
                MyWorldInfo info = tup.Item2;
                var sessionNameId = MyStringId.GetOrCompute(info.SessionName);
                string localizedName = MyTexts.GetString(sessionNameId);
                var checkpoint = MyLocalCache.LoadCheckpoint(directory, out _);
                checkpoint.OnlineMode = MyOnlineModeEnum.PUBLIC;
                // Keen, why do random checkpoints point to the SBC and not the folder!
                directory = directory.Replace("Sandbox.sbc", "");
                _checkpoints.Add(new PremadeCheckpointItem
                {
                    Name = localizedName, Icon = Path.Combine(directory, "thumb.jpg"), Path = directory,
                    Checkpoint = checkpoint
                });
            }

            foreach (var checkpoint in _checkpoints)
            {
                _worldNames.Add(checkpoint.Name);
            }

            PremadeCheckpoints.ItemsSource = _worldNames;
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
                // Trash code to work around inconsistent path formats.
                var fileRelPath = file.Replace($"{_currentItem.Path.TrimEnd('\\')}\\", "");
                var destPath = Path.Combine(worldPath, fileRelPath);
                File.Copy(file, destPath);
            }

            checkpoint.SessionName = worldName;

            MyLocalCache.SaveCheckpoint(checkpoint, worldPath);


            _instanceManager.SelectWorld(worldPath, false);
            _instanceManager.ImportSelectedWorldConfig();
            Close();
        }

        private void PremadeCheckpoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = _checkpoints.FirstOrDefault(x => x.Name == PremadeCheckpoints.SelectedItem.ToString());
            _currentItem = selected;
            if (_currentItem == null) return;
            SettingsView.DataContext = new SessionSettingsViewModel(_currentItem.Checkpoint.Settings);
            CheckpointImage.Source = new BitmapImage(new Uri(_currentItem.Icon));
        }
        
         private void GetWorldInfo(string savesPath, List<Tuple<string, MyWorldInfo>> result)
        {
            foreach (var saveDir in Directory.GetDirectories(savesPath, "*", SearchOption.TopDirectoryOnly))            
            {
                bool isCompatible;
                string platformSessionPath = null;
                
                platformSessionPath = MyLocalCache.GetSessionPathFromScenario(saveDir, false, out isCompatible);
                if (platformSessionPath != null && isCompatible)
                {
                    AddWorldInfo(result, platformSessionPath, saveDir, " [PC]");
                }
                
                string xboxPlatformSessionPath = MyLocalCache.GetSessionPathFromScenario(saveDir, true, out isCompatible);
                if (xboxPlatformSessionPath != null && isCompatible)
                {
                    AddWorldInfo(result, xboxPlatformSessionPath, saveDir, " [XBOX]");
                }

                if (platformSessionPath == null && xboxPlatformSessionPath == null && isCompatible)
                {
                    AddWorldInfo(result, saveDir, saveDir, "");
                }
            }
        }
        
        private static void AddWorldInfo(List<Tuple<string, MyWorldInfo>> result, string sessionDir, string saveDir, string namePostfix)
        {
            MyWorldInfo worldInfo = null;
            var worldConfiguration = MyLocalCache.LoadWorldConfiguration(sessionDir);
            if (worldConfiguration == null)
            {
                worldInfo = MyLocalCache.LoadWorldInfoFromFile(sessionDir);
            }
            else
            {
                if (string.IsNullOrEmpty(worldConfiguration.SessionName) || !worldConfiguration.LastSaveTime.HasValue)
                {
                    worldInfo = MyLocalCache.LoadWorldInfoFromFile(sessionDir);
                }
                else
                {
                    worldInfo = new MyWorldInfo
                    {
                        SessionName = worldConfiguration.SessionName,
                        LastSaveTime = worldConfiguration.LastSaveTime.Value
                    };
                }

                if (worldInfo != null && string.IsNullOrEmpty(worldInfo.SessionName))
                {
                    worldInfo.SessionName = Path.GetFileName(sessionDir);
                }
            }

            if (worldInfo != null)
            {
                worldInfo.SessionName += namePostfix;
            }

            worldInfo.SaveDirectory = saveDir;
            result.Add(Tuple.Create(sessionDir, worldInfo));
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
