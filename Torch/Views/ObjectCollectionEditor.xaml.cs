using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
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
using NLog.Fluent;

namespace Torch.Views
{
    /// <summary>
    /// Interaction logic for ObjectCollectionEditor.xaml
    /// </summary>
    public partial class ObjectCollectionEditor : Window
    {
        public ObjectCollectionEditor()
        {
            InitializeComponent();
        }

        public void Edit(ICollection collection, string title)
        {
            Editor.Edit(collection);
            Title = title;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowDialog();
        }
    }
}
