using System.Collections.Generic;
using System.Linq;
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
    }
}
