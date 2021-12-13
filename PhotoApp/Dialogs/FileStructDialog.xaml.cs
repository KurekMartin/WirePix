using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PhotoApp.Dialogs
{
    public partial class FileStructDialog : UserControl
    {
        private MainWindow mainWindow;
        private List<string> fileStructure = new List<string>(); //struktura souboru

        private int selectedTag = 0; //index vybraneho tagu

        public FileStructDialog(MainWindow window, List<ButtonGroupStruct> groupList, string initStructure = "")
        {
            InitializeComponent();
            mainWindow = window;

            Grid groupGrid = BaseStructDialog.CreateMainGrid(groupList, Button_Click);

            grdMain.Children.Add(groupGrid);

            BaseStructDialog.FillComboBox(cbYear, Properties.Resources.Year);
            BaseStructDialog.FillComboBox(cbMonth, Properties.Resources.Month);
            BaseStructDialog.FillComboBox(cbDay, Properties.Resources.Day);

            fileStructure = Tags.TagsToList(initStructure);

            BaseStructDialog.TagsToStackPanel(spNameStruct, fileStructure, TagClick);

            BaseStructDialog.SelectTag(spNameStruct, selectedTag, selectedTag);
            TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spNameStruct, selectedTag);
            BaseStructDialog.ShowControls(spControls, tb.Text);
        }

        //přidání tagu po kliknutí na tlačítko
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tbError.Text = ""; //reset chybové hlášky
            string insertValue = ((Button)sender).Tag.ToString();

            ClickReturn retVal = BaseStructDialog.AddTag(insertValue, fileStructure);
            if (retVal.error.Length > 0)
            {
                tbError.Text = retVal.error;
            }
            else
            {
                fileStructure = retVal.tags;
            }

            BaseStructDialog.TagsToStackPanel(spNameStruct, fileStructure, TagClick);
            TagsToName();

            if (fileStructure.Count() > 0)
            {
                //SelectTag(fileStructure.Count() - 1);
                selectedTag = BaseStructDialog.SelectTag(spNameStruct, selectedTag, fileStructure.Count() - 1);
                TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spNameStruct, selectedTag);
                BaseStructDialog.ShowControls(spControls, tb.Text);
            }

        }


        //vyber tagu v nazvu souboru
        private void TagClick(object sender, MouseButtonEventArgs e)
        {
            TextBlock block = sender as TextBlock;
            int index = (int)block.Tag;
            //SelectTag(index);
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

                ChangeReturn cReturn = BaseStructDialog.ComboBoxChanged(cbItem, selectedTag, fileStructure);
                fileStructure = cReturn.tagStructure;


                BaseStructDialog.TagsToStackPanel(spNameStruct, fileStructure, TagClick);
                TagsToName();

                //SelectTag(index);
                selectedTag = BaseStructDialog.SelectTag(spNameStruct, selectedTag, cReturn.index);
                TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spNameStruct, selectedTag);
                BaseStructDialog.ShowControls(spControls, tb.Text);
            }
        }

        private void TagsToName()
        {

            //dosazení hodnot za tagy
            string fileName = Tags.TagsToValues(fileStructure);
            if (fileName != "")
            {
                fileName += ".xxx";
            }
            tbFileName.Text = fileName;
        }


        //smazani vybraneho tagu
        private void btnDeleteTag_Click(object sender, RoutedEventArgs e)
        {
            if (fileStructure.Count > 0)
            {
                fileStructure.RemoveAt(selectedTag);
            }


            BaseStructDialog.TagsToStackPanel(spNameStruct, fileStructure, TagClick);
            TagsToName();

            //pokud smažeme poslední tag -> vybere se opět poslední
            if (selectedTag >= fileStructure.Count())
            {
                selectedTag = fileStructure.Count() - 1;

            }
            //selectedTag(selectedTag);
            selectedTag = BaseStructDialog.SelectTag(spNameStruct, selectedTag, selectedTag);
            TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spNameStruct, selectedTag);
            BaseStructDialog.ShowControls(spControls, tb.Text);
        }


        //změna CustomTextu -> update tagu
        private void tbCustomText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (e.Changes.First().AddedLength == 1 || e.Changes.First().RemovedLength == 1)
            {

                int textPos = selectedTag;
                int i = fileStructure[textPos].IndexOf("(");
                if (i != -1)
                {
                    fileStructure[textPos] = fileStructure[textPos].Substring(0, i);
                }
                fileStructure[textPos] += $"({tbCustomText.Text})";

                BaseStructDialog.TagsToStackPanel(spNameStruct, fileStructure, TagClick);
                TagsToName();

                //SelectTag(textPos);
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
            int i = BaseStructDialog.CheckCustomText(fileStructure);
            if (i > -1)
            {
                selectedTag = BaseStructDialog.SelectTag(spNameStruct, selectedTag, i);
                TextBlock tb = BaseStructDialog.FindTextBlockByIndex(spNameStruct, selectedTag);
                BaseStructDialog.ShowControls(spControls, tb.Text);

                tbError.Text = $"Chybí hodnota pro {Tags.GetTagByCode(Properties.Resources.CustomText).visibleText}.\n" +
                    $"Doplňte tuto hodnotu nebo tag odstraňte";
                tbCustomText.Focus();
                return;
            }
            string result = string.Join("", fileStructure.ToArray());
            mainWindow.DialogClose(this, result);
        }
    }
}
