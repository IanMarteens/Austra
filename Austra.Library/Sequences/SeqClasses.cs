﻿namespace Austra.Library;

/// <summary>Represents any sequence returning a double value.</summary>
public abstract partial class DoubleSequence : IFormattable
{
    /// <summary>Implements a sequence transformed by a mapper lambda.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="mapper">The mapping function.</param>
    private sealed class Mapped(DoubleSequence source, Func<double, double> mapper) : DoubleSequence
    {
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

        /// <summary>Transform a sequence acording to the function passed as parameter.</summary>
        /// <remarks>This implementation conflates two mappers into a single instance.</remarks>
        /// <param name="mapper">The transforming function.</param>
        /// <returns>The transformed sequence.</returns>
        public override DoubleSequence Map(Func<double, double> mapper) =>
            new Mapped(source, x => mapper(this.mapper(x)));

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() =>
            source.HasLength ? source.Length() : base.Length();

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DoubleSequence Reset()
        {
            source.Reset();
            return this;
        }

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => source.HasLength;
    }

    /// <summary>Implements a sequence filtered by a predicate.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="filter">The filtering lambda.</param>
    private sealed class Filtered(DoubleSequence source, Func<double, bool> filter) : DoubleSequence
    {
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

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DoubleSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Joins the common part of two sequences with the help of a lambda.</summary>
    /// <param name="s1">First sequence.</param>
    /// <param name="s2">Second sequence.</param>
    /// <param name="zipper">The joining function.</param>
    private sealed class Zipped(DoubleSequence s1, DoubleSequence s2,
        Func<double, double, double> zipper) : DoubleSequence
    {
        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            while (s1.Next(out double value1) && s2.Next(out double value2))
            {
                value = zipper(value1, value2);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DoubleSequence Reset()
        {
            s1.Reset();
            s2.Reset();
            return this;
        }

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <remarks>
        /// We can calculate the length if both operands has a known length.
        /// </remarks>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() => HasLength ? Math.Min(s1.Length(), s2.Length()) : base.Length();

        /// <summary>Checks if we can get the length without iterating.</summary>
        /// <remarks>
        /// We can calculate the length if both operands has a known length.
        /// </remarks>
        protected override bool HasLength => s1.HasLength && s2.HasLength;
    }

    /// <summary>Implements a sequence of double values based in an integer range.</summary>
    /// <remarks>Creates a double sequence from an integer range.</remarks>
    /// <param name="first">The first value in the sequence.</param>
    /// <param name="last">The last value in the sequence.</param>
    private class RangeSequence(int first, int last) : DoubleSequence
    {
        /// <summary>Calculated length of the sequence.</summary>
        private readonly int length = Abs(last - first) + 1;
        /// <summary>Current value.</summary>
        protected int current = first;
        /// <summary>First value in the sequence.</summary>
        protected int first = first;
        /// <summary>Last value in the sequence.</summary>
        protected int last = last;

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public sealed override int Length() => length;

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected sealed override bool HasLength => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected sealed override double[] Materialize()
        {
            double[] result = GC.AllocateUninitializedArray<double>(length);
            Materialize(result.AsSpan());
            return result;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DoubleSequence Reset()
        {
            current = first;
            return this;
        }

        /// <summary>Negates a sequence without an underlying storage.</summary>
        /// <returns>The negated sequence.</returns>
        protected override DoubleSequence Negate() => new RangeSequenceDesc(-first, -last);

        /// <summary>Sorts the content of this sequence.</summary>
        /// <returns>A sorted sequence.</returns>
        public override DoubleSequence Sort() => this;

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <returns>A sorted sequence in descending order.</returns>
        public override DoubleSequence SortDescending() => new RangeSequenceDesc(last, first);

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override double Min() => first;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override double Max() => last;

        /// <summary>Gets only the unique values in this sequence.</summary>
        /// <remarks>This sequence has always unique values.</remarks>
        /// <returns>A sequence with unique values.</returns>
        public sealed override DoubleSequence Distinct() => this;

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

    /// <summary>Implements a sequence of double values based in an integer range.</summary>
    /// <remarks>Creates a double sequence from an integer range.</remarks>
    /// <param name="first">The first value in the sequence.</param>
    /// <param name="last">The last value in the sequence.</param>
    private sealed class RangeSequenceDesc(int first, int last) : RangeSequence(first, last)
    {
        /// <summary>Sorts the content of this sequence.</summary>
        /// <returns>A sorted sequence.</returns>
        public override DoubleSequence Sort() => new RangeSequence(last, first);

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <returns>A sorted sequence in descending order.</returns>
        public override DoubleSequence SortDescending() => this;

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override double Min() => last;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override double Max() => first;

        /// <summary>Negates a sequence without an underlying storage.</summary>
        /// <returns>The negated sequence.</returns>
        protected override DoubleSequence Negate() => new RangeSequence(-first, -last);

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current >= last)
            {
                value = current--;
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
    private sealed class GridSequence(double lower, double upper, int steps) : DoubleSequence
    {
        /// <summary>The distance between two steps.</summary>
        private readonly double delta = (upper - lower) / steps;
        /// <summary>Current index in the sequence.</summary>
        private int current;

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() => steps + 1;

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override double[] Materialize()
        {
            double[] result = GC.AllocateUninitializedArray<double>(steps + 1);
            Materialize(result.AsSpan());
            return result;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DoubleSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Negates a sequence without an underlying storage.</summary>
        /// <returns>The negated sequence.</returns>
        protected override DoubleSequence Negate() => new GridSequence(-lower, -upper, steps);

        /// <summary>Gets only the unique values in this sequence.</summary>
        /// <remarks>This sequence has always unique values.</remarks>
        /// <returns>A sequence with unique values.</returns>
        public override DoubleSequence Distinct() => this;

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override double Min() => Math.Min(lower, upper);

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override double Max() => Math.Max(lower, upper);

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
                value = lower + delta * current++;
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
    private sealed class VectorSequence(Vector source) : DoubleSequence
    {
        /// <summary>Current index in the sequence.</summary>
        private int current;

        /// <summary>Creates a sequence of doubles from an array of doubles.</summary>
        /// <param name="values">An array of doubles.</param>
        public VectorSequence(double[] values) : this(new Vector(values)) { }

        /// <summary>Creates a sequence of doubles from the values in a series.</summary>
        /// <remarks>The values array of the series is reversed.</remarks>
        /// <param name="series">The time series.</param>
        public VectorSequence(Series series) : this(series.GetValues().Reverse()) { }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DoubleSequence Reset()
        {
            current = 0;
            return this;
        }

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

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override double Min() => source.Min();

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override double Max() => source.Max();

        /// <summary>Gets the sum of all the values in the sequence.</summary>
        /// <returns>The sum of all the values in the sequence.</returns>
        public override double Sum() => source.Sum();

        /// <summary>Gets the product of all the values in the sequence.</summary>
        /// <returns>The product of all the values in the sequence.</returns>
        public override double Product() => source.Product();

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() => source.Length;

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => true;

        /// <summary>Checks the sequence has a storage.</summary>
        protected override bool HasStorage => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override double[] Materialize() => (double[])source;
    }

    /// <summary>Implements a sequence using random values.</summary>
    /// <param name="size">Size of the sequence.</param>
    /// <param name="random">Random generator.</param>
    private sealed class RandomSequence(int size, Random random) : DoubleSequence
    {
        /// <summary>The current index in the sequence.</summary>
        private int current;

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() => size;

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override double[] Materialize()
        {
            double[] result = GC.AllocateUninitializedArray<double>(size);
            Materialize(result.AsSpan());
            return result;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DoubleSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < size)
            {
                value = random.NextDouble();
                current++;
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>Implements a sequence using normal random values.</summary>
    /// <param name="size">Size of the sequence.</param>
    /// <param name="random">Random generator.</param>
    private sealed class NormalRandomSequence(int size, NormalRandom random) : DoubleSequence
    {
        /// <summary>The current index in the sequence.</summary>
        private int current;

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() => size;

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override double[] Materialize()
        {
            double[] result = GC.AllocateUninitializedArray<double>(size);
            Materialize(result.AsSpan());
            return result;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DoubleSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < size)
            {
                value = random.NextDouble();
                current++;
                return true;
            }
            value = default;
            return false;
        }
    }
}