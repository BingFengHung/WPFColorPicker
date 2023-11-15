using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPFColorPicker
{
    internal class StringToBrushColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string colorString = value.ToString();
            var converter = new BrushConverter();
            if (converter.IsValid(colorString))
            {
                return (Brush)converter.ConvertFromString(colorString);
            }
            else
            {
                // 如果字串無效，返回一個預設顏色，例如透明或黑色
                return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
