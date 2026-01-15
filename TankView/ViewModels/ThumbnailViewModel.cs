using Avalonia.Media.Imaging;
using TankLib;

namespace TankView.ViewModels;

public class ThumbnailViewModel : GUIDViewModel {
	public ThumbnailViewModel(teResourceGUID guid) : base(guid) {
		// todo
	}

	public Bitmap? Thumbnail { get; }
}
