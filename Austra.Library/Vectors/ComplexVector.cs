namespace Austra.Library;

using Austra.Library.Stats;
using static Unsafe;

/// <summary>Represents a dense complex vector of arbitrary size.</summary>
/// <remarks>
/// For the sake of acceleration, the vector's components are stored in two separate arrays.
/// Most operations are non-destructive.
/// </remarks>
public readonly struct ComplexVector :
    IFormattable,
    IEnumerable<Complex>,
    IEquatable<ComplexVector>,
    IEqualityOperators<ComplexVector, ComplexVector, bool>,
    IAdditionOperators<ComplexVector, ComplexVector, ComplexVector>,
    IAdditionOperators<ComplexVector, Complex, ComplexVector>,
    IAdditionOperators<ComplexVector, double, ComplexVector>,
    ISubtractionOperators<ComplexVector, ComplexVector, ComplexVector>,
    ISubtractionOperators<ComplexVector, Complex, ComplexVector>,
    ISubtractionOperators<ComplexVector, double, ComplexVector>,
    IMultiplyOperators<ComplexVector, ComplexVector, Complex>,
    IMultiplyOperators<ComplexVector, Complex, ComplexVector>,
    IMultiplyOperators<ComplexVector, double, ComplexVector>,
    IUnaryNegationOperators<ComplexVector, ComplexVector>,
    IPointwiseOperators<ComplexVector>, ISafeIndexed, IVector
{
    /// <summary>Stores the real components of the vector.</summary>
    private readonly double[] re;
    /// <summary>Stores the imaginary components of the vector.</summary>
    private readonly double[] im;

    /// <summary>Creates a complex vector of a given size.</summary>
    /// <param name="size">Vector length.</param>
    public ComplexVector(int size) => (re, im) = (new double[size], new double[size]);

    /// <summary>Creates a complex vector for a complex value.</summary>
    /// <param name="value">A complex value.</param>
    public ComplexVector(Complex value) =>
        (re, im) = (new[] { value.Real }, new[] { value.Imaginary });

    /// <summary>Creates a complex vector for a real value.</summary>
    /// <param name="value">A real value.</param>
    public ComplexVector(double value) => (re, im) = (new[] { value }, new double[1]);

    /// <summary>Creates a complex vector from two complex values.</summary>
    /// <param name="v1">First complex value.</param>
    /// <param name="v2">Second complex value.</param>
    public ComplexVector(Complex v1, Complex v2) =>
        (re, im) = (new[] { v1.Real, v2.Real }, new[] { v1.Imaginary, v2.Imaginary });

    /// <summary>Creates a complex vector from two real values.</summary>
    /// <param name="v1">A real value.</param>
    /// <param name="v2">Second value.</param>
    public ComplexVector(double v1, double v2) => (re, im) = (new[] { v1, v2 }, new double[2]);

    internal ComplexVector(Complex c1, Complex c2, Complex c3) => (re, im) = (
        new[] { c1.Real, c2.Real, c3.Real },
        new[] { c1.Imaginary, c2.Imaginary, c3.Imaginary });

    /// <summary>Creates a complex vector from separate component arrays.</summary>
    /// <param name="re">The real components of the vector.</param>
    /// <param name="im">The imaginary components of the vector.</param>
    public ComplexVector(double[] re, double[] im)
    {
        if (re.Length != im.Length)
            throw new VectorLengthException();
        (this.re, this.im) = (re, im);
    }

    /// <summary>Creates a complex vector from two real vectors.</summary>
    /// <param name="re">The real components of the vector.</param>
    /// <param name="im">The imaginary components of the vector.</param>
    public ComplexVector(Vector re, Vector im) : this((double[])re, (double[])im) { }

    /// <summary>Creates a complex vector from a real vector.</summary>
    /// <param name="re">The real components of the vector.</param>
    public ComplexVector(Vector re) : this((double[])re, new double[re.Length]) { }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="rnd">A random number generator.</param>
    /// <param name="offset">An offset for the random numbers.</param>
    /// <param name="width">Width for the uniform distribution.</param>
    public unsafe ComplexVector(int size, Random rnd, double offset, double width) : this(size)
    {
        fixed (double* p = re, q = im)
            for (int i = 0; i < size; i++)
            {
                p[i] = FusedMultiplyAdd(rnd.NextDouble(), width, offset);
                q[i] = FusedMultiplyAdd(rnd.NextDouble(), width, offset);
            }
    }

    /// <summary>Creates a vector filled with a uniform distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="rnd">A random number generator.</param>
    public ComplexVector(int size, Random rnd)
    {
        re = GC.AllocateUninitializedArray<double>(size);
        im = GC.AllocateUninitializedArray<double>(size);
        ref double p = ref MemoryMarshal.GetArrayDataReference(re);
        ref double q = ref MemoryMarshal.GetArrayDataReference(im);
        for (int i = 0; i < size; i++)
            (Add(ref p, i), Add(ref q, i)) = (rnd.NextDouble(), rnd.NextDouble());
    }

    /// <summary>Creates a vector filled with a normal distribution generator.</summary>
    /// <param name="size">Size of the vector.</param>
    /// <param name="rnd">A normal random number generator.</param>
    public ComplexVector(int size, NormalRandom rnd)
    {
        re = GC.AllocateUninitializedArray<double>(size);
        im = GC.AllocateUninitializedArray<double>(size);
        ref double p = ref MemoryMarshal.GetArrayDataReference(re);
        ref double q = ref MemoryMarshal.GetArrayDataReference(im);
        for (int i = 0; i < size; i++)
            (Add(ref p, i), Add(ref q, i)) = rnd.NextDoubles();
    }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public unsafe ComplexVector(int size, Func<int, Complex> f) : this(size)
    {
        fixed (double* p = re, q = im)
            for (int i = 0; i < size; i++)
                (p[i], q[i]) = f(i);
    }

    /// <summary>Creates a vector using a formula to fill its items.</summary>
    /// <param name="size">The size of the vector.</param>
    /// <param name="f">A function defining item content.</param>
    public unsafe ComplexVector(int size, Func<int, ComplexVector, Complex> f) : this(size)
    {
        fixed (double* p = re, q = im)
            for (int i = 0; i < size; i++)
                (p[i], q[i]) = f(i, this);
    }

    /// <summary>Initializes a complex vector from a complex array.</summary>
    /// <param name="values">The complex components of the vector.</param>
    public ComplexVector(Complex[] values) : this(values.Length)
    {
        ref double p = ref MemoryMarshal.GetArrayDataReference(re);
        ref double q = ref MemoryMarshal.GetArrayDataReference(im);
        if (Vector256.IsHardwareAccelerated)
        {
            ref double r = ref As<Complex, double>(ref MemoryMarshal.GetArrayDataReference(values));
            int t = values.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
            {
                Vector256<double> v1 = Vector256.LoadUnsafe(ref Add(ref r, 2 * i));
                Vector256<double> v2 = Vector256.LoadUnsafe(ref Add(ref r, 2 * i + 4));
                Vector256.StoreUnsafe(Avx2.Permute4x64(Avx.UnpackLow(v1, v2), 0b11011000),
                    ref Add(ref p, i));
                Vector256.StoreUnsafe(Avx2.Permute4x64(Avx.UnpackHigh(v1, v2), 0b11011000),
                    ref Add(ref q, i));
            }
            Vector256<double> v3 = Vector256.LoadUnsafe(ref Add(ref r, 2 * t));
            Vector256<double> v4 = Vector256.LoadUnsafe(ref Add(ref r, 2 * t + 4));
            Vector256.StoreUnsafe(Avx2.Permute4x64(Avx.UnpackLow(v3, v4), 0b11011000),
                ref Add(ref p, t));
            Vector256.StoreUnsafe(Avx2.Permute4x64(Avx.UnpackHigh(v3, v4), 0b11011000),
                ref Add(ref q, t));
        }
        else
        {
            ref Complex r = ref MemoryMarshal.GetArrayDataReference(values);
            for (int i = 0; i < values.Length; i++)
                (Add(ref p, i), Add(ref q, i)) = Add(ref r, i);
        }
    }

    /// <summary>Deconstructs the vector into a tuple of real and imaginary vectors.</summary>
    /// <param name="re">The real vector.</param>
    /// <param name="im">The imaginary vector.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out Vector re, out Vector im) => (re, im) = (this.re, this.im);

    /// <summary>Creates an identical vector.</summary>
    /// <remarks>A new copy of the original storage is created.</remarks>
    /// <returns>A deep clone of the instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComplexVector Clone() => new((double[])re.Clone(), (double[])im.Clone());

    /// <summary>Explicit conversion from vector to array.</summary>
    /// <param name="v">The original vector.</param>
    /// <returns>An array of <see cref="Complex"/> numbers.</returns>
    public unsafe static explicit operator Complex[](ComplexVector v)
    {
        Complex[] result = new Complex[v.Length];
        fixed (double* p = v.re, q = v.im)
        fixed (Complex* r = result)
        {
            int i = 0;
            if (Avx2.IsSupported)
            {
                for (int top = v.Length & ~3; i < top; i += 4)
                {
                    Vector256<double> vr = Avx.LoadVector256(p + i);
                    Vector256<double> vi = Avx.LoadVector256(q + i);
                    Avx.Store((double*)(r + i), Avx2.Permute4x64(Avx.Permute2x128(
                        vr, vi, 0b0010_0000), 0b11_01_10_00));
                    Avx.Store((double*)(r + i + 2), Avx2.Permute4x64(Avx.Permute2x128(
                        vr, vi, 0b0011_0001), 0b11_01_10_00));
                }
            }
            for (; i < result.Length; i++)
                r[i] = new(p[i], q[i]);
        }
        return result;
    }

    /// <summary>Creates a reversed copy of the vector.</summary>
    /// <returns>An independent reversed copy.</returns>
    public ComplexVector Reverse()
    {
        ComplexVector result = Clone();
        Array.Reverse(result.re);
        Array.Reverse(result.im);
        return result;
    }

    /// <summary>Returns a new vector with the distinct values in the original one.</summary>
    /// <remarks>Results are unordered.</remarks>
    /// <returns>A new vector with distinct values.</returns>
    public ComplexVector Distinct()
    {
        HashSet<Complex> set = new(Length);
        foreach (Complex value in this)
            set.Add(value);
        return new(set.ToArray());
    }

    /// <summary>Creates a new complex vector with conjugated values.</summary>
    /// <returns>Each item with the sign of the imaginary value inverted.</returns>
    public ComplexVector Conjugate() => new(re, -new Vector(im));

    /// <summary>Gets the first complex in the vector.</summary>
    public Complex First => new(re[0], im[0]);
    /// <summary>Gets the last complex in the vector.</summary>
    public Complex Last => new(re[^1], im[^1]);

    /// <summary>Gets the dimensions of the vector.</summary>
    public int Length => re.Length;

    /// <summary>Has the vector been properly initialized?</summary>
    /// <remarks>
    /// Since <see cref="ComplexVector"/> is a struct, its default constructor doesn't
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
                Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(re), index),
                Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(im), index))
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
    public ComplexVector this[Range range]
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
            Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(re), index),
            Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(im), index))
        : Complex.Zero;

    /// <summary>Adds two complex vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The component by component sum.</returns>
    public static ComplexVector operator +(ComplexVector v1, ComplexVector v2) => new(
        new Vector(v1.re) + new Vector(v2.re),
        new Vector(v1.im) + new Vector(v2.im));

    /// <summary>Subtracts two complex vectors.</summary>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The component by component sum.</returns>
    public static ComplexVector operator -(ComplexVector v1, ComplexVector v2) => new(
        new Vector(v1.re) - new Vector(v2.re),
        new Vector(v1.im) - new Vector(v2.im));

    /// <summary>Negates a vector.</summary>
    /// <param name="v">The vector operand.</param>
    /// <returns>The item-wise negation.</returns>
    public static ComplexVector operator -(ComplexVector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == v.Length);

        return new(-new Vector(v.re), -new Vector(v.im));
    }

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="v">A vector summand.</param>
    /// <param name="c">A scalar summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    public static ComplexVector operator +(ComplexVector v, Complex c)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<ComplexVector>().Length == v.Length);

        return new(new Vector(v.re) + c.Real, new Vector(v.im) + c.Imaginary);
    }

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="v">A vector summand.</param>
    /// <param name="d">A scalar summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    public static ComplexVector operator +(ComplexVector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<ComplexVector>().Length == v.Length);

        return new(new Vector(v.re) + d, new Vector(v.im));
    }

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="c">A scalar summand.</param>
    /// <param name="v">A vector summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComplexVector operator +(Complex c, ComplexVector v) => v + c;

    /// <summary>Adds a scalar to a vector.</summary>
    /// <param name="d">A scalar summand.</param>
    /// <param name="v">A vector summand.</param>
    /// <returns>The scalar is added to each vector's item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComplexVector operator +(double d, ComplexVector v) => v + d;

    /// <summary>Subtracts a scalar to a vector.</summary>
    /// <param name="v">The vector minuend.</param>
    /// <param name="c">The scalar subtrahend.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static ComplexVector operator -(ComplexVector v, Complex c)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<ComplexVector>().Length == v.Length);

        return new(new Vector(v.re) - c.Real, new Vector(v.im) - c.Imaginary);
    }

    /// <summary>Subtracts a scalar to a vector.</summary>
    /// <param name="v">The vector minuend.</param>
    /// <param name="d">The scalar subtrahend.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static ComplexVector operator -(ComplexVector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<ComplexVector>().Length == v.Length);

        return new(new Vector(v.re) - d, new Vector(v.im));
    }

    /// <summary>Subtracts a vector from a scalar.</summary>
    /// <param name="c">The scalar minuend.</param>
    /// <param name="v">The vector subtrahend.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static ComplexVector operator -(Complex c, ComplexVector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<ComplexVector>().Length == v.Length);

        return new(c.Real - new Vector(v.re), c.Imaginary - new Vector(v.im));
    }

    /// <summary>Subtracts a vector from a scalar.</summary>
    /// <param name="d">The scalar minuend.</param>
    /// <param name="v">The vector subtrahend.</param>
    /// <returns>The scalar is subtracted from each vector's item.</returns>
    public static ComplexVector operator -(double d, ComplexVector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<ComplexVector>().Length == v.Length);

        return new(d - new Vector(v.re), -new Vector(v.im));
    }

    /// <summary>Pointwise multiplication.</summary>
    /// <param name="other">Second vector operand.</param>
    /// <returns>The component by component product.</returns>
    public ComplexVector PointwiseMultiply(ComplexVector other)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(other.IsInitialized);
        if (Length != other.Length)
            throw new VectorLengthException();
        Contract.Ensures(Contract.Result<Vector>().Length == Length);

        double[] r = new double[Length], m = new double[Length];
        ref double pr = ref MemoryMarshal.GetArrayDataReference(re);
        ref double pi = ref MemoryMarshal.GetArrayDataReference(im);
        ref double qr = ref MemoryMarshal.GetArrayDataReference(other.re);
        ref double qi = ref MemoryMarshal.GetArrayDataReference(other.im);
        ref double vr = ref MemoryMarshal.GetArrayDataReference(r);
        ref double vm = ref MemoryMarshal.GetArrayDataReference(m);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = r.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
            {
                Vector256<double> vpr = Vector256.LoadUnsafe(ref Add(ref pr, i));
                Vector256<double> vpi = Vector256.LoadUnsafe(ref Add(ref pi, i));
                Vector256<double> vqr = Vector256.LoadUnsafe(ref Add(ref qr, i));
                Vector256<double> vqi = Vector256.LoadUnsafe(ref Add(ref qi, i));
                Vector256.StoreUnsafe((vpr * vqr).MultiplyAddNeg(vpi, vqi), ref Add(ref vr, i));
                Vector256.StoreUnsafe((vpr * vqi).MultiplyAdd(vpi, vqr), ref Add(ref vm, i));
            }
            Vector256<double> wpr = Vector256.LoadUnsafe(ref Add(ref pr, t));
            Vector256<double> wpi = Vector256.LoadUnsafe(ref Add(ref pi, t));
            Vector256<double> wqr = Vector256.LoadUnsafe(ref Add(ref qr, t));
            Vector256<double> wqi = Vector256.LoadUnsafe(ref Add(ref qi, t));
            Vector256.StoreUnsafe((wpr * wqr).MultiplyAddNeg(wpi, wqi), ref Add(ref vr, t));
            Vector256.StoreUnsafe((wpr * wqi).MultiplyAdd(wpi, wqr), ref Add(ref vm, t));
        }
        else
            for (int i = 0; i < r.Length; i++)
            {
                Add(ref vr, i) = Add(ref pr, i) * Add(ref qr, i) - Add(ref pi, i) * Add(ref qi, i);
                Add(ref vm, i) = Add(ref pr, i) * Add(ref qi, i) + Add(ref pi, i) * Add(ref qr, i);
            }
        return new(r, m);
    }

    /// <summary>Pointwise division.</summary>
    /// <param name="other">Second vector operand.</param>
    /// <returns>The component by component quotient.</returns>
    public unsafe ComplexVector PointwiseDivide(ComplexVector other)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(other.IsInitialized);
        if (Length != other.Length)
            throw new VectorLengthException();
        Contract.Ensures(Contract.Result<Vector>().Length == Length);

        int len = Length;
        double[] r = new double[len], m = new double[len];
        fixed (double* pr = re, pi = im, qr = other.re, qi = other.im, vr = r, vm = m)
        {
            int i = 0;
            if (Avx.IsSupported)
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                {
                    Vector256<double> vpr = Avx.LoadVector256(pr + i);
                    Vector256<double> vpi = Avx.LoadVector256(pi + i);
                    Vector256<double> vqr = Avx.LoadVector256(qr + i);
                    Vector256<double> vqi = Avx.LoadVector256(qi + i);
                    Vector256<double> quotient = Avx.Multiply(vqr, vqr).MultiplyAdd(vqi, vqi);
                    Avx.Store(vr + i,
                        Avx.Divide(Avx.Multiply(vpr, vqr).MultiplyAdd(vpi, vqi), quotient));
                    Avx.Store(vm + i,
                        Avx.Divide(Avx.Multiply(vpr, vqi).MultiplyAddNeg(vpi, vqr), quotient));
                }
            for (; i < len; i++)
                (r[i], m[i]) = new Complex(pr[i], pi[i]) / new Complex(qr[i], qi[i]);
        }
        return new(r, m);
    }

    /// <summary>Dot product of two vectors.</summary>
    /// <remarks>The values from the second vector are automatically conjugated.</remarks>
    /// <param name="v1">First vector operand.</param>
    /// <param name="v2">Second vector operand.</param>
    /// <returns>The dot product of the operands.</returns>
    public static unsafe Complex operator *(ComplexVector v1, ComplexVector v2)
    {
        Contract.Requires(v1.IsInitialized);
        Contract.Requires(v2.IsInitialized);
        if (v1.Length != v2.Length)
            throw new VectorLengthException();
        Contract.EndContractBlock();

        fixed (double* pr = v1.re, pi = v1.im, qr = v2.re, qi = v2.im)
        {
            double sumRe = 0, sumIm = 0;
            int i = 0, size = v1.Length;
            if (Avx.IsSupported)
            {
                Vector256<double> accRe = Vector256<double>.Zero;
                Vector256<double> accIm = Vector256<double>.Zero;
                for (int top = size & Simd.AVX_MASK; i < top; i += 4)
                {
                    Vector256<double> vpr = Avx.LoadVector256(pr + i);
                    Vector256<double> vpi = Avx.LoadVector256(pi + i);
                    Vector256<double> vqr = Avx.LoadVector256(qr + i);
                    Vector256<double> vqi = Avx.LoadVector256(qi + i);
                    accRe = Avx.Add(accRe, Avx.Multiply(vpr, vqr).MultiplyAdd(vpi, vqi));
                    accIm = Avx.Add(accIm, Avx.Multiply(vpi, vqr).MultiplySub(vpr, vqi));
                }
                sumRe = accRe.Sum();
                sumIm = accIm.Sum();
            }
            for (; i < size; i++)
            {
                sumRe += pr[i] * qr[i] + pi[i] * qi[i];
                sumIm += pi[i] * qr[i] - pr[i] * qi[i];
            }
            return new(sumRe, sumIm);
        }
    }

    /// <summary>Gets the squared norm of this vector.</summary>
    /// <returns>The dot product with itself.</returns>
    public unsafe double Squared()
    {
        Contract.Requires(IsInitialized);

        fixed (double* p = re, q = im)
        {
            double sum = 0;
            int i = 0;
            if (Avx.IsSupported)
            {
                Vector256<double> acc = Vector256<double>.Zero;
                for (int top = Length & Simd.AVX_MASK; i < top; i += 4)
                {
                    Vector256<double> v = Avx.LoadVector256(p + i);
                    Vector256<double> w = Avx.LoadVector256(q + i);
                    acc = Avx.Add(acc, Avx.Multiply(v, v).MultiplyAdd(w, w));
                }
                sum = acc.Sum();
            }
            for (; i < Length; i++)
                sum += re[i] * re[i] + im[i] * im[i];
            return sum;
        }
    }

    /// <summary>Gets the Euclidean norm of this vector.</summary>
    /// <returns>The squared root of the dot product.</returns>
    public double Norm() => Sqrt(Squared());

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="v">Vector to be multiplied.</param>
    /// <param name="c">A scalar multiplier.</param>
    /// <returns>The multiplication of the vector by the scalar.</returns>
    public static unsafe ComplexVector operator *(ComplexVector v, Complex c)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == v.Length);

        double[] re = new double[v.Length], im = new double[v.Length];
        fixed (double* pr = v.re, pi = v.im, qr = re, qi = im)
        {
            int len = v.Length;
            int i = 0;
            if (Avx.IsSupported)
            {
                Vector256<double> vr = Vector256.Create(c.Real);
                Vector256<double> vi = Vector256.Create(c.Imaginary);
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                {
                    Vector256<double> vpr = Avx.LoadVector256(pr + i);
                    Vector256<double> vpi = Avx.LoadVector256(pi + i);
                    Avx.Store(qr + i, Avx.Multiply(vpr, vr).MultiplyAddNeg(vpi, vi));
                    Avx.Store(qi + i, Avx.Multiply(vpr, vi).MultiplyAdd(vpi, vr));
                }
            }
            for (; i < len; i++)
            {
                qr[i] = pr[i] * c.Real - pi[i] * c.Imaginary;
                qi[i] = pr[i] * c.Imaginary + pi[i] * c.Real;
            }
        }
        return new(re, im);
    }

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="v">Vector to be multiplied.</param>
    /// <param name="d">A scalar multiplier.</param>
    /// <returns>The multiplication of the vector by the scalar.</returns>
    public static unsafe ComplexVector operator *(ComplexVector v, double d)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == v.Length);

        return new(new Vector(v.re) * d, new Vector(v.im) * d);
    }

    /// <summary>Divides a complex vector by a complex value.</summary>
    /// <param name="v">Vector to be divided.</param>
    /// <param name="c">A complex scalar divisor.</param>
    /// <returns>The division of the vector by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComplexVector operator /(ComplexVector v, Complex c) =>
        v * Complex.Reciprocal(c);

    /// <summary>Divides a complex vector by a scalar.</summary>
    /// <param name="v">Vector to be divided.</param>
    /// <param name="d">A scalar divisor.</param>
    /// <returns>The division of the vector by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComplexVector operator /(ComplexVector v, double d) =>
        v * (1.0 / d);

    /// <summary>Multiplies a complex scalar value by a vector.</summary>
    /// <param name="c">A complex scalar multiplier.</param>
    /// <param name="v">Vector to be multiplied.</param>
    /// <returns>The multiplication of the vector by the complex scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComplexVector operator *(Complex c, ComplexVector v) => v * c;

    /// <summary>Multiplies a real scalar value by a vector.</summary>
    /// <param name="d">A real scalar multiplier.</param>
    /// <param name="v">Vector to be multiplied.</param>
    /// <returns>The multiplication of the vector by the real scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComplexVector operator *(double d, ComplexVector v) => v * d;

    /// <summary>Calculates the sum of the vector's items.</summary>
    /// <returns>The sum of all vector's items.</returns>
    public Complex Sum()
    {
        Contract.Requires(IsInitialized);

        return new(new Vector(re).Sum(), new Vector(im).Sum());
    }

    /// <summary>Computes the mean of the vector's items.</summary>
    /// <returns><code>this.Sum() / this.Length</code></returns>
    public Complex Mean() => Sum() / Length;

    /// <summary>Gets the real part of the complex numbers in this vector.</summary>
    public Vector Real => new(re);

    /// <summary>Gets the imaginary part of the complex numbers in this vector.</summary>
    public Vector Imaginary => new(im);

    /// <summary>
    /// Gets a vector containing the magnitudes of the complex numbers in this vector.
    /// </summary>
    /// <param name="n">The number of amplitudes to be returned.</param>
    /// <returns>A new vector with magnitudes.</returns>
    internal unsafe Vector Magnitudes(int n)
    {
        double[] result = GC.AllocateUninitializedArray<double>(n);
        fixed (double* p = re, q = im, r = result)
        {
            int i = 0;
            if (Avx.IsSupported)
                for (int top = n & Simd.AVX_MASK; i < top; i += 4)
                {
                    Vector256<double> v = Avx.LoadVector256(p + i);
                    Vector256<double> w = Avx.LoadVector256(q + i);
                    Avx.Store(r + i, Avx.Sqrt(Avx.Multiply(v, v).MultiplyAdd(w, w)));
                }
            for (; i < n; i++)
                r[i] = Sqrt(re[i] * re[i] + im[i] * im[i]);
            return result;
        }
    }

    /// <summary>
    /// Gets a vector containing the magnitudes of the complex numbers in this vector.
    /// </summary>
    /// <returns>A new vector with magnitudes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Magnitudes()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == Length);

        return Magnitudes(Length);
    }

    /// <summary>Gets maximum absolute magnitude in the vector.</summary>
    /// <remarks>This operation can be hardware-accelerated.</remarks>
    /// <returns>The value in the cell with the highest amplitude.</returns>
    public unsafe double AbsMax()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<double>() >= 0);

        fixed (double* p = re, q = im)
        {
            int i = 0;
            double result = 0;
            if (Avx.IsSupported)
            {
                Vector256<double> max = Vector256<double>.Zero;
                for (int top = Length & Simd.AVX_MASK; i < top; i += 4)
                {
                    Vector256<double> v = Avx.LoadVector256(p + i);
                    Vector256<double> w = Avx.LoadVector256(q + i);
                    max = Avx.Max(max, Avx.Sqrt(Avx.Multiply(v, v).MultiplyAdd(w, w)));
                }
                result = max.Max();
            }
            for (; i < Length; i++)
                result = Max(result, Sqrt(re[i] * re[i] + im[i] * im[i]));
            return result;
        }
    }

    /// <summary>
    /// Gets a vector containing the phases of the complex numbers in this vector.
    /// </summary>
    /// <remarks>This operation can be hardware-accelerated.</remarks>
    /// <param name="n">The number of phases to be returned.</param>
    /// <returns>A new vector with phases.</returns>
    internal unsafe Vector Phases(int n)
    {
        double[] result = GC.AllocateUninitializedArray<double>(n);
        fixed (double* p = re, q = im, r = result)
        {
            int i = 0;
            if (Avx2.IsSupported)
                for (int top = n & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(r + i, Avx.LoadVector256(q + i).Atan2(Avx.LoadVector256(p + i)));
            for (; i < n; i++)
                r[i] = Atan2(im[i], re[i]);
        }
        return result;
    }

    /// <summary>
    /// Gets a vector containing the phases of the complex numbers in this vector.
    /// </summary>
    /// <remarks>This operation can be hardware-accelerated.</remarks>
    /// <returns>A new vector with phases.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Phases()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == Length);

        return Phases(Length);
    }

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    public int IndexOf(Complex value)
    {
        Vector r = new(re);
        int idx = r.IndexOf(value.Real);
        while (idx != -1)
        {
            if (im[idx] == value.Imaginary)
                return idx;
            idx = r.IndexOf(value.Real, idx + 1);
        }
        return idx;
    }

    /// <summary>
    /// Creates a new vector by transforming each item with the given function.
    /// </summary>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new vector with transformed content.</returns>
    public ComplexVector Map(Func<Complex, Complex> mapper)
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
    public Vector MapReal(Func<Complex, double> mapper)
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

    /// <summary>
    /// Creates a new complex vector by filtering the items with the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>A new vector with the filtered items.</returns>
    public ComplexVector Filter(Func<Complex, bool> predicate)
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
        return j == Length ? this : new(newRe[..j], newIm[..j]);
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
    public unsafe ComplexVector Zip(ComplexVector other, Func<Complex, Complex, Complex> zipper)
    {
        int len = Min(Length, other.Length);
        double[] newRe = new double[len], newIm = new double[len];
        fixed (double* p1r = re, p1i = im, p2r = other.re, p2i = other.im)
        fixed (double* qr = newRe, qi = newIm)
            for (int i = 0; i < len; i++)
                (qr[i], qi[i]) = zipper(new(p1r[i], p1i[i]), new(p2r[i], p2i[i]));
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
        CommonMatrix.ToString((Complex[])this, v => v.ToString("G6"));

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        $"ans ∊ ℂ({Length})" + Environment.NewLine +
        CommonMatrix.ToString((Complex[])this, v => v.ToString(format, provider));

    /// <summary>Checks if the provided argument is a vector with the same values.</summary>
    /// <param name="other">The vector to be compared.</param>
    /// <returns><see langword="true"/> if the vector argument has the same items.</returns>
    public bool Equals(ComplexVector other) =>
        new Vector(re).Equals(other.re) && new Vector(im).Equals(other.im);

    /// <summary>Checks if the provided argument is a complex vector with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a vector with the same items.</returns>
    public override bool Equals(object? obj) =>
        obj is ComplexVector vector && Equals(vector);

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
    public static bool operator ==(ComplexVector left, ComplexVector right) => left.Equals(right);

    /// <summary>Compares two complex vectors for inequality. </summary>
    /// <param name="left">First vector operand.</param>
    /// <param name="right">Second vector operand.</param>
    /// <returns><see langword="true"/> if any pair of corresponding items are not equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ComplexVector left, ComplexVector right) => !(left == right);
}
