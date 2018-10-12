using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;

namespace DataTool.WPF.Tool {
    public partial class ImageGridView {
        private List<ImageGridEntry> internalItems = new List<ImageGridEntry>();
        public IReadOnlyList<ImageGridEntry> Items => internalItems;
        
        public ImageGridView() {
            InitializeComponent();
        }

        public ImageGridEntry Add(string name, byte[] image) {
            var entry = new ImageGridEntry(name, image);
            Add(entry);
            NotifyPropertyChanged(nameof(Items));
            return entry;
        }

        public void Add(ImageGridEntry entry) {
            internalItems.Add(entry);
            NotifyPropertyChanged(nameof(Items));
        }
        // ReSharper disable once EventNeverSubscribedTo.Global
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ImageGridEntry {
        public string Name { get; }
        public byte[] Image { get; }
        public event RoutedEventHandler OnClick;
        public object Payload { get; set; }
        public RoutedEventHandler ClickRouter { get; }
        public int ImageWidth { get; set; } = 128;
        public int ImageHeight { get; set; } = 128;
        public int Width { get; set; } = 128;
        public int Height { get; set; } = 152;
        
        public ImageGridEntry(string name, byte[] image) {
            Name = name;
            Image = image;

            ClickRouter = (sender, args) => { OnClick?.Invoke(this, args); };
        }
    }
}

