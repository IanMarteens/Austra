﻿namespace Austra.Library;

/// <summary>Represents any sequence returning integer values.</summary>
public abstract partial class NSequence
{
    /// <summary>Implements a sequence transformed by a mapper lambda.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="mapper">The mapping function.</param>
    private sealed class Mapped(NSequence source, Func<int, int> mapper) : NSequence
    {
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

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => source.HasLength;
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
    private class RangeSequence(int first, int last) : NSequence
    {
        /// <summary>Calculated length of the sequence.</summary>
        protected readonly int length = Abs(last - first) + 1;
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

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override int this[int index] =>
            (uint)index < length ? first + index : throw new IndexOutOfRangeException();

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public override int this[Index idx] => idx.IsFromEnd
            ? new RangeSequenceDesc(last, first)[idx.Value - 1]
            : this[idx.Value];

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override NSequence this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length());
                return new RangeSequence(first + offset, first + (offset + length - 1));
            }
        }

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected sealed override int[] Materialize()
        {
            int[] result = GC.AllocateUninitializedArray<int>(length);
            Materialize(result.AsSpan());
            return result;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
        {
            current = first;
            return this;
        }

        /// <summary>Negates a sequence without an underlying storage.</summary>
        /// <returns>The negated sequence.</returns>
        protected override NSequence Negate() => new RangeSequenceDesc(-first, -last);

        /// <summary>Sorts the content of this sequence.</summary>
        /// <returns>A sorted sequence.</returns>
        public override NSequence Sort() => this;

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <returns>A sorted sequence in descending order.</returns>
        public override NSequence SortDescending() => new RangeSequenceDesc(last, first);

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value, or <see cref="double.NaN"/> when empty.</returns>
        public override int First() => first;

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value, or <see cref="double.NaN"/> when empty.</returns>
        public override int Last() => last;

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override int Min() => first;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override int Max() => last;

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
        public override NSequence Sort() => new RangeSequence(last, first);

        /// <summary>Sorts the content of this sequence in descending order.</summary>
        /// <returns>A sorted sequence in descending order.</returns>
        public override NSequence SortDescending() => this;

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override int this[int index] =>
            (uint)index < length ? first - index : throw new IndexOutOfRangeException();

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public override int this[Index idx] => idx.IsFromEnd
            ? new RangeSequence(last, first)[idx.Value - 1]
            : this[idx.Value];

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override NSequence this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length());
                return new RangeSequenceDesc(
                    first - offset, first - (offset + length - 1));
            }
        }

        /// <summary>Gets the minimum value from the sequence.</summary>
        /// <returns>The minimum value.</returns>
        public override int Min() => last;

        /// <summary>Gets the maximum value from the sequence.</summary>
        /// <returns>The maximum value.</returns>
        public override int Max() => first;

        /// <summary>Negates a sequence without an underlying storage.</summary>
        /// <returns>The negated sequence.</returns>
        protected override NSequence Negate() => new RangeSequence(-first, -last);

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
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

    /// <summary>Implements a sequence using a vector as its storage.</summary>
    /// <param name="source">The underlying vector.</param>
    private sealed class VectorSequence(NVector source) : NSequence
    {
        /// <summary>Current index in the sequence.</summary>
        private int current;

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override NSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out int value)
        {
            if (current < source.Length)
            {
                value = source[current++];
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

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public override int this[Index idx] => source[idx];

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

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() => source.Length;

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => true;

        /// <summary>Checks the sequence has a storage.</summary>
        protected override bool HasStorage => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override int[] Materialize() => (int[])source;
    }
}