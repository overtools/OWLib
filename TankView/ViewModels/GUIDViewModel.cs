using TankLib;

namespace TankView.ViewModels;

public class GUIDViewModel(teResourceGUID guid) : ViewModelBase {
	public teResourceGUID GUID { get; } = guid;
	public string Name => GUID.ToString();
}
