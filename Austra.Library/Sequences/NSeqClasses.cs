﻿using System.Collections.Generic;

namespace Austra.Library;

/// <summary>Represents any sequence returning integer values.</summary>
public abstract partial class NSequence
{
    /// <summary>Implements a sequence of integers with a known length.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    private abstract class FixLengthSequence(int length): NSequence
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
        protected override int[] Materialize()
        {
            int[] result = GC.AllocateUninitializedArray<int>(length);
            Materialize(result.AsSpan());
            return result;
        }
    }

    /// <summary>Implements a sequence with an integer cursor.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    private abstract class CursorSequence(int length) : FixLengthSequence(length)
    {
        /// <summary>The current index/cursor on the sequence.</summary>
        protected int current;

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public sealed override NSequence Reset()
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
        private readonly int max =  first + ((last - first) / step) * step;
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
            Length() > 1 && (source[0] == 0 || source[^1] == 0);

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
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
        {
            if (current < length)
            {
                seed = unfold(value = seed);
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
    private sealed class Unfolder1(int length, int seed, Func<int, int, int> unfold) :
        CursorSequence(length)
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
        {
            if (current < length)
            {
                seed = unfold(++current, value = seed);
                return true;
            }
            value = default;
            return false;
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
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
        {
            if (current < length)
            {
                value = first;
                second = unfold(value, first = second);
                current++;
                return true;
            }
            value = default;
            return false;
        }
    }
}
