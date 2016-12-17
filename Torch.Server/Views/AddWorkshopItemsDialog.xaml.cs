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

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for AddModsDialog.xaml
    /// </summary>
    public partial class AddModsDialog : Window
    {
        public ulong[] Result = new ulong[0];
        public AddModsDialog()
        {
            InitializeComponent();
        }

        private void Done_Clicked(object sender, RoutedEventArgs e)
        {
            var text = urlBlock.Text;
            var lines = text.Split('\n');
            Result = new ulong[lines.Length];
            for (var i = 0; i < lines.Length; i++)
            {
                Result[i] = ExtractId(lines[i]);
            }

            Close();
        }

        private ulong ExtractId(string input)
        {
            ulong result;
            var match = Regex.Match(input, @"(?<=id=)\d+").Value;

            if (string.IsNullOrEmpty(match))
                ulong.TryParse(input, out result);
            else
                ulong.TryParse(match, out result);

            return result;
        }
    }
}
