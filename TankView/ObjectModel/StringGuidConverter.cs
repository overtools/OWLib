using System;
using System.Globalization;
using System.Windows.Data;
using TankLib;
using TankView.Helper;

namespace TankView.ObjectModel {
    public class StringGuidConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var guid = DataHelper.GetGuid(value);

            if (guid == null || guid == 0)
                return "null";

            return teResourceGUID.AsString(guid.Value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
