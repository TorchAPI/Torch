using System;
using System.Collections;
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

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for PropertyGrid.xaml
    /// </summary>
    public partial class PropertyGrid : UserControl
    {
        public PropertyGrid()
        {
            InitializeComponent();
        }

        public void SetObject(object obj)
        {
            var props = obj.GetType().GetProperties();
            foreach (var prop in props)
            {
                var p = prop.GetValue(obj);
                Grid.Items.Add(new PropertyView(p, prop.Name));
            }
        }
    }

    public class PropertyView : ViewModel
    {
        private object _obj;

        public string Name { get; }
        public string Value { get { return _obj.ToString(); } }
        public DataTemplate ValueEditTemplate;

        public PropertyView(object obj, string name)
        {
            Name = name;
            _obj = obj;

            ValueEditTemplate = new DataTemplate();
        }
    }

    /*
    public class PropertyGridDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is IEnumerable)
            {
                
            }
        }
    }*/
}
