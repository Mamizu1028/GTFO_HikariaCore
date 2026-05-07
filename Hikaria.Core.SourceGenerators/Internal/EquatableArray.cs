using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Hikaria.Core.SourceGenerators.Internal;

public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

    private readonly ImmutableArray<T> _array;

    public EquatableArray(ImmutableArray<T> array) => _array = array;

    public EquatableArray(IEnumerable<T> source) => _array = source?.ToImmutableArray() ?? ImmutableArray<T>.Empty;

    public int Length => _array.IsDefault ? 0 : _array.Length;
    public bool IsEmpty => Length == 0;
    public T this[int index] => _array[index];

    public ImmutableArray<T> AsImmutableArray() => _array.IsDefault ? ImmutableArray<T>.Empty : _array;

    public bool Equals(EquatableArray<T> other)
    {
        if (Length != other.Length) return false;
        for (int i = 0; i < Length; i++)
            if (!_array[i].Equals(other._array[i])) return false;
        return true;
    }

    public override bool Equals(object obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_array.IsDefault) return 0;
        int hash = 17;
        foreach (var item in _array)
            hash = unchecked(hash * 31 + (item?.GetHashCode() ?? 0));
        return hash;
    }

    public IEnumerator<T> GetEnumerator() => AsImmutableArray().AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static bool operator ==(EquatableArray<T> a, EquatableArray<T> b) => a.Equals(b);
    public static bool operator !=(EquatableArray<T> a, EquatableArray<T> b) => !a.Equals(b);
}
