using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Torch.Server
{
    public static class ListBoxExtensions
    {
        //https://stackoverflow.com/questions/28689125/how-to-autoscroll-listbox-to-bottom-wpf-c
        public static void ScrollToItem(this ListBox listBox, int index)
        {
            // Find a container
            UIElement container = null;
            for (int i = index; i > 0; i--)
            {
                container = listBox.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;
                if (container != null)
                {
                    break;
                }
            }
            if (container == null)
                return;

            // Find the ScrollContentPresenter
            ScrollContentPresenter presenter = null;
            for (Visual vis = container; vis != null && vis != listBox; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if ((presenter = vis as ScrollContentPresenter) != null)
                    break;
            if (presenter == null)
                return;

            // Find the IScrollInfo
            var scrollInfo =
                !presenter.CanContentScroll ? presenter :
                presenter.Content as IScrollInfo ??
                FirstVisualChild(presenter.Content as ItemsPresenter) as IScrollInfo ??
                presenter;

            // Find the amount of items that is "Visible" in the ListBox
            var height = (container as ListBoxItem).ActualHeight;
            var lbHeight = listBox.ActualHeight;
            var showCount = (int)Math.Floor(lbHeight / height) - 1;

            //Set the scrollbar
            if (scrollInfo.CanVerticallyScroll)
                scrollInfo.SetVerticalOffset(index - showCount);
        }

        private static DependencyObject FirstVisualChild(Visual visual)
        {
            if (visual == null) return null;
            if (VisualTreeHelper.GetChildrenCount(visual) == 0) return null;
            return VisualTreeHelper.GetChild(visual, 0);
        }
    }
}
