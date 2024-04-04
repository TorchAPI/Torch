﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using NLog;
using Torch.Collections;
using Torch.Server.ViewModels;
using Torch.Server.ViewModels.Blocks;
using Torch.Server.ViewModels.Entities;
using Torch.Server.Views.Blocks;
using Torch.Server.Views.Entities;
using VRage.Game.ModAPI;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for EntitiesControl.xaml
    /// </summary>
    public partial class EntitiesControl : UserControl
    {
        public EntityTreeViewModel Entities { get; set; }

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public EntitiesControl()
        {
            InitializeComponent();
            Entities = new EntityTreeViewModel(this);
            DataContext = Entities;
            Entities.Init();
            SortCombo.ItemsSource = Enum.GetNames(typeof(EntityTreeViewModel.SortEnum));
            SortCombo_GridTab.ItemsSource = Enum.GetNames(typeof(EntityTreeViewModel.SortEnum));
            SortCombo_Characters.ItemsSource = Enum.GetNames(typeof(EntityTreeViewModel.SortEnum));
            SortCombo_Voxels.ItemsSource = Enum.GetNames(typeof(EntityTreeViewModel.SortEnum));
            SortCombo_FloatingObjects.ItemsSource = Enum.GetNames(typeof(EntityTreeViewModel.SortEnum));
        }
        
        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is EntityViewModel vm)
            {
                Entities.CurrentEntity = vm;
                switch (e.NewValue)
                {
                    case GridViewModel gvm:
                        EditorFrame.Content = new Entities.GridView {DataContext = gvm};
                        break;
                    case BlockViewModel bvm:
                        EditorFrame.Content = new BlockView {DataContext = bvm};
                        break;
                    case VoxelMapViewModel vvm:
                        EditorFrame.Content = new VoxelMapView {DataContext = vvm};
                        break;
                    case CharacterViewModel cvm:
                        EditorFrame.Content = new CharacterView {DataContext = cvm};
                        break;
                }
            }
            else
            {
                Entities.CurrentEntity = null;
                EditorFrame.Content = null;
            }
        }
        
        private void TreeView_Grids_OnSelectionItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is EntityViewModel vm)
            {
                Entities.CurrentEntity = vm;
                switch (e.NewValue)
                {
                    case GridViewModel gvm:
                        EditorFrame_Grids.Content = new Entities.GridView {DataContext = gvm};
                        break;
                    case BlockViewModel bvm:
                        EditorFrame_Grids.Content = new BlockView {DataContext = bvm};
                        break;
                }
            }
            else
            {
                Entities.CurrentEntity = null;
                EditorFrame_Grids.Content = null;
            }
        }

        private void TreeView_Characters_OnSelectionItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is EntityViewModel vm)
            {
                Entities.CurrentEntity = vm;
               
                if (e.NewValue is CharacterViewModel cvm)
                    EditorFrame_Characters.Content = new CharacterView { DataContext = cvm };
            }
            else
            {
                Entities.CurrentEntity = null;
                EditorFrame_Characters.Content = null;
            }
        }

        private void TreeView_VoxelMaps_OnSelectionItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is EntityViewModel vm)
            {
                Entities.CurrentEntity = vm;
                
                switch (e.NewValue)
                {
                    case VoxelMapViewModel vvm:
                        EditorFrame_VoxelMaps.Content = new VoxelMapView { DataContext = vvm };
                        break;
                }
            }
            else
            {
                Entities.CurrentEntity = null;
                EditorFrame_VoxelMaps.Content = null;
            }
        }

        private void TreeView_FloatingObjects_OnSelectionItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is EntityViewModel vm)
            {
                Entities.CurrentEntity = vm;
                
                switch (e.NewValue)
                {
                    case FloatingObjectViewModel fvm:
                        EditorFrame_FloatingObjects.Content = new Entities.GridView { DataContext = fvm };
                        break;
                }
            }
            else
            {
                Entities.CurrentEntity = null;
                EditorFrame_FloatingObjects.Content = null;
            }
        }            

        private void Delete_OnClick(object sender, RoutedEventArgs e)
        {
            if (Entities.CurrentEntity?.Entity is IMyCharacter)
                return;
            TorchBase.Instance.Invoke(() => Entities.CurrentEntity?.Delete());
        }

        private void Stop_OnClick(object sender, RoutedEventArgs e)
        {
            TorchBase.Instance.Invoke(() => Entities.CurrentEntity?.Entity.Physics?.ClearSpeed());
        }

        private void TreeViewItem_OnExpanded(object sender, RoutedEventArgs e)
        {
            //Exact item that was expanded.
            var item = (TreeViewItem)e.OriginalSource;
            if (item.DataContext is ILazyLoad l)
                l.Load();
        }

        private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sort = (EntityTreeViewModel.SortEnum)SortCombo.SelectedIndex;
            
            var comparer = new EntityViewModel.Comparer(sort);

            Task[] sortTasks = new Task[4];

            Entities.CurrentSort = sort;
            Entities.SortedCharacters.SetComparer(comparer);
            Entities.SortedFloatingObjects.SetComparer(comparer);
            Entities.SortedGrids.SetComparer(comparer);
            Entities.SortedVoxelMaps.SetComparer(comparer);

            foreach (var i in Entities.SortedCharacters)
                i.DescriptiveName = i.GetSortedName(sort);
            foreach (var i in Entities.SortedFloatingObjects)
                i.DescriptiveName = i.GetSortedName(sort);
            foreach (var i in Entities.SortedGrids)
                i.DescriptiveName = i.GetSortedName(sort);
            foreach (var i in Entities.SortedVoxelMaps)
                i.DescriptiveName = i.GetSortedName(sort);
        }

        private void DeleteFloating_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var i in Entities.SortedFloatingObjects)
                TorchBase.Instance.Invoke(() => i?.Delete());
        }
    }
}
