namespace Austra.Library;

/// <summary>Represents any sequence returning integer values.</summary>
public abstract partial class NSequence : Sequence<int, NSequence>,
    IFormattable,
    IEquatable<NSequence>,
    IEqualityOperators<NSequence, NSequence, bool>,
    IAdditionOperators<NSequence, NSequence, NSequence>,
    IAdditionOperators<NSequence, int, NSequence>,
    ISubtractionOperators<NSequence, NSequence, NSequence>,
    ISubtractionOperators<NSequence, int, NSequence>,
    IMultiplyOperators<NSequence, NSequence, int>,
    IMultiplyOperators<NSequence, int, NSequence>,
    IDivisionOperators<NSequence, int, NSequence>,
    IUnaryNegationOperators<NSequence, NSequence>,
    IPointwiseOperators<NSequence>,
    IIndexable
{
    /// <summary>Creates a sequence from a range.</summary>
    /// <param name="first">The first value in the sequence.</param>
    /// <param name="last">The last value in the sequence.</param>
    /// <returns>A sequence returning a range of values.</returns>
    public static NSequence Create(int first, int last) => first <= last
        ? new GridSequence(first, 1, last)
        : new GridSequenceDesc(first, 1, last);

    /// <summary>Creates a sequence from a range and a step.</summary>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="step">Distance between sequence values.</param>
    /// <param name="last">Upper bound of the sequence. It may be rounded down.</param>
    /// <returns>A sequence returning a range of values.</returns>
    public static NSequence Create(int first, int step, int last) => first <= last
        ? new GridSequence(first, step, last)
        : new GridSequenceDesc(first, step, last);

    /// <summary>Creates a sequence from a vector.</summary>
    /// <param name="values">The vector containing the sequence's values.</param>
    /// <returns>The sequence encapsulating the vector.</returns>
    public static NSequence Create(NVector values) =>
        new VectorSequence(values);

    /// <summary>Creates a sequence from random values.</summary>
    /// <param name="size">The size of the series.</param>
    /// <returns>A random sequence of non-negative integers.</returns>
    public static NSequence Random(int size) =>
        new RandomSequence(size, 0, int.MaxValue, System.Random.Shared);

    /// <summary>Creates a sequence from random values.</summary>
    /// <remarks>The <paramref name="upperBound"/> is exclusive.</remarks>
    /// <param name="size">The size of the series.</param>
    /// <param name="upperBound">The upper bound for the random values.</param>
    /// <returns>A random sequence of non-negative integers.</returns>
    public static NSequence Random(int size, int upperBound) =>
        new RandomSequence(size, 0, upperBound, System.Random.Shared);

    /// <summary>Creates a sequence from random values inside an interval.</summary>
    /// <remarks>The <paramref name="upperBound"/> is exclusive.</remarks>
    /// <param name="size">The size of the sequence.</param>
    /// <param name="lowerBound">The lower bound for the random values.</param>
    /// <param name="upperBound">The upper bound for the random values.</param>
    /// <returns>A random sequence of integers in the given interval.</returns>
    public static NSequence Random(int size, int lowerBound, int upperBound) =>
        new RandomSequence(size, lowerBound, upperBound, System.Random.Shared);

    /// <summary>Creates a sequence by unfolding an initial state by a function.</summary>
    /// <param name="size">The size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generating function.</param>
    /// <returns>The sequence unfolded from the initial state and the function.</returns>
    public static NSequence Unfold(int size, int seed, Func<int, int> unfold) =>
        new Unfolder0(size, seed, unfold);

    /// <summary>Creates a sequence by unfolding an initial state by a function.</summary>
    /// <param name="size">The size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generating function.</param>
    /// <returns>The sequence unfolded from the initial state and the function.</returns>
    public static NSequence Unfold(int size, int seed, Func<int, int, int> unfold) =>
        new Unfolder1(size, seed, unfold);

    /// <summary>Creates a sequence by unfolding an initial state by a function.</summary>
    /// <param name="size">The size of the sequence.</param>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="second">Second value in the sequence.</param>
    /// <param name="unfold">The generating function.</param>
    /// <returns>The sequence unfolded from the initial state and the function.</returns>
    public static NSequence Unfold(int size, int first, int second, Func<int, int, int> unfold) =>
        new Unfolder2(size, first, second, unfold);

    /// <summary>Creates an integer sequence for finding values in a vector.</summary>
    /// <param name="vector">The source vector.</param>
    /// <param name="value">The value to find.</param>
    /// <returns>All indexes where the value exists, or an empty sequence.</returns>
    internal static NSequence Iterate(DVector vector, double value) =>
        new IndexFinder(vector, value);

    /// <summary>Creates an integer sequence for finding values in a vector.</summary>
    /// <param name="vector">The source vector.</param>
    /// <param name="condition">A predicate on the value of a vector's item.</param>
    /// <returns>All indexes where the value exists, or an empty sequence.</returns>
    internal static NSequence Iterate(DVector vector, Func<double, bool> condition) =>
        new IndexFinderWithLambda(vector, condition);

    /// <summary>Creates an integer sequence for finding values in a vector.</summary>
    /// <param name="vector">The source vector.</param>
    /// <param name="value">The value to find.</param>
    /// <returns>All indexes where the value exists, or an empty sequence.</returns>
    internal static NSequence Iterate(CVector vector, Complex value) =>
        new CIndexFinder(vector, value);

    /// <summary>Creates an integer sequence for finding values in a vector.</summary>
    /// <param name="vector">The source vector.</param>
    /// <param name="condition">A predicate on the value of a vector's item.</param>
    /// <returns>All indexes where the value exists, or an empty sequence.</returns>
    internal static NSequence Iterate(CVector vector, Func<Complex, bool> condition) =>
        new CIndexFinderWithLambda(vector, condition);

    /// <summary>Transform a sequence acording to the function passed as parameter.</summary>
    /// <param name="mapper">The transforming function.</param>
    /// <returns>The transformed sequence.</returns>
    public override NSequence Map(Func<int, int> mapper) => new Mapped(this, mapper);

    /// <summary>Creates a real sequence acording to the function passed as parameter.</summary>
    /// <param name="mapper">The transforming function.</param>
    /// <returns>The transformed sequence.</returns>
    public DSequence MapReal(Func<int, double> mapper) => new RealMapped(this, mapper);

    /// <summary>Transform a sequence acording to the predicate passed as parameter.</summary>
    /// <param name="filter">A predicate for selecting surviving values</param>
    /// <returns>The filtered sequence.</returns>
    public override NSequence Filter(Func<int, bool> filter) => new Filtered(this, filter);

    /// <summary>Joins the common part of two sequence with the help of a lambda.</summary>
    /// <param name="other">The second sequence.</param>
    /// <param name="zipper">The joining sequence.</param>
    /// <returns>The combined sequence.</returns>
    public override NSequence Zip(NSequence other, Func<int, int, int> zipper) =>
        new Zipped(this, other, zipper);

    /// <summary>Get the initial values of a sequence that satisfy a predicate.</summary>
    /// <param name="predicate">The predicate to be satisfied.</param>
    /// <returns>A prefix of the original sequence.</returns>
    public override NSequence While(Func<int, bool> predicate) =>
        new SeqWhile(this, predicate);

    /// <summary>Get the initial values of a sequence until a predicate is satisfied.</summary>
    /// <param name="predicate">The predicate to be satisfied.</param>
    /// <returns>A prefix of the original sequence.</returns>
    public override NSequence Until(Func<int, bool> predicate) =>
        new SeqUntil(this, predicate);

    /// <summary>Get the initial values of a sequence until a value is found.</summary>
    /// <param name="value">The value that will be the end of the new sequence.</param>
    /// <returns>A prefix of the original sequence.</returns>
    public override NSequence Until(int value) =>
        new SeqUntilValue(this, value);

    /// <summary>Gets the value at the specified index.</summary>
    /// <param name="idx">A position inside the sequence.</param>
    /// <returns>The value at the given position.</returns>
    public override int this[Index idx] => idx.IsFromEnd ? Materialize()[idx] : this[idx.Value];

    /// <summary>Gets a range from the sequence.</summary>
    /// <param name="range">A range inside the sequence.</param>
    /// <returns>The sequence for the given range.</returns>
    public override NSequence this[Range range] => new VectorSequence(Materialize()[range]);

    /// <summary>Adds the common part of two sequences.</summary>
    /// <param name="s1">First sequence operand.</param>
    /// <param name="s2">Second sequence operand.</param>
    /// <returns>The component by component sum of the sequences.</returns>
    public static NSequence operator +(NSequence s1, NSequence s2)
    {
        if (!s1.HasStorage || !s2.HasStorage)
            return s1.Add(s2);
        int[] a1 = s1.Materialize();
        int[] a2 = s2.Materialize();
        int[] r = GC.AllocateUninitializedArray<int>(Math.Min(a1.Length, a2.Length));
        a1.AsSpan(0, r.Length).Add(a2.AsSpan(0, r.Length), r);
        return new VectorSequence(r);
    }

    /// <summary>Adds a sequence to this sequence.</summary>
    /// <param name="other">Sequence to add.</param>
    /// <returns>The component by component sum of the sequences.</returns>
    protected virtual NSequence Add(NSequence other) =>
        ReferenceEquals(this, other) ? Map(x => x + x) : Zip(other, (x, y) => x + y);

    /// <summary>Adds a scalar value to a sequence.</summary>
    /// <param name="s">Sequence operand.</param>
    /// <param name="d">Scalar operand.</param>
    /// <returns>The component by component sum of the sequence and the scalar.</returns>
    public static NSequence operator +(NSequence s, int d) =>
        s.HasStorage ? new VectorSequence(s.ToVector() + d) : s.Shift(d);

    /// <summary>Adds a sequence to a scalar value.</summary>
    /// <param name="d">Scalar operand.</param>
    /// <param name="s">Sequence operand.</param>
    /// <returns>The component by component sum of the scalar and the sequence.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NSequence operator +(int d, NSequence s) => s + d;

    /// <summary>Shifts a sequence without an underlying storage.</summary>
    /// <param name="d">Amount to shift.</param>
    /// <returns>The shifted sequence.</returns>
    protected virtual NSequence Shift(int d) => Map(x => x + d);

    /// <summary>Subtracts the common part of two sequences.</summary>
    /// <param name="s1">Sequence minuend.</param>
    /// <param name="s2">Sequence subtrahend.</param>
    /// <returns>The component by component subtraction of the sequences.</returns>
    public static NSequence operator -(NSequence s1, NSequence s2)
    {
        if (!s1.HasStorage || !s2.HasStorage)
            return ReferenceEquals(s1, s2) ? s1.Map(x => 0) : s1.Zip(s2, (x, y) => x - y);
        int[] a1 = s1.Materialize();
        int[] a2 = s2.Materialize();
        int[] r = GC.AllocateUninitializedArray<int>(Math.Min(a1.Length, a2.Length));
        a1.AsSpan(0, r.Length).Sub(a2.AsSpan(0, r.Length), r);
        return new VectorSequence(r);
    }

    /// <summary>Subtracts a scalar from a sequence.</summary>
    /// <param name="s">Sequence minuend.</param>
    /// <param name="d">Scalar subtrahend.</param>
    /// <returns>The component by component subtraction of the sequence and the scalar.</returns>
    public static NSequence operator -(NSequence s, int d) =>
        s.HasStorage ? new VectorSequence(s.ToVector() - d) : s.Shift(-d);

    /// <summary>Subtracts a sequence from a scalar.</summary>
    /// <param name="s">Sequence minuend.</param>
    /// <param name="d">Scalar subtrahend.</param>
    /// <returns>The component by component subtraction of the sequence and the scalar.</returns>
    public static NSequence operator -(int d, NSequence s) =>
        s.HasStorage ? new VectorSequence(d - s.ToVector()) : s.Map(x => d - x);

    /// <summary>Negates a sequence.</summary>
    /// <param name="s">The sequence operand.</param>
    /// <returns>The component by component negation.</returns>
    public static NSequence operator -(NSequence s) =>
        s.HasStorage ? new VectorSequence(-s.ToVector()) : s.Negate();

    /// <summary>Negates a sequence without an underlying storage.</summary>
    /// <returns>The negated sequence.</returns>
    protected virtual NSequence Negate() => Map(x => -x);

    /// <summary>Calculates the scalar product of the common part of two sequences.</summary>
    /// <param name="s1">First sequence.</param>
    /// <param name="s2">Second sequence.</param>
    /// <returns>The dot product of the common part.</returns>
    public static int operator *(NSequence s1, NSequence s2)
    {
        if (!s1.HasStorage || !s2.HasStorage)
            return ReferenceEquals(s2, s2)
                ? s1.Map(x => x * x).Sum() 
                :  s1.Zip(s2, (x, y) => x * y).Sum();
        int[] a1 = s1.Materialize();
        int[] a2 = s2.Materialize();
        int size = Math.Min(a1.Length, a2.Length);
        return a1.AsSpan(0, size).Dot(a2.AsSpan(0, size));
    }

    /// <summary>Multiplies a sequence by a scalar value.</summary>
    /// <param name="s">Sequence multiplicand.</param>
    /// <param name="d">A scalar multiplier.</param>
    /// <returns>The multiplication of the sequence by the scalar.</returns>
    public static NSequence operator *(NSequence s, int d) =>
        s.HasStorage ? new VectorSequence(s.ToVector() * d) : s.Scale(d);

    /// <summary>Divides a sequence by a scalar value.</summary>
    /// <param name="s">Sequence dividend.</param>
    /// <param name="d">A scalar divisor.</param>
    /// <returns>The quotient of the sequence over the scalar.</returns>
    public static NSequence operator /(NSequence s, int d) =>
        new VectorSequence(s.Materialize().AsSpan().Div(d));

    /// <summary>Scales a sequence without an underlying storage.</summary>
    /// <param name="d">The scalar multiplier.</param>
    /// <returns>The scaled sequence.</returns>
    protected virtual NSequence Scale(int d) => Map(x => x * d);

    /// <summary>Multiplies a scalar value by a sequence.</summary>
    /// <param name="d">Scalar multiplicand.</param>
    /// <param name="s">Sequence multiplier.</param>
    /// <returns>The multiplication of the sequence by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NSequence operator *(int d, NSequence s) => s * d;

    /// <summary>Item by item multiplication of two sequences.</summary>
    /// <param name="other">The second sequence.</param>
    /// <returns>A sequence with all the multiplication results.</returns>
    public override NSequence PointwiseMultiply(NSequence other)
    {
        if (!HasStorage || !other.HasStorage)
            return ReferenceEquals(this, other)
                ? new Mapped(this, x => x * x)
                : new Zipped(this, other, (x, y) => x * y);
        int[] a1 = Materialize();
        int[] a2 = other.Materialize();
        int size = Math.Min(a1.Length, a2.Length);
        return new VectorSequence(a1.AsSpan(size).Mul(a2.AsSpan(size)));
    }

    /// <summary>Item by item division of sequences.</summary>
    /// <param name="other">The second sequence.</param>
    /// <returns>A sequence with all the quotient results.</returns>
    public override NSequence PointwiseDivide(NSequence other)
    {
        if (!HasStorage || !other.HasStorage)
            return ReferenceEquals(this, other)
                ? new Mapped(this, x => 1)
                : new Zipped(this, other, (x, y) => x / y);
        int[] a1 = Materialize();
        int[] a2 = other.Materialize();
        int size = Math.Min(a1.Length, a2.Length);
        return new VectorSequence(a1.AsSpan(size).Div(a2.AsSpan(size)));
    }

    /// <summary>Gets all statistics from the values in the secuence.</summary>
    /// <returns>Simple statistics of all the values in the sequence.</returns>
    public virtual Accumulator Stats()
    {
        Accumulator result = new();
        while (Next(out int value))
            result += value;
        Reset();
        return result;
    }

    /// <summary>Gets only the unique values in this sequence.</summary>
    /// <returns>A sequence with unique values.</returns>
    public override NSequence Distinct()
    {
        if (HasStorage)
            return Create(new HashSet<int>(Materialize()).ToArray());
        HashSet<int> set = HasLength ? new(Length()) : [];
        while (Next(out int d))
            set.Add(d);
        Reset();
        return Create(new NVector([.. set]));
    }

    /// <summary>Gets the minimum value from the sequence.</summary>
    /// <returns>The minimum value.</returns>
    public virtual int Min()
    {
        if (!Next(out int value))
            throw new EmptySequenceException();
        while (Next(out int v))
            value = Math.Min(value, v);
        Reset();
        return value;
    }

    /// <summary>Gets the maximum value from the sequence.</summary>
    /// <returns>The maximum value.</returns>
    public virtual int Max()
    {
        if (!Next(out int value))
            throw new EmptySequenceException();
        while (Next(out int v))
            value = Math.Max(value, v);
        Reset();
        return value;
    }

    /// <summary>Sorts the content of this sequence.</summary>
    /// <returns>A sorted sequence.</returns>
    public virtual NSequence Sort()
    {
        int[] data = Materialize();
        Array.Sort(data);
        return Create(data);
    }

    /// <summary>Sorts the content of this sequence in descending order.</summary>
    /// <returns>A sorted sequence in descending order.</returns>
    public virtual NSequence SortDescending()
    {
        int[] data = Materialize();
        Array.Sort(data, (x, y) => y.CompareTo(x));
        return Create(data);
    }

    /// <summary>Evaluated the sequence and formats it like a <see cref="NVector"/>.</summary>
    /// <returns>A formated list of double values.</returns>
    public override string ToString() => ToString("N0");

    /// <summary>Gets a textual representation of this sequence.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider = null)
    {
        int[] values = Materialize();
        return values.Length == 0 ? "∅" : values.ToString(v => v.ToString(format, provider));
    }

    /// <summary>Checks if two sequence has the same length and arguments.</summary>
    /// <param name="other">The second sequence to be compared.</param>
    /// <returns><see langword="true"/> if the two sequences have the same items.</returns>
    public bool Equals(NSequence? other) =>
        other is not null && Materialize().Eqs(other.Materialize());

    /// <summary>Checks if the provided argument is a sequence with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a sequence with the same items.</returns>
    public override bool Equals(object? obj) =>
        obj is NSequence seq && Equals(seq);

    /// <summary>Returns the hashcode for this vector.</summary>
    /// <returns>A hashcode summarizing the content of the vector.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)Materialize()).GetHashCode(EqualityComparer<int>.Default);

    /// <summary>Compares two vectors for equality. </summary>
    /// <param name="left">First sequence operand.</param>
    /// <param name="right">Second sequence operand.</param>
    /// <returns><see langword="true"/> if all corresponding items are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(NSequence? left, NSequence? right) => left?.Equals(right) == true;

    /// <summary>Compares two vectors for inequality. </summary>
    /// <param name="left">First sequence operand.</param>
    /// <param name="right">Second sequence operand.</param>
    /// <returns><see langword="true"/> if any pair of corresponding items are not equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(NSequence? left, NSequence? right) => left?.Equals(right) != true;

    /// <summary>Converts this sequence into an integer vector.</summary>
    /// <returns>A new vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NVector ToVector() => Materialize();

    /// <summary>Creates a plot for this sequence.</summary>
    /// <returns>A plot containing a frozen vector as its dataset.</returns>
    public Plot<NVector> Plot() => new(ToVector());
}
