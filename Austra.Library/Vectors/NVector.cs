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
public readonly struct NVector :
    IFormattable,
    IEnumerable<int>,
    IEquatable<NVector>,
    IEqualityOperators<NVector, NVector, bool>,
    IAdditionOperators<NVector, NVector, NVector>,
    IAdditionOperators<NVector, int, NVector>,
    ISubtractionOperators<NVector, NVector, NVector>,
    ISubtractionOperators<NVector, int, NVector>,
    IUnaryNegationOperators<NVector, NVector>,
    IMultiplyOperators<NVector, NVector, int>,
    IMultiplyOperators<NVector, int, NVector>,
    ISafeIndexed, IVector, IIndexable
{
    /// <summary>Stores the components of the vector.</summary>
    private readonly int[] values;

    /// <summary>Creates a vector of a given size.</summary>
    /// <param name="size">Vector length.</param>
    public NVector(int size) => values = size == 0 ? []: new int[size];

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

    /// <summary>Initializes a vector from a scalar.</summary>
    /// <remarks>This constructor is used by the AUSTRA parser.</remarks>
    /// <param name="size">Vector length.</param>
    /// <param name="value">Scalar value to be repeated.</param>
    public NVector(int size, double value)
    {
        values = GC.AllocateUninitializedArray<int>(size);
        Array.Fill(values, (int)value);
    }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="rnd">A random number generator.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NVector(int size, Random rnd)
    {
        values = GC.AllocateUninitializedArray<int>(size);
        for (int i = 0; i < values.Length; i++)
            values[i] = rnd.Next();
    }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="upperBound">Exclusive pper bound for the random values.</param>
    /// <param name="rnd">A random number generator.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NVector(int size, int upperBound, Random rnd)
    {
        values = GC.AllocateUninitializedArray<int>(size);
        for (int i = 0; i < values.Length; i++)
            values[i] = rnd.Next(upperBound);
    }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="lowerBound">Inclusive lower bound for the random values.</param>
    /// <param name="upperBound">Exclusive pper bound for the random values.</param>
    /// <param name="rnd">A random number generator.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NVector(int size, int lowerBound, int upperBound, Random rnd)
    {
        values = GC.AllocateUninitializedArray<int>(size);
        for (int i = 0; i < values.Length; i++)
            values[i] = rnd.Next(lowerBound, upperBound);
    }

    /// <summary>Creates a vector with a given size, and fills it with ones.</summary>
    /// <param name="size">Vector length.</param>
    /// <returns>A new vector with repeated ones.</returns>
    public static NVector Ones(int size) => new(size, 1);

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

    /// <summary>Unsafe access to the vector's components, skipping bounds checking.</summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int UnsafeThis(int index) =>
        Add(ref MM.GetArrayDataReference(values), index);

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

    /// <summary>Gets the item with the maximum value.</summary>
    /// <returns>The item with the maximum value.</returns>
    public int Maximum()
    {
        Contract.Requires(IsInitialized);
        return values.Max();
    }

    /// <summary>Gets the item with the minimum value.</summary>
    /// <returns>The item with the minimum value.</returns>
    public int Minimum()
    {
        Contract.Requires(IsInitialized);
        return values.Min();
    }

    /// <summary>Adds two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The component by component sum.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public static NVector operator +(NVector v1, NVector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        int[] result = GC.AllocateUninitializedArray<int>(v1.Length);
        v1.values.AsSpan().Add(v2.values, result);
        return result;
    }

    /// <summary>Subtracts two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The component by component subtraction.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVector operator -(NVector v1, NVector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        int[] result = GC.AllocateUninitializedArray<int>(v1.Length);
        v1.values.AsSpan().Sub(v2.values, result);
        return result;
    }

    /// <summary>Negates a vector.</summary>
    /// <param name="v">The vector operand.</param>
    /// <returns>The component by component negation.</returns>
    public static NVector operator -(NVector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<NVector>().Length == v.Length);
        int[] result = GC.AllocateUninitializedArray<int>(v.values.Length);
        v.values.AsSpan().Neg(result);
        return result;
    }

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="v">A vector summand.</param>
    /// <param name="d">A scalar summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    public static NVector operator +(NVector v, int d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<NVector>().Length == v.Length);
        int[] result = GC.AllocateUninitializedArray<int>(v.Length);
        v.values.AsSpan().Add(d, result);
        return result;
    }

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="d">A scalar summand.</param>
    /// <param name="v">A vector summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVector operator +(int d, NVector v) => v + d;

    /// <summary>Subtracts a scalar from a vector.</summary>
    /// <param name="v">The vector operand.</param>
    /// <param name="d">The scalar operand.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static NVector operator -(NVector v, int d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<NVector>().Length == v.Length);
        int[] result = GC.AllocateUninitializedArray<int>(v.Length);
        v.values.AsSpan().Sub(d, result);
        return result;
    }

    /// <summary>Subtracts a vector from a scalar.</summary>
    /// <param name="d">The scalar operand.</param>
    /// <param name="v">The vector operand.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static NVector operator -(int d, NVector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<NVector>().Length == v.Length);
        int[] result = GC.AllocateUninitializedArray<int>(v.Length);
        Vec.Sub(d, v.values, result);
        return result;
    }

    /// <summary>Pointwise multiplication.</summary>
    /// <param name="other">Second vector operand.</param>
    /// <returns>The component by component product.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public NVector PointwiseMultiply(NVector other)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(other.IsInitialized);
        if (Length != other.Length)
            throw new VectorLengthException();
        Contract.Ensures(Contract.Result<NVector>().Length == Length);
        return values.AsSpan().Mul(other.values);
    }

    /// <summary>Pointwise division.</summary>
    /// <param name="other">Second vector operand.</param>
    /// <returns>The component by component quotient.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public NVector PointwiseDivide(NVector other)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(other.IsInitialized);
        if (Length != other.Length)
            throw new VectorLengthException();
        Contract.Ensures(Contract.Result<NVector>().Length == Length);
        return values.AsSpan().Div(other.values);
    }

    /// <summary>Inplace negation of the vector.</summary>
    /// <returns>The same vector instance, with items negated.</returns>
    public NVector InplaceNegate()
    {
        values.AsSpan().Neg();
        return this;
    }

    /// <summary>Gets statistics on the vector values.</summary>
    /// <returns>The vector's statistics.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Accumulator Stats() => new(values);

    /// <summary>Dot product of two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The dot product of the operands.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public static int operator *(NVector v1, NVector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        Contract.EndContractBlock();
        return v1.values.AsSpan().Dot(v2.values);
    }

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="v">Vector to be multiplied.</param>
    /// <param name="d">A scalar multiplier.</param>
    /// <returns>The multiplication of the vector by the scalar.</returns>
    public static NVector operator *(NVector v, int d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == v.Length);
        int[] result = GC.AllocateUninitializedArray<int>(v.values.Length);
        v.values.AsSpan().Mul(d, result);
        return result;
    }

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="d">A scalar multiplicand.</param>
    /// <param name="v">Vector to be multiplied.</param>
    /// <returns>The multiplication of the vector by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVector operator *(int d, NVector v) => v * d;

    /// <summary>Divides a vector by a scalar value.</summary>
    /// <param name="v">Vector to be divided.</param>
    /// <param name="d">A scalar divisor.</param>
    /// <returns>The quotient of the vector over the scalar.</returns>
    public static NVector operator /(NVector v, int d) => v.values.AsSpan().Div(d);

    /// <summary>Calculates the product of the vector's items.</summary>
    /// <returns>The product of all vector's items.</returns>
    public int Product()
    {
        Contract.Requires(IsInitialized);

        int result = 1;
        ref int p = ref MM.GetArrayDataReference(values);
        ref int q = ref Add(ref p, values.Length);
        if (V8.IsHardwareAccelerated && Length > V8i.Count)
        {
            ref int last = ref Add(ref p, values.Length & Simd.MASK16);
            V8i prod = V8i.One;
            do
            {
                prod *= V8.LoadUnsafe(ref p);
                p = ref Add(ref p, V8i.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            result = (prod.GetLower() * prod.GetUpper()).Product();
        }
        else if (V4.IsHardwareAccelerated && Length > V4i.Count)
        {
            ref int last = ref Add(ref p, values.Length & Simd.MASK8);
            V4i prod = V4i.One;
            do
            {
                prod *= V4.LoadUnsafe(ref p);
                p = ref Add(ref p, V4i.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            result = prod.Product();
        }
        for (; IsAddressLessThan(ref p, ref q); p = ref Add(ref p, 1))
            result *= p;
        return result;
    }

    /// <summary>Gets the absolute values of the vector's items.</summary>
    /// <returns>A new vector with non-negative items.</returns>
    public NVector Abs()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == Length);
        return values.Abs();
    }

    /// <summary>Checks whether the predicate is satisfied by all items.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if all items satisfy the predicate.</returns>
    public bool All(Func<int, bool> predicate) => values.AsSpan().All(predicate);

    /// <summary>Checks whether the predicate is satisfied by at least one item.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if there exists a item satisfying the predicate.</returns>
    public bool Any(Func<int, bool> predicate) => values.AsSpan().Any(predicate);

    /// <summary>Returns a new vector with the distinct values in the original one.</summary>
    /// <remarks>Results are unordered.</remarks>
    /// <returns>A new vector with distinct values.</returns>
    public NVector Distinct() => values.AsSpan().Distinct();

    /// <summary>Creates a new vector by filtering items with the given predicate.</summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>A new vector with the filtered items.</returns>
    public NVector Filter(Func<int, bool> predicate) => values.Filter(predicate);

    /// <summary>Creates a new vector by filtering and mapping at the same time.</summary>
    /// <remarks>This method can save an intermediate buffer and one iteration.</remarks>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new vector with the filtered items.</returns>
    public NVector FilterMap(Func<int, bool> predicate, Func<int, int> mapper) =>
        values.FilterMap(predicate, mapper);

    /// <summary>Checks if the vector contains the given value.</summary>
    /// <param name="value">Value to locate.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    public bool Contains(int value) => IndexOf(value) != -1;

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(int value) => IndexOf(value, 0);

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <param name="from">The zero-based starting index.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    public int IndexOf(int value, int from)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(from >= 0 && from < Length);
        Contract.Ensures(Contract.Result<int>() >= -1 && Contract.Result<int>() < Length);

        int result = new ReadOnlySpan<int>(values, from, Length - from).IndexOf(value);
        return result >= 0 ? result + from : -1;
    }

    /// <summary>Returns all indexes containing ocurrences of a value.</summary>
    /// <param name="value">Value to find.</param>
    /// <returns>An integer sequences with all found indexes.</returns>
    public NSequence Find(int value) => NSequence.Iterate((int[])this, value);

    /// <summary>Returns all indexes satisfying a condition.</summary>
    /// <param name="condition">The condition to be satisfied.</param>
    /// <returns>An integer sequences with all found indexes.</returns>
    public NSequence Find(Func<int, bool> condition) => NSequence.Iterate((int[])this, condition);

    /// <summary>
    /// Creates a new vector by transforming each item with the given function.
    /// </summary>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new vector with the transformed content.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NVector Map(Func<int, int> mapper) => values.Map(mapper);

    /// <summary>
    /// Creates a new real vector by transforming each item with the given function.
    /// </summary>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new real vector with the transformed content.</returns>
    public DVector MapReal(Func<int, double> mapper)
    {
        double[] newValues = GC.AllocateUninitializedArray<double>(values.Length);
        ref int p = ref MM.GetArrayDataReference(values);
        for (int i = 0; i < newValues.Length; i++)
            newValues[i] = mapper(Add(ref p, i));
        return newValues;
    }

    /// <summary>Creates an aggregate value by applying the reducer to each item.</summary>
    /// <param name="seed">The initial value.</param>
    /// <param name="reducer">The reducing function.</param>
    /// <returns>The final synthesized value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Reduce(int seed, Func<int, int, int> reducer) =>
        values.AsSpan().Reduce(seed, reducer);

    /// <summary>Calculates the sum of the vector's items.</summary>
    /// <returns>The sum of all vector's items.</returns>
    public int Sum()
    {
        Contract.Requires(IsInitialized);
        return values.Sum();
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

    /// <summary>Returns a new vector with sorted values.</summary>
    /// <returns>A new vector with sorted values.</returns>
    public NVector SortDescending()
    {
        NVector result = Clone();
        Array.Sort(result.values, static (x, y) => y.CompareTo(x));
        return result;
    }

    /// <summary>Combines the common prefix of two vectors.</summary>
    /// <param name="other">Second vector to combine.</param>
    /// <param name="zipper">The combining function.</param>
    /// <returns>The combining function applied to each pair of items.</returns>
    public NVector Zip(NVector other, Func<int, int, int> zipper) =>
        values.AsSpan().Zip(other.values, zipper);

    /// <summary>Convert this vector to a vector of reals.</summary>
    /// <returns>A new vector of reals.</returns>
    public DVector ToVector()
    {
        double[] newValues = GC.AllocateUninitializedArray<double>(Length);
        ref int p = ref MM.GetArrayDataReference(values);
        ref double q = ref MM.GetArrayDataReference(newValues);
        if (Avx.IsSupported && Length >= V4i.Count)
        {
            nuint top = (nuint)(Length - V4i.Count);
            for (nuint i = 0; i < top; i += (nuint)V4i.Count)
            {
                var (lower, upper) = V4.Widen(V4.LoadUnsafe(ref p, i));
                V4.StoreUnsafe(V4.ConvertToDouble(lower), ref q, i);
                V4.StoreUnsafe(V4.ConvertToDouble(upper), ref q, i + 4);
            }
            var (lo, up) = V4.Widen(V4.LoadUnsafe(ref p, top));
            V4.StoreUnsafe(V4.ConvertToDouble(lo), ref q, top);
            V4.StoreUnsafe(V4.ConvertToDouble(up), ref q, top + 4);
        }
        else
            for (int i = 0; i < newValues.Length; i++)
                Add(ref q, i) = Add(ref p, i);
        return newValues;
    }

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <returns>Space-separated components.</returns>
    public override string ToString() =>
        $"ans ∊ ℤ({Length})" + Environment.NewLine +
        values.ToString(v => v.ToString("N0"));

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        $"ans ∊ ℤ({Length})" + Environment.NewLine +
        values.ToString(v => v.ToString(format, provider));

    /// <summary>Retrieves an enumerator to iterate over components.</summary>
    /// <returns>The enumerator from the underlying array.</returns>
    public IEnumerator<int> GetEnumerator() =>
        ((IEnumerable<int>)values).GetEnumerator();

    /// <summary>Retrieves an enumerator to iterate over components.</summary>
    /// <returns>The enumerator from the underlying array.</returns>
    IEnumerator IEnumerable.GetEnumerator() =>
        values.GetEnumerator();

    /// <summary>Checks if the provided argument is a vector with the same values.</summary>
    /// <param name="other">The vector to be compared.</param>
    /// <returns><see langword="true"/> if the vector argument has the same items.</returns>
    public bool Equals(NVector other) => values.Eqs(other.values);

    /// <summary>Checks if the provided argument is a vector with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a vector with the same items.</returns>
    public override bool Equals(object? obj) => obj is NVector vector && Equals(vector);

    /// <summary>Returns the hashcode for this vector.</summary>
    /// <returns>A hashcode summarizing the content of the vector.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)values).GetHashCode(EqualityComparer<int>.Default);

    /// <summary>Compares two vectors for equality. </summary>
    /// <param name="left">First vector operand.</param>
    /// <param name="right">Second vector operand.</param>
    /// <returns><see langword="true"/> if all corresponding items are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(NVector left, NVector right) => left.Equals(right);

    /// <summary>Compares two vectors for inequality. </summary>
    /// <param name="left">First vector operand.</param>
    /// <param name="right">Second vector operand.</param>
    /// <returns><see langword="true"/> if any pair of corresponding items are not equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(NVector left, NVector right) => !left.Equals(right);
}
