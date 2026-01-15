using System;
using System.Collections;
using System.Collections.Generic;

namespace TankView;

public sealed class VirtualizedList<T>(int count, Func<int, T> getter) : IList<T> {
	public bool IsFixedSize => true;
	public object SyncRoot => this;
	public bool IsSynchronized => false;
	public int Count { get; } = count;

	public T this[int index] {
		get => getter(index);
		set => throw new NotSupportedException();
	}

	public bool IsReadOnly => true;

	public void Add(T? value) => throw new NotSupportedException();
	public void Clear() => throw new NotSupportedException();
	public bool Contains(T? value) => false;
	public int IndexOf(T? value) => -1;
	public void Insert(int index, T? value) => throw new NotSupportedException();
	public bool Remove(T? value) => throw new NotSupportedException();
	public void RemoveAt(int index) => throw new NotSupportedException();
	public void CopyTo(T[] array, int index) => throw new NotSupportedException();

	public IEnumerator<T> GetEnumerator() {
		for (var i = 0; i < Count; i++) {
			yield return getter(i);
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
