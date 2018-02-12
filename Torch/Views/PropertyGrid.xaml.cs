using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
using NLog;
using NLog.Fluent;
using VRage.Game;
using VRage.Serialization;

namespace Torch.Views
{
    /// <summary>
    /// Interaction logic for PropertyGrid.xaml
    /// </summary>
    public partial class PropertyGrid : UserControl
    {
        private Dictionary<Type, Grid> _viewCache = new Dictionary<Type, Grid>();

        public static readonly DependencyProperty IgnoreDisplayProperty = DependencyProperty.Register("IgnoreDisplay", typeof(bool), typeof(PropertyGrid));

        public bool IgnoreDisplay
        {
            get => (bool)base.GetValue(IgnoreDisplayProperty);
            set => base.SetValue(IgnoreDisplayProperty, value);
        }

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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var categories = new Dictionary<string, List<PropertyInfo>>();
            var descriptors = new Dictionary<PropertyInfo, DisplayAttribute>(properties.Length);

            foreach (var property in properties)
            {
                var a = property.GetCustomAttribute<DisplayAttribute>();
                if (IgnoreDisplay)
                    a = null;
                descriptors[property] = a;
                string category = a?.GroupName ?? "Misc";

                if (!categories.TryGetValue(category, out List<PropertyInfo> l))
                {
                    l = new List<PropertyInfo>();
                    categories[category] = l;
                }
                l.Add(property);
            }

            var curRow = 0;
            foreach (var c in categories.OrderBy(x => x.Key))
            {
                grid.RowDefinitions.Add(new RowDefinition());
                var cl = new TextBlock
                         {
                             Text = c.Key,
                             VerticalAlignment = VerticalAlignment.Center
                         };
                cl.SetValue(Grid.ColumnProperty, 0);
                cl.SetValue(Grid.ColumnSpanProperty, 2);
                cl.SetValue(Grid.RowProperty, curRow);
                cl.Margin = new Thickness(3);
                cl.FontWeight = FontWeights.Bold;
                grid.Children.Add(cl);
                curRow++;

                c.Value.Sort((a,b)=> string.Compare((descriptors[a]?.Name ?? a.Name), descriptors[b]?.Name ?? b.Name, StringComparison.Ordinal));

                foreach (var property in c.Value)
                {
                    if (property.GetGetMethod() == null)
                        continue;

                    grid.RowDefinitions.Add(new RowDefinition());

                    var descriptor = descriptors[property];
                    var displayName = descriptor?.Name;
                    var propertyType = property.PropertyType;

                    var text = new TextBlock
                               {
                                   Text = displayName ?? property.Name,
                                   ToolTip = displayName,
                                   VerticalAlignment = VerticalAlignment.Center
                               };
                    text.SetValue(Grid.ColumnProperty, 0);
                    text.SetValue(Grid.RowProperty, curRow);
                    text.Margin = new Thickness(3);
                    text.Tag = $"{text.Text}: {descriptor?.Description}";
                    text.IsMouseDirectlyOverChanged += Text_IsMouseDirectlyOverChanged;
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
                    else if (propertyType == typeof(bool) || propertyType == typeof(bool?))
                    {
                        valueControl = new CheckBox();
                        valueControl.SetBinding(CheckBox.IsCheckedProperty, property.Name);
                    }
                    else if (propertyType.IsEnum)
                    {
                        valueControl = new ComboBox
                                       {
                                           ItemsSource = Enum.GetValues(property.PropertyType)
                                       };
                        valueControl.SetBinding(ComboBox.SelectedItemProperty, property.Name);
                    }
                    else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                        var button = new Button
                                     {
                                         Content = "Edit Collection"
                                     };
                        button.SetBinding(Button.DataContextProperty, property.Name);
                        button.Click += (sender, args) => EditDictionary(((Button)sender).DataContext);

                        valueControl = button;
                    }
                    else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(SerializableDictionary<,>))
                    {
                        var button = new Button
                                     {
                                         Content = "Edit Collection"
                                     };
                        button.SetBinding(Button.DataContextProperty, $"{property.Name}.Dictionary");
                        button.Click += (sender, args) => EditDictionary(((Button)sender).DataContext);

                        valueControl = button;
                    }
                    else if (propertyType.IsGenericType && typeof(ICollection).IsAssignableFrom(propertyType.GetGenericTypeDefinition()))
                    {
                        var button = new Button
                                     {
                                         Content = "Edit Collection"
                                     };
                        button.SetBinding(Button.DataContextProperty, $"{property.Name}");

                        var gt = propertyType.GetGenericArguments()[0];

                        //TODO: Is this the best option? Probably not
                        if (gt.IsPrimitive || gt == typeof(string))
                        {
                            button.Click += (sender, args) => EditPrimitiveCollection(((Button)sender).DataContext);
                        }
                        else
                        {
                            button.Click += (sender, args) => EditObjectCollection(((Button)sender).DataContext);
                        }

                        valueControl = button;
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
                    valueControl.IsMouseDirectlyOverChanged += Text_IsMouseDirectlyOverChanged;
                    grid.Children.Add(valueControl);

                    curRow++;
                }
            }

            _viewCache.Add(t, grid);
            return grid;
        }

        private void Text_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TbDescription.Text = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;
        }

        private void EditDictionary(object dict)
        {
            var dic = (IDictionary)dict;
            new DictionaryEditorDialog().Edit(dic);
        }

        private void EditPrimitiveCollection(object collection, string title = "Collection Editor")
        {
            var c = (ICollection)collection;
            new CollectionEditor().Edit(c, title);
        }

        private void EditObjectCollection(object collection, string title = "Collection Editor")
        {
            var c = (ICollection)collection;
            new ObjectCollectionEditor().Edit(c, title);
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
