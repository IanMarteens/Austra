namespace Austra.Library;

/// <summary>Represents any sequence returning complex values.</summary>
public abstract partial class CSequence
{
    /// <summary>Implements a sequence of complex values based with a known length.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    private abstract class FixLengthSequence(int length) : CSequence
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
        protected override Complex[] Materialize() => Materialize(length);
    }

    /// <summary>A sequence with an integer cursor.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    private abstract class CursorSequence(int length) : FixLengthSequence(length)
    {
        /// <summary>The current index in the sequence.</summary>
        protected int current;

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override CSequence Reset()
        {
            current = 0;
            return this;
        }
    }

    /// <summary>A fixed length sequence that repeats the same value a number of times.</summary>
    /// <param name="length">Number of items in the sequence.</param>
    /// <param name="value">The repeated value.</param>
    private sealed class RepeatSequence(int length, Complex value) : CursorSequence(length)
    {
        /// <summary>The value to repeat.</summary>
        private readonly Complex value = value;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
        {
            if (current++ < length)
            {
                value = this.value;
                return true;
            }
            value = default;
            return false;
        }

        public override Complex this[int idx] =>
            (uint)idx < length ? value : throw new IndexOutOfRangeException();

        public override bool Contains(Complex value) => value == this.value;

        public override Complex First() => value;
        public override Complex Last() => value;
        public override Complex Sum() => length * value;
        public override Complex Product() => Complex.Pow(value, length);
        public override CSequence Distinct() => this;

        protected override bool ContainsZero => value == Complex.Zero;
        protected override CSequence Negate() => new RepeatSequence(length, -value);
        protected override CSequence Scale(Complex d) =>
            new RepeatSequence(length, value * d);
        protected override CSequence Shift(Complex d) =>
            new RepeatSequence(length, value + d);
    }
    /// <summary>Implements a sequence transformed by a mapper lambda.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="mapper">The mapping function.</param>
    private sealed class Mapped(CSequence source, Func<Complex, Complex> mapper) : CSequence
    {
        private readonly Func<Complex, Complex> mapper = mapper;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
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
        public override CSequence Map(Func<Complex, Complex> mapper) =>
            new Mapped(source, x => mapper(this.mapper(x)));

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => source.HasLength;

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() =>
            source.HasLength ? source.Length() : base.Length();

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override CSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Implements a sequence transformed by a mapper lambda.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="mapper">The mapping function.</param>
    private sealed class RealMapped(CSequence source, Func<Complex, double> mapper) : DSequence
    {
        private readonly Func<Complex, double> mapper = mapper;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out double value)
        {
            if (source.Next(out Complex cValue))
            {
                value = mapper(cValue);
                return true;
            }
            value = 0d;
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
    private sealed class Filtered(CSequence source, Func<Complex, bool> filter) : CSequence
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
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
        public override CSequence Map(Func<Complex, Complex> mapper) =>
            new FilteredMapped(source, filter, mapper);

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override CSequence Reset()
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
        CSequence source,
        Func<Complex, bool> filter,
        Func<Complex, Complex> mapper) : CSequence
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
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
        public override CSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Joins the common part of two sequences with the help of a lambda.</summary>
    /// <param name="s1">First sequence.</param>
    /// <param name="s2">Second sequence.</param>
    /// <param name="zipper">The joining function.</param>
    private sealed class Zipped(CSequence s1, CSequence s2,
        Func<Complex, Complex, Complex> zipper) : CSequence
    {
        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
        {
            while (s1.Next(out Complex value1) && s2.Next(out Complex value2))
            {
                value = zipper(value1, value2);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override CSequence Reset()
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
            HasLength ? Min(s1.Length(), s2.Length()) : base.Length();
    }

    /// <summary>Returns a sequence while a condition is met.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="condition">The condition that must be met.</param>
    private class SeqWhile(CSequence source, Func<Complex, bool> condition) : CSequence
    {
        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
        {
            if (source.Next(out value) && condition(value))
                return true;
            value = default;
            return false;
        }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override CSequence Reset()
        {
            source.Reset();
            return this;
        }
    }

    /// <summary>Returns a sequence until a condition is met.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="sentinel">The value to stop iterating.</param>
    private class SeqUntilValue(CSequence source, Complex sentinel) : CSequence
    {
        private bool done;

        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
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
        public override CSequence Reset()
        {
            done = false;
            source.Reset();
            return this;
        }
    }

    /// <summary>Returns a sequence until a condition is met.</summary>
    /// <param name="source">The original sequence.</param>
    /// <param name="condition">The condition that must be met.</param>
    private class SeqUntil(CSequence source, Func<Complex, bool> condition) : CSequence
    {
        private bool done;

        /// <summary>Gets the next number in the computed sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
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
        public override CSequence Reset()
        {
            done = false;
            source.Reset();
            return this;
        }
    }

    /// <summary>Implements a sequence of double values based in a grid.</summary>
    /// <remarks>Creates a double sequence from a grid.</remarks>
    /// <param name="lower">The first value in the sequence.</param>
    /// <param name="upper">The last value in the sequence.</param>
    /// <param name="steps">The number of steps in the sequence, minus one.</param>
    private sealed class GridSequence(Complex lower, Complex upper, int steps) :
        CursorSequence(steps + 1)
    {
        /// <summary>The distance between two steps.</summary>
        private readonly Complex delta = (upper - lower) / steps;

        /// <summary>Shifts a sequence without an underlying storage.</summary>
        /// <param name="d">Amount to shift.</param>
        /// <returns>The shifted sequence.</returns>
        protected override CSequence Shift(Complex d) =>
            new GridSequence(lower + d, upper + d, steps);

        /// <summary>Negates a sequence without an underlying storage.</summary>
        /// <returns>The negated sequence.</returns>
        protected override CSequence Negate() => new GridSequence(-lower, -upper, steps);

        /// <summary>Scales a sequence without an underlying storage.</summary>
        /// <param name="d">The scalar multiplier.</param>
        /// <returns>The scaled sequence.</returns>
        protected override CSequence Scale(Complex d) =>
            new GridSequence(lower * d, upper * d, steps);

        /// <summary>Gets only the unique values in this sequence.</summary>
        /// <remarks>This sequence has always unique values.</remarks>
        /// <returns>A sequence with unique values.</returns>
        public override CSequence Distinct() => this;

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="index">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// When <paramref name="index"/> is out of range.
        /// </exception>
        public override Complex this[int index] =>
            (uint)index < Length() ? lower + index * delta : throw new IndexOutOfRangeException();

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public override Complex this[Index idx]
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
        public override CSequence this[Range range]
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
        public override Complex First() => lower;

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value, or <see cref="double.NaN"/> when empty.</returns>
        public override Complex Last() => upper;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
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
    private sealed class VectorSequence(CVector source) : CursorSequence(source.Length)
    {
        /// <summary>Creates a sequence of complex numbers from an array of complex numbers.</summary>
        /// <param name="values">An array of complex numbers.</param>
        public VectorSequence(Complex[] values) : this(new CVector(values)) { }

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
        {
            if (current < length)
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
        public override Complex this[int index] => source[index];

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public override Complex this[Index idx] => source[idx];

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override CSequence this[Range range] => new VectorSequence(source[range]);

        /// <summary>Checks if the underlying vector contains the given value.</summary>
        /// <param name="value">Value to locate.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        public override bool Contains(Complex value) => source.Contains(value);

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value, or <see cref="double.NaN"/> when empty.</returns>
        public override Complex First() => source[0];

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value, or <see cref="double.NaN"/> when empty.</returns>
        public override Complex Last() => source[^1];

        /// <summary>Checks if the sequence contains a zero value.</summary>
        /// <remarks>
        /// This is a fast check, and we try it to be sure.
        /// Of course, a zero could be anywhere in the sequence.
        /// </remarks>
        protected override bool ContainsZero =>
            Length() > 1 && (source[0] == 0d || source[^1] == 0d);

        /// <summary>Gets the sum of all the values in the sequence.</summary>
        /// <returns>The sum of all the values in the sequence.</returns>
        public override Complex Sum() => source.Sum();

        /// <summary>Gets the product of all the values in the sequence.</summary>
        /// <returns>The product of all the values in the sequence.</returns>
        public override Complex Product() => ContainsZero ? Complex.Zero : source.Product();

        /// <summary>Checks the sequence has a storage.</summary>
        protected override bool HasStorage => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override Complex[] Materialize() => (Complex[])source;
    }

    /// <summary>Implements a sequence using random values.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="random">Random generator.</param>
    private sealed class RandomSequence(int length, Random random) : CursorSequence(length)
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
        {
            if (current < length)
            {
                value = new(random.NextDouble(), random.NextDouble());
                current++;
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>Implements a sequence using normal random values.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="random">Random generator.</param>
    private sealed class NormalRandomSequence(int length, NormalRandom random) :
        CursorSequence(length)
    {
        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
        {
            if (current < length)
            {
                var (re, im) = random.NextDoubles();
                value = new(re, im);
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
    private sealed class Unfolder0(int length, Complex seed, Func<Complex, Complex> unfold) :
        CursorSequence(length)
    {
        private readonly Complex seed = seed;
        private Complex x = seed;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
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
        public override CSequence Reset()
        {
            x = seed;
            return base.Reset();
        }
    }

    /// <summary>Implements an unfolding sequence using a generator function.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="seed">First value in the sequence.</param>
    /// <param name="unfold">The generator function.</param>
    private sealed class Unfolder1(int length, Complex seed, Func<int, Complex, Complex> unfold) :
        CursorSequence(length)
    {
        private readonly Complex seed = seed;
        private Complex x = seed;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
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
        public override CSequence Reset()
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
    private sealed class Unfolder2(int length, Complex first, Complex second,
        Func<Complex, Complex, Complex> unfold) : CursorSequence(length)
    {
        private readonly Complex first = first;
        private readonly Complex second = second;
        private Complex x = first, y = second;

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
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
        public override CSequence Reset()
        {
            (x, y) = (first, second);
            return base.Reset();
        }
    }
}