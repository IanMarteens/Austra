namespace Austra.Library;

/// <summary>Represents any sequence returning Austra dates.</summary>
public abstract partial class DateSequence
{
    /// <summary>Implements a sequence of integers with a known length.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    private abstract class FixLengthSequence(int length) : DateSequence
    {
        /// <summary>The length of the sequence.</summary>
        protected readonly int length = length;

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public sealed override Date this[Index idx] => this[idx.GetOffset(length)];

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public sealed override int Length() => length;

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected sealed override bool HasLength => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override Date[] Materialize() => Materialize(length);
    }

    /// <summary>Implements a sequence with an integer cursor.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    private abstract class CursorSequence(int length) : FixLengthSequence(length)
    {
        /// <summary>The current index/cursor on the sequence.</summary>
        protected int current;

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DateSequence Reset()
        {
            current = 0;
            return this;
        }
    }

    /// <summary>A fixed length sequence that repeats the same value a number of times.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    /// <param name="value">The repeated value.</param>
    private sealed class RepeatSequence(int length, Date value) : CursorSequence(length)
    {
        /// <summary>The value to repeat.</summary>
        private readonly Date value = value;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
        {
            if (current++ < length)
            {
                value = this.value;
                return true;
            }
            value = default;
            return false;
        }

        public override Date this[int idx] =>
            (uint)idx < length ? value : throw new IndexOutOfRangeException();

        public override bool Contains(Date value) => value == this.value;

        public override Date First() => value;
        public override Date Last() => value;
        public override Date Max() => value;
        public override Date Min() => value;
        public override DateSequence Distinct() => this;
        public override DateSequence Sort() => this;
        public override DateSequence SortDescending() => this;
    }

    /// <summary>Implements a sequence transformed by a mapper lambda.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="mapper">The mapping function.</param>
    private sealed class Mapped(DateSequence source, Func<Date, Date> mapper) : DateSequence
    {
        /// <summary>The mapping function.</summary>
        private readonly Func<Date, Date> mapper = mapper;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
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
        public override DateSequence Map(Func<Date, Date> mapper) =>
            new Mapped(source, x => mapper(this.mapper(x)));

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => source.HasLength;

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() =>
            source.HasLength ? source.Length() : base.Length();

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DateSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Implements a sequence filtered by a predicate.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="filter">The filtering lambda.</param>
    private sealed class Filtered(DateSequence source, Func<Date, bool> filter) : DateSequence
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
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
        public override DateSequence Map(Func<Date, Date> mapper) =>
            new FilteredMapped(source, filter, mapper);

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DateSequence Reset()
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
        DateSequence source,
        Func<Date, bool> filter,
        Func<Date, Date> mapper) : DateSequence
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
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
        public override DateSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Joins the common part of two sequences with the help of a lambda.</summary>
    /// <param name="s1">First sequence.</param>
    /// <param name="s2">Second sequence.</param>
    /// <param name="zipper">The joining function.</param>
    private sealed class Zipped(DateSequence s1, DateSequence s2,
        Func<Date, Date, Date> zipper) : DateSequence
    {
        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
        {
            while (s1.Next(out Date value1) && s2.Next(out Date value2))
            {
                value = zipper(value1, value2);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DateSequence Reset()
        {
            s1.Reset();
            s2.Reset();
            return this;
        }

        /// <summary>Checks if we can get the length without iterating.</summary>
        /// <remarks>
        /// We can calculate the length if both operands has a known length.
        /// </remarks>
        protected override bool HasLength => s1.HasLength && s2.HasLength;

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <remarks>
        /// We can calculate the length if both operands has a known length.
        /// </remarks>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() =>
            HasLength ? Math.Min(s1.Length(), s2.Length()) : base.Length();
    }

    /// <summary>Returns a sequence while a condition is met.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="condition">The condition that must be met.</param>
    private class SeqWhile(DateSequence source, Func<Date, bool> condition) : DateSequence
    {
        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
        {
            if (source.Next(out value) && condition(value))
                return true;
            value = default;
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override DateSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Returns a sequence until a condition is met.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="condition">The condition that must be met.</param>
    private class SeqUntil(DateSequence source, Func<Date, bool> condition) : DateSequence
    {
        private bool done;

        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
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
        public override DateSequence Reset()
        {
            done = false;
            source.Reset();
            return this;
        }
    }

    /// <summary>Returns a sequence until a condition is met.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="sentinel">The value to stop iterating.</param>
    private class SeqUntilValue(DateSequence source, Date sentinel) : DateSequence
    {
        private bool done;

        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
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
        public override DateSequence Reset()
        {
            done = false;
            source.Reset();
            return this;
        }
    }

    /// <summary>Implements a sequence of integers based in an range and a step.</summary>
    /// <remarks><c>first &lt;= last</c></remarks>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="step">Distance between sequence values, in days.</param>
    /// <param name="last">Upper bound of the sequence. It may be rounded down.</param>
    private class GridSequence(Date first, int step, Date last) :
        FixLengthSequence(Abs(last - first) / step + 1)
    {
        /// <summary>The first value in the sequence.</summary>
        protected readonly Date first = first;
        /// <summary>The last value in the sequence.</summary>
        protected readonly Date last = last;
        /// <summary>The step of the sequence.</summary>
        protected readonly int step = step;
        /// <summary>
        /// Maximum value in the sequence, which is the last value rounded down to the step.
        /// </summary>
        private readonly Date max = first + ((last - first) / step) * step;
        /// <summary>Current value.</summary>
        protected Date current = first;

        /// <summary>
        /// Resets the sequence by setting the next value to <see cref="first"/>.
        /// </summary>
        /// <returns>Echoes this sequence.</returns>
        public sealed override DateSequence Reset()
        {
            current = first;
            return this;
        }

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override Date this[int index] =>
            (uint)index < length ? first + index * step : throw new IndexOutOfRangeException();

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override DateSequence this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length());
                return new GridSequence(
                    first + offset * step, step, first + (offset + length - 1) * step);
            }
        }

        /// <summary>Checks if the underlying vector contains the given value.</summary>
        /// <param name="value">Value to locate.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        public override bool Contains(Date value) =>
            value <= max && value >= first && (value - first) % step == 0;

        /// <summary>Sorts the content of this sequence.</summary>
        /// <returns>A sorted sequence.</returns>
        public override DateSequence Sort() => this;

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <returns>A sorted sequence.</returns>
        public override DateSequence SortDescending() => new GridSequenceDesc(max, step, first);

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value, or <see cref="Date.Zero"/> when empty.</returns>
        public override Date First() => first;

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value, or <see cref="Date.Zero"/> when empty.</returns>
        public override Date Last() => max;

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override Date Min() => first;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override Date Max() => max;

        /// <summary>Gets only the unique values in this sequence.</summary>
        /// <remarks>This sequence has always unique values.</remarks>
        /// <returns>A sequence with unique values.</returns>
        public sealed override DateSequence Distinct() => this;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
        {
            if (current <= last)
            {
                value = current;
                current += step;
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>Implements a sequence of integers based in an range and a step.</summary>
    /// <remarks><c>first &gt;= last</c></remarks>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="step">Distance between sequence values.</param>
    /// <param name="last">Upper bound of the sequence. It may be rounded down.</param>
    private sealed class GridSequenceDesc(Date first, int step, Date last) :
        GridSequence(first, step, last)
    {
        /// <summary>
        /// Last actual value in the sequence, which is the last value rounded up to the step.
        /// </summary>
        private readonly Date min = first - step * (Abs(last - first) / step);

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override Date this[int index] =>
            (uint)index < length ? first - index * step : throw new IndexOutOfRangeException();

        /// <summary>Checks if the underlying vector contains the given value.</summary>
        /// <param name="value">Value to locate.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        public override bool Contains(Date value) =>
            value >= min && value <= first && (value - min) % step == 0;

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override DateSequence this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length());
                return new GridSequenceDesc(
                    first - offset * step, step, first - (offset + length - 1) * step);
            }
        }

        /// <summary>Sorts the content of this sequence.</summary>
        /// <returns>A sorted sequence.</returns>
        public override DateSequence Sort() => new GridSequence(min, step, first);

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <returns>A sorted sequence.</returns>
        public override DateSequence SortDescending() => this;

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override Date Min() => min;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override Date Max() => first;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
        {
            if (current >= last)
            {
                value = current;
                current -= step;
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>Implements a sequence of integers based in an range and a step.</summary>
    /// <remarks><c>first &lt;= last</c></remarks>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="step">Distance between sequence values, in months.</param>
    /// <param name="length">Number of values in the sequence.</param>
    private class MonthGridSequence(Date first, int step, int length) :
        FixLengthSequence(length)
    {
        /// <summary>The first value in the sequence.</summary>
        protected readonly Date first = first;
        /// <summary>The step of the sequence.</summary>
        protected readonly int step = step;
        /// <summary>
        /// Maximum value in the sequence, which is the last value rounded down to the step.
        /// </summary>
        private readonly Date max = first.AddMonths((length - 1) * step);
        /// <summary>Current value.</summary>
        protected int current = 0;

        /// <summary>
        /// Resets the sequence by setting the next value to <see cref="first"/>.
        /// </summary>
        /// <returns>Echoes this sequence.</returns>
        public sealed override DateSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override Date this[int index] =>
            (uint)index < length ? first.AddMonths(index * step) : throw new IndexOutOfRangeException();

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override DateSequence this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length());
                return new MonthGridSequence(first.AddMonths(offset * step), step, length);
            }
        }

        /// <summary>Checks if the underlying vector contains the given value.</summary>
        /// <param name="value">Value to locate.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        public override bool Contains(Date value)
        {
            if (value <= max && value >= first)
            {
                var (y1, m1, _) = first;
                var (y2, m2, _) = value;
                int months = (y2 - y1) * 12 + m2 - m1;
                return months % step == 0 && first.AddMonths(months) == value;
            }
            return false;
        }

        /// <summary>Sorts the content of this sequence.</summary>
        /// <returns>A sorted sequence.</returns>
        public override DateSequence Sort() => this;

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <returns>A sorted sequence.</returns>
        public override DateSequence SortDescending() => first.Day == max.Day
            ? new MonthGridSequenceDesc(max, step, length)
            : base.SortDescending();

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value, or <see cref="Date.Zero"/> when empty.</returns>
        public override Date First() => first;

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value, or <see cref="Date.Zero"/> when empty.</returns>
        public override Date Last() => max;

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override Date Min() => first;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override Date Max() => max;

        /// <summary>Gets only the unique values in this sequence.</summary>
        /// <remarks>This sequence has always unique values.</remarks>
        /// <returns>A sequence with unique values.</returns>
        public sealed override DateSequence Distinct() => this;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
        {
            if (current < length)
            {
                value = first.AddMonths(current++ * step);
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>Implements a sequence of integers based in an range and a step.</summary>
    /// <remarks><c>first &lt;= last</c></remarks>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="step">Distance between sequence values, in months.</param>
    /// <param name="length">Number of values in the sequence.</param>
    private class MonthGridSequenceDesc(Date first, int step, int length) :
        FixLengthSequence(length)
    {
        /// <summary>The first value in the sequence.</summary>
        protected readonly Date first = first;
        /// <summary>The step of the sequence.</summary>
        protected readonly int step = step;
        /// <summary>
        /// Maximum value in the sequence, which is the last value rounded down to the step.
        /// </summary>
        private readonly Date min = first.AddMonths(-(length - 1) * step);
        /// <summary>Current value.</summary>
        protected int current = 0;

        /// <summary>
        /// Resets the sequence by setting the next value to <see cref="first"/>.
        /// </summary>
        /// <returns>Echoes this sequence.</returns>
        public sealed override DateSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override Date this[int index] =>
            (uint)index < length ? first.AddMonths(-index * step) : throw new IndexOutOfRangeException();

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override DateSequence this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length());
                return new MonthGridSequence(first.AddMonths(offset * step), step, length);
            }
        }

        /// <summary>Checks if the underlying vector contains the given value.</summary>
        /// <param name="value">Value to locate.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        public override bool Contains(Date value)
        {
            if (value >= min && value <= first)
            {
                var (y1, m1, _) = first;
                var (y2, m2, _) = value;
                int months = (y1 - y2) * 12 + m1 - m2;
                return months % step == 0 && first.AddMonths(-months) == value;
            }
            return false;
        }

        /// <summary>Sorts the content of this sequence.</summary>
        /// <returns>A sorted sequence.</returns>
        public override DateSequence Sort() => first.Day == min.Day
            ? new MonthGridSequence(min, step, length)
            : base.Sort();

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <returns>A sorted sequence.</returns>
        public override DateSequence SortDescending() => this;

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value, or <see cref="Date.Zero"/> when empty.</returns>
        public override Date First() => first;

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value, or <see cref="Date.Zero"/> when empty.</returns>
        public override Date Last() => min;

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override Date Min() => min;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override Date Max() => first;

        /// <summary>Gets only the unique values in this sequence.</summary>
        /// <remarks>This sequence has always unique values.</remarks>
        /// <returns>A sequence with unique values.</returns>
        public sealed override DateSequence Distinct() => this;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
        {
            if (current < length)
            {
                value = first.AddMonths(-current++ * step);
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>Implements a sequence using a vector as its storage.</summary>
    /// <param name="source">The underlying vector.</param>
    private sealed class VectorSequence(DateVector source) : CursorSequence(source.Length)
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
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
        public override Date this[int index] => source[index];

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override DateSequence this[Range range] => new VectorSequence(source[range]);

        /// <summary>Checks if the underlying vector contains the given value.</summary>
        /// <param name="value">Value to locate.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        public override bool Contains(Date value) => source.Contains(value);

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value.</returns>
        public override Date First() => source[0];

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value.</returns>
        public override Date Last() => source[^1];

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override Date Max() => source.Maximum();

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override Date Min() => source.Minimum();

        /// <summary>Checks the sequence has a storage.</summary>
        protected override bool HasStorage => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override Date[] Materialize() => (Date[])source;
    }

    /// <summary>Implements an unfolding sequence using a generator function.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generator function.</param>
    private sealed class Unfolder0(int length, Date seed, Func<Date, Date> unfold) :
        CursorSequence(length)
    {
        private readonly Date seed = seed;
        private Date x = seed;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
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
        public override DateSequence Reset()
        {
            x = seed;
            return base.Reset();
        }
    }

    /// <summary>Implements an unfolding sequence using a generator function.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generator function.</param>
    private sealed class Unfolder1(int length, Date seed, Func<int, Date, Date> unfold) :
        CursorSequence(length)
    {
        private readonly Date seed = seed;
        private Date x = seed;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
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
        public override DateSequence Reset()
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
    private sealed class Unfolder2(int length, Date first, Date second, Func<Date, Date, Date> unfold) :
        CursorSequence(length)
    {
        private readonly Date first = first;
        private readonly Date second = second;
        private Date x = first, y = second;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Date value)
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
        public override DateSequence Reset()
        {
            (x, y) = (first, second);
            return base.Reset();
        }
    }
}
