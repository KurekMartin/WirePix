using PhotoApp.Models;
using System;
using System.Windows.Data;

namespace PhotoApp.Converters
{
    class IntToFileSearchStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var state = (int)value;
            switch (state)
            {
                case Device.DEVICE_FILES_NOT_SEARCHED:
                    return Properties.Resources.FileSearchStatus_NotSearched;
                case Device.DEVICE_FILES_WAITING:
                    return Properties.Resources.FileSearchStatus_Waiting;
                case Device.DEVICE_FILES_SEARCHING:
                    return Properties.Resources.FileSearchStatus_Searching;
                case Device.DEVICE_FILES_READY:
                    return Properties.Resources.FileSearchStatus_Ready;
                case Device.DEVICE_FILES_CANCELED:
                    return Properties.Resources.FileSearchStatus_Canceled;
                case Device.DEVICE_FILES_ERROR:
                    return Properties.Resources.FileSearchStatus_Error;
                default:
                    return Properties.Resources.FileSearchStatus_Unknown;

            }
        }
            public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
