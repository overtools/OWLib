using System.Collections.Generic;
using TankLib;
using TankView.Models;

namespace TankView.ViewModels;

public abstract class GUIDCollectionViewModel : ViewModelBase {
	protected GUIDCollectionViewModel(IEnumerable<ulong> guids, GUIDType type) {
		GUIDType = type;
		GUIDs = new GUIDCollection(guids);
		Items = new VirtualizedList<GUIDViewModel>(GUIDs.Count, GetItem);
	}

	public GUIDType GUIDType { get; }
	public GUIDCollection GUIDs { get; }

	public int Count => GUIDs.Count;
	public IList<GUIDViewModel> Items { get; }

	public GUIDViewModel? SelectedItem {
		get;
		set => SetField(ref field, value);
	}

	public abstract object? Preview { get; }
	public virtual GUIDViewModel GetItem(int index) => new(GUIDs[index]);
}

public class GUIDCollectionViewModel<T>(IEnumerable<ulong> guids, GUIDType type) : GUIDCollectionViewModel(guids, type) where T : new() {
	public override object? Preview { get; } = typeof(T) == typeof(teResourceGUID) ? null : new T();
}
