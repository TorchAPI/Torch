using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Torch.Server
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
            ulong[] mods;
            if (!string.IsNullOrEmpty(ModIdBox.Text))
            {
                mods = new ulong[1];
                ulong.TryParse(ModIdBox.Text, out mods[0]);
            }
            else
            {
                var dialog = new AddModsDialog();
                dialog.ShowDialog();
                mods = dialog.Result;
            }

            //foreach (var id in mods)
            //{
            //    var details = SteamHelper.GetItemDetails(id);
            //    if (details.FileType != WorkshopFileType.Community)
            //        continue;

            //    var item = SteamHelper.GetModItem(details);
            //    var desc = details.Description.Length < 500 ? details.Description : details.Description.Substring(0, 500);
            //    ModList.Items.Add(new ModViewModel(item, desc));
            //}
        }

        private void modList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
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
