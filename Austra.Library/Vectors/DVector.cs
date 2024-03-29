﻿namespace Austra.Library;

/// <summary>Represents a dense vector of double values, of arbitrary size.</summary>
/// <remarks>
/// <para>
/// <see cref="DVector"/> provides a thin wrapper around a single array.
/// Most method operations are non destructive, and return a new vector,
/// at the cost of extra memory allocation.
/// </para>
/// <para>
/// Methods like <see cref="MultiplyAdd(DVector, DVector)"/> save memory by reusing 
/// intermediate storage, and also time by using SIMD fused multiply-add instructions.
/// </para>
/// <para>
/// Most methods are hardware accelerated, either by using managed references,
/// SIMD operations or both. Memory pinning has been reduced to the minimum, for
/// easing the garbage collector's work.
/// </para>
/// </remarks>
public readonly struct DVector :
    IFormattable,
    IEnumerable<double>,
    IEquatable<DVector>,
    IEqualityOperators<DVector, DVector, bool>,
    IAdditionOperators<DVector, DVector, DVector>,
    IAdditionOperators<DVector, double, DVector>,
    ISubtractionOperators<DVector, DVector, DVector>,
    ISubtractionOperators<DVector, double, DVector>,
    IMultiplyOperators<DVector, DVector, double>,
    IMultiplyOperators<DVector, double, DVector>,
    IDivisionOperators<DVector, double, DVector>,
    IUnaryNegationOperators<DVector, DVector>,
    IPointwiseOperators<DVector>,
    ISafeIndexed, IVector, IIndexable
{
    /// <summary>Stores the components of the vector.</summary>
    private readonly double[] values;

    /// <summary>Creates a vector of a given size.</summary>
    /// <param name="size">Vector length.</param>
    public DVector(int size) => values = size == 0 ? [] : new double[size];

    /// <summary>Initializes a vector from an array.</summary>
    /// <param name="values">The components of the vector.</param>
    public DVector(double[] values) => this.values = values;

    /// <summary>Initializes a vector from a scalar.</summary>
    /// <param name="size">Vector length.</param>
    /// <param name="value">Scalar value to be repeated.</param>
    public DVector(int size, double value)
    {
        values = GC.AllocateUninitializedArray<double>(size);
        Array.Fill(values, value);
    }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="random">A random number generator.</param>
    /// <param name="offset">An offset for the random numbers.</param>
    /// <param name="width">Width for the uniform distribution.</param>
    public DVector(int size, Random random, double offset, double width)
    {
        values = GC.AllocateUninitializedArray<double>(size);
        values.AsSpan().CreateRandom(random, offset, width);
    }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="random">A random number generator.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector(int size, Random random)
    {
        values = GC.AllocateUninitializedArray<double>(size);
        values.AsSpan().CreateRandom(random);
    }

    /// <summary>Creates a vector filled with a normal distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="random">A normal random number generator.</param>
    public DVector(int size, NormalRandom random)
    {
        values = GC.AllocateUninitializedArray<double>(size);
        values.AsSpan().CreateRandom(random);
    }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public DVector(int size, Func<int, double> f)
    {
        values = GC.AllocateUninitializedArray<double>(size);
        for (int i = 0; i < values.Length; i++)
            values[i] = f(i);
    }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public DVector(int size, Func<int, DVector, double> f)
    {
        values = new double[size];
        for (int i = 0; i < values.Length; i++)
            values[i] = f(i, this);
    }

    /// <summary>Creates a vector by concatenating a prefix vector with a new value.</summary>
    /// <param name="prefix">Values at the left.</param>
    /// <param name="newValue">New value at the right.</param>
    public DVector(DVector prefix, double newValue)
    {
        values = GC.AllocateUninitializedArray<double>(prefix.Length + 1);
        Array.Copy(prefix.values, values, prefix.Length);
        values[^1] = newValue;
    }

    /// <summary>Creates a vector by concatenating a new value with a suffix vector.</summary>
    /// <param name="suffix">Values at the right.</param>
    /// <param name="newValue">New value at the left.</param>
    public DVector(double newValue, DVector suffix)
    {
        values = GC.AllocateUninitializedArray<double>(suffix.Length + 1);
        values[0] = newValue;
        Array.Copy(suffix.values, 0, values, 1, suffix.Length);
    }

    /// <summary>Creates a vector by concatenating two vectors.</summary>
    /// <param name="v1">First vector.</param>
    /// <param name="v2">Second vector.</param>
    public DVector(DVector v1, DVector v2)
    {
        values = GC.AllocateUninitializedArray<double>(v1.Length + v2.Length);
        Array.Copy(v1.values, values, v1.Length);
        Array.Copy(v2.values, 0, values, v1.Length, v2.Length);
    }

    /// <summary>Creates a vector by concatenating many vectors.</summary>
    /// <param name="v">An array of vectors.</param>
    public DVector(params DVector[] v)
    {
        values = GC.AllocateUninitializedArray<double>(v.Sum(v => v.Length));
        int offset = 0;
        foreach (DVector vi in v)
        {
            Array.Copy(vi.values, 0, values, offset, vi.Length);
            offset += vi.Length;
        }
    }

    /// <summary>Creates an identical vector.</summary>
    /// <remarks>This operation does not share the internal storage.</remarks>
    /// <returns>A deep clone of the instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector Clone() => (double[])values.Clone();

    /// <summary>Implicit conversion from an array to a vector.</summary>
    /// <param name="values">An array.</param>
    /// <returns>A vector with the same components as the array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DVector(double[] values) => new(values);

    /// <summary>Explicit conversion from vector to array.</summary>
    /// <remarks>
    /// Use carefully: it returns the underlying component array.
    /// </remarks>
    /// <param name="v">The original vector.</param>
    /// <returns>The underlying component array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator double[](DVector v) => v.values;

    /// <summary>Gets the dimensions of the vector.</summary>
    public int Length => values.Length;

    /// <summary>Gets the Euclidean norm of this vector.</summary>
    /// <returns>The squared root of the dot product.</returns>
    public double Norm() => Math.Sqrt(Squared());

    /// <summary>Has the vector been properly initialized?</summary>
    /// <remarks>
    /// Since Vector is a struct, its default constructor doesn't
    /// initializes the underlying component array.
    /// </remarks>
    public bool IsInitialized => values != null;

    /// <summary>Gets or sets the component at a given index.</summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index.</returns>
    public double this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => values[index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => values[index] = value;
    }

    /// <summary>Gets or sets the component at a given index.</summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index.</returns>
    public double this[Index index]
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
    public double SafeThis(int index) =>
        (uint)index >= values.Length
        ? 0.0
        : Add(ref MM.GetArrayDataReference(values), index);

    /// <summary>Unsafe access to the vector's components, skipping bounds checking.</summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal double UnsafeThis(int index) =>
        Add(ref MM.GetArrayDataReference(values), index);

    /// <summary>Gets the first value in the vector.</summary>
    public double First => values[0];
    /// <summary>Gets the last value in the vector.</summary>
    public double Last => values[^1];

    /// <summary>Copies the content of this vector into an existing one.</summary>
    /// <remarks>This operation does not share the internal storage.</remarks>
    /// <param name="dest">The destination vector.</param>
    internal void CopyTo(DVector dest)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(dest.IsInitialized);
        Contract.Requires(Length == dest.Length);
        Array.Copy(values, dest.values, Length);
    }

    /// <summary>Extracts a slice from the vector.</summary>
    /// <param name="range">The range to extract.</param>
    /// <returns>A new copy of the requested data.</returns>
    public DVector this[Range range]
    {
        get
        {
            (int offset, int length) = range.GetOffsetAndLength(values.Length);
            return values[offset..(offset + length)];
        }
    }

    /// <summary>Gets the cell with the maximum absolute value.</summary>
    /// <returns>The max-norm of the vector.</returns>
    public double AMax()
    {
        Contract.Requires(IsInitialized);
        return values.AsSpan().AMax();
    }

    /// <summary>Gets the cell with the minimum absolute value.</summary>
    /// <returns>The minimum absolute of the vector.</returns>
    public double AMin()
    {
        Contract.Requires(IsInitialized);
        return values.AsSpan().AMin();
    }

    /// <summary>Gets the item with the maximum value.</summary>
    /// <returns>The item with the maximum value.</returns>
    public double Maximum()
    {
        Contract.Requires(IsInitialized);
        return values.AsSpan().Max();
    }

    /// <summary>Gets the item with the minimum value.</summary>
    /// <returns>The item with the minimum value.</returns>
    public double Minimum()
    {
        Contract.Requires(IsInitialized);
        return values.AsSpan().Min();
    }

    /// <summary>Adds two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The component by component sum.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public static DVector operator +(DVector v1, DVector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        double[] result = GC.AllocateUninitializedArray<double>(v1.Length);
        v1.values.AsSpan().Add(v2.values, result);
        return result;
    }

    /// <summary>Inplace addition of two vectors.</summary>
    /// <param name="v">Second vector operand.</param>
    /// <returns>The component by component sum.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector InplaceAdd(DVector v)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        if (Length != v.Length)
            throw new VectorLengthException();
        values.AsSpan().Add(v.values);
        return this;
    }

    /// <summary>Subtracts two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The component by component subtraction.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DVector operator -(DVector v1, DVector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        double[] result = GC.AllocateUninitializedArray<double>(v1.Length);
        v1.values.AsSpan().Sub(v2.values, result);
        return result;
    }

    /// <summary>Inplace substraction of two vectors.</summary>
    /// <param name="v">Subtrahend.</param>
    /// <returns>The component by component subtraction.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector InplaceSub(DVector v)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        if (Length != v.Length)
            throw new VectorLengthException();
        values.AsSpan().Sub(v.values);
        return this;
    }

    /// <summary>Negates a vector.</summary>
    /// <param name="v">The vector operand.</param>
    /// <returns>The component by component negation.</returns>
    public static DVector operator -(DVector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == v.Length);
        double[] result = GC.AllocateUninitializedArray<double>(v.values.Length);
        v.values.AsSpan().Neg(result);
        return result;
    }

    /// <summary>Inplace negation of the vector.</summary>
    /// <returns>The same vector instance, with items negated.</returns>
    public DVector InplaceNegate()
    {
        values.AsSpan().Neg();
        return this;
    }

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="v">A vector summand.</param>
    /// <param name="d">A scalar summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    public static DVector operator +(DVector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == v.Length);
        double[] result = GC.AllocateUninitializedArray<double>(v.Length);
        v.values.AsSpan().Add(d, result);
        return result;
    }

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="d">A scalar summand.</param>
    /// <param name="v">A vector summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DVector operator +(double d, DVector v) => v + d;

    /// <summary>Subtracts a scalar from a vector.</summary>
    /// <param name="v">The vector operand.</param>
    /// <param name="d">The scalar operand.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static DVector operator -(DVector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == v.Length);
        double[] result = GC.AllocateUninitializedArray<double>(v.Length);
        v.values.AsSpan().Sub(d, result);
        return result;
    }

    /// <summary>Subtracts a vector from a scalar.</summary>
    /// <param name="d">The scalar operand.</param>
    /// <param name="v">The vector operand.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static DVector operator -(double d, DVector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == v.Length);
        double[] result = GC.AllocateUninitializedArray<double>(v.Length);
        Vec.Sub(d, v.values, result);
        return result;
    }

    /// <summary>Pointwise multiplication.</summary>
    /// <param name="other">Second vector operand.</param>
    /// <returns>The component by component product.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public DVector PointwiseMultiply(DVector other)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(other.IsInitialized);
        if (Length != other.Length)
            throw new VectorLengthException();
        Contract.Ensures(Contract.Result<DVector>().Length == Length);
        return values.AsSpan().Mul(other.values);
    }

    /// <summary>Pointwise division.</summary>
    /// <param name="other">Second vector operand.</param>
    /// <returns>The component by component quotient.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public DVector PointwiseDivide(DVector other)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(other.IsInitialized);
        if (Length != other.Length)
            throw new VectorLengthException();
        Contract.Ensures(Contract.Result<DVector>().Length == Length);
        return values.AsSpan().Div(other.values);
    }

    /// <summary>Dot product of two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The dot product of the operands.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public static double operator *(DVector v1, DVector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        Contract.EndContractBlock();
        return v1.values.AsSpan().Dot(v2.values);
    }

    /// <summary>Gets the squared norm of this vector.</summary>
    /// <remarks>
    /// It avoids duplicated memory accesses, and it is faster than the normal dot product.
    /// It also skips the runtime check for equal lengths.
    /// </remarks>
    /// <returns>The dot product with itself.</returns>
    public double Squared()
    {
        Contract.Requires(IsInitialized);

        double sum = 0d;
        ref double p = ref MM.GetArrayDataReference(values);
        ref double q = ref Add(ref p, values.Length);
        if (V8.IsHardwareAccelerated && Length > V8d.Count)
        {
            ref double last = ref Add(ref p, values.Length & Simd.MASK8);
            V8d acc = V8d.Zero;
            do
            {
                V8d v = V8.LoadUnsafe(ref p);
                acc = Avx512F.FusedMultiplyAdd(v, v, acc);
                p = ref Add(ref p, V8d.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            sum = V8.Sum(acc);
        }
        else if (V4.IsHardwareAccelerated && Length > V4d.Count)
        {
            ref double last = ref Add(ref p, values.Length & Simd.MASK4);
            V4d acc = V4d.Zero;
            do
            {
                V4d v = V4.LoadUnsafe(ref p);
                acc = acc.MultiplyAdd(v, v);
                p = ref Add(ref p, V4d.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            sum = V4.Sum(acc);
        }
        for (; IsAddressLessThan(ref p, ref q); p = ref Add(ref p, 1))
            sum = FusedMultiplyAdd(p, p, sum);
        return sum;
    }

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="v">Vector to be multiplied.</param>
    /// <param name="d">A scalar multiplier.</param>
    /// <returns>The multiplication of the vector by the scalar.</returns>
    public static DVector operator *(DVector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == v.Length);
        double[] result = GC.AllocateUninitializedArray<double>(v.values.Length);
        v.values.AsSpan().Mul(d, result);
        return result;
    }

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="d">A scalar multiplicand.</param>
    /// <param name="v">Vector to be multiplied.</param>
    /// <returns>The multiplication of the vector by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DVector operator *(double d, DVector v) => v * d;

    /// <summary>Divides a vector by a scalar value.</summary>
    /// <param name="v">Vector to be divided.</param>
    /// <param name="d">A scalar divisor.</param>
    /// <returns>The division of the vector by the scalar.</returns>
    public static DVector operator /(DVector v, double d) => v * (1.0 / d);

    /// <summary>The external product of two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>A matrix such that a[i, j] = v1[i] * v2[j].</returns>
    public static Matrix operator ^(DVector v1, DVector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == v1.Length);
        Contract.Ensures(Contract.Result<Matrix>().Cols == v2.Length);

        int rows = v1.Length, cols = v2.Length;
        double[] result = GC.AllocateUninitializedArray<double>(rows * cols);
        ref double r = ref MM.GetArrayDataReference(result);
        foreach (double d in v1.values)
        {
            v2.values.AsSpan().Mul(d, MM.CreateSpan(ref r, cols));
            r = ref Add(ref r, cols);
        }
        return new(rows, cols, result);
    }

    /// <summary>Optimized vector multiplication and addition.</summary>
    /// <remarks>The current vector is the multiplicand.</remarks>
    /// <param name="multiplier">The multiplier vector.</param>
    /// <param name="summand">The vector to be added to the pointwise multiplication.</param>
    /// <returns><code>this .* multiplier + summand</code></returns>
    public DVector MultiplyAdd(DVector multiplier, DVector summand)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(multiplier.IsInitialized);
        Contract.Requires(summand.IsInitialized);
        Contract.Requires(Length == multiplier.Length);
        Contract.Requires(Length == summand.Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MM.GetArrayDataReference(values);
        ref double q = ref MM.GetArrayDataReference(multiplier.values);
        ref double r = ref MM.GetArrayDataReference(summand.values);
        ref double s = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= V8d.Count)
        {
            nuint t = (nuint)(result.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                    V8.LoadUnsafe(ref p, i), V8.LoadUnsafe(ref q, i), V8.LoadUnsafe(ref r, i)),
                    ref s, i);
            V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                V8.LoadUnsafe(ref p, t), V8.LoadUnsafe(ref q, t), V8.LoadUnsafe(ref r, t)),
                ref s, t);
        }
        else if (V4.IsHardwareAccelerated && result.Length >= V4d.Count)
        {
            nuint t = (nuint)(result.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref r, i).MultiplyAdd(
                    V4.LoadUnsafe(ref p, i), V4.LoadUnsafe(ref q, i)), ref s, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref r, t).MultiplyAdd(
                V4.LoadUnsafe(ref p, t), V4.LoadUnsafe(ref q, t)), ref s, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref s, i) = FusedMultiplyAdd(Add(ref p, i), Add(ref q, i), Add(ref r, i));
        return result;
    }

    /// <summary>Optimized vector multiplication and addition.</summary>
    /// <remarks>The current vector is the multiplicand.</remarks>
    /// <param name="multiplier">The multiplier scalar.</param>
    /// <param name="summand">The vector to be added to the scalar multiplication.</param>
    /// <returns><code>this * multiplier + summand</code></returns>
    public DVector MultiplyAdd(double multiplier, DVector summand)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(summand.IsInitialized);
        Contract.Requires(Length == summand.Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MM.GetArrayDataReference(values);
        ref double r = ref MM.GetArrayDataReference(summand.values);
        ref double s = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= V8d.Count)
        {
            V8d vq = V8.Create(multiplier);
            nuint t = (nuint)(result.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                    V8.LoadUnsafe(ref p, i), vq, V8.LoadUnsafe(ref r, i)), ref s, i);
            V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                V8.LoadUnsafe(ref p, t), vq, V8.LoadUnsafe(ref r, t)), ref s, t);
        }
        else if (V4.IsHardwareAccelerated && Fma.IsSupported && result.Length >= V4d.Count)
        {
            V4d vq = V4.Create(multiplier);
            nuint t = (nuint)(result.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(Fma.MultiplyAdd(
                    V4.LoadUnsafe(ref p, i), vq, V4.LoadUnsafe(ref r, i)), ref s, i);
            V4.StoreUnsafe(Fma.MultiplyAdd(
                V4.LoadUnsafe(ref p, t), vq, V4.LoadUnsafe(ref r, t)), ref s, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref s, i) = FusedMultiplyAdd(Add(ref p, i), multiplier, Add(ref r, i));
        return result;
    }

    /// <summary>Optimized vector multiplication and subtraction.</summary>
    /// <remarks>
    /// <para>The current vector is the multiplicand.</para>
    /// <para>This operation is hardware-accelerated when possible.</para>
    /// </remarks>
    /// <param name="multiplier">The multiplier vector.</param>
    /// <param name="subtrahend">The vector to be subtracted from the multiplication.</param>
    /// <returns><code>this .* multiplier - subtrahend</code></returns>
    public DVector MultiplySubtract(DVector multiplier, DVector subtrahend)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(multiplier.IsInitialized);
        Contract.Requires(subtrahend.IsInitialized);
        Contract.Requires(Length == multiplier.Length);
        Contract.Requires(Length == subtrahend.Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MM.GetArrayDataReference(values);
        ref double q = ref MM.GetArrayDataReference(multiplier.values);
        ref double r = ref MM.GetArrayDataReference(subtrahend.values);
        ref double s = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= V8d.Count)
        {
            nuint t = (nuint)(result.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(Avx512F.FusedMultiplySubtract(
                    V8.LoadUnsafe(ref p, i), V8.LoadUnsafe(ref q, i), V8.LoadUnsafe(ref r, i)),
                    ref s, i);
            V8.StoreUnsafe(Avx512F.FusedMultiplySubtract(
                V8.LoadUnsafe(ref p, t), V8.LoadUnsafe(ref q, t), V8.LoadUnsafe(ref r, t)),
                ref s, t);
        }
        else if (V4.IsHardwareAccelerated && result.Length >= V4d.Count)
        {
            nuint t = (nuint)(result.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref r, i).MultiplySub(
                        V4.LoadUnsafe(ref p, i), V4.LoadUnsafe(ref q, i)), ref s, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref r, t).MultiplySub(
                    V4.LoadUnsafe(ref p, t), V4.LoadUnsafe(ref q, t)), ref s, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref s, i) = Add(ref p, i) * Add(ref q, i) - Add(ref r, i);
        return result;
    }

    /// <summary>Optimized vector scaling and subtraction.</summary>
    /// <remarks>
    /// <para>The current vector is the multiplicand.</para>
    /// <para>This operation is hardware-accelerated when possible.</para>
    /// </remarks>
    /// <param name="multiplier">The multiplier scalar.</param>
    /// <param name="subtrahend">The vector to be subtracted from the multiplication.</param>
    /// <returns><code>this * multiplier - subtrahend</code></returns>
    public DVector MultiplySubtract(double multiplier, DVector subtrahend)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(subtrahend.IsInitialized);
        Contract.Requires(Length == subtrahend.Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MM.GetArrayDataReference(values);
        ref double r = ref MM.GetArrayDataReference(subtrahend.values);
        ref double s = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= V8d.Count)
        {
            V8d vq = V8.Create(multiplier);
            nuint t = (nuint)(result.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(Avx512F.FusedMultiplySubtract(
                    V8.LoadUnsafe(ref p, i), vq, V8.LoadUnsafe(ref r, i)), ref s, i);
            V8.StoreUnsafe(Avx512F.FusedMultiplySubtract(
                V8.LoadUnsafe(ref p, t), vq, V8.LoadUnsafe(ref r, t)), ref s, t);
        }
        else if (V4.IsHardwareAccelerated && Fma.IsSupported && result.Length >= V4d.Count)
        {
            V4d vq = V4.Create(multiplier);
            nuint t = (nuint)(result.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(Fma.MultiplySubtract(
                    V4.LoadUnsafe(ref p, i), vq, V4.LoadUnsafe(ref r, i)), ref s, i);
            V4.StoreUnsafe(Fma.MultiplySubtract(
                V4.LoadUnsafe(ref p, t), vq, V4.LoadUnsafe(ref r, t)), ref s, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref s, i) = Add(ref p, i) * multiplier - Add(ref r, i);
        return result;
    }

    /// <summary>Optimized subtraction of scaled vector.</summary>
    /// <remarks>
    /// <para>The current vector is the minuend.</para>
    /// <para>This operation is hardware-accelerated when possible.</para>
    /// </remarks>
    /// <param name="multiplier">The multiplier scalar.</param>
    /// <param name="subtrahend">The vector to scaled.</param>
    /// <returns><code>this - multiplier * subtrahend</code></returns>
    public DVector SubtractMultiply(double multiplier, DVector subtrahend)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(subtrahend.IsInitialized);
        Contract.Requires(Length == subtrahend.Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MM.GetArrayDataReference(values);
        ref double r = ref MM.GetArrayDataReference(subtrahend.values);
        ref double s = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= V8d.Count)
        {
            V8d vq = V8.Create(multiplier);
            nuint t = (nuint)(result.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(Avx512F.FusedMultiplyAddNegated(
                    V8.LoadUnsafe(ref r, i), vq, V8.LoadUnsafe(ref p, i)), ref s, i);
            V8.StoreUnsafe(Avx512F.FusedMultiplyAddNegated(
                V8.LoadUnsafe(ref r, t), vq, V8.LoadUnsafe(ref p, t)), ref s, t);
        }
        else if (V4.IsHardwareAccelerated && Fma.IsSupported && result.Length >= V4d.Count)
        {
            V4d vq = V4.Create(multiplier);
            nuint t = (nuint)(result.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(Fma.MultiplyAddNegated(
                    V4.LoadUnsafe(ref r, i), vq, V4.LoadUnsafe(ref p, i)), ref s, i);
            V4.StoreUnsafe(Fma.MultiplyAddNegated(
                V4.LoadUnsafe(ref r, t), vq, V4.LoadUnsafe(ref p, t)), ref s, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref s, i) = Add(ref p, i) - multiplier * Add(ref r, i);
        return result;
    }

    /// <summary>Low-level method to linearly combine vectors with weights.</summary>
    /// <param name="weights">The weights for each vector.</param>
    /// <param name="vectors">Vectors to be linearly combined.</param>
    /// <returns>The linear combination of vectors.</returns>
    /// <remarks>
    /// When <paramref name="weights"/> has one more item than <paramref name="vectors"/>,
    /// that first item is used as the constant term.
    /// </remarks>
    public static DVector Combine(DVector weights, params DVector[] vectors)
    {
        if (weights.Length == 0 ||
            weights.Length != vectors.Length &&
            weights.Length != vectors.Length + 1)
            throw new ArgumentException("Weights and vectors don't match");
        int size = vectors[0].Length;
        double[] values = new double[size];
        ref double p = ref MM.GetArrayDataReference(values);
        int firstW = weights.Length == vectors.Length ? 0 : 1;
        if (firstW > 0)
            Array.Fill(values, weights.UnsafeThis(0));
        nuint t = V8.IsHardwareAccelerated ? (nuint)(size - V8d.Count) : (nuint)(size - V4d.Count);
        for (int i = 0; i < vectors.Length; i++)
        {
            if (vectors[i].Length != size)
                throw new VectorLengthException();
            ref double q = ref MM.GetArrayDataReference(vectors[i].values);
            double w = weights.UnsafeThis(firstW + i);
            if (V8.IsHardwareAccelerated && size >= V8d.Count)
            {
                V8d vec = V8.Create(w);
                for (nuint j = 0; j < t; j += (nuint)V8d.Count)
                    V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                        V8.LoadUnsafe(ref q, j), vec, V8.LoadUnsafe(ref p, j)), ref p, j);
                V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                    V8.LoadUnsafe(ref q, t), vec, V8.LoadUnsafe(ref p, t)), ref p, t);
            }
            else if (V4.IsHardwareAccelerated && Fma.IsSupported && size >= V4d.Count)
            {
                V4d vec = V4.Create(w);
                for (nuint j = 0; j < t; j += (nuint)V4d.Count)
                    V4.StoreUnsafe(Fma.MultiplyAdd(
                        V4.LoadUnsafe(ref q, j), vec, V4.LoadUnsafe(ref p, j)), ref p, j);
                V4.StoreUnsafe(Fma.MultiplyAdd(
                    V4.LoadUnsafe(ref q, t), vec, V4.LoadUnsafe(ref p, t)), ref p, t);
            }
            else
                for (int j = 0; j < size; j++)
                    Add(ref p, j) = FusedMultiplyAdd(Add(ref q, j), w, Add(ref p, j));
        }
        return values;
    }

    /// <summary>Low-level method to linearly combine two vectors with weights.</summary>
    /// <remarks>
    /// This method is a frequent special case of the more general
    /// <see cref="Combine(DVector, DVector[])"/>.
    /// </remarks>
    /// <param name="w1">Weight for the first vector.</param>
    /// <param name="w2">Weight for the second vector.</param>
    /// <param name="v1">First vector in the linear combination.</param>
    /// <param name="v2">Second vector in the linear combination.</param>
    /// <returns>Returns the linear combination <c>w1 * v1 + w2 * v2</c>.</returns>
    public static DVector Combine2(double w1, double w2, DVector v1, DVector v2)
    {
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        double[] values = GC.AllocateUninitializedArray<double>(v1.Length);
        ref double a = ref MM.GetArrayDataReference(v1.values);
        ref double b = ref MM.GetArrayDataReference(v2.values);
        ref double c = ref MM.GetArrayDataReference(values);
        if (V8.IsHardwareAccelerated && values.Length >= V8d.Count)
        {
            V8d vw1 = V8.Create(w1), vw2 = V8.Create(w2);
            nuint t = (nuint)(values.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                    V8.LoadUnsafe(ref a, i), vw1, V8.LoadUnsafe(ref b, i) * vw2), ref c, i);
            V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                V8.LoadUnsafe(ref a, t), vw1, V8.LoadUnsafe(ref b, t) * vw2), ref c, t);
        }
        else if (V4.IsHardwareAccelerated && Fma.IsSupported && values.Length >= V4d.Count)
        {
            V4d vw1 = V4.Create(w1), vw2 = V4.Create(w2);
            nuint t = (nuint)(values.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(Fma.MultiplyAdd(
                    V4.LoadUnsafe(ref a, i), vw1, V4.LoadUnsafe(ref b, i) * vw2), ref c, i);
            V4.StoreUnsafe(Fma.MultiplyAdd(
                V4.LoadUnsafe(ref a, t), vw1, V4.LoadUnsafe(ref b, t) * vw2), ref c, t);
        }
        else
            for (int i = 0; i < values.Length; i++)
                Add(ref c, i) = FusedMultiplyAdd(Add(ref b, i), w2, Add(ref a, i) * w1);
        return values;
    }

    /// <summary>Gets statistics on the vector values.</summary>
    /// <returns>The vector's statistics.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Accumulator Stats() => new(values);

    /// <summary>Computes the maximum difference between cells.</summary>
    /// <param name="v">The reference vector.</param>
    /// <returns>The max-norm of the vector difference.</returns>
    public double Distance(DVector v)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        return values.Distance(v.values);
    }

    /// <summary>Pointwise squared root.</summary>
    /// <returns>A new vector with the square root of the original items.</returns>
    public DVector Sqrt()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MM.GetArrayDataReference(values);
        ref double q = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= V8d.Count)
        {
            nuint t = (nuint)(result.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(Avx512F.Sqrt(V8.LoadUnsafe(ref p, i)), ref q, i);
            V8.StoreUnsafe(Avx512F.Sqrt(V8.LoadUnsafe(ref p, t)), ref q, t);
        }
        else if (V4.IsHardwareAccelerated && result.Length >= V4d.Count)
        {
            nuint t = (nuint)(result.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.Sqrt(V4.LoadUnsafe(ref p, i)), ref q, i);
            V4.StoreUnsafe(V4.Sqrt(V4.LoadUnsafe(ref p, t)), ref q, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref q, i) = Math.Sqrt(Add(ref p, i));
        return result;
    }

    /// <summary>Gets the absolute values of the vector's items.</summary>
    /// <returns>A new vector with non-negative items.</returns>
    public DVector Abs()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == Length);
        return values.Abs();
    }

    /// <summary>Checks whether the predicate is satisfied by all items.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if all items satisfy the predicate.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool All(Func<double, bool> predicate) => values.AsSpan().All(predicate);

    /// <summary>Checks whether the predicate is satisfied by at least one item.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if there exists a item satisfying the predicate.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Any(Func<double, bool> predicate) => values.AsSpan().Any(predicate);

    /// <summary>Checks if the vector contains the given value.</summary>
    /// <param name="value">Value to locate.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(double value) => IndexOf(value) != -1;

    /// <summary>Returns a new vector with the distinct values in the original one.</summary>
    /// <remarks>Results are unordered.</remarks>
    /// <returns>A new vector with distinct values.</returns>
    public DVector Distinct() => values.AsSpan().Distinct();

    /// <summary>Returns all indexes containing ocurrences of a value.</summary>
    /// <param name="value">Value to find.</param>
    /// <returns>An integer sequences with all found indexes.</returns>
    public NSequence Find(double value) => NSequence.Iterate(values, value);

    /// <summary>Returns all indexes satisfying a condition.</summary>
    /// <param name="condition">The condition to be satisfied.</param>
    /// <returns>An integer sequences with all found indexes.</returns>
    public NSequence Find(Func<double, bool> condition) => NSequence.Iterate(values, condition);

    /// <summary>Creates a new vector by filtering items with the given predicate.</summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>A new vector with the filtered items.</returns>
    public DVector Filter(Func<double, bool> predicate) => values.Filter(predicate);

    /// <summary>Creates a new vector by filtering and mapping at the same time.</summary>
    /// <remarks>This method can save an intermediate buffer and one iteration.</remarks>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new vector with the filtered items.</returns>
    public DVector FilterMap(Func<double, bool> predicate, Func<double, double> mapper) =>
        values.FilterMap(predicate, mapper);

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(double value) => IndexOf(value, 0);

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <param name="from">The zero-based starting index.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    public int IndexOf(double value, int from)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(from >= 0 && from < Length);
        Contract.Ensures(Contract.Result<int>() >= -1 && Contract.Result<int>() < Length);

        int result = Vec.IndexOf(new ReadOnlySpan<double>(values, from, Length - from), value);
        return result >= 0 ? result + from : -1;
    }

    /// <summary>
    /// Creates a new vector by transforming each item with the given function.
    /// </summary>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new vector with the transformed content.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector Map(Func<double, double> mapper) => values.Map(mapper);

    /// <summary>Computes the mean of the vector's items.</summary>
    /// <returns><code>this.Sum() / this.Length</code></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Mean() => Sum() / Length;

    /// <summary>Calculates the product of the vector's items.</summary>
    /// <returns>The product of all vector's items.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Product()
    {
        Contract.Requires(IsInitialized);
        return values.Product();
    }

    /// <summary>Creates an aggregate value by applying the reducer to each item.</summary>
    /// <param name="seed">The initial value.</param>
    /// <param name="reducer">The reducing function.</param>
    /// <returns>The final synthesized value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Reduce(double seed, Func<double, double, double> reducer) =>
        values.AsSpan().Reduce(seed, reducer);

    /// <summary>Creates a reversed copy of the vector.</summary>
    /// <returns>An independent reversed copy.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector Reverse() => values.Reverse();

    /// <summary>Returns a new vector with sorted values.</summary>
    /// <returns>A new vector with sorted values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector Sort() => values.Sort();

    /// <summary>Returns a new vector with sorted values.</summary>
    /// <returns>A new vector with sorted values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector SortDescending() => values.SortDescending();

    /// <summary>Calculates the sum of the vector's items.</summary>
    /// <returns>The sum of all vector's items.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Sum()
    {
        Contract.Requires(IsInitialized);
        return values.Sum();
    }

    /// <summary>Combines the common prefix of two vectors.</summary>
    /// <param name="other">Second vector to combine.</param>
    /// <param name="zipper">The combining function.</param>
    /// <returns>The combining function applied to each pair of items.</returns>
    public DVector Zip(DVector other, Func<double, double, double> zipper) =>
        values.AsSpan().Zip(other.values, zipper);

    /// <summary>Computes the autocorrelation for a fixed lag.</summary>
    /// <param name="lag">Lag number in samples.</param>
    /// <param name="average">Estimated average for vector items.</param>
    /// <returns>The autocorrelation factor.</returns>
    internal double AutoCorrelation(int lag, double average)
    {
        double ex = 0, ey = 0, exy = 0, exx = 0, eyy = 0;
        ref double p = ref MM.GetArrayDataReference(values);
        ref double q = ref Add(ref p, lag);
        nuint i = 0, count = (nuint)(Length - lag);
        if (Avx512F.IsSupported)
        {
            V8d avg = V8.Create(average);
            V8d vex = V8d.Zero, vey = V8d.Zero;
            V8d vexx = V8d.Zero, vexy = V8d.Zero, veyy = V8d.Zero;
            for (nuint top = count & Simd.MASK8; i < top; i += (nuint)V8d.Count)
            {
                V8d x = V8.LoadUnsafe(ref p, i) - avg, y = V8.LoadUnsafe(ref q, i) - avg;
                vex += x; vey += y;
                vexx = Avx512F.FusedMultiplyAdd(x, x, vexx);
                vexy = Avx512F.FusedMultiplyAdd(x, y, vexy);
                veyy = Avx512F.FusedMultiplyAdd(y, y, veyy);
            }
            ex = V8.Sum(vex); ey = V8.Sum(vey);
            exx = V8.Sum(vexx); exy = V8.Sum(vexy); eyy = V8.Sum(veyy);
        }
        else if (Avx.IsSupported)
        {
            V4d avg = V4.Create(average);
            V4d vex = V4d.Zero, vey = V4d.Zero;
            V4d vexx = V4d.Zero, vexy = V4d.Zero, veyy = V4d.Zero;
            for (nuint top = count & Simd.MASK4; i < top; i += (nuint)V4d.Count)
            {
                V4d x = V4.LoadUnsafe(ref p, i) - avg, y = V4.LoadUnsafe(ref q, i) - avg;
                vex += x; vey += y;
                vexx = vexx.MultiplyAdd(x, x);
                vexy = vexy.MultiplyAdd(x, y);
                veyy = veyy.MultiplyAdd(y, y);
            }
            ex = V4.Sum(vex); ey = V4.Sum(vey);
            exx = V4.Sum(vexx); exy = V4.Sum(vexy); eyy = V4.Sum(veyy);
        }
        for (; i < count; i++)
        {
            double x = Add(ref p, i) - average, y = Add(ref q, i) - average;
            ex += x; ey += y;
            exx = FusedMultiplyAdd(x, x, exx);
            exy = FusedMultiplyAdd(x, y, exy);
            eyy = FusedMultiplyAdd(y, y, eyy);
        }
        return (exy - ex * ey / count) /
            Math.Sqrt((exx - ex * ex / count) * (eyy - ey * ey / count));
    }

    /// <summary>Computes the autocorrelation for a fixed lag.</summary>
    /// <param name="lag">Lag number in samples.</param>
    /// <returns>The autocorrelation factor.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double AutoCorrelation(int lag) => AutoCorrelation(lag, Mean());

    /// <summary>Computes autocorrelation for a range of lags.</summary>
    /// <param name="size">Number of lags to compute.</param>
    /// <returns>All the autocorrelations in an array.</returns>
    internal double[] CorrelogramRaw(int size)
    {
        if (size < 1)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be positive");
        if (size >= Length - 1)
            throw new ArgumentOutOfRangeException(nameof(size), "Size too large");
        double mean = Mean();
        // Avoid cyclic autocorrelation by padding with enough zeros.
        uint count = BitOperations.RoundUpToPowerOf2((uint)Length) * 2;
        Complex[] fft = new Complex[count];
        for (int i = 0; i < values.Length; i++)
            fft[i] = values[i] - mean;
        FFT.Transform(fft);
        for (int i = 0; i < fft.Length; i++)
        {
            (double re, double im) = fft[i];
            fft[i] = re * re + im * im;
        }
        FFT.Inverse(fft);
        double dc = fft[0].Real;
        double[] newValues = GC.AllocateUninitializedArray<double>(size);
        for (int i = 0; i < newValues.Length; i++)
            newValues[i] = fft[i].Real / dc;
        return newValues;
    }

    /// <summary>Computes autocorrelation for a range of lags.</summary>
    /// <param name="size">Number of lags to compute.</param>
    /// <returns>Pairs lags/autocorrelation.</returns>
    public Series<int> Correlogram(int size) =>
        Series.Create("CORR", null, CorrelogramRaw(size));

    /// <summary>Computes autocorrelation for all lags.</summary>
    /// <returns>Pairs lags/autocorrelation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Series<int> ACF() => Correlogram(Length - 2);

    /// <summary>Computes the partial autocorrelation for all lags.</summary>
    /// <remarks>Based on the Levinson-Durbin method.</remarks>
    /// <returns>All the partial autocorrelations in an array.</returns>
    public double[] PACFRaw()
    {
        double[] acf = CorrelogramRaw(Length - 2);
        double[] pacf = new double[acf.Length];
        double[] last = new double[acf.Length];
        double[] prev = new double[acf.Length];
        for (int k = 0; k < acf.Length; k++)
        {
            double num = acf[k], den = 1d;
            for (int j = 1; j < k; j++)
            {
                double p = prev[j];
                num -= p * acf[k - j];
                den -= p * acf[j];
            }
            pacf[k] = last[k] = num = den <= 0.0 ? 0.0 : num / den;
            for (int j = 0; j < k; j++)
                last[j] = prev[j] - num * prev[k - j];
            (last, prev) = (prev, last);
        }
        return pacf;
    }

    /// <summary>Computes the partial autocorrelation for all lags.</summary>
    /// <returns>Pairs lags/partial autocorrelation.</returns>
    public Series<int> PACF() => Series.Create("PACF", null, PACFRaw());

    /// <summary>Multilinear regression based in Ordinary Least Squares.</summary>
    /// <param name="predictors">Predicting series.</param>
    /// <returns>Regression coefficients.</returns>
    public DVector LinearModel(params DVector[] predictors)
    {
        int size = predictors[0].Length;
        if (predictors.Any(p => p.Length != size))
            throw new VectorLengthException();
        DVector[] rows = new DVector[predictors.Length + 1];
        rows[0] = new(size, 1.0);
        for (int i = 0; i < predictors.Length; i++)
            rows[i + 1] = predictors[i];
        Matrix x = new(rows);
        return x.MultiplyTranspose(x).Cholesky().Solve(x * this);
    }

    /// <summary>Creates a linear model a set of predictors.</summary>
    /// <param name="predictors">Vectors used to predict this one.</param>
    /// <returns>A full linear model.</returns>
    public LinearVModel FullLinearModel(params DVector[] predictors) =>
        new(this, predictors);

    /// <summary>Creates an AR model from a vector and a degree.</summary>
    /// <remarks>
    /// Coefficients are estimated using the Yule-Walker method.
    /// </remarks>
    /// <param name="degree">Number of independent variables in the model.</param>
    /// <returns>A full autoregressive model.</returns>
    public ARVModel ARModel(int degree) => new(this, degree);

    /// <summary>Creates an MA model from a vector and a degree.</summary>
    /// <remarks>
    /// Coefficients are estimated using iterated OLS.
    /// </remarks>
    /// <param name="degree">Number of independent variables in the model.</param>
    /// <returns>A full moving average model.</returns>
    public MAVModel MAModel(int degree) => new(this, degree);

    /// <summary>Finds the coefficients for an autoregressive model.</summary>
    /// <param name="degree">Number of coefficients in the model.</param>
    /// <returns>The coefficients of the AR(degree) model.</returns>
    public DVector AutoRegression(int degree) => AutoRegression(degree, out _, out _);

    /// <summary>Finds the coefficients for an autoregressive model.</summary>
    /// <param name="degree">Number of coefficients in the model.</param>
    /// <param name="matrix">The correlation matrix.</param>
    /// <param name="correlations">The correlations.</param>
    /// <returns>The coefficients of the AR(degree) model.</returns>
    internal unsafe DVector AutoRegression(int degree, out Matrix matrix, out DVector correlations)
    {
        if (degree <= 0)
            throw new ArgumentOutOfRangeException(nameof(degree), "Degree must be positive");
        if (Length - degree <= 1)
            throw new ArgumentOutOfRangeException(nameof(degree), "Degree too large");
        // Remove the mean, if not zero.
        double mean = Mean();
        double[][] m = new double[degree][];
        for (int i = 0; i < m.Length; i++)
            m[i] = new double[degree];
        int length = Length;
        double[] coeffs = new double[degree];
        fixed (double* c = coeffs)
        {
            DVector v = mean == 0 ? this : this - mean;
            fixed (double* p = v.values)
                for (int i = degree - 1; i < length - 1; i++)
                {
                    double vhi = p[i + 1];
                    for (int j = 0; j < degree; j++)
                    {
                        double vhj = p[i - j];
                        c[j] += vhi * vhj;
                        double[] mj = m[j];
                        for (int k = j; k < degree; k++)
                            mj[k] += vhj * p[i - k];
                    }
                }
            for (int i = 0; i < degree; i++)
            {
                c[i] /= (length - degree);
                double[] mi = m[i];
                for (int j = i; j < degree; j++)
                    m[j][i] = mi[j] /= length - degree;
            }
            matrix = new(degree, degree, (i, j) => m[i][j]);
            correlations = new((double[])coeffs.Clone());
            SolveLE(m, c, degree);
        }
        return coeffs;

        static bool SolveLE(double[][] mat, double* vec, int n)
        {
            for (int i = 0; i < n - 1; i++)
            {
                double max = Math.Abs(mat[i][i]);
                int maxi = i;
                for (int j = i + 1; j < n; j++)
                {
                    double h = Math.Abs(mat[j][i]);
                    if (h > max)
                    {
                        max = h;
                        maxi = j;
                    }
                }
                if (maxi != i)
                {
                    (mat[maxi], mat[i]) = (mat[i], mat[maxi]);
                    (vec[maxi], vec[i]) = (vec[i], vec[maxi]);
                }

                double[] hvec = mat[i];
                double pivot = hvec[i];
                if (Math.Abs(pivot) == 0.0)
                    return false;
                for (int j = i + 1; j < n; j++)
                {
                    double[] matj = mat[j];
                    double q = -matj[i] / pivot;
                    matj[i] = 0.0;
                    for (int k = i + 1; k < n; k++)
                        matj[k] += q * hvec[k];
                    vec[j] += q * vec[i];
                }
            }
            vec[n - 1] /= mat[n - 1][n - 1];
            for (int i = n - 2; i >= 0; i--)
            {
                double[] hvec = mat[i];
                for (int j = n - 1; j > i; j--)
                    vec[i] -= hvec[j] * vec[j];
                vec[i] /= hvec[i];
            }
            return true;
        }
    }

    /// <summary>Finds the coefficients for a moving average model.</summary>
    /// <param name="degree">Number of coefficients in the model.</param>
    /// <returns>
    /// The coefficients of the MA(degree) model. The first coefficient is the constant term.
    /// </returns>
    public DVector MovingAverage(int degree) =>
        new MACalculator(degree, this).Run(128, 1e-9);

    /// <summary>Computes the real discrete Fourier transform.</summary>
    /// <returns>The spectrum.</returns>
    public FftRModel Fft() => new(FFT.Transform(values));

    /// <summary>Compares two vectors for equality within a tolerance.</summary>
    /// <param name="v1">First vector to compare.</param>
    /// <param name="v2">Second vector to compare.</param>
    /// <param name="epsilon">The tolerance.</param>
    /// <returns>True if each pair of components is inside the tolerance range.</returns>
    public static bool Equals(DVector v1, DVector v2, double epsilon)
    {
        if (v1.Length != v2.Length)
            return false;
        for (int i = 0; i < v1.Length; i++)
            if (Math.Abs(v1.UnsafeThis(i) - v2.UnsafeThis(i)) > epsilon)
                return false;
        return true;
    }

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <returns>Space-separated components.</returns>
    public override string ToString() =>
        $"ans ∊ ℝ({Length})" + Environment.NewLine +
        values.ToString(v => v.ToString("G6"));

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        $"ans ∊ ℝ({Length})" + Environment.NewLine +
        values.ToString(v => v.ToString(format, provider));

    /// <summary>Retrieves an enumerator to iterate over components.</summary>
    /// <returns>The enumerator from the underlying array.</returns>
    public IEnumerator<double> GetEnumerator() =>
        ((IEnumerable<double>)values).GetEnumerator();

    /// <summary>Retrieves an enumerator to iterate over components.</summary>
    /// <returns>The enumerator from the underlying array.</returns>
    IEnumerator IEnumerable.GetEnumerator() =>
        values.GetEnumerator();

    /// <summary>Checks if the provided argument is a vector with the same values.</summary>
    /// <param name="other">The vector to be compared.</param>
    /// <returns><see langword="true"/> if the vector argument has the same items.</returns>
    public bool Equals(DVector other) => values.Eqs(other.values);

    /// <summary>Checks if the provided argument is a vector with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a vector with the same items.</returns>
    public override bool Equals(object? obj) => obj is DVector vector && Equals(vector);

    /// <summary>Returns the hashcode for this vector.</summary>
    /// <returns>A hashcode summarizing the content of the vector.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)values).GetHashCode(EqualityComparer<double>.Default);

    /// <summary>Compares two vectors for equality. </summary>
    /// <param name="left">First vector operand.</param>
    /// <param name="right">Second vector operand.</param>
    /// <returns><see langword="true"/> if all corresponding items are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(DVector left, DVector right) => left.Equals(right);

    /// <summary>Compares two vectors for inequality. </summary>
    /// <param name="left">First vector operand.</param>
    /// <param name="right">Second vector operand.</param>
    /// <returns><see langword="true"/> if any pair of corresponding items are not equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(DVector left, DVector right) => !left.Equals(right);

    /// <summary>Creates a plot for this vector.</summary>
    /// <returns>A plot containing this vector as its dataset.</returns>
    public Plot<DVector> Plot() => new(this);

    internal (double total, double residuals, double r2) GetSumSquares(DVector other)
    {
        SimpleAccumulator acc = new();
        double res = 0;
        for (int i = 0; i < values.Length; i++)
        {
            double v = values[i];
            acc.Add(v);
            v -= other.values[i];
            res += v * v;
        }
        double total = acc.Variance * (Length - 1);
        return (total, res, (total - res) / total);
    }
}
