﻿namespace Austra.Library.Helpers;

/// <summary>Implements common matrix and vector operations.</summary>
/// <remarks>
/// We have three matrix types: <see cref="Matrix"/>, <see cref="LMatrix"/>,
/// and <see cref="RMatrix"/>, with common operations. On the other hand, matrices
/// also belong to a vector space, so they share some code with <see cref="DVector"/>.
/// </remarks>
public static class Vec
{
    /// <summary>Number of characters in a line.</summary>
    public static int TERMINAL_COLUMNS { get; set; } = 80;

    /// <summary>Gets the absolute values of the array items.</summary>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <param name="values">The array to transform.</param>
    /// <returns>A new array with non-negative items.</returns>
    public static T[] Abs<T>(this T[] values) where T : struct, INumberBase<T>
    {
        T[] result = GC.AllocateUninitializedArray<T>(values.Length);
        ref T p = ref MM.GetArrayDataReference(values);
        ref T q = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= Vector512<T>.Count)
        {
            nuint t = (nuint)(result.Length - Vector512<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(V8.Abs(V8.LoadUnsafe(ref p, i)), ref q, i);
            V8.StoreUnsafe(V8.Abs(V8.LoadUnsafe(ref p, t)), ref q, t);
        }
        else if (V4.IsHardwareAccelerated && result.Length >= Vector256<T>.Count)
        {
            nuint t = (nuint)(result.Length - Vector256<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(V4.Abs(V4.LoadUnsafe(ref p, i)), ref q, i);
            V4.StoreUnsafe(V4.Abs(V4.LoadUnsafe(ref p, t)), ref q, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Unsafe.Add(ref q, i) = T.Abs(Unsafe.Add(ref p, i));
        return result;
    }

    /// <summary>Pointwise sum of two equally sized spans.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="span1">First summand.</param>
    /// <param name="span2">Second summand.</param>
    /// <param name="target">The span to receive the sum of the first two argument.</param>
    public static void Add<T>(this Span<T> span1, Span<T> span2, Span<T> target)
        where T : INumberBase<T>
    {
        ref T a = ref MM.GetReference(span1);
        ref T b = ref MM.GetReference(span2);
        ref T c = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= Vector512<T>.Count)
        {
            nuint t = (nuint)(target.Length - Vector512<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref a, i) + V8.LoadUnsafe(ref b, i), ref c, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref a, t) + V8.LoadUnsafe(ref b, t), ref c, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= Vector256<T>.Count)
        {
            nuint t = (nuint)(target.Length - Vector256<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref a, i) + V4.LoadUnsafe(ref b, i), ref c, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref a, t) + V4.LoadUnsafe(ref b, t), ref c, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Unsafe.Add(ref c, i) = Unsafe.Add(ref a, i) + Unsafe.Add(ref b, i);
    }

    /// <summary>Pointwise inplace sum of two equally sized spans.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="span1">First summand and target.</param>
    /// <param name="span2">Second summand.</param>
    public static void Add<T>(this Span<T> span1, Span<T> span2) where T: INumberBase<T>
    {
        ref T a = ref MM.GetReference(span1);
        ref T b = ref MM.GetReference(span2);
        nuint i = 0;
        if (V8.IsHardwareAccelerated && span1.Length >= Vector512<T>.Count)
            for (nuint top = (nuint)(span1.Length & ~(Vector512<T>.Count - 1));
                i < top; i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref a, i) + V8.LoadUnsafe(ref b, i), ref a, i);
        else if (V4.IsHardwareAccelerated && span1.Length >= Vector256<T>.Count)
            for (nuint top = (nuint)(span1.Length & ~(Vector256<T>.Count - 1));
                i < top; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref a, i) + V4.LoadUnsafe(ref b, i), ref a, i);
        for (; i < (nuint)span1.Length; i++)
            Unsafe.Add(ref a, i) = Unsafe.Add(ref a, i) + Unsafe.Add(ref b, i);
    }

    /// <summary>Pointwise addition of a scalar to a span.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="span">Span summand.</param>
    /// <param name="scalar">Scalar summand.</param>
    /// <param name="target">Target memory for the operation.</param>
    public static void Add<T>(this Span<T> span, T scalar, Span<T> target) where T : INumberBase<T>
    {
        ref T p = ref MM.GetReference(span);
        ref T q = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= Vector512<T>.Count)
        {
            Vector512<T> vec = V8.Create(scalar);
            nuint t = (nuint)(target.Length - Vector512<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref p, i) + vec, ref q, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref p, t) + vec, ref q, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= Vector256<T>.Count)
        {
            Vector256<T> vec = V4.Create(scalar);
            nuint t = (nuint)(target.Length - Vector256<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref p, i) + vec, ref q, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref p, t) + vec, ref q, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Unsafe.Add(ref q, i) = Unsafe.Add(ref p, i) + scalar;
    }

    /// <summary>Checks whether the predicate is satisfied by all items.</summary>
    /// <typeparam name="T">The type of the span.</typeparam>
    /// <param name="span">The span to search.</param>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if all items satisfy the predicate.</returns>
    public static bool All<T>(this Span<T> span, Func<T, bool> predicate) where T : struct
    {
        foreach (T item in span)
            if (!predicate(item))
                return false;
        return true;
    }

    /// <summary>Gets the item in a span with the maximum absolute value.</summary>
    /// <param name="span">The data span.</param>
    /// <returns>The maximum absolute value in the samples.</returns>
    public static double AMax(this Span<double> span)
    {
        if (V8.IsHardwareAccelerated && span.Length >= V8d.Count)
        {
            ref double p = ref MM.GetReference(span);
            ref double t = ref Unsafe.Add(ref p, span.Length - V8d.Count);
            V8d vm = V8.Abs(V8.LoadUnsafe(ref p));
            p = ref Unsafe.Add(ref p, V8d.Count);
            for (; IsAddressLessThan(ref p, ref t); p = ref Unsafe.Add(ref p, V8d.Count))
                vm = V8.Max(vm, V8.Abs(V8.LoadUnsafe(ref p)));
            return V8.Max(vm, V8.Abs(V8.LoadUnsafe(ref t))).Max();
        }
        else if (V4.IsHardwareAccelerated && span.Length >= V4d.Count)
        {
            ref double p = ref MM.GetReference(span);
            ref double t = ref Unsafe.Add(ref p, span.Length - V4d.Count);
            V4d vm = V4.Abs(V4.LoadUnsafe(ref p));
            p = ref Unsafe.Add(ref p, V4d.Count);
            for (; IsAddressLessThan(ref p, ref t); p = ref Unsafe.Add(ref p, V4d.Count))
                vm = V4.Max(vm, V4.Abs(V4.LoadUnsafe(ref p)));
            return V4.Max(vm, V4.Abs(V4.LoadUnsafe(ref t))).Max();
        }
        double max = Math.Abs(span[0]);
        for (int i = 1; i < span.Length; i++)
            max = Math.Max(max, Math.Abs(span[i]));
        return max;
    }

    /// <summary>Gets the item in a span with the minimum absolute value.</summary>
    /// <param name="span">The data span.</param>
    /// <returns>The minimum absolute value in the samples.</returns>
    public static double AMin(this Span<double> span)
    {
        if (V8.IsHardwareAccelerated && span.Length >= V8d.Count)
        {
            ref double p = ref MM.GetReference(span);
            ref double t = ref Unsafe.Add(ref p, span.Length - V8d.Count);
            V8d vm = V8.Abs(V8.LoadUnsafe(ref p));
            p = ref Unsafe.Add(ref p, V8d.Count);
            for (; IsAddressLessThan(ref p, ref t); p = ref Unsafe.Add(ref p, V8d.Count))
                vm = V8.Min(vm, V8.Abs(V8.LoadUnsafe(ref p)));
            return V8.Min(vm, V8.Abs(V8.LoadUnsafe(ref t))).Min();
        }
        else if (V4.IsHardwareAccelerated && span.Length >= V4d.Count)
        {
            ref double p = ref MM.GetReference(span);
            ref double t = ref Unsafe.Add(ref p, span.Length - V4d.Count);
            V4d vm = V4.Abs(V4.LoadUnsafe(ref p));
            p = ref Unsafe.Add(ref p, V4d.Count);
            for (; IsAddressLessThan(ref p, ref t); p = ref Unsafe.Add(ref p, V4d.Count))
                vm = V4.Min(vm, V4.Abs(V4.LoadUnsafe(ref p)));
            return V4.Min(vm, V4.Abs(V4.LoadUnsafe(ref t))).Min();
        }
        double min = Math.Abs(span[0]);
        for (int i = 1; i < span.Length; i++)
            min = Math.Min(min, Math.Abs(span[i]));
        return min;
    }

    /// <summary>Checks whether the predicate is satisfied by at least one item.</summary>
    /// <typeparam name="T">The type of the span.</typeparam>
    /// <param name="span">The span to search.</param>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if there exists a item satisfying the predicate.</returns>
    public static bool Any<T>(this Span<T> span, Func<T, bool> predicate) where T : struct
    {
        foreach (T item in span)
            if (predicate(item))
                return true;
        return false;
    }

    /// <summary>Creates a diagonal matrix given its diagonal.</summary>
    /// <param name="diagonal">Values in the diagonal.</param>
    /// <returns>An array with its main diagonal initialized.</returns>
    public static double[] CreateDiagonal(this DVector diagonal)
    {
        int size = diagonal.Length, r = size + 1; ;
        double[] values = new double[size * size];
        ref double a = ref MM.GetArrayDataReference(values);
        ref double b = ref MM.GetArrayDataReference((double[])diagonal);
        for (; size-- > 0; a = ref Unsafe.Add(ref a, r), b = ref Unsafe.Add(ref b, 1))
            a = b;
        return values;
    }

    /// <summary>Creates an identity matrix given a size.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <returns>An identity matrix with the requested size.</returns>
    public static double[] CreateIdentity(int size)
    {
        double[] values = new double[size * size];
        int r = size + 1;
        ref double a = ref MM.GetArrayDataReference(values);
        for (; size-- > 0; a = ref Unsafe.Add(ref a, r))
            a = 1.0;
        return values;
    }

    /// <summary>Initializes a span with random values.</summary>
    /// <param name="span">The memory target for the operation.</param>
    /// <param name="random">A random number generator.</param>
    public static void CreateRandom(this Span<double> span, Random random)
    {
        ref double p = ref MM.GetReference(span);
        if (Avx512F.IsSupported && span.Length >= V8d.Count && random == Random.Shared)
        {
            nuint t = (nuint)(span.Length - V8d.Count);
            Random512 rnd512 = Random512.Shared;
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(rnd512.NextDouble(), ref p, i);
            V8.StoreUnsafe(rnd512.NextDouble(), ref p, t);
        }
        else if (Avx2.IsSupported && span.Length >= V4d.Count && random == Random.Shared)
        {
            nuint t = (nuint)(span.Length - V4d.Count);
            Random256 rnd256 = Random256.Shared;
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(rnd256.NextDouble(), ref p, i);
            V4.StoreUnsafe(rnd256.NextDouble(), ref p, t);
        }
        else
            for (int i = 0; i < span.Length; i++)
                Unsafe.Add(ref p, i) = random.NextDouble();
    }

    /// <summary>Initializes a span with random values.</summary>
    /// <param name="span">The memory target for the operation.</param>
    /// <param name="random">A random number generator.</param>
    /// <param name="offset">An offset for the random numbers.</param>
    /// <param name="width">Width for the uniform distribution.</param>
    public static void CreateRandom(this Span<double> span, Random random,
        double offset, double width)
    {
        ref double p = ref MM.GetReference(span);
        if (Avx512F.IsSupported && span.Length >= V8d.Count && random == Random.Shared)
        {
            nuint t = (nuint)(span.Length - V8d.Count);
            V8d vOff = V8.Create(offset);
            V8d vWidth = V8.Create(width);
            Random512 rnd512 = Random512.Shared;
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(rnd512.NextDouble(), vWidth, vOff), ref p, i);
            V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(rnd512.NextDouble(), vWidth, vOff), ref p, t);
        }
        else if (Avx2.IsSupported && span.Length >= V4d.Count && random == Random.Shared)
        {
            nuint t = (nuint)(span.Length - V4d.Count);
            V4d vOff = V4.Create(offset);
            V4d vWidth = V4.Create(width);
            Random256 rnd256 = Random256.Shared;
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(rnd256.NextDouble().MultiplyAdd(vWidth, vOff), ref p, i);
            V4.StoreUnsafe(rnd256.NextDouble().MultiplyAdd(vWidth, vOff), ref p, t);
        }
        else
            for (int i = 0; i < span.Length; i++)
                Unsafe.Add(ref p, i) = FusedMultiplyAdd(random.NextDouble(), width, offset);
    }

    /// <summary>Initializes a span with normal random values.</summary>
    /// <param name="span">The memory target for the operation.</param>
    /// <param name="random">A random number generator.</param>
    public static void CreateRandom(this Span<double> span, NormalRandom random)
    {
        ref double p = ref MM.GetReference(span);
        if (Avx512F.IsSupported && span.Length >= V8d.Count && random == NormalRandom.Shared)
        {
            nuint t = (nuint)(span.Length - V8d.Count);
            Random512 rnd512 = Random512.Shared;
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(rnd512.NextNormal(), ref p, i);
            V8.StoreUnsafe(rnd512.NextNormal(), ref p, t);
        }
        else if (Avx2.IsSupported && span.Length >= V4d.Count && random == NormalRandom.Shared)
        {
            nuint t = (nuint)(span.Length - V4d.Count);
            Random256 rnd256 = Random256.Shared;
            for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                V4.StoreUnsafe(rnd256.NextNormal(), ref p, i);
            V4.StoreUnsafe(rnd256.NextNormal(), ref p, t);
        }
        else
        {
            int i = 0;
            for (int t = span.Length & ~1; i < t; i += 2)
                random.NextDoubles(ref Unsafe.Add(ref p, i));
            if (i < span.Length)
                Unsafe.Add(ref p, i) = random.NextDouble();
        }
    }

    /// <summary>Gets the main diagonal of a 1D-array.</summary>
    /// <param name="values">A 1D-array containing a matrix.</param>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <returns>A vector containing values in the main diagonal.</returns>
    public static DVector Diagonal(this double[] values, int rows, int cols)
    {
        ArgumentNullException.ThrowIfNull(values);
        Contract.Ensures(Contract.Result<DVector>().Length == Math.Min(rows, cols));

        int r = cols + 1, size = Math.Min(rows, cols);
        double[] result = GC.AllocateUninitializedArray<double>(size);
        ref double a = ref MM.GetArrayDataReference(values);
        ref double b = ref MM.GetArrayDataReference(result);
        for (; size-- > 0; a = ref Unsafe.Add(ref a, r), b = ref Unsafe.Add(ref b, 1))
            b = a;
        return result;
    }

    /// <summary>Deconstruct a complex number into its real and imaginary parts.</summary>
    /// <param name="complex">The value to be deconstructed.</param>
    /// <param name="real">The real part.</param>
    /// <param name="imaginary">The imaginary part.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deconstruct(this Complex complex, out double real, out double imaginary) =>
        (real, imaginary) = (complex.Real, complex.Imaginary);

    /// <summary>Gets the product of the cells in the main diagonal.</summary>
    /// <param name="values">A 1D-array.</param>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <returns>The product of the main diagonal.</returns>
    public static double Det(this double[] values, int rows, int cols)
    {
        int r = cols + 1, size = Math.Min(rows, cols);
        double product = 1.0;
        for (ref double p = ref MM.GetArrayDataReference(values); size-- > 0; p = ref Unsafe.Add(ref p, r))
            product *= p;
        return product;
    }

    /// <summary>Computes the maximum difference between two arrays.</summary>
    /// <remarks>Arrays can be of different lengths.</remarks>
    /// <param name="first">First array.</param>
    /// <param name="second">Second array.</param>
    /// <returns>The max-norm of the vector difference.</returns>
    public static double Distance(this double[] first, double[] second)
    {
        int len = Math.Min(first.Length, second.Length);
        if (V8.IsHardwareAccelerated && len >= V8d.Count)
        {
            ref double p = ref MM.GetArrayDataReference(first);
            ref double q = ref MM.GetArrayDataReference(second);
            ref double lastp = ref Unsafe.Add(ref p, len - V8d.Count);
            ref double lastq = ref Unsafe.Add(ref q, len - V8d.Count);
            V8d vm = V8d.Zero;
            for (; IsAddressLessThan(ref p, ref lastp); p = ref Unsafe.Add(ref p, V8d.Count),
                q = ref Unsafe.Add(ref q, V8d.Count))
                vm = V8.Max(vm, V8.Abs(V8.LoadUnsafe(ref p) - V8.LoadUnsafe(ref q)));
            return V8.Max(vm, V8.Abs(V8.LoadUnsafe(ref lastp) - V8.LoadUnsafe(ref lastq))).Max();
        }
        if (V4.IsHardwareAccelerated && len >= V4d.Count)
        {
            ref double p = ref MM.GetArrayDataReference(first);
            ref double q = ref MM.GetArrayDataReference(second);
            ref double lastp = ref Unsafe.Add(ref p, len - V4d.Count);
            ref double lastq = ref Unsafe.Add(ref q, len - V4d.Count);
            V4d vm = V4d.Zero;
            for (; IsAddressLessThan(ref p, ref lastp); p = ref Unsafe.Add(ref p, V4d.Count),
                q = ref Unsafe.Add(ref q, V4d.Count))
                vm = V4.Max(vm, V4.Abs(V4.LoadUnsafe(ref p) - V4.LoadUnsafe(ref q)));
            return V4.Max(vm, V4.Abs(V4.LoadUnsafe(ref lastp) - V4.LoadUnsafe(ref lastq))).Max();
        }
        double max = 0;
        for (int i = 0; i < len; i++)
        {
            double v = Math.Abs(first[i] - second[i]);
            if (v > max)
                max = v;
        }
        return max;
    }

    /// <summary>Returns a new array with the distinct values in the span.</summary>
    /// <typeparam name="T">The type of the span.</typeparam>
    /// <remarks>Results are unordered.</remarks>
    /// <param name="span">The span to transform.</param>
    /// <returns>A new array with distinct values.</returns>
    public static T[] Distinct<T>(this Span<T> span) where T : struct =>
        [.. ((HashSet<T>)([.. span]))];

    /// <summary>Pointwise division of two equally sized spans.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="span1">Span dividend.</param>
    /// <param name="span2">Span divisor.</param>
    /// <returns>The pointwise quotient of the two arguments.</returns>
    public static T[] Div<T>(this Span<T> span1, Span<T> span2) where T : INumberBase<T>
    {
        T[] result = GC.AllocateUninitializedArray<T>(span1.Length);
        ref T a = ref MM.GetReference(span1);
        ref T b = ref MM.GetReference(span2);
        ref T c = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= Vector512<T>.Count)
        {
            nuint t = (nuint)(result.Length - Vector512<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref a, i) / V8.LoadUnsafe(ref b, i), ref c, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref a, t) / V8.LoadUnsafe(ref b, t), ref c, t);
        }
        else if (V4.IsHardwareAccelerated && result.Length >= Vector256<T>.Count)
        {
            nuint t = (nuint)(result.Length - Vector256<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref a, i) / V4.LoadUnsafe(ref b, i), ref c, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref a, t) / V4.LoadUnsafe(ref b, t), ref c, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Unsafe.Add(ref c, i) = Unsafe.Add(ref a, i) / Unsafe.Add(ref b, i);
        return result;
    }

    /// <summary>Pointwise division of a span by an integer.</summary>
    /// <param name="span">Span dividend.</param>
    /// <param name="divisor">Scalar divisor.</param>
    /// <returns>The pointwise quotient of the two arguments.</returns>
    public static int[] Div(this Span<int> span, int divisor)
    {
        int[] result = GC.AllocateUninitializedArray<int>(span.Length);
        ref int a = ref MM.GetReference(span);
        ref int c = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= V8i.Count)
        {
            V8i d = V8.Create(divisor);
            nuint t = (nuint)(result.Length - V8i.Count);
            for (nuint i = 0; i < t; i += (nuint)V8i.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref a, i) / d, ref c, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref a, t) / d, ref c, t);
        }
        else if (V4.IsHardwareAccelerated && result.Length >= V4i.Count)
        {
            V4i d = V4.Create(divisor);
            nuint t = (nuint)(result.Length - V4i.Count);
            for (nuint i = 0; i < t; i += (nuint)V4i.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref a, i) / d, ref c, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref a, t) / d, ref c, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Unsafe.Add(ref c, i) = Unsafe.Add(ref a, i) / divisor;
        return result;
    }

    /// <summary>Checks two arrays for equality.</summary>
    /// <typeparam name="T">The type of the arrays.</typeparam>
    /// <param name="array1">First array operand.</param>
    /// <param name="array2">Second array operand.</param>
    /// <returns><see langword="true"/> if both array has the same items.</returns>
    public static bool Eqs<T>(this T[] array1, T[] array2)
        where T : IEquatable<T>, IEqualityOperators<T, T, bool>
    {
        if (array1.Length != array2.Length)
            return false;
        ref T p = ref MM.GetArrayDataReference(array1);
        ref T q = ref MM.GetArrayDataReference(array2);
        if (V8.IsHardwareAccelerated && array1.Length >= Vector512<T>.Count)
        {
            ref T lstP = ref Unsafe.Add(ref p, array1.Length - Vector512<T>.Count);
            ref T lstQ = ref Unsafe.Add(ref q, array1.Length - Vector512<T>.Count);
            for (; IsAddressLessThan(ref p, ref lstP); p = ref Unsafe.Add(ref p, Vector512<T>.Count),
                q = ref Unsafe.Add(ref q, Vector512<T>.Count))
                if (!V8.EqualsAll(V8.LoadUnsafe(ref p), V8.LoadUnsafe(ref q)))
                    return false;
            if (!V8.EqualsAll(V8.LoadUnsafe(ref lstP), V8.LoadUnsafe(ref lstQ)))
                return false;
        }
        else if (V4.IsHardwareAccelerated && array1.Length >= Vector256<T>.Count)
        {
            ref T lstP = ref Unsafe.Add(ref p, array1.Length - Vector256<T>.Count);
            ref T lstQ = ref Unsafe.Add(ref q, array1.Length - Vector256<T>.Count);
            for (; IsAddressLessThan(ref p, ref lstP); p = ref Unsafe.Add(ref p, Vector256<T>.Count),
                q = ref Unsafe.Add(ref q, Vector256<T>.Count))
                if (!V4.EqualsAll(V4.LoadUnsafe(ref p), V4.LoadUnsafe(ref q)))
                    return false;
            if (!V4.EqualsAll(V4.LoadUnsafe(ref lstP), V4.LoadUnsafe(ref lstQ)))
                return false;
        }
        else
            for (int i = 0; i < array1.Length; i++)
                if (Unsafe.Add(ref p, i) != Unsafe.Add(ref q, i))
                    return false;
        return true;
    }

    /// <summary>Creates a new array by filtering items with the given predicate.</summary>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <param name="values">The array to filter.</param>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>A new array with the filtered items.</returns>
    public static T[] Filter<T>(this T[] values, Func<T, bool> predicate) where T : struct
    {
        T[] newValues = GC.AllocateUninitializedArray<T>(values.Length);
        int j = 0;
        foreach (T value in values)
            if (predicate(value))
                newValues[j++] = value;
        return j == 0 ? [] : j == values.Length ? values : newValues[..j];
    }

    /// <summary>Creates a new vector by filtering and mapping at the same time.</summary>
    /// <remarks>This method can save an intermediate buffer and one iteration.</remarks>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <param name="values">The array to transform.</param>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new array with the filtered items.</returns>
    public static T[] FilterMap<T>(this T[] values, Func<T, bool> predicate, Func<T, T> mapper) where T : struct
    {
        T[] newValues = GC.AllocateUninitializedArray<T>(values.Length);
        int j = 0;
        foreach (T value in values)
            if (predicate(value))
                newValues[j++] = mapper(value);
        return j == 0 ? [] : j == values.Length ? values : newValues[..j];
    }

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <typeparam name="T">The type of the span.</typeparam>
    /// <param name="values">The span to search.</param>
    /// <param name="value">The value to locate.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    public static int IndexOf<T>(this ReadOnlySpan<T> values, T value)
        where T : struct, IEquatable<T>
    {
        ref T p = ref MM.GetReference(values);
        nuint size = (nuint)values.Length;
        if (V8.IsHardwareAccelerated && size >= (nuint)Vector512<T>.Count)
        {
            Vector512<T> v = V8.Create(value);
            nuint t = size - (nuint)Vector512<T>.Count;
            ulong mask;
            for (nuint i = 0; i < t; i += (nuint)Vector512<T>.Count)
            {
                mask = V8.ExtractMostSignificantBits(V8.Equals(V8.LoadUnsafe(ref p, i), v));
                if (mask != 0)
                    return (int)i + BitOperations.TrailingZeroCount(mask);
            }
            mask = V8.ExtractMostSignificantBits(V8.Equals(V8.LoadUnsafe(ref p, t), v));
            if (mask != 0)
                return (int)t + BitOperations.TrailingZeroCount(mask);
        }
        else if (V4.IsHardwareAccelerated && size >= (nuint)Vector256<T>.Count)
        {
            Vector256<T> v = V4.Create(value);
            nuint t = size - (nuint)Vector256<T>.Count;
            uint mask;
            for (nuint i = 0; i < t; i += (nuint)Vector256<T>.Count)
            {
                mask = V4.ExtractMostSignificantBits(V4.Equals(V4.LoadUnsafe(ref p, i), v));
                if (mask != 0)
                    return (int)i + BitOperations.TrailingZeroCount(mask);
            }
            mask = V4.ExtractMostSignificantBits(V4.Equals(V4.LoadUnsafe(ref p, t), v));
            if (mask != 0)
                return (int)t + BitOperations.TrailingZeroCount(mask);
        }
        else
            for (nuint i = 0; i < size; i++)
                if (Unsafe.Add(ref p, i).Equals(value))
                    return (int)i;
        return -1;
    }

    /// <summary>
    /// Creates a new array by transforming each item with the given function.
    /// </summary>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <param name="values">The array to transform.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new array with the transformed content.</returns>
    public static T[] Map<T>(this T[] values, Func<T, T> mapper) where T : struct
    {
        T[] newValues = GC.AllocateUninitializedArray<T>(values.Length);
        ref T p = ref MM.GetArrayDataReference(values);
        ref T q = ref MM.GetArrayDataReference(newValues);
        int i = 0;
        for (int size = newValues.Length & (~3); i < size; i += 4)
        {
            var (a, b, c, d) = (mapper(Unsafe.Add(ref p, i)), mapper(Unsafe.Add(ref p, i + 1)),
                mapper(Unsafe.Add(ref p, i + 2)), mapper(Unsafe.Add(ref p, i + 3)));
            Unsafe.Add(ref q, i) = a;
            Unsafe.Add(ref q, i + 1) = b;
            Unsafe.Add(ref q, i + 2) = c;
            Unsafe.Add(ref q, i + 3) = d;
        }
        for (; i < newValues.Length; i++)
            Unsafe.Add(ref q, i) = mapper(Unsafe.Add(ref p, i));
        return newValues;
    }

    /// <summary>Gets the item with the maximum value in the array.</summary>
    /// <typeparam name="T">The type of the span.</typeparam>
    /// <param name="values">Array with data.</param>
    /// <returns>The item with the maximum value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Max<T>(this Span<T> values) where T : INumber<T>, IMinMaxValue<T>
    {
        if (V8.IsHardwareAccelerated && values.Length >= Vector512<T>.Count)
        {
            ref T p = ref MM.GetReference(values);
            ref T t = ref Unsafe.Add(ref p, values.Length - Vector512<T>.Count);
            Vector512<T> vm = V8.LoadUnsafe(ref p);
            p = ref Unsafe.Add(ref p, Vector512<T>.Count);
            for (; IsAddressLessThan(ref p, ref t); p = ref Unsafe.Add(ref p, Vector512<T>.Count))
                vm = V8.Max(vm, V8.LoadUnsafe(ref p));
            return V8.Max(vm, V8.LoadUnsafe(ref t)).Max();
        }
        if (V4.IsHardwareAccelerated && values.Length >= Vector256<T>.Count)
        {
            ref T p = ref MM.GetReference(values);
            ref T t = ref Unsafe.Add(ref p, values.Length - Vector256<T>.Count);
            Vector256<T> vm = V4.LoadUnsafe(ref p);
            p = ref Unsafe.Add(ref p, Vector256<T>.Count);
            for (; IsAddressLessThan(ref p, ref t); p = ref Unsafe.Add(ref p, Vector256<T>.Count))
                vm = V4.Max(vm, V4.LoadUnsafe(ref p));
            return V4.Max(vm, V4.LoadUnsafe(ref t)).Max();
        }
        T max = T.MinValue;
        foreach (T d in values)
            max = T.Max(max, d);
        return max;
    }

    /// <summary>Gets the item with the minimum value in the array.</summary>
    /// <typeparam name="T">The type of the span.</typeparam>
    /// <param name="values">Array with data.</param>
    /// <returns>The item with the minimum value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Min<T>(this Span<T> values) where T : INumber<T>, IMinMaxValue<T>
    {
        if (V8.IsHardwareAccelerated && values.Length >= Vector512<T>.Count)
        {
            ref T p = ref MM.GetReference(values);
            ref T t = ref Unsafe.Add(ref p, values.Length - Vector512<T>.Count);
            Vector512<T> vm = V8.LoadUnsafe(ref p);
            p = ref Unsafe.Add(ref p, V8d.Count);
            for (; IsAddressLessThan(ref p, ref t); p = ref Unsafe.Add(ref p, V8d.Count))
                vm = V8.Min(vm, V8.LoadUnsafe(ref p));
            return V8.Min(vm, V8.LoadUnsafe(ref t)).Min();
        }
        if (V4.IsHardwareAccelerated && values.Length >= Vector256<T>.Count)
        {
            ref T p = ref MM.GetReference(values);
            ref T t = ref Unsafe.Add(ref p, values.Length - Vector256<T>.Count);
            Vector256<T> vm = V4.LoadUnsafe(ref p);
            p = ref Unsafe.Add(ref p, Vector256<T>.Count);
            for (; IsAddressLessThan(ref p, ref t); p = ref Unsafe.Add(ref p, V4d.Count))
                vm = V4.Min(vm, V4.LoadUnsafe(ref p));
            return V4.Min(vm, V4.LoadUnsafe(ref t)).Min();
        }
        T min = T.MaxValue;
        foreach (T d in values)
            min = T.Min(min, d);
        return min;
    }

    /// <summary>Pointwise multiplication of two equally sized spans.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="span1">Span multiplicand.</param>
    /// <param name="span2">Span multiplier.</param>
    /// <returns>The pointwise multiplication of the two arguments.</returns>
    public static T[] Mul<T>(this Span<T> span1, Span<T> span2) where T : INumberBase<T>
    {
        T[] result = GC.AllocateUninitializedArray<T>(span1.Length);
        ref T a = ref MM.GetReference(span1);
        ref T b = ref MM.GetReference(span2);
        ref T c = ref MM.GetArrayDataReference(result);
        if (V8.IsHardwareAccelerated && result.Length >= Vector512<T>.Count)
        {
            nuint t = (nuint)(result.Length - Vector512<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref a, i) * V8.LoadUnsafe(ref b, i), ref c, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref a, t) * V8.LoadUnsafe(ref b, t), ref c, t);
        }
        else if (V4.IsHardwareAccelerated && result.Length >= Vector256<T>.Count)
        {
            nuint t = (nuint)(result.Length - Vector256<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref a, i) * V4.LoadUnsafe(ref b, i), ref c, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref a, t) * V4.LoadUnsafe(ref b, t), ref c, t);
        }
        else
            for (int i = 0; i < result.Length; i++)
                Unsafe.Add(ref c, i) = Unsafe.Add(ref a, i) * Unsafe.Add(ref b, i);
        return result;
    }

    /// <summary>Pointwise multiplication of a span and a scalar.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="span">Span multiplicand.</param>
    /// <param name="scalar">Scalar multiplier.</param>
    /// <param name="target">Target memory for the operation.</param>
    public static void Mul<T>(this Span<T> span, T scalar, Span<T> target) where T : INumberBase<T>
    {
        ref T p = ref MM.GetReference(span);
        ref T q = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= Vector512<T>.Count)
        {
            Vector512<T> vec = V8.Create(scalar);
            nuint t = (nuint)(target.Length - Vector512<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref p, i) * vec, ref q, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref p, t) * vec, ref q, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= Vector256<T>.Count)
        {
            Vector256<T> vec = V4.Create(scalar);
            nuint t = (nuint)(target.Length - Vector256<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref p, i) * vec, ref q, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref p, t) * vec, ref q, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Unsafe.Add(ref q, i) = Unsafe.Add(ref p, i) * scalar;
    }

    /// <summary>Pointwise negation of a span.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="span">Span to negate.</param>
    /// <param name="target">Target memory for the operation.</param>
    public static void Neg<T>(this Span<T> span, Span<T> target) where T : INumberBase<T>
    {
        ref T p = ref MM.GetReference(span);
        ref T q = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= Vector512<T>.Count)
        {
            nuint t = (nuint)(target.Length - Vector512<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(-V8.LoadUnsafe(ref p, i), ref q, i);
            V8.StoreUnsafe(-V8.LoadUnsafe(ref p, t), ref q, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= Vector256<T>.Count)
        {
            nuint t = (nuint)(target.Length - Vector256<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(-V4.LoadUnsafe(ref p, i), ref q, i);
            V4.StoreUnsafe(-V4.LoadUnsafe(ref p, t), ref q, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Unsafe.Add(ref q, i) = -Unsafe.Add(ref p, i);
    }

    /// <summary>Inplace pointwise negation of a span.</summary>
    /// <typeparam name="T">The type of the span.</typeparam>
    /// <param name="span">Span to negate.</param>
    public static void Neg<T>(this Span<T> span) where T : INumberBase<T>
    {
        ref T p = ref MM.GetReference(span);
        int i = 0;
        if (V8.IsHardwareAccelerated && span.Length >= Vector512<T>.Count)
            for (int top = span.Length & ~(Vector512<T>.Count - 1); i < top;
                i += Vector512<T>.Count, p = ref Unsafe.Add(ref p, Vector512<T>.Count))
                V8.StoreUnsafe(-V8.LoadUnsafe(ref p), ref p);
        else if (V4.IsHardwareAccelerated && span.Length >= Vector256<T>.Count)
            for (int top = span.Length & ~(Vector256<T>.Count - 1); i < top;
                i += Vector256<T>.Count, p = ref Unsafe.Add(ref p, Vector256<T>.Count))
                V4.StoreUnsafe(-V4.LoadUnsafe(ref p), ref p);
        for (; i < span.Length; i++, p = ref Unsafe.Add(ref p, 1))
            p = -p;
    }

    /// <summary>Calculates the product of the items of an array.</summary>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <param name="values">The array to calculate the product.</param>
    /// <returns>The product of all array items.</returns>
    public static T Product<T>(this T[] values) where T : INumberBase<T>
    {
        T result = T.MultiplicativeIdentity;
        ref T p = ref MM.GetArrayDataReference(values);
        ref T q = ref Unsafe.Add(ref p, values.Length);
        if (V8.IsHardwareAccelerated && values.Length > Vector512<T>.Count)
        {
            ref T last = ref Unsafe.Add(ref p, values.Length & ~(Vector512<T>.Count - 1));
            Vector512<T> prod = Vector512<T>.One;
            do
            {
                prod *= V8.LoadUnsafe(ref p);
                p = ref Unsafe.Add(ref p, Vector512<T>.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            result = (prod.GetLower() * prod.GetUpper()).Product();
        }
        else if (V4.IsHardwareAccelerated && values.Length > Vector256<T>.Count)
        {
            ref T last = ref Unsafe.Add(ref p, values.Length & ~(Vector256<T>.Count - 1));
            Vector256<T> prod = Vector256<T>.One;
            do
            {
                prod *= V4.LoadUnsafe(ref p);
                p = ref Unsafe.Add(ref p, Vector256<T>.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            result = prod.Product();
        }
        for (; IsAddressLessThan(ref p, ref q); p = ref Unsafe.Add(ref p, 1))
            result *= p;
        return result;
    }

    /// <summary>Creates an aggregate value by applying the reducer to each item.</summary>
    /// <typeparam name="T">The type of the span.</typeparam>
    /// <param name="span">The span to reduce.</param>
    /// <param name="seed">The initial value.</param>
    /// <param name="reducer">The reducing function.</param>
    /// <returns>The final synthesized value.</returns>
    public static T Reduce<T>(this Span<T> span, T seed, Func<T, T, T> reducer) where T : struct
    {
        foreach (T item in span)
            seed = reducer(seed, item);
        return seed;
    }

    /// <summary>Creates a reversed copy of an array.</summary>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <param name="values">The array to reverse.</param>
    /// <returns>An independent reversed copy.</returns>
    public static T[] Reverse<T>(this T[] values) where T : struct
    {
        T[] result = (T[])values.Clone();
        Array.Reverse(result);
        return result;
    }

    /// <summary>Creates a new array with sorted values.</summary>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <param name="values">The array to sort.</param>
    /// <returns>A new array with sorted values.</returns>
    public static T[] Sort<T>(this T[] values) where T : IComparable<T>
    {
        T[] result = (T[])values.Clone();
        Array.Sort(result);
        return result;
    }

    /// <summary>Creates a new array with sorted values.</summary>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <param name="values">The array to sort.</param>
    /// <returns>A new array with sorted values.</returns>
    public static T[] SortDescending<T>(this T[] values) where T : IComparable<T>
    {
        T[] result = (T[])values.Clone();
        Array.Sort(result, static (x, y) => y.CompareTo(x));
        return result;
    }

    /// <summary>Pointwise subtraction of two equally sized spans.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="span1">Minuend.</param>
    /// <param name="span2">Subtrahend.</param>
    /// <param name="target">The span to receive the result.</param>
    public static void Sub<T>(this Span<T> span1, Span<T> span2, Span<T> target)
        where T : INumberBase<T>
    {
        ref T a = ref MM.GetReference(span1);
        ref T b = ref MM.GetReference(span2);
        ref T c = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= Vector512<T>.Count)
        {
            nuint t = (nuint)(target.Length - Vector512<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref a, i) - V8.LoadUnsafe(ref b, i), ref c, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref a, t) - V8.LoadUnsafe(ref b, t), ref c, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= Vector256<T>.Count)
        {
            nuint t = (nuint)(target.Length - Vector256<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref a, i) - V4.LoadUnsafe(ref b, i), ref c, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref a, t) - V4.LoadUnsafe(ref b, t), ref c, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Unsafe.Add(ref c, i) = Unsafe.Add(ref a, i) - Unsafe.Add(ref b, i);
    }

    /// <summary>Pointwise inplace subtraction of two equally sized spans.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="span1">Minuend and target.</param>
    /// <param name="span2">Subtrahend.</param>
    public static void Sub<T>(this Span<T> span1, Span<T> span2) where T: INumberBase<T>
    {
        ref T a = ref MM.GetReference(span1);
        ref T b = ref MM.GetReference(span2);
        nuint i = 0;
        if (V8.IsHardwareAccelerated && span1.Length >= Vector512<T>.Count)
            for (nuint top = (nuint)(span1.Length & ~(Vector512<T>.Count - 1)); i < top;
                i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref a, i) - V8.LoadUnsafe(ref b, i), ref a, i);
        else if (V4.IsHardwareAccelerated && span1.Length >= Vector256<T>.Count)
            for (nuint top = (nuint)(span1.Length & ~(Vector256<T>.Count - 1));
                i < top; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref a, i) - V4.LoadUnsafe(ref b, i), ref a, i);
        for (; i < (nuint)span1.Length; i++)
            Unsafe.Add(ref a, i) = Unsafe.Add(ref a, i) - Unsafe.Add(ref b, i);
    }

    /// <summary>Pointwise subtraction of a scalar from a span.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="span">Array minuend.</param>
    /// <param name="scalar">Scalar subtrahend.</param>
    /// <param name="target">Target memory for the operation.</param>
    public static void Sub<T>(this Span<T> span, T scalar, Span<T> target)
        where T : INumberBase<T>
    {
        ref T p = ref MM.GetReference(span);
        ref T q = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= Vector512<T>.Count)
        {
            Vector512<T> vec = V8.Create(scalar);
            nuint t = (nuint)(target.Length - Vector512<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(V8.LoadUnsafe(ref p, i) - vec, ref q, i);
            V8.StoreUnsafe(V8.LoadUnsafe(ref p, t) - vec, ref q, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= Vector256<T>.Count)
        {
            Vector256<T> vec = V4.Create(scalar);
            nuint t = (nuint)(target.Length - Vector256<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref p, i) - vec, ref q, i);
            V4.StoreUnsafe(V4.LoadUnsafe(ref p, t) - vec, ref q, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Unsafe.Add(ref q, i) = Unsafe.Add(ref p, i) - scalar;
    }

    /// <summary>Pointwise subtraction of a span from a scalar.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="scalar">Scalar minuend.</param>
    /// <param name="span">Span subtrahend.</param>
    /// <param name="target">Target memory for the operation.</param>
    public static void Sub<T>(T scalar, Span<T> span, Span<T> target)
        where T : INumberBase<T>
    {
        ref T p = ref MM.GetReference(span);
        ref T q = ref MM.GetReference(target);
        if (V8.IsHardwareAccelerated && target.Length >= Vector512<T>.Count)
        {
            Vector512<T> vec = V8.Create(scalar);
            nuint t = (nuint)(target.Length - Vector512<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector512<T>.Count)
                V8.StoreUnsafe(vec - V8.LoadUnsafe(ref p, i), ref q, i);
            V8.StoreUnsafe(vec - V8.LoadUnsafe(ref p, t), ref q, t);
        }
        else if (V4.IsHardwareAccelerated && target.Length >= Vector256<T>.Count)
        {
            Vector256<T> vec = V4.Create(scalar);
            nuint t = (nuint)(target.Length - Vector256<T>.Count);
            for (nuint i = 0; i < t; i += (nuint)Vector256<T>.Count)
                V4.StoreUnsafe(vec - V4.LoadUnsafe(ref p, i), ref q, i);
            V4.StoreUnsafe(vec - V4.LoadUnsafe(ref p, t), ref q, t);
        }
        else
            for (int i = 0; i < target.Length; i++)
                Unsafe.Add(ref q, i) = scalar - Unsafe.Add(ref p, i);
    }

    /// <summary>Calculates the sum of the vector's items.</summary>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <param name="values">The vector to sum.</param>
    /// <returns>The sum of all vector's items.</returns>
    public static T Sum<T>(this T[] values) where T : INumberBase<T>
    {
        T result = T.AdditiveIdentity;
        ref T p = ref MM.GetArrayDataReference(values);
        ref T q = ref Unsafe.Add(ref p, values.Length);
        if (V8.IsHardwareAccelerated && values.Length > Vector512<T>.Count)
        {
            ref T last = ref Unsafe.Add(ref p, values.Length & ~(Vector512<T>.Count - 1));
            Vector512<T> sum = Vector512<T>.Zero;
            do
            {
                sum += V8.LoadUnsafe(ref p);
                p = ref Unsafe.Add(ref p, Vector512<T>.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            result = V8.Sum(sum);
        }
        else if (V4.IsHardwareAccelerated && values.Length > Vector256<T>.Count)
        {
            ref T last = ref Unsafe.Add(ref p, values.Length & ~(Vector256<T>.Count - 1));
            Vector256<T> sum = Vector256<T>.Zero;
            do
            {
                sum += V4.LoadUnsafe(ref p);
                p = ref Unsafe.Add(ref p, Vector256<T>.Count);
            }
            while (IsAddressLessThan(ref p, ref last));
            result = V4.Sum(sum);
        }
        for (; IsAddressLessThan(ref p, ref q); p = ref Unsafe.Add(ref p, 1))
            result += p;
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
        int r = cols + 1, size = Math.Min(rows, cols);
        for (ref double p = ref MM.GetArrayDataReference(values); size-- > 0; p = ref Unsafe.Add(ref p, r))
            trace += p;
        return trace;
    }

    /// <summary>Combines the common prefix of two spans.</summary>
    /// <typeparam name="T">The type of the spans.</typeparam>
    /// <param name="first">First span to combine.</param>
    /// <param name="second">Second span to combine.</param>
    /// <param name="zipper">The combining function.</param>
    /// <returns>The combining function applied to each pair of items.</returns>
    public static T[] Zip<T>(this Span<T> first, Span<T> second, Func<T, T, T> zipper) where T : struct
    {
        int len = Math.Min(first.Length, second.Length);
        T[] newValues = GC.AllocateUninitializedArray<T>(len);
        ref T p = ref MM.GetReference(first);
        ref T q = ref MM.GetReference(second);
        ref T r = ref MM.GetArrayDataReference(newValues);
        for (int i = 0; i < len; i++)
            Unsafe.Add(ref r, i) = zipper(Unsafe.Add(ref p, i), Unsafe.Add(ref q, i));
        return newValues;
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
            Unsafe.Add(ref q, j) = FusedMultiplyAdd(Unsafe.Add(ref p, j), d, Unsafe.Add(ref q, j));
    }

    /// <summary>
    /// Multiplies a span by a scalar and subtracts the result to a memory location.
    /// </summary>
    /// <param name="span">Source vector.</param>
    /// <param name="d">Scale factor.</param>
    /// <param name="target">The target memory of the whole operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MulNegStore(this Span<double> span, double d, Span<double> target)
    {
        ref double p = ref MM.GetReference(span);
        ref double q = ref MM.GetReference(target);

        nuint j = 0, c = (nuint)target.Length;
        if (Avx512F.IsSupported)
        {
            V8d vec = V8.Create(d);
            for (nuint t = c & Simd.MASK8; j < t; j += (nuint)V8d.Count)
                V8.StoreUnsafe(Avx512F.FusedMultiplyAddNegated(
                    V8.LoadUnsafe(ref p, j), vec, V8.LoadUnsafe(ref q, j)), ref q, j);
        }
        else if (Avx.IsSupported)
        {
            V4d vec = V4.Create(d);
            for (nuint t = c & Simd.MASK4; j < t; j += (nuint)V4d.Count)
                V4.StoreUnsafe(V4.LoadUnsafe(ref q, j)
                    .MultiplyAddNeg(V4.LoadUnsafe(ref p, j), vec), ref q, j);
        }
        for (; j < c; j++)
            Unsafe.Add(ref q, j) = FusedMultiplyAdd(Unsafe.Add(ref p, j), -d, Unsafe.Add(ref q, j));
    }

    /// <summary>Calculates the dot product of two spans.</summary>
    /// <remarks>The second span can be longer than the first span.</remarks>
    /// <param name="span1">First span operand.</param>
    /// <param name="span2">Second span operand.</param>
    /// <returns>The dot product of the vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(this Span<double> span1, Span<double> span2)
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
            sum = V4.Sum(acc);
        }
        for (int j = (int)i; j < span1.Length; j++)
            sum = FusedMultiplyAdd(Unsafe.Add(ref p, j), Unsafe.Add(ref q, j), sum);
        return sum;
    }

    /// <summary>Calculates the dot product of two spans.</summary>
    /// <remarks>The second span can be longer than the first span.</remarks>
    /// <param name="span1">First span operand.</param>
    /// <param name="span2">Second span operand.</param>
    /// <returns>The dot product of the vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Dot(this Span<int> span1, Span<int> span2)
    {
        int sum = 0;
        ref int p = ref MM.GetReference(span1);
        ref int q = ref MM.GetReference(span2);
        nuint i = 0;
        if (V8.IsHardwareAccelerated)
        {
            V8i acc = V8i.Zero;
            for (nuint top = (nuint)span1.Length & Simd.MASK16; i < top; i += (nuint)V8i.Count)
                acc += V8.LoadUnsafe(ref p, i) * V8.LoadUnsafe(ref q, i);
            sum = V8.Sum(acc);
        }
        else if (V4.IsHardwareAccelerated)
        {
            V4i acc = V4i.Zero;
            for (nuint top = (nuint)span1.Length & Simd.MASK8; i < top; i += (nuint)V4i.Count)
                acc += V4.LoadUnsafe(ref p, i) * V4.LoadUnsafe(ref q, i);
            sum = V4.Sum(acc);
        }
        for (int j = (int)i; j < span1.Length; j++)
            sum += Unsafe.Add(ref p, j) * Unsafe.Add(ref q, j);
        return sum;
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
            for (int r = 0, rsz = 0; r < s1; r += 4, rsz += s2 + s2)
            {
                for (int c = 0; c < r; c += 4)
                {
                    double* pp = a + (rsz + c);
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
                    double* pp = a + (rsz + r);
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
        string[] cells = [.. data.Select(formatter)];
        int width = Math.Max(3, cells.Max(c => c.Length));
        int cols = (TERMINAL_COLUMNS + 2) / (width + 2);
        StringBuilder sb = new(Math.Min(data.Length / cols, 12) * (TERMINAL_COLUMNS + 2));
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
                    widths[j] = Math.Max(widths[j], data[i, j]?.Length ?? 0);
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
