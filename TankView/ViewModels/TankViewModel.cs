using System.Collections.Generic;
using System.Linq;
using TACTLib.Core.Product.Tank;
using TankLib;

namespace TankView.ViewModels;

public class TankViewModel : ViewModelBase {
	public TankViewModel(ProductHandler_Tank tank) {
		GUIDTypes = tank.m_assets.Keys.GroupBy(teResourceGUID.Type).Select(x => new GUIDTypeViewModel(x.Key, x)).ToList();
		SelectedType = GUIDTypes.FirstOrDefault(x => x.Type == 0x004) ?? GUIDTypes[0];
	}

	public TankViewModel() {
		GUIDTypes = [];
		SelectedType = new GUIDTypeViewModel(0, []);
	}

	public IReadOnlyList<GUIDTypeViewModel> GUIDTypes { get; }

	public GUIDTypeViewModel SelectedType {
		get;
		set => SetField(ref field, value);
	}
}
