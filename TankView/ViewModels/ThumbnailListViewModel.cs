using System.Collections.Generic;
using TankView.Models;
using TankView.ViewModels.Preview;

namespace TankView.ViewModels;

public sealed class ThumbnailListViewModel(IEnumerable<ulong> guids, GUIDType guidType) : GUIDCollectionViewModel<PreviewImageViewModel>(guids, guidType) {
	public override GUIDViewModel GetItem(int index) => new ThumbnailViewModel(GUIDs[index]);
}
