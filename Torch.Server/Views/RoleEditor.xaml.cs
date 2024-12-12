using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Torch.Views;
using VRage.Game.ModAPI;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for RoleEditor.xaml
    /// </summary>
    public partial class RoleEditor : Window
    {
        public RoleEditor()
        {
            InitializeComponent();
            DataContext = Items;
        }

        public ObservableCollection<IDictionaryItem> Items { get; } = new ObservableCollection<IDictionaryItem>();
        private Type _itemType;

        private Action _commitChanges;
        public MyPromoteLevel BulkPromote { get; set; } = MyPromoteLevel.Scripter;

        public void Edit(IDictionary dict)
        {
            Items.Clear();
            var dictType = dict.GetType();
            _itemType = typeof(DictionaryItem<,>).MakeGenericType(dictType.GenericTypeArguments[0], dictType.GenericTypeArguments[1]);

            foreach (var key in dict.Keys)
            {
                Items.Add((IDictionaryItem)Activator.CreateInstance(_itemType, key, dict[key]));
            }

            ItemGrid.ItemsSource = Items;

            _commitChanges = () =>
            {
                dict.Clear();
                foreach (var item in Items)
                {
                    dict[item.Key] = item.Value;
                }
            };

            Show();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Ok_OnClick(object sender, RoutedEventArgs e)
        {
            _commitChanges?.Invoke();
            Close();
        }

        public interface IDictionaryItem
        {
            object Key { get; set; }
            object Value { get; set; }
        }

        public class DictionaryItem<TKey, TValue> : ViewModel, IDictionaryItem
        {
            private TKey _key;
            private TValue _value;

            object IDictionaryItem.Key { get => _key; set => SetValue(ref _key, (TKey)value); }
            object IDictionaryItem.Value { get => _value; set => SetValue(ref _value, (TValue)value); }

            public TKey Key { get => _key; set => SetValue(ref _key, value); }
            public TValue Value { get => _value; set => SetValue(ref _value, value); }

            public DictionaryItem()
            {
                _key = default(TKey);
                _value = default(TValue);
            }

            public DictionaryItem(TKey key, TValue value)
            {
                _key = key;
                _value = value;
            }
        }

        private void AddNew_OnClick(object sender, RoutedEventArgs e)
        {
            Items.Add((IDictionaryItem)Activator.CreateInstance(_itemType));
        }

        private void BulkEdit(object sender, RoutedEventArgs e)
        {
            List<ulong> l = Items.Where(i => i.Value.Equals(BulkPromote)).Select(i => (ulong)i.Key).ToList();
            var w = new CollectionEditor();
            w.Edit((ICollection<ulong>)l, "Bulk edit");
            var r = Items.Where(j => j.Value.Equals(BulkPromote) || l.Contains((ulong)j.Key)).ToList();
            foreach (var k in r)
                Items.Remove(k);
            foreach (var m in l)
                Items.Add(new DictionaryItem<ulong, MyPromoteLevel>(m, BulkPromote));
        }
    }
}
