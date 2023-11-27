namespace Austra.Library;

/// <summary>Represents any sequence returning a complex values.</summary>
public abstract partial class CSequence : Sequence<Complex, CSequence>,
    IFormattable,
    IEquatable<CSequence>,
    IEqualityOperators<CSequence, CSequence, bool>,
    IAdditionOperators<CSequence, CSequence, CSequence>,
    IAdditionOperators<CSequence, Complex, CSequence>,
    ISubtractionOperators<CSequence, CSequence, CSequence>,
    ISubtractionOperators<CSequence, Complex, CSequence>,
    IMultiplyOperators<CSequence, CSequence, Complex>,
    IMultiplyOperators<CSequence, Complex, CSequence>,
    IDivisionOperators<CSequence, Complex, CSequence>,
    IUnaryNegationOperators<CSequence, CSequence>,
    IPointwiseOperators<CSequence>
{
    /// <summary>Creates a sequence from a complex vector.</summary>
    /// <param name="vector">The vector containing the sequence's values.</param>
    /// <returns>The sequence encapsulating the vector.</returns>
    public static CSequence Create(ComplexVector vector) =>
        new VectorSequence(vector);

    /// <summary>Creates a sequence from random values.</summary>
    /// <param name="size">The size of the series.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static CSequence Random(int size) =>
        new RandomSequence(size, System.Random.Shared);

    /// <summary>Creates a sequence from normal random values.</summary>
    /// <param name="size">The size of the series.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static CSequence NormalRandom(int size) =>
        new NormalRandomSequence(size, Stats.NormalRandom.Shared);

    /// <summary>Creates a sequence from normal random values.</summary>
    /// <param name="size">The size of the series.</param>
    /// <param name="variance">The variance of the normal distribution.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static CSequence NormalRandom(int size, double variance) =>
        new NormalRandomSequence(size, new NormalRandom(0, Sqrt(variance)));

    /// <summary>Transform a sequence acording to the function passed as parameter.</summary>
    /// <param name="mapper">The transforming function.</param>
    /// <returns>The transformed sequence.</returns>
    public override CSequence Map(Func<Complex, Complex> mapper) =>
        new Mapped(this, mapper);

    /// <summary>Transform a sequence acording to the predicate passed as parameter.</summary>
    /// <param name="filter">A predicate for selecting surviving values</param>
    /// <returns>The filtered sequence.</returns>
    public override CSequence Filter(Func<Complex, bool> filter) =>
        new Filtered(this, filter);

    /// <summary>Joins the common part of two sequence with the help of a lambda.</summary>
    /// <param name="other">The second sequence.</param>
    /// <param name="zipper">The joining sequence.</param>
    /// <returns>The combined sequence.</returns>
    public override CSequence Zip(CSequence other, Func<Complex, Complex, Complex> zipper) =>
        new Zipped(this, other, zipper);

    /// <summary>Gets the value at the specified index.</summary>
    /// <param name="idx">A position inside the sequence.</param>
    /// <returns>The value at the given position.</returns>
    public override Complex this[Index idx] => idx.IsFromEnd ? Materialize()[idx] : this[idx.Value];

    /// <summary>Gets a range from the sequence.</summary>
    /// <param name="range">A range inside the sequence.</param>
    /// <returns>The sequence for the given range.</returns>
    public override CSequence this[Range range] => new VectorSequence(Materialize()[range]);

    /// <summary>Adds the common part of two sequences.</summary>
    /// <param name="s1">First sequence operand.</param>
    /// <param name="s2">Second sequence operand.</param>
    /// <returns>The component by component sum of the sequences.</returns>
    public static CSequence operator +(CSequence s1, CSequence s2) => !s1.HasStorage && !s2.HasStorage
        ? s1.Zip(s2, (x, y) => x + y)
        : new VectorSequence(s1.ToVector() + s2.ToVector());

    /// <summary>Adds a scalar value to a sequence.</summary>
    /// <param name="s">Sequence operand.</param>
    /// <param name="d">Scalar operand.</param>
    /// <returns>The component by component sum of the sequence and the scalar.</returns>
    public static CSequence operator +(CSequence s, Complex d) => !s.HasStorage
        ? s.Map(x => x + d)
        : new VectorSequence(s.ToVector() + d);

    /// <summary>Adds a sequence to a scalar value.</summary>
    /// <param name="d">Scalar operand.</param>
    /// <param name="s">Sequence operand.</param>
    /// <returns>The component by component sum of the scalar and the sequence.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CSequence operator +(Complex d, CSequence s) => s + d;

    /// <summary>Subtracts a scalar common part of two sequences.</summary>
    /// <param name="s1">Sequence minuend.</param>
    /// <param name="s2">Sequence subtrahend.</param>
    /// <returns>The component by component subtraction of the sequences.</returns>
    public static CSequence operator -(CSequence s1, CSequence s2) => !s1.HasStorage && !s2.HasStorage
        ? s1.Zip(s2, (x, y) => x - y)
        : new VectorSequence(s1.ToVector() - s2.ToVector());

    /// <summary>Subtracts a scalar from a sequence.</summary>
    /// <param name="s">Sequence minuend.</param>
    /// <param name="d">Scalar subtrahend.</param>
    /// <returns>The component by component subtraction of the sequence and the scalar.</returns>
    public static CSequence operator -(CSequence s, Complex d) => !s.HasStorage
        ? s.Map(x => x - d)
        : new VectorSequence(s.ToVector() - d);

    /// <summary>Subtracts a scalar from a sequence.</summary>
    /// <param name="s">Sequence minuend.</param>
    /// <param name="d">Scalar subtrahend.</param>
    /// <returns>The component by component subtraction of the sequence and the scalar.</returns>
    public static CSequence operator -(Complex d, CSequence s) => !s.HasStorage
        ? s.Map(x => d - x)
        : new VectorSequence(d - s.ToVector());

    /// <summary>Negates a sequence.</summary>
    /// <param name="s">The sequence operand.</param>
    /// <returns>The component by component negation.</returns>
    public static CSequence operator -(CSequence s) => !s.HasStorage
        ? s.Negate()
        : new VectorSequence(-s.ToVector());

    /// <summary>Negates a sequence without an underlying storage.</summary>
    /// <returns>The negated sequence.</returns>
    protected virtual CSequence Negate() => Map(x => -x);

    /// <summary>Calculates the scalar product of the common part of two sequences.</summary>
    /// <param name="s1">First sequence.</param>
    /// <param name="s2">Second sequence.</param>
    /// <returns>The dot product of the common part.</returns>
    public static Complex operator *(CSequence s1, CSequence s2) => !s1.HasStorage && !s2.HasStorage
        ? s1.Zip(s2, (x, y) => x * Complex.Conjugate(y)).Sum()
        : s1.ToVector() * s2.ToVector();

    /// <summary>Multiplies a sequence by a scalar value.</summary>
    /// <param name="s">Sequence multiplicand.</param>
    /// <param name="d">A scalar multiplier.</param>
    /// <returns>The multiplication of the sequence by the scalar.</returns>
    public static CSequence operator *(CSequence s, Complex d) => !s.HasStorage
        ? s.Scale(d)
        : new VectorSequence(s.ToVector() * d);

    /// <summary>Scales a sequence without an underlying storage.</summary>
    /// <param name="d">The scalar multiplier.</param>
    /// <returns>The scaled sequence.</returns>
    protected virtual CSequence Scale(Complex d) => Map(x => x * d);

    /// <summary>Multiplies a scalar value by a sequence.</summary>
    /// <param name="d">Scalar multiplicand.</param>
    /// <param name="s">Sequence multiplier.</param>
    /// <returns>The multiplication of the sequence by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CSequence operator *(Complex d, CSequence s) => s * d;

    /// <summary>Divides a sequence by a scalar value.</summary>
    /// <param name="s">Sequence dividend.</param>
    /// <param name="d">A scalar divisor.</param>
    /// <returns>The quotient of the sequence and the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CSequence operator /(CSequence s, Complex d) => s * Complex.Reciprocal(d);

    /// <summary>Item by item multiplication of two sequences.</summary>
    /// <param name="other">The second sequence.</param>
    /// <returns>A sequence with all the multiplication results.</returns>
    public override CSequence PointwiseMultiply(CSequence other)
    {
        if (!HasStorage && !other.HasStorage)
            return new Zipped(this, other, (x, y) => x * y);
        ComplexVector a1 = new(Materialize());
        ComplexVector a2 = new(other.Materialize());
        int size = Min(a1.Length, a2.Length);
        return new VectorSequence(a1.PointwiseMultiply(a2));
    }

    /// <summary>Item by item division of sequences.</summary>
    /// <param name="other">The second sequence.</param>
    /// <returns>A sequence with all the quotient results.</returns>
    public override CSequence PointwiseDivide(CSequence other)
    {
        {
            if (!HasStorage && !other.HasStorage)
                return new Zipped(this, other, (x, y) => x / y);
            ComplexVector a1 = new(Materialize());
            ComplexVector a2 = new(other.Materialize());
            int size = Min(a1.Length, a2.Length);
            return new VectorSequence(a1.PointwiseDivide(a2));
        }
    }

    /// <summary>Gets the first value in the sequence.</summary>
    /// <returns>The first value, or <see cref="double.NaN"/> when empty.</returns>
    public override Complex First()
    {
        if (!Next(out Complex value))
            return Complex.NaN;
        return value;
    }

    /// <summary>Gets the last value in the sequence.</summary>
    /// <returns>The last value, or <see cref="double.NaN"/> when empty.</returns>
    public override Complex Last()
    {
        Complex saved = Complex.NaN;
        while (Next(out Complex value))
            saved = value;
        return saved;
    }

    /// <summary>Gets only the unique values in this sequence.</summary>
    /// <returns>A sequence with unique values.</returns>
    public override CSequence Distinct()
    {
        if (HasStorage)
            return Create(new ComplexVector(new HashSet<Complex>(Materialize()).ToArray()));
        HashSet<Complex> set = HasLength ? new(Length()) : [];
        while (Next(out Complex d))
            set.Add(d);
        return Create(new ComplexVector(set.ToArray()));
    }

    /// <summary>Converts this sequence into a complex vector.</summary>
    /// <returns>A new complex vector.</returns>
    public ComplexVector ToVector() => new(Materialize());

    /// <summary>Creates an array with all values from the sequence.</summary>
    /// <returns>The values as an array.</returns>
    protected override Complex[] Materialize()
    {
        if (HasLength)
        {
            Complex[] data = GC.AllocateUninitializedArray<Complex>(Length());
            ref Complex rd = ref MM.GetArrayDataReference(data);
            while (Next(out Complex value))
            {
                rd = value;
                rd = ref Add(ref rd, 1);
            }
            return data;
        }
        List<Complex> values = [];
        while (Next(out Complex value))
            values.Add(value);
        return [.. values];
    }

    /// <summary>Evaluated the sequence and formats it like a <see cref="ComplexVector"/>.</summary>
    /// <returns>A formated list of complex values.</returns>
    public override string ToString() =>
        Materialize().ToString(v => v.ToString("G6"));

    /// <summary>Gets a textual representation of this sequence.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        Materialize().ToString(v => v.ToString(format, provider));

    /// <summary>Checks if two sequence has the same length and arguments.</summary>
    /// <param name="other">The second sequence to be compared.</param>
    /// <returns><see langword="true"/> if the two sequences have the same items.</returns>
    public bool Equals(CSequence? other) =>
        other is not null && new ComplexVector(Materialize()) == new ComplexVector(other.Materialize());

    /// <summary>Checks if the provided argument is a sequence with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a sequence with the same items.</returns>
    public override bool Equals(object? obj) =>
        obj is CSequence seq && Equals(seq);

    /// <summary>Returns the hashcode for this vector.</summary>
    /// <returns>A hashcode summarizing the content of the vector.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)Materialize()).GetHashCode(EqualityComparer<Complex>.Default);

    /// <summary>Compares two vectors for equality. </summary>
    /// <param name="left">First sequence operand.</param>
    /// <param name="right">Second sequence operand.</param>
    /// <returns><see langword="true"/> if all corresponding items are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(CSequence? left, CSequence? right) => left?.Equals(right) == true;

    /// <summary>Compares two vectors for inequality. </summary>
    /// <param name="left">First sequence operand.</param>
    /// <param name="right">Second sequence operand.</param>
    /// <returns><see langword="true"/> if any pair of corresponding items are not equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(CSequence? left, CSequence? right) => left?.Equals(right) != true;

    /// <summary>Creates a plot for this sequence.</summary>
    /// <returns>A plot containing a frozen vector as its dataset.</returns>
    public Plot<ComplexVector> Plot() => new(ToVector());
}
