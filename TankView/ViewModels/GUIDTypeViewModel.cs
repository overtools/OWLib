using System.Collections.Generic;
using TankLib;
using TankView.Models;

namespace TankView.ViewModels;

public sealed class GUIDTypeViewModel : ViewModelBase {
	public GUIDTypeViewModel(ushort type, IEnumerable<ulong> guids) {
		Type = type;

		GUIDType = type switch {
			0x003 => GUIDType.Entity,
			0x004 or 0x0F1 => GUIDType.Image,
			0x00C => GUIDType.Model,
			0x03F or 0x0B2 or 0x0BB => GUIDType.Sound,
			0x07C or 0x0A9 or 0x071 => GUIDType.String,
			0x09F => GUIDType.MapHeader,
			0x075 => GUIDType.Hero,
			0x0D0 => GUIDType.Conversation,
			_ => GUIDType.Unknown,
		};

		DisplayName = GUIDType != GUIDType.Unknown ? $"{type:X3}: {GUIDType:G}" : type.ToString("X3");

		Collection = GUIDType switch {
			GUIDType.Image => new ThumbnailListViewModel(guids, GUIDType),
			_ => new GUIDCollectionViewModel<teResourceGUID>(guids, GUIDType),
		};
	}

	public ushort Type { get; }
	public string DisplayName { get; }
	public GUIDType GUIDType { get; }
	public GUIDCollectionViewModel Collection { get; }
}
