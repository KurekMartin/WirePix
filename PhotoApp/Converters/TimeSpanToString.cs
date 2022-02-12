using System;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters
{
    class TimeSpanToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan time = (TimeSpan)value;
            string timeString = "";
            if (time.Ticks == 0)
            {
                timeString = "Počítám zbývající čas";
            }
            else
            {
                timeString = "Zbývá ";
                if (time.Days > 0)
                {
                    timeString += time.Days + "d ";
                }
                if (time.Hours > 0)
                {
                    timeString += time.Hours + "h ";
                }
                if (time.Minutes > 0)
                {
                    timeString += time.Minutes + "m ";
                }
                timeString += time.Seconds + "s ";
            }
            return timeString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
