using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using VRage.Game;

namespace Torch.Views
{
    /// <summary>
    /// Interaction logic for PropertyGrid.xaml
    /// </summary>
    public partial class PropertyGrid : UserControl
    {
        private Dictionary<Type, Grid> _viewCache = new Dictionary<Type, Grid>();

        public PropertyGrid()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
            {
                ScrollViewer.Content = null;
                return;
            }

            var content = GenerateForType(e.NewValue.GetType());
            content.DataContext = e.NewValue;
            ScrollViewer.Content = content;
        }

        public Grid GenerateForType(Type t)
        {
            if (_viewCache.TryGetValue(t, out var v))
                return v;

            var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var curRow = 0;
            foreach (var property in properties.OrderBy(x => x.Name))
            {
                if (property.GetGetMethod() == null)
                    continue;

                grid.RowDefinitions.Add(new RowDefinition());

                var displayName = property.GetCustomAttribute<DisplayAttribute>()?.Name;

                var text = new TextBlock
                {
                    Text = property.Name,
                    ToolTip = displayName,
                    VerticalAlignment = VerticalAlignment.Center
                };
                text.SetValue(Grid.ColumnProperty, 0);
                text.SetValue(Grid.RowProperty, curRow);
                text.Margin = new Thickness(3);
                grid.Children.Add(text);

                FrameworkElement valueControl;
                if (property.GetSetMethod() == null)
                {
                    valueControl = new TextBlock();
                    var binding = new Binding(property.Name)
                    {
                        Mode = BindingMode.OneWay
                    };
                    valueControl.SetBinding(TextBlock.TextProperty, binding);
                }
                else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                {
                    valueControl = new CheckBox();
                    valueControl.SetBinding(CheckBox.IsCheckedProperty, property.Name);
                }
                else if (property.PropertyType.IsEnum)
                {
                    valueControl = new ComboBox
                    {
                        ItemsSource = Enum.GetValues(property.PropertyType)
                    };
                    valueControl.SetBinding(ComboBox.SelectedItemProperty, property.Name);
                }
                else
                {
                    valueControl = new TextBox();
                    valueControl.SetBinding(TextBox.TextProperty, property.Name);
                }

                valueControl.Margin = new Thickness(3);
                valueControl.VerticalAlignment = VerticalAlignment.Center;
                valueControl.SetValue(Grid.ColumnProperty, 1);
                valueControl.SetValue(Grid.RowProperty, curRow);
                grid.Children.Add(valueControl);

                curRow++;
            }

            _viewCache.Add(t, grid);
            return grid;
        }

        private void UpdateFilter(object sender, TextChangedEventArgs e)
        {
            var filterText = ((TextBox)sender).Text;
            var grid = (Grid)ScrollViewer.Content;
            foreach (var child in grid.Children)
            {
                if (!(child is TextBlock block))
                    continue;

                var rowNum = (int)block.GetValue(Grid.RowProperty);
                var row = grid.RowDefinitions[rowNum];

                if (block.Text.Contains(filterText, StringComparison.InvariantCultureIgnoreCase))
                {
                    row.Height = GridLength.Auto;
                }
                else
                {
                    row.Height = new GridLength(0);
                }
            }
        }
    }
}
