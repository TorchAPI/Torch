using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Torch.Views
{
    /// <summary>
    /// Interaction logic for CollectionEditor.xaml
    /// </summary>
    public partial class CollectionEditor : Window
    {
        private static readonly Dictionary<Type, MethodInfo> MethodCache = new Dictionary<Type, MethodInfo>();
        private static readonly MethodInfo EditMethod;

        public CollectionEditor()
        {
            InitializeComponent();
        }

        static CollectionEditor()
        {
            var m = typeof(CollectionEditor).GetMethods();
            EditMethod = m.First(mt => mt.Name == "Edit" && mt.GetGenericArguments().Length == 1);
        }

        public void Edit(ICollection collection, string name)
        {
            var gt = collection.GetType().GenericTypeArguments[0];
            MethodInfo gm;
            if (!MethodCache.TryGetValue(gt, out gm))
            {
                gm = EditMethod.MakeGenericMethod(gt);
                MethodCache.Add(gt, gm);
            }

            gm.Invoke(this, new object[] {collection, name});
        }

        public void Edit<T>(ICollection<T> collection, string name)
        {
            ItemList.Text = string.Join("\r\n", collection.Select(x => x.ToString()));
            Title = name;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowDialog();

            var parsed = new List<T>();
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                foreach (var item in ItemList.Text.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries))
                {
                    parsed.Add((T)converter.ConvertFromString(item));
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error parsing list, check your input.", "Edit Error");
                return;
            }

            collection.Clear();
            foreach (var item in parsed)
                collection.Add(item);

        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
