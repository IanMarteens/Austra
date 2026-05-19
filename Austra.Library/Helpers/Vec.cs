namespace Austra.Library.Helpers;

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

    /// <summary>Extension block for operations on generic numeric spans.</summary>
    /// <typeparam name="T">The type of the values elements.</typeparam>
    /// <param name="span1">The values to operate on.</param>
    extension<T>(Span<T> span1) where T : INumberBase<T>
    {
        /// <summary>Gets the absolute values of the array items.</summary>
        /// <returns>A new array with non-negative items.</returns>
        public T[] Abs()
        {
            T[] result = GC.AllocateUninitializedArray<T>(span1.Length);
            ref T p = ref MM.GetReference(span1);
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
        /// <param name="span2">Second operand.</param>
        /// <param name="target">The values to receive the sum of the first two argument.</param>
        public void Add(Span<T> span2, Span<T> target)
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
        /// <param name="span2">Second summand.</param>
        public void Add(Span<T> span2)
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

        /// <summary>Pointwise addition of a scalar to a values.</summary>
        /// <param name="scalar">Scalar summand.</param>
        /// <param name="target">Target memory for the operation.</param>
        public void Add(T scalar, Span<T> target)
        {
            ref T p = ref MM.GetReference(span1);
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

        /// <summary>Pointwise division of two equally sized spans.</summary>
        /// <param name="span2">Span divisor.</param>
        /// <returns>The pointwise quotient of the two arguments.</returns>
        public T[] Div(Span<T> span2)
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

        /// <summary>Pointwise multiplication of two equally sized spans.</summary>
        /// <param name="span2">Span multiplier.</param>
        /// <returns>The pointwise multiplication of the two arguments.</returns>
        public T[] Mul(Span<T> span2)
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

        /// <summary>Pointwise multiplication of a values and a scalar.</summary>
        /// <param name="scalar">Scalar multiplier.</param>
        /// <param name="target">Target memory for the operation.</param>
        public void Mul(T scalar, Span<T> target)
        {
            ref T p = ref MM.GetReference(span1);
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

        /// <summary>Pointwise negation of a values.</summary>
        /// <param name="target">Target memory for the operation.</param>
        public void Neg(Span<T> target)
        {
            ref T p = ref MM.GetReference(span1);
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

        /// <summary>Inplace pointwise negation of a values.</summary>
        public void Neg()
        {
            ref T p = ref MM.GetReference(span1);
            int i = 0;
            if (V8.IsHardwareAccelerated && span1.Length >= Vector512<T>.Count)
                for (int top = span1.Length & ~(Vector512<T>.Count - 1); i < top;
                    i += Vector512<T>.Count, p = ref Unsafe.Add(ref p, Vector512<T>.Count))
                    V8.StoreUnsafe(-V8.LoadUnsafe(ref p), ref p);
            else if (V4.IsHardwareAccelerated && span1.Length >= Vector256<T>.Count)
                for (int top = span1.Length & ~(Vector256<T>.Count - 1); i < top;
                    i += Vector256<T>.Count, p = ref Unsafe.Add(ref p, Vector256<T>.Count))
                    V4.StoreUnsafe(-V4.LoadUnsafe(ref p), ref p);
            for (; i < span1.Length; i++, p = ref Unsafe.Add(ref p, 1))
                p = -p;
        }

        /// <summary>Calculates the product of the items of an array.</summary>
        /// <returns>The product of all array items.</returns>
        public T Product()
        {
            T result = T.MultiplicativeIdentity;
            ref T p = ref MM.GetReference(span1);
            ref T q = ref Unsafe.Add(ref p, span1.Length);
            if (V8.IsHardwareAccelerated && span1.Length > Vector512<T>.Count)
            {
                ref T last = ref Unsafe.Add(ref p, span1.Length & ~(Vector512<T>.Count - 1));
                Vector512<T> prod = Vector512<T>.One;
                do
                {
                    prod *= V8.LoadUnsafe(ref p);
                    p = ref Unsafe.Add(ref p, Vector512<T>.Count);
                }
                while (IsAddressLessThan(ref p, ref last));
                result = (prod.GetLower() * prod.GetUpper()).Product();
            }
            else if (V4.IsHardwareAccelerated && span1.Length > Vector256<T>.Count)
            {
                ref T last = ref Unsafe.Add(ref p, span1.Length & ~(Vector256<T>.Count - 1));
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

        /// <summary>Pointwise subtraction of two equally sized spans.</summary>
        /// <param name="span2">Subtrahend.</param>
        /// <param name="target">The values to receive the result.</param>
        public void Sub(Span<T> span2, Span<T> target)
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
        /// <param name="span2">Subtrahend.</param>
        public void Sub(Span<T> span2)
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

        /// <summary>Pointwise subtraction of a scalar from a values.</summary>
        /// <param name="scalar">Scalar subtrahend.</param>
        /// <param name="target">Target memory for the operation.</param>
        public void Sub(T scalar, Span<T> target)
        {
            ref T p = ref MM.GetReference(span1);
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

        /// <summary>Calculates the sum of the vector's items.</summary>
        /// <returns>The sum of all vector's items.</returns>
        public T Sum()
        {
            T result = T.AdditiveIdentity;
            ref T p = ref MM.GetReference(span1);
            ref T q = ref Unsafe.Add(ref p, span1.Length);
            if (V8.IsHardwareAccelerated && span1.Length > Vector512<T>.Count)
            {
                ref T last = ref Unsafe.Add(ref p, span1.Length & ~(Vector512<T>.Count - 1));
                Vector512<T> sum = Vector512<T>.Zero;
                do
                {
                    sum += V8.LoadUnsafe(ref p);
                    p = ref Unsafe.Add(ref p, Vector512<T>.Count);
                }
                while (IsAddressLessThan(ref p, ref last));
                result = V8.Sum(sum);
            }
            else if (V4.IsHardwareAccelerated && span1.Length > Vector256<T>.Count)
            {
                ref T last = ref Unsafe.Add(ref p, span1.Length & ~(Vector256<T>.Count - 1));
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
    }

    /// <summary>
    /// Extension blocks for operations on generic spans with struct elements.
    /// </summary>
    /// <typeparam name="T">The type of the values elements.</typeparam>
    /// <param name="span">The values to operate on.</param>
    extension<T>(Span<T> span) where T : unmanaged, IEquatable<T>, IEqualityOperators<T, T, bool>
    {
        /// <summary>Checks whether the predicate is satisfied by all items.</summary>
        /// <param name="predicate">The predicate to be checked.</param>
        /// <returns><see langword="true"/> if all items satisfy the predicate.</returns>
        public bool All(Func<T, bool> predicate)
        {
            foreach (T item in span)
                if (!predicate(item))
                    return false;
            return true;
        }

        /// <summary>Checks whether the predicate is satisfied by at least one item.</summary>
        /// <param name="predicate">The predicate to be checked.</param>
        /// <returns><see langword="true"/> if there exists a item satisfying the predicate.</returns>
        public bool Any(Func<T, bool> predicate)
        {
            foreach (T item in span)
                if (predicate(item))
                    return true;
            return false;
        }

        /// <summary>Returns a new array with the distinct values in the values.</summary>
        /// <remarks>Results are unordered.</remarks>
        /// <returns>A new array with distinct values.</returns>
        public T[] Distinct() =>
            [.. (HashSet<T>)[.. span]];

        /// <summary>Creates a new array by filtering items with the given predicate.</summary>
        /// <param name="predicate">The predicate to evaluate.</param>
        /// <returns>A new array with the filtered items.</returns>
        public T[] Filter(Func<T, bool> predicate)
        {
            T[] newValues = GC.AllocateUninitializedArray<T>(span.Length);
            int j = 0;
            foreach (T value in span)
                if (predicate(value))
                    newValues[j++] = value;
            return j == 0 ? [] : j == span.Length ? span.ToArray() : newValues[..j];
        }

        /// <summary>Creates a new vector by filtering and mapping at the same time.</summary>
        /// <remarks>This method can save an intermediate buffer and one iteration.</remarks>
        /// <param name="predicate">The predicate to evaluate.</param>
        /// <param name="mapper">The mapping function.</param>
        /// <returns>A new array with the filtered items.</returns>
        public T[] FilterMap(Func<T, bool> predicate, Func<T, T> mapper)
        {
            T[] newValues = GC.AllocateUninitializedArray<T>(span.Length);
            int j = 0;
            foreach (T value in span)
                if (predicate(value))
                    newValues[j++] = mapper(value);
            return j == 0 ? [] : j == span.Length ? span.ToArray() : newValues[..j];
        }

        /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
        /// <param name="value">The value to locate.</param>
        /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
        public int IndexOf(T value)
        {
            ref T p = ref MM.GetReference(span);
            nuint size = (nuint)span.Length;
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
        /// <param name="mapper">The mapping function.</param>
        /// <returns>A new array with the transformed content.</returns>
        public T[] Map(Func<T, T> mapper)
        {
            T[] newValues = GC.AllocateUninitializedArray<T>(span.Length);
            ref T p = ref MM.GetReference(span);
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

        /// <summary>Creates an aggregate value by applying the reducer to each item.</summary>
        /// <param name="seed">The initial value.</param>
        /// <param name="reducer">The reducing function.</param>
        /// <returns>The final synthesized value.</returns>
        public T Reduce(T seed, Func<T, T, T> reducer)
        {
            foreach (T item in span)
                seed = reducer(seed, item);
            return seed;
        }

        /// <summary>Gets a text representation of an array.</summary>
        /// <param name="formatter">A formatter for items.</param>
        /// <returns>A text representation of the vector.</returns>
        public string ToString(Func<T, string> formatter)
        {
            if (span.Length == 0)
                return "";
            string[] cells = [.. span.ToArray().Select(formatter)];
            int width = Math.Max(3, cells.Max(c => c.Length));
            int cols = (TERMINAL_COLUMNS + 2) / (width + 2);
            StringBuilder sb = new(Math.Min(span.Length / cols, 12) * (TERMINAL_COLUMNS + 2));
            int offset = 0;
            for (int row = 0; row < 11 && offset < span.Length; row++)
            {
                for (int col = 0; col < cols && offset < span.Length; col++, offset++)
                {
                    sb.Append(cells[offset].PadLeft(width));
                    if (col < cols - 1)
                        sb.Append("  ");
                }
                sb.AppendLine();
            }
            if (offset < span.Length)
            {
                if (span.Length - offset <= cols)
                    for (int col = 0; col < cols && offset < span.Length; col++, offset++)
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

        /// <summary>Combines the common prefix of two spans.</summary>
        /// <param name="other">Second values to combine.</param>
        /// <param name="zipper">The combining function.</param>
        /// <returns>The combining function applied to each pair of items.</returns>
        public T[] Zip(Span<T> other, Func<T, T, T> zipper)
        {
            int len = Math.Min(span.Length, other.Length);
            T[] newValues = GC.AllocateUninitializedArray<T>(len);
            ref T p = ref MM.GetReference(span);
            ref T q = ref MM.GetReference(other);
            ref T r = ref MM.GetArrayDataReference(newValues);
            for (int i = 0; i < len; i++)
                Unsafe.Add(ref r, i) = zipper(Unsafe.Add(ref p, i), Unsafe.Add(ref q, i));
            return newValues;
        }
    }

    /// <summary>
    /// Extension block for operations on double values coming from matrices.
    /// </summary>
    /// <param name="values">The values to be transformed or queried.</param>
    extension(Span<double> values)
    {
        /// <summary>Initializes a values with random values.</summary>
        /// <param name="random">A random number generator.</param>
        public void CreateRandom(Random random)
        {
            ref double p = ref MM.GetReference(values);
            if (Avx512F.IsSupported && values.Length >= V8d.Count && random == Random.Shared)
            {
                nuint t = (nuint)(values.Length - V8d.Count);
                Random512 rnd512 = Random512.Shared;
                for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                    V8.StoreUnsafe(rnd512.NextDouble(), ref p, i);
                V8.StoreUnsafe(rnd512.NextDouble(), ref p, t);
            }
            else if (Avx2.IsSupported && values.Length >= V4d.Count && random == Random.Shared)
            {
                nuint t = (nuint)(values.Length - V4d.Count);
                Random256 rnd256 = Random256.Shared;
                for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                    V4.StoreUnsafe(rnd256.NextDouble(), ref p, i);
                V4.StoreUnsafe(rnd256.NextDouble(), ref p, t);
            }
            else
                for (int i = 0; i < values.Length; i++)
                    Unsafe.Add(ref p, i) = random.NextDouble();
        }

        /// <summary>Initializes a values with random values.</summary>
        /// <param name="random">A random number generator.</param>
        /// <param name="offset">An offset for the random numbers.</param>
        /// <param name="width">Width for the uniform distribution.</param>
        public void CreateRandom(Random random, double offset, double width)
        {
            ref double p = ref MM.GetReference(values);
            if (Avx512F.IsSupported && values.Length >= V8d.Count && random == Random.Shared)
            {
                nuint t = (nuint)(values.Length - V8d.Count);
                V8d vOff = V8.Create(offset);
                V8d vWidth = V8.Create(width);
                Random512 rnd512 = Random512.Shared;
                for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                    V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(rnd512.NextDouble(), vWidth, vOff), ref p, i);
                V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(rnd512.NextDouble(), vWidth, vOff), ref p, t);
            }
            else if (Avx2.IsSupported && values.Length >= V4d.Count && random == Random.Shared)
            {
                nuint t = (nuint)(values.Length - V4d.Count);
                V4d vOff = V4.Create(offset);
                V4d vWidth = V4.Create(width);
                Random256 rnd256 = Random256.Shared;
                for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                    V4.StoreUnsafe(rnd256.NextDouble().MultiplyAdd(vWidth, vOff), ref p, i);
                V4.StoreUnsafe(rnd256.NextDouble().MultiplyAdd(vWidth, vOff), ref p, t);
            }
            else
                for (int i = 0; i < values.Length; i++)
                    Unsafe.Add(ref p, i) = FusedMultiplyAdd(random.NextDouble(), width, offset);
        }

        /// <summary>Initializes a values with normal random values.</summary>
        /// <param name="random">A random number generator.</param>
        public void CreateRandom(NormalRandom random)
        {
            ref double p = ref MM.GetReference(values);
            if (Avx512F.IsSupported && values.Length >= V8d.Count && random == NormalRandom.Shared)
            {
                nuint t = (nuint)(values.Length - V8d.Count);
                Random512 rnd512 = Random512.Shared;
                for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                    V8.StoreUnsafe(rnd512.NextNormal(), ref p, i);
                V8.StoreUnsafe(rnd512.NextNormal(), ref p, t);
            }
            else if (Avx2.IsSupported && values.Length >= V4d.Count && random == NormalRandom.Shared)
            {
                nuint t = (nuint)(values.Length - V4d.Count);
                Random256 rnd256 = Random256.Shared;
                for (nuint i = 0; i < t; i += (nuint)V4d.Count)
                    V4.StoreUnsafe(rnd256.NextNormal(), ref p, i);
                V4.StoreUnsafe(rnd256.NextNormal(), ref p, t);
            }
            else
            {
                int i = 0;
                for (int t = values.Length & ~1; i < t; i += 2)
                    random.NextDoubles(ref Unsafe.Add(ref p, i));
                if (i < values.Length)
                    Unsafe.Add(ref p, i) = random.NextDouble();
            }
        }

        /// <summary>Gets the main diagonal of a 1D-array.</summary>
        /// <param name="rows">Number of rows.</param>
        /// <param name="cols">Number of columns.</param>
        /// <returns>A vector containing values in the main diagonal.</returns>
        public double[] Diagonal(int rows, int cols)
        {
            Contract.Ensures(Contract.Result<DVector>().Length == Math.Min(rows, cols));

            int r = cols + 1, size = Math.Min(rows, cols);
            double[] result = GC.AllocateUninitializedArray<double>(size);
            ref double a = ref MM.GetReference(values);
            ref double b = ref MM.GetArrayDataReference(result);
            for (; size-- > 0; a = ref Unsafe.Add(ref a, r), b = ref Unsafe.Add(ref b, 1))
                b = a;
            return result;
        }

        /// <summary>Gets the product of the cells in the main diagonal.</summary>
        /// <param name="rows">Number of rows.</param>
        /// <param name="cols">Number of columns.</param>
        /// <returns>The product of the main diagonal.</returns>
        public double Det(int rows, int cols)
        {
            int r = cols + 1, size = Math.Min(rows, cols);
            double product = 1.0;
            for (ref double p = ref MM.GetReference(values); size-- > 0; p = ref Unsafe.Add(ref p, r))
                product *= p;
            return product;
        }

        /// <summary>Computes the maximum difference between two spans.</summary>
        /// <remarks>Spans can be of different lengths.</remarks>
        /// <param name="other">Second values.</param>
        /// <returns>The max-norm of the vector difference.</returns>
        public double Distance(Span<double> other)
        {
            int len = Math.Min(values.Length, other.Length);
            if (V8.IsHardwareAccelerated && len >= V8d.Count)
            {
                ref double p = ref MM.GetReference(values);
                ref double q = ref MM.GetReference(other);
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
                ref double p = ref MM.GetReference(values);
                ref double q = ref MM.GetReference(other);
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
                double v = Math.Abs(values[i] - other[i]);
                if (v > max)
                    max = v;
            }
            return max;
        }

        /// <summary>Calculates the dot product of two spans.</summary>
        /// <remarks>The second values can be longer than the first values.</remarks>
        /// <param name="span2">Second values operand.</param>
        /// <returns>The dot product of the vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Dot(Span<double> span2)
        {
            double sum = 0;
            ref double p = ref MM.GetReference(values);
            ref double q = ref MM.GetReference(span2);
            nuint i = 0;
            if (V8.IsHardwareAccelerated && Avx512F.IsSupported)
            {
                V8d acc = V8d.Zero;
                for (nuint top = (nuint)values.Length & Simd.MASK8; i < top; i += (nuint)V8d.Count)
                    acc = Avx512F.FusedMultiplyAdd(V8.LoadUnsafe(ref p, i), V8.LoadUnsafe(ref q, i), acc);
                sum = V8.Sum(acc);
            }
            else if (V4.IsHardwareAccelerated)
            {
                V4d acc = V4d.Zero;
                for (nuint top = (nuint)values.Length & Simd.MASK4; i < top; i += (nuint)V4d.Count)
                    acc = acc.MultiplyAdd(V4.LoadUnsafe(ref p, i), V4.LoadUnsafe(ref q, i));
                sum = V4.Sum(acc);
            }
            for (int j = (int)i; j < values.Length; j++)
                sum = FusedMultiplyAdd(Unsafe.Add(ref p, j), Unsafe.Add(ref q, j), sum);
            return sum;
        }

        /// <summary>Matrix multiplication implementation.</summary>
        /// <param name="other">Second matrix.</param>
        /// <param name="result">Result matrix.</param>
        /// <param name="m">Number of rows in the first matrix.</param>
        /// <param name="n">Number of columns in the first matrix / rows in the second matrix.</param>
        /// <param name="p">Number of columns in the second matrix.</param>
        public void MatrixMult(double[] other, double[] result, int m, int n, int p)
        {
            const long MINSIZE = 64L * 64L * 64L;
            const long MAXSIZE = 1024L * 1024L * 1024L;
            long size = (long)m * n * p;

            ref double a = ref MM.GetReference(values);
            ref double b = ref MM.GetArrayDataReference(other);
            ref double c = ref MM.GetArrayDataReference(result);

            if (size <= MINSIZE)
            {
                nuint top8 = (nuint)(p & Simd.MASK8);
                nuint top4 = (nuint)(p & Simd.MASK4);
                for (int i = 0, top = p & Simd.MASK4; i < m; i++)
                {
                    ref double pb = ref b;
                    for (int k = 0; k < n; k++)
                    {
                        double d = Unsafe.Add(ref a, k);
                        nuint j = 0;
                        if (Avx512F.IsSupported)
                            for (V8d vd = V8.Create(d); j < top8; j += (nuint)V8d.Count)
                                V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                                    V8.LoadUnsafe(ref pb, j), vd, V8.LoadUnsafe(ref c, j)),
                                    ref c, j);
                        if (Avx.IsSupported)
                            for (V4d vd = V4.Create(d); j < top4; j += (nuint)V4d.Count)
                                V4.StoreUnsafe(V4.LoadUnsafe(ref c, j).MultiplyAdd(
                                    V4.LoadUnsafe(ref pb, j), vd),
                                    ref c, j);
                        for (; j < (nuint)p; j++)
                            Unsafe.Add(ref c, j) = FusedMultiplyAdd(
                                Unsafe.Add(ref pb, j), d, Unsafe.Add(ref c, j));
                        pb = ref Unsafe.Add(ref pb, p);
                    }
                    a = ref Unsafe.Add(ref a, n);
                    c = ref Unsafe.Add(ref c, p);
                }
            }
            else if (size < MAXSIZE)
            {
                const int BLK_SIZE = 128;
                int pbl = p * BLK_SIZE;
                for (int ii = 0; ii < m; ii += BLK_SIZE)
                    for (int kk = 0, pkk = 0; kk < n; kk += BLK_SIZE, pkk += pbl)
                        for (int jj = 0; jj < p; jj += BLK_SIZE)
                        {
                            ref double pa = ref Unsafe.Add(ref a, n * ii);
                            ref double pc = ref Unsafe.Add(ref c, p * ii);
                            int topi = Math.Min(m, ii + BLK_SIZE);
                            nuint topj = (nuint)Math.Min(p, jj + BLK_SIZE);
                            nuint top = (nuint)((((uint)topj - jj) & ~15) + jj);
                            for (int i = ii; i < topi; i++)
                            {
                                ref double pb = ref Unsafe.Add(ref b, pkk);
                                int topk = Math.Min(n, kk + BLK_SIZE);
                                for (int k = kk; k < topk; k++)
                                {
                                    double d = Unsafe.Add(ref pa, k);
                                    nuint j = (nuint)jj;
                                    if (Avx512F.IsSupported)
                                        for (V8d vd = V8.Create(d); j < top; j += 16)
                                        {
                                            V8d op1 = V8.LoadUnsafe(ref pb, j);
                                            V8d op2 = V8.LoadUnsafe(ref pb, j + 8);
                                            V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                                                op1, vd, V8.LoadUnsafe(ref pc, j)),
                                                ref pc, j);
                                            V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                                                op2, vd, V8.LoadUnsafe(ref pc, j + 8)),
                                                ref pc, j + 8);
                                        }
                                    else if (Avx.IsSupported)
                                        for (V4d vd = V4.Create(d); j < top; j += 16)
                                        {
                                            V4.StoreUnsafe(V4.LoadUnsafe(ref pc, j)
                                                .MultiplyAdd(V4.LoadUnsafe(ref pb, j), vd),
                                                ref pc, j);
                                            V4.StoreUnsafe(V4.LoadUnsafe(ref pc, j + 4)
                                                .MultiplyAdd(V4.LoadUnsafe(ref pb, j + 4), vd),
                                                ref pc, j + 4);
                                            V4.StoreUnsafe(V4.LoadUnsafe(ref pc, j + 8)
                                                .MultiplyAdd(V4.LoadUnsafe(ref pb, j + 8), vd),
                                                ref pc, j + 8);
                                            V4.StoreUnsafe(V4.LoadUnsafe(ref pc, j + 12)
                                                .MultiplyAdd(V4.LoadUnsafe(ref pb, j + 12), vd),
                                                ref pc, j + 12);
                                        }
                                    for (; j < topj; j++)
                                        Unsafe.Add(ref pc, j) = FusedMultiplyAdd(
                                            d, Unsafe.Add(ref pb, j), Unsafe.Add(ref pc, j));
                                    pb = ref Unsafe.Add(ref pb, p);
                                }
                                pa = ref Unsafe.Add(ref pa, n);
                                pc = ref Unsafe.Add(ref pc, p);
                            }
                        }
            }
            else
            {
                const int BLK_SIZE = 256;
                int pbl = p * BLK_SIZE;
                for (int ii = 0; ii < m; ii += BLK_SIZE)
                    for (int kk = 0, pkk = 0; kk < n; kk += BLK_SIZE, pkk += pbl)
                        for (int jj = 0; jj < p; jj += BLK_SIZE)
                        {
                            ref double pa = ref Unsafe.Add(ref a, n * ii);
                            ref double pc = ref Unsafe.Add(ref c, p * ii);
                            int topi = Math.Min(m, ii + BLK_SIZE);
                            nuint topj = (nuint)Math.Min(p, jj + BLK_SIZE);
                            nuint top = (nuint)((((uint)topj - jj) & ~15) + jj);
                            for (int i = ii; i < topi; i++)
                            {
                                ref double pb = ref Unsafe.Add(ref b, pkk);
                                int topk = Math.Min(n, kk + BLK_SIZE);
                                for (int k = kk; k < topk; k++)
                                {
                                    double d = Unsafe.Add(ref pa, k);
                                    nuint j = (nuint)jj;
                                    if (Avx512F.IsSupported)
                                        for (V8d vd = V8.Create(d); j < top; j += 16)
                                        {
                                            V8d op1 = V8.LoadUnsafe(ref pb, j);
                                            V8d op2 = V8.LoadUnsafe(ref pb, j + 8);
                                            V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                                                op1, vd, V8.LoadUnsafe(ref pc, j)), ref pc, j);
                                            V8.StoreUnsafe(Avx512F.FusedMultiplyAdd(
                                                op2, vd, V8.LoadUnsafe(ref pc, j + 8)), ref pc, j + 8);
                                        }
                                    if (Avx.IsSupported)
                                        for (var vd = V4.Create(d); j < top; j += 16)
                                        {
                                            V4.StoreUnsafe(V4.LoadUnsafe(ref pc, j)
                                                .MultiplyAdd(V4.LoadUnsafe(ref pb, j), vd),
                                                ref pc, j);
                                            V4.StoreUnsafe(V4.LoadUnsafe(ref pc, j + 4)
                                                .MultiplyAdd(V4.LoadUnsafe(ref pb, j + 4), vd),
                                                ref pc, j + 4);
                                            V4.StoreUnsafe(V4.LoadUnsafe(ref pc, j + 8)
                                                .MultiplyAdd(V4.LoadUnsafe(ref pb, j + 8), vd),
                                                ref pc, j + 8);
                                            V4.StoreUnsafe(V4.LoadUnsafe(ref pc, j + 12)
                                                .MultiplyAdd(V4.LoadUnsafe(ref pb, j + 12), vd),
                                                ref pc, j + 12);
                                        }
                                    for (; j < topj; j++)
                                        Unsafe.Add(ref pc, j) = FusedMultiplyAdd(
                                            d, Unsafe.Add(ref pb, j), Unsafe.Add(ref pc, j));
                                    pb = ref Unsafe.Add(ref pb, p);
                                }
                                pa = ref Unsafe.Add(ref pa, n);
                                pc = ref Unsafe.Add(ref pc, p);
                            }
                        }
            }
        }

        /// <summary>
        /// Multiplies a values by a scalar and sums the result to a memory location.
        /// </summary>
        /// <param name="d">Scale factor.</param>
        /// <param name="target">The target memory of the whole operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MulAddStore(double d, Span<double> target)
        {
            ref double p = ref MM.GetReference(values);
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
        /// Multiplies a values by a scalar and subtracts the result to a memory location.
        /// </summary>
        /// <param name="d">Scale factor.</param>
        /// <param name="target">The target memory of the whole operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MulNegStore(double d, Span<double> target)
        {
            ref double p = ref MM.GetReference(values);
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

        /// <summary>Calculates the trace of a 1D-array.</summary>
        /// <param name="rows">Number of rows.</param>
        /// <param name="cols">Number of columns.</param>
        /// <returns>The sum of the cells in the main diagonal.</returns>
        public double Trace(int rows, int cols)
        {
            double trace = 0;
            int r = cols + 1, size = Math.Min(rows, cols);
            for (ref double p = ref MM.GetReference(values); size-- > 0; p = ref Unsafe.Add(ref p, r))
                trace += p;
            return trace;
        }

        /// <summary>Gets a text representation of a matrix.</summary>
        /// <param name="rowCount">Number of rows.</param>
        /// <param name="colCount">Number of columns.</param>
        /// <param name="formatter">Converts items to text.</param>
        /// <param name="triangularity">Which part of the matrix is significative.</param>
        /// <returns>A text representation of the matrix.</returns>
        public string ToString(
            int rowCount, int colCount,
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
                columnsLeft.Add(FormatColumn(values, j, rows, upper, lower));

            List<(int, string[])> columnsRight = new(right);
            for (int j = 0; j < right; j++)
                columnsRight.Add(FormatColumn(values, colCount - right + j, rows, upper, lower));

            int chars = columnsLeft.Sum(t => t.Item1 + 2) + columnsRight.Sum(t => t.Item1 + 2);
            for (int j = left; j < colCount - right; j++)
            {
                (int, string[]) candidate = FormatColumn(values, j, rows, upper, lower);
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

            (int, string[]) FormatColumn(Span<double> values, int column, int height, int upper, int lower)
            {
                string[] c = new string[height];
                int index = 0;
                for (int row = 0; row < upper; row++)
                    c[index++] = Filter(values[row * colCount + column], row, column);
                if (rowEllipsis)
                    c[index++] = "";
                for (int row = rowCount - lower; row < rowCount; row++)
                    c[index++] = Filter(values[row * colCount + column], row, column);
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

    /// <summary>Gets the item in a values with the maximum absolute value.</summary>
    /// <param name="span">The values values.</param>
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

    /// <summary>Gets the item in a values with the minimum absolute value.</summary>
    /// <param name="span">The values values.</param>
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

    /// <summary>Deconstruct a complex number into its real and imaginary parts.</summary>
    /// <param name="complex">The value to be deconstructed.</param>
    /// <param name="real">The real part.</param>
    /// <param name="imaginary">The imaginary part.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deconstruct(this Complex complex, out double real, out double imaginary) =>
        (real, imaginary) = (complex.Real, complex.Imaginary);

    /// <summary>Pointwise division of a values by an integer.</summary>
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

    /// <summary>Checks two arrays of dates for equality.</summary>
    /// <param name="array1">First array operand.</param>
    /// <param name="array2">Second array operand.</param>
    /// <returns><see langword="true"/> if both array has the same items.</returns>
    public static bool Eqs(this Date[] array1, Date[] array2)
    {
        if (array1.Length != array2.Length)
            return false;
        ref uint p = ref As<Date, uint>(ref MM.GetArrayDataReference(array1));
        ref uint q = ref As<Date, uint>(ref MM.GetArrayDataReference(array2));
        if (V8.IsHardwareAccelerated && array1.Length >= Vector512<uint>.Count)
        {
            ref uint lstP = ref Unsafe.Add(ref p, array1.Length - Vector512<uint>.Count);
            ref uint lstQ = ref Unsafe.Add(ref q, array1.Length - Vector512<uint>.Count);
            for (; IsAddressLessThan(ref p, ref lstP); p = ref Unsafe.Add(ref p, Vector512<uint>.Count),
                q = ref Unsafe.Add(ref q, Vector512<uint>.Count))
                if (!V8.EqualsAll(V8.LoadUnsafe(ref p), V8.LoadUnsafe(ref q)))
                    return false;
            if (!V8.EqualsAll(V8.LoadUnsafe(ref lstP), V8.LoadUnsafe(ref lstQ)))
                return false;
        }
        else if (V4.IsHardwareAccelerated && array1.Length >= Vector256<uint>.Count)
        {
            ref uint lstP = ref Unsafe.Add(ref p, array1.Length - Vector256<uint>.Count);
            ref uint lstQ = ref Unsafe.Add(ref q, array1.Length - Vector256<uint>.Count);
            for (; IsAddressLessThan(ref p, ref lstP); p = ref Unsafe.Add(ref p, Vector256<uint>.Count),
                q = ref Unsafe.Add(ref q, Vector256<uint>.Count))
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

    /// <summary>Gets the item with the maximum value in the array.</summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="values">Array with values.</param>
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
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="values">Array with values.</param>
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

    /// <summary>Pointwise subtraction of a values from a scalar.</summary>
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

    /// <summary>Calculates the dot product of two spans.</summary>
    /// <remarks>The second values can be longer than the first values.</remarks>
    /// <param name="span1">First values operand.</param>
    /// <param name="span2">Second values operand.</param>
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
    /// <param name="data">A 1D-array with values.</param>
    public unsafe static void Transpose(int rows, int cols, double[] data)
    {
        Contract.Requires(rows == cols);
        fixed (double* a = data)
            Transpose(a, rows);
    }

    /// <summary>In place transposition of a square matrix.</summary>
    /// <param name="a">Pointer to raw values.</param>
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
}
