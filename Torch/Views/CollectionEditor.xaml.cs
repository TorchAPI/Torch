using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace Torch.Views
{
    /// <summary>
    /// Interaction logic for CollectionEditor.xaml
    /// </summary>
    public partial class CollectionEditor : Window
    {
        public CollectionEditor()
        {
            InitializeComponent();
        }

        public void Edit<T>(ICollection<T> collection, string name)
        {
            ItemList.Text = string.Join("\r\n", collection.Select(x => x.ToString()));
            Title = name;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowDialog();

            var parsed = new List<T>();
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                foreach (var item in ItemList.Text.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries))
                {
                    parsed.Add((T)converter.ConvertFromString(item));
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error parsing list, check your input.", "Edit Error");
                return;
            }

            collection.Clear();
            foreach (var item in parsed)
                collection.Add(item);

        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
