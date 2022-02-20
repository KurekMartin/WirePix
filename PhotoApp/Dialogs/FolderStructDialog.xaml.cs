using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PhotoApp.Dialogs
{
    public partial class FolderStructDialog : UserControl, INotifyPropertyChanged
    {
        private MainWindow mainWindow;
        private ObservableCollection<ObservableCollection<string>> _folderStructure = new ObservableCollection<ObservableCollection<string>>(); //struktura pro uložení fotek
        //private List<string> selectedStructure = new List<string>(); //struktura aktuální složky

        private int _selectedFolderIndex = -1; //index vybrané složky
        private int _selectedTagIndex = -1; //index vybraneho tagu

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
                    ObservableCollection<string> result = FolderStructure[SelectedFolderIndex];
                    //result.CollectionChanged += SelectedFolder_CollectionChanged;
                    return result;
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

        private void SelectedFolder_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("SelectedFolder");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public FolderStructDialog(MainWindow window, List<ButtonGroupStruct> groupList, string initStructure = "")
        {
            DataContext = this;
            InitializeComponent();
            mainWindow = window;

            Grid groupGrid = BaseStructDialog.CreateMainGrid(groupList, Button_Click);

            grdMain.Children.Add(groupGrid);

            BaseStructDialog.FillComboBox(cbYear, Properties.Resources.Year);
            BaseStructDialog.FillComboBox(cbMonth, Properties.Resources.Month);
            BaseStructDialog.FillComboBox(cbDay, Properties.Resources.Day);

            while (initStructure.Length > 0)
            {
                initStructure = initStructure.Trim('\\');

                string folder = Regex.Match(initStructure, @"[^\\]*").ToString(); //ziskani složky bez \

                FolderStructure.Add(new ObservableCollection<string>(Tags.TagsToList(folder)));

                initStructure = initStructure.Remove(0, folder.Length);
            }
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
                    error.Text = "Lze použít maximálně 6 tagů";
                    return false;
                }
            }
            else
            {
                if (tags.Count() == 0) //tagy, které neobsahují {} (např. - _)
                {
                    error.Text = $"Nelze použít {Tags.GetTagByVisibleText(insertValue).visibleText} na začátku názvu.";
                    return false;
                }
                else if (insertValue == Tags.GetTag(code: Properties.Resources.NewFolder).visibleText)
                {
                    if (!tbCustomText.IsVisible || (tbCustomText.IsVisible && IsValidCustomText(tbCustomText.Text)))
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
                if (lastTag == Tags.GetTag(code: Properties.Resources.Hyphen) ||
                    lastTag == Tags.GetTag(code: Properties.Resources.Underscore))
                {
                    error.Text = $"Nelze použít {insertValue} po {lastTag.visibleText}.";
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
            if (e.RemovedItems.Count > 0)
            {
                ComboBoxItem cbItem = ((ComboBox)sender).SelectedItem as ComboBoxItem;
                int oldIndex = SelectedTagIndex;
                FolderStructure[SelectedFolderIndex][SelectedTagIndex] = Tags.GetTag(label: cbItem.Content.ToString()).visibleText;
                SelectedTagIndex = oldIndex;
            }
        }

        //vyber tagu v nazvu souboru
        private void TagSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbControlsError.Visibility = Visibility.Hidden;
            if (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0 && Tags.GetTag(visibleText: e.RemovedItems[0].ToString()).code == Properties.Resources.CustomText) //kontrola parametru CustomText
            {
                string tag = FolderStructure[SelectedFolderIndex].First(x => x == e.RemovedItems[0].ToString());
                string text = Tags.GetTagParameter(tag);
                if (!IsValidCustomText(text))
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

        private bool IsValidCustomText(string text)
        {
            return text != string.Empty && text.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        private void ShowCustomTextError()
        {
            string text = tbCustomText.Text;
            if (text.Length == 0)
            {
                tbControlsError.Text = $"Chybí hodnota pro {Tags.GetTagByCode(Properties.Resources.CustomText).visibleText}.\n" +
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
            cbYear.Visibility = cbMonth.Visibility = cbDay.Visibility = tbCustomText.Visibility = btnDeleteTag.Visibility = Visibility.Collapsed;

            if (FolderStructure.Count() > 0 && FolderStructure[SelectedFolderIndex].Count() > 0)
            {
                btnDeleteFolder.Visibility = Visibility.Visible;

                if (SelectedTagIndex >= 0)
                {
                    btnDeleteTag.Visibility = Visibility.Visible;

                    string tagText = FolderStructure[SelectedFolderIndex][SelectedTagIndex];
                    TagStruct tag = Tags.GetTag(visibleText: tagText);
                    if (tag.code.StartsWith(Properties.Resources.Year))
                    {
                        cbYear.Visibility = Visibility.Visible;
                    }
                    else if (tag.code.StartsWith(Properties.Resources.Month))
                    {
                        cbMonth.Visibility = Visibility.Visible;
                    }
                    else if (tag.code.StartsWith(Properties.Resources.Day))
                    {
                        cbDay.Visibility = Visibility.Visible;
                    }
                    else if (tag.code == Properties.Resources.CustomText)
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
            if (tbCustomText.Visibility == Visibility.Visible && !IsValidCustomText(tbCustomText.Text))
            {
                tbCustomText.Focus();
                ShowCustomTextError();
                return;
            }
            string result = "";
            foreach (ObservableCollection<string> dir in FolderStructure)
            {
                result = Path.Combine(result, string.Join("", dir.ToArray()));
            }

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
