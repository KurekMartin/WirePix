using System;
using System.Windows.Data;

namespace PhotoApp.Converters
{
    class IntToFileSearchStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((int)value == Device.DEVICE_FILES_NOT_SEARCHED)
            {
                return Properties.Resources.FileSearchStatus_NotSearched;
            }
            else if ((int)value == Device.DEVICE_FILES_SEARCHING)
            {
                return Properties.Resources.FileSearchStatus_Searching;
            }
            else if ((int)value == Device.DEVICE_FILES_READY)
            {
                return Properties.Resources.FileSearchStatus_Ready;
            }
            else if ((int)value == Device.DEVICE_FILES_ERROR)
            {
                return Properties.Resources.FileSearchStatus_Error;
            }
            else
            {
                return Properties.Resources.FileSearchStatus_Unknown;
            }

        }
        public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
