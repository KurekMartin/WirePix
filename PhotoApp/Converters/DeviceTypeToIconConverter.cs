using MaterialDesignThemes.Wpf;
using MediaDevices;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
