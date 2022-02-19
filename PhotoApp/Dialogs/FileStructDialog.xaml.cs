using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PhotoApp.Models;
using System.IO;
using System;

namespace PhotoApp.Dialogs
{
    public partial class FileStructDialog : UserControl, INotifyPropertyChanged
    {
        private MainWindow mainWindow;

        private ObservableCollection<string> _fileStructure = new ObservableCollection<string>(); //struktura souboru

        public ObservableCollection<string> FileStructure
        {
            get { return _fileStructure; }
            set { _fileStructure = value; OnPropertyChanged(); }
        }

        int _selectedIndex = -1;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { _selectedIndex = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //private int selectedTag = 0; //index vybraneho tagu

        public FileStructDialog(MainWindow window, List<ButtonGroupStruct> groupList, string initStructure = "")
        {
            DataContext = this;
            InitializeComponent();
            mainWindow = window;

            Grid groupGrid = BaseStructDialog.CreateMainGrid(groupList, Button_Click);

            grdMain.Children.Add(groupGrid);

            BaseStructDialog.FillComboBox(cbYear, Properties.Resources.Year);
            BaseStructDialog.FillComboBox(cbMonth, Properties.Resources.Month);
            BaseStructDialog.FillComboBox(cbDay, Properties.Resources.Day);



            FileStructure = new ObservableCollection<string>(Tags.TagsToList(initStructure));
            ShowControls();
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


        //přidání tagu po kliknutí na tlačítko
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tbError.Text = ""; //reset chybové hlášky
            string insertValue = ((Button)sender).Tag.ToString();

            if (TagAdd(insertValue, FileStructure.ToList(), tbError))
            {
                FileStructure.Add(insertValue);
                SelectedIndex++;
            }

            if (FileStructure.Count() > 0)
            {
                ShowControls();
            }

        }

        private void ShowControls()
        {
            cbYear.Visibility = cbMonth.Visibility = cbDay.Visibility = tbCustomText.Visibility = Visibility.Collapsed;

            if (FileStructure.Count() > 0)
            {
                tbFileExt.Visibility = Visibility.Visible;

                if (SelectedIndex >= 0)
                {
                    btnDeleteTag.Visibility = Visibility.Visible;

                    string tagText = FileStructure[SelectedIndex];
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
                    }
                }
            }
            else
            {
                tbFileExt.Visibility = Visibility.Hidden;
                btnDeleteTag.Visibility = Visibility.Hidden;
            }
            
        }


        //vyber tagu v nazvu souboru
        private void TagSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbControlsError.Visibility = Visibility.Hidden;
            if (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0 && Tags.GetTag(visibleText: e.RemovedItems[0].ToString()).code == Properties.Resources.CustomText) //kontrola parametru CustomText
            {
                string tag = FileStructure.First(x => x == e.RemovedItems[0].ToString());
                string text = Tags.GetTagParameter(tag);
                if (!IsValidCustomText(text))
                {
                    SelectedIndex = FileStructure.IndexOf(tag);
                    tbCustomText.Focus();
                    ShowCustomTextError();
                }
            }
            ShowControls();
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
                int oldIndex = SelectedIndex;
                FileStructure[SelectedIndex] = Tags.GetTag(label: cbItem.Content.ToString()).visibleText;
                SelectedIndex = oldIndex;
            }
        }

        //smazani vybraneho tagu
        private void btnDeleteTag_Click(object sender, RoutedEventArgs e)
        {
            int oldIndex = SelectedIndex;
            
            if (FileStructure.Count > 0)
            {
                FileStructure.RemoveAt(SelectedIndex);
                if(oldIndex >= FileStructure.Count)
                {
                    SelectedIndex = oldIndex - 1;
                }
                else
                {
                    SelectedIndex = oldIndex;
                }
            }
            
            ShowControls();
        }


        //změna CustomTextu -> update tagu
        private void tbCustomText_TextChanged(object sender, TextChangedEventArgs e)
        {

            int oldIndex = SelectedIndex;
            string tag = FileStructure[SelectedIndex];
            int i = tag.IndexOf("(");
            if (i != -1)
            {
                tag = tag.Substring(0, i);
            }
            tag += $"({tbCustomText.Text})";

            FileStructure[SelectedIndex] = tag;
            SelectedIndex = oldIndex;
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



        //ukončení formuláře
        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            if (tbCustomText.Visibility == Visibility.Visible && !IsValidCustomText(tbCustomText.Text))
            {
                tbCustomText.Focus();
                ShowCustomTextError();
                return;
            }
            string result = string.Join("", FileStructure.ToList());
            mainWindow.DialogClose(this, result);
        }
    }
}
