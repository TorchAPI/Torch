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
using Torch.Server.Managers;
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

        public WorldGeneratorDialog(InstanceManager instanceManager)
        {
            _instanceManager = instanceManager;
            InitializeComponent();

            MyDefinitionManager.Static.LoadScenarios();
            var scenarios = MyDefinitionManager.Static.GetScenarioDefinitions();
            MyDefinitionManager.Static.UnloadData();
            foreach (var scenario in scenarios)
            {
                //TODO: Load localization
                _checkpoints.Add(new PremadeCheckpointItem { Name = scenario.DisplayNameText, Icon = @"C:\Users\jgross\Documents\Projects\TorchAPI\Torch\bin\x64\Release\Content\CustomWorlds\Empty World\thumb.jpg" });
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
            /*
            var worldPath = Path.Combine("Instance", "Saves", WorldName.Text);
            var checkpointItem = (PremadeCheckpointItem)PremadeCheckpoints.SelectedItem;
            if (Directory.Exists(worldPath))
            {
                MessageBox.Show("World already exists with that name.");
                return;
            }
            Directory.CreateDirectory(worldPath);
            foreach (var file in Directory.EnumerateFiles(checkpointItem.Path, "*", SearchOption.AllDirectories))
            {
                File.Copy(file, Path.Combine(worldPath, file.Replace($"{checkpointItem.Path}\\", "")));
            }
            _instanceManager.SelectWorld(worldPath, false);*/
        }
    }

    public class PremadeCheckpointItem
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
    }
}
