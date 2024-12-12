using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;

namespace Torch.Views
{
    /// <summary>
    /// Interaction logic for DictionaryEditorDialog.xaml
    /// </summary>
    public partial class DictionaryEditorDialog : Window
    {
        public DictionaryEditorDialog()
        {
            InitializeComponent();
            DataContext = Items;
        }

        public ObservableCollection<IDictionaryItem> Items { get; } = new ObservableCollection<IDictionaryItem>();
        private Type _itemType;

        private Action _commitChanges;

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
    }
}
