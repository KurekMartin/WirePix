using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PhotoApp.Dialogs
{
    public struct ClickReturn
    {
        public string error { get; set; }
        public List<string> tags { get; set; }
    }
    public struct ChangeReturn
    {
        public List<string> tagStructure { get; set; }
        public int index { get; set; }
    }
    static class BaseStructDialog
    {
        //vytvoreni hlavni mrizky s tlacitky
        public static Grid CreateMainGrid(List<ButtonGroupStruct> groupList, RoutedEventHandler ClickFunction)
        {
            Point gridSize = new Point();
            gridSize.X = groupList.Max(x => x.gridPosition.X) + 1;
            gridSize.Y = groupList.Max(x => x.gridPosition.Y) + 1;
            //mrizka pro skupiny tlacitek
            Grid groupGrid = NewGridSize(gridSize);
            groupGrid.Name = "grdGroupGrid";
            groupGrid.Margin = new Thickness(10);
            groupGrid.VerticalAlignment = VerticalAlignment.Top;
            groupGrid.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetRow(groupGrid, 2);

            //vytvoreni skupin tlacitek + prirazeni do mrizky
            foreach (ButtonGroupStruct buttonGroup in groupList)
            {
                GroupBox groupBox = CreateButtonGroup(buttonGroup, ClickFunction);
                Grid.SetColumn(groupBox, (int)buttonGroup.gridPosition.X);
                Grid.SetRow(groupBox, (int)buttonGroup.gridPosition.Y);

                groupGrid.Children.Add(groupBox);
            }

            return groupGrid;
        }

        //vytvoreni mrizky dane velikosti
        private static Grid NewGridSize(Point gridSize)
        {
            Grid grid = new Grid();

            //sloupce
            for (int i = 0; i < gridSize.X; i++)
            {
                ColumnDefinition colDef = new ColumnDefinition();
                colDef.Width = new GridLength(1, GridUnitType.Star);
                grid.ColumnDefinitions.Add(colDef);
            }

            //radky
            for (int i = 0; i < gridSize.Y; i++)
            {
                RowDefinition rowDef = new RowDefinition();
                rowDef.Height = new GridLength(1, GridUnitType.Star);
                grid.RowDefinitions.Add(rowDef);
            }

            return grid;
        }

        //vytvoreni skupiny tlacitek
        private static GroupBox CreateButtonGroup(ButtonGroupStruct buttonGroup, RoutedEventHandler ButtonClick)
        {
            //vytvoreni groupBoxu se jmenem
            GroupBox groupBox = new GroupBox();
            groupBox.Header = buttonGroup.groupName;
            groupBox.Margin = new Thickness(5);
            groupBox.VerticalAlignment = VerticalAlignment.Top;

            //vytvoreni mrizky na tlacitka
            StackPanel spButtons = new StackPanel();
            spButtons.Orientation = Orientation.Horizontal;
            spButtons.HorizontalAlignment = HorizontalAlignment.Center;
            groupBox.Content = spButtons;

            //vytvoreni a pridani tlacitek
            int i = 0;
            foreach (ButtonStruct btn in buttonGroup.buttons)
            {
                Button button = new Button();
                button.Content = btn.btnText;
                button.Tag = btn.insertValue;
                button.Click += ButtonClick;

                button.HorizontalAlignment = HorizontalAlignment.Center;
                button.VerticalAlignment = VerticalAlignment.Top;

                button.Height = double.NaN; //height auto

                button.Margin = new Thickness(5, 0, 5, 0);

                Grid.SetColumn(button, i);
                spButtons.Children.Add(button);
                i++;
            }

            //nastaveni pozice v hlavni mrizce
            Grid.SetColumn(groupBox, (int)buttonGroup.gridPosition.X);
            Grid.SetRow(groupBox, (int)buttonGroup.gridPosition.Y);

            return groupBox;
        }

        //pridani tlacitek do skupiny
        public static void FillComboBox(ComboBox cb, string codePartMatch)
        {
            int i = 0;
            foreach (NameString item in Tags.GetTagListGroup(codePartMatch))
            {
                ComboBoxItem cbItem = new ComboBoxItem();
                cbItem.Content = item.buttonLabel;
                cb.Items.Add(cbItem);
                if (i == 0)
                {
                    cb.SelectedItem = cbItem;
                }
                i++;
            }
        }

        public static ClickReturn AddTag(string insertValue, List<string> tags)
        {
            ClickReturn cr = new ClickReturn();
            cr.error = "";
            cr.tags = tags;

            if (insertValue.StartsWith("{"))
            {
                //omezeni maximalniho poctu tagu                            //+ slozka nemuze obsahovat stejne tagy
                if (cr.tags.Count(x => x.StartsWith("{")) < 6) // 4 && !selectedStructure.Contains(insertValue))
                {
                    //pridani do aktualni struktury nazvu
                    cr.tags.Add(insertValue);
                }
                else
                {
                    cr.error = "Lze použít maximálně 6 tagů";
                }
            }
            else
            {
                if (insertValue == Tags.GetTagByCode(Properties.Resources.NewFolder).visibleText)
                {
                    cr.tags.Add(insertValue);
                    return cr;
                }
                if (cr.tags.Count() == 0) //tagy, které neobsahují {} (např. - _)
                {
                    cr.error = $"Nelze použít {Tags.GetTagByVisibleText(insertValue).visibleText} na začátku názvu.";
                    return cr;
                }
                //nesmí být - nebo _ za sebou
                if ((Tags.GetTagByVisibleText(insertValue) == Tags.GetTagByCode(Properties.Resources.Hyphen) ||
                    Tags.GetTagByVisibleText(insertValue) == Tags.GetTagByCode(Properties.Resources.Underscore)) &&
                    (Tags.GetTagByVisibleText(cr.tags.Last()) != Tags.GetTagByCode(Properties.Resources.Hyphen) &&
                    Tags.GetTagByVisibleText(cr.tags.Last()) != Tags.GetTagByCode(Properties.Resources.Underscore)))
                {

                    cr.tags.Add(insertValue);
                }
                else
                {
                    cr.error = $"Nelze použít {Tags.GetTagByVisibleText(insertValue).visibleText} po {Tags.GetTagByVisibleText(cr.tags.Last()).visibleText}.";
                    return cr;
                }
            }
            return cr;
        }

        //aktualni tagy slozky/souboru zobrazi ve stack panelu
        public static void TagsToStackPanel(StackPanel sp, List<string> tags, MouseButtonEventHandler tagClick)
        {
            sp.Children.Clear();

            int i = 0;
            //vytvoření TextBlocků se všech tagů ve vybrané složce
            foreach (string tag in tags)
            {
                TextBlock block = new TextBlock();
                block.Text = tag;
                block.Padding = new Thickness(5, 0, 5, 0);
                block.MouseLeftButtonDown += tagClick;
                block.Tag = i;
                sp.Children.Add(block);
                i++;
            }
        }

        //změna vybraneho tagu
        public static int SelectTag(StackPanel tags, int selectedIndex, int newIndex)
        {
            //odstarnění zvýraznění u starého tagu
            TextBlock oldBlock = FindTextBlockByIndex(tags, selectedIndex);
            if (oldBlock != null)
            {
                oldBlock.Background = Brushes.Transparent;
            }

            //vyhledání TextBlocku, který se má označit
            TextBlock newBlock = FindTextBlockByIndex(tags, newIndex);

            newBlock.Background = Brushes.Aqua;

            return newIndex;
        }


        //vyhledání TextBlocku ve stackpanelu podle indexu
        public static TextBlock FindTextBlockByIndex(StackPanel sp, int index)
        {
            int i = 0;
            foreach (TextBlock tb in sp.Children)
            {
                if (i == index)
                {
                    return tb;
                }
                i++;
            }
            return new TextBlock();
        }

        //zobrazení ovládacích prvků pro zvolený tag
        public static void ShowControls(StackPanel controlPanel, string blockText)
        {
            string tag = Regex.Match(blockText, @"\{.*?\}").ToString(); //ziskani tagu vcetne {}
            
            //zobrazení ovládacích prvků podle vybraného tagu
            foreach (object o in controlPanel.Children)
            {
                if (o.GetType() == typeof(ComboBox))
                {
                    ComboBox cb = o as ComboBox;
                    cb.Visibility = Visibility.Collapsed;

                    if (tag.Length > 2)
                    {
                        if (cb.Name == "cbYear" && Tags.GetTagByVisibleText(tag).code.StartsWith(Properties.Resources.Year))
                        {
                            cb.Visibility = Visibility.Visible;
                            cb.SelectedItem = GetTagIndex(cb, tag);
                        }
                        if (cb.Name == "cbMonth" && Tags.GetTagByVisibleText(tag).code.StartsWith(Properties.Resources.Month))
                        {
                            cb.Visibility = Visibility.Visible;
                            cb.SelectedItem = GetTagIndex(cb, tag);
                        }
                        if (cb.Name == "cbDay" && Tags.GetTagByVisibleText(tag).code.StartsWith(Properties.Resources.Day))
                        {
                            cb.Visibility = Visibility.Visible;
                            cb.SelectedItem = GetTagIndex(cb, tag);
                        }
                    }
                }
                else if (o.GetType() == typeof(TextBox))
                {
                    TextBox tb = o as TextBox;
                    if (tag.Length > 2 && Tags.GetTagByVisibleText(tag).code == Properties.Resources.CustomText)
                    {
                        tb.Visibility = Visibility.Visible;
                        string value = Regex.Match(blockText, @"(?<=\().+?(?=\))").ToString(); //ziskani hodnoty cutomText
                        if (value.Length > 0)
                        {
                            tb.Text = value;
                        }
                        else
                        {
                            tb.Text = "";
                        }
                    }
                    else
                    {
                        tb.Visibility = Visibility.Collapsed;
                    }
                }
                else if (o.GetType() == typeof(Button))
                {
                    Button btn = o as Button;
                    if (blockText.Length > 0)
                    {
                        btn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        btn.Visibility = Visibility.Collapsed;
                    }
                }
            }


        }

        //ziskani prvku z comboBoxu podle tagu (nazvu)
        private static ComboBoxItem GetTagIndex(ComboBox cb, string tag)
        {
            ComboBoxItem item = new ComboBoxItem();
            foreach (ComboBoxItem it in cb.Items)
            {
                if ((string)it.Content == Tags.GetTagByVisibleText(tag).buttonLabel)
                {
                    return it;
                }
            }
            return item;
        }

        //kontrola, zda všechny tagy CustomText mají vyplněnou hodnotu
        public static int CheckCustomText(List<string> tags)
        {
            int i = 0;
            foreach (string tag in tags)
            {
                if (tag.Contains(Tags.GetTagByCode("STR").visibleText) &&
                    Regex.Match(tag, @"(?<=\().*?(?=\))").ToString().Length == 0)
                {
                    return i;
                }
                i++;
            }

            return -1;
        }

        public static ChangeReturn ComboBoxChanged(ComboBoxItem newItem, int selectedTag, List<string> tagStructure)
        {
            ChangeReturn cReturn = new ChangeReturn();
            cReturn.index = 0;
            NameString newTag = Tags.GetTagByButtonLabel(newItem.Content.ToString());

            //přepsání tagu
            if (tagStructure.Count() > 0)
            {

                int i = selectedTag;
                tagStructure[i] = newTag.visibleText;
                cReturn.index = i;
            }
            cReturn.tagStructure = tagStructure;

            return cReturn;
        }
    }
}
