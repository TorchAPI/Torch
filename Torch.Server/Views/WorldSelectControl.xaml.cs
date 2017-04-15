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
using Sandbox.Game.Gui;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for WorldSelectControl.xaml
    /// </summary>
    public partial class WorldSelectControl : UserControl
    {
        public WorldSelectControl()
        {
            InitializeComponent();
            //LoadWorlds();
        }

        public void LoadWorlds(string path = null)
        {
            WorldList.Items.Clear();
            var worlds = new MyLoadWorldInfoListResult(path);
            worlds.Task.Wait();

            foreach (var world in worlds.AvailableSaves)
            {
                WorldList.Items.Add(world.Item1);
            }
        }
    }
}
