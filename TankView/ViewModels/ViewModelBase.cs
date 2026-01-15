using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TankView.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged, INotifyPropertyChanging {
	public event PropertyChangedEventHandler? PropertyChanged;
	public event PropertyChangingEventHandler? PropertyChanging;

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected void OnPropertyChanging([CallerMemberName] string? propertyName = null) {
		PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
	}

	protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
		if (EqualityComparer<T>.Default.Equals(field, value)) {
			return false;
		}

		OnPropertyChanging(propertyName);
		field = value;
		OnPropertyChanged(propertyName);

		return true;
	}
}
