using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Torch.Server.ViewModels.Blocks;
using VRage.Utils;

namespace Torch.Server.Views.Blocks
{
    /// <summary>
    /// Interaction logic for PropertyView.xaml
    /// </summary>
    public partial class PropertyView : UserControl
    {
        public PropertyView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            switch (args.NewValue)
            {
                case PropertyViewModel<bool> vmBool:
                    InitBool();
                    break;
                case PropertyViewModel<StringBuilder> vmSb:
                    InitStringBuilder();
                    break;
                default:
                    InitDefault();
                    break;
            }
        }

        private void InitStringBuilder()
        {
            var textBox = new TextBox { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch };
            var binding = new Binding("Value") { Source = DataContext, Converter = new StringBuilderConverter()};
            textBox.SetBinding(TextBox.TextProperty, binding);
            Frame.Content = textBox;

        }

        private void InitBool()
        {
            var checkBox = new CheckBox {VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left};
            var binding = new Binding("Value") { Source = DataContext };
            checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
            Frame.Content = checkBox;
        }

        private void InitDefault()
        {
            var textBox = new TextBox { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch};
            var binding = new Binding("Value") { Source = DataContext };
            textBox.SetBinding(TextBox.TextProperty, binding);
            Frame.Content = textBox;
        }
    }

    public class StringBuilderConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((StringBuilder)value).ToString();
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new StringBuilder((string)value);
        }
    }

    public class StringIdConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return MyStringId.GetOrCompute((string)value);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }
}
