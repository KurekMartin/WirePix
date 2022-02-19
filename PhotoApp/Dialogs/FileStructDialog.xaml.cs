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
            else
            {
                btnDeleteTag.Visibility = Visibility.Hidden;
            }
            if (FileStructure.Count > 0)
            {
                tbFileExt.Visibility = Visibility.Visible;
            }
            else
            {
                tbFileExt.Visibility = Visibility.Hidden;
            }
        }


        //vyber tagu v nazvu souboru
        private void TagClick(object sender, SelectionChangedEventArgs e)
        {
            ShowControls();
        }

        //kontrola textBoxu - povolené jen písmena - _
        private void tbCustomText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[a-zA-Z0-9\-_]*$");
            e.Handled = !regex.IsMatch(e.Text);
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
                SelectedIndex = oldIndex-1;
            }
            ShowControls();
        }


        //změna CustomTextu -> update tagu
        private void tbCustomText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (e.Changes.First().AddedLength == 1 || e.Changes.First().RemovedLength == 1)
            {
                int i = FileStructure[SelectedIndex].IndexOf("(");
                if (i != -1)
                {
                    FileStructure[SelectedIndex] = FileStructure[SelectedIndex].Substring(0, i);
                }
                FileStructure[SelectedIndex] += $"({tbCustomText.Text})";
            }

        }

        //zákaz vkládání textu do textBoxu
        private void tbCustomText_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        //ukončení formuláře
        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            //int i = BaseStructDialog.CheckCustomText(FileStructure.ToList());
            //if (i > -1)
            //{
            //    selectedTag = BaseStructDialog.SelectTag(spNameStruct, selectedTag, i);
            //    TextBlock tb = BaseStructDialog.FindTextBlockByIndex<TextBlock>(spNameStruct, selectedTag);
            //    BaseStructDialog.ShowControls(spControls, tb.Text);

            //    tbError.Text = $"Chybí hodnota pro {Tags.GetTagByCode(Properties.Resources.CustomText).visibleText}.\n" +
            //        $"Doplňte tuto hodnotu nebo tag odstraňte";
            //    tbCustomText.Focus();
            //    return;
            //}
            string result = string.Join("", FileStructure.ToList());
            mainWindow.DialogClose(this, result);
        }
    }
}
