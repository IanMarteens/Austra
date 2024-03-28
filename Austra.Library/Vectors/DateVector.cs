namespace Austra.Library;

/// <summary>Represents a dense vector of Austra dates, of arbitrary size.</summary>
public readonly struct DateVector :
    IFormattable,
    IEnumerable<Date>,
    IEquatable<DateVector>,
    IEqualityOperators<DateVector, DateVector, bool>,
    IAdditionOperators<DateVector, NVector, DateVector>,
    IAdditionOperators<DateVector, int, DateVector>,
    ISubtractionOperators<DateVector, DateVector, NVector>,
    ISubtractionOperators<DateVector, int, DateVector>,
    ISafeIndexed, IVector, IIndexable
{
    /// <summary>Stores the components of the vector.</summary>
    private readonly Date[] values;

    /// <summary>Initializes a vector from an array.</summary>
    /// <param name="values">The components of the vector.</param>
    public DateVector(Date[] values) => this.values = values;

    /// <summary>Initializes a vector from a scalar.</summary>
    /// <param name="size">Vector length.</param>
    /// <param name="value">Scalar value to be repeated.</param>
    public DateVector(int size, Date value)
    {
        values = GC.AllocateUninitializedArray<Date>(size);
        Array.Fill(values, value);
    }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public DateVector(int size, Func<int, Date> f)
    {
        values = GC.AllocateUninitializedArray<Date>(size);
        for (int i = 0; i < values.Length; i++)
            values[i] = f(i);
    }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="from">Inclusive lower bound for the random values.</param>
    /// <param name="to">Exclusive pper bound for the random values.</param>
    public DateVector(int size, Date from, Date to)
    {
        values = GC.AllocateUninitializedArray<Date>(size);
        Random rnd = Random.Shared;
        for (int i = 0; i < values.Length; i++)
            values[i] = new((uint)rnd.Next((int)(uint)from, (int)(uint)to));
    }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public DateVector(int size, Func<int, DateVector, Date> f)
    {
        values = new Date[size];
        for (int i = 0; i < values.Length; i++)
            values[i] = f(i, this);
    }

    /// <summary>Creates a vector by concatenating a prefix vector with a new value.</summary>
    /// <param name="prefix">Values at the left.</param>
    /// <param name="newValue">New value at the right.</param>
    public DateVector(DateVector prefix, Date newValue)
    {
        values = GC.AllocateUninitializedArray<Date>(prefix.Length + 1);
        Array.Copy(prefix.values, values, prefix.Length);
        values[^1] = newValue;
    }

    /// <summary>Creates a vector by concatenating a new value with a suffix vector.</summary>
    /// <param name="suffix">Values at the right.</param>
    /// <param name="newValue">New value at the left.</param>
    public DateVector(Date newValue, DateVector suffix)
    {
        values = GC.AllocateUninitializedArray<Date>(suffix.Length + 1);
        values[0] = newValue;
        Array.Copy(suffix.values, 0, values, 1, suffix.Length);
    }

    /// <summary>Creates a vector by concatenating two vectors.</summary>
    /// <param name="v1">First vector.</param>
    /// <param name="v2">Second vector.</param>
    public DateVector(DateVector v1, DateVector v2)
    {
        values = GC.AllocateUninitializedArray<Date>(v1.Length + v2.Length);
        Array.Copy(v1.values, values, v1.Length);
        Array.Copy(v2.values, 0, values, v1.Length, v2.Length);
    }

    /// <summary>Creates a vector by concatenating many vectors.</summary>
    /// <param name="v">An array of vectors.</param>
    public DateVector(params DateVector[] v)
    {
        values = GC.AllocateUninitializedArray<Date>(v.Sum(v => v.Length));
        int offset = 0;
        foreach (DateVector vi in v)
        {
            Array.Copy(vi.values, 0, values, offset, vi.Length);
            offset += vi.Length;
        }
    }

    /// <summary>Creates an identical vector.</summary>
    /// <remarks>This operation does not share the internal storage.</remarks>
    /// <returns>A deep clone of the instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateVector Clone() => (Date[])values.Clone();

    /// <summary>Implicit conversion from an array to a vector.</summary>
    /// <param name="values">An array.</param>
    /// <returns>A vector with the same components as the array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DateVector(Date[] values) => new(values);

    /// <summary>Explicit conversion from vector to array.</summary>
    /// <remarks>Use it carefully: it returns the underlying component array.</remarks>
    /// <param name="v">The original vector.</param>
    /// <returns>The underlying component array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Date[](DateVector v) => v.values;

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
    public Date this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => values[index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => values[index] = value;
    }

    /// <summary>Gets or sets the component at a given index.</summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index.</returns>
    public Date this[Index index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => values[index.GetOffset(values.Length)];
    }

    /// <summary>Extracts a slice from the vector.</summary>
    /// <param name="range">The range to extract.</param>
    /// <returns>A new copy of the requested data.</returns>
    public DateVector this[Range range]
    {
        get
        {
            (int offset, int length) = range.GetOffsetAndLength(values.Length);
            return values[offset..(offset + length)];
        }
    }

    /// <summary>
    /// Safe access to the vector's components. If the index is out of range, a zero is returned.
    /// </summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index, or zero, if index is out of range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Date SafeThis(int index) =>
        (uint)index >= values.Length
        ? default
        : Add(ref MM.GetArrayDataReference(values), index);

    /// <summary>Unsafe access to the vector's components, skipping bounds checking.</summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Date UnsafeThis(int index) =>
        Add(ref MM.GetArrayDataReference(values), index);

    /// <summary>Gets the first value in the vector.</summary>
    public Date First => values[0];
    /// <summary>Gets the last value in the vector.</summary>
    public Date Last => values[^1];

    /// <summary>Casts a vector of dates to a new type.</summary>
    /// <typeparam name="T">The new type of the returned span.</typeparam>
    /// <param name="values">The array to cast.</param>
    /// <returns>A reinterpreted span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<T> Cast<T>(Date[] values) where T : unmanaged =>
        MM.Cast<Date, T>(values.AsSpan());

    /// <summary>Adds a vector of integers to a vector of dates.</summary>
    /// <param name="v1">A date vector operand.</param>
    /// <param name="v2">A vector of integers operand.</param>
    /// <returns>The original date vector shifted by a number of days.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public static DateVector operator +(DateVector v1, NVector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        Date[] result = GC.AllocateUninitializedArray<Date>(v1.Length);
        Cast<int>(v1.values).Add((int[])v2, Cast<int>(result));
        return result;
    }

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="v">A vector summand.</param>
    /// <param name="d">A scalar summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    public static DateVector operator +(DateVector v, int d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<NVector>().Length == v.Length);
        Date[] result = GC.AllocateUninitializedArray<Date>(v.Length);
        Cast<int>(v.values).Add(d, Cast<int>(result));
        return result;
    }

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="d">A scalar summand.</param>
    /// <param name="v">A vector summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateVector operator +(int d, DateVector v) => v + d;

    /// <summary>Subtracts two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The component by component subtraction.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVector operator -(DateVector v1, DateVector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        int[] result = GC.AllocateUninitializedArray<int>(v1.Length);
        Cast<int>(v1.values).Sub(Cast<int>(v2.values), result);
        return result;
    }

    /// <summary>Subtracts a scalar from a vector.</summary>
    /// <param name="v">The vector operand.</param>
    /// <param name="d">The scalar operand.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static DateVector operator -(DateVector v, int d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<NVector>().Length == v.Length);
        Date[] result = GC.AllocateUninitializedArray<Date>(v.Length);
        Cast<int>(v.values).Sub(d, Cast<int>(result));
        return result;
    }

    /// <summary>Subtracts a scalar date from a vector.</summary>
    /// <param name="v">The vector operand.</param>
    /// <param name="d">The scalar operand.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static NVector operator -(DateVector v, Date d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<NVector>().Length == v.Length);
        int[] result = GC.AllocateUninitializedArray<int>(v.Length);
        Cast<int>(v.values).Sub((int)(uint)d, result);
        return result;
    }

    /// <summary>Checks whether the predicate is satisfied by all items.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if all items satisfy the predicate.</returns>
    public bool All(Func<Date, bool> predicate) => values.AsSpan().All(predicate);

    /// <summary>Checks whether the predicate is satisfied by at least one item.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if there exists a item satisfying the predicate.</returns>
    public bool Any(Func<Date, bool> predicate) => values.AsSpan().Any(predicate);

    /// <summary>Checks if the vector contains the given value.</summary>
    /// <param name="value">Value to locate.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    public bool Contains(Date value) => IndexOf(value) != -1;

    /// <summary>Returns a new vector with the distinct values in the original one.</summary>
    /// <remarks>Results are unordered.</remarks>
    /// <returns>A new vector with distinct values.</returns>
    public DateVector Distinct() => values.AsSpan().Distinct();

    /// <summary>Creates a new vector by filtering items with the given predicate.</summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>A new vector with the filtered items.</returns>
    public DateVector Filter(Func<Date, bool> predicate) => values.Filter(predicate);

    /// <summary>Creates a new vector by filtering and mapping at the same time.</summary>
    /// <remarks>This method can save an intermediate buffer and one iteration.</remarks>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new vector with the filtered items.</returns>
    public DateVector FilterMap(Func<Date, bool> predicate, Func<Date, Date> mapper) =>
        values.FilterMap(predicate, mapper);

    /// <summary>Returns all indexes containing ocurrences of a value.</summary>
    /// <param name="value">Value to find.</param>
    /// <returns>An integer sequences with all found indexes.</returns>
    public NSequence Find(Date value) => NSequence.Iterate(values, value);

    /// <summary>Returns all indexes satisfying a condition.</summary>
    /// <param name="condition">The condition to be satisfied.</param>
    /// <returns>An integer sequences with all found indexes.</returns>
    public NSequence Find(Func<Date, bool> condition) => NSequence.Iterate(values, condition);

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(Date value) => IndexOf(value, 0);

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <param name="from">The zero-based starting index.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    public int IndexOf(Date value, int from)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(from >= 0 && from < Length);
        Contract.Ensures(Contract.Result<int>() >= -1 && Contract.Result<int>() < Length);

        int result = Vec.IndexOf(Cast<uint>(values)[from..], (uint)value);
        return result >= 0 ? result + from : -1;
    }

    /// <summary>
    /// Creates a new vector by transforming each item with the given function.
    /// </summary>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new vector with the transformed content.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateVector Map(Func<Date, Date> mapper) => values.Map(mapper);

    /// <summary>Gets the item with the maximum value.</summary>
    /// <returns>The item with the maximum value.</returns>
    public Date Maximum()
    {
        Contract.Requires(IsInitialized);
        return new(Cast<uint>(values).Max());
    }

    /// <summary>Gets the item with the minimum value.</summary>
    /// <returns>The item with the minimum value.</returns>
    public Date Minimum()
    {
        Contract.Requires(IsInitialized);
        return new(Cast<uint>(values).Max());
    }

    /// <summary>Creates an aggregate value by applying the reducer to each item.</summary>
    /// <param name="seed">The initial value.</param>
    /// <param name="reducer">The reducing function.</param>
    /// <returns>The final synthesized value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Date Reduce(Date seed, Func<Date, Date, Date> reducer) =>
        values.AsSpan().Reduce(seed, reducer);

    /// <summary>Creates a reversed copy of the vector.</summary>
    /// <returns>An independent reversed copy.</returns>
    public DateVector Reverse() => values.Reverse();

    /// <summary>Returns a new vector with sorted values.</summary>
    /// <returns>A new vector with sorted values.</returns>
    public DateVector Sort() => values.Sort();

    /// <summary>Returns a new vector with sorted values.</summary>
    /// <returns>A new vector with sorted values.</returns>
    public DateVector SortDescending() => values.SortDescending();

    /// <summary>Combines the common prefix of two vectors.</summary>
    /// <param name="other">Second vector to combine.</param>
    /// <param name="zipper">The combining function.</param>
    /// <returns>The combining function applied to each pair of items.</returns>
    public DateVector Zip(DateVector other, Func<Date, Date, Date> zipper) =>
        values.AsSpan().Zip(other.values, zipper);

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <returns>Space-separated components.</returns>
    public override string ToString() =>
        $"ans ∊ Date({Length})" + Environment.NewLine +
        values.ToString(v => v.ToString());

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        $"ans ∊ Date({Length})" + Environment.NewLine +
        values.ToString(v => v.ToString(format, provider));

    /// <summary>Retrieves an enumerator to iterate over components.</summary>
    /// <returns>The enumerator from the underlying array.</returns>
    public IEnumerator<Date> GetEnumerator() =>
        ((IEnumerable<Date>)values).GetEnumerator();

    /// <summary>Retrieves an enumerator to iterate over components.</summary>
    /// <returns>The enumerator from the underlying array.</returns>
    IEnumerator IEnumerable.GetEnumerator() =>
        values.GetEnumerator();

    /// <summary>Checks if the provided argument is a vector with the same values.</summary>
    /// <param name="other">The vector to be compared.</param>
    /// <returns><see langword="true"/> if the vector argument has the same items.</returns>
    public bool Equals(DateVector other) => values.Eqs(other.values);

    /// <summary>Checks if the provided argument is a vector with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a vector with the same items.</returns>
    public override bool Equals(object? obj) => obj is DateVector vector && Equals(vector);

    /// <summary>Returns the hashcode for this vector.</summary>
    /// <returns>A hashcode summarizing the content of the vector.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)values).GetHashCode(EqualityComparer<Date>.Default);

    /// <summary>Compares two vectors for equality. </summary>
    /// <param name="left">First vector operand.</param>
    /// <param name="right">Second vector operand.</param>
    /// <returns><see langword="true"/> if all corresponding items are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(DateVector left, DateVector right) => left.Equals(right);

    /// <summary>Compares two vectors for inequality. </summary>
    /// <param name="left">First vector operand.</param>
    /// <param name="right">Second vector operand.</param>
    /// <returns><see langword="true"/> if any pair of corresponding items are not equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(DateVector left, DateVector right) => !left.Equals(right);
}
