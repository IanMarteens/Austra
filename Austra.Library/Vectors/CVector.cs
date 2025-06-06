﻿namespace Austra.Library;

/// <summary>Represents a dense complex vector of arbitrary size.</summary>
/// <remarks>
/// For the sake of acceleration, the vector's components are stored in two separate arrays.
/// Most operations are non-destructive.
/// </remarks>
public readonly struct CVector :
    IFormattable,
    IEnumerable<Complex>,
    IEquatable<CVector>,
    IEqualityOperators<CVector, CVector, bool>,
    IAdditionOperators<CVector, CVector, CVector>,
    IAdditionOperators<CVector, Complex, CVector>,
    IAdditionOperators<CVector, double, CVector>,
    ISubtractionOperators<CVector, CVector, CVector>,
    ISubtractionOperators<CVector, Complex, CVector>,
    ISubtractionOperators<CVector, double, CVector>,
    IMultiplyOperators<CVector, CVector, Complex>,
    IMultiplyOperators<CVector, Complex, CVector>,
    IMultiplyOperators<CVector, double, CVector>,
    IUnaryNegationOperators<CVector, CVector>,
    IPointwiseOperators<CVector>,
    ISafeIndexed, IVector, IIndexable
{
    /// <summary>Stores the real components of the vector.</summary>
    private readonly double[] re;
    /// <summary>Stores the imaginary components of the vector.</summary>
    private readonly double[] im;

    /// <summary>Creates a complex vector of a given size.</summary>
    /// <param name="size">Vector length.</param>
    public CVector(int size) => (re, im) = (new double[size], new double[size]);

    /// <summary>Creates a complex vector for a complex value.</summary>
    /// <param name="value">A complex value.</param>
    public CVector(Complex value) =>
        (re, im) = ([value.Real], [value.Imaginary]);

    /// <summary>Creates a complex vector for a real value.</summary>
    /// <param name="value">A real value.</param>
    public CVector(double value) => (re, im) = ([value], new double[1]);

    /// <summary>Creates a complex vector from two complex values.</summary>
    /// <param name="v1">First complex value.</param>
    /// <param name="v2">Second complex value.</param>
    public CVector(Complex v1, Complex v2) =>
        (re, im) = ([v1.Real, v2.Real], [v1.Imaginary, v2.Imaginary]);

    /// <summary>Creates a complex vector from two real values.</summary>
    /// <param name="v1">A real value.</param>
    /// <param name="v2">Second value.</param>
    public CVector(double v1, double v2) => (re, im) = ([v1, v2], new double[2]);

    internal CVector(Complex c1, Complex c2, Complex c3) => (re, im) = (
        [c1.Real, c2.Real, c3.Real],
        [c1.Imaginary, c2.Imaginary, c3.Imaginary]);

    /// <summary>Creates a complex vector from separate component arrays.</summary>
    /// <param name="re">The real components of the vector.</param>
    /// <param name="im">The imaginary components of the vector.</param>
    public CVector(double[] re, double[] im)
    {
        if (re.Length != im.Length)
            throw new VectorLengthException();
        (this.re, this.im) = (re, im);
    }

    /// <summary>Creates a complex vector from two real vectors.</summary>
    /// <param name="re">The real components of the vector.</param>
    /// <param name="im">The imaginary components of the vector.</param>
    public CVector(DVector re, DVector im) : this((double[])re, (double[])im) { }

    /// <summary>Creates a complex vector from a real vector.</summary>
    /// <param name="re">The real components of the vector.</param>
    public CVector(DVector re) : this((double[])re, new double[re.Length]) { }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="rnd">A random number generator.</param>
    /// <param name="offset">An offset for the random numbers.</param>
    /// <param name="width">Width for the uniform distribution.</param>
    public CVector(int size, Random rnd, double offset, double width)
        : this(new DVector(size, rnd, offset, width), new DVector(size, rnd, offset, width)) { }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="rnd">A random number generator.</param>
    public CVector(int size, Random rnd)
        : this(new DVector(size, rnd), new DVector(size, rnd)) { }

    /// <summary>Creates a vector filled with a normal distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="rnd">A normal random number generator.</param>
    public CVector(int size, NormalRandom rnd)
        : this(new DVector(size, rnd), new DVector(size, rnd)) { }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public CVector(int size, Func<int, Complex> f) : this(size)
    {
        ref double p = ref MM.GetArrayDataReference(re);
        ref double q = ref MM.GetArrayDataReference(im);
        for (int i = 0; i < size; i++)
            (Add(ref p, i), Add(ref q, i)) = f(i);
    }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public CVector(int size, Func<int, CVector, Complex> f) : this(size)
    {
        ref double p = ref MM.GetArrayDataReference(re);
        ref double q = ref MM.GetArrayDataReference(im);
        for (int i = 0; i < size; i++)
            (Add(ref p, i), Add(ref q, i)) = f(i, this);
    }

    /// <summary>Initializes a complex vector from a complex array.</summary>
    /// <param name="values">The complex components of the vector.</param>
    public CVector(Complex[] values) : this(values.Length)
    {
        ref double p = ref MM.GetArrayDataReference(re);
        ref double q = ref MM.GetArrayDataReference(im);
        if (V4.IsHardwareAccelerated && re.Length >= V4d.Count)
        {
            ref double r = ref As<Complex, double>(ref MM.GetArrayDataReference(values));
            int t = values.Length - V4d.Count;
            for (int i = 0; i < t; i += V4d.Count)
            {
                V4d v1 = V4.LoadUnsafe(ref Add(ref r, 2 * i));
                V4d v2 = V4.LoadUnsafe(ref Add(ref r, 2 * i + 4));
                V4.StoreUnsafe(Avx2.Permute4x64(Avx.UnpackLow(v1, v2), 0b11011000),
                    ref Add(ref p, i));
                V4.StoreUnsafe(Avx2.Permute4x64(Avx.UnpackHigh(v1, v2), 0b11011000),
                    ref Add(ref q, i));
            }
            V4d v3 = V4.LoadUnsafe(ref Add(ref r, 2 * t));
            V4d v4 = V4.LoadUnsafe(ref Add(ref r, 2 * t + 4));
            V4.StoreUnsafe(Avx2.Permute4x64(Avx.UnpackLow(v3, v4), 0b11011000),
                ref Add(ref p, t));
            V4.StoreUnsafe(Avx2.Permute4x64(Avx.UnpackHigh(v3, v4), 0b11011000),
                ref Add(ref q, t));
        }
        else
        {
            ref Complex r = ref MM.GetArrayDataReference(values);
            for (int i = 0; i < values.Length; i++)
                (Add(ref p, i), Add(ref q, i)) = Add(ref r, i);
        }
    }

    /// <summary>Deconstructs the vector into a tuple of real and imaginary vectors.</summary>
    /// <param name="re">The real vector.</param>
    /// <param name="im">The imaginary vector.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out DVector re, out DVector im) => (re, im) = (this.re, this.im);

    /// <summary>Creates an identical vector.</summary>
    /// <remarks>A new copy of the original storage is created.</remarks>
    /// <returns>A deep clone of the instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CVector Clone() => new((double[])re.Clone(), (double[])im.Clone());

    /// <summary>Explicit conversion from vector to array.</summary>
    /// <param name="v">The original vector.</param>
    /// <returns>An array of <see cref="Complex"/> numbers.</returns>
    public static explicit operator Complex[](CVector v)
    {
        Complex[] result = GC.AllocateUninitializedArray<Complex>(v.Length);
        ref double p = ref MM.GetArrayDataReference(v.re);
        ref double q = ref MM.GetArrayDataReference(v.im);
        ref double rs = ref As<Complex, double>(ref MM.GetArrayDataReference(result));
        if (V4.IsHardwareAccelerated && v.re.Length >= V4d.Count)
        {
            nuint t = (nuint)(v.re.Length - V4d.Count);
            for (nuint i = 0, idx = 0; i < t; i += (nuint)V4d.Count, idx += 8)
            {
                V4d vr = V4.LoadUnsafe(ref p, i), vi = V4.LoadUnsafe(ref q, i);
                V4.StoreUnsafe(Avx2.Permute4x64(Avx.Permute2x128(
                    vr, vi, 0b0010_0000), 0b11_01_10_00), ref rs, idx);
                V4.StoreUnsafe(Avx2.Permute4x64(Avx.Permute2x128(
                    vr, vi, 0b0011_0001), 0b11_01_10_00), ref rs, idx + (nuint)V4d.Count);
            }
            V4d wr = V4.LoadUnsafe(ref p, t), wi = V4.LoadUnsafe(ref q, t);
            V4.StoreUnsafe(Avx2.Permute4x64(Avx.Permute2x128(
                wr, wi, 0b0010_0000), 0b11_01_10_00), ref rs, t + t);
            V4.StoreUnsafe(Avx2.Permute4x64(Avx.Permute2x128(
                wr, wi, 0b0011_0001), 0b11_01_10_00), ref rs, t + t + (nuint)V4d.Count);
        }
        else
            for (int i = 0; i < result.Length; i++)
                result[i] = new(Add(ref p, i), Add(ref q, i));
        return result;
    }

    /// <summary>Creates a reversed copy of the vector.</summary>
    /// <returns>An independent reversed copy.</returns>
    public CVector Reverse()
    {
        CVector result = Clone();
        Array.Reverse(result.re);
        Array.Reverse(result.im);
        return result;
    }

    /// <summary>Returns a new vector with the distinct values in the original one.</summary>
    /// <remarks>Results are unordered.</remarks>
    /// <returns>A new vector with distinct values.</returns>
    public CVector Distinct()
    {
        HashSet<Complex> set = new(Length);
        foreach (Complex value in this)
            set.Add(value);
        return new(set.ToArray());
    }

    /// <summary>Creates a new complex vector with conjugated values.</summary>
    /// <returns>Each item with the sign of the imaginary value inverted.</returns>
    public CVector Conjugate() => new(re, -new DVector(im));

    /// <summary>Gets the first complex in the vector.</summary>
    public Complex First => new(re[0], im[0]);
    /// <summary>Gets the last complex in the vector.</summary>
    public Complex Last => new(re[^1], im[^1]);

    /// <summary>Gets the dimensions of the vector.</summary>
    public int Length => re.Length;

    /// <summary>Has the vector been properly initialized?</summary>
    /// <remarks>
    /// Since <see cref="CVector"/> is a struct, its default constructor doesn't
    /// initializes the underlying component arrays.
    /// </remarks>
    public bool IsInitialized => re != null && im != null;

    /// <summary>Gets or sets the component at a given index.</summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The complex value at the given index.</returns>
    public Complex this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (uint)index < (uint)re.Length
            ? new(
                Add(ref MM.GetArrayDataReference(re), index),
                Add(ref MM.GetArrayDataReference(im), index))
            : throw new IndexOutOfRangeException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => (re[index], im[index]) = value;
    }

    /// <summary>Gets the component at a given index.</summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The complex value at the given index.</returns>
    public Complex this[Index index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(re[index], im[index]);
    }

    /// <summary>Extracts a slice from the vector.</summary>
    /// <param name="range">The range defining the slice.</param>
    /// <returns>The new vector representing the slice.</returns>
    public CVector this[Range range]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(re[range], im[range]);
    }

    /// <summary>
    /// Safe access to the vector's components. If the index is out of range, a zero is returned.
    /// </summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index, or zero, if index is out of range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Complex SafeThis(int index) => (uint)index < (uint)Length
        ? new(
            Add(ref MM.GetArrayDataReference(re), index),
            Add(ref MM.GetArrayDataReference(im), index))
        : Complex.Zero;

    /// <summary>Adds two complex vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The component by component sum.</returns>
    public static CVector operator +(CVector v1, CVector v2) => new(
        new DVector(v1.re) + new DVector(v2.re),
        new DVector(v1.im) + new DVector(v2.im));

    /// <summary>Subtracts two complex vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The component by component sum.</returns>
    public static CVector operator -(CVector v1, CVector v2) => new(
        new DVector(v1.re) - new DVector(v2.re),
        new DVector(v1.im) - new DVector(v2.im));

    /// <summary>Negates a complex vector.</summary>
    /// <param name="v">The vector operand.</param>
    /// <returns>The itemwise negation.</returns>
    public static CVector operator -(CVector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == v.Length);
        return new(-new DVector(v.re), -new DVector(v.im));
    }

    /// <summary>Adds a complex scalar to a complex vector.</summary>
    /// <param name="v">A vector summand.</param>
    /// <param name="c">A complex scalar summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    public static CVector operator +(CVector v, Complex c)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<CVector>().Length == v.Length);
        return new(new DVector(v.re) + c.Real, new DVector(v.im) + c.Imaginary);
    }

    /// <summary>Adds a double scalar to a complex vector.</summary>
    /// <param name="v">A vector summand.</param>
    /// <param name="d">A double scalar summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    public static CVector operator +(CVector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<CVector>().Length == v.Length);
        return new(new DVector(v.re) + d, new DVector(v.im));
    }

    /// <summary>Adds a complex scalar to a complex vector.</summary>
    /// <param name="c">A complex scalar summand.</param>
    /// <param name="v">A vector summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CVector operator +(Complex c, CVector v) => v + c;

    /// <summary>Adds a double scalar to a complex vector.</summary>
    /// <param name="d">A scalar summand.</param>
    /// <param name="v">A vector summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CVector operator +(double d, CVector v) => v + d;

    /// <summary>Subtracts a scalar from a complex vector.</summary>
    /// <param name="v">The vector minuend.</param>
    /// <param name="c">The scalar subtrahend.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static CVector operator -(CVector v, Complex c)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<CVector>().Length == v.Length);
        return new(new DVector(v.re) - c.Real, new DVector(v.im) - c.Imaginary);
    }

    /// <summary>Subtracts a scalar from a complex vector.</summary>
    /// <param name="v">The vector minuend.</param>
    /// <param name="d">The scalar subtrahend.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static CVector operator -(CVector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<CVector>().Length == v.Length);
        return new(new DVector(v.re) - d, new DVector(v.im));
    }

    /// <summary>Subtracts a vector from a scalar.</summary>
    /// <param name="c">The scalar minuend.</param>
    /// <param name="v">The vector subtrahend.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static CVector operator -(Complex c, CVector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<CVector>().Length == v.Length);
        return new(c.Real - new DVector(v.re), c.Imaginary - new DVector(v.im));
    }

    /// <summary>Subtracts a vector from a scalar.</summary>
    /// <param name="d">The scalar minuend.</param>
    /// <param name="v">The vector subtrahend.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static CVector operator -(double d, CVector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<CVector>().Length == v.Length);
        return new(d - new DVector(v.re), -new DVector(v.im));
    }

    /// <summary>Pointwise multiplication.</summary>
    /// <param name="other">Second vector operand.</param>
    /// <returns>The component by component product.</returns>
    public CVector PointwiseMultiply(CVector other)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(other.IsInitialized);
        if (Length != other.Length)
            throw new VectorLengthException();
        Contract.Ensures(Contract.Result<DVector>().Length == Length);

        double[] r = new double[Length], m = new double[Length];
        ref double pr = ref MM.GetArrayDataReference(re);
        ref double pi = ref MM.GetArrayDataReference(im);
        ref double qr = ref MM.GetArrayDataReference(other.re);
        ref double qi = ref MM.GetArrayDataReference(other.im);
        ref double vr = ref MM.GetArrayDataReference(r);
        ref double vm = ref MM.GetArrayDataReference(m);
        if (V8.IsHardwareAccelerated && r.Length >= V8d.Count)
        {
            nuint t = (nuint)(r.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
            {
                V8d vpr = V8.LoadUnsafe(ref pr, i), vpi = V8.LoadUnsafe(ref pi, i);
                V8d vqr = V8.LoadUnsafe(ref qr, i), vqi = V8.LoadUnsafe(ref qi, i);
                V8.StoreUnsafe(Avx512F.FusedMultiplyAddNegated(vpi, vqi, vpr * vqr), ref vr, i);
                V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(vpi, vqr, vpr * vqi), ref vm, i);
            }
            V8d wpr = V8.LoadUnsafe(ref pr, t), wpi = V8.LoadUnsafe(ref pi, t);
            V8d wqr = V8.LoadUnsafe(ref qr, t), wqi = V8.LoadUnsafe(ref qi, t);
            V8.StoreUnsafe(Avx512F.FusedMultiplyAddNegated(wpi, wqi, wpr * wqr), ref vr, t);
            V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(wpi, wqr, wpr * wqi), ref vm, t);
        }
        else if (V4.IsHardwareAccelerated && r.Length >= V4d.Count)
        {
            nuint t = (nuint)(r.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
            {
                V4d vpr = V4.LoadUnsafe(ref pr, i), vpi = V4.LoadUnsafe(ref pi, i);
                V4d vqr = V4.LoadUnsafe(ref qr, i), vqi = V4.LoadUnsafe(ref qi, i);
                V4.StoreUnsafe((vpr * vqr).MultiplyAddNeg(vpi, vqi), ref vr, i);
                V4.StoreUnsafe((vpr * vqi).MultiplyAdd(vpi, vqr), ref vm, i);
            }
            V4d wpr = V4.LoadUnsafe(ref pr, t), wpi = V4.LoadUnsafe(ref pi, t);
            V4d wqr = V4.LoadUnsafe(ref qr, t), wqi = V4.LoadUnsafe(ref qi, t);
            V4.StoreUnsafe((wpr * wqr).MultiplyAddNeg(wpi, wqi), ref vr, t);
            V4.StoreUnsafe((wpr * wqi).MultiplyAdd(wpi, wqr), ref vm, t);
        }
        else
            for (int i = 0; i < r.Length; i++)
                (Add(ref vr, i), Add(ref vm, i)) = (
                    Add(ref pr, i) * Add(ref qr, i) - Add(ref pi, i) * Add(ref qi, i),
                    Add(ref pr, i) * Add(ref qi, i) + Add(ref pi, i) * Add(ref qr, i));
        return new(r, m);
    }

    /// <summary>Pointwise division.</summary>
    /// <param name="other">Second vector operand.</param>
    /// <returns>The component by component quotient.</returns>
    public CVector PointwiseDivide(CVector other)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(other.IsInitialized);
        if (Length != other.Length)
            throw new VectorLengthException();
        Contract.Ensures(Contract.Result<DVector>().Length == Length);

        double[] r = GC.AllocateUninitializedArray<double>(Length);
        double[] m = GC.AllocateUninitializedArray<double>(Length);
        ref double pr = ref MM.GetArrayDataReference(re);
        ref double pi = ref MM.GetArrayDataReference(im);
        ref double qr = ref MM.GetArrayDataReference(other.re);
        ref double qi = ref MM.GetArrayDataReference(other.im);
        ref double vr = ref MM.GetArrayDataReference(r);
        ref double vm = ref MM.GetArrayDataReference(m);
        if (V8.IsHardwareAccelerated && r.Length >= V8d.Count)
        {
            nuint t = (nuint)(r.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
            {
                V8d vpr = V8.LoadUnsafe(ref pr, i), vpi = V8.LoadUnsafe(ref pi, i);
                V8d vqr = V8.LoadUnsafe(ref qr, i), vqi = V8.LoadUnsafe(ref qi, i);
                V8d quot = V8d.One / Avx512F.FusedMultiplyAdd(vqi, vqi, vqr * vqr);
                V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(vpi, vqi, vpr * vqr) * quot, ref vr, i);
                V8.StoreUnsafe(Avx512F.FusedMultiplyAddNegated(vpr, vqi, vpi * vqr) * quot, ref vm, i);
            }
            V8d wpr = V8.LoadUnsafe(ref pr, t), wpi = V8.LoadUnsafe(ref pi, t);
            V8d wqr = V8.LoadUnsafe(ref qr, t), wqi = V8.LoadUnsafe(ref qi, t);
            V8d wquot = V8d.One / Avx512F.FusedMultiplyAdd(wqi, wqi, wqr * wqr);
            V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(wpi, wqi, wpr * wqr) * wquot, ref vr, t);
            V8.StoreUnsafe(Avx512F.FusedMultiplyAddNegated(wpr, wqi, wpi * wqr) * wquot, ref vm, t);
        }
        else if (V4.IsHardwareAccelerated && r.Length >= V4d.Count)
        {
            nuint t = (nuint)(r.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
            {
                V4d vpr = V4.LoadUnsafe(ref pr, i), vpi = V4.LoadUnsafe(ref pi, i);
                V4d vqr = V4.LoadUnsafe(ref qr, i), vqi = V4.LoadUnsafe(ref qi, i);
                V4d quot = (vqr * vqr).MultiplyAdd(vqi, vqi);
                V4.StoreUnsafe((vpr * vqr).MultiplyAdd(vpi, vqi) / quot, ref vr, i);
                V4.StoreUnsafe((vpi * vqr).MultiplyAddNeg(vpr, vqi) / quot, ref vm, i);
            }
            V4d wpr = V4.LoadUnsafe(ref pr, t), wpi = V4.LoadUnsafe(ref pi, t);
            V4d wqr = V4.LoadUnsafe(ref qr, t), wqi = V4.LoadUnsafe(ref qi, t);
            V4d wquot = (wqr * wqr).MultiplyAdd(wqi, wqi);
            V4.StoreUnsafe((wpr * wqr).MultiplyAdd(wpi, wqi) / wquot, ref vr, t);
            V4.StoreUnsafe((wpi * wqr).MultiplyAddNeg(wpr, wqi) / wquot, ref vm, t);
        }
        else
            for (int i = 0; i < r.Length; i++)
                (Add(ref vr, i), Add(ref vm, i)) = new Complex(Add(ref pr, i), Add(ref pi, i))
                    / new Complex(Add(ref qr, i), Add(ref qi, i));
        return new(r, m);
    }

    /// <summary>Dot product of two vectors.</summary>
    /// <remarks>The values from the second vector are automatically conjugated.</remarks>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The dot product of the operands.</returns>
    public static Complex operator *(CVector v1, CVector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        Contract.EndContractBlock();

        ref double pr = ref MM.GetArrayDataReference(v1.re);
        ref double pi = ref MM.GetArrayDataReference(v1.im);
        ref double qr = ref MM.GetArrayDataReference(v2.re);
        ref double qi = ref MM.GetArrayDataReference(v2.im);
        double sumRe = 0, sumIm = 0;
        int i = 0, size = v1.Length;
        if (V8.IsHardwareAccelerated)
        {
            V8d accRe = V8d.Zero, accIm = V8d.Zero;
            for (int top = size & Simd.MASK8; i < top; i += V8d.Count)
            {
                V8d vpr = V8.LoadUnsafe(ref Add(ref pr, i));
                V8d vpi = V8.LoadUnsafe(ref Add(ref pi, i));
                V8d vqr = V8.LoadUnsafe(ref Add(ref qr, i));
                V8d vqi = V8.LoadUnsafe(ref Add(ref qi, i));
                accRe += Avx512F.FusedMultiplyAdd(vpi, vqi, vpr * vqr);
                accIm += Avx512F.FusedMultiplyAddNegated(vpr, vqi, vpi * vqr);
            }
            sumRe = V8.Sum(accRe);
            sumIm = V8.Sum(accIm);
        }
        else if (V4.IsHardwareAccelerated)
        {
            V4d accRe = V4d.Zero, accIm = V4d.Zero;
            for (int top = size & Simd.MASK4; i < top; i += V4d.Count)
            {
                V4d vpr = V4.LoadUnsafe(ref Add(ref pr, i));
                V4d vpi = V4.LoadUnsafe(ref Add(ref pi, i));
                V4d vqr = V4.LoadUnsafe(ref Add(ref qr, i));
                V4d vqi = V4.LoadUnsafe(ref Add(ref qi, i));
                accRe += (vpr * vqr).MultiplyAdd(vpi, vqi);
                accIm += (vpi * vqr).MultiplyAddNeg(vpr, vqi);
            }
            sumRe = V4.Sum(accRe);
            sumIm = V4.Sum(accIm);
        }
        for (; i < size; i++)
        {
            sumRe += FusedMultiplyAdd(Add(ref pi, i), Add(ref qi, i), Add(ref pr, i) * Add(ref qr, i));
            sumIm += FusedMultiplyAdd(-Add(ref pr, i), Add(ref qi, i), Add(ref pi, i) * Add(ref qr, i));
        }
        return new(sumRe, sumIm);
    }

    /// <summary>Gets the squared norm of this vector.</summary>
    /// <returns>The dot product with itself.</returns>
    public double Squared()
    {
        Contract.Requires(IsInitialized);
        ref double p = ref MM.GetArrayDataReference(re);
        ref double q = ref MM.GetArrayDataReference(im);
        double sum = 0;
        nuint i = 0;
        if (Avx512F.IsSupported)
        {
            V8d acc = V8d.Zero;
            for (nuint top = (nuint)Length & Simd.MASK8; i < top; i += (nuint)V8d.Count)
            {
                V8d v = V8.LoadUnsafe(ref p, i), w = V8.LoadUnsafe(ref q, i);
                acc += Avx512F.FusedMultiplyAdd(w, w, v * v);
            }
            sum = V8.Sum(acc);
        }
        else if (Avx.IsSupported)
        {
            V4d acc = V4d.Zero;
            for (nuint top = (nuint)Length & Simd.MASK4; i < top; i += (nuint)V4d.Count)
            {
                V4d v = V4.LoadUnsafe(ref p, i), w = V4.LoadUnsafe(ref q, i);
                acc += (v * v).MultiplyAdd(w, w);
            }
            sum = V4.Sum(acc);
        }
        for (; i < (nuint)Length; i++)
            sum += Add(ref p, i) * Add(ref p, i) + Add(ref q, i) * Add(ref q, i);
        return sum;
    }

    /// <summary>Gets the Euclidean norm of this vector.</summary>
    /// <returns>The squared root of the dot product.</returns>
    public double Norm() => Sqrt(Squared());

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="v">Vector to be multiplied.</param>
    /// <param name="c">A scalar multiplier.</param>
    /// <returns>The multiplication of the vector by the scalar.</returns>
    public static CVector operator *(CVector v, Complex c)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == v.Length);

        double[] re = new double[v.Length], im = new double[v.Length];
        ref double pr = ref MM.GetArrayDataReference(v.re);
        ref double pi = ref MM.GetArrayDataReference(v.im);
        ref double qr = ref MM.GetArrayDataReference(re);
        ref double qi = ref MM.GetArrayDataReference(im);
        int len = v.Length, i = 0;
        if (Avx.IsSupported)
        {
            V4d vr = V4.Create(c.Real), vi = V4.Create(c.Imaginary);
            for (int top = len & Simd.MASK4; i < top; i += 4)
            {
                V4d vpr = V4.LoadUnsafe(ref Add(ref pr, i));
                V4d vpi = V4.LoadUnsafe(ref Add(ref pi, i));
                V4.StoreUnsafe((vpr * vr).MultiplyAddNeg(vpi, vi), ref Add(ref qr, i));
                V4.StoreUnsafe((vpr * vi).MultiplyAdd(vpi, vr), ref Add(ref qi, i));
            }
        }
        for (; i < len; i++)
        {
            Add(ref qr, i) = Add(ref pr, i) * c.Real - Add(ref pi, i) * c.Imaginary;
            Add(ref qi, i) = Add(ref pr, i) * c.Imaginary + Add(ref pi, i) * c.Real;
        }
        return new(re, im);
    }

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="v">Vector to be multiplied.</param>
    /// <param name="d">A scalar multiplier.</param>
    /// <returns>The multiplication of the vector by the scalar.</returns>
    public static CVector operator *(CVector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == v.Length);
        return new(new DVector(v.re) * d, new DVector(v.im) * d);
    }

    /// <summary>Divides a complex vector by a complex value.</summary>
    /// <param name="v">Vector to be divided.</param>
    /// <param name="c">A complex scalar divisor.</param>
    /// <returns>The division of the vector by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CVector operator /(CVector v, Complex c) =>
        v * Complex.Reciprocal(c);

    /// <summary>Divides a complex vector by a scalar.</summary>
    /// <param name="v">Vector to be divided.</param>
    /// <param name="d">A scalar divisor.</param>
    /// <returns>The division of the vector by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CVector operator /(CVector v, double d) =>
        v * (1.0 / d);

    /// <summary>Multiplies a complex scalar value by a vector.</summary>
    /// <param name="c">A complex scalar multiplier.</param>
    /// <param name="v">Vector to be multiplied.</param>
    /// <returns>The multiplication of the vector by the complex scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CVector operator *(Complex c, CVector v) => v * c;

    /// <summary>Multiplies a real scalar value by a vector.</summary>
    /// <param name="d">A real scalar multiplier.</param>
    /// <param name="v">Vector to be multiplied.</param>
    /// <returns>The multiplication of the vector by the real scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CVector operator *(double d, CVector v) => v * d;

    /// <summary>Optimized vector multiplication and addition.</summary>
    /// <remarks>The current vector is the multiplicand.</remarks>
    /// <param name="multiplier">The multiplier scalar.</param>
    /// <param name="summand">The vector to be added to the scalar multiplication.</param>
    /// <returns><code>this * multiplier + summand</code></returns>
    public CVector MultiplyAdd(double multiplier, CVector summand)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(summand.IsInitialized);
        Contract.Requires(Length == summand.Length);

        return new(
            new DVector(re).MultiplyAdd(multiplier, summand.re),
            new DVector(im).MultiplyAdd(multiplier, summand.im));
    }

    /// <summary>Optimized vector scaling and subtraction.</summary>
    /// <remarks>
    /// <para>The current vector is the multiplicand.</para>
    /// <para>This operation is hardware-accelerated when possible.</para>
    /// </remarks>
    /// <param name="multiplier">The multiplier scalar.</param>
    /// <param name="subtrahend">The vector to be subtracted from the multiplication.</param>
    /// <returns><code>this * multiplier - subtrahend</code></returns>
    public CVector MultiplySubtract(double multiplier, CVector subtrahend)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(subtrahend.IsInitialized);
        Contract.Requires(Length == subtrahend.Length);

        return new(
            new DVector(re).MultiplySubtract(multiplier, subtrahend.re),
            new DVector(im).MultiplySubtract(multiplier, subtrahend.im));
    }

    /// <summary>Optimized subtraction of scaled vector.</summary>
    /// <remarks>
    /// <para>The current vector is the minuend.</para>
    /// <para>This operation is hardware-accelerated when possible.</para>
    /// </remarks>
    /// <param name="multiplier">The multiplier scalar.</param>
    /// <param name="subtrahend">The vector to scaled.</param>
    /// <returns><code>this - multiplier * subtrahend</code></returns>
    public CVector SubtractMultiply(double multiplier, CVector subtrahend)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(subtrahend.IsInitialized);
        Contract.Requires(Length == subtrahend.Length);

        return new(
            new DVector(re).SubtractMultiply(multiplier, subtrahend.re),
            new DVector(im).SubtractMultiply(multiplier, subtrahend.im));
    }

    /// <summary>Inplace addition of two vectors.</summary>
    /// <param name="v">Second vector operand.</param>
    /// <returns>The component by component sum.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CVector InplaceAdd(CVector v)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        if (Length != v.Length)
            throw new VectorLengthException();
        return new(new DVector(re).InplaceAdd(v.re), new DVector(im).InplaceAdd(v.im));
    }

    /// <summary>Inplace substraction of two vectors.</summary>
    /// <param name="v">Subtrahend.</param>
    /// <returns>The component by component subtraction.</returns>
    /// <exception cref="VectorLengthException">If the vectors have different lengths.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CVector InplaceSub(CVector v)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        if (Length != v.Length)
            throw new VectorLengthException();
        return new(new DVector(re).InplaceSub(v.re), new DVector(im).InplaceSub(v.im));
    }

    /// <summary>Inplace negation of the vector.</summary>
    /// <returns>The same vector instance, with items negated.</returns>
    public CVector InplaceNegate() =>
        new(new DVector(re).InplaceNegate(), new DVector(im).InplaceNegate());

    /// <summary>Calculates the sum of the vector's items.</summary>
    /// <returns>The sum of all vector's items.</returns>
    public Complex Sum()
    {
        Contract.Requires(IsInitialized);
        return new(re.Sum(), im.Sum());
    }

    /// <summary>Calculates the product of the vector's items.</summary>
    /// <returns>The product of all vector's items.</returns>
    public Complex Product()
    {
        Contract.Requires(IsInitialized);

        Complex result = 1d;
        ref double p = ref MM.GetArrayDataReference(re);
        ref double q = ref MM.GetArrayDataReference(im);
        ref double t = ref Add(ref p, Length);
        if (V4.IsHardwareAccelerated && Length > V4d.Count)
        {
            ref double last = ref Add(ref p, Length & Simd.MASK4);
            V4d prodRe = V4d.One, prodIm = V4d.Zero;
            do
            {
                V4d aRe = V4.LoadUnsafe(ref p);
                V4d aIm = V4.LoadUnsafe(ref q);
                prodRe = prodRe * aRe - prodIm * aIm;
                prodIm = prodRe * aIm + prodIm * aRe;
                p = ref Add(ref p, V4d.Count);
                q = ref Add(ref q, V4d.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            result = new Complex(prodRe.ToScalar(), prodIm.ToScalar())
                * new Complex(prodRe.GetElement(1), prodIm.GetElement(1))
                * new Complex(prodRe.GetElement(2), prodIm.GetElement(2))
                * new Complex(prodRe.GetElement(3), prodIm.GetElement(3));
        }
        for (; IsAddressLessThan(ref p, ref t); p = ref Add(ref p, 1), q = ref Add(ref q, 1))
            result *= new Complex(p, q);
        return result;
    }

    /// <summary>Computes the mean of the vector's items.</summary>
    /// <returns><code>this.Sum() / this.Length</code></returns>
    public Complex Mean() => Sum() / Length;

    /// <summary>Gets the real part of the complex numbers in this vector.</summary>
    public DVector Real => new(re);

    /// <summary>Gets the imaginary part of the complex numbers in this vector.</summary>
    public DVector Imaginary => new(im);

    /// <summary>
    /// Gets a vector containing the magnitudes of the complex numbers in this vector.
    /// </summary>
    /// <param name="n">The number of amplitudes to be returned.</param>
    /// <returns>A new vector with magnitudes.</returns>
    internal DVector Magnitudes(int n)
    {
        double[] result = GC.AllocateUninitializedArray<double>(n);
        ref double p = ref MM.GetArrayDataReference(re);
        ref double q = ref MM.GetArrayDataReference(im);
        ref double r = ref MM.GetArrayDataReference(result);
        int i = 0;
        if (Avx.IsSupported)
            for (int top = n & Simd.MASK4; i < top; i += V4d.Count)
            {
                V4d v = V4.LoadUnsafe(ref Add(ref p, i));
                V4d w = V4.LoadUnsafe(ref Add(ref q, i));
                V4.StoreUnsafe(Avx.Sqrt((v * v).MultiplyAdd(w, w)), ref Add(ref r, i));
            }
        for (; i < n; i++)
            Add(ref r, i) = Sqrt(Add(ref p, i) * Add(ref p, i) + Add(ref q, i) * Add(ref q, i));
        return result;
    }

    /// <summary>
    /// Gets a vector containing the magnitudes of the complex numbers in this vector.
    /// </summary>
    /// <returns>A new vector with magnitudes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector Magnitudes()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == Length);
        return Magnitudes(Length);
    }

    /// <summary>Gets maximum absolute magnitude in the vector.</summary>
    /// <remarks>This operation can be hardware-accelerated.</remarks>
    /// <returns>The value in the cell with the highest amplitude.</returns>
    public double AbsMax()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<double>() >= 0);

        ref double p = ref MM.GetArrayDataReference(re);
        ref double q = ref MM.GetArrayDataReference(im);
        int i = 0;
        double result = 0;
        if (Avx.IsSupported)
        {
            V4d max = V4d.Zero;
            for (int top = Length & Simd.MASK4; i < top; i += 4)
            {
                V4d v = V4.LoadUnsafe(ref Add(ref p, i));
                V4d w = V4.LoadUnsafe(ref Add(ref q, i));
                max = Avx.Max(max, Avx.Sqrt((v * v).MultiplyAdd(w, w)));
            }
            result = max.Max();
        }
        for (; i < re.Length; i++)
            result = Max(result, Sqrt(re[i] * re[i] + im[i] * im[i]));
        return result;
    }

    /// <summary>Gets minimum absolute magnitude in the vector.</summary>
    /// <remarks>This operation can be hardware-accelerated.</remarks>
    /// <returns>The value in the cell with the smaller amplitude.</returns>
    public double AbsMin()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<double>() >= 0);

        ref double p = ref MM.GetArrayDataReference(re);
        ref double q = ref MM.GetArrayDataReference(im);
        int i = 0;
        double result = 0;
        if (Avx.IsSupported)
        {
            V4d min = V4.Create(double.PositiveInfinity);
            for (int top = Length & Simd.MASK4; i < top; i += 4)
            {
                V4d v = V4.LoadUnsafe(ref Add(ref p, i));
                V4d w = V4.LoadUnsafe(ref Add(ref q, i));
                min = Avx.Min(min, Avx.Sqrt((v * v).MultiplyAdd(w, w)));
            }
            result = min.Min();
        }
        for (; i < re.Length; i++)
            result = Min(result, Sqrt(re[i] * re[i] + im[i] * im[i]));
        return result;
    }

    /// <summary>
    /// Gets a vector containing the phases of the complex numbers in this vector.
    /// </summary>
    /// <remarks>This operation can be hardware-accelerated.</remarks>
    /// <param name="n">The number of phases to be returned.</param>
    /// <returns>A new vector with phases.</returns>
    internal DVector Phases(int n)
    {
        double[] result = GC.AllocateUninitializedArray<double>(n);
        ref double p = ref MM.GetArrayDataReference(re);
        ref double q = ref MM.GetArrayDataReference(im);
        ref double r = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= V8d.Count)
        {
            int t = result.Length - V8d.Count;
            for (int i = 0; i < t; i += V8d.Count)
                V8.StoreUnsafe(
                    V8.LoadUnsafe(ref Add(ref q, i)).Atan2(V8.LoadUnsafe(ref Add(ref p, i))),
                    ref Add(ref r, i));
            V8.StoreUnsafe(
                V8.LoadUnsafe(ref Add(ref q, t)).Atan2(V8.LoadUnsafe(ref Add(ref p, t))),
                ref Add(ref r, t));
        }
        else if (V4.IsHardwareAccelerated && result.Length >= V4d.Count)
        {
            int t = result.Length - V4d.Count;
            for (int i = 0; i < t; i += V4d.Count)
                V4.StoreUnsafe(
                    V4.LoadUnsafe(ref Add(ref q, i)).Atan2(V4.LoadUnsafe(ref Add(ref p, i))),
                    ref Add(ref r, i));
            V4.StoreUnsafe(
                V4.LoadUnsafe(ref Add(ref q, t)).Atan2(V4.LoadUnsafe(ref Add(ref p, t))),
                ref Add(ref r, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                result[i] = Atan2(im[i], re[i]);
        return result;
    }

    /// <summary>
    /// Gets a vector containing the phases of the complex numbers in this vector.
    /// </summary>
    /// <remarks>This operation can be hardware-accelerated.</remarks>
    /// <returns>A new vector with phases.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector Phases()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == Length);
        return Phases(Length);
    }

    /// <summary>Checks if the vector contains the given value.</summary>
    /// <param name="value">Value to locate.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Complex value) => IndexOf(value) != -1;

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    public int IndexOf(Complex value) => IndexOf(value, 0);

    /// <summary>Returns the zero-based index of the next occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <param name="from">The zero-based index at which search starts.</param>
    /// <returns>Index of the next ocurrence, if found; <c>-1</c>, otherwise.</returns>
    public int IndexOf(Complex value, int from)
    {
        DVector r = new(re);
        for (int i = r.IndexOf(value.Real, from); i != -1; i = r.IndexOf(value.Real, i + 1))
            if (im[i] == value.Imaginary)
                return i;
        return -1;
    }

    /// <summary>Returns all indexes containing ocurrences of a value.</summary>
    /// <param name="value">Value to find.</param>
    /// <returns>An integer sequences with all found indexes.</returns>
    public NSequence Find(Complex value) =>
        NSequence.Iterate((Complex[])this, value);

    /// <summary>Returns all indexes satisfying a condition.</summary>
    /// <param name="condition">The condition to be satisfied.</param>
    /// <returns>An integer sequences with all found indexes.</returns>
    public NSequence Find(Func<Complex, bool> condition) =>
        NSequence.Iterate((Complex[])this, condition);

    /// <summary>
    /// Creates a new vector by transforming each item with the given function.
    /// </summary>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new vector with transformed content.</returns>
    public CVector Map(Func<Complex, Complex> mapper)
    {
        double[] newRe = GC.AllocateUninitializedArray<double>(Length);
        double[] newIm = GC.AllocateUninitializedArray<double>(Length);
        for (int i = 0; i < re.Length; i++)
            (newRe[i], newIm[i]) = mapper(new(re[i], im[i]));
        return new(newRe, newIm);
    }

    /// <summary>
    /// Creates a real vector by transforming each item with the given function.
    /// </summary>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A real vector with the transformed content.</returns>
    public DVector MapReal(Func<Complex, double> mapper)
    {
        double[] newValues = GC.AllocateUninitializedArray<double>(Length);
        for (int i = 0; i < re.Length; i++)
            newValues[i] = mapper(new(re[i], im[i]));
        return new(newValues);
    }

    /// <summary>Checks whether the predicate is satisfied by all items.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if all items satisfy the predicate.</returns>
    public bool All(Func<Complex, bool> predicate)
    {
        foreach (Complex value in this)
            if (!predicate(value))
                return false;
        return true;
    }

    /// <summary>Checks whether the predicate is satisfied by at least one item.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if there exists a item satisfying the predicate.</returns>
    public bool Any(Func<Complex, bool> predicate)
    {
        foreach (Complex value in this)
            if (predicate(value))
                return true;
        return false;
    }

    /// <summary>Creates a new vector by filtering items with the given predicate.</summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>A new vector with the filtered items.</returns>
    public CVector Filter(Func<Complex, bool> predicate)
    {
        double[] newRe = GC.AllocateUninitializedArray<double>(Length);
        double[] newIm = GC.AllocateUninitializedArray<double>(Length);
        int j = 0;
        foreach (Complex value in this)
            if (predicate(value))
            {
                (newRe[j], newIm[j]) = value;
                j++;
            }
        return j == 0 ? new([], []) : j == Length ? this : new(newRe[..j], newIm[..j]);
    }

    /// <summary>Creates a new vector by filtering and mapping at the same time.</summary>
    /// <remarks>This method can save an intermediate buffer and one iteration.</remarks>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new vector with the filtered items.</returns>
    public CVector FilterMap(Func<Complex, bool> predicate, Func<Complex, Complex> mapper)
    {
        double[] newRe = GC.AllocateUninitializedArray<double>(Length);
        double[] newIm = GC.AllocateUninitializedArray<double>(Length);
        int j = 0;
        foreach (Complex value in this)
            if (predicate(value))
            {
                (newRe[j], newIm[j]) = mapper(value);
                j++;
            }
        return j == 0 ? new([], []) : j == Length ? this : new(newRe[..j], newIm[..j]);
    }

    /// <summary>Creates an aggregate value by applying the reducer to each item.</summary>
    /// <param name="seed">The initial value.</param>
    /// <param name="reducer">The reducing function.</param>
    /// <returns>The final synthesized value.</returns>
    public Complex Reduce(Complex seed, Func<Complex, Complex, Complex> reducer)
    {
        foreach (Complex value in this)
            seed = reducer(seed, value);
        return seed;
    }

    /// <summary>Combines the common prefix of two vectors.</summary>
    /// <param name="other">Second vector to combine.</param>
    /// <param name="zipper">The combining function.</param>
    /// <returns>The combining function applied to each pair of items.</returns>
    public CVector Zip(CVector other, Func<Complex, Complex, Complex> zipper)
    {
        int len = Min(Length, other.Length);
        double[] newRe = new double[len], newIm = new double[len];
        ref double p1r = ref MM.GetArrayDataReference(re);
        ref double p1i = ref MM.GetArrayDataReference(im);
        ref double p2r = ref MM.GetArrayDataReference(other.re);
        ref double p2i = ref MM.GetArrayDataReference(other.im);
        ref double qr = ref MM.GetArrayDataReference(newRe);
        ref double qi = ref MM.GetArrayDataReference(newIm);
        for (int i = 0; i < len; i++)
            (Add(ref qr, i), Add(ref qi, i)) = zipper(
                new(Add(ref p1r, i), Add(ref p1i, i)), new(Add(ref p2r, i), Add(ref p2i, i)));
        return new(newRe, newIm);
    }

    /// <summary>Computes the real discrete Fourier transform.</summary>
    /// <returns>The spectrum.</returns>
    public FftCModel Fft()
    {
        Complex[] values = (Complex[])this;
        FFT.Transform(values);
        return new(values);
    }

    /// <summary>Retrieves an enumerator to iterate over components.</summary>
    /// <returns>The enumerator from the underlying arrays.</returns>
    public IEnumerator<Complex> GetEnumerator()
    {
        for (int i = 0; i < re.Length; i++)
            yield return new(re[i], im[i]);
    }

    /// <summary>Retrieves an enumerator to iterate over components.</summary>
    /// <returns>The enumerator from the underlying array.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <returns>Space-separated components.</returns>
    public override string ToString() =>
        $"ans ∊ ℂ({Length})" + Environment.NewLine +
        ((Complex[])this).ToString(v => v.ToString("G6"));

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        $"ans ∊ ℂ({Length})" + Environment.NewLine +
        ((Complex[])this).ToString(v => v.ToString(format, provider));

    /// <summary>Checks if the provided argument is a vector with the same values.</summary>
    /// <param name="other">The vector to be compared.</param>
    /// <returns><see langword="true"/> if the vector argument has the same items.</returns>
    public bool Equals(CVector other) => re.Eqs(other.re) && im.Eqs(other.im);

    /// <summary>Checks if the provided argument is a complex vector with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a vector with the same items.</returns>
    public override bool Equals(object? obj) => obj is CVector vector && Equals(vector);

    /// <summary>Returns the hashcode for this complex vector.</summary>
    /// <returns>A hashcode summarizing the content of the vector.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(
            ((IStructuralEquatable)re).GetHashCode(EqualityComparer<double>.Default),
            ((IStructuralEquatable)im).GetHashCode(EqualityComparer<double>.Default));

    /// <summary>Compares two complex vectors for equality. </summary>
    /// <param name="left">First vector operand.</param>
    /// <param name="right">Second vector operand.</param>
    /// <returns><see langword="true"/> if all corresponding items are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(CVector left, CVector right) => left.Equals(right);

    /// <summary>Compares two complex vectors for inequality. </summary>
    /// <param name="left">First vector operand.</param>
    /// <param name="right">Second vector operand.</param>
    /// <returns><see langword="true"/> if any pair of corresponding items are not equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(CVector left, CVector right) => !left.Equals(right);

    /// <summary>Creates a plot for this vector.</summary>
    /// <returns>A plot containing this vector as its dataset.</returns>
    public Plot<CVector> Plot() => new(this);
}
