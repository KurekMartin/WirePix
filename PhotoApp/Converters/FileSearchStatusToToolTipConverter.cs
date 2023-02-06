using MaterialDesignThemes.Wpf;
using PhotoApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PhotoApp.Converters
{
    internal class FileSearchStatusToToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int status = (int)value;
            if (status == Device.DEVICE_FILES_NOT_SEARCHED ||
                status == Device.DEVICE_FILES_CANCELED ||
                status == Device.DEVICE_FILES_ERROR ||
                status == Device.DEVICE_FILES_READY)
            {
                return Properties.Resources.Refresh;
            }
            else if (status == Device.DEVICE_FILES_WAITING ||
                     status == Device.DEVICE_FILES_SEARCHING)
            {
                return Properties.Resources.Cancel;
            }
            return Properties.Resources.Error;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
