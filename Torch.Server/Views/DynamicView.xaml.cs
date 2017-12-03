using System;
using System.Collections.Generic;
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

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for DynamicView.xaml
    /// </summary>
    public partial class DynamicView : UserControl
    {
        private static Dictionary<Type, StackPanel> _map = new Dictionary<Type, StackPanel>();

        public DynamicView()
        {
            InitializeComponent();
            DataContextChanged += DynamicView_DataContextChanged;
        }

        private void DynamicView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var content = GenerateForType(e.NewValue.GetType());
            content.DataContext = e.NewValue;
            Content = content;
        }

        public static StackPanel GenerateForType(Type t)
        {
            if (_map.TryGetValue(t, out StackPanel v))
                return v;

            var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var panel = new StackPanel();

            foreach (var property in properties)
            {
                panel.Children.Add(GenerateDefault(property));
            }

            _map.Add(t, panel);
            return panel;
        }

        private static StackPanel GenerateBool(PropertyInfo propInfo)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var label = new Label { Content = propInfo.Name };
            var checkbox = new CheckBox();
            checkbox.SetBinding(CheckBox.IsCheckedProperty, propInfo.Name);

            panel.Children.Add(checkbox);
            panel.Children.Add(label);
            return panel;
        }

        private static StackPanel GenerateDefault(PropertyInfo propInfo)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var label = new Label { Content = propInfo.Name };
            var textbox = new TextBox();
            textbox.SetBinding(TextBox.TextProperty, propInfo.Name);

            panel.Children.Add(label);
            panel.Children.Add(textbox);
            return panel;
        }
    }
}
