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
using System.Windows.Shapes;

namespace Torch.Launcher
{
    /// <summary>
    /// Interaction logic for SpaceDirPrompt.xaml
    /// </summary>
    public partial class SpaceDirPrompt : Window
    {
        public string SelectedDir { get; private set; }
        public bool Success { get; private set; }
        public SpaceDirPrompt()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(PathBox.Text))
            {
                MessageBox.Show(this, "That's not a valid directory.");
                return;
            }

            if (!Directory.GetFiles(PathBox.Text).Any(i => i.Contains("SpaceEngineers.exe")))
            {
                MessageBox.Show(this, "SE was not found in the given directory.");
                return;
            }

            Success = true;
            SelectedDir = PathBox.Text;
            Close();
        }
    }
}
