using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TankView.Helper;

namespace TankView.ObjectModel {
    public class GUIDVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var guid = DataHelper.GetGuid(value);
            if (guid == null || guid == 0)
                return Visibility.Collapsed;;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
