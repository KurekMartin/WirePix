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
        private List<ButtonGroupStruct> _buttons = new List<ButtonGroupStruct>();
        private Point _buttonGridSize = new Point();
        int _selectedIndex = -1;
        public ObservableCollection<string> FileStructure
        {
            get { return _fileStructure; }
            set { _fileStructure = value; OnPropertyChanged(); }
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { _selectedIndex = value; OnPropertyChanged(); }
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public FileStructDialog(MainWindow window, List<ButtonGroupStruct> buttons, string initStructure = "")
        {
            DataContext = this;
            InitializeComponent();
            mainWindow = window;

            ButtonGridSize = new Point(buttons.Max(x => x.gridPosition.X)+1,
                                       buttons.Max(x => x.gridPosition.Y)+1);
            Buttons = buttons;

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
                    error.Text = $"Nelze použít {Tags.GetTagByVisibleText(insertValue).VisibleText} na začátku názvu.";
                    return false;
                }

                TagStruct lastTag = Tags.GetTag(visibleText: tags.Last());
                if (lastTag == Tags.GetTag(code: Properties.Resources.Hyphen) ||
                    lastTag == Tags.GetTag(code: Properties.Resources.Underscore))
                {
                    error.Text = $"Nelze použít {insertValue} po {lastTag.VisibleText}.";
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
            cbGroupSelect.Visibility = tbCustomText.Visibility = Visibility.Collapsed;

            if (FileStructure.Count() > 0)
            {
                tbFileExt.Visibility = Visibility.Visible;

                if (SelectedIndex >= 0)
                {
                    btnDeleteTag.Visibility = Visibility.Visible;

                    string tagText = FileStructure[SelectedIndex];
                    TagStruct tag = Tags.GetTag(visibleText: tagText);
                    List<TagStruct> tagGroup = Tags.GetTagGroup(tag.Group);

                    if (tagGroup.Count > 0)
                    {
                        cbGroupSelect.ItemsSource = tagGroup;
                        cbGroupSelect.SelectedIndex = tagGroup.IndexOf(tag);
                        cbGroupSelect.Visibility = Visibility.Visible;
                    }
                    else if (tag.Code == Properties.Resources.CustomText)
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
            if (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0 && Tags.GetTag(visibleText: e.RemovedItems[0].ToString()).Code == Properties.Resources.CustomText) //kontrola parametru CustomText
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
                tbControlsError.Text = $"Chybí hodnota pro {Tags.GetTagByCode(Properties.Resources.CustomText).VisibleText}.\n" +
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
            if (e.RemovedItems.Count > 0 && ((ComboBox)sender).SelectedIndex > -1)
            {
                TagStruct tag = (TagStruct)((ComboBox)sender).SelectedItem;
                int oldIndex = SelectedIndex;
                FileStructure[SelectedIndex] = tag.VisibleText;
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
                if (oldIndex >= FileStructure.Count)
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
