namespace Austra.Library;

/// <summary>Represents any sequence returning integer values.</summary>
public abstract partial class NSequence : Sequence<int, NSequence>
{
    /// <summary>Creates a sequence from a range.</summary>
    /// <param name="first">The first value in the sequence.</param>
    /// <param name="last">The last value in the sequence.</param>
    /// <returns>A sequence returning a range of values.</returns>
    public static NSequence Create(int first, int last) =>
        first <= last ? new RangeSequence(first, last) : new RangeSequenceDesc(first, last);

    /// <summary>Creates a sequence from a vector.</summary>
    /// <param name="values">The vector containing the sequence's values.</param>
    /// <returns>The sequence encapsulating the vector.</returns>
    public static NSequence Create(int[] values) =>
        new VectorSequence(values);

    /// <summary>Transform a sequence acording to the function passed as parameter.</summary>
    /// <param name="mapper">The transforming function.</param>
    /// <returns>The transformed sequence.</returns>
    public override NSequence Map(Func<int, int> mapper) =>
        new Mapped(this, mapper);

    /// <summary>Transform a sequence acording to the predicate passed as parameter.</summary>
    /// <param name="filter">A predicate for selecting surviving values</param>
    /// <returns>The filtered sequence.</returns>
    public override NSequence Filter(Func<int, bool> filter) =>
        new Filtered(this, filter);

    /// <summary>Joins the common part of two sequence with the help of a lambda.</summary>
    /// <param name="other">The second sequence.</param>
    /// <param name="zipper">The joining sequence.</param>
    /// <returns>The combined sequence.</returns>
    public override NSequence Zip(NSequence other, Func<int, int, int> zipper) =>
        new Zipped(this, other, zipper);

    /// <summary>Gets the value at the specified index.</summary>
    /// <param name="idx">A position inside the sequence.</param>
    /// <returns>The value at the given position.</returns>
    public override int this[Index idx] => idx.IsFromEnd ? Materialize()[idx] : this[idx.Value];

    /// <summary>Gets a range from the sequence.</summary>
    /// <param name="range">A range inside the sequence.</param>
    /// <returns>The sequence for the given range.</returns>
    public override NSequence this[Range range] => new VectorSequence(Materialize()[range]);

    /// <summary>Negates a sequence.</summary>
    /// <param name="s">The sequence operand.</param>
    /// <returns>The component by component negation.</returns>
    public static NSequence operator -(NSequence s)
    {
        //if (!s.HasStorage)
        return s.Negate();
        //double[] a = s.Materialize();
        //double[] r = GC.AllocateUninitializedArray<double>(a.Length);
        //a.AsSpan().NegV(r);
        //return new VectorSequence(r);
    }

    /// <summary>Negates a sequence without an underlying storage.</summary>
    /// <returns>The negated sequence.</returns>
    protected virtual NSequence Negate() => Map(x => -x);

    /// <summary>Item by item multiplication of two sequences.</summary>
    /// <param name="other">The second sequence.</param>
    /// <returns>A sequence with all the multiplication results.</returns>
    public override NSequence PointwiseMultiply(NSequence other)
    {
        //if (!HasStorage && !other.HasStorage)
        return new Zipped(this, other, (x, y) => x * y);
        //double[] a1 = Materialize();
        //double[] a2 = other.Materialize();
        //int size = Math.Min(a1.Length, a2.Length);
        //return new VectorSequence(a1.AsSpan(size).MulV(a2.AsSpan(size)));
    }

    /// <summary>Item by item division of sequences.</summary>
    /// <param name="other">The second sequence.</param>
    /// <returns>A sequence with all the quotient results.</returns>
    public override NSequence PointwiseDivide(NSequence other)
    {
        //if (!HasStorage && !other.HasStorage)
        return new Zipped(this, other, (x, y) => x / y);
        //double[] a1 = Materialize();
        //double[] a2 = other.Materialize();
        //int size = Math.Min(a1.Length, a2.Length);
        //return new VectorSequence(a1.AsSpan(size).DivV(a2.AsSpan(size)));
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
        return Create([.. set]);
    }

    /// <summary>Gets the first value in the sequence.</summary>
    /// <returns>The first value, or <see cref="double.NaN"/> when empty.</returns>
    public override int First()
    {
        if (!Next(out int value))
            return default;
        return value;
    }

    /// <summary>Gets the last value in the sequence.</summary>
    /// <returns>The last value, or <see cref="double.NaN"/> when empty.</returns>
    public override int Last()
    {
        int saved = default;
        while (Next(out int value))
            saved = value;
        return saved;
    }

    /// <summary>Gets the minimum value from the sequence.</summary>
    /// <returns>The minimum value.</returns>
    public virtual int Min()
    {
        if (!Next(out int value))
            return int.MaxValue;
        while (Next(out int v))
            value = Math.Min(value, v);
        return value;
    }

    /// <summary>Gets the maximum value from the sequence.</summary>
    /// <returns>The maximum value.</returns>
    public virtual int Max()
    {
        if (!Next(out int value))
            return int.MinValue;
        while (Next(out int v))
            value = Math.Max(value, v);
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

    /// <summary>Creates an array with all values from the sequence.</summary>
    /// <returns>The values as an array.</returns>
    protected override int[] Materialize()
    {
        if (HasLength)
        {
            int[] data = GC.AllocateUninitializedArray<int>(Length());
            ref int rd = ref MM.GetArrayDataReference(data);
            while (Next(out int value))
            {
                rd = value;
                rd = ref Add(ref rd, 1);
            }
            return data;
        }
        List<int> values = [];
        while (Next(out int value))
            values.Add(value);
        return [.. values];
    }
}
