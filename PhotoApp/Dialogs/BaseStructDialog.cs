using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs
{
    static class BaseStructDialog
    {
        public static bool TagAdd(string tagCode, List<string> tags, TextBlock error)
        {
            TagStruct tag = Tags.GetTag(code: tagCode);
            if (tag.Group == Properties.TagGroups.Separator && tags.Count() == 0) //separator na začátku
            {
                error.Text = $"Nelze použít {Tags.GetTag(code: tagCode).ButtonLabel} na začátku názvu.";
                return false;
            }
            else if (tags.Count(x => tag.Group != Properties.TagGroups.Separator) > Properties.Settings.Default.MaxTags) //dosažen max počet tagů
            {
                error.Text = $"Lze použít maximálně {Properties.Settings.Default.MaxTags} značek";
                return false;
            }
            else if (tag.Group == Properties.TagGroups.Separator) //dva separatory za sebou
            {
                TagStruct lastTag = Tags.GetTag(code: tags.Last());
                if (lastTag.Group == Properties.TagGroups.Separator)
                {
                    error.Text = $"Nelze použít {tag.ButtonLabel} po {lastTag.ButtonLabel}.";
                    return false;
                }
            }
            return true;
        }
        public static bool ValidCustomText(TextBox textBox)
        {
            if (textBox.Visibility != Visibility.Visible)
            {
                return true;
            }
            else
            {
                return Tags.IsValidFileName(textBox.Text);
            }
        }
        public static void ShowCustomTextError(TextBox customText,TextBlock errorBlock)
        {
            string text = customText.Text;
            if (text.Length == 0)
            {
                errorBlock.Text = $"Chybí hodnota pro {Tags.GetTag(code: Properties.TagCodes.CustomText).VisibleText}.\n" +
                    $"Doplňte tuto hodnotu nebo tag odstraňte";
                errorBlock.Visibility = Visibility.Visible;
            }
            else if (text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                List<char> invalidChars = text.Where(x => Path.GetInvalidFileNameChars().Contains(x)).ToList();
                errorBlock.Text = $"Text obsahuje neplatné znaky: {string.Join("", invalidChars)}";
                errorBlock.Visibility = Visibility.Visible;
            }
            customText.Focus();
        }
    }
}
