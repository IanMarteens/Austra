namespace Austra.Library;

/// <summary>Represents any sequence returning double-precision values.</summary>
public abstract partial class DSequence : Sequence<double, DSequence>,
    IFormattable,
    IEquatable<DSequence>,
    IEqualityOperators<DSequence, DSequence, bool>,
    IAdditionOperators<DSequence, DSequence, DSequence>,
    IAdditionOperators<DSequence, double, DSequence>,
    ISubtractionOperators<DSequence, DSequence, DSequence>,
    ISubtractionOperators<DSequence, double, DSequence>,
    IMultiplyOperators<DSequence, DSequence, double>,
    IMultiplyOperators<DSequence, double, DSequence>,
    IDivisionOperators<DSequence, double, DSequence>,
    IUnaryNegationOperators<DSequence, DSequence>,
    IPointwiseOperators<DSequence>,
    IIndexable
{
    /// <summary>Creates a sequence from a range.</summary>
    /// <param name="first">The first value in the sequence.</param>
    /// <param name="last">The last value in the sequence.</param>
    /// <returns>A sequence returning a range of values.</returns>
    public static DSequence Create(int first, int last) =>
        first <= last ? new RangeSequence(first, last) : new RangeSequenceDesc(first, last);

    /// <summary>Creates a sequence from a range.</summary>
    /// <param name="first">The first value in the sequence.</param>
    /// <param name="last">The last value in the sequence.</param>
    /// <returns>A sequence returning a range of values.</returns>
    public static DSequence Create(double first, double last) =>
        first <= last ? new RangeSequence((int)first, (int)last) : new RangeSequenceDesc((int)first, (int)last);

    /// <summary>Creates a sequence from a uniform grid.</summary>
    /// <param name="lower">The first value in the sequence.</param>
    /// <param name="steps">The number of steps in the sequence, minus one.</param>
    /// <param name="upper">The last value in the sequence.</param>
    /// <returns>A sequence returning a uniform grid of values.</returns>
    public static DSequence Create(double lower, int steps, double upper) =>
        new GridSequence(lower, upper, steps);
    
    /// <summary>Creates a sequence from a vector.</summary>
    /// <param name="vector">The vector containing the sequence's values.</param>
    /// <returns>The sequence encapsulating the vector.</returns>
    public static DSequence Create(DVector vector) =>
        new VectorSequence(vector);

    /// <summary>Creates a sequence from a time series.</summary>
    /// <param name="series">The series containing the sequence's values.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DSequence Create(Series series) =>
        new VectorSequence(series);

    /// <summary>Creates a sequence from a matrix.</summary>
    /// <param name="matrix">A matrix containing the sequence's values.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DSequence Create(Matrix matrix) =>
        new VectorSequence((double[])matrix);

    /// <summary>Creates a sequence from random values.</summary>
    /// <param name="size">The size of the series.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DSequence Random(int size) =>
        new RandomSequence(size, System.Random.Shared);

    /// <summary>Creates a sequence from normal random values.</summary>
    /// <param name="size">The size of the series.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DSequence NormalRandom(int size) =>
        new NormalRandomSequence(size, Library.Stats.NormalRandom.Shared);

    /// <summary>Creates a sequence from normal random values.</summary>
    /// <param name="size">The size of the series.</param>
    /// <param name="variance">The variance of the normal distribution.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DSequence NormalRandom(int size, double variance) =>
        new NormalRandomSequence(size, new NormalRandom(0, Sqrt(variance)));

    /// <summary>Creates an autoregressive (AR) sequence.</summary>
    /// <param name="size">The size of the series.</param>
    /// <param name="variance">The variance of the normal distribution.</param>
    /// <param name="coefficients">Autoregressive coefficients.</param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DSequence AR(int size, double variance, DVector coefficients) =>
        coefficients.Length == 0
        ? throw new VectorLengthException()
        : new ArSequence(size, variance, coefficients);

    /// <summary>Creates a moving average (MA) sequence.</summary>
    /// <param name="size">The size of the series.</param>
    /// <param name="variance">The variance of the normal distribution.</param>
    /// <param name="coefficients">
    /// Moving average coefficients. The first term is the independent term.
    /// </param>
    /// <returns>The sequence encapsulating the time series.</returns>
    public static DSequence MA(int size, double variance, DVector coefficients) =>
        coefficients.Length == 0
        ? throw new VectorLengthException()
        : new MaSequence(size, variance, coefficients.UnsafeThis(0), coefficients[1..]);

    /// <summary>Creates a sequence by unfolding an initial state by a function.</summary>
    /// <param name="size">The size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generating function.</param>
    /// <returns>The sequence unfolded from the initial state and the function.</returns>
    public static DSequence Unfold(int size, double seed, Func<double, double> unfold) =>
        new Unfolder0(size, seed, unfold);

    /// <summary>Creates a sequence by unfolding an initial state by a function.</summary>
    /// <param name="size">The size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generating function.</param>
    /// <returns>The sequence unfolded from the initial state and the function.</returns>
    public static DSequence Unfold(int size, double seed, Func<int, double, double> unfold) =>
        new Unfolder1(size, seed, unfold);

    /// <summary>Creates a sequence by unfolding an initial state by a function.</summary>
    /// <param name="size">The size of the sequence.</param>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="second">Second value in the sequence.</param>
    /// <param name="unfold">The generating function.</param>
    /// <returns>The sequence unfolded from the initial state and the function.</returns>
    public static DSequence Unfold(int size, double first, double second,
        Func<double, double, double> unfold) =>
        new Unfolder2(size, first, second, unfold);

    /// <summary>Transform a sequence acording to the function passed as parameter.</summary>
    /// <param name="mapper">The transforming function.</param>
    /// <returns>The transformed sequence.</returns>
    public override DSequence Map(Func<double, double> mapper) =>
        new Mapped(this, mapper);

    /// <summary>Transform a sequence acording to the predicate passed as parameter.</summary>
    /// <param name="filter">A predicate for selecting surviving values</param>
    /// <returns>The filtered sequence.</returns>
    public override DSequence Filter(Func<double, bool> filter) =>
        new Filtered(this, filter);

    /// <summary>Joins the common part of two sequence with the help of a lambda.</summary>
    /// <param name="other">The second sequence.</param>
    /// <param name="zipper">The joining sequence.</param>
    /// <returns>The combined sequence.</returns>
    public override DSequence Zip(DSequence other, Func<double, double, double> zipper) =>
        new Zipped(this, other, zipper);

    /// <summary>Get the initial values of a sequence that satisfy a predicate.</summary>
    /// <param name="predicate">The predicate to be satisfied.</param>
    /// <returns>A prefix of the original sequence.</returns>
    public override DSequence While(Func<double, bool> predicate) =>
        new SeqWhile(this, predicate);

    /// <summary>Get the initial values of a sequence until a predicate is satisfied.</summary>
    /// <param name="predicate">The predicate to be satisfied.</param>
    /// <returns>A prefix of the original sequence.</returns>
    public override DSequence Until(Func<double, bool> predicate) =>
        new SeqUntil(this, predicate);

    /// <summary>Get the initial values of a sequence until a value is found.</summary>
    /// <param name="value">The value that will be the end of the new sequence.</param>
    /// <returns>A prefix of the original sequence.</returns>
    public override DSequence Until(double value) =>
        new SeqUntilValue(this, value);

    /// <summary>Gets the value at the specified index.</summary>
    /// <param name="idx">A position inside the sequence.</param>
    /// <returns>The value at the given position.</returns>
    public override double this[Index idx] => idx.IsFromEnd ? Materialize()[idx] : this[idx.Value];

    /// <summary>Gets a range from the sequence.</summary>
    /// <param name="range">A range inside the sequence.</param>
    /// <returns>The sequence for the given range.</returns>
    public override DSequence this[Range range] => new VectorSequence(Materialize()[range]);

    /// <summary>Implicit conversion from vector to sequence.</summary>
    /// <param name="vector">A vector.</param>
    /// <returns>A vector with the same components as the array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DSequence(DVector vector) => new VectorSequence(vector);

    /// <summary>Adds the common part of two sequences.</summary>
    /// <param name="s1">First sequence operand.</param>
    /// <param name="s2">Second sequence operand.</param>
    /// <returns>The component by component sum of the sequences.</returns>
    public static DSequence operator +(DSequence s1, DSequence s2)
    {
        if (!s1.HasStorage || !s2.HasStorage)
            return s1.Zip(s2, (x, y) => x + y);
        double[] a1 = s1.Materialize();
        double[] a2 = s2.Materialize();
        double[] r = GC.AllocateUninitializedArray<double>(Math.Min(a1.Length, a2.Length));
        a1.AsSpan(0, r.Length).Add(a2.AsSpan(0, r.Length), r);
        return new VectorSequence(r);
    }

    /// <summary>Adds a scalar value to a sequence.</summary>
    /// <param name="s">Sequence operand.</param>
    /// <param name="d">Scalar operand.</param>
    /// <returns>The component by component sum of the sequence and the scalar.</returns>
    public static DSequence operator +(DSequence s, double d) =>
        s.HasStorage ? new VectorSequence(s.ToVector() + d) : s.Shift(d);

    /// <summary>Adds a sequence to a scalar value.</summary>
    /// <param name="d">Scalar operand.</param>
    /// <param name="s">Sequence operand.</param>
    /// <returns>The component by component sum of the scalar and the sequence.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DSequence operator +(double d, DSequence s) => s + d;

    /// <summary>Shifts a sequence without an underlying storage.</summary>
    /// <param name="d">Amount to shift.</param>
    /// <returns>The shifted sequence.</returns>
    protected virtual DSequence Shift(double d) => Map(x => x + d);

    /// <summary>Subtracts the common part of two sequences.</summary>
    /// <param name="s1">Sequence minuend.</param>
    /// <param name="s2">Sequence subtrahend.</param>
    /// <returns>The component by component subtraction of the sequences.</returns>
    public static DSequence operator -(DSequence s1, DSequence s2)
    {
        if (!s1.HasStorage || !s2.HasStorage)
            return s1.Zip(s2, (x, y) => x - y);
        double[] a1 = s1.Materialize();
        double[] a2 = s2.Materialize();
        double[] r = GC.AllocateUninitializedArray<double>(Math.Min(a1.Length, a2.Length));
        a1.AsSpan(0, r.Length).Sub(a2.AsSpan(0, r.Length), r);
        return new VectorSequence(r);
    }

    /// <summary>Subtracts a scalar from a sequence.</summary>
    /// <param name="s">Sequence minuend.</param>
    /// <param name="d">Scalar subtrahend.</param>
    /// <returns>The component by component subtraction of the sequence and the scalar.</returns>
    public static DSequence operator -(DSequence s, double d) =>
        s.HasStorage ? new VectorSequence(s.ToVector() - d) : s.Shift(-d);

    /// <summary>Subtracts a sequence from a scalar.</summary>
    /// <param name="s">Sequence minuend.</param>
    /// <param name="d">Scalar subtrahend.</param>
    /// <returns>The component by component subtraction of the sequence and the scalar.</returns>
    public static DSequence operator -(double d, DSequence s) =>
        s.HasStorage ? new VectorSequence(d - s.ToVector()) : s.Map(x => d - x);

    /// <summary>Negates a sequence.</summary>
    /// <param name="s">The sequence operand.</param>
    /// <returns>The component by component negation.</returns>
    public static DSequence operator -(DSequence s) =>
        s.HasStorage ? new VectorSequence(-s.ToVector()) : s.Negate();

    /// <summary>Negates a sequence without an underlying storage.</summary>
    /// <returns>The negated sequence.</returns>
    protected virtual DSequence Negate() => Map(x => -x);

    /// <summary>Calculates the scalar product of the common part of two sequences.</summary>
    /// <param name="s1">First sequence.</param>
    /// <param name="s2">Second sequence.</param>
    /// <returns>The dot product of the common part.</returns>
    public static double operator *(DSequence s1, DSequence s2)
    {
        if (!s1.HasStorage || !s2.HasStorage)
            return s1.Zip(s2, (x, y) => x * y).Sum();
        int size = Math.Min(s1.Length(), s2.Length());
        return s1.Materialize().AsSpan(0, size).Dot(s2.Materialize().AsSpan(0, size));
    }

    /// <summary>Multiplies a sequence by a scalar value.</summary>
    /// <param name="s">Sequence multiplicand.</param>
    /// <param name="d">A scalar multiplier.</param>
    /// <returns>The multiplication of the sequence by the scalar.</returns>
    public static DSequence operator *(DSequence s, double d) =>
        s.HasStorage ? new VectorSequence(s.ToVector() * d) : s.Scale(d);

    /// <summary>Scales a sequence without an underlying storage.</summary>
    /// <param name="d">The scalar multiplier.</param>
    /// <returns>The scaled sequence.</returns>
    protected virtual DSequence Scale(double d) => Map(x => x * d);

    /// <summary>Multiplies a scalar value by a sequence.</summary>
    /// <param name="d">Scalar multiplicand.</param>
    /// <param name="s">Sequence multiplier.</param>
    /// <returns>The multiplication of the sequence by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DSequence operator *(double d, DSequence s) => s * d;

    /// <summary>Divides a sequence by a scalar value.</summary>
    /// <param name="s">Sequence dividend.</param>
    /// <param name="d">A scalar divisor.</param>
    /// <returns>The quotient of the sequence and the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DSequence operator /(DSequence s, double d) => s * (1d / d);

    /// <summary>Item by item multiplication of two sequences.</summary>
    /// <param name="other">The second sequence.</param>
    /// <returns>A sequence with all the multiplication results.</returns>
    public override DSequence PointwiseMultiply(DSequence other)
    {
        if (!HasStorage || !other.HasStorage)
            return ReferenceEquals(this, other)
                ? new Mapped(this, x => x * x)
                : new Zipped(this, other, (x, y) => x * y);
        double[] a1 = Materialize();
        double[] a2 = other.Materialize();
        int size = Math.Min(a1.Length, a2.Length);
        return new VectorSequence(a1.AsSpan(size).Mul(a2.AsSpan(size)));
    }

    /// <summary>Item by item division of sequences.</summary>
    /// <param name="other">The second sequence.</param>
    /// <returns>A sequence with all the quotient results.</returns>
    public override DSequence PointwiseDivide(DSequence other)
    {
        if (!HasStorage || !other.HasStorage)
            return ReferenceEquals(this, other)
                ? new Mapped(this, x => 1)
                : new Zipped(this, other, (x, y) => x / y);
        double[] a1 = Materialize();
        double[] a2 = other.Materialize();
        int size = Math.Min(a1.Length, a2.Length);
        return new VectorSequence(a1.AsSpan(size).Div(a2.AsSpan(size)));
    }

    /// <summary>Gets all statistics from the values in the secuence.</summary>
    /// <returns>Simple statistics of all the values in the sequence.</returns>
    public virtual Accumulator Stats()
    {
        Accumulator result = new();
        while (Next(out double value))
            result += value;
        Reset();
        return result;
    }

    /// <summary>Gets the minimum value from the sequence.</summary>
    /// <returns>The minimum value.</returns>
    public virtual double Min()
    {
        if (!Next(out double value))
            throw new EmptySequenceException();
        while (Next(out double v))
            value = Math.Min(value, v);
        Reset();
        return value;
    }

    /// <summary>Gets the maximum value from the sequence.</summary>
    /// <returns>The maximum value.</returns>
    public virtual double Max()
    {
        if (!Next(out double value))
            throw new EmptySequenceException();
        while (Next(out double v))
            value = Math.Max(value, v);
        Reset();
        return value;
    }

    /// <summary>Sorts the content of this sequence.</summary>
    /// <returns>A sorted sequence.</returns>
    public virtual DSequence Sort()
    {
        double[] data = Materialize();
        Array.Sort(data);
        return Create(data);
    }

    /// <summary>Sorts the content of this sequence in descending order.</summary>
    /// <returns>A sorted sequence in descending order.</returns>
    public virtual DSequence SortDescending()
    {
        double[] data = Materialize();
        Array.Sort(data, static (x, y) => y.CompareTo(x));
        return Create(data);
    }

    /// <summary>Gets only the unique values in this sequence.</summary>
    /// <returns>A sequence with unique values.</returns>
    public override DSequence Distinct()
    {
        if (HasStorage)
            return Create(new HashSet<double>(Materialize()).ToArray());
        HashSet<double> set = HasLength ? new(Length()) : [];
        while (Next(out double d))
            set.Add(d);
        Reset();
        return Create(set.ToArray());
    }

    /// <summary>Converts this sequence into a vector.</summary>
    /// <returns>A new vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector ToVector() => Materialize();

    /// <summary>Evaluated the sequence and formats it like a <see cref="DVector"/>.</summary>
    /// <returns>A formated list of double values.</returns>
    public override string ToString() => ToString("G6");

    /// <summary>Gets a textual representation of this sequence.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider = null)
    {
        double[] values = Materialize();
        return values.Length == 0 ? "∅" : values.ToString(v => v.ToString(format, provider));
    }

    /// <summary>Checks if two sequence has the same length and arguments.</summary>
    /// <param name="other">The second sequence to be compared.</param>
    /// <returns><see langword="true"/> if the two sequences have the same items.</returns>
    public bool Equals(DSequence? other) =>
        other is not null && Materialize().Eqs(other.Materialize());

    /// <summary>Checks if the provided argument is a sequence with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a sequence with the same items.</returns>
    public override bool Equals(object? obj) =>
        obj is DSequence seq && Equals(seq);

    /// <summary>Returns the hashcode for this vector.</summary>
    /// <returns>A hashcode summarizing the content of the vector.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)Materialize()).GetHashCode(EqualityComparer<double>.Default);

    /// <summary>Compares two vectors for equality. </summary>
    /// <param name="left">First sequence operand.</param>
    /// <param name="right">Second sequence operand.</param>
    /// <returns><see langword="true"/> if all corresponding items are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(DSequence? left, DSequence? right) => left?.Equals(right) == true;

    /// <summary>Compares two vectors for inequality. </summary>
    /// <param name="left">First sequence operand.</param>
    /// <param name="right">Second sequence operand.</param>
    /// <returns><see langword="true"/> if any pair of corresponding items are not equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(DSequence? left, DSequence? right) => left?.Equals(right) != true;

    /// <summary>Creates a plot for this sequence.</summary>
    /// <returns>A plot containing a frozen vector as its dataset.</returns>
    public Plot<DVector> Plot() => new(ToVector());

    /// <summary>Computes autocorrelation for all lags.</summary>
    /// <returns>Pairs lags/autocorrelation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Series<int> ACF() => ToVector().ACF();

    /// <summary>Computes the partial autocorrelation for all lags.</summary>
    /// <returns>Pairs lags/partial autocorrelation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Series<int> PACF() => ToVector().PACF();

    /// <summary>Calculate coefficients for an autoregressive model.</summary>
    /// <param name="degree">Number of degrees in the model.</param>
    /// <returns>The coefficients of the AR(degree) model.</returns>
    public DVector AutoRegression(int degree) => ToVector().AutoRegression(degree);

    /// <summary>Creates an AR model from a sequence and a degree.</summary>
    /// <param name="degree">Number of independent variables in the model.</param>
    /// <returns>A full autoregressive model.</returns>
    public ARVModel ARModel(int degree) => new(ToVector(), degree);

    /// <summary>Calculate coefficients for a moving average model.</summary>
    /// <param name="degree">Number of degrees in the model.</param>
    /// <returns>
    /// The coefficients of the MA(degree) model. The first coefficient is the constant term.
    /// </returns>
    public DVector MovingAverage(int degree) => ToVector().MovingAverage(degree);

    /// <summary>Creates a MV model from a sequence and a degree.</summary>
    /// <param name="degree">Number of independent variables in the model.</param>
    /// <returns>A full moving average model.</returns>
    public MAVModel MAModel(int degree) => new(ToVector(), degree);

    /// <summary>Computes the real discrete Fourier transform.</summary>
    /// <returns>The spectrum.</returns>
    public FftRModel Fft() => new(FFT.Transform(Materialize()));
}
