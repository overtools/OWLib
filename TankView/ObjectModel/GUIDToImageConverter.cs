using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using DataTool.DataModels.Hero;
using DirectXTexNet;
using TankView.Helper;

namespace TankView.ObjectModel {
    public class GUIDToImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var guid = DataHelper.GetGuid(value);
            if (guid == null || DataHelper.GetDataType(guid.Value) != DataHelper.DataType.Image)
                return default;

            try {
                var data = DataHelper.ConvertDDS(guid.Value, DXGI_FORMAT.R8G8B8A8_UNORM, 0, out var width, out var height);
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