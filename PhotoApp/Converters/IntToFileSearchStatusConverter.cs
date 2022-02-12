using System;
using System.Windows.Data;

namespace PhotoApp.Converters
{
    class IntToFileSearchStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((int)value == Device.DEVICE_FILES_SEARCHING)
            {
                return "Probíhá";
            }
            else if ((int)value == Device.DEVICE_FILES_READY)
            {
                return "Dokončeno";
            }
            else
            {
                return "Neznámý";
            }

        }
        public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
