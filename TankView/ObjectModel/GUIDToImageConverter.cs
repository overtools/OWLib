using System;
using System.Globalization;
using System.Windows.Data;
using TankView.Helper;

namespace TankView.ObjectModel {
    public class GUIDToImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var guid = DataHelper.GetGuid(value);
            if (guid == null || DataHelper.GetDataType(guid.Value) != DataHelper.DataType.Image)
                return default;

            try {
                var data = DataHelper.ConvertDDS(guid.Value, out var width, out var height);
                if(data.IsEmpty) {
                    return null;
                }
                return new RGBABitmapSource(data, width, height);
            } catch {
                return default;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return default;
        }
    }
}