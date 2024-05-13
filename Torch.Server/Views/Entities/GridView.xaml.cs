using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using VRage.Game.Entity;

namespace Torch.Server.Views.Entities
{
    /// <summary>
    /// Interaction logic for GridView.xaml
    /// </summary>
    public partial class GridView : UserControl
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public GridView()
        {
            InitializeComponent();

            ThemeControl.UpdateDynamicControls += UpdateResourceDict;
            UpdateResourceDict(ThemeControl.currentTheme);
        }

        public void UpdateResourceDict(ResourceDictionary dictionary)
        {
            this.Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(dictionary);
        }

        protected virtual void RepairGrid(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (!long.TryParse(btn.Tag.ToString(), out long id)) return;
                if (id != 0)
                {
                    // MyEntities.GetEntityById() always returned null but this works... 
                    MyCubeGrid grid = MyEntities.GetEntities().Where(x=>x.GetType()==typeof(MyCubeGrid)).Cast<MyCubeGrid>().FirstOrDefault(x=>x.EntityId==id);
                    if (grid is null) return;
                    TorchBase.Instance.InvokeBlocking(() =>
                    {
                        try
                        {
                            foreach (MySlimBlock block in grid.GetBlocks())
                            {
                                block.IncreaseMountLevel(block.MaxIntegrity - block.BuildIntegrity, block.OwnerId);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);
                        }
                    });
                    return;
                }
            }

            Log.Warn("Cannot repair entity");
        }
    }
}
