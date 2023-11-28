namespace Austra.Library;

/// <summary>Represents a dense vector of integers, of arbitrary size.</summary>
/// <remarks>
/// <para>
/// <see cref="NVector"/> provides a thin wrapper around a one-dimensional array.
/// Most method operations are non destructive, and return a new vector,
/// at the cost of extra memory allocation.
/// </para>
/// <para>
/// Also, most methods are hardware accelerated, either by using managed references,
/// SIMD operations or both. Memory pinning has been reduced to the minimum, for
/// easing the garbage collector's work.
/// </para>
/// </remarks>
public readonly struct NVector: ISafeIndexed, IVector
{
    /// <summary>Stores the components of the vector.</summary>
    private readonly int[] values;

    /// <summary>Creates a vector of a given size.</summary>
    /// <param name="size">Vector length.</param>
    public NVector(int size) => values = new int[size];

    /// <summary>Initializes a vector from an array.</summary>
    /// <param name="values">The components of the vector.</param>
    public NVector(int[] values) => this.values = values;

    /// <summary>Initializes a vector from a scalar.</summary>
    /// <param name="size">Vector length.</param>
    /// <param name="value">Scalar value to be repeated.</param>
    public NVector(int size, int value)
    {
        values = GC.AllocateUninitializedArray<int>(size);
        Array.Fill(values, value);
    }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public NVector(int size, Func<int, int> f)
    {
        values = GC.AllocateUninitializedArray<int>(size);
        for (int i = 0; i < values.Length; i++)
            values[i] = f(i);
    }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public NVector(int size, Func<int, NVector, int> f)
    {
        values = new int[size];
        for (int i = 0; i < values.Length; i++)
            values[i] = f(i, this);
    }

    /// <summary>Creates a vector by concatenating a prefix vector with a new value.</summary>
    /// <param name="prefix">Values at the left.</param>
    /// <param name="newValue">New value at the right.</param>
    public NVector(NVector prefix, int newValue)
    {
        values = GC.AllocateUninitializedArray<int>(prefix.Length + 1);
        Array.Copy(prefix.values, values, prefix.Length);
        values[^1] = newValue;
    }

    /// <summary>Creates a vector by concatenating a new value with a suffix vector.</summary>
    /// <param name="suffix">Values at the right.</param>
    /// <param name="newValue">New value at the left.</param>
    public NVector(int newValue, NVector suffix)
    {
        values = GC.AllocateUninitializedArray<int>(suffix.Length + 1);
        values[0] = newValue;
        Array.Copy(suffix.values, 0, values, 1, suffix.Length);
    }

    /// <summary>Creates a vector by concatenating two vectors.</summary>
    /// <param name="v1">First vector.</param>
    /// <param name="v2">Second vector.</param>
    public NVector(NVector v1, NVector v2)
    {
        values = GC.AllocateUninitializedArray<int>(v1.Length + v2.Length);
        Array.Copy(v1.values, values, v1.Length);
        Array.Copy(v2.values, 0, values, v1.Length, v2.Length);
    }

    /// <summary>Creates a vector by concatenating many vectors.</summary>
    /// <param name="v">An array of vectors.</param>
    public NVector(params NVector[] v)
    {
        values = GC.AllocateUninitializedArray<int>(v.Sum(v => v.Length));
        int offset = 0;
        foreach (NVector vi in v)
        {
            Array.Copy(vi.values, 0, values, offset, vi.Length);
            offset += vi.Length;
        }
    }

    /// <summary>Creates an identical vector.</summary>
    /// <remarks>This operation does not share the internal storage.</remarks>
    /// <returns>A deep clone of the instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NVector Clone() => (int[])values.Clone();

    /// <summary>Implicit conversion from an array to a vector.</summary>
    /// <param name="values">An array.</param>
    /// <returns>A vector with the same components as the array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NVector(int[] values) => new(values);

    /// <summary>Explicit conversion from vector to array.</summary>
    /// <remarks>
    /// Use carefully: it returns the underlying component array.
    /// </remarks>
    /// <param name="v">The original vector.</param>
    /// <returns>The underlying component array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int[](NVector v) => v.values;

    /// <summary>Gets the dimensions of the vector.</summary>
    public int Length => values.Length;

    /// <summary>Has the vector been properly initialized?</summary>
    /// <remarks>
    /// Since Vector is a struct, its default constructor doesn't
    /// initializes the underlying component array.
    /// </remarks>
    public bool IsInitialized => values != null;

    /// <summary>Gets or sets the component at a given index.</summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index.</returns>
    public int this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => values[index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => values[index] = value;
    }

    /// <summary>Gets or sets the component at a given index.</summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index.</returns>
    public int this[Index index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => values[index.GetOffset(values.Length)];
    }

    /// <summary>
    /// Safe access to the vector's components. If the index is out of range, a zero is returned.
    /// </summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index, or zero, if index is out of range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int SafeThis(int index) =>
        (uint)index >= values.Length
        ? 0
        : Add(ref MM.GetArrayDataReference(values), index);

    /// <summary>Gets the first value in the vector.</summary>
    public int First => values[0];
    /// <summary>Gets the last value in the vector.</summary>
    public int Last => values[^1];

    /// <summary>Copies the content of this vector into an existing one.</summary>
    /// <remarks>This operation does not share the internal storage.</remarks>
    /// <param name="dest">The destination vector.</param>
    internal void CopyTo(NVector dest)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(dest.IsInitialized);
        Contract.Requires(Length == dest.Length);
        Array.Copy(values, dest.values, Length);
    }

    /// <summary>Extracts a slice from the vector.</summary>
    /// <param name="range">The range to extract.</param>
    /// <returns>A new copy of the requested data.</returns>
    public NVector this[Range range]
    {
        get
        {
            (int offset, int length) = range.GetOffsetAndLength(values.Length);
            return values[offset..(offset + length)];
        }
    }

    /// <summary>Checks whether the predicate is satisfied by all items.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if all items satisfy the predicate.</returns>
    public bool All(Func<int, bool> predicate)
    {
        foreach (int value in values)
            if (!predicate(value))
                return false;
        return true;
    }

    /// <summary>Checks whether the predicate is satisfied by at least one item.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if there exists a item satisfying the predicate.</returns>
    public bool Any(Func<int, bool> predicate)
    {
        foreach (int value in values)
            if (predicate(value))
                return true;
        return false;
    }

    /// <summary>Returns a new vector with the distinct values in the original one.</summary>
    /// <remarks>Results are unordered.</remarks>
    /// <returns>A new vector with distinct values.</returns>
    public NVector Distinct()
    {
        HashSet<int> set = new(Length);
        foreach (int value in values)
            set.Add(value);
        return new(set.ToArray());
    }

    /// <summary>
    /// Creates a new vector by filtering the items with the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>A new vector with the filtered items.</returns>
    public NVector Filter(Func<int, bool> predicate)
    {
        int[] newValues = GC.AllocateUninitializedArray<int>(values.Length);
        int j = 0;
        foreach (int value in values)
            if (predicate(value))
                newValues[j++] = value;
        return j == 0 ? new NVector(0) : j == Length ? this : newValues[..j];
    }

    /// <summary>
    /// Creates a new vector by transforming each item with the given function.
    /// </summary>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new vector with the transformed content.</returns>
    public NVector Map(Func<int, int> mapper)
    {
        int[] newValues = GC.AllocateUninitializedArray<int>(values.Length);
        ref int p = ref MM.GetArrayDataReference(values);
        for (int i = 0; i < newValues.Length; i++)
            newValues[i] = mapper(Add(ref p, i));
        return newValues;
    }

    /// <summary>Creates an aggregate value by applying the reducer to each item.</summary>
    /// <param name="seed">The initial value.</param>
    /// <param name="reducer">The reducing function.</param>
    /// <returns>The final synthesized value.</returns>
    public double Reduce(int seed, Func<int, int, int> reducer)
    {
        foreach (int value in values)
            seed = reducer(seed, value);
        return seed;
    }

    /// <summary>Creates a reversed copy of the vector.</summary>
    /// <returns>An independent reversed copy.</returns>
    public NVector Reverse()
    {
        NVector result = Clone();
        Array.Reverse(result.values);
        return result;
    }

    /// <summary>Returns a new vector with sorted values.</summary>
    /// <returns>A new vector with sorted values.</returns>
    public NVector Sort()
    {
        NVector result = Clone();
        Array.Sort(result.values);
        return result;
    }

    /// <summary>Combines the common prefix of two vectors.</summary>
    /// <param name="other">Second vector to combine.</param>
    /// <param name="zipper">The combining function.</param>
    /// <returns>The combining function applied to each pair of items.</returns>
    public NVector Zip(NVector other, Func<int, int, int> zipper)
    {
        int len = Min(Length, other.Length);
        int[] newValues = GC.AllocateUninitializedArray<int>(len);
        ref int p = ref MM.GetArrayDataReference(values);
        ref int q = ref MM.GetArrayDataReference(other.values);
        ref int r = ref MM.GetArrayDataReference(newValues);
        for (int i = 0; i < len; i++)
            Add(ref r, i) = zipper(Add(ref p, i), Add(ref q, i));
        return newValues;
    }
}
