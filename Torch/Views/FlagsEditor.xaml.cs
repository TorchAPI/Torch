using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace Torch.Views
{
    /// <summary>
    /// Interaction logic for FlagsEditorDialog.xaml
    /// </summary>
    public partial class FlagsEditorDialog : Window
    {
        public FlagsEditorDialog()
        {
            InitializeComponent();
        }

        private List<Flag> _flags;
        private PropertyInfo _property;
        private object _obj;
        
        public void EditEnum(PropertyInfo prop, object obj)
        {
            if (!prop.PropertyType.IsEnum || prop.PropertyType.GetCustomAttribute<FlagsAttribute>() == null)
                throw new ArgumentException("Type is not a flags enum");

            _property = prop;
            _obj = obj;
            _flags = new List<Flag>();
            var initial = (int)Convert.ChangeType(prop.GetValue(obj), typeof(int));
            foreach (var value in Enum.GetValues(prop.PropertyType))
            {
                var val = (int)Convert.ChangeType(value, typeof(int));
                _flags.Add(new Flag
                {
                    Name = Enum.GetName(prop.PropertyType, value),
                    Value = val,
                    IsChecked = (initial & val) > 0
                });
            }

            Items.ItemsSource = _flags;
            ShowDialog();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Ok_OnClick(object sender, RoutedEventArgs e)
        {
            var final = 0;
            foreach (var item in _flags)
            {
                if (item.IsChecked)
                    final |= item.Value;
            }
            
            _property.SetValue(_obj, Enum.ToObject(_property.PropertyType, final));
            Close();
        }
        
        private class Flag
        {
            public bool IsChecked { get; set; }
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}
