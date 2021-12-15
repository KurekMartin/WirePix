using MediaDevices;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoApp
{
    public readonly struct NameString
    {
        public NameString(string code, string visibleText, string buttonLabel)
        {
            this.code = code;
            this.visibleText = visibleText;
            this.buttonLabel = buttonLabel;
        }
        public string code { get; }
        public string visibleText { get; }
        public string buttonLabel { get; }

        public static bool operator ==(NameString n1, NameString n2)
        {
            return (n1.code == n2.code && n1.visibleText == n2.visibleText && n1.buttonLabel == n2.buttonLabel);
        }
        public static bool operator !=(NameString n1, NameString n2)
        {
            return (n1.code != n2.code || n1.visibleText != n2.visibleText || n1.buttonLabel != n2.buttonLabel);
        }
    }

    public readonly struct ButtonStruct
    {
        public ButtonStruct(string code)
        {
            NameString nameString = Tags.GetTagByCode(code);
            this.btnText = nameString.buttonLabel;
            insertValue = nameString.visibleText;
        }
        public string btnText { get; }
        public string insertValue { get; }
    }

    public readonly struct ButtonGroupStruct
    {
        public ButtonGroupStruct(string groupName, List<string> btns, Point gridPos)
        {
            this.groupName = groupName;

            //vytvoreni tlacitek z tagu
            buttons = new List<ButtonStruct>();
            foreach (string btn in btns)
            {
                buttons.Add(new ButtonStruct(btn));
            }

            gridPosition = gridPos;
        }
        public string groupName { get; }
        public List<ButtonStruct> buttons { get; }
        public Point gridPosition { get; }
    }
    static class Tags
    {
        private static readonly List<NameString> tagList = new List<NameString>()
        {
            //            code                              visible text        button label
            new NameString(Properties.Resources.YearLong,   "{YearLong}",       "Rok"),
            new NameString(Properties.Resources.Year,       "{Year}",           "Rok krátce (YY)"),
            new NameString(Properties.Resources.Month,      "{Month}",          "Měsíc"),
            new NameString(Properties.Resources.MonthShort, "{MonthShort}",     "Měsíc krátce (název)"),
            new NameString(Properties.Resources.MonthLong,  "{MonthLong}",      "Měsíc dlouze (název)"),
            new NameString(Properties.Resources.Day,        "{Day}",            "Den"),
            new NameString(Properties.Resources.DayShort,   "{DayShort}",       "Den krátce (název)"),
            new NameString(Properties.Resources.DayLong,    "{DayLong}",        "Den dlouze (název)"),
            new NameString(Properties.Resources.DeviceName, "{DeviceName}",     "Název"),
            new NameString(Properties.Resources.DeviceManuf,"{DeviceMan}",      "Výrobce"),
            new NameString(Properties.Resources.SequenceNum,"{SequenceNum}",    "Číslo"),
            new NameString(Properties.Resources.CustomText, "{CustomText}",     "Text"),
            new NameString(Properties.Resources.FileName,   "{FileName}",       "Název souboru"),
            new NameString(Properties.Resources.NewFolder,  "\\",               "Nová složka"),
            new NameString(Properties.Resources.Hyphen,     "-",                "-"),
            new NameString(Properties.Resources.Underscore, "_",                "_")
        };
        private static readonly List<string> dateTags = new List<string>(){
            Properties.Resources.Year,
            Properties.Resources.YearLong,
            Properties.Resources.Month,
            Properties.Resources.MonthShort,
            Properties.Resources.MonthLong,
            Properties.Resources.Day,
            Properties.Resources.DayLong,
            Properties.Resources.DayShort
        }

        public static NameString GetTagByCode(string code)
        {
            return tagList.First(x => x.code == code);
        }

        public static NameString GetTagByVisibleText(string text)
        {
            if (text.Contains('('))
            {
                text = Regex.Match(text, @"\{.*?\}").ToString(); //v případě tagu s parametrem {tag}(param) odstraní parametr
            }
            return tagList.First(x => x.visibleText == text);
        }

        public static NameString GetTagByButtonLabel(string label)
        {
            return tagList.First(x => x.buttonLabel == label);
        }

        public static List<NameString> GetTagListGroup(string matchCodePart)
        {
            return tagList.FindAll(x => x.code.Contains(matchCodePart));
        }

        //získání hodnot pro zobrazení náhledu výsledného názvu
        public static string GetSampleValueByTag(string codeTag, MediaFileInfo fileInfo = null, Device device = null, string filePath = null)
        {
            DateTime date = DateTime.Now;
            string manufacturer = "Manufacturer";
            string model = "DeviceName";
            string filename = "FileName";
            if (File.Exists(filePath))
            {
                if(dateTags.Contains(codeTag))
                {
                    date = FileExif.GetDateTimeOriginal(filePath);
                    // soubor nemusí obsahovat EXIF informace
                    if (date == new DateTime())
                    {
                        date = (DateTime)fileInfo.CreationTime; // nemusí obsahovat správné datum (někdy se používá DateAuthored nebo LastWriteTime)
                        if (date == new DateTime())
                        {
                            date = (DateTime)fileInfo.DateAuthored;
                        }
                        if (date == new DateTime())
                        {
                            date = (DateTime)fileInfo.LastWriteTime;
                        }
                    }
                }

                if(codeTag == Properties.Resources.DeviceManuf)
                {
                    manufacturer = FileExif.GetManufacturer(filePath);
                    if (manufacturer == null || manufacturer == string.Empty)
                    {
                        manufacturer = device.Manufacturer;
                    }
                }

                if(codeTag == Properties.Resources.DeviceName)
                {
                    model = FileExif.GetModel(filePath);
                    if (model == null || model == string.Empty)
                    {
                        model = device.Name;
                    }
                }

                if(codeTag == Properties.Resources.FileName)
                {
                    filename = Path.GetFileNameWithoutExtension(filePath);
                }
            }

            if (codeTag == Properties.Resources.YearLong)
            {
                return date.Year.ToString();
            }
            else if (codeTag == Properties.Resources.Year)
            {
                return (date.Year % 100).ToString();
            }
            else if (codeTag == Properties.Resources.Month)
            {
                return date.Month.ToString("00");
            }
            else if (codeTag == Properties.Resources.MonthShort)
            {
                return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(date.Month);
            }
            else if (codeTag == Properties.Resources.MonthLong)
            {
                return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(date.Month);
            }
            else if (codeTag == Properties.Resources.Day)
            {
                return date.Day.ToString("00");
            }
            else if (codeTag == Properties.Resources.DayShort)
            {
                return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek);
            }
            else if (codeTag == Properties.Resources.DayLong)
            {
                return CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(date.DayOfWeek);
            }
            else if (codeTag == Properties.Resources.DeviceName)
            {
                return model;
            }
            else if (codeTag == Properties.Resources.DeviceManuf)
            {
                return manufacturer;
            }
            else if (codeTag == Properties.Resources.FileName)
            {
                return filename;
            }
            else if (codeTag == Properties.Resources.SequenceNum)
            {
                return "####";
            }
            else { return ""; }
        }



        //prevede tagy na list
        public static List<string> TagsToList(string tagString)
        {
            List<string> tags = new List<string>();
            while (tagString.Length > 0)
            {
                string tag = Regex.Match(tagString, @"\{.*?\}").ToString(); //ziskani tagu vcetne {}              

                if (tag.Length < tagString.Length && tagString[tag.Length].Equals('('))
                {
                    tag += Regex.Match(tagString, @"\(.*?\)").ToString();
                }
                else if (tagString[0] != '{')
                {
                    tag = Regex.Match(tagString, @"[^\{]*").ToString(); //získání textu po další {
                }

                tagString = tagString.Remove(0, tag.Length);

                tags.Add(tag);
            }
            return tags;
        }

        //dosadi hodnoty za tagy
        public static string TagsToValues(List<string> tags, Device device = null, MediaFileInfo fileInfo = null, string filePath = null)
        {
            string values = "";
            foreach (string tag in tags)
            {
                string str = Regex.Match(tag, @"\{.*?\}").ToString(); //ziskani tagu vcetne {}

                if (str.Length > 2)
                {
                    NameString nameString = GetTagByVisibleText(str);
                    if (nameString.code == Properties.Resources.CustomText)
                    {
                        values += Regex.Match(tag, @"(?<=\().*?(?=\))").ToString(); //ziskani hodnoty cutomText
                    }
                    values += GetSampleValueByTag(nameString.code, fileInfo, device, filePath);
                }
                else
                {
                    values += tag;
                }
            }
            return values;
        }

        //prevod stringu na strukturu slozek
        public static string TagsToBlock(string folders, string file, Device device = null)
        {
            List<string> tagList = new List<string>();
            string block = "";
            int i = 0;
            while (folders != null && folders.Length > 0)
            {
                if (i == 0) { block += "\\"; }
                if (folders[0] == '\\')
                {
                    folders = folders.Trim('\\');
                    block += "\n" + new string(' ', i) + " └> ";
                    i++;
                }
                string level = Regex.Match(folders, @"[^\\]*").ToString(); //ziskani urovne bez \
                tagList = TagsToList(level);
                block += TagsToValues(tagList, device);
                folders = folders.Remove(0, level.Length);
            }

            if (file != null && file.Length > 0)
            {
                tagList = TagsToList(file);
                block += "\n" + new string(' ', i + 6);
                block += TagsToValues(tagList, device) + ".xxx";
            }

            return block;
        }
    }
}
