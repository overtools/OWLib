using TankLib;

namespace TankView.Models;

public sealed record GUIDAsset(teResourceGUID GUID) {
	public int Type => teResourceGUID.Type(GUID);
}
