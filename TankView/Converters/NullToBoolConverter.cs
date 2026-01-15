using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TankView.Converters;

public sealed class NullToBoolConverter : IValueConverter {
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value != null;
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
