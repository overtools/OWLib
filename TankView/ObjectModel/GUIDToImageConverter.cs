using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using DataTool.WPF.IO;
using DirectXTexNet;
using TankView.Helper;
using TankView.ViewModel;

namespace TankView.ObjectModel {
    public class GUIDToImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is GUIDEntry guid) || DataHelper.GetDataType(guid) != DataHelper.DataType.Image) {
                return default;
            }

            try {
                var data = DataHelper.ConvertDDS(guid, DXGI_FORMAT.R8G8B8A8_UNORM, DDSConverter.ImageFormat.PNG, 0);

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