namespace Austra.Library;

/// <summary>Represents any sequence returning double-precision values.</summary>
public abstract partial class DSequence : IFormattable
{
    /// <summary>Implements a sequence of doubles with a known length.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    private abstract class FixLengthSequence(int length) : DSequence
    {
        /// <summary>The length of the sequence.</summary>
        protected readonly int length = length;

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public sealed override int Length() => length;

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected sealed override bool HasLength => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override double[] Materialize() => Materialize(length);
    }

    /// <summary>A fixed length sequence with an integer cursor.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    private abstract class CursorSequence(int length) : FixLengthSequence(length)
    {
        /// <summary>The current cursor/index in the sequence.</summary>
        protected int current;

        /// <summary>Resets the sequence by reseting the cursor.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            current = 0;
            return this;
        }
    }

    /// <summary>A fixed length sequence that repeats the same value a number of times.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    /// <param name="value">The repeated value.</param>
    private sealed class RepeatSequence(int length, double value): CursorSequence(length)
    {
        /// <summary>The value to repeat.</summary>
        private readonly double value = value;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current++ < length)
            {
                value = this.value;
                return true;
            }
            value = default;
            return false;
        }

        public override double this[int idx] => 
            (uint)idx < length ? value : throw new IndexOutOfRangeException();

        public override bool Contains(double value) => value == this.value;

        public override double First() => value;
        public override double Last() => value;
        public override double Max() => value;
        public override double Min() => value;
        public override double Sum() => length * value;
        public override double Product() => Pow(value, length);
        public override DSequence Distinct() => this;
        public override DSequence Sort() => this;
        public override DSequence SortDescending() => this;

        protected override bool ContainsZero => value == 0d;
        protected override DSequence Negate() => new RepeatSequence(length, -value);
        protected override DSequence Scale(double d) =>
            new RepeatSequence(length, value * d);
        protected override DSequence Shift(double d) =>
            new RepeatSequence(length, value + d);
    }

    /// <summary>Implements a sequence transformed by a mapper lambda.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="mapper">The mapping function.</param>
    private sealed class Mapped(DSequence source, Func<double, double> mapper) : DSequence
    {
        /// <summary>The mapping function.</summary>
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
        public override DSequence Map(Func<double, double> mapper) =>
            new Mapped(source, x => mapper(this.mapper(x)));

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() =>
            source.HasLength ? source.Length() : base.Length();

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
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
    private sealed class Filtered(DSequence source, Func<double, bool> filter) : DSequence
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

        /// <summary>Transform a sequence acording to the function passed as parameter.</summary>
        /// <remarks>This implementation conflates filter and mapper into a single instance.</remarks>
        /// <param name="mapper">The transforming function.</param>
        /// <returns>The filtered and transformed sequence.</returns>
        public override DSequence Map(Func<double, double> mapper) =>
            new FilteredMapped(source, filter, mapper);

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>A sequence filtered and transformed by lambdas.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="filter">The filtering lambda.</param>
    /// <param name="mapper">The mapping function.</param>
    private sealed class FilteredMapped(
        DSequence source,
        Func<double, bool> filter,
        Func<double, double> mapper) : DSequence
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            while (source.Next(out value))
                if (filter(value))
                {
                    value = mapper(value);
                    return true;
                }
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Joins the common part of two sequences with the help of a lambda.</summary>
    /// <param name="s1">First sequence.</param>
    /// <param name="s2">Second sequence.</param>
    /// <param name="zipper">The joining function.</param>
    private sealed class Zipped(DSequence s1, DSequence s2,
        Func<double, double, double> zipper) : DSequence
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
        public override DSequence Reset()
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
        public override int Length() =>
            HasLength ? Math.Min(s1.Length(), s2.Length()) : base.Length();

        /// <summary>Checks if we can get the length without iterating.</summary>
        /// <remarks>
        /// We can calculate the length if both operands has a known length.
        /// </remarks>
        protected override bool HasLength => s1.HasLength && s2.HasLength;
    }

    /// <summary>Returns a sequence while a condition is met.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="condition">The condition that must be met.</param>
    private class SeqWhile(DSequence source, Func<double, bool> condition) : DSequence
    {
        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (source.Next(out value) && condition(value))
                return true;
            value = default;
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Returns a sequence until a condition is met.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="condition">The condition that must be met.</param>
    private class SeqUntil(DSequence source, Func<double, bool> condition) : DSequence
    {
        private bool done;

        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (!done && source.Next(out value))
            {
                done = condition(value);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            done = false;
            source.Reset();
            return this;
        }
    }

    /// <summary>Returns a sequence until a condition is met.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="sentinel">The value to stop iterating.</param>
    private class SeqUntilValue(DSequence source, double sentinel) : DSequence
    {
        private bool done;

        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (!done && source.Next(out value))
            {
                done = value == sentinel;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            done = false;
            source.Reset();
            return this;
        }
    }

    /// <summary>A range sequence with a mapping function.</summary>
    /// <param name="first">First item.</param>
    /// <param name="last">Last item.</param>
    /// <param name="mapper">The mapping function.</param>
    private sealed class MappedRangeAsc(int first, int last, Func<double, double> mapper)
        : FixLengthSequence(Abs(last - first) + 1)
    {
        /// <summary>First number in the sequence.</summary>
        private readonly double first = first;
        /// <summary>Current value.</summary>
        private int current;

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override double this[int index] =>
            (uint)index >= length
            ? throw new IndexOutOfRangeException()
            : mapper(first + index);

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value.</returns>
        public override double First() => mapper(first);

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value.</returns>
        public override double Last() => mapper(first + length - 1);

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < length)
            {
                value = mapper(first + current++);
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>A range sequence with a mapping function.</summary>
    /// <param name="first">First item.</param>
    /// <param name="last">Last item.</param>
    /// <param name="mapper">The mapping function.</param>
    private sealed class MappedRangeDesc(int first, int last, Func<double, double> mapper)
        : FixLengthSequence(Abs(last - first) + 1)
    {
        /// <summary>First number in the sequence.</summary>
        private readonly double first = first;
        /// <summary>Current value.</summary>
        private int current;

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override double this[int index] =>
            (uint)index >= length
            ? throw new IndexOutOfRangeException()
            : mapper(first - index);

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value.</returns>
        public override double First() => mapper(first);

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value.</returns>
        public override double Last() => mapper(first - length + 1);

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < length)
            {
                value = mapper(first - current++);
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
    private class RangeSequence(int first, int last) : FixLengthSequence(Abs(last - first) + 1)
    {
        /// <summary>Current value.</summary>
        protected int current = first;
        /// <summary>First value in the sequence.</summary>
        protected int first = first;
        /// <summary>Last value in the sequence.</summary>
        protected int last = last;

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override double this[int index] =>
            (uint)index < length ? first + index : throw new IndexOutOfRangeException();

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public override double this[Index idx] => idx.IsFromEnd
            ? new RangeSequenceDesc(last, first)[idx.Value - 1]
            : this[idx.Value];

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override DSequence this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length());
                return new RangeSequence(first + offset, first + (offset + length - 1));
            }
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            current = first;
            return this;
        }

        /// <summary>Transform a sequence acording to the function passed as parameter.</summary>
        /// <param name="mapper">The transforming function.</param>
        /// <returns>The transformed sequence.</returns>
        public sealed override DSequence Map(Func<double, double> mapper) =>
            first <= last
            ? new MappedRangeAsc(first, last, mapper)
            : new MappedRangeDesc(first, last, mapper);

        /// <summary>Checks if the sequence contains a zero value.</summary>
        protected override bool ContainsZero => first <= 0 && last >= 0;

        /// <summary>Negates a sequence without an underlying storage.</summary>
        /// <returns>The negated sequence.</returns>
        protected override DSequence Negate() => new RangeSequenceDesc(-first, -last);

        /// <summary>Sorts the content of this sequence.</summary>
        /// <returns>A sorted sequence.</returns>
        public override DSequence Sort() => this;

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <returns>A sorted sequence in descending order.</returns>
        public override DSequence SortDescending() => new RangeSequenceDesc(last, first);

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value, or <see cref="double.NaN"/> when empty.</returns>
        public override double First() => first;

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value, or <see cref="double.NaN"/> when empty.</returns>
        public override double Last() => last;

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override double Min() => first;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override double Max() => last;

        /// <summary>Gets the sum of all the values in the sequence.</summary>
        /// <returns>The sum of all the values in the sequence.</returns>
        public override double Sum() => ((long)last * (last + 1) - (long)first * (first - 1)) / 2.0;

        /// <summary>Gets only the unique values in this sequence.</summary>
        /// <remarks>This sequence has always unique values.</remarks>
        /// <returns>A sequence with unique values.</returns>
        public sealed override DSequence Distinct() => this;

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
        /// <remarks>Returns a sequence with the inverted order.</remarks>
        /// <returns>A sorted sequence.</returns>
        public override DSequence Sort() => new RangeSequence(last, first);

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <remarks>This sequence is already in descending order.</remarks>
        /// <returns>A sorted sequence in descending order.</returns>
        public override DSequence SortDescending() => this;

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override double this[int index] =>
            (uint)index < length ? first - index : throw new IndexOutOfRangeException();

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public override double this[Index idx] => idx.IsFromEnd
            ? new RangeSequence(last, first)[idx.Value - 1]
            : this[idx.Value];

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override DSequence this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length());
                return new RangeSequenceDesc(
                    first - offset, first - (offset + length - 1));
            }
        }

        /// <summary>Checks if the sequence contains a zero value.</summary>
        protected override bool ContainsZero => first >= 0 && last <= 0;

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override double Min() => last;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override double Max() => first;

        /// <summary>Gets the sum of all the values in the sequence.</summary>
        /// <returns>The sum of all the values in the sequence.</returns>
        public override double Sum() => ((long)first * (first + 1) - (long)last * (last - 1)) / 2.0;

        /// <summary>Negates a sequence without an underlying storage.</summary>
        /// <returns>The negated sequence.</returns>
        protected override DSequence Negate() => new RangeSequence(-first, -last);

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
    private sealed class GridSequence(double lower, double upper, int steps) :
        CursorSequence(steps + 1)
    {
        /// <summary>The distance between two steps.</summary>
        private readonly double delta = (upper - lower) / steps;

        /// <summary>Negates a sequence without an underlying storage.</summary>
        /// <returns>The negated sequence.</returns>
        protected override DSequence Negate() => new GridSequence(-lower, -upper, steps);

        /// <summary>Shifts a sequence without an underlying storage.</summary>
        /// <param name="d">Amount to shift.</param>
        /// <returns>The shifted sequence.</returns>
        protected override DSequence Shift(double d) =>
            new GridSequence(lower + d, upper + d, steps);

        /// <summary>Scales a sequence without an underlying storage.</summary>
        /// <param name="d">The scalar multiplier.</param>
        /// <returns>The scaled sequence.</returns>
        protected override DSequence Scale(double d) =>
            new GridSequence(lower * d, upper * d, steps);

        /// <summary>Gets only the unique values in this sequence.</summary>
        /// <remarks>This sequence has always unique values.</remarks>
        /// <returns>A sequence with unique values.</returns>
        public override DSequence Distinct() => this;

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override double this[int index] =>
            (uint)index < Length() ? lower + index * delta : throw new IndexOutOfRangeException();

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public override double this[Index idx]
        {
            get
            {
                int i = idx.GetOffset(Length());
                return (uint)i < Length() ? lower + i * delta : throw new IndexOutOfRangeException();
            }
        }

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override DSequence this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length());
                return new GridSequence(
                    lower + offset * delta, lower + (offset + length - 1) * delta, length - 1);
            }
        }

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value, or <see cref="double.NaN"/> when empty.</returns>
        public override double First() => lower;

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value, or <see cref="double.NaN"/> when empty.</returns>
        public override double Last() => upper;

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
    private sealed class VectorSequence(DVector source) : CursorSequence(source.Length)
    {
        /// <summary>Creates a sequence of doubles from an array of doubles.</summary>
        /// <param name="values">An array of doubles.</param>
        public VectorSequence(double[] values) : this(new DVector(values)) { }

        /// <summary>Creates a sequence of doubles from the values in a series.</summary>
        /// <remarks>The values array of the series is reversed.</remarks>
        /// <param name="series">The time series.</param>
        public VectorSequence(Series series) : this(series.Values.Reverse()) { }

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < length)
            {
                value = source.UnsafeThis(current++);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override double this[int index] => source[index];

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public override double this[Index idx] => source[idx];

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override DSequence this[Range range] => new VectorSequence(source[range]);

        /// <summary>Checks if the underlying vector contains the given value.</summary>
        /// <param name="value">Value to locate.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        public override bool Contains(double value) => source.Contains(value);

        /// <summary>Gets all statistics from the values in the secuence.</summary>
        /// <returns>Simple statistics of all the values in the sequence.</returns>
        public override Accumulator Stats() => source.Stats();

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value, or <see cref="double.NaN"/> when empty.</returns>
        public override double First() => source[0];

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value, or <see cref="double.NaN"/> when empty.</returns>
        public override double Last() => source[^1];

        /// <summary>Checks if the sequence contains a zero value.</summary>
        /// <remarks>
        /// This is a fast check, and we try it to be sure.
        /// Of course, a zero could be anywhere in the sequence.
        /// </remarks>
        protected override bool ContainsZero =>
            length > 1 && (source.UnsafeThis(0) == 0d || source.UnsafeThis(length - 1) == 0d);

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override double Min() => source.Minimum();

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override double Max() => source.Maximum();

        /// <summary>Gets the sum of all the values in the sequence.</summary>
        /// <returns>The sum of all the values in the sequence.</returns>
        public override double Sum() => source.Sum();

        /// <summary>Gets the product of all the values in the sequence.</summary>
        /// <returns>The product of all the values in the sequence.</returns>
        public override double Product() => ContainsZero ? 0d : source.Product();

        /// <summary>Checks the sequence has a storage.</summary>
        protected override bool HasStorage => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override double[] Materialize() => (double[])source;
    }

    /// <summary>Implements a sequence using random values.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="random">Random generator.</param>
    private sealed class RandomSequence(int length, Random random) : CursorSequence(length)
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < length)
            {
                value = random.NextDouble();
                current++;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override double[] Materialize()
        {
            if (!Avx512F.IsSupported || length < V8d.Count)
                return base.Materialize();
            double[] data = GC.AllocateUninitializedArray<double>(length);
            ref double a = ref MM.GetArrayDataReference(data);
            nuint t = (nuint)(data.Length - V8d.Count);
            Random512 rnd = Random512.Shared;
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(rnd.NextDouble(), ref a, i);
            V8.StoreUnsafe(rnd.NextDouble(), ref a, t);
            Reset();
            return data;
        }
    }

    /// <summary>Implements a sequence using normal random values.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="random">Random generator.</param>
    private sealed class NormalRandomSequence(int length, NormalRandom random) :
        CursorSequence(length)
    {
        /// <summary>Shifts a sequence without an underlying storage.</summary>
        /// <param name="d">Amount to shift.</param>
        /// <returns>The shifted sequence.</returns>
        protected override DSequence Shift(double d) =>
            new NormalRandomSequence(length, random.Shift(d));

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < length)
            {
                value = random.NextDouble();
                current++;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override double[] Materialize()
        {
            if (!Avx512F.IsSupported || length < V8d.Count ||
                random != Library.Stats.NormalRandom.Shared)
                return base.Materialize();
            double[] data = GC.AllocateUninitializedArray<double>(length);
            ref double a = ref MM.GetArrayDataReference(data);
            nuint t = (nuint)(data.Length - V8d.Count);
            Random512 rnd = Random512.Shared;
            for (nuint i = 0; i < t; i += (nuint)V8d.Count)
                V8.StoreUnsafe(rnd.NextNormal(), ref a, i);
            V8.StoreUnsafe(rnd.NextNormal(), ref a, t);
            Reset();
            return data;
        }
    }

    /// <summary>Implements an autoregressive sequence.</summary>
    /// <param name="length">The length of the sequence.</param>
    /// <param name="variance">Variance of the sequence.</param>
    /// <param name="coefficients">Autoregression coefficients.</param>
    private sealed class ArSequence(int length, double variance, DVector coefficients) :
        CursorSequence(length)
    {
        /// <summary>The normal random source for noise.</summary>
        private readonly NormalRandom generator = new(0, Sqrt(variance));
        /// <summary>Autoregression coefficients.</summary>
        private readonly double[] coefficients = (double[])coefficients;
        /// <summary>Buffer for previous terms in the sequence.</summary>
        private readonly double[] previousTerms = new double[coefficients.Length];
        /// <summary>Cached samples when AVX512F is supported.</summary>
        private V8d nextTerms;
        /// <summary>Next cached sample index, when AVX512F is supported.</summary>
        private int next = int.MaxValue;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < length)
            {
                if (V8.IsHardwareAccelerated)
                {
                    if (next >= V8d.Count)
                    {
                        nextTerms = Random512.Shared.NextNormal() * V8.Create(generator.StandardDeviation);
                        next = 0;
                    }
                    value = nextTerms[next++] + coefficients.AsSpan().Dot(previousTerms);
                }
                else
                    value = generator.NextDouble() + coefficients.AsSpan().Dot(previousTerms);
                Array.Copy(previousTerms, 0, previousTerms, 1, previousTerms.Length - 1);
                previousTerms[0] = value;
                current++;
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>Implements moving average sequence.</summary>
    /// <param name="length">The length of the sequence.</param>
    /// <param name="variance">Variance of the sequence.</param>
    /// <param name="mean">Mean term.</param>
    /// <param name="coefficients">Moving average coefficients.</param>
    private sealed class MaSequence(int length, double variance, double mean, DVector coefficients) :
        CursorSequence(length)
    {
        /// <summary>The normal random source for noise.</summary>
        private readonly NormalRandom generator = new(0, Sqrt(variance));
        /// <summary>Autoregression coefficients.</summary>
        private readonly double[] coefficients = (double[])coefficients;
        /// <summary>Buffer for previous terms in the sequence.</summary>
        private readonly double[] previousTerms = new double[coefficients.Length];
        /// <summary>Cached samples when AVX512F is supported.</summary>
        private V8d nextTerms;
        /// <summary>Next cached sample index, when AVX512F is supported.</summary>
        private int next = int.MaxValue;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < length)
            {
                double innovation;
                if (V8.IsHardwareAccelerated)
                {
                    if (next >= V8d.Count)
                    {
                        nextTerms = Random512.Shared.NextNormal() * V8.Create(generator.StandardDeviation);
                        next = 0;
                    }
                    innovation = nextTerms[next++];
                }
                else
                    innovation = generator.NextDouble();
                value = mean + innovation + coefficients.AsSpan().Dot(previousTerms);
                Array.Copy(previousTerms, 0, previousTerms, 1, previousTerms.Length - 1);
                previousTerms[0] = innovation;
                current++;
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>Implements an unfolding sequence using a generator function.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generator function.</param>
    private sealed class Unfolder0(int length, double seed, Func<double, double> unfold) :
        CursorSequence(length)
    {
        private readonly double seed = seed;
        private double x = seed;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < length)
            {
                if (current == 0)
                    value = x;
                else
                {
                    value = unfold(x);
                    x = value;
                }
                current++;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Resets the sequence by reseting the cursor.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            x = seed;
            return base.Reset();
        }
    }

    /// <summary>Implements an unfolding sequence using a generator function.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generator function.</param>
    private sealed class Unfolder1(int length, double seed, Func<int, double, double> unfold) :
        CursorSequence(length)
    {
        private readonly double seed = seed;
        private double x = seed;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < length)
            {
                if (current == 0)
                    value = x;
                else
                {
                    value = unfold(current, x);
                    x = value;
                }
                current++;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Resets the sequence by reseting the cursor.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            x = seed;
            return base.Reset();
        }
    }

    /// <summary>Implements an unfolding sequence using a generator function.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="second">Second value in the sequence.</param>
    /// <param name="unfold">The generator function.</param>
    private sealed class Unfolder2(int length, double first, double second,
        Func<double, double, double> unfold) : CursorSequence(length)
    {
        private readonly double first = first;
        private readonly double second = second;
        private double x = first, y = second;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (current < length)
            {
                if (current == 0)
                    value = x;
                else if (current == 1)
                    value = y;
                else
                {
                    value = unfold(x, y);
                    x = y;
                    y = value;
                }
                current++;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Resets the sequence by reseting the cursor.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DSequence Reset()
        {
            (x, y) = (first, second);
            return base.Reset();
        }
    }
}