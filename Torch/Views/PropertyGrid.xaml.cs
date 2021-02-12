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
                TbFilter.IsEnabled = false;
                return;
            }

            var content = GenerateForType(e.NewValue.GetType());
            content.DataContext = e.NewValue;
            ScrollViewer.Content = content;
            TbFilter.IsEnabled = true;
        }

        public Grid GenerateForType(Type t)
        {
            if (_viewCache.TryGetValue(t, out var v))
                return v;

            var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var grid = new Grid();
            grid.MouseMove += Grid_MouseMove;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            var categories = new Dictionary<string, List<PropertyInfo>>();
            var descriptors = new Dictionary<PropertyInfo, DisplayAttribute>(properties.Length);

            foreach (var property in properties)
            {
                //Attempt to load our custom DisplayAttribute
                var a = property.GetCustomAttribute<DisplayAttribute>();
                //If not found and IgnoreDisplay is not set, fall back to system DisplayAttribute
                if (a == null && !IgnoreDisplay)
                    a = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
                if (!IgnoreDisplay && a == null || a?.Visible == false)
                    continue;
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
                grid.RowDefinitions.Add(new RowDefinition(){Height = new GridLength(1, GridUnitType.Auto)});
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

                c.Value.Sort((a, b) =>
                             {
                                 var c1 = descriptors[a]?.Order.CompareTo(descriptors[b]?.Order);
                                 if (c1.HasValue && c1.Value != 0)
                                     return c1.Value;
                                 return string.Compare((descriptors[a]?.Name ?? a.Name), descriptors[b]?.Name ?? b.Name, StringComparison.Ordinal);
                             });

                foreach (var property in c.Value)
                {
                    if (property.GetGetMethod() == null)
                        continue;

                    var def = new RowDefinition(){Height = new GridLength(1, GridUnitType.Auto)};
                    grid.RowDefinitions.Add(def);

                    var descriptor = descriptors[property];
                    var displayName = descriptor?.Name;
                    var propertyType = property.PropertyType;

                    var text = new TextBlock
                               {
                                   Text = displayName ?? property.Name,
                                   ToolTip = descriptor?.ToolTip ?? displayName,
                                   VerticalAlignment = VerticalAlignment.Center
                               };
                    text.SetValue(Grid.ColumnProperty, 0);
                    text.SetValue(Grid.RowProperty, curRow);
                    text.Margin = new Thickness(3);
                    def.Tag = new Tuple<string, string>(text.Text, descriptor?.Description);
                    //if (descriptor?.Enabled == false)
                    //    text.IsEnabled = false;
                    grid.Children.Add(text);

                    FrameworkElement valueControl;
                    if (descriptor?.EditorType != null)
                    {
                        valueControl = (FrameworkElement)Activator.CreateInstance(descriptor.EditorType);
                        valueControl.SetBinding(FrameworkElement.DataContextProperty, property.Name);
                    }
                    else if (property.GetSetMethod() == null && !(propertyType.IsGenericType && typeof(ICollection).IsAssignableFrom(propertyType.GetGenericTypeDefinition()))|| descriptor?.ReadOnly == true)
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
                        var isFlags = propertyType.GetCustomAttribute<FlagsAttribute>() != null;

                        if (isFlags)
                        {
                            var button = new Button
                            {
                                Content = "Edit Flags"
                            };
                            button.SetBinding(Button.DataContextProperty, property.Name);
                            button.Click += EditFlags;

                            valueControl = button;
                        }
                        else
                        {
                            valueControl = new ComboBox
                            {
                                ItemsSource = Enum.GetValues(property.PropertyType)
                            };
                            valueControl.SetBinding(ComboBox.SelectedItemProperty, property.Name);
                        }
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
                        button.SetBinding(Button.DataContextProperty, property.Name);

                        var gt = propertyType.GetGenericArguments()[0];

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
                    else if (propertyType.IsPrimitive)
                    {
                        valueControl = new TextBox();
                        valueControl.SetBinding(TextBox.TextProperty, property.Name);
                    }
                    else if (propertyType == typeof(string))
                    {
                        var tb  = new TextBox();
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.AcceptsReturn = true;
                        tb.AcceptsTab = true;
                        tb.SpellCheck.IsEnabled = true;
                        tb.SetBinding(TextBox.TextProperty, property.Name);
                        valueControl = tb;
                    }
                    else
                    {
                        var button = new Button
                                     {
                                         Content = "Edit Object"
                                     };
                        button.SetBinding(Button.DataContextProperty, property.Name);
                        button.Click += (sender, args) => EditObject(((Button)sender).DataContext);
                        valueControl = button;
                    }

                    valueControl.Margin = new Thickness(3);
                    valueControl.VerticalAlignment = VerticalAlignment.Center;
                    valueControl.SetValue(Grid.ColumnProperty, 1);
                    valueControl.SetValue(Grid.RowProperty, curRow);
                    if (descriptor?.Enabled == false)
                        valueControl.IsEnabled = false;
                    grid.Children.Add(valueControl);

                    curRow++;
                }
            }

            _viewCache.Add(t, grid);
            return grid;
        }

        private int _lastActiveRow;
        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            var grid = (Grid)sender;
            var mousePoint = e.GetPosition(grid);
            var heightSum = grid.RowDefinitions[0].ActualHeight;
            var activeRow = 0;
            
            while (heightSum < mousePoint.Y && activeRow < grid.RowDefinitions.Count)
            {
                heightSum += grid.RowDefinitions[activeRow].ActualHeight;
                activeRow++;
            }

            if (activeRow > grid.RowDefinitions.Count - 1 || activeRow == _lastActiveRow)
                return;

            _lastActiveRow = activeRow;
            var tag = (Tuple<string, string>)grid.RowDefinitions[activeRow].Tag;
            
            TbDescription.Inlines.Clear();
            TbDescription.Inlines.Add(new Run(tag?.Item1 ?? "?") {FontWeight = FontWeights.Bold});
            TbDescription.Inlines.Add(new Run($"{Environment.NewLine}{tag?.Item2 ?? "No description."}"));
        }

        private void EditFlags(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var obj = DataContext;
            var propName = btn.GetBindingExpression(DataContextProperty).ParentBinding.Path.Path;
            var propInfo = DataContext.GetType().GetProperty(propName);
            
            new FlagsEditorDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner, 
                Owner = Window.GetWindow(this)
            }.EditEnum(propInfo, obj);
        }
        
        private void EditDictionary(object dict)
        {
            var dic = (IDictionary)dict;
            new DictionaryEditorDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner, 
                Owner = Window.GetWindow(this)
            }.Edit(dic);
        }

        private void EditPrimitiveCollection(object collection, string title = "Collection Editor")
        {
            var c = (ICollection)collection;
            new CollectionEditor
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner, 
                Owner = Window.GetWindow(this)
            }.Edit(c, title);
        }

        private void EditObjectCollection(object collection, string title = "Collection Editor")
        {
            var c = (ICollection)collection;
            new ObjectCollectionEditor
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner, 
                Owner = Window.GetWindow(this)
            }.Edit(c, title);
        }

        private void EditObject(object o, string title = "Edit Object")
        {
            new ObjectEditor
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner, 
                Owner = Window.GetWindow(this)
            }.Edit(o, title);
        }

        private void UpdateFilter(object sender, TextChangedEventArgs e)
        {
            var filterText = ((TextBox)sender).Text;
            var grid = (Grid)ScrollViewer.Content;
            if (grid == null)
                return;
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
