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
        private static readonly Dictionary<Type, MethodInfo> MethodCache = new Dictionary<Type, MethodInfo>();
        private static readonly MethodInfo EditMethod;

        public ObjectCollectionEditor()
        {
            InitializeComponent();
        }

        static ObjectCollectionEditor()
        {
            var m = typeof(ObjectCollectionEditor).GetMethods();
            EditMethod = m.First(mt => mt.Name == "Edit" && mt.GetGenericArguments().Length == 1);
        }

        public void Edit(ICollection collection, string title)
        {
            if (collection == null)
            {
                MessageBox.Show("Cannot load null collection.", "Edit Error");
                return;
            }

            var gt = collection.GetType().GenericTypeArguments[0];

            //substitute for 'where T : new()'
            if (gt.GetConstructor(Type.EmptyTypes) == null)
            {
                MessageBox.Show("Unsupported collection type. Type must have paramaterless ctor.", "Edit Error");
                return;
            }

            if (!MethodCache.TryGetValue(gt, out MethodInfo gm))
            {
                gm = EditMethod.MakeGenericMethod(gt);
                MethodCache.Add(gt, gm);
            }

            gm.Invoke(this, new object[] {collection, title});
        }

        public void Edit<T>(ICollection<T> collection, string title) where T : new()
        {
            var oc = collection as ObservableCollection<T> ?? new ObservableCollection<T>(collection);

            AddButton.Click += (sender, args) =>
                               {
                                   var t = new T();
                                   oc.Add(t);
                                   ElementList.SelectedItem = t;
                               };

            RemoveButton.Click += RemoveButton_OnClick<T>;
            ElementList.SelectionChanged += ElementsList_OnSelected;

            ElementList.ItemsSource = oc;

            Title = title;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowDialog();

            if (!(collection is ObservableCollection<T>))
            {
                collection.Clear();
                foreach (var o in oc)
                    collection.Add(o);
            }
        }

        private void RemoveButton_OnClick<T>(object sender, RoutedEventArgs e)
        {
            //this is kinda shitty, but item count is normally small, and it prevents CollectionModifiedExceptions
            var l = (ObservableCollection<T>)ElementList.ItemsSource;
            var r = new List<T>(ElementList.SelectedItems.Cast<T>());
            foreach (var item in r)
                l.Remove(item);
            if (l.Any())
                ElementList.SelectedIndex = 0;
        }

        private void ElementsList_OnSelected(object sender, RoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem;
            PGrid.DataContext = item;
        }
    }
}
