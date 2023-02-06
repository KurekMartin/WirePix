using MaterialDesignThemes.Wpf;
using System;
using System.Globalization;
using System.Windows.Data;
namespace PhotoApp.Converters
{
    internal class FileSearchStatusToListIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int status = (int)value;
            switch (status)
            {
                case Device.DEVICE_FILES_READY:
                    return PackIconKind.Check;
                case Device.DEVICE_FILES_SEARCHING:
                    return PackIconKind.Magnify;
                case Device.DEVICE_FILES_CANCELED:
                    return PackIconKind.Cancel;
                case Device.DEVICE_FILES_WAITING:
                    return PackIconKind.TimerSand;
                case Device.DEVICE_FILES_ERROR:
                    return PackIconKind.AlertCircleOutline;
                case Device.DEVICE_FILES_NOT_SEARCHED:
                    return PackIconKind.TimerSandEmpty;
                default: return PackIconKind.QuestionMarkCircle;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
