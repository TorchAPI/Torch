using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Torch.Views
{
    /// <summary>
    /// Interaction logic for EmbeddedCollectionEditor.xaml
    /// </summary>
    public partial class EmbeddedCollectionEditor : UserControl
    {
        public EmbeddedCollectionEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var c = dependencyPropertyChangedEventArgs.NewValue as ICollection;
            //var c = DataContext as ICollection;
            if (c != null)
                Edit(c);
        }

        private static readonly Dictionary<Type, MethodInfo> MethodCache = new Dictionary<Type, MethodInfo>();
        private static readonly MethodInfo EditMethod;


        static EmbeddedCollectionEditor()
        {
            var m = typeof(EmbeddedCollectionEditor).GetMethods();
            EditMethod = m.First(mt => mt.Name == "Edit" && mt.GetGenericArguments().Length == 1);
        }

        public void Edit(ICollection collection)
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

            gm.Invoke(this, new object[] {collection});
        }

        public void Edit<T>(ICollection<T> collection) where T : new()
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
            oc.CollectionChanged += (sender, args) => RefreshList();

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

        private void RefreshList()
        {
            ElementList.Items.Refresh();
        }
    }
}

