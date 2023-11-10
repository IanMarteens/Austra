namespace Austra.Library;

/// <summary>Represents any sequence returning a double value.</summary>
public abstract class DoubleSequence : IFormattable
{
    /// <summary>Gets the next number in the sequence.</summary>
    /// <param name="value">The next number in the sequence.</param>
    /// <returns><see langword="true"/>, when there is a next number.</returns>
    public abstract bool Next(out double value);

    /// <summary>Transform a sequence acording to the function passed as parameter.</summary>
    /// <param name="mapper">The transforming function.</param>
    /// <returns>The transformed sequence.</returns>
    public DoubleSequence Map(Func<double, double> mapper) =>
        new Mapper(this, mapper);

    /// <summary>Transform a sequence acording to the predicate passed as parameter.</summary>
    /// <param name="filter">A predicate for selecting surviving values</param>
    /// <returns>The filtered sequence.</returns>
    public DoubleSequence Filter(Func<double, bool> filter) =>
        new Filtered(this, filter);

    /// <summary>Gets all statistics from the values in the secuence.</summary>
    /// <returns>Simple statistics of all the values in the sequence.</returns>
    public virtual Accumulator Stats()
    {
        Accumulator result = new();
        while (Next(out double value))
            result += value;
        return result;
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

    /// <summary>Creates an array with all values from the sequence.</summary>
    /// <returns>The values as an array.</returns>
    protected virtual double[] Materialize()
    {
        List<double> values = [];
        while (Next(out double value))
            values.Add(value);
        return [.. values];
    }

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

    private sealed class Mapper(DoubleSequence source, Func<double, double> mapper) : DoubleSequence
    {
        private readonly DoubleSequence source = source;
        private readonly Func<double, double> mapper = mapper;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (source.Next(out value))
            {
                value = mapper(value);
                return true;
            }
            return false;
        }
    }

    private sealed class Filtered(DoubleSequence source, Func<double, bool> filter) : DoubleSequence
    {
        private readonly DoubleSequence source = source;
        private readonly Func<double, bool> filter = filter;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            while (source.Next(out value))
                if (filter(value))
                    return true;
            return false;
        }
    }
}

/// <summary>Implements a sequence of double values based in an integer range.</summary>
/// <remarks>Creates a double sequence from an integer range.</remarks>
/// <param name="first">The first value in the sequence.</param>
/// <param name="last">The last value in the sequence.</param>
public class DoubleRangeSequence(int first, int last) : DoubleSequence
{
    private readonly int last = last;
    private int current = first;

    /// <summary>Gets the next number in the sequence.</summary>
    /// <param name="value">The next number in the sequence.</param>
    /// <returns><see langword="true"/>, when there is a next number.</returns>
    public override bool Next(out double value)
    {
        if (current <= last)
        {
            value = current++;
            return true;
        }
        value = default;
        return false;
    }
}

/// <summary>Implements a sequence of double values based in a grid.</summary>
/// <remarks>Creates a double sequence from a grid.</remarks>
/// <param name="lower">The first value in the sequence.</param>
/// <param name="upper">The last value in the sequence.</param>
/// <param name="steps">The number of steps in the sequence, minus one.</param>
public class DoubleGridSequence(double lower, double upper, int steps) : DoubleSequence
{
    private readonly double lower = lower;
    private readonly double upper = upper;
    private readonly int steps = steps;
    private int current;

    /// <summary>Gets the next number in the sequence.</summary>
    /// <param name="value">The next number in the sequence.</param>
    /// <returns><see langword="true"/>, when there is a next number.</returns>
    public override bool Next(out double value)
    {
        if (current == 0)
        {
            value = lower;
            current = 1;
        }
        else if (current == steps)
        {
            value = upper;
            current++;
        }
        else if (current < steps)
            value = lower + (upper - lower) * current++ / steps;
        else
        {
            value = default;
            return false;
        }
        return true;
    }
}

/// <summary>Implements a sequence using a vector as its storage.</summary>
/// <param name="source">The underlying vector.</param>
public class DoubleVectorSequence(Vector source) : DoubleSequence
{
    private readonly Vector source = source;
    private int current;

    /// <summary>Creates a sequence of doubles from the values in a series.</summary>
    /// <param name="series">The time series.</param>
    public DoubleVectorSequence(Series series) : this(series.GetValues().Reverse()) { }

    /// <summary>Gets the next number in the sequence.</summary>
    /// <param name="value">The next number in the sequence.</param>
    /// <returns><see langword="true"/>, when there is a next number.</returns>
    public override bool Next(out double value)
    {
        if (current < source.Length)
        {
            value = source[current++];
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>Gets all statistics from the values in the secuence.</summary>
    /// <returns>Simple statistics of all the values in the sequence.</returns>
    public override Accumulator Stats() => source.Stats();

    /// <summary>Gets the sum of all the values in the sequence.</summary>
    /// <returns>The sum of all the values in the sequence.</returns>
    public override double Sum() => source.Sum();

    /// <summary>Gets the product of all the values in the sequence.</summary>
    /// <returns>The product of all the values in the sequence.</returns>
    public override double Product() => source.Product();

    /// <summary>Gets the total number of values in the sequence.</summary>
    /// <returns>The total number of values in the sequence.</returns>
    public override int Length() => source.Length;

    /// <summary>Creates an array with all values from the sequence.</summary>
    /// <returns>The values as an array.</returns>
    protected override double[] Materialize() => (double[])source;
}
