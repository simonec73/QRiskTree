using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QRiskTreeEditor
{
    internal class BoolToTickConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? ((char)0xFC).ToString() : ((char)0xFB).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
