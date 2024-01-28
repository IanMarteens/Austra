namespace Austra.Library;

/// <summary>Represents any sequence returning integer values.</summary>
public abstract partial class NSequence
{
    /// <summary>Implements a sequence of integers with a known length.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    private abstract class FixLengthSequence(int length) : NSequence
    {
        /// <summary>The length of the sequence.</summary>
        protected readonly int length = length;

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public sealed override int this[Index idx] => this[idx.GetOffset(length)];

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public sealed override int Length() => length;

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected sealed override bool HasLength => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override int[] Materialize() => Materialize(length);
    }

    /// <summary>Implements a sequence with an integer cursor.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    private abstract class CursorSequence(int length) : FixLengthSequence(length)
    {
        /// <summary>The current index/cursor on the sequence.</summary>
        protected int current;

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
        {
            current = 0;
            return this;
        }
    }

    /// <summary>Implements a sequence transformed by a mapper lambda.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="mapper">The mapping function.</param>
    private sealed class Mapped(NSequence source, Func<int, int> mapper) : NSequence
    {
        /// <summary>The mapping function.</summary>
        private readonly Func<int, int> mapper = mapper;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
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
        public override NSequence Map(Func<int, int> mapper) =>
            new Mapped(source, x => mapper(this.mapper(x)));

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => source.HasLength;

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() =>
            source.HasLength ? source.Length() : base.Length();

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Implements a sequence transformed by a mapper lambda.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="mapper">The mapping function.</param>
    private sealed class RealMapped(NSequence source, Func<int, double> mapper) : DSequence
    {
        private readonly Func<int, double> mapper = mapper;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (source.Next(out int intValue))
            {
                value = mapper(intValue);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => source.HasLength;

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
    }

    /// <summary>Implements a sequence filtered by a predicate.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="filter">The filtering lambda.</param>
    private sealed class Filtered(NSequence source, Func<int, bool> filter) : NSequence
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
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
        public override NSequence Map(Func<int, int> mapper) =>
            new FilteredMapped(source, filter, mapper);

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
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
        NSequence source,
        Func<int, bool> filter,
        Func<int, int> mapper) : NSequence
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
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
        public override NSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Joins the common part of two sequences with the help of a lambda.</summary>
    /// <param name="s1">First sequence.</param>
    /// <param name="s2">Second sequence.</param>
    /// <param name="zipper">The joining function.</param>
    private sealed class Zipped(NSequence s1, NSequence s2,
        Func<int, int, int> zipper) : NSequence
    {
        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
        {
            while (s1.Next(out int value1) && s2.Next(out int value2))
            {
                value = zipper(value1, value2);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
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
    private class SeqWhile(NSequence source, Func<int, bool> condition) : NSequence
    {
        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
        {
            if (source.Next(out value) && condition(value))
                return true;
            value = default;
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Returns a sequence until a condition is met.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="condition">The condition that must be met.</param>
    private class SeqUntil(NSequence source, Func<int, bool> condition) : NSequence
    {
        private bool done;

        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
        {
            if (!done && source.Next(out value))
            {   done = condition(value);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
        {
            done = false;
            source.Reset();
            return this;
        }
    }

    /// <summary>Returns a sequence until a condition is met.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="sentinel">The value to stop iterating.</param>
    private class SeqUntilValue(NSequence source, int sentinel) : NSequence
    {
        private bool done;

        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
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
        public override NSequence Reset()
        {
            done = false;
            source.Reset();
            return this;
        }
    }

    /// <summary>Implements a sequence of integers based in an range and a step.</summary>
    /// <remarks><c>first &lt;= last</c></remarks>
    /// <param name="first">First value in the sequence.</param>
    /// <param name="step">Distance between sequence values.</param>
    /// <param name="last">Upper bound of the sequence. It may be rounded down.</param>
    private class GridSequence(int first, int step, int last) :
        FixLengthSequence(Abs(last - first) / step + 1)
    {
        /// <summary>The first value in the sequence.</summary>
        protected readonly int first = first;
        /// <summary>The last value in the sequence.</summary>
        protected readonly int last = last;
        /// <summary>The step of the sequence.</summary>
        protected readonly int step = step;
        /// <summary>
        /// Maximum value in the sequence, which is the last value rounded down to the step.
        /// </summary>
        private readonly int max = first + ((last - first) / step) * step;
        /// <summary>Current value.</summary>
        protected int current = first;

        /// <summary>
        /// Resets the sequence by setting the next value to <see cref="first"/>.
        /// </summary>
        /// <returns>Echoes this sequence.</returns>
        public sealed override NSequence Reset()
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
        public override int this[int index] =>
            (uint)index < length ? first + index * step : throw new IndexOutOfRangeException();

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override NSequence this[Range range]
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
        public override bool Contains(int value) =>
            value <= max && value >= first && (value - first) % step == 0;

        /// <summary>Checks if the sequence contains a zero value.</summary>
        protected override bool ContainsZero => first < 0 && max >= 0 && (-first) % step == 0;

        /// <summary>Shifts a sequence without an underlying storage.</summary>
        /// <param name="d">Amount to shift.</param>
        /// <returns>The shifted sequence.</returns>
        protected override NSequence Shift(int d) =>
            new GridSequence(first + d, step, max + d);

        /// <summary>Negates a sequence without an underlying storage.</summary>
        /// <returns>The negated sequence.</returns>
        protected override NSequence Negate() => new GridSequenceDesc(-first, step, -max);

        /// <summary>Scales a sequence without an underlying storage.</summary>
        /// <param name="d">The scalar multiplier.</param>
        /// <returns>The scaled sequence.</returns>
        protected override NSequence Scale(int d) => d >= 0
            ? new GridSequence(first * d, step * d, max * d)
            : new GridSequenceDesc(first * d, -step * d, max * d);

        /// <summary>Sorts the content of this sequence.</summary>
        /// <returns>A sorted sequence.</returns>
        public override NSequence Sort() => this;

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <returns>A sorted sequence.</returns>
        public override NSequence SortDescending() => new GridSequenceDesc(max, step, first);

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value, or <see cref="double.NaN"/> when empty.</returns>
        public override int First() => first;

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value, or <see cref="double.NaN"/> when empty.</returns>
        public override int Last() => max;

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override int Min() => first;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override int Max() => max;

        /// <summary>Gets the sum of all the values in the sequence.</summary>
        /// <returns>The sum of all the values in the sequence.</returns>
        public override int Sum() => (length * first + step * length * (length - 1) / 2);

        /// <summary>Adds a sequence to this sequence.</summary>
        /// <param name="other">Sequence to add.</param>
        /// <returns>The component by component sum of the sequences.</returns>
        protected override NSequence Add(NSequence other) =>
            other is GridSequence gs and not GridSequenceDesc
            ? new GridSequence(first + gs.first, step + gs.step,
                length < gs.length ? max + gs[length - 1] : this[gs.length - 1] + gs.max)
            : Zip(other, (x, y) => x + y);

        /// <summary>Gets only the unique values in this sequence.</summary>
        /// <remarks>This sequence has always unique values.</remarks>
        /// <returns>A sequence with unique values.</returns>
        public sealed override NSequence Distinct() => this;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
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
    private sealed class GridSequenceDesc(int first, int step, int last) :
        GridSequence(first, step, last)
    {
        /// <summary>
        /// Last actual value in the sequence, which is the last value rounded up to the step.
        /// </summary>
        private readonly int min = first - step * (Abs(last - first) / step);

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override int this[int index] =>
            (uint)index < length ? first - index * step : throw new IndexOutOfRangeException();

        /// <summary>Checks if the underlying vector contains the given value.</summary>
        /// <param name="value">Value to locate.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        public override bool Contains(int value) =>
            value >= min && value <= first && (value - min) % step == 0;

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override NSequence this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length());
                return new GridSequenceDesc(
                    first - offset * step, step, first - (offset + length - 1) * step);
            }
        }

        /// <summary>Checks if the sequence contains a zero value.</summary>
        protected override bool ContainsZero => first >= 0 && min <= 0 && (-min) % step == 0;

        /// <summary>Shifts a sequence without an underlying storage.</summary>
        /// <param name="d">Amount to shift.</param>
        /// <returns>The shifted sequence.</returns>
        protected override NSequence Shift(int d) =>
            new GridSequenceDesc(first + d, step, last + d);

        /// <summary>Negates a sequence without an underlying storage.</summary>
        /// <returns>The negated sequence.</returns>
        protected override NSequence Negate() => new GridSequence(-first, step, -min);

        /// <summary>Scales a sequence without an underlying storage.</summary>
        /// <param name="d">The scalar multiplier.</param>
        /// <returns>The scaled sequence.</returns>
        protected override NSequence Scale(int d) => d >= 0
            ? new GridSequenceDesc(first * d, step * d, min * d)
            : new GridSequence(first * d, -step * d, min * d);

        /// <summary>Sorts the content of this sequence.</summary>
        /// <returns>A sorted sequence.</returns>
        public override NSequence Sort() => new GridSequence(min, step, first);

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <returns>A sorted sequence.</returns>
        public override NSequence SortDescending() => this;

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override int Min() => min;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override int Max() => first;

        /// <summary>Gets the sum of all the values in the sequence.</summary>
        /// <returns>The sum of all the values in the sequence.</returns>
        public override int Sum() => (length * min + step * length * (length - 1) / 2);

        /// <summary>Adds a sequence to this sequence.</summary>
        /// <param name="other">Sequence to add.</param>
        /// <returns>The component by component sum of the sequences.</returns>
        protected override NSequence Add(NSequence other) =>
            other is GridSequenceDesc gs
            ? new GridSequenceDesc(first + gs.first, step + gs.step,
                length < gs.length ? min + gs[length - 1] : this[gs.length - 1] + gs.min)
            : Zip(other, (x, y) => x + y);

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
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

    /// <summary>Implements a sequence using a vector as its storage.</summary>
    /// <param name="source">The underlying vector.</param>
    private sealed class VectorSequence(NVector source) : CursorSequence(source.Length)
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
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
        public override int this[int index] => source[index];

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override NSequence this[Range range] => new VectorSequence(source[range]);

        /// <summary>Checks if the underlying vector contains the given value.</summary>
        /// <param name="value">Value to locate.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        public override bool Contains(int value) => source.Contains(value);

        /// <summary>Gets all statistics from the values in the secuence.</summary>
        /// <returns>Simple statistics of all the values in the sequence.</returns>
        public override Accumulator Stats() => source.Stats();

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value.</returns>
        public override int First() => source[0];

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value.</returns>
        public override int Last() => source[^1];

        /// <summary>Checks if the sequence contains a zero value.</summary>
        /// <remarks>
        /// This is a fast check, and we try it to be sure.
        /// Of course, a zero could be anywhere in the sequence.
        /// </remarks>
        protected override bool ContainsZero =>
            length > 1 && (source.UnsafeThis(0) == 0 || source.UnsafeThis(length - 1) == 0);

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override int Min() => source.Minimum();

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override int Max() => source.Maximum();

        /// <summary>Gets the sum of all the values in the sequence.</summary>
        /// <returns>The sum of all the values in the sequence.</returns>
        public override int Sum() => source.Sum();

        /// <summary>Gets the product of all the values in the sequence.</summary>
        /// <returns>The product of all the values in the sequence.</returns>
        public override int Product() => source.Product();

        /// <summary>Checks the sequence has a storage.</summary>
        protected override bool HasStorage => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override int[] Materialize() => (int[])source;
    }

    /// <summary>Implements a sequence using random values.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="lo">Lower bound of the random values.</param>
    /// <param name="hi">Upper bound of the random values.</param>
    /// <param name="random">Random generator.</param>
    private sealed class RandomSequence(int length, int lo, int hi, Random random) :
        CursorSequence(length)
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
        {
            if (current < length)
            {
                value = random.Next(lo, hi);
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
    private sealed class Unfolder0(int length, int seed, Func<int, int> unfold) :
        CursorSequence(length)
    {
        private readonly int seed = seed;
        private int x = seed;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
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
        public override NSequence Reset()
        {
            x = seed;
            return base.Reset();
        }
    }

    /// <summary>Implements an unfolding sequence using a generator function.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generator function.</param>
    private sealed class Unfolder1(int length, int seed, Func<int, int, int> unfold) :
        CursorSequence(length)
    {
        private readonly int seed = seed;
        private int x = seed;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
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
        public override NSequence Reset()
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
    private sealed class Unfolder2(int length, int first, int second, Func<int, int, int> unfold) :
        CursorSequence(length)
    {
        private readonly int first = first;
        private readonly int second = second;
        private int x = first, y = second;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
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
        public override NSequence Reset()
        {
            (x, y) = (first, second);
            return base.Reset();
        }
    }

    /// <summary>An integer sequence with indexes in a vector for a given value.</summary>
    /// <remarks>This is a trivial wrapper for <see cref="DVector.IndexOf(double, int)"/></remarks>
    /// <param name="vector">Vector to search.</param>
    /// <param name="v">Value to be searched in the vector.</param>
    private sealed class IndexFinder(DVector vector, double v) : NSequence
    {
        /// <summary>The current index in the vector.</summary>
        private int current;

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the next index in the sequence.</summary>
        /// <param name="value">The next index in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next index.</returns>
        public override bool Next(out int value)
        {
            if (current >= 0 && (current = vector.IndexOf(v, current)) >= 0)
            {
                value = current;
                current++;
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>An integer sequence with indexes in a vector for a given value.</summary>
    /// <param name="vector">Vector to search.</param>
    /// <param name="condition">A predicate on the value of a vector's item.</param>
    private sealed class IndexFinderWithLambda(DVector vector, Func<double, bool> condition) : NSequence
    {
        /// <summary>The current index in the vector.</summary>
        private int current;

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the next index in the sequence.</summary>
        /// <param name="value">The next index in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next index.</returns>
        public override bool Next(out int value)
        {
            if (current >= 0)
                for (; current < vector.Length; current++)
                    if (condition(vector.UnsafeThis(current)))
                    {
                        value = current;
                        current++;
                        return true;
                    }
            current = -1;
            value = default;
            return false;
        }
    }

    /// <summary>An integer sequence with indexes in a vector for a given value.</summary>
    /// <remarks>This is a trivial wrapper for <see cref="CVector.IndexOf(Complex, int)"/></remarks>
    /// <param name="vector">Vector to search.</param>
    /// <param name="v">Value to be searched in the vector.</param>
    private sealed class CIndexFinder(CVector vector, Complex v) : NSequence
    {
        /// <summary>The current index in the vector.</summary>
        private int current;

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the next index in the sequence.</summary>
        /// <param name="value">The next index in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next index.</returns>
        public override bool Next(out int value)
        {
            if (current >= 0 && (current = vector.IndexOf(v, current)) >= 0)
            {
                value = current;
                current++;
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>An integer sequence with indexes in a vector for a given value.</summary>
    /// <param name="vector">Vector to search.</param>
    /// <param name="condition">A predicate on the value of a vector's item.</param>
    private sealed class CIndexFinderWithLambda(CVector vector, Func<Complex, bool> condition) : NSequence
    {
        /// <summary>The current index in the vector.</summary>
        private int current;

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the next index in the sequence.</summary>
        /// <param name="value">The next index in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next index.</returns>
        public override bool Next(out int value)
        {
            if (current >= 0)
                for (; current < vector.Length; current++)
                    if (condition(vector[current]))
                    {
                        value = current;
                        current++;
                        return true;
                    }
            current = -1;
            value = default;
            return false;
        }
    }
}
