using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace PhotoApp.Converters
{
    class ListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string separator = parameter as string;
            List<string> list;
            if (value.GetType() == typeof(ObservableCollection<string>))
            {
                list = ((ObservableCollection<string>)value).ToList();
            }
            else
            {
                list = (List<string>)value;
            }
            return string.Join(separator, list.ToArray());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
