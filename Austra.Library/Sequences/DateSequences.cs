namespace Austra.Library;

/// <summary>Represents any sequence returning Austra dates.</summary>
public abstract partial class DateSequence : BaseSequence<Date, DateSequence>,
    IFormattable,
    IEquatable<DateSequence>,
    IEqualityOperators<DateSequence, DateSequence, bool>,
    IContainer<Date>,
    IIndexable
{
    /// <summary>Creates a sequence from a range.</summary>
    /// <param name="first">The first value in the sequence.</param>
    /// <param name="last">The last value in the sequence.</param>
    /// <returns>A sequence returning a range of values.</returns>
    public static DateSequence Create(Date first, Date last) => first <= last
        ? new GridSequence(first, 1, last)
        : new GridSequenceDesc(first, 1, last);

    /// <summary>Creates a sequence from a range and a tenor.</summary>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="step">Distance between sequence values, in days.</param>
    /// <param name="last">Upper bound of the sequence. It may be rounded down.</param>
    /// <returns>A sequence returning a range of values.</returns>
    public static DateSequence Create(Date first, int step, Date last) => first <= last
        ? new GridSequence(first, step, last)
        : new GridSequenceDesc(first, step, last);

    /// <summary>Creates a sequence from a range and a tenor in months.</summary>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="tenor">Distance between sequence values, in months.</param>
    /// <param name="length">The number of values in the sequence.</param>
    /// <returns>A sequence returning a range of values.</returns>
    public static DateSequence Create(Date first, int tenor, int length) =>
        new MonthGridSequence(first, tenor, length);

    /// <summary>Creates a date sequence from a vector.</summary>
    /// <param name="values">The vector containing the sequence's dates.</param>
    /// <returns>The sequence encapsulating the vector.</returns>
    public static DateSequence Create(DateVector values) =>
        new VectorSequence(values);

    /// <summary>Creates a sequence by repeating a value a given number of times.</summary>
    /// <param name="size">The size of the sequence.</param>
    /// <param name="value">The repeated value.</param>
    /// <returns>The repeating sequence.</returns>
    public static DateSequence Repeat(int size, Date value) =>
        new RepeatSequence(size, value);

    /// <summary>Creates a sequence by unfolding an initial state by a function.</summary>
    /// <param name="size">The size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generating function.</param>
    /// <returns>The sequence unfolded from the initial state and the function.</returns>
    public static DateSequence Unfold(int size, Date seed, Func<Date, Date> unfold) =>
        new Unfolder0(size, seed, unfold);

    /// <summary>Creates a sequence by unfolding an initial state by a function.</summary>
    /// <param name="size">The size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generating function.</param>
    /// <returns>The sequence unfolded from the initial state and the function.</returns>
    public static DateSequence Unfold(int size, Date seed, Func<int, Date, Date> unfold) =>
        new Unfolder1(size, seed, unfold);

    /// <summary>Creates a sequence by unfolding an initial state by a function.</summary>
    /// <param name="size">The size of the sequence.</param>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="second">Second value in the sequence.</param>
    /// <param name="unfold">The generating function.</param>
    /// <returns>The sequence unfolded from the initial state and the function.</returns>
    public static DateSequence Unfold(int size, Date first, Date second, Func<Date, Date, Date> unfold) =>
        new Unfolder2(size, first, second, unfold);

    /// <summary>Transform a sequence acording to the function passed as parameter.</summary>
    /// <param name="mapper">The transforming function.</param>
    /// <returns>The transformed sequence.</returns>
    public override DateSequence Map(Func<Date, Date> mapper) => new Mapped(this, mapper);

    /// <summary>Transform a sequence acording to the predicate passed as parameter.</summary>
    /// <param name="filter">A predicate for selecting surviving values</param>
    /// <returns>The filtered sequence.</returns>
    public override DateSequence Filter(Func<Date, bool> filter) => new Filtered(this, filter);

    /// <summary>Joins the common part of two sequence with the help of a lambda.</summary>
    /// <param name="other">The second sequence.</param>
    /// <param name="zipper">The joining sequence.</param>
    /// <returns>The combined sequence.</returns>
    public override DateSequence Zip(DateSequence other, Func<Date, Date, Date> zipper) =>
        new Zipped(this, other, zipper);

    /// <summary>Get the initial values of a sequence that satisfy a predicate.</summary>
    /// <param name="predicate">The predicate to be satisfied.</param>
    /// <returns>A prefix of the original sequence.</returns>
    public override DateSequence While(Func<Date, bool> predicate) =>
        new SeqWhile(this, predicate);

    /// <summary>Get the initial values of a sequence until a predicate is satisfied.</summary>
    /// <param name="predicate">The predicate to be satisfied.</param>
    /// <returns>A prefix of the original sequence.</returns>
    public override DateSequence Until(Func<Date, bool> predicate) =>
        new SeqUntil(this, predicate);

    /// <summary>Get the initial values of a sequence until a value is found.</summary>
    /// <param name="value">The value that will be the end of the new sequence.</param>
    /// <returns>A prefix of the original sequence.</returns>
    public override DateSequence Until(Date value) =>
        new SeqUntilValue(this, value);

    /// <summary>Gets the value at the specified index.</summary>
    /// <param name="idx">A position inside the sequence.</param>
    /// <returns>The value at the given position.</returns>
    public override Date this[Index idx] => idx.IsFromEnd ? Materialize()[idx] : this[idx.Value];

    /// <summary>Gets a range from the sequence.</summary>
    /// <param name="range">A range inside the sequence.</param>
    /// <returns>The sequence for the given range.</returns>
    public override DateSequence this[Range range] => new VectorSequence(Materialize()[range]);

    /// <summary>Gets only the unique values in this sequence.</summary>
    /// <returns>A sequence with unique values.</returns>
    public override DateSequence Distinct()
    {
        if (HasStorage)
            return Create(new HashSet<Date>(Materialize()).ToArray());
        HashSet<Date> set = HasLength ? new(Length()) : [];
        while (Next(out Date d))
            set.Add(d);
        Reset();
        return Create(new DateVector([.. set]));
    }

    /// <summary>Gets the maximum value from the sequence.</summary>
    /// <returns>The maximum value.</returns>
    public virtual Date Max()
    {
        if (!Next(out Date value))
            throw new EmptySequenceException();
        while (Next(out Date v))
            value = Date.Max(value, v);
        Reset();
        return value;
    }

    /// <summary>Gets the minimum value from the sequence.</summary>
    /// <returns>The minimum value.</returns>
    public virtual Date Min()
    {
        if (!Next(out Date value))
            throw new EmptySequenceException();
        while (Next(out Date v))
            value = Date.Max(value, v);
        Reset();
        return value;
    }

    /// <summary>Sorts the content of this sequence.</summary>
    /// <returns>A sorted sequence.</returns>
    public virtual DateSequence Sort()
    {
        Date[] data = Materialize();
        Array.Sort(data);
        return Create(data);
    }

    /// <summary>Sorts the content of this sequence in descending order.</summary>
    /// <returns>A sorted sequence in descending order.</returns>
    public virtual DateSequence SortDescending()
    {
        Date[] data = Materialize();
        Array.Sort(data, (x, y) => y.CompareTo(x));
        return Create(data);
    }

    /// <summary>Checks if two sequence has the same length and arguments.</summary>
    /// <param name="other">The second sequence to be compared.</param>
    /// <returns><see langword="true"/> if the two sequences have the same items.</returns>
    public bool Equals(DateSequence? other) =>
        other is not null && Materialize().Eqs(other.Materialize());

    /// <summary>Checks if the provided argument is a sequence with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a sequence with the same items.</returns>
    public override bool Equals(object? obj) =>
        obj is DateSequence seq && Equals(seq);

    /// <summary>Returns the hashcode for this vector.</summary>
    /// <returns>A hashcode summarizing the content of the vector.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)Materialize()).GetHashCode(EqualityComparer<Date>.Default);

    /// <summary>Compares two vectors for equality. </summary>
    /// <param name="left">First sequence operand.</param>
    /// <param name="right">Second sequence operand.</param>
    /// <returns><see langword="true"/> if all corresponding items are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(DateSequence? left, DateSequence? right) => left?.Equals(right) == true;

    /// <summary>Compares two vectors for inequality. </summary>
    /// <param name="left">First sequence operand.</param>
    /// <param name="right">Second sequence operand.</param>
    /// <returns><see langword="true"/> if any pair of corresponding items are not equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(DateSequence? left, DateSequence? right) => left?.Equals(right) != true;

    /// <summary>Converts this sequence into an integer vector.</summary>
    /// <returns>A new vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateVector ToVector() => Materialize();

    /// <summary>Creates a plot for this sequence.</summary>
    /// <returns>A plot containing a frozen vector as its dataset.</returns>
    public Plot<DateVector> Plot() => new(ToVector());

    /// <summary>Evaluated the sequence and formats it like a <see cref="NVector"/>.</summary>
    /// <returns>A formated list of double values.</returns>
    public override string ToString() => ToString("d");

    /// <summary>Gets a textual representation of this sequence.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider = null)
    {
        Date[] values = Materialize();
        return values.Length == 0 ? "∅" : values.ToString(v => v.ToString(format, provider));
    }

}
