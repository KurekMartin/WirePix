using MaterialDesignThemes.Wpf;
using MediaDevices;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters
{
    internal class DeviceTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DeviceType deviceType = (DeviceType)value;
            switch (deviceType)
            {
                case DeviceType.Camera:
                case DeviceType.Video:
                    return PackIconKind.Camera;
                case DeviceType.Phone:
                case DeviceType.MediaPlayer:
                    return PackIconKind.Cellphone;
                default:
                    return PackIconKind.Help;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
