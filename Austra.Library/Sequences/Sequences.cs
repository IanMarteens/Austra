namespace Austra.Library;

/// <summary>Represents any sequence returning a double value.</summary>
public abstract partial class DoubleSequence :
    IFormattable, 
    IEquatable<DoubleSequence>,
    IEqualityOperators<DoubleSequence, DoubleSequence, bool>,
    IAdditionOperators<DoubleSequence, DoubleSequence, DoubleSequence>,
    IAdditionOperators<DoubleSequence, double, DoubleSequence>,
    ISubtractionOperators<DoubleSequence, DoubleSequence, DoubleSequence>,
    ISubtractionOperators<DoubleSequence, double, DoubleSequence>,
    IMultiplyOperators<DoubleSequence, DoubleSequence, double>,
    IMultiplyOperators<DoubleSequence, double, DoubleSequence>,
    IDivisionOperators<DoubleSequence, double, DoubleSequence>,
    IUnaryNegationOperators<DoubleSequence, DoubleSequence>
{
    /// <summary>Gets the next number in the sequence.</summary>
    /// <param name="value">The next number in the sequence.</param>
    /// <returns><see langword="true"/>, when there is a next number.</returns>
    public abstract bool Next(out double value);

    /// <summary>Performs a shallow copy of the sequence.</summary>
    /// <returns>A shallow copy of the sequence.</returns>
    public DoubleSequence Clone() => (DoubleSequence)MemberwiseClone();

    /// <summary>Resets the sequence.</summary>
    /// <returns>Echoes this sequence.</returns>
    public virtual DoubleSequence Reset() => this;

    /// <summary>Transform a sequence acording to the function passed as parameter.</summary>
    /// <param name="mapper">The transforming function.</param>
    /// <returns>The transformed sequence.</returns>
    public virtual DoubleSequence Map(Func<double, double> mapper) =>
        new Mapped(this, mapper);

    /// <summary>Transform a sequence acording to the predicate passed as parameter.</summary>
    /// <param name="filter">A predicate for selecting surviving values</param>
    /// <returns>The filtered sequence.</returns>
    public DoubleSequence Filter(Func<double, bool> filter) =>
        new Filtered(this, filter);

    /// <summary>Joins the common part of two sequence with the help of a lambda.</summary>
    /// <param name="other">The second sequence.</param>
    /// <param name="zipper">The joining sequence.</param>
    /// <returns>The combined sequence.</returns>
    public DoubleSequence Zip(DoubleSequence other, Func<double, double, double> zipper) =>
        new Zipped(this, other, zipper);

    /// <summary>Creates a sequence from a range.</summary>
    /// <param name="first">The first value in the sequence.</param>
    /// <param name="last">The last value in the sequence.</param>
    /// <returns>A sequence returning a range of values.</returns>
    public static DoubleSequence Create(int first, int last) =>
        first <= last ? new RangeSequence(first, last) : new RangeSequenceDesc(first, last);

    /// <summary>Creates a sequence from a uniform grid.</summary>
    /// <param name="lower">The first value in the sequence.</param>
    /// <param name="upper">The last value in the sequence.</param>
    /// <param name="steps">The number of steps in the sequence, minus one.</param>
    /// <returns>A sequence returning a uniform grid of values.</returns>
    public static DoubleSequence Create(double lower, double upper, int steps) =>
        new GridSequence(lower, upper, steps);

    /// <summary>Creates a sequence from a vector.</summary>
    /// <param name="vector">The vector containing the sequence's values.</param>
    /// <returns>The sequence encapsulating the vector.</returns>
    public static DoubleSequence Create(Vector vector) =>
        new VectorSequence(vector);

    /// <summary>Creates a sequence from a time series.</summary>
    /// <param name="series">The series containing the sequence's values.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DoubleSequence Create(Series series) =>
        new VectorSequence(series);

    /// <summary>Creates a sequence from random values.</summary>
    /// <param name="size">The size of the series.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DoubleSequence Random(int size) =>
        new RandomSequence(size, System.Random.Shared);

    /// <summary>Creates a sequence from normal random values.</summary>
    /// <param name="size">The size of the series.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DoubleSequence NormalRandom(int size) =>
        new NormalRandomSequence(size, Library.Stats.NormalRandom.Shared);

    /// <summary>Creates a sequence from normal random values.</summary>
    /// <param name="size">The size of the series.</param>
    /// <param name="variance">The variance of the normal distribution.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DoubleSequence NormalRandom(int size, double variance) =>
        new NormalRandomSequence(size, new NormalRandom(0, Sqrt(variance)));

    /// <summary>Creates a sequence from normal random values.</summary>
    /// <param name="size">The size of the series.</param>
    /// <param name="variance">The variance of the normal distribution.</param>
    /// <param name="coefficients">Autoregressive coefficients.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DoubleSequence NormalRandom(int size, double variance, Vector coefficients) =>
        coefficients.Length == 0
        ? throw new VectorLengthException()
        : new ArSequence(size, variance, coefficients);

    /// <summary>Adds the common part of two sequences.</summary>
    /// <param name="s1">First sequence operand.</param>
    /// <param name="s2">Second sequence operand.</param>
    /// <returns>The component by component sum of the sequences.</returns>
    public static DoubleSequence operator+(DoubleSequence s1, DoubleSequence s2)
    {
        double[] a1 = s1.Materialize();
        double[] a2 = s2.Materialize();
        double[] r = GC.AllocateUninitializedArray<double>(Math.Min(a1.Length, a2.Length));
        a1.AsSpan(0, r.Length).AddV(a2.AsSpan(0, r.Length), r);
        return new VectorSequence(r);
    }

    /// <summary>Adds a scalar value to a sequence.</summary>
    /// <param name="s">Sequence operand.</param>
    /// <param name="d">Scalar operand.</param>
    /// <returns>The component by component sum of the sequence and the scalar.</returns>
    public static DoubleSequence operator +(DoubleSequence s, double d)
    {
        if (!s.HasStorage)
            return s.Map(x => x + d);
        double[] a = s.Materialize();
        double[] r = GC.AllocateUninitializedArray<double>(a.Length);
        a.AsSpan().AddV(d, r.AsSpan());
        return new VectorSequence(r);
    }

    /// <summary>Adds a sequence to a scalar value.</summary>
    /// <param name="d">Scalar operand.</param>
    /// <param name="s">Sequence operand.</param>
    /// <returns>The component by component sum of the scalar and the sequence.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DoubleSequence operator +(double d, DoubleSequence s) => s + d;

    /// <summary>Subtracts a scalar common part of two sequences.</summary>
    /// <param name="s1">Sequence minuend.</param>
    /// <param name="s2">Sequence subtrahend.</param>
    /// <returns>The component by component subtraction of the sequences.</returns>
    public static DoubleSequence operator -(DoubleSequence s1, DoubleSequence s2)
    {
        double[] a1 = s1.Materialize();
        double[] a2 = s2.Materialize();
        double[] r = GC.AllocateUninitializedArray<double>(Math.Min(a1.Length, a2.Length));
        a1.AsSpan(0, r.Length).SubV(a2.AsSpan(0, r.Length), r);
        return new VectorSequence(r);
    }

    /// <summary>Subtracts a scalar from a sequence.</summary>
    /// <param name="s">Sequence minuend.</param>
    /// <param name="d">Scalar subtrahend.</param>
    /// <returns>The component by component subtraction of the sequence and the scalar.</returns>
    public static DoubleSequence operator -(DoubleSequence s, double d)
    {
        if (!s.HasStorage)
            return s.Map(x => x - d);
        double[] a = s.Materialize();
        double[] r = GC.AllocateUninitializedArray<double>(a.Length);
        a.AsSpan().SubV(d, r.AsSpan());
        return new VectorSequence(r);
    }

    /// <summary>Subtracts a scalar from a sequence.</summary>
    /// <param name="s">Sequence minuend.</param>
    /// <param name="d">Scalar subtrahend.</param>
    /// <returns>The component by component subtraction of the sequence and the scalar.</returns>
    public static DoubleSequence operator -(double d, DoubleSequence s)
    {
        if (!s.HasStorage)
            return s.Map(x => d - x);
        double[] a = s.Materialize();
        double[] r = GC.AllocateUninitializedArray<double>(a.Length);
        CommonMatrix.SubV(d, a, r);
        return new VectorSequence(r);
    }

    /// <summary>Negates a sequence.</summary>
    /// <param name="s">The sequence operand.</param>
    /// <returns>The component by component negation.</returns>
    public static DoubleSequence operator -(DoubleSequence s)
    {
        if (!s.HasStorage)
            return s.Negate();
        double[] a = s.Materialize();
        double[] r = GC.AllocateUninitializedArray<double>(a.Length);
        a.AsSpan().NegV(r);
        return new VectorSequence(r);
    }

    /// <summary>Negates a sequence without an underlying storage.</summary>
    /// <returns>The negated sequence.</returns>
    protected virtual DoubleSequence Negate() => Map(x => -x);

    /// <summary>Calculates the scalar product of the common part of two sequences.</summary>
    /// <param name="s1">First sequence.</param>
    /// <param name="s2">Second sequence.</param>
    /// <returns>The dot product of the common part.</returns>
    public static double operator*(DoubleSequence s1, DoubleSequence s2)
    {
        if (!s1.HasStorage && !s2.HasStorage)
            return s1.Zip(s2, (x, y) => x * y).Sum();
        double[] a1 = s1.Materialize();
        double[] a2 = s2.Materialize();
        int size = Math.Min(a1.Length, a2.Length);
        return a1.AsSpan(0, size).DotProduct(a2.AsSpan(0, size));
    }

    /// <summary>Multiplies a sequence by a scalar value.</summary>
    /// <param name="s">Sequence multiplicand.</param>
    /// <param name="d">A scalar multiplier.</param>
    /// <returns>The multiplication of the sequence by the scalar.</returns>
    public static DoubleSequence operator *(DoubleSequence s, double d)
    {
        if (!s.HasStorage)
            return s.Map(x => d * x);
        double[] a = s.Materialize();
        double[] r = GC.AllocateUninitializedArray<double>(a.Length);
        a.AsSpan().MulV(d, r.AsSpan());
        return new VectorSequence(r);
    }

    /// <summary>Multiplies a scalar value by a sequence.</summary>
    /// <param name="d">Scalar multiplicand.</param>
    /// <param name="s">Sequence multiplier.</param>
    /// <returns>The multiplication of the sequence by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DoubleSequence operator *(double d, DoubleSequence s) => s * d;

    /// <summary>Divides a sequence by a scalar value.</summary>
    /// <param name="s">Sequence dividend.</param>
    /// <param name="d">A scalar divisor.</param>
    /// <returns>The quotient of the sequence and the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DoubleSequence operator /(DoubleSequence s, double d) => s * (1d / d);

    /// <summary>Gets all statistics from the values in the secuence.</summary>
    /// <returns>Simple statistics of all the values in the sequence.</returns>
    public virtual Accumulator Stats()
    {
        Accumulator result = new();
        while (Next(out double value))
            result += value;
        return result;
    }

    /// <summary>Reduces a sequence to a single number.</summary>
    /// <param name="seed">The seed value.</param>
    /// <param name="reducer">A function that combines two elements into one.</param>
    /// <returns>The reduced values.</returns>
    public virtual double Reduce(double seed, Func<double, double, double> reducer)
    {
        while (Next(out double value))
            seed = reducer(seed, value);
        return seed;
    }

    /// <summary>Gets the sum of all the values in the sequence.</summary>
    /// <returns>The sum of all the values in the sequence.</returns>
    public virtual double Sum()
    {
        double total = 0;
        while (Next(out double value))
            total += value;
        return total;
    }

    /// <summary>Gets the product of all the values in the sequence.</summary>
    /// <returns>The product of all the values in the sequence.</returns>
    public virtual double Product()
    {
        double product = 1;
        while (Next(out double value))
            product *= value;
        return product;
    }

    /// <summary>Gets the total number of values in the sequence.</summary>
    /// <returns>The total number of values in the sequence.</returns>
    public virtual int Length()
    {
        int count = 0;
        while (Next(out _))
            count++;
        return count;
    }

    /// <summary>Checks whether the predicate is satisfied by all items.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if all items satisfy the predicate.</returns>
    public bool All(Func<double, bool> predicate)
    {
        while (Next(out double value))
            if (!predicate(value))
                return false;
        return true;
    }

    /// <summary>Checks whether the predicate is satisfied by at least one item.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if there exists a item satisfying the predicate.</returns>
    public bool Any(Func<double, bool> predicate)
    {
        while (Next(out double value))
            if (predicate(value))
                return true;
        return false;
    }

    /// <summary>Gets the minimum value from the sequence.</summary>
    /// <returns>The minimum value.</returns>
    public virtual double Min()
    {
        if (!Next(out double value))
            return double.NaN;
        while (Next(out double v))
            value = Math.Min(value, v);
        return value;
    }

    /// <summary>Gets the maximum value from the sequence.</summary>
    /// <returns>The maximum value.</returns>
    public virtual double Max()
    {
        if (!Next(out double value))
            return double.NaN;
        while (Next(out double v))
            value = Math.Max(value, v);
        return value;
    }

    /// <summary>Sorts the content of this sequence.</summary>
    /// <returns>A sorted sequence.</returns>
    public virtual DoubleSequence Sort()
    {
        double[] data = Materialize();
        Array.Sort(data);
        return Create(data);
    }

    /// <summary>Sorts the content of this sequence in descending order.</summary>
    /// <returns>A sorted sequence in descending order.</returns>
    public virtual DoubleSequence SortDescending()
    {
        double[] data = Materialize();
        Array.Sort(data, (x, y) => y.CompareTo(x));
        return Create(data);
    }

    /// <summary>Gets only the unique values in this sequence.</summary>
    /// <returns>A sequence with unique values.</returns>
    public virtual DoubleSequence Distinct()
    {
        if (HasStorage)
            return Create(new HashSet<double>(Materialize()).ToArray());
        HashSet<double> set = HasLength ? new(Length()) : [];
        while (Next(out double d))
            set.Add(d);
        return Create(set.ToArray());
    }

    /// <summary>Converts this sequence into a vector.</summary>
    /// <returns>A new vector.</returns>
    public Vector ToVector() => Materialize();

    /// <summary>Creates an array with all values from the sequence.</summary>
    /// <returns>The values as an array.</returns>
    protected virtual double[] Materialize()
    {
        if (HasLength)
        {
            double[] data = GC.AllocateUninitializedArray<double>(Length());
            ref double rd = ref MM.GetArrayDataReference(data);
            while (Next(out double value))
            {
                rd = value;
                rd = ref Add(ref rd, 1);
            }
            return data;
        }
        List<double> values = [];
        while (Next(out double value))
            values.Add(value);
        return [.. values];
    }

    /// <summary>Fills a span with all values from the sequence.</summary>
    protected void Materialize(Span<double> span)
    {
        for (ref double d = ref MM.GetReference(span); Next(out double v); d = ref Add(ref d, 1))
            d = v;
    }

    /// <summary>Checks if we can get the length without iterating.</summary>
    protected virtual bool HasLength => false;

    /// <summary>Checks the sequence has a storage.</summary>
    protected virtual bool HasStorage => false;

    /// <summary>Evaluated the sequence and formats it like a <see cref="Vector"/>.</summary>
    /// <returns>A formated list of double values.</returns>
    public override string ToString() =>
        Materialize().ToString(v => v.ToString("G6"));

    /// <summary>Gets a textual representation of this vector.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        Materialize().ToString(v => v.ToString(format, provider));

    /// <summary>Checks if two sequence has the same length and arguments.</summary>
    /// <param name="other">The second sequence to be compared.</param>
    /// <returns><see langword="true"/> if the two sequences have the same items.</returns>
    public bool Equals(DoubleSequence? other) =>
        other is not null && Materialize().EqualsV(other.Materialize());

    /// <summary>Checks if the provided argument is a sequence with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a sequence with the same items.</returns>
    public override bool Equals(object? obj) =>
        obj is DoubleSequence seq && Equals(seq);

    /// <summary>Returns the hashcode for this vector.</summary>
    /// <returns>A hashcode summarizing the content of the vector.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)Materialize()).GetHashCode(EqualityComparer<double>.Default);

    /// <summary>Compares two vectors for equality. </summary>
    /// <param name="left">First sequence operand.</param>
    /// <param name="right">Second sequence operand.</param>
    /// <returns><see langword="true"/> if all corresponding items are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(DoubleSequence? left, DoubleSequence? right) => left?.Equals(right) == true;

    /// <summary>Compares two vectors for inequality. </summary>
    /// <param name="left">First sequence operand.</param>
    /// <param name="right">Second sequence operand.</param>
    /// <returns><see langword="true"/> if any pair of corresponding items are not equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(DoubleSequence? left, DoubleSequence? right) => left?.Equals(right) != true;
}
