namespace Austra.Library;

/// <summary>Common matrix operations.</summary>
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
        ref double a = ref MemoryMarshal.GetArrayDataReference(values);
        for (; size-- > 0; a = ref Add(ref a, r))
            a = 1.0;
        return values;
    }

    /// <summary>Creates a diagonal matrix given its diagonal.</summary>
    /// <param name="diagonal">Values in the diagonal.</param>
    /// <returns>An array with its main diagonal initialized.</returns>
    public static double[] CreateDiagonal(Vector diagonal)
    {
        int size = diagonal.Length, r = size + 1; ;
        double[] values = new double[size * size];
        ref double a = ref MemoryMarshal.GetArrayDataReference(values);
        ref double b = ref MemoryMarshal.GetArrayDataReference((double[])diagonal);
        for (; size-- > 0; a = ref Add(ref a, r), b = ref Add(ref b, 1))
            a = b;
        return values;
    }

    /// <summary>Gets the main diagonal of a 1D-array.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="values">A 1D-array.</param>
    /// <returns>A vector containing values in the main diagonal.</returns>
    public static Vector Diagonal(int rows, int cols, double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        int r = cols + 1, size = Min(rows, cols);
        double[] result = GC.AllocateUninitializedArray<double>(size);
        ref double a = ref MemoryMarshal.GetArrayDataReference(values);
        ref double b = ref MemoryMarshal.GetArrayDataReference(result);
        for (; size-- > 0; a = ref Add(ref a, r), b = ref Add(ref b, 1))
            b = a;
        return result;
    }

    /// <summary>Calculates the trace of a 1D-array.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="values">A 1D-array.</param>
    /// <returns>The sum of the cells in the main diagonal.</returns>
    public static double Trace(int rows, int cols, double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        double trace = 0;
        int r = cols + 1, size = Min(rows, cols);
        if (size <= 4)
            for (int s = size; s-- > 0;)
                trace += values[rows * s + s];
        else
            for (ref double p = ref MemoryMarshal.GetArrayDataReference(values);
                size-- > 0; p = ref Add(ref p, r))
                trace += p;
        return trace;
    }
    /// <summary>Gets the product of the cells in the main diagonal.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="values">A 1D-array.</param>
    /// <returns>The product of the main diagonal.</returns>
    public static double DiagonalProduct(int rows, int cols, double[] values)
    {
        int r = cols + 1, size = Min(rows, cols);
        double product = 1.0;
        for (ref double a = ref MemoryMarshal.GetArrayDataReference(values);
            size-- > 0; a = Add(ref a, r))
            product *= a;
        return product;
    }

    /// <summary>Gets the item in an array with the maximum absolute value.</summary>
    /// <param name="array">The data array.</param>
    /// <returns>The maximum absolute value in the samples.</returns>
    public static double AbsoluteMaximum(double[] array)
    {
        if (Vector256.IsHardwareAccelerated && array.Length >= Vector256<double>.Count)
        {
            ref double p = ref MemoryMarshal.GetArrayDataReference(array);
            ref double last = ref Add(ref p, array.Length - Vector256<double>.Count);
            Vector256<double> vm = Vector256.Abs(Vector256.LoadUnsafe(ref p));
            for (; IsAddressLessThan(ref p, ref last); p = ref Add(ref p, Vector256<double>.Count))
                vm = Vector256.Max(vm, Vector256.Abs(Vector256.LoadUnsafe(ref p)));
            return Vector256.Max(vm, Vector256.Abs(Vector256.LoadUnsafe(ref last))).Max();
        }
        double max = 0;
        for (int i = 0; i < array.Length; i++)
        {
            double v = Abs(array[i]);
            if (v > max)
                max = v;
        }
        return max;
    }

    /// <summary>Gets the item in an array with the minimum absolute value.</summary>
    /// <param name="array">The data array.</param>
    /// <returns>The minimum absolute value in the samples.</returns>
    public static double AbsoluteMinimum(double[] array)
    {
        if (Vector256.IsHardwareAccelerated && array.Length >= Vector256<double>.Count)
        {
            ref double p = ref MemoryMarshal.GetArrayDataReference(array);
            ref double last = ref Add(ref p, array.Length - Vector256<double>.Count);
            Vector256<double> vm = Vector256.Abs(Vector256.LoadUnsafe(ref p));
            for (; IsAddressLessThan(ref p, ref last); p = ref Add(ref p, Vector256<double>.Count))
                vm = Vector256.Min(vm, Vector256.Abs(Vector256.LoadUnsafe(ref p)));
            return Vector256.Min(vm, Vector256.Abs(Vector256.LoadUnsafe(ref last))).Min();
        }
        double min = 0;
        for (int i = 0; i < array.Length; i++)
        {
            double v = Abs(array[i]);
            if (v < min)
                min = v;
        }
        return min;
    }

    /// <summary>Gets the item in an array with the maximum absolute value.</summary>
    /// <param name="p">Pointer to the array.</param>
    /// <param name="size">Number of items in the array.</param>
    /// <returns>The max-norm of the array segment.</returns>
    public unsafe static double AbsoluteMinimum(double* p, int size)
    {
        double min = Abs(*p);
        int i = 0;
        if (Avx.IsSupported && size >= 8)
        {
            Vector256<double> z = Vector256<double>.Zero;
            Vector256<double> vm = Vector256.Create(min);
            for (int top = size & Simd.AVX_MASK; i < top; i += 4)
            {
                Vector256<double> v = Avx.LoadVector256(p + i);
                vm = Avx.Min(Avx.Max(v, Avx.Subtract(z, v)), vm);
            }
            min = vm.Min();
        }
        for (; i < size; i++)
        {
            double v = Abs(p[i]);
            if (v < min)
                min = v;
        }
        return min;
    }

    /// <summary>Gets the item with the maximum value in the array.</summary>
    /// <param name="values">Array with data.</param>
    /// <returns>The item with the maximum value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Maximum(double[] values)
    {
        if (Vector256.IsHardwareAccelerated && values.Length >= Vector256<double>.Count)
        {
            ref double p = ref MemoryMarshal.GetArrayDataReference(values);
            ref double last = ref Add(ref p, values.Length - Vector256<double>.Count);
            Vector256<double> vm = Vector256.LoadUnsafe(ref p);
            for (; IsAddressLessThan(ref p, ref last); p = ref Add(ref p, Vector256<double>.Count))
                vm = Avx.Max(vm, Vector256.LoadUnsafe(ref p));
            vm = Avx.Max(vm, Vector256.LoadUnsafe(ref last));
            return vm.Max();
        }
        double max = double.MaxValue;
        foreach (double d in values)
            if (d > max)
                max = d;
        return max;
    }

    /// <summary>Gets the item with the minimum value in the array.</summary>
    /// <param name="values">Array with data.</param>
    /// <returns>The item with the minimum value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Minimum(double[] values)
    {
        if (Vector256.IsHardwareAccelerated && values.Length >= Vector256<double>.Count)
        {
            ref double p = ref MemoryMarshal.GetArrayDataReference(values);
            ref double last = ref Add(ref p, values.Length - Vector256<double>.Count);
            Vector256<double> vm = Vector256.LoadUnsafe(ref p);
            for (; IsAddressLessThan(ref p, ref last); p = ref Add(ref p, Vector256<double>.Count))
                vm = Avx.Min(vm, Vector256.LoadUnsafe(ref p));
            vm = Avx.Min(vm, Vector256.LoadUnsafe(ref last));
            return vm.Min();
        }
        double min = double.MaxValue;
        foreach (double d in values)
            if (d < min)
                min = d;
        return min;
    }

    /// <summary>Pointwise sum of two equally sized arrays.</summary>
    /// <param name="array1">First summand.</param>
    /// <param name="array2">Second summand.</param>
    /// <returns>The pointwise sum of the two arguments.</returns>
    public static double[] AddV(double[] array1, double[] array2)
    {
        double[] result = GC.AllocateUninitializedArray<double>(array1.Length);
        ref double a = ref MemoryMarshal.GetArrayDataReference(array1);
        ref double b = ref MemoryMarshal.GetArrayDataReference(array2);
        ref double c = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated && result.Length >= Vector256<double>.Count)
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

    /// <summary>Pointwise subtraction of two equally sized arrays.</summary>
    /// <param name="array1">Minuend.</param>
    /// <param name="array2">Subtrahend.</param>
    /// <returns>The pointwise subtraction of the two arguments.</returns>
    public static double[] SubV(double[] array1, double[] array2)
    {
        double[] result = GC.AllocateUninitializedArray<double>(array1.Length);
        ref double a = ref MemoryMarshal.GetArrayDataReference(array1);
        ref double b = ref MemoryMarshal.GetArrayDataReference(array2);
        ref double c = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated && result.Length >= Vector256<double>.Count)
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

    /// <summary>Pointwise addition of a scalar to an array.</summary>
    /// <param name="array">Array summand.</param>
    /// <param name="scalar">Scalar summand.</param>
    /// <returns>The pointwise sum of the two arguments.</returns>
    public static double[] AddV(double[] array, double scalar)
    {
        double[] result = GC.AllocateUninitializedArray<double>(array.Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(array);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated && result.Length >= Vector256<double>.Count)
        {
            Vector256<double> vec = Vector256.Create(scalar);
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref p, i)) + vec, ref Add(ref q, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref p, t)) + vec, ref Add(ref q, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref q, i) = Add(ref p, i) + scalar;
        return result;
    }

    /// <summary>Pointwise subtraction of a scalar from an array.</summary>
    /// <param name="array">Array minuend.</param>
    /// <param name="scalar">Scalar subtrahend.</param>
    /// <returns>The pointwise subtraction of the two arguments.</returns>
    public static double[] SubV(double[] array, double scalar)
    {
        double[] result = GC.AllocateUninitializedArray<double>(array.Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(array);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated && result.Length >= Vector256<double>.Count)
        {
            Vector256<double> vec = Vector256.Create(scalar);
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref p, i)) - vec, ref Add(ref q, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref p, t)) - vec, ref Add(ref q, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref q, i) = Add(ref p, i) - scalar;
        return result;
    }

    /// <summary>Pointwise subtraction of an array from a scalar.</summary>
    /// <param name="scalar">Scalar minuend.</param>
    /// <param name="array">Array subtrahend.</param>
    /// <returns>The pointwise subtraction of the two arguments.</returns>
    public static double[] SubV(double scalar, double[] array)
    {
        double[] result = GC.AllocateUninitializedArray<double>(array.Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(array);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated && result.Length >= Vector256<double>.Count)
        {
            Vector256<double> vec = Vector256.Create(scalar);
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    vec - Vector256.LoadUnsafe(ref Add(ref p, i)), ref Add(ref q, i));
            Vector256.StoreUnsafe(
                vec - Vector256.LoadUnsafe(ref Add(ref p, t)), ref Add(ref q, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref q, i) = scalar - Add(ref p, i);
        return result;
    }

    /// <summary>Pointwise negation of an array.</summary>
    /// <param name="array">Array to negate.</param>
    /// <returns>The pointwise negation of the argument.</returns>
    public static double[] NegV(double[] array)
    {
        double[] result = GC.AllocateUninitializedArray<double>(array.Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(array);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated && result.Length >= Vector256<double>.Count)
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

    /// <summary>Pointwise multiplication of two equally sized arrays.</summary>
    /// <param name="array1">Array multiplicand.</param>
    /// <param name="array2">Array multiplier.</param>
    /// <returns>The pointwise multiplication of the two arguments.</returns>
    public static double[] MulV(double[] array1, double[] array2)
    {
        double[] result = GC.AllocateUninitializedArray<double>(array1.Length);
        ref double a = ref MemoryMarshal.GetArrayDataReference(array1);
        ref double b = ref MemoryMarshal.GetArrayDataReference(array2);
        ref double c = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated && result.Length >= Vector256<double>.Count)
        {
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref a, i)) * Vector256.LoadUnsafe(ref Add(ref b, i)),
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

    /// <summary>Pointwise multiplication of an array and a scalar.</summary>
    /// <param name="array">Array multiplicand.</param>
    /// <param name="scalar">Scalar multiplier.</param>
    /// <returns>The pointwise multiplication of the two arguments.</returns>
    public static double[] MulV(double[] array, double scalar)
    {
        double[] result = GC.AllocateUninitializedArray<double>(array.Length);
        ref double p = ref MemoryMarshal.GetArrayDataReference(array);
        ref double q = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated && result.Length >= Vector256<double>.Count)
        {
            Vector256<double> vec = Vector256.Create(scalar);
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref p, i)) * vec,
                    ref Add(ref q, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref p, t)) * vec,
                ref Add(ref q, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref q, i) = Add(ref p, i) * scalar;
        return result;
    }

    /// <summary>Pointwise division of two equally sized arrays.</summary>
    /// <param name="array1">Array dividend.</param>
    /// <param name="array2">Array divisor.</param>
    /// <returns>The pointwise quotient of the two arguments.</returns>
    public static double[] DivV(double[] array1, double[] array2)
    {
        double[] result = GC.AllocateUninitializedArray<double>(array1.Length);
        ref double a = ref MemoryMarshal.GetArrayDataReference(array1);
        ref double b = ref MemoryMarshal.GetArrayDataReference(array2);
        ref double c = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated && result.Length >= Vector256<double>.Count)
        {
            int t = array1.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref a, i)) / Vector256.LoadUnsafe(ref Add(ref b, i)),
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
            int s1 = size & Simd.AVX_MASK;
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
    /// <param name="first">First array.</param>
    /// <param name="second">Second array.</param>
    /// <returns>The max-norm of the vector difference.</returns>
    public unsafe static double Distance(double[] first, double[] second)
    {
        double max = 0;
        fixed (double* p = first, q = second)
        {
            int i = 0, len = Min(first.Length, second.Length);
            if (Avx.IsSupported)
            {
                Vector256<double> z = Vector256<double>.Zero, vm = z;
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                {
                    Vector256<double> v = Avx.LoadVector256(p + i);
                    vm = Avx.Max(Avx.Max(v, Avx.Subtract(z, v)), vm);
                }
                max = vm.Max();
            }
            for (; i < len; i++)
            {
                double v = Abs(p[i] - q[i]);
                if (v > max)
                    max = v;
            }
        }
        return max;
    }

    /// <summary>Gets a text representation of an array.</summary>
    /// <param name="data">An array from a vector.</param>
    /// <param name="formatter">A formatter for items.</param>
    /// <returns>A text representation of the vector.</returns>
    /// <typeparam name="T">The type of the items to format.</typeparam>
    public static string ToString<T>(T[] data, Func<T, string> formatter)
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
    /// <param name="rowCount">Number of rows.</param>
    /// <param name="colCount">Number of columns.</param>
    /// <param name="data">A 1D-array from a matrix.</param>
    /// <param name="formatter">Converts items to text.</param>
    /// <param name="triangularity">Which part of the matrix is significative.</param>
    /// <returns>A text representation of the matrix.</returns>
    public static string ToString(int rowCount, int colCount, double[] data,
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
