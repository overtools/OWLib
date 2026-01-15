using System.Collections.Generic;
using TankLib;

namespace TankView.Models;

public sealed class GUIDCollection(IEnumerable<ulong> guids) {
	public IReadOnlyList<ulong> GUIDs { get; } = new List<ulong>(guids);
	public int Count => GUIDs.Count;

	public teResourceGUID this[int index] => new(GUIDs[index]);
}
