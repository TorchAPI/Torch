using System.Windows;
using System.Windows.Controls;

namespace Torch.Server.Views.Blocks
{
    /// <summary>
    /// Interaction logic for BlockView.xaml
    /// </summary>
    public partial class BlockView : UserControl
    {
        public BlockView()
        {
            InitializeComponent();

            ThemeControl.UpdateDynamicControls += UpdateResourceDict;
            UpdateResourceDict(ThemeControl.currentTheme);
        }

        public void UpdateResourceDict(ResourceDictionary dictionary)
        {
            this.Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(dictionary);
        }

        /*
        public void SetTarget(BlockViewModel model)
        {
            DataContext = model;
            Stack.Children.Clear();

            var propList = new List<ITerminalProperty>();
            model.Block.GetProperties(propList);
            foreach (var prop in propList)
            {
                Type propType = null;
                foreach (var iface in prop.GetType().GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ITerminalProperty<>))
                        propType = iface.GenericTypeArguments[0];
                }

                var modelType = typeof(PropertyViewModel<>).MakeGenericType(propType);
                var vm = Activator.CreateInstance(modelType, prop, model.Block);

                var label = new Label { Content = $"{prop.Id}: "};
                var textBox = new TextBox { Margin = new Thickness(3) };
                var binding = new Binding("Value") {Source = vm};
                textBox.SetBinding(TextBox.TextProperty, binding);
                var stack = new DockPanel {Children = {label, textBox}, LastChildFill = true};
                Stack.Children.Add(stack);
            }

            /*
            var properties = model.PropertyWrapper.GetType().GetProperties();
            foreach (var property in properties)
            {
                var control = new TextBox {Margin = new Thickness(3), Text = property.GetValue(model.PropertyWrapper).ToString()};
                var bindingPath = $"{nameof(model.PropertyWrapper)}.{property.Name}";
                var binding = new Binding {Path = new PropertyPath(bindingPath), Source = model};
                BindingOperations.SetBinding(control, TextBox.TextProperty, binding);
                Stack.Children.Add(control);
            }
        }*/
    }
}
