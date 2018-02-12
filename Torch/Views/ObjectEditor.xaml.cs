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

namespace Torch.Views
{
    /// <summary>
    /// Interaction logic for ObjectEditor.xaml
    /// </summary>
    public partial class ObjectEditor : Window
    {
        public ObjectEditor()
        {
            InitializeComponent();
        }

        public void Edit(object o, string title = "Edit Object")
        {
            PGrid.DataContext = o;
            Title = title;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowDialog();
        }
    }
}
