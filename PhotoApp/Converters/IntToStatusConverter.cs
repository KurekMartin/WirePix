using System;
using System.Windows.Data;

namespace PhotoApp.Converters
{
    class IntToStatusConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if((int)value== Device.DEVICE_CANNOT_CONNECT)
            {
                return "Nelze se připojit";
            }
            else if((int)value==Device.DEVICE_READY)
            {
                return "Připraveno";
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
