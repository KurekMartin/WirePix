using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PhotoApp.Dialogs
{
    public partial class FolderStructDialog : UserControl
    {
        private MainWindow mainWindow;
        private List<List<string>> folderStructure = new List<List<string>>(); //struktura pro uložení fotek
        private List<string> selectedStructure = new List<string>(); //struktura aktuální složky

        private int selectedFolderLevel = 0; //index vybrané složky
        private int selectedTag = 0; //index vybraneho tagu

        public FolderStructDialog(MainWindow window, List<ButtonGroupStruct> groupList, string initStructure = "")
        {
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

                folderStructure.Add(Tags.TagsToList(folder));

                initStructure = initStructure.Remove(0, folder.Length);
            }
            if (folderStructure.Count > 0)
            {
                GenerateTree();
            }
            else
            {
                //vytvoreni korene treeView
                NewFolderLevel();
            }
        }


        //přidání tagu po kliknutí na tlačítko
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tbError.Text = ""; //reset chybové hlášky
            string insertValue = ((Button)sender).Tag.ToString();

            ClickReturn retVal = BaseStructDialog.AddTag(insertValue, selectedStructure);
            if (retVal.error.Length > 0)
            {
                tbError.Text = retVal.error;
            }
            else
            {
                selectedStructure = retVal.tags;
            }
            if (selectedStructure.Last() == Tags.GetTagByCode(Properties.Resources.NewFolder).visibleText)
            {
                selectedStructure.RemoveAt(selectedStructure.Count() - 1);
                if (selectedStructure.Count(x => x.StartsWith("{")) > 0)
                {
                    NewFolderLevel();
                    btnDeleteFolder.Visibility = Visibility.Hidden;
                }
            }

            BaseStructDialog.TagsToStackPanel(spNameStruct, selectedStructure, TagClick);

            UpdateTreeLayer();
            if (selectedStructure.Count() > 0)
            {
                int index = selectedStructure.Count() - 1;

                selectedTag = BaseStructDialog.SelectTag(spNameStruct, selectedTag, index);
                TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spNameStruct, index);
                BaseStructDialog.ShowControls(spControls, tb.Text);
            }

            //pokud obsahuje alespoň jeden tag -> možnost smazání složky
            if (selectedStructure.Count() > 0)
            {
                btnDeleteFolder.Visibility = Visibility.Visible;
            }

        }

        //vytvoření nové složky
        private void NewFolderLevel()
        {
            spNameStruct.Children.Clear();

            TextBlock newItem = new TextBlock();
            spFolderStructure.Children.Add(newItem);
            selectedStructure = new List<string>();
            folderStructure.Add(selectedStructure);

            int index = folderStructure.Count() - 1;

            newItem.Tag = index;
            ChangeSelectedFolder(index);

            newItem.MouseLeftButtonDown += FolderName_MouseLeftButtonDown;
            btnDeleteFolder.Visibility = Visibility.Collapsed;

            UpdateTreeLayer();
        }

        //výběr složky
        private void FolderName_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock selectedItem = (TextBlock)sender;
            ChangeSelectedFolder((int)selectedItem.Tag);
        }


        //zvýraznění vybrané úrovně
        private void ChangeSelectedFolder(int newIndex)
        {
            TextBlock oldSelect = BaseStructDialog.FindTextBlockByIndex(spFolderStructure, selectedFolderLevel);
            TextBlock newSelect = BaseStructDialog.FindTextBlockByIndex(spFolderStructure, newIndex);
            oldSelect.Background = Brushes.Transparent;

            //nová složka neobsahuje žádný tag a vybereme jinou složku -> odstranění prázdné složky
            if (selectedStructure.Count() == 0 && newIndex != folderStructure.Count() - 1)
            {
                spFolderStructure.Children.Remove(oldSelect);
                folderStructure.RemoveAt(selectedFolderLevel);
            }
            selectedFolderLevel = newIndex;

            spNameStruct.Children.Clear();
            //zobrazení tagů vybrané složky
            if (newIndex < folderStructure.Count())
            {
                selectedStructure = folderStructure.ElementAt(newIndex);
                BaseStructDialog.TagsToStackPanel(spNameStruct, selectedStructure, TagClick);

                selectedTag = BaseStructDialog.SelectTag(spNameStruct, 0, 0);
                TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spNameStruct, selectedTag);
                BaseStructDialog.ShowControls(spControls, tb.Text);
                btnDeleteFolder.Visibility = Visibility.Visible;
            }
            newSelect.Background = Brushes.Aqua;
        }

        //aktualizace vybrané složky
        private void UpdateTreeLayer()
        {
            TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spFolderStructure, selectedFolderLevel);
            tb.Text = FolderLevel(selectedStructure,selectedFolderLevel);
        }


        //vyber tagu v nazvu aktualni slozky
        private void TagClick(object sender, MouseButtonEventArgs e)
        {
            TextBlock block = sender as TextBlock;
            int index = (int)block.Tag;

            selectedTag = BaseStructDialog.SelectTag(spNameStruct, selectedTag, index);
            BaseStructDialog.ShowControls(spControls, block.Text);
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

                ChangeReturn cReturn = BaseStructDialog.ComboBoxChanged(cbItem, selectedTag, selectedStructure);
                selectedStructure = cReturn.tagStructure;

                BaseStructDialog.TagsToStackPanel(spNameStruct, selectedStructure, TagClick);
                UpdateTreeLayer();

                //SelectTag(index);
                selectedTag = BaseStructDialog.SelectTag(spNameStruct, selectedTag, cReturn.index);
                TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spNameStruct, selectedTag);
                BaseStructDialog.ShowControls(spControls, tb.Text);
            }
        }



        //smazani vybraneho tagu
        private void btnDeleteTag_Click(object sender, RoutedEventArgs e)
        {
            selectedStructure.RemoveAt(selectedTag);
            if (selectedStructure.Count() == 0)
            {
                DeleteSelectedFolder();
            }
            BaseStructDialog.TagsToStackPanel(spNameStruct, selectedStructure, TagClick);
            UpdateTreeLayer();

            //pokud smažeme poslední tag -> vybere se opět poslední
            if (selectedTag >= selectedStructure.Count())
            {
                selectedTag = selectedStructure.Count() - 1;

            }
            //SelectTag(selectedTag);
            selectedTag = BaseStructDialog.SelectTag(spNameStruct, selectedTag, selectedTag);
            TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spNameStruct, selectedTag);
            BaseStructDialog.ShowControls(spControls, tb.Text);
        }

        //smazani vybrane slozky
        private void btnDeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedFolder();
        }

        private void DeleteSelectedFolder()
        {
            folderStructure.RemoveAt(selectedFolderLevel);
            TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spFolderStructure, selectedFolderLevel);
            spFolderStructure.Children.Remove(tb);
            GenerateTree();
        }

        //strom složek
        private void GenerateTree()
        {
            spFolderStructure.Children.Clear();

            int i = 0;
            foreach (List<string> dir in folderStructure)
            {
                TextBlock folderLevel = new TextBlock();
                folderLevel.Text = FolderLevel(dir, i);
                folderLevel.Tag = i;
                folderLevel.MouseLeftButtonDown += FolderName_MouseLeftButtonDown;
                spFolderStructure.Children.Add(folderLevel);
                i++;
            }

            if (spFolderStructure.Children.Count == 0)
            {
                NewFolderLevel();
            }

            ChangeSelectedFolder(spFolderStructure.Children.Count - 1);
        }

        //odsazení + dosazení hodnot za tagy
        private string FolderLevel(List<string> tags, int level)
        {
            string folderName = "";
            if (level > 0)
            {
                folderName += new string(' ', level);
                folderName += "└> ";
            }
            folderName += Tags.TagsToValues(tags);
            return folderName;
        }

        //změna CustomTextu -> update tagu
        private void tbCustomText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (e.Changes.First().AddedLength == 1 || e.Changes.First().RemovedLength == 1)
            {

                int textPos = selectedTag;
                int i = selectedStructure[textPos].IndexOf("(");
                if (i != -1)
                {
                    selectedStructure[textPos] = selectedStructure[textPos].Substring(0, i);
                }
                selectedStructure[textPos] += $"({tbCustomText.Text})";

                BaseStructDialog.TagsToStackPanel(spNameStruct, selectedStructure, TagClick);
                UpdateTreeLayer();

                selectedTag = BaseStructDialog.SelectTag(spNameStruct, selectedTag, textPos);
                TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spNameStruct, selectedTag);
                BaseStructDialog.ShowControls(spControls, tb.Text);
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
            if (!CheckCustomText())
            {
                tbError.Text = $"Chybí hodnota pro {Tags.GetTagByCode(Properties.Resources.CustomText).visibleText}.\n" +
                    $"Doplňte tuto hodnotu nebo tag odstraňte";
                tbCustomText.Focus();
                return;
            }
            string result = "";
            foreach (List<string> dir in folderStructure)
            {
                result = Path.Combine(result, string.Join("", dir.ToArray()));
            }

            mainWindow.DialogClose(this, result);
        }

        //kontrola, zda všechny tagy CustomText mají vyplněnou hodnotu
        private bool CheckCustomText()
        {
            int i = 0;
            foreach (List<string> dir in folderStructure)
            {
                int index = BaseStructDialog.CheckCustomText(dir);
                if (index > -1)
                {
                    ChangeSelectedFolder(i);

                    selectedTag = BaseStructDialog.SelectTag(spNameStruct, selectedTag, index);
                    TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spNameStruct, selectedTag);
                    BaseStructDialog.ShowControls(spControls, tb.Text);

                    return false;
                }
                i++;
            }

            return true;
        }
    }
}
