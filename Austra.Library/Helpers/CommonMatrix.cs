namespace Austra.Library.Helpers;

/// <summary>Implements common matrix and vector operations.</summary>
public static class CommonMatrix
{
    /// <summary>Number of characters in a line.</summary>
    public static int TERMINAL_COLUMNS { get; set; } = 80;

    /// <summary>Deconstruct a complex number into its real and imaginary parts.</summary>
    /// <param name="complex">The value to be deconstructed.</param>
    /// <param name="real">The real part.</param>
    /// <param name="imaginary">The imaginary part.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deconstruct(this Complex complex, out double real, out double imaginary) =>
        (real, imaginary) = (complex.Real, complex.Imaginary);

    /// <summary>Creates an identity matrix given its size.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <returns>An identity matrix with the requested size.</returns>
    public static double[] CreateIdentity(int size)
    {
        double[] values = new double[size * size];
        int r = size + 1;
        ref double a = ref MM.GetArrayDataReference(values);
        for (; size-- > 0; a = ref Add(ref a, r))
            a = 1.0;
        return values;
    }

    /// <summary>Creates a diagonal matrix given its diagonal.</summary>
    /// <param name="diagonal">Values in the diagonal.</param>
    /// <returns>An array with its main diagonal initialized.</returns>
    public static double[] CreateDiagonal(this Vector diagonal)
    {
        int size = diagonal.Length, r = size + 1; ;
        double[] values = new double[size * size];
        ref double a = ref MM.GetArrayDataReference(values);
        ref double b = ref MM.GetArrayDataReference((double[])diagonal);
        for (; size-- > 0; a = ref Add(ref a, r), b = ref Add(ref b, 1))
            a = b;
        return values;
    }

    /// <summary>Gets the main diagonal of a 1D-array.</summary>
    /// <param name="values">A 1D-array containing a matrix.</param>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <returns>A vector containing values in the main diagonal.</returns>
    public static Vector Diagonal(this double[] values, int rows, int cols)
    {
        ArgumentNullException.ThrowIfNull(values);
        int r = cols + 1, size = Min(rows, cols);
        double[] result = GC.AllocateUninitializedArray<double>(size);
        ref double a = ref MM.GetArrayDataReference(values);
        ref double b = ref MM.GetArrayDataReference(result);
        for (; size-- > 0; a = ref Add(ref a, r), b = ref Add(ref b, 1))
            b = a;
        return result;
    }

    /// <summary>Calculates the trace of a 1D-array.</summary>
    /// <param name="values">A 1D-array.</param>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <returns>The sum of the cells in the main diagonal.</returns>
    public static double Trace(this double[] values, int rows, int cols)
    {
        if (values is null)
            return 0;
        double trace = 0;
        int r = cols + 1, size = Min(rows, cols);
        for (ref double p = ref MM.GetArrayDataReference(values); size-- > 0; p = ref Add(ref p, r))
            trace += p;
        return trace;
    }

    /// <summary>Gets the product of the cells in the main diagonal.</summary>
    /// <param name="values">A 1D-array.</param>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <returns>The product of the main diagonal.</returns>
    public static double DiagonalProduct(this double[] values, int rows, int cols)
    {
        int r = cols + 1, size = Min(rows, cols);
        double product = 1.0;
        for (ref double p = ref MM.GetArrayDataReference(values); size-- > 0; p = ref Add(ref p, r))
            product *= p;
        return product;
    }

    /// <summary>Gets the item in an span with the maximum absolute value.</summary>
    /// <param name="span">The data span.</param>
    /// <returns>The maximum absolute value in the samples.</returns>
    public static double AbsoluteMaximum(this Span<double> span)
    {
        if (V8.IsHardwareAccelerated && span.Length >= V8d.Count)
        {
            ref double p = ref MM.GetReference(span);
            ref double t = ref Add(ref p, span.Length - V8d.Count);
            V8d vm = V8.Abs(V8.LoadUnsafe(ref p));
            for (; IsAddressLessThan(ref p, ref t); p = ref Add(ref p, V8d.Count))
                vm = V8.Max(vm, V8.Abs(V8.LoadUnsafe(ref p)));
            return V8.Max(vm, V8.Abs(V8.LoadUnsafe(ref t))).Max();
        }
        else if (V4.IsHardwareAccelerated && span.Length >= V4d.Count)
        {
            ref double p = ref MM.GetReference(span);
            ref double t = ref Add(ref p, span.Length - V4d.Count);
            V4d vm = V4.Abs(V4.LoadUnsafe(ref p));
            for (; IsAddressLessThan(ref p, ref t); p = ref Add(ref p, V4d.Count))
                vm = V4.Max(vm, V4.Abs(V4.LoadUnsafe(ref p)));
            return V4.Max(vm, V4.Abs(V4.LoadUnsafe(ref t))).Max();
        }
        double max = Abs(span[0]);
        for (int i = 1; i < span.Length; i++)
            max = Max(max, Abs(span[i]));
        return max;
    }

    /// <summary>Gets the item in a span with the minimum absolute value.</summary>
    /// <param name="span">The data span.</param>
    /// <returns>The minimum absolute value in the samples.</returns>
    public static double AbsoluteMinimum(this Span<double> span)
    {
        if (V8.IsHardwareAccelerated && span.Length >= V8d.Count)
        {
            ref double p = ref MM.GetReference(span);
            ref double t = ref Add(ref p, span.Length - V8d.Count);
            V8d vm = V8.Abs(V8.LoadUnsafe(ref p));
            for (; IsAddressLessThan(ref p, ref t); p = ref Add(ref p, V8d.Count))
                vm = V8.Min(vm, V8.Abs(V8.LoadUnsafe(ref p)));
            return V8.Min(vm, V8.Abs(V8.LoadUnsafe(ref t))).Min();
        }
        else if (V4.IsHardwareAccelerated && span.Length >= V4d.Count)
        {
            ref double p = ref MM.GetReference(span);
            ref double t = ref Add(ref p, span.Length - V4d.Count);
            V4d vm = V4.Abs(V4.LoadUnsafe(ref p));
            for (; IsAddressLessThan(ref p, ref t); p = ref Add(ref p, V4d.Count))
                vm = V4.Min(vm, V4.Abs(V4.LoadUnsafe(ref p)));
            return V4.Min(vm, V4.Abs(V4.LoadUnsafe(ref t))).Min();
        }
        double min = Abs(span[0]);
        for (int i = 1; i < span.Length; i++)
            min = Min(min, Abs(span[i]));
        return min;
    }

    /// <summary>Gets the item with the maximum value in the array.</summary>
    /// <param name="values">Array with data.</param>
    /// <returns>The item with the maximum value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Maximum(this double[] values)
    {
        if (V8.IsHardwareAccelerated && values.Length >= V8d.Count)
        {
            ref double p = ref MM.GetArrayDataReference(values);
            ref double t = ref Add(ref p, values.Length - V8d.Count);
            V8d vm = V8.LoadUnsafe(ref p);
            for (; IsAddressLessThan(ref p, ref t); p = ref Add(ref p, V8d.Count))
                vm = V8.Max(vm, V8.LoadUnsafe(ref p));
            return V8.Max(vm, V8.LoadUnsafe(ref t)).Max();
        }
        if (V4.IsHardwareAccelerated && values.Length >= V4d.Count)
        {
            ref double p = ref MM.GetArrayDataReference(values);
            ref double t = ref Add(ref p, values.Length - V4d.Count);
            V4d vm = V4.LoadUnsafe(ref p);
            for (; IsAddressLessThan(ref p, ref t); p = ref Add(ref p, V4d.Count))
                vm = Avx.Max(vm, V4.LoadUnsafe(ref p));
            return Avx.Max(vm, V4.LoadUnsafe(ref t)).Max();
        }
        double max = double.MaxValue;
        foreach (double d in values)
            max = Max(max, d);
        return max;
    }

    /// <summary>Gets the item with the minimum value in the array.</summary>
    /// <param name="values">Array with data.</param>
    /// <returns>The item with the minimum value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Minimum(this double[] values)
    {
        if (V8.IsHardwareAccelerated && values.Length >= V8d.Count)
        {
            ref double p = ref MM.GetArrayDataReference(values);
            ref double t = ref Add(ref p, values.Length - V8d.Count);
            V8d vm = V8.LoadUnsafe(ref p);
            for (; IsAddressLessThan(ref p, ref t); p = ref Add(ref p, V8d.Count))
                vm = V8.Min(vm, V8.LoadUnsafe(ref p));
            return V8.Min(vm, V8.LoadUnsafe(ref t)).Min();
        }
        if (V4.IsHardwareAccelerated && values.Length >= V4d.Count)
        {
            ref double p = ref MM.GetArrayDataReference(values);
            ref double t = ref Add(ref p, values.Length - V4d.Count);
            V4d vm = V4.LoadUnsafe(ref p);
            for (; IsAddressLessThan(ref p, ref t); p = ref Add(ref p, V4d.Count))
                vm = Avx.Min(vm, V4.LoadUnsafe(ref p));
            return Avx.Min(vm, V4.LoadUnsafe(ref t)).Min();
        }
        double min = double.MaxValue;
        foreach (double d in values)
            min = Min(min, d);
        return min;
    }

    /// <summary>Pointwise sum of two equally sized spans.</summary>
    /// <param name="span1">First summand.</param>
    /// <param name="span2">Second summand.</param>
    /// <param name="target">The span to receive the sum of the first two argument.</param>
    public static void AddV(this Span<double> span1, Span<double> span2, Span<double> target)
    {
        ref double a = ref MM.GetReference(span1);
        ref double b = ref MM.GetReference(span2);
        ref double c = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= V8d.Count)
        {
            nuint t = (nuint)(target.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref a, i) + V8.LoadUnsafe(ref b, i), ref c, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref a, t) + V8.LoadUnsafe(ref b, t), ref c, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= V4d.Count)
        {
            nuint t = (nuint)(target.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref a, i) + V4.LoadUnsafe(ref b, i), ref c, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref a, t) + V4.LoadUnsafe(ref b, t), ref c, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Add(ref c, i) = Add(ref a, i) + Add(ref b, i);
    }

    /// <summary>Pointwise subtraction of two equally sized spans.</summary>
    /// <param name="span1">Minuend.</param>
    /// <param name="span2">Subtrahend.</param>
    /// <param name="target">The span to receive the result.</param>
    public static void SubV(this Span<double> span1, Span<double> span2, Span<double> target)
    {
        ref double a = ref MM.GetReference(span1);
        ref double b = ref MM.GetReference(span2);
        ref double c = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= V8d.Count)
        {
            nuint t = (nuint)(target.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref a, i) - V8.LoadUnsafe(ref b, i), ref c, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref a, t) - V8.LoadUnsafe(ref b, t), ref c, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= V4d.Count)
        {
            nuint t = (nuint)(target.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref a, i) - V4.LoadUnsafe(ref b, i), ref c, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref a, t) - V4.LoadUnsafe(ref b, t), ref c, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Add(ref c, i) = Add(ref a, i) - Add(ref b, i);
    }

    /// <summary>Pointwise addition of a scalar to a span.</summary>
    /// <param name="span">Span summand.</param>
    /// <param name="scalar">Scalar summand.</param>
    /// <param name="target">Target memory for the operation.</param>
    public static void AddV(this Span<double> span, double scalar, Span<double> target)
    {
        ref double p = ref MM.GetReference(span);
        ref double q = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= V8d.Count)
        {
            V8d vec = V8.Create(scalar);
            nuint t = (nuint)(target.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref p, i) + vec, ref q, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref p, t) + vec, ref q, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= V4d.Count)
        {
            V4d vec = V4.Create(scalar);
            nuint t = (nuint)(target.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref p, i) + vec, ref q, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref p, t) + vec, ref q, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Add(ref q, i) = Add(ref p, i) + scalar;
    }

    /// <summary>Pointwise subtraction of a scalar from a span.</summary>
    /// <param name="span">Array minuend.</param>
    /// <param name="scalar">Scalar subtrahend.</param>
    /// <param name="target">Target memory for the operation.</param>
    public static void SubV(this Span<double> span, double scalar, Span<double> target)
    {
        ref double p = ref MM.GetReference(span);
        ref double q = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= V8d.Count)
        {
            V8d vec = V8.Create(scalar);
            nuint t = (nuint)(target.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref p, i) - vec, ref q, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref p, t) - vec, ref q, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= V4d.Count)
        {
            V4d vec = V4.Create(scalar);
            nuint t = (nuint)(target.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref p, i) - vec, ref q, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref p, t) - vec, ref q, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Add(ref q, i) = Add(ref p, i) - scalar;
    }

    /// <summary>Pointwise subtraction of an span from a scalar.</summary>
    /// <param name="scalar">Scalar minuend.</param>
    /// <param name="span">Span subtrahend.</param>
    /// <param name="target">Target memory for the operation.</param>
    public static void SubV(double scalar, Span<double> span, Span<double> target)
    {
        ref double p = ref MM.GetReference(span);
        ref double q = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= V8d.Count)
        {
            V8d vec = V8.Create(scalar);
            nuint t = (nuint)(target.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(vec - V8.LoadUnsafe(ref p, i), ref q, i);
            V8.StoreUnsafe(vec - V8.LoadUnsafe(ref p, t), ref q, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= V4d.Count)
        {
            V4d vec = V4.Create(scalar);
            nuint t = (nuint)(target.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(vec - V4.LoadUnsafe(ref p, i), ref q, i);
            V4.StoreUnsafe(vec - V4.LoadUnsafe(ref p, t), ref q, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Add(ref q, i) = scalar - Add(ref p, i);
    }

    /// <summary>Pointwise negation of a span.</summary>
    /// <param name="span">Span to negate.</param>
    /// <param name="target">Target memory for the operation.</param>
    public static void NegV(this Span<double> span, Span<double> target)
    {
        ref double p = ref MM.GetReference(span);
        ref double q = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= V8d.Count)
        {
            nuint t = (nuint)(target.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(-V8.LoadUnsafe(ref p, i), ref q, i);
            V8.StoreUnsafe(-V8.LoadUnsafe(ref p, t), ref q, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= V4d.Count)
        {
            nuint t = (nuint)(target.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(-V4.LoadUnsafe(ref p, i), ref q, i);
            V4.StoreUnsafe(-V4.LoadUnsafe(ref p, t), ref q, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Add(ref q, i) = -Add(ref p, i);
    }

    /// <summary>Pointwise multiplication of two equally sized spans.</summary>
    /// <param name="span1">Span multiplicand.</param>
    /// <param name="span2">Span multiplier.</param>
    /// <returns>The pointwise multiplication of the two arguments.</returns>
    public static double[] MulV(this Span<double> span1, Span<double> span2)
    {
        double[] result = GC.AllocateUninitializedArray<double>(span1.Length);
        ref double a = ref MM.GetReference(span1);
        ref double b = ref MM.GetReference(span2);
        ref double c = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= V8d.Count)
        {
            nuint t = (nuint)(result.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref a, i) * V8.LoadUnsafe(ref b, i), ref c, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref a, t) * V8.LoadUnsafe(ref b, t), ref c, t);
        }
        else if (V4.IsHardwareAccelerated && result.Length >= V4d.Count)
        {
            nuint t = (nuint)(result.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref a, i) * V4.LoadUnsafe(ref b, i), ref c, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref a, t) * V4.LoadUnsafe(ref b, t), ref c, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref c, i) = Add(ref a, i) * Add(ref b, i);
        return result;
    }

    /// <summary>Pointwise multiplication of a span and a scalar.</summary>
    /// <param name="span">Span multiplicand.</param>
    /// <param name="scalar">Scalar multiplier.</param>
    /// <param name="target">Target memory for the operation.</param>
    public static void MulV(this Span<double> span, double scalar, Span<double> target)
    {
        ref double p = ref MM.GetReference(span);
        ref double q = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= V8d.Count)
        {
            V8d vec = V8.Create(scalar);
            nuint t = (nuint)(target.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref p, i) * vec, ref q, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref p, t) * vec, ref q, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= V4d.Count)
        {
            V4d vec = V4.Create(scalar);
            nuint t = (nuint)(target.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref p, i) * vec, ref q, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref p, t) * vec, ref q, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Add(ref q, i) = Add(ref p, i) * scalar;
    }

    /// <summary>Pointwise division of two equally sized spans.</summary>
    /// <param name="span1">Span dividend.</param>
    /// <param name="span2">Span divisor.</param>
    /// <returns>The pointwise quotient of the two arguments.</returns>
    public static double[] DivV(this Span<double> span1, Span<double> span2)
    {
        double[] result = GC.AllocateUninitializedArray<double>(span1.Length);
        ref double a = ref MM.GetReference(span1);
        ref double b = ref MM.GetReference(span2);
        ref double c = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= V8d.Count)
        {
            nuint t = (nuint)(result.Length - V8d.Count);
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref a, i) / V8.LoadUnsafe(ref b, i), ref c, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref a, t) / V8.LoadUnsafe(ref b, t), ref c, t);
        }
        else if (V4.IsHardwareAccelerated && result.Length >= V4d.Count)
        {
            nuint t = (nuint)(result.Length - V4d.Count);
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref a, i) / V4.LoadUnsafe(ref b, i), ref c, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref a, t) / V4.LoadUnsafe(ref b, t), ref c, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref c, i) = Add(ref a, i) / Add(ref b, i);
        return result;
    }

    /// <summary>
    /// Multiplies a span by a scalar and sums the result to a memory location.
    /// </summary>
    /// <param name="span">Source vector.</param>
    /// <param name="d">Scale factor.</param>
    /// <param name="target">The target memory of the whole operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MulAddStore(this Span<double> span, double d, Span<double> target)
    {
        ref double p = ref MM.GetReference(span);
        ref double q = ref MM.GetReference(target);

        nuint j = 0, c = (nuint)target.Length;
        if (Avx512F.IsSupported)
        {
            V8d vec = V8.Create(d);
            for (nuint t = c & Simd.MASK8; j < t; j += (nuint)V8d.Count)
                V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                    V8.LoadUnsafe(ref p, j), vec, V8.LoadUnsafe(ref q, j)), ref q, j);
        }
        else if (Avx.IsSupported)
        {
            V4d vec = V4.Create(d);
            for (nuint t = c & Simd.MASK4; j < t; j += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref q, j)
                    .MultiplyAdd(V4.LoadUnsafe(ref p, j), vec), ref q, j);
        }
        for (; j < c; j++)
            Add(ref q, j) += Add(ref p, j) * d;
    }

    /// <summary>Calculates the dot product of two spans.</summary>
    /// <remarks>The second span can be longer than the first span.</remarks>
    /// <param name="span1">First span operand.</param>
    /// <param name="span2">Second span operand.</param>
    /// <returns>The dot product of the vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double DotProduct(this Span<double> span1, Span<double> span2)
    {
        double sum = 0;
        ref double p = ref MM.GetReference(span1);
        ref double q = ref MM.GetReference(span2);
        nuint i = 0;
        if (V8.IsHardwareAccelerated)
        {
            V8d acc = V8d.Zero;
            for (nuint top = (nuint)span1.Length & Simd.MASK8; i < top; i += (nuint)V8d.Count)
                acc = Avx512F.FusedMultiplyAdd(V8.LoadUnsafe(ref p, i), V8.LoadUnsafe(ref q, i), acc);
            sum = V8.Sum(acc);
        }
        else if (V4.IsHardwareAccelerated)
        {
            V4d acc = V4d.Zero;
            for (nuint top = (nuint)span1.Length & Simd.MASK4; i < top; i += (nuint)V4d.Count)
                acc = acc.MultiplyAdd(V4.LoadUnsafe(ref p, i), V4.LoadUnsafe(ref q, i));
            sum = acc.Sum();
        }
        for (int j = (int)i; j < span1.Length; j++)
            sum = FusedMultiplyAdd(Add(ref p, j), Add(ref q, j), sum);
        return sum;
    }

    /// <summary>Checks two arrays for equality.</summary>
    /// <param name="array1">First array operand.</param>
    /// <param name="array2">Second array operand.</param>
    /// <returns><see langword="true"/> if both array has the same items.</returns>
    public static bool EqualsV(this double[] array1, double[] array2)
    {
        if (array1.Length != array2.Length)
            return false;
        ref double p = ref MM.GetArrayDataReference(array1);
        ref double q = ref MM.GetArrayDataReference(array2);
        if (V8.IsHardwareAccelerated && array1.Length >= V8d.Count)
        {
            ref double lstP = ref Add(ref p, array1.Length - V8d.Count);
            ref double lstQ = ref Add(ref q, array1.Length - V8d.Count);
            for (; IsAddressLessThan(ref p, ref lstP); p = ref Add(ref p, V8d.Count),
                q = ref Add(ref q, V8d.Count))
                if (!V8.EqualsAll(V8.LoadUnsafe(ref p), V8.LoadUnsafe(ref q)))
                    return false;
            if (!V8.EqualsAll(V8.LoadUnsafe(ref lstP), V8.LoadUnsafe(ref lstQ)))
                return false;
        }
        else if (V4.IsHardwareAccelerated && array1.Length >= V4d.Count)
        {
            ref double lstP = ref Add(ref p, array1.Length - V4d.Count);
            ref double lstQ = ref Add(ref q, array1.Length - V4d.Count);
            for (; IsAddressLessThan(ref p, ref lstP); p = ref Add(ref p, V4d.Count),
                q = ref Add(ref q, V4d.Count))
                if (!V4.EqualsAll(V4.LoadUnsafe(ref p), V4.LoadUnsafe(ref q)))
                    return false;
            if (!V4.EqualsAll(V4.LoadUnsafe(ref lstP), V4.LoadUnsafe(ref lstQ)))
                return false;
        }
        else
            for (int i = 0; i < array1.Length; i++)
                if (Add(ref p, i) != Add(ref q, i))
                    return false;
        return true;
    }

    /// <summary>In-place transposition of a square matrix.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="data">A 1D-array with data.</param>
    public unsafe static void Transpose(int rows, int cols, double[] data)
    {
        Contract.Requires(rows == cols);
        fixed (double* a = data)
            Transpose(a, rows);
    }

    /// <summary>In place transposition of a square matrix.</summary>
    /// <param name="a">Pointer to raw data.</param>
    /// <param name="size">The size of the matrix.</param>
    internal unsafe static void Transpose(double* a, int size)
    {
        if (Avx.IsSupported)
        {
            int s1 = size & Simd.MASK4;
            int s2 = size + size;
            for (int r = 0; r < s1; r += 4)
            {
                for (int c = 0; c < r; c += 4)
                {
                    double* pp = a + (r * size + c);
                    double* qq = a + (c * size + r);
                    var row1 = Avx.LoadVector256(pp);
                    var row2 = Avx.LoadVector256(pp + size);
                    var row3 = Avx.LoadVector256(pp + s2);
                    var row4 = Avx.LoadVector256(pp + s2 + size);
                    var t1 = Avx.Shuffle(row1, row2, 0b0000);
                    var t2 = Avx.Shuffle(row1, row2, 0b1111);
                    var t3 = Avx.Shuffle(row3, row4, 0b0000);
                    var t4 = Avx.Shuffle(row3, row4, 0b1111);
                    row1 = Avx.LoadVector256(qq);
                    row2 = Avx.LoadVector256(qq + size);
                    row3 = Avx.LoadVector256(qq + s2);
                    row4 = Avx.LoadVector256(qq + s2 + size);
                    Avx.Store(qq, Avx.Permute2x128(t1, t3, 0b00100000));
                    Avx.Store(qq + size, Avx.Permute2x128(t2, t4, 0b00100000));
                    Avx.Store(qq + s2, Avx.Permute2x128(t1, t3, 0b00110001));
                    Avx.Store(qq + s2 + size, Avx.Permute2x128(t2, t4, 0b00110001));
                    t1 = Avx.Shuffle(row1, row2, 0b0000);
                    t2 = Avx.Shuffle(row1, row2, 0b1111);
                    t3 = Avx.Shuffle(row3, row4, 0b0000);
                    t4 = Avx.Shuffle(row3, row4, 0b1111);
                    Avx.Store(pp, Avx.Permute2x128(t1, t3, 0b00100000));
                    Avx.Store(pp + size, Avx.Permute2x128(t2, t4, 0b00100000));
                    Avx.Store(pp + s2, Avx.Permute2x128(t1, t3, 0b00110001));
                    Avx.Store(pp + s2 + size, Avx.Permute2x128(t2, t4, 0b00110001));
                }
                // Transpose a diagonal block.
                {
                    double* pp = a + (r * size + r);
                    var row1 = Avx.LoadVector256(pp);
                    var row2 = Avx.LoadVector256(pp + size);
                    var row3 = Avx.LoadVector256(pp + s2);
                    var row4 = Avx.LoadVector256(pp + s2 + size);
                    var t1 = Avx.Shuffle(row1, row2, 0b0000);
                    var t2 = Avx.Shuffle(row1, row2, 0b1111);
                    var t3 = Avx.Shuffle(row3, row4, 0b0000);
                    var t4 = Avx.Shuffle(row3, row4, 0b1111);
                    Avx.Store(pp, Avx.Permute2x128(t1, t3, 0b00100000));
                    Avx.Store(pp + size, Avx.Permute2x128(t2, t4, 0b00100000));
                    Avx.Store(pp + s2, Avx.Permute2x128(t1, t3, 0b00110001));
                    Avx.Store(pp + s2 + size, Avx.Permute2x128(t2, t4, 0b00110001));
                }
            }
            for (int r = s1; r < size; r++)
            {
                double* src = a + r * size;
                double* dst = a + r;
                for (int c = 0; c < s1; c++)
                    (src[c], dst[c * size]) = (dst[c * size], src[c]);
            }
            for (int r = s1; r < size; r++)
                for (int c = s1; c < r; c++)
                    (a[r * size + c], a[c * size + r]) = (a[c * size + r], a[r * size + c]);
        }
        else
        {
            double* b = a;
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < row; col++)
                    (a[col * size + row], b[col]) = (b[col], a[col * size + row]);
                b += size;
            }
        }
    }

    /// <summary>Computes the maximum difference between two arrays.</summary>
    /// <remarks>Arrays can be of different lengths.</remarks>
    /// <param name="first">First array.</param>
    /// <param name="second">Second array.</param>
    /// <returns>The max-norm of the vector difference.</returns>
    public static double Distance(this double[] first, double[] second)
    {
        int len = Min(first.Length, second.Length);
        if (V8.IsHardwareAccelerated && len >= V8d.Count)
        {
            ref double p = ref MM.GetArrayDataReference(first);
            ref double q = ref MM.GetArrayDataReference(second);
            ref double lastp = ref Add(ref p, len - V8d.Count);
            ref double lastq = ref Add(ref q, len - V8d.Count);
            V8d vm = V8d.Zero;
            for (; IsAddressLessThan(ref p, ref lastp); p = ref Add(ref p, V8d.Count),
                q = ref Add(ref q, V8d.Count))
                vm = V8.Max(vm, V8.Abs(V8.LoadUnsafe(ref p) - V8.LoadUnsafe(ref q)));
            return V8.Max(vm, V8.Abs(V8.LoadUnsafe(ref lastp) - V8.LoadUnsafe(ref lastq))).Max();
        }
        if (V4.IsHardwareAccelerated && len >= V4d.Count)
        {
            ref double p = ref MM.GetArrayDataReference(first);
            ref double q = ref MM.GetArrayDataReference(second);
            ref double lastp = ref Add(ref p, len - V4d.Count);
            ref double lastq = ref Add(ref q, len - V4d.Count);
            V4d vm = V4d.Zero;
            for (; IsAddressLessThan(ref p, ref lastp); p = ref Add(ref p, V4d.Count),
                q = ref Add(ref q, V4d.Count))
                vm = V4.Max(vm, V4.Abs(V4.LoadUnsafe(ref p) - V4.LoadUnsafe(ref q)));
            return V4.Max(vm, V4.Abs(V4.LoadUnsafe(ref lastp) - V4.LoadUnsafe(ref lastq))).Max();
        }
        double max = 0;
        for (int i = 0; i < len; i++)
        {
            double v = Abs(first[i] - second[i]);
            if (v > max)
                max = v;
        }
        return max;
    }

    /// <summary>Gets a text representation of an array.</summary>
    /// <param name="data">An array from a vector.</param>
    /// <param name="formatter">A formatter for items.</param>
    /// <returns>A text representation of the vector.</returns>
    /// <typeparam name="T">The type of the items to format.</typeparam>
    public static string ToString<T>(this T[] data, Func<T, string> formatter)
        where T : struct
    {
        if (data.Length == 0)
            return "";
        string[] cells = data.Select(formatter).ToArray();
        int width = Max(3, cells.Max(c => c.Length));
        int cols = (TERMINAL_COLUMNS + 2) / (width + 2);
        StringBuilder sb = new(Min(data.Length / cols, 12) * (TERMINAL_COLUMNS + 2));
        int offset = 0;
        for (int row = 0; row < 11 && offset < data.Length; row++)
        {
            for (int col = 0; col < cols && offset < data.Length; col++, offset++)
            {
                sb.Append(cells[offset].PadLeft(width));
                if (col < cols - 1)
                    sb.Append("  ");
            }
            sb.AppendLine();
        }
        if (offset < data.Length)
        {
            if (data.Length - offset <= cols)
                for (int col = 0; col < cols && offset < data.Length; col++, offset++)
                {
                    sb.Append(cells[offset].PadLeft(width));
                    if (col < cols - 1)
                        sb.Append("  ");
                }
            else
            {
                for (int col = 0; col < cols - 2; col++, offset++)
                {
                    sb.Append(cells[offset].PadLeft(width));
                    if (col < cols - 1)
                        sb.Append("  ");
                }
                sb.Append("...".PadLeft(width))
                    .Append("  ")
                    .Append(cells[^1].PadLeft(width));
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>Gets a text representation of a matrix.</summary>
    /// <param name="data">A 1D-array from a matrix.</param>
    /// <param name="rowCount">Number of rows.</param>
    /// <param name="colCount">Number of columns.</param>
    /// <param name="formatter">Converts items to text.</param>
    /// <param name="triangularity">Which part of the matrix is significative.</param>
    /// <returns>A text representation of the matrix.</returns>
    public static string ToString(this double[] data, int rowCount, int colCount,
        Func<double, string> formatter, sbyte triangularity)
    {
        const int upperRows = 8, lowerRows = 4, minLeftColumns = 5, rightColumns = 2;

        int upper = rowCount <= upperRows ? rowCount : upperRows;
        int lower = rowCount <= upperRows
            ? 0
            : rowCount <= upperRows + lowerRows
            ? rowCount - upperRows
            : lowerRows;
        bool rowEllipsis = rowCount > upper + lower;
        int rows = rowEllipsis ? upper + lower + 1 : upper + lower;

        int left = colCount <= minLeftColumns ? colCount : minLeftColumns;
        int right = colCount <= minLeftColumns
            ? 0
            : colCount <= minLeftColumns + rightColumns
            ? colCount - minLeftColumns
            : rightColumns;

        List<(int, string[])> columnsLeft = new(left);
        for (int j = 0; j < left; j++)
            columnsLeft.Add(FormatColumn(j, rows, upper, lower));

        List<(int, string[])> columnsRight = new(right);
        for (int j = 0; j < right; j++)
            columnsRight.Add(FormatColumn(colCount - right + j, rows, upper, lower));

        int chars = columnsLeft.Sum(t => t.Item1 + 2) + columnsRight.Sum(t => t.Item1 + 2);
        for (int j = left; j < colCount - right; j++)
        {
            (int, string[]) candidate = FormatColumn(j, rows, upper, lower);
            chars += candidate.Item1 + 2;
            if (chars > TERMINAL_COLUMNS - 4)
                break;
            columnsLeft.Add(candidate);
        }

        int cols = columnsLeft.Count + columnsRight.Count;
        bool colEllipsis = colCount > cols;
        if (colEllipsis)
            cols++;

        string[,] array = new string[rows, cols];
        int colIndex = 0;
        foreach ((int, string[]) column in columnsLeft)
        {
            for (int i = 0; i < column.Item2.Length; i++)
                array[i, colIndex] = column.Item2[i];
            colIndex++;
        }
        int saveCol = colEllipsis ? colIndex++ : colIndex;
        foreach ((int, string[]) column in columnsRight)
        {
            for (int i = 0; i < column.Item2.Length; i++)
                array[i, colIndex] = column.Item2[i];
            colIndex++;
        }
        if (colEllipsis)
        {
            colIndex = saveCol;
            int rowIndex = 0;
            if (triangularity == 0)
            {
                for (int row = 0; row < upper; row++)
                    array[rowIndex++, colIndex] = "..";
                if (rowEllipsis)
                    array[rowIndex++, colIndex] = "..";
                for (int row = rowCount - lower; row < rowCount; row++)
                    array[rowIndex++, colIndex] = "..";
            }
            else
            {
                (_, string[] refCol) = triangularity < 0 ? columnsLeft[^1] : columnsRight[0];
                for (int row = 0; row < upper; row++, rowIndex++)
                    if (refCol[rowIndex] != "")
                        array[rowIndex, colIndex] = "..";
                if (rowEllipsis)
                    if (triangularity < 0)
                        rowIndex++;
                    else
                        array[rowIndex++, colIndex] = "..";
                for (int row = rowCount - lower; row < rowCount; row++, rowIndex++)
                    if (refCol[rowIndex] != "")
                        array[rowIndex, colIndex] = "..";
            }
        }
        return FormatStringArrayToString(array);

        string Filter(double value, int row, int column) =>
            triangularity switch
            {
                1 => row > column ? "" : formatter(value),
                -1 => row < column ? "" : formatter(value),
                _ => formatter(value)
            };

        (int, string[]) FormatColumn(int column, int height, int upper, int lower)
        {
            string[] c = new string[height];
            int index = 0;
            for (int row = 0; row < upper; row++)
                c[index++] = Filter(data[row * colCount + column], row, column);
            if (rowEllipsis)
                c[index++] = "";
            for (int row = rowCount - lower; row < rowCount; row++)
                c[index++] = Filter(data[row * colCount + column], row, column);
            int w = c.Max(x => x.Length);
            if (rowEllipsis)
                if (triangularity == 0 ||
                    triangularity < 0 && column <= upper ||
                    triangularity > 0 && column >= upper)
                    c[upper] = "..";
                else
                    c[upper] = "";
            return (w, c);
        }

        static string FormatStringArrayToString(string[,] data)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1);
            Span<int> widths = stackalloc int[cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    widths[j] = Max(widths[j], data[i, j]?.Length ?? 0);
            StringBuilder sb = new();
            for (int i = 0; i < rows; i++)
            {
                sb.Append(data[i, 0].PadLeft(widths[0]));
                for (int j = 1; j < cols; j++)
                    sb.Append("  ").Append((data[i, j] ?? "").PadLeft(widths[j]));
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
