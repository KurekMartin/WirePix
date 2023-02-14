using PhotoApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;

namespace PhotoApp.Dialogs
{
    public partial class FileTypeSelectionDialog : UserControl
    {
        public static readonly DependencyProperty FileTypeSelectionProperty =
            DependencyProperty.Register(
                "FileTypeSelection", typeof(FileTypeSelection), typeof(FileTypeSelectionDialog),
                new FrameworkPropertyMetadata(new FileTypeSelection(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public FileTypeSelection FileTypeSelection
        {
            get { return (FileTypeSelection)GetValue(FileTypeSelectionProperty); }
            set { SetValue(FileTypeSelectionProperty, value); }
        }
        public FileTypeSelectionDialog()
        {
            InitializeComponent();
            SetInitialState();
        }

        private void SetInitialState()
        {
            List<string> types = FileTypeSelection.FileTypes.ToList();
            for (int i = 0; i < icItemsGrid.Items.Count; i++)
            {
                //get text
                //if text in types -> select
                ContentPresenter item = icItemsGrid.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (item != null)
                {
                    item.ApplyTemplate();
                    TextBlock tb = item.ContentTemplate.FindName("tbItemText", item) as TextBlock;
                    if (tb != null && types.Contains(tb.Text))
                    {
                        CheckBox cb = item.ContentTemplate.FindName("cbSelect", item) as CheckBox;
                        cb.IsChecked = true;
                    }
                }
            }
        }

        private void btnSelectAll_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetAllItemsChecked(true);
        }

        private void btnDeselectAll_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetAllItemsChecked(false);
        }

        private void SetAllItemsChecked(bool value)
        {
            for (int i = 0; i < icItemsGrid.Items.Count; i++)
            {
                ContentPresenter item = icItemsGrid.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                item.ApplyTemplate();
                CheckBox cb = item.ContentTemplate.FindName("cbSelect", item) as CheckBox;
                cb.IsChecked = value;
            }
        }

        private void cbSelect_Unchecked(object sender, RoutedEventArgs e)
        {
            string text = GetItemText(sender as CheckBox);
            FileTypeSelection.FileTypes.Remove(text);
        }

        private void cbSelect_Checked(object sender, RoutedEventArgs e)
        {
            string text = GetItemText(sender as CheckBox);
            if (!FileTypeSelection.FileTypes.Contains(text))
            {
                int index = FileTypeSelection.FileTypes.ToList().FindLastIndex(f => string.Compare(f, text) < 0);
                FileTypeSelection.FileTypes.Insert(index + 1, text);
            }
        }

        private string GetItemText(CheckBox item)
        {
            StackPanel sp = VisualTreeHelper.GetParent(item) as StackPanel;
            TextBlock tb = sp.FindName("tbItemText") as TextBlock;
            return tb.Text;
        }

        private void tbItemText_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;
            StackPanel sp = VisualTreeHelper.GetParent(textBlock) as StackPanel;
            CheckBox cb = sp.FindName("cbSelect") as CheckBox;
            cb.IsChecked = !cb.IsChecked;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetInitialState();
        }
    }
}
