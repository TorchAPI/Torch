using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Torch.Server.ViewModels;
using VRage.Game;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for AddModDisalog.xaml
    /// </summary>
    public partial class AddModDisalog : Window
    {
        public ModItemInfo Result;
        public AddModDisalog()
        {
            InitializeComponent();
            DataContext = new ModItemInfo(new MyObjectBuilder_Checkpoint.ModItem());
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
