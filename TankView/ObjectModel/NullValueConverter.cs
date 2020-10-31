using System;
using System.Globalization;
using System.Windows.Data;

namespace TankView.ObjectModel {
    public class NullValueConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = (string) value;
            return string.IsNullOrEmpty(str) ? "null" : str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
