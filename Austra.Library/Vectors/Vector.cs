namespace Austra.Library;

using static Unsafe;
using Austra.Library.Stats;

/// <summary>Represents a dense vector of arbitrary size.</summary>
/// <remarks>
/// <see cref="Vector"/> provides a thin wrapper around a single array.
/// Most method operations are non destructive, and return a new vector.
/// </remarks>
public readonly struct Vector :
    IFormattable,
    IEnumerable<double>,
    IEquatable<Vector>,
    IEqualityOperators<Vector, Vector, bool>,
    IAdditionOperators<Vector, Vector, Vector>,
    IAdditionOperators<Vector, double, Vector>,
    ISubtractionOperators<Vector, Vector, Vector>,
    ISubtractionOperators<Vector, double, Vector>,
    IMultiplyOperators<Vector, Vector, double>,
    IMultiplyOperators<Vector, double, Vector>,
    IDivisionOperators<Vector, double, Vector>,
    IUnaryNegationOperators<Vector, Vector>,
    IPointwiseOperators<Vector>, ISafeIndexed, IVector
{
    /// <summary>Stores the components of the vector.</summary>
    private readonly double[] values;

    /// <summary>Creates a vector of a given size.</summary>
    /// <param name="size">Vector length.</param>
    public Vector(int size) => values = new double[size];

    /// <summary>Initializes a vector from an array.</summary>
    /// <param name="values">The components of the vector.</param>
    public Vector(double[] values) => this.values = values;

    /// <summary>Initializes a vector from a scalar.</summary>
    /// <param name="size">Vector length.</param>
    /// <param name="value">Scalar value to be repeated.</param>
    public Vector(int size, double value)
    {
        values = GC.AllocateUninitializedArray<double>(size);
        Array.Fill(values, value);
    }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="rnd">A random number generator.</param>
    /// <param name="offset">An offset for the random numbers.</param>
    /// <param name="width">Width for the uniform distribution.</param>
    public Vector(int size, Random rnd, double offset, double width)
    {
        values = GC.AllocateUninitializedArray<double>(size);
        for (int i = 0; i < values.Length; i++)
            values[i] = FusedMultiplyAdd(rnd.NextDouble(), width, offset);
    }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="rnd">A random number generator.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector(int size, Random rnd)
    {
        values = GC.AllocateUninitializedArray<double>(size);
        for (int i = 0; i < values.Length; i++)
            values[i] = rnd.NextDouble();
    }

    /// <summary>Creates a vector filled with a normal distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="rnd">A normal random number generator.</param>
    public Vector(int size, NormalRandom rnd)
    {
        values = GC.AllocateUninitializedArray<double>(size);
        ref double p = ref MemoryMarshal.GetArrayDataReference(values);
        int i = 0;
        for (int t = size & ~1; i < t; i += 2)
            rnd.NextDoubles(ref Add(ref p, i));
        if (i < size)
            Add(ref p, i) = rnd.NextDouble();
    }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public Vector(int size, Func<int, double> f)
    {
        values = GC.AllocateUninitializedArray<double>(size);
        for (int i = 0; i < values.Length; i++)
            values[i] = f(i);
    }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public Vector(int size, Func<int, Vector, double> f)
    {
        values = new double[size];
        for (int i = 0; i < values.Length; i++)
            values[i] = f(i, this);
    }

    /// <summary>Creates a vector by concatenating a prefix vector with a new value.</summary>
    /// <param name="prefix">Values at the left.</param>
    /// <param name="newValue">New value at the right.</param>
    public Vector(Vector prefix, double newValue)
    {
        values = GC.AllocateUninitializedArray<double>(prefix.Length + 1);
        Array.Copy(prefix.values, values, prefix.Length);
        values[^1] = newValue;
    }

    /// <summary>Creates a vector by concatenating a new value with a suffix vector.</summary>
    /// <param name="suffix">Values at the right.</param>
    /// <param name="newValue">New value at the left.</param>
    public Vector(double newValue, Vector suffix)
    {
        values = GC.AllocateUninitializedArray<double>(suffix.Length + 1);
        values[0] = newValue;
        Array.Copy(suffix.values, 0, values, 1, suffix.Length);
    }

    /// <summary>Creates a vector by concatenating two vectors.</summary>
    /// <param name="v1">First vector.</param>
    /// <param name="v2">Second vector.</param>
    public Vector(Vector v1, Vector v2)
    {
        values = GC.AllocateUninitializedArray<double>(v1.Length + v2.Length);
        Array.Copy(v1.values, values, v1.Length);
        Array.Copy(v2.values, 0, values, v1.Length, v2.Length);
    }

    /// <summary>Creates a vector by concatenating many vectors.</summary>
    /// <param name="v">An array of vectors.</param>
    public Vector(params Vector[] v)
    {
        values = GC.AllocateUninitializedArray<double>(v.Sum(v => v.Length));
        int offset = 0;
        foreach (Vector vi in v)
        {
            Array.Copy(vi.values, 0, values, offset, vi.Length);
            offset += vi.Length;
        }
    }

    /// <summary>Creates an identical vector.</summary>
    /// <remarks>This operation does not share the internal storage.</remarks>
    /// <returns>A deep clone of the instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Clone() => (double[])values.Clone();

    /// <summary>Implicit conversion from an array to a vector.</summary>
    /// <param name="values">An array.</param>
    /// <returns>A vector with the same components as the array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector(double[] values) => new(values);

    /// <summary>Explicit conversion from vector to array.</summary>
    /// <remarks>
    /// Use carefully: it returns the underlying component array.
    /// </remarks>
    /// <param name="v">The original vector.</param>
    /// <returns>The underlying component array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator double[](Vector v) => v.values;

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
        : Add(ref MemoryMarshal.GetArrayDataReference(values), index);

    /// <summary>Gets the first value in the vector.</summary>
    public double First => values[0];
    /// <summary>Gets the last value in the vector.</summary>
    public double Last => values[^1];

    /// <summary>Copies the content of this vector into an existing one.</summary>
    /// <remarks>This operation does not share the internal storage.</remarks>
    /// <param name="dest">The destination vector.</param>
    internal unsafe void CopyTo(Vector dest)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(dest.IsInitialized);
        Contract.Requires(Length == dest.Length);

        fixed (double* p = values, q = dest.values)
        {
            int size = values.Length * sizeof(double);
            Buffer.MemoryCopy(p, q, size, size);
        }
    }

    /// <summary>Extracts a slice from the vector.</summary>
    /// <param name="range">The range to extract.</param>
    /// <returns>A new copy of the requested data.</returns>
    public Vector this[Range range]
    {
        get
        {
            (int offset, int length) = range.GetOffsetAndLength(values.Length);
            return values[offset..(offset + length)];
        }
    }

    /// <summary>Gets the cell with the maximum absolute value.</summary>
    /// <returns>The max-norm of the vector.</returns>
    public unsafe double AMax()
    {
        Contract.Requires(IsInitialized);

        fixed (double* p = values)
            return CommonMatrix.AbsoluteMaximum(p, values.Length);
    }

    /// <summary>Gets the cell with the minimum absolute value.</summary>
    /// <returns>The minimum absolute of the vector.</returns>
    public unsafe double AMin()
    {
        Contract.Requires(IsInitialized);

        fixed (double* p = values)
            return CommonMatrix.AbsoluteMinimum(p, values.Length);
    }

    /// <summary>Gets the item with the maximum value.</summary>
    /// <returns>The item with the maximum value.</returns>
    public double Maximum()
    {
        Contract.Requires(IsInitialized);
        return CommonMatrix.Maximum(values);
    }

    /// <summary>Gets the item with the minimum value.</summary>
    /// <returns>The item with the minimum value.</returns>
    public double Minimum()
    {
        Contract.Requires(IsInitialized);
        return CommonMatrix.Minimum(values);
    }

    /// <summary>Adds two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The component by component sum.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public static Vector operator +(Vector v1, Vector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();

        double[] result = GC.AllocateUninitializedArray<double>(v1.Length);
        ref double a = ref MemoryMarshal.GetArrayDataReference(v1.values);
        ref double b = ref MemoryMarshal.GetArrayDataReference(v2.values);
        ref double c = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref a, i)) + Vector256.LoadUnsafe(ref Add(ref b, i)),
                    ref Add(ref c, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref a, t)) + Vector256.LoadUnsafe(ref Add(ref b, t)),
                ref Add(ref c, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref c, i) = Add(ref a, i) + Add(ref b, i);
        return result;
    }

    /// <summary>Subtracts two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The component by component subtraction.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator -(Vector v1, Vector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();

        double[] result = GC.AllocateUninitializedArray<double>(v1.Length);
        ref double a = ref MemoryMarshal.GetArrayDataReference(v1.values);
        ref double b = ref MemoryMarshal.GetArrayDataReference(v2.values);
        ref double c = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref a, i)) - Vector256.LoadUnsafe(ref Add(ref b, i)),
                    ref Add(ref c, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref a, t)) - Vector256.LoadUnsafe(ref Add(ref b, t)),
                ref Add(ref c, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref c, i) = Add(ref a, i) - Add(ref b, i);
        return result;
    }

    /// <summary>Negates a vector.</summary>
    /// <param name="v">The vector operand.</param>
    /// <returns>The component by component negation.</returns>
    public static Vector operator -(Vector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == v.Length);

        double[] result = GC.AllocateUninitializedArray<double>(v.Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(v.values);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    -Vector256.LoadUnsafe(ref Add(ref p, i)), ref Add(ref q, i));
            Vector256.StoreUnsafe(
                -Vector256.LoadUnsafe(ref Add(ref p, t)), ref Add(ref q, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref q, i) = -Add(ref p, i);
        return result;
    }

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="v">A vector summand.</param>
    /// <param name="d">A scalar summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    public static Vector operator +(Vector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == v.Length);

        double[] result = GC.AllocateUninitializedArray<double>(v.Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(v.values);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            Vector256<double> vec = Vector256.Create(d);
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref p, i)) + vec, ref Add(ref q, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref p, t)) + vec, ref Add(ref q, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref q, i) = Add(ref p, i) + d;
        return result;
    }

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="d">A scalar summand.</param>
    /// <param name="v">A vector summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator +(double d, Vector v) => v + d;

    /// <summary>Subtracts a scalar from a vector.</summary>
    /// <param name="v">The vector operand.</param>
    /// <param name="d">The scalar operand.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static Vector operator -(Vector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == v.Length);

        double[] result = GC.AllocateUninitializedArray<double>(v.Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(v.values);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            Vector256<double> vec = Vector256.Create(d);
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref p, i)) - vec, ref Add(ref q, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref p, t)) - vec, ref Add(ref q, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref q, i) = Add(ref p, i) - d;
        return result;
    }

    /// <summary>Subtracts a vector from a scalar.</summary>
    /// <param name="d">The scalar operand.</param>
    /// <param name="v">The vector operand.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static Vector operator -(double d, Vector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == v.Length);

        double[] result = GC.AllocateUninitializedArray<double>(v.Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(v.values);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            Vector256<double> vec = Vector256.Create(d);
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    vec - Vector256.LoadUnsafe(ref Add(ref p, i)), ref Add(ref q, i));
            Vector256.StoreUnsafe(
                vec - Vector256.LoadUnsafe(ref Add(ref p, t)), ref Add(ref q, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref q, i) = Add(ref p, i) - d;
        return result;
    }

    /// <summary>Pointwise multiplication.</summary>
    /// <param name="other">Second vector operand.</param>
    /// <returns>The component by component product.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public Vector PointwiseMultiply(Vector other)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(other.IsInitialized);
        if (Length != other.Length)
            throw new VectorLengthException();
        Contract.Ensures(Contract.Result<Vector>().Length == Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double a = ref MemoryMarshal.GetArrayDataReference(values);
        ref double b = ref MemoryMarshal.GetArrayDataReference(other.values);
        ref double c = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = values.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref a, i))
                        * Vector256.LoadUnsafe(ref Add(ref b, i)),
                    ref Add(ref c, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref a, t)) * Vector256.LoadUnsafe(ref Add(ref b, t)),
                ref Add(ref c, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref c, i) = Add(ref a, i) * Add(ref b, i);
        return result;
    }

    /// <summary>Pointwise division.</summary>
    /// <param name="other">Second vector operand.</param>
    /// <returns>The component by component quotient.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public Vector PointwiseDivide(Vector other)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(other.IsInitialized);
        if (Length != other.Length)
            throw new VectorLengthException();
        Contract.Ensures(Contract.Result<Vector>().Length == Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double a = ref MemoryMarshal.GetArrayDataReference(values);
        ref double b = ref MemoryMarshal.GetArrayDataReference(other.values);
        ref double c = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = values.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref a, i))
                        / Vector256.LoadUnsafe(ref Add(ref b, i)),
                    ref Add(ref c, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref a, t)) / Vector256.LoadUnsafe(ref Add(ref b, t)),
                ref Add(ref c, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref c, i) = Add(ref a, i) / Add(ref b, i);
        return result;
    }

    /// <summary>Dot product of two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The dot product of the operands.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    public static double operator *(Vector v1, Vector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        Contract.EndContractBlock();

        double sum = 0;
        int i = 0;
        ref double p = ref MemoryMarshal.GetArrayDataReference(v1.values);
        ref double q = ref MemoryMarshal.GetArrayDataReference(v2.values);
        if (Vector256.IsHardwareAccelerated)
        {
            Vector256<double> acc = Vector256<double>.Zero;
            for (int top = v1.values.Length & Simd.AVX_MASK; i < top; i += Vector256<double>.Count)
                acc = acc.MultiplyAdd(
                    Vector256.LoadUnsafe(ref Add(ref p, i)),
                    Vector256.LoadUnsafe(ref Add(ref q, i)));
            sum = acc.Sum();
        }
        for (; i < v1.values.Length; i++)
            sum = FusedMultiplyAdd(Add(ref p, i), Add(ref q, i), sum);
        return sum;
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
        ref double p = ref MemoryMarshal.GetArrayDataReference(values);
        ref double q = ref Add(ref p, values.Length);
        if (Vector256.IsHardwareAccelerated && Length > Vector256<double>.Count)
        {
            ref double last = ref Add(ref p, values.Length & Simd.AVX_MASK + 1);
            Vector256<double> acc = Vector256<double>.Zero;
            do
            {
                Vector256<double> v = Vector256.LoadUnsafe(ref p);
                acc = acc.MultiplyAdd(v, v);
                p = ref Add(ref p, Vector256<double>.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            sum = acc.Sum();
        }
        for (; IsAddressLessThan(ref p, ref q); p = ref Add(ref p, 1))
            sum = FusedMultiplyAdd(p, p, sum);
        return sum;
    }

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="v">Vector to be multiplied.</param>
    /// <param name="d">A scalar multiplier.</param>
    /// <returns>The multiplication of the vector by the scalar.</returns>
    public static Vector operator *(Vector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == v.Length);

        double[] result = GC.AllocateUninitializedArray<double>(v.Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(v.values);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        int len = v.values.Length;
        if (Vector256.IsHardwareAccelerated)
        {
            Vector256<double> vec = Vector256.Create(d);
            int t = len - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref p, i)) * vec,
                    ref Add(ref q, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref p, t)) * vec,
                ref Add(ref q, t));
        }
        else
            for (int i = 0; i < len; i++)
                Add(ref q, i) = Add(ref p, i) * d;
        return result;
    }

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="d">A scalar multiplicand.</param>
    /// <param name="v">Vector to be multiplied.</param>
    /// <returns>The multiplication of the vector by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator *(double d, Vector v) => v * d;

    /// <summary>Divides a vector by a scalar value.</summary>
    /// <param name="v">Vector to be divided.</param>
    /// <param name="d">A scalar divisor.</param>
    /// <returns>The division of the vector by the scalar.</returns>
    public static Vector operator /(Vector v, double d) => v * (1.0 / d);

    /// <summary>The external product of two vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>A matrix such that a[i, j] = v1[i] * v2[j].</returns>
    public static unsafe Matrix operator ^(Vector v1, Vector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == v1.Length);
        Contract.Ensures(Contract.Result<Matrix>().Cols == v2.Length);

        int rows = v1.Length, cols = v2.Length;
        double[] result = new double[rows * cols];
        fixed (double* pA = result, pV1 = v1.values, pV2 = v2.values)
            for (int i = 0, idx = 0; i < rows; i++)
            {
                double d = pV1[i];
                for (int j = 0; j < cols; j++)
                    pA[idx++] = pV2[j] * d;
            }
        return new(rows, cols, result);
    }

    /// <summary>Optimized vector multiplication and addition.</summary>
    /// <remarks>The current vector is the multiplicand.</remarks>
    /// <param name="multiplier">The multiplier vector.</param>
    /// <param name="summand">The vector to be added to the pointwise multiplication.</param>
    /// <returns><code>this .* multiplier + summand</code></returns>
    public Vector MultiplyAdd(Vector multiplier, Vector summand)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(multiplier.IsInitialized);
        Contract.Requires(summand.IsInitialized);
        Contract.Requires(Length == multiplier.Length);
        Contract.Requires(Length == summand.Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(values);
        ref double q = ref MemoryMarshal.GetArrayDataReference(multiplier.values);
        ref double r = ref MemoryMarshal.GetArrayDataReference(summand.values);
        ref double s = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref r, i)).MultiplyAdd(
                        Vector256.LoadUnsafe(ref Add(ref p, i)),
                        Vector256.LoadUnsafe(ref Add(ref q, i))),
                    ref Add(ref s, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref r, t)).MultiplyAdd(
                    Vector256.LoadUnsafe(ref Add(ref p, t)),
                    Vector256.LoadUnsafe(ref Add(ref q, t))),
                ref Add(ref s, t));
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
    public Vector MultiplyAdd(double multiplier, Vector summand)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(summand.IsInitialized);
        Contract.Requires(Length == summand.Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(values);
        ref double r = ref MemoryMarshal.GetArrayDataReference(summand.values);
        ref double s = ref MemoryMarshal.GetArrayDataReference(result);
        if (Fma.IsSupported)
        {
            Vector256<double> vq = Vector256.Create(multiplier);
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += 4)
                Vector256.StoreUnsafe(
                    Fma.MultiplyAdd(Vector256.LoadUnsafe(ref Add(ref p, i)), vq,
                        Vector256.LoadUnsafe(ref Add(ref r, i))),
                    ref Add(ref s, i));
            Vector256.StoreUnsafe(
                Fma.MultiplyAdd(Vector256.LoadUnsafe(ref Add(ref p, t)), vq,
                    Vector256.LoadUnsafe(ref Add(ref r, t))),
                ref Add(ref s, t));
        }
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
    public Vector MultiplySubtract(Vector multiplier, Vector subtrahend)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(multiplier.IsInitialized);
        Contract.Requires(subtrahend.IsInitialized);
        Contract.Requires(Length == multiplier.Length);
        Contract.Requires(Length == subtrahend.Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(values);
        ref double q = ref MemoryMarshal.GetArrayDataReference(multiplier.values);
        ref double r = ref MemoryMarshal.GetArrayDataReference(subtrahend.values);
        ref double s = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref r, i)).MultiplySub(
                        Vector256.LoadUnsafe(ref Add(ref p, i)),
                        Vector256.LoadUnsafe(ref Add(ref q, i))),
                    ref Add(ref s, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref r, t)).MultiplySub(
                    Vector256.LoadUnsafe(ref Add(ref p, t)),
                    Vector256.LoadUnsafe(ref Add(ref q, t))),
                ref Add(ref s, t));
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
    public Vector MultiplySubtract(double multiplier, Vector subtrahend)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(subtrahend.IsInitialized);
        Contract.Requires(Length == subtrahend.Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(values);
        ref double r = ref MemoryMarshal.GetArrayDataReference(subtrahend.values);
        ref double s = ref MemoryMarshal.GetArrayDataReference(result);
        if (Fma.IsSupported)
        {
            Vector256<double> vq = Vector256.Create(multiplier);
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += 4)
                Vector256.StoreUnsafe(
                    Fma.MultiplySubtract(Vector256.LoadUnsafe(ref Add(ref p, i)), vq,
                        Vector256.LoadUnsafe(ref Add(ref r, i))),
                    ref Add(ref s, i));
            Vector256.StoreUnsafe(
                Fma.MultiplySubtract(Vector256.LoadUnsafe(ref Add(ref p, t)), vq,
                    Vector256.LoadUnsafe(ref Add(ref r, t))),
                ref Add(ref s, t));
        }
        for (int i = 0; i < result.Length; i++)
            Add(ref s, i) = Add(ref p, i) * multiplier - Add(ref r, i);
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
    public static unsafe Vector Combine(Vector weights, Vector[] vectors)
    {
        if (weights.Length == 0 ||
            weights.Length != vectors.Length &&
            weights.Length != vectors.Length + 1)
            throw new ArgumentException("Weights and vectors don't match");
        int size = vectors[0].Length;
        double[] values = new double[size];
        fixed (double* p = values)
        {
            int firstW = weights.Length == vectors.Length ? 0 : 1;
            if (firstW > 0)
                Array.Fill(values, weights[0]);
            for (int i = 0; i < vectors.Length; i++)
            {
                if (vectors[i].Length != size)
                    throw new VectorLengthException();
                fixed (double* pa = vectors[i].values)
                {
                    int j = 0;
                    double w = weights[firstW + i];
                    if (Avx.IsSupported)
                    {
                        Vector256<double> vec = Vector256.Create(w);
                        for (int top = size & Simd.AVX_MASK; j < top; j += 4)
                            Avx.Store(p + j, Avx.LoadVector256(p + j).MultiplyAdd(pa + j, vec));
                    }
                    for (; j < size; j++)
                        p[j] += pa[j] * w;
                }
            }
        }
        return values;
    }

    /// <summary>Low-level method to linearly combine two vectors with weights.</summary>
    /// <remarks>
    /// This method is a frequent special case of the more general
    /// <see cref="Combine(Vector, Vector[])"/>.
    /// </remarks>
    /// <param name="w1">Weight for the first vector.</param>
    /// <param name="w2">Weight for the second vector.</param>
    /// <param name="v1">First vector in the linear combination.</param>
    /// <param name="v2">Second vector in the linear combination.</param>
    /// <returns>Returns the linear combination <c>w1 * v1 + w2 * v2</c>.</returns>
    public static Vector Combine2(double w1, double w2, Vector v1, Vector v2)
    {
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        double[] values = GC.AllocateUninitializedArray<double>(v1.Length);
        ref double a = ref MemoryMarshal.GetArrayDataReference(v1.values);
        ref double b = ref MemoryMarshal.GetArrayDataReference(v2.values);
        ref double c = ref MemoryMarshal.GetArrayDataReference(values);
        if (Fma.IsSupported)
        {
            Vector256<double> vw1 = Vector256.Create(w1), vw2 = Vector256.Create(w2);
            int t = values.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += 4)
                Vector256.StoreUnsafe(
                    Fma.MultiplyAdd(
                        Vector256.LoadUnsafe(ref Add(ref a, i)),
                        vw1,
                        Vector256.LoadUnsafe(ref Add(ref b, i)) * vw2),
                    ref Add(ref c, i));
            Vector256.StoreUnsafe(
                Fma.MultiplyAdd(
                    Vector256.LoadUnsafe(ref Add(ref a, t)),
                    vw1,
                    Vector256.LoadUnsafe(ref Add(ref b, t)) * vw2),
                ref Add(ref c, t));
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
    public double Distance(Vector v)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        return CommonMatrix.Distance(values, v.values);
    }

    /// <summary>Calculates the sum of the vector's items.</summary>
    /// <returns>The sum of all vector's items.</returns>
    public double Sum()
    {
        Contract.Requires(IsInitialized);

        double result = 0d;
        ref double p = ref MemoryMarshal.GetArrayDataReference(values);
        ref double q = ref Add(ref p, values.Length);
        if (Vector256.IsHardwareAccelerated && Length > Vector256<double>.Count)
        {
            ref double last = ref Add(ref p, values.Length & Simd.AVX_MASK + 1);
            Vector256<double> sum = Vector256<double>.Zero;
            do
            {
                sum += Vector256.LoadUnsafe(ref p);
                p = ref Add(ref p, Vector256<double>.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            result = sum.Sum();
        }
        for (; IsAddressLessThan(ref p, ref q); p = ref Add(ref p, 1))
            result += p;
        return result;
    }

    /// <summary>Calculates the product of the vector's items.</summary>
    /// <returns>The product of all vector's items.</returns>
    public double Product()
    {
        Contract.Requires(IsInitialized);

        double result = 1d;
        ref double p = ref MemoryMarshal.GetArrayDataReference(values);
        ref double q = ref Add(ref p, values.Length);
        if (Vector256.IsHardwareAccelerated && Length > Vector256<double>.Count)
        {
            ref double last = ref Add(ref p, values.Length & Simd.AVX_MASK + 1);
            Vector256<double> prod = Vector256.Create(1d);
            do
            {
                prod *= Vector256.LoadUnsafe(ref p);
                p = ref Add(ref p, Vector256<double>.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            result = prod.Product();
        }
        for (; IsAddressLessThan(ref p, ref q); p = ref Add(ref p, 1))
            result *= p;
        return result;
    }

    /// <summary>Computes the mean of the vector's items.</summary>
    /// <returns><code>this.Sum() / this.Length</code></returns>
    public double Mean() => Sum() / Length;

    /// <summary>Pointwise squared root.</summary>
    /// <returns>A new vector with the square root of the original items.</returns>
    public Vector Sqrt()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(values);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.Sqrt(Vector256.LoadUnsafe(ref Add(ref p, i))),
                    ref Add(ref q, i));
            Vector256.StoreUnsafe(
                Vector256.Sqrt(Vector256.LoadUnsafe(ref Add(ref p, t))),
                ref Add(ref q, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref q, i) = Math.Sqrt(Add(ref p, i));
        return result;
    }

    /// <summary>Gets the absolute values of the vector's items.</summary>
    /// <returns>A new vector with non-negative items.</returns>
    public Vector Abs()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == Length);

        double[] result = GC.AllocateUninitializedArray<double>(Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(values);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.Abs(Vector256.LoadUnsafe(ref Add(ref p, i))),
                    ref Add(ref q, i));
            Vector256.StoreUnsafe(
                Vector256.Abs(Vector256.LoadUnsafe(ref Add(ref p, t))),
                ref Add(ref q, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref q, i) = Math.Abs(Add(ref p, i));
        return result;
    }

    /// <summary>
    /// Creates a new vector by transforming each item with the given function.
    /// </summary>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new vector with transformed content.</returns>
    public unsafe Vector Map(Func<double, double> mapper)
    {
        double[] newValues = GC.AllocateUninitializedArray<double>(values.Length);
        fixed (double* p = values)
            for (int i = 0; i < newValues.Length; i++)
                newValues[i] = mapper(p[i]);
        return newValues;
    }

    /// <summary>Checks whether the predicate is satisfied by all items.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if all items satisfy the predicate.</returns>
    public bool All(Func<double, bool> predicate)
    {
        foreach (double value in values)
            if (!predicate(value))
                return false;
        return true;
    }

    /// <summary>Checks whether the predicate is satisfied by at least one item.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if there exists a item satisfying the predicate.</returns>
    public bool Any(Func<double, bool> predicate)
    {
        foreach (double value in values)
            if (predicate(value))
                return true;
        return false;
    }

    /// <summary>
    /// Creates a new vector by filtering the items with the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>A new vector with the filtered items.</returns>
    public Vector Filter(Func<double, bool> predicate)
    {
        double[] newValues = GC.AllocateUninitializedArray<double>(values.Length);
        int j = 0;
        foreach (double value in values)
            if (predicate(value))
                newValues[j++] = value;
        return j == 0 ? Array.Empty<double>() : j == Length ? this : newValues[..j];
    }

    /// <summary>Creates an aggregate value by applying the reducer to each item.</summary>
    /// <param name="seed">The initial value.</param>
    /// <param name="reducer">The reducing function.</param>
    /// <returns>The final synthesized value.</returns>
    public double Reduce(double seed, Func<double, double, double> reducer)
    {
        foreach (double value in values)
            seed = reducer(seed, value);
        return seed;
    }

    /// <summary>Combines the common prefix of two vectors.</summary>
    /// <param name="other">Second vector to combine.</param>
    /// <param name="zipper">The combining function.</param>
    /// <returns>The combining function applied to each pair of items.</returns>
    public unsafe Vector Zip(Vector other, Func<double, double, double> zipper)
    {
        int len = Min(Length, other.Length);
        double[] newValues = GC.AllocateUninitializedArray<double>(len);
        fixed (double* p = values, q = other.values, r = newValues)
            for (int i = 0; i < len; i++)
                r[i] = zipper(p[i], q[i]);
        return newValues;
    }

    /// <summary>Computes the autocorrelation for a fixed lag.</summary>
    /// <param name="lag">Lag number in samples.</param>
    /// <param name="average">Estimated average for vector items.</param>
    /// <returns>The autocorrelation factor.</returns>
    internal unsafe double AutoCorrelation(int lag, double average)
    {
        int count = Length - lag;
        double ex = 0, ey = 0, exy = 0, exx = 0, eyy = 0;
        fixed (double* p = values)
        {
            double* q = p + lag;
            int i = 0;
            if (Avx.IsSupported)
            {
                Vector256<double> avg = Vector256.Create(average);
                Vector256<double> vex = Vector256<double>.Zero;
                Vector256<double> vey = Vector256<double>.Zero;
                Vector256<double> vexx = Vector256<double>.Zero;
                Vector256<double> vexy = Vector256<double>.Zero;
                Vector256<double> veyy = Vector256<double>.Zero;
                for (int top = count & Simd.AVX_MASK; i < top; i += 4)
                {
                    Vector256<double> x = Avx.Subtract(Avx.LoadVector256(p + i), avg);
                    Vector256<double> y = Avx.Subtract(Avx.LoadVector256(q + i), avg);
                    vex = Avx.Add(vex, x);
                    vey = Avx.Add(vey, y);
                    vexx = vexx.MultiplyAdd(x, x);
                    vexy = vexy.MultiplyAdd(x, y);
                    veyy = veyy.MultiplyAdd(y, y);
                }
                ex = vex.Sum(); ey = vey.Sum();
                exx = vexx.Sum(); exy = vexy.Sum(); eyy = veyy.Sum();
            }
            for (; i < count; i++)
            {
                double x = p[i] - average, y = q[i] - average;
                ex += x;
                ey += y;
                exy += x * y;
                exx += x * x;
                eyy += y * y;
            }
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

    /// <summary>Multilinear regression based in Ordinary Least Squares.</summary>
    /// <param name="predictors">Predicting series.</param>
    /// <returns>Regression coefficients.</returns>
    public Vector LinearModel(params Vector[] predictors)
    {
        int size = predictors[0].Length;
        if (predictors.Any(p => p.Length != size))
            throw new VectorLengthException();
        Vector[] rows = new Vector[predictors.Length + 1];
        rows[0] = new(size, 1.0);
        for (int i = 0; i < predictors.Length; i++)
            rows[i + 1] = predictors[i];
        Matrix x = new(rows);
        return x.MultiplyTranspose(x).Cholesky().Solve(x * this);
    }

    /// <summary>Creates a linear model a set of predictors.</summary>
    /// <param name="predictors">Vectors used to predict this one.</param>
    /// <returns>A full linear model.</returns>
    public LinearVModel FullLinearModel(params Vector[] predictors) =>
        new(this, predictors);

    /// <summary>Creates an AR model from a vector and a degree.</summary>
    /// <param name="degree">Number of independent variables in the model.</param>
    /// <returns>A full autoregressive model.</returns>
    public ARVModel ARModel(int degree) => new(this, degree);

    /// <summary>Finds the coefficients for an autoregressive model.</summary>
    /// <param name="degree">Number of coefficients in the model.</param>
    /// <returns>The coefficients of the AR(degree) model.</returns>
    public Vector AutoRegression(int degree) => AutoRegression(degree, out _, out _);

    /// <summary>Finds the coefficients for an autoregressive model.</summary>
    /// <param name="degree">Number of coefficients in the model.</param>
    /// <param name="matrix">The correlation matrix.</param>
    /// <param name="correlations">The correlations.</param>
    /// <returns>The coefficients of the AR(degree) model.</returns>
    internal unsafe Vector AutoRegression(int degree, out Matrix matrix, out Vector correlations)
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
            Vector v = mean == 0 ? this : this - mean;
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

    /// <summary>Creates a reversed copy of the vector.</summary>
    /// <returns>An independent reversed copy.</returns>
    public Vector Reverse()
    {
        Vector result = Clone();
        Array.Reverse(result.values);
        return result;
    }

    /// <summary>Returns a new vector with the distinct values in the original one.</summary>
    /// <remarks>Results are unordered.</remarks>
    /// <returns>A new vector with distinct values.</returns>
    public Vector Distinct()
    {
        HashSet<double> set = new(Length);
        foreach (double value in values)
            set.Add(value);
        return new(set.ToArray());
    }

    /// <summary>Returns a new vector with sorted values.</summary>
    /// <returns>A new vector with sorted values.</returns>
    public Vector Sort()
    {
        Vector result = Clone();
        Array.Sort(result.values);
        return result;
    }

    /// <summary>Computes the real discrete Fourier transform.</summary>
    /// <returns>The spectrum.</returns>
    public FftRModel Fft() => new(FFT.Transform(values));

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(double value) => IndexOf(value, 0);

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <param name="from">The zero-based starting index.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    public unsafe int IndexOf(double value, int from)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(from >= 0 && from < Length);
        Contract.Ensures(Contract.Result<int>() >= -1 && Contract.Result<int>() < Length);

        fixed (double* p = values)
        {
            int i = 0, size = Length - from;
            double* q = p + from;
            if (Avx.IsSupported)
            {
                Vector256<double> v = Vector256.Create(value);
                for (int top = size & Simd.AVX_MASK; i < top; i += 4)
                {
                    int mask = Avx.MoveMask(Avx.CompareEqual(Avx.LoadVector256(q + i), v));
                    if (mask != 0)
                        return i + BitOperations.TrailingZeroCount(mask) + from;
                }
            }
            for (; i < size; i++)
                if (q[i] == value)
                    return i + from;
        }
        return -1;
    }

    /// <summary>Compares two vectors for equality withing a tolerance.</summary>
    /// <param name="v1">First vector to compare.</param>
    /// <param name="v2">Second vector to compare.</param>
    /// <param name="epsilon">The tolerance.</param>
    /// <returns>True if each pair of components is inside the tolerance.</returns>
    public static bool Equals(Vector v1, Vector v2, double epsilon)
    {
        if (v1.Length != v2.Length)
            return false;
        for (int i = 0; i < v1.Length; i++)
            if (Math.Abs(v1[i] - v2[i]) > epsilon)
                return false;
        return true;
    }

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <returns>Space-separated components.</returns>
    public override string ToString() =>
        $"ans ∊ ℝ({Length})" + Environment.NewLine +
        CommonMatrix.ToString(values, v => v.ToString("G6"));

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        $"ans ∊ ℝ({Length})" + Environment.NewLine +
        CommonMatrix.ToString(values, v => v.ToString(format, provider));

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
    public unsafe bool Equals(Vector other)
    {
        if (other.Length != Length)
            return false;
        fixed (double* p = values, q = other.values)
        {
            int i = 0, size = Length;
            if (Avx.IsSupported)
                for (int top = size & Simd.AVX_MASK; i < top; i += 4)
                    if (Avx.MoveMask(Avx.CompareEqual(
                        Avx.LoadVector256(p + i), Avx.LoadVector256(q + i))) != 0xF)
                        return false;
            for (; i < size; i++)
                if (p[i] != q[i])
                    return false;
        }
        return true;
    }

    /// <summary>Checks if the provided argument is a vector with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a vector with the same items.</returns>
    public override bool Equals(object? obj) =>
        obj is Vector vector && Equals(vector);

    /// <summary>Returns the hashcode for this vector.</summary>
    /// <returns>A hashcode summarizing the content of the vector.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)values).GetHashCode(EqualityComparer<double>.Default);

    /// <summary>Compares two vectors for equality. </summary>
    /// <param name="left">First vector operand.</param>
    /// <param name="right">Second vector operand.</param>
    /// <returns><see langword="true"/> if all corresponding items are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector left, Vector right) => left.Equals(right);

    /// <summary>Compares two vectors for inequality. </summary>
    /// <param name="left">First vector operand.</param>
    /// <param name="right">Second vector operand.</param>
    /// <returns><see langword="true"/> if any pair of corresponding items are not equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector left, Vector right) => !left.Equals(right);

    internal (double total, double residuals, double r2) GetSumSquares(Vector other)
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
