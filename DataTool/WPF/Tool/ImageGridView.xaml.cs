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

        public ImageGridEntry Add(string name, byte[] image, int width = 128, int height = 128) {
            var entry = new ImageGridEntry(name, image) {
                Width = width + 256,
                Height = height,
                ImageWidth = width,
                ImageHeight = height
            };
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

        private void SelectHandler(object sender, RoutedEventArgs @event) {
            (ImageList.SelectedItem as ImageGridEntry)?.ClickRouter(ImageList.SelectedItem, @event);
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
        public Cursor Cursor { get; set; } = Cursors.Hand;
        
        public ImageGridEntry(string name, byte[] image) {
            Name = name;
            Image = image;

            ClickRouter = (sender, args) => { OnClick?.Invoke(this, args); };
        }
    }
}

