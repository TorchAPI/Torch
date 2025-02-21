using System.Text.RegularExpressions;
using System.Windows;

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
