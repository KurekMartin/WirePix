﻿using MaterialDesignThemes.Wpf;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters
{
    internal class FileSearchStatusToButtonIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int status = (int)value;
            if (status == Device.DEVICE_FILES_NOT_SEARCHED ||
                status == Device.DEVICE_FILES_CANCELED ||
                status == Device.DEVICE_FILES_ERROR ||
                status == Device.DEVICE_FILES_READY)
            {
                return PackIconKind.Refresh;
            }
            else if (status == Device.DEVICE_FILES_WAITING ||
                     status == Device.DEVICE_FILES_SEARCHING)
            {
                return PackIconKind.Cancel;
            }
            return PackIconKind.Error;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
