using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using Sandbox.Engine.Networking;
using SteamSDK;
using VRage.Game;

namespace Piston.Server
{
    /// <summary>
    /// Interaction logic for ModsControl.xaml
    /// </summary>
    public partial class ModsControl : UserControl
    {
        public ModsControl()
        {
            InitializeComponent();
        }

        private void addBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddModsDialog();
            dialog.ShowDialog();

            foreach (var id in dialog.Result)
            {
                var details = SteamHelper.GetItemDetails(id);
                if (details.FileType != WorkshopFileType.Community)
                    continue;

                var item = SteamHelper.GetModItem(details);
                var desc = string.Join("\n", details.Description.ReadLines(5, true), "Double click to open the workshop page.");
                ModList.Items.Add(new ModViewModel(item, desc));
            }
        }

        private void modList_OnMouesDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var box = (ListView)sender;
            if (box.SelectedItem == null)
                return;

            var id = ((ModViewModel)box.SelectedItem).PublishedFileId;
            Process.Start($"http://steamcommunity.com/sharedfiles/filedetails/?id={id}");
        }

        private void remBtn_Click(object sender, RoutedEventArgs e)
        {
            var box = (ListView)sender;
            if (box.SelectedItems == null || box.SelectedItems.Count == 0)
                return;

            foreach (var item in box.SelectedItems)
            {
                box.Items.Remove(item);
            }
        }
    }
}
