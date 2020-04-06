using TankView.ObjectModel;
using TankView.Properties;

namespace TankView.ViewModel {
    public class ImageExtractionFormats : ObservableHashCollection<ImageFormat> {
        public ImageExtractionFormats() {
            Add(new ImageFormat("png", "PNG"));
            Add(new ImageFormat("tif", "TIF"));
            Add(new ImageFormat("dds", "DDS"));
            Add(new ImageFormat("tga", "TGA"));
            Add(new ImageFormat("jpg", "JPG"));
        }
    }

    public class ImageFormat {
        public string Format { get; set; }
        public string Name { get; set; }

        private bool _active { get; set; }

        public bool Active {
            get => _active;
            set {
                if (value == true) {
                    Settings.Default.ImageExtractionFormat = Format;
                    Settings.Default.Save();
                }

                _active = value;
            }
        }

        public ImageFormat(string v1, string v2) {
            Format = v1;
            Name = v2;
            _active = Settings.Default.ImageExtractionFormat == Format;
        }

        // ReSharper disable once NonReadonlyMemberInGetHashCode
        public override int GetHashCode() {
            return (Format?.ToLowerInvariant()?.GetHashCode()).GetValueOrDefault();
        }
    }
}
