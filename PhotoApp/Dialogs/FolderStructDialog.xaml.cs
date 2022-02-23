﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PhotoApp.Dialogs
{
    public partial class FolderStructDialog : UserControl, INotifyPropertyChanged
    {
        private MainWindow mainWindow;
        private ObservableCollection<ObservableCollection<string>> _folderStructure = new ObservableCollection<ObservableCollection<string>>(); //struktura pro uložení fotek
        private List<ButtonGroupStruct> _buttons = new List<ButtonGroupStruct>();
        private Point _buttonGridSize = new Point();
        private int _selectedFolderIndex = -1; //index vybrané složky
        private int _selectedTagIndex = -1; //index vybraneho tagu

        private static int _folderLimit = 7;

        public ObservableCollection<ObservableCollection<string>> FolderStructure
        {
            get { return _folderStructure; }
            set { _folderStructure = value; OnPropertyChanged(); }
        }

        public int SelectedFolderIndex
        {
            get { return _selectedFolderIndex; }
            set
            {
                _selectedFolderIndex = value;
                OnPropertyChanged();
            }
        }

        public int SelectedTagIndex
        {
            get { return _selectedTagIndex; }
            set { _selectedTagIndex = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> SelectedFolder
        {
            get
            {
                if (SelectedFolderIndex > -1 && SelectedFolderIndex < FolderStructure.Count)
                {
                    return FolderStructure[SelectedFolderIndex];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                FolderStructure[SelectedFolderIndex] = value;
                OnPropertyChanged();
            }
        }
        public List<ButtonGroupStruct> Buttons
        {
            get { return _buttons; }
            set { _buttons = value; OnPropertyChanged(); }
        }

        public Point ButtonGridSize
        {
            get { return _buttonGridSize; }
            set { _buttonGridSize = value; OnPropertyChanged(); }
        }

        private void SelectedFolder_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("SelectedFolder");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public FolderStructDialog(MainWindow window, List<ButtonGroupStruct> buttons, List<List<string>> initStructure = null)
        {
            DataContext = this;
            InitializeComponent();
            mainWindow = window;

            ButtonGridSize = new Point(buttons.Max(x => x.gridPosition.X) + 1,
                                       buttons.Max(x => x.gridPosition.Y) + 1);
            Buttons = buttons;

            initStructure.ForEach(x => FolderStructure.Add(new ObservableCollection<string>(x)));
            if (FolderStructure.Count == 0)
            {
                //vytvoreni korene treeView
                NewFolderLevel();
            }
            ShowControls();
        }


        //přidání tagu po kliknutí na tlačítko
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tbError.Text = ""; //reset chybové hlášky
            string insertValue = ((Button)sender).Tag.ToString();

            if (TagAdd(insertValue, FolderStructure[SelectedFolderIndex].ToList(), tbError))
            {
                FolderStructure[SelectedFolderIndex].Add(insertValue);
                SelectedTagIndex++;
            }

            if (FolderStructure[SelectedFolderIndex].Count() > 0)
            {
                ShowControls();
            }

        }

        private bool TagAdd(string insertValue, List<string> tags, TextBlock error)
        {
            if (insertValue.StartsWith("{"))
            {
                //omezeni maximalniho poctu tagu
                if (tags.Count(x => x.StartsWith("{")) > 6)
                {
                    error.Text = "Lze použít maximálně 6 značek";
                    return false;
                }
            }
            else
            {
                if (tags.Count() == 0) //tagy, které neobsahují {} (např. - _)
                {
                    error.Text = $"Nelze použít {Tags.GetTag(visibleText: insertValue).VisibleText} na začátku názvu.";
                    return false;
                }
                else if (insertValue == Tags.GetTag(code: Properties.TagCodes.NewFolder).VisibleText)
                {
                    if (FolderStructure.Count() >= _folderLimit)
                    {
                        error.Text = $"Můžete použít maximálně {_folderLimit} složek";
                        return false;
                    }
                    else if (!tbCustomText.IsVisible || (tbCustomText.IsVisible && Tags.IsValidCustomText(tbCustomText.Text)))
                    {
                        NewFolderLevel();
                        return false;
                    }
                    else
                    {
                        ShowCustomTextError();
                        return false;
                    }

                }

                TagStruct lastTag = Tags.GetTag(visibleText: tags.Last());
                if (lastTag == Tags.GetTag(code: Properties.TagCodes.Hyphen) ||
                    lastTag == Tags.GetTag(code: Properties.TagCodes.Underscore))
                {
                    error.Text = $"Nelze použít {insertValue} po {lastTag.VisibleText}.";
                    return false;
                }
            }
            return true;
        }

        //vytvoření nové složky
        private void NewFolderLevel()
        {
            FolderStructure.Add(new ObservableCollection<string>());
            SelectedFolderIndex++;
            SelectedTagIndex = -1;
            btnDeleteFolder.Visibility = Visibility.Collapsed;
        }


        //kontrola textBoxu - povolené jen písmena - _
        private void tbCustomText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //Regex regex = new Regex(@"^[a-zA-Z0-9\-_]*$");
            //e.Handled = !regex.IsMatch(e.Text);

            e.Handled = e.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
        }


        //změna vybraneho tagu
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0 && ((ComboBox)sender).SelectedIndex > -1)
            {
                TagStruct tag = (TagStruct)((ComboBox)sender).SelectedItem;
                int oldIndex = SelectedTagIndex;
                FolderStructure[SelectedFolderIndex][SelectedTagIndex] = tag.VisibleText;
                SelectedTagIndex = oldIndex;
            }
        }

        //vyber tagu v nazvu souboru
        private void TagSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbControlsError.Visibility = Visibility.Hidden;
            if (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0 && Tags.GetTag(visibleText: e.RemovedItems[0].ToString()).Code == Properties.TagCodes.CustomText) //kontrola parametru CustomText
            {
                string tag = FolderStructure[SelectedFolderIndex].First(x => x == e.RemovedItems[0].ToString());
                string text = Tags.GetTagParameter(tag);
                if (!Tags.IsValidCustomText(text))
                {
                    SelectedTagIndex = FolderStructure[SelectedFolderIndex].IndexOf(tag);
                    ShowCustomTextError();
                }
            }
            ShowControls();
        }

        //smazani vybraneho tagu
        private void btnDeleteTag_Click(object sender, RoutedEventArgs e)
        {
            int oldIndex = SelectedTagIndex;

            if (FolderStructure[SelectedFolderIndex].Count > 0)
            {
                FolderStructure[SelectedFolderIndex].RemoveAt(SelectedTagIndex);
                if (oldIndex >= FolderStructure[SelectedFolderIndex].Count)
                {
                    SelectedTagIndex = oldIndex - 1;
                }
                else
                {
                    SelectedTagIndex = oldIndex;
                }
            }

            ShowControls();
        }

        //smazani vybrane slozky
        private void btnDeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedFolder();
        }

        private void DeleteSelectedFolder()
        {
            FolderStructure.RemoveAt(SelectedFolderIndex);
            if (FolderStructure.Count == 0)
            {
                NewFolderLevel();
            }
        }

        //změna CustomTextu -> update tagu
        private void tbCustomText_TextChanged(object sender, TextChangedEventArgs e)
        {
            int oldIndex = SelectedTagIndex;
            string tag = FolderStructure[SelectedFolderIndex][SelectedTagIndex];
            int i = tag.IndexOf("(");
            if (i != -1)
            {
                tag = tag.Substring(0, i);
            }
            tag += $"({tbCustomText.Text})";

            FolderStructure[SelectedFolderIndex][SelectedTagIndex] = tag;
            SelectedTagIndex = oldIndex;
            tbCustomText.Focus();

            if (tbCustomText.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                ShowCustomTextError();
            }
            else { tbError.Text = ""; }
        }

        private void ShowCustomTextError()
        {
            string text = tbCustomText.Text;
            if (text.Length == 0)
            {
                tbControlsError.Text = $"Chybí hodnota pro {Tags.GetTag(code: Properties.TagCodes.CustomText).VisibleText}.\n" +
                    $"Doplňte tuto hodnotu nebo tag odstraňte";
                tbControlsError.Visibility = Visibility.Visible;
            }
            else if (text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                List<char> invalidChars = text.Where(x => Path.GetInvalidFileNameChars().Contains(x)).ToList();
                tbControlsError.Text = $"Text obsahuje neplatné znaky: {string.Join("", invalidChars)}";
                tbControlsError.Visibility = Visibility.Visible;
            }
            tbCustomText.Focus();
        }

        private void ShowControls()
        {
            cbGroupSelect.Visibility = tbCustomText.Visibility = btnDeleteTag.Visibility = Visibility.Collapsed;

            if (FolderStructure.Count() > 0 && FolderStructure[SelectedFolderIndex].Count() > 0)
            {
                btnDeleteFolder.Visibility = Visibility.Visible;

                if (SelectedTagIndex >= 0)
                {
                    btnDeleteTag.Visibility = Visibility.Visible;

                    string tagText = FolderStructure[SelectedFolderIndex][SelectedTagIndex];
                    TagStruct tag = Tags.GetTag(visibleText: tagText);
                    List<TagStruct> tagGroup = Tags.GetTagGroup(tag.Group);

                    if (tagGroup.Count > 0)
                    {
                        cbGroupSelect.ItemsSource = tagGroup;
                        cbGroupSelect.SelectedIndex = tagGroup.IndexOf(tag);
                        cbGroupSelect.Visibility = Visibility.Visible;
                    }
                    else if (tag.Code == Properties.TagCodes.CustomText)
                    {
                        tbCustomText.Visibility = Visibility.Visible;
                        tbCustomText.Text = Tags.GetTagParameter(tagText);
                        tbCustomText.Focus();
                    }
                }
                else
                {
                    btnDeleteTag.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                btnDeleteFolder.Visibility = Visibility.Hidden;

            }
        }

        //ukončení formuláře
        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            if (tbCustomText.Visibility == Visibility.Visible && !Tags.IsValidCustomText(tbCustomText.Text))
            {
                tbCustomText.Focus();
                ShowCustomTextError();
                return;
            }
            List<List<string>> result = new List<List<string>>();
            FolderStructure.ToList().ForEach(x => result.Add(x.ToList()));
            mainWindow.DialogClose(this, result);
        }

        private void FolderSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnPropertyChanged("SelectedFolder");
            ShowControls();
            if (FolderStructure.Count > 0 && SelectedFolderIndex != FolderStructure.Count() - 1 && FolderStructure.Last().Count == 0)
            {
                FolderStructure.Remove(FolderStructure.Last());
            }
        }
    }
}
