using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using DataTool.DataModels.Hero;
using TankView.Helper;

namespace TankView.ObjectModel {
    public class HeroImagesToImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var images = new List<System.Windows.Controls.Image>();
            if (!(value is List<Hero.HeroImage> heroImages)) {
                return images;
            }

            foreach (var heroImage in heroImages) {
                var image = new System.Windows.Controls.Image();

                var guid = heroImage.TextureGUID;
                if (DataHelper.GetDataType(guid) != DataHelper.DataType.Image || guid == 0)
                    continue;

                try {
                    var data = DataHelper.ConvertDDS(guid, out var width, out var height);
                    image.Source = new RGBABitmapSource(data, width, height);
                    image.Width = width;
                    image.Height = height;
                    images.Add(image);
                } catch {
                    // ignored
                }
            }

            return images;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return default;
        }
    }
}