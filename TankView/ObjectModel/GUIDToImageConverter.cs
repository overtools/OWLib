using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using DataTool.Helper;
using DirectXTexNet;
using TankView.Helper;

namespace TankView.ObjectModel {
    public class GUIDToImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var guid = DataHelper.GetGuid(value);
            if (guid == null || DataHelper.GetDataType(guid.Value) != DataHelper.DataType.Image)
                return default;

            try {
                var data = DataHelper.ConvertDDS(guid.Value, DXGI_FORMAT.R8G8B8A8_UNORM, WICCodecs.PNG, 0);

                var bitmap = new BitmapImage();
                using (var ms = new MemoryStream(data)) {
                    ms.Position = 0;
                    bitmap.BeginInit();
                    bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = null;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                }

                bitmap.Freeze();
                return bitmap;
            } catch {
                return default;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return default;
        }
    }
}