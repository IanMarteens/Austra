﻿namespace Austra.Library;

/// <summary>Represents any sequence returning a double value.</summary>
public abstract partial class CSequence
{
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

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => source.HasLength;
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

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <remarks>
        /// We can calculate the length if both operands has a known length.
        /// </remarks>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() => HasLength ? Min(s1.Length(), s2.Length()) : base.Length();

        /// <summary>Checks if we can get the length without iterating.</summary>
        /// <remarks>
        /// We can calculate the length if both operands has a known length.
        /// </remarks>
        protected override bool HasLength => s1.HasLength && s2.HasLength;
    }

    /// <summary>Implements a sequence using a vector as its storage.</summary>
    /// <param name="source">The underlying vector.</param>
    private sealed class VectorSequence(ComplexVector source) : CSequence
    {
        /// <summary>Current index in the sequence.</summary>
        private int current;

        /// <summary>Creates a sequence of complex numbers from an array of complex numbers.</summary>
        /// <param name="values">An array of complex numbers.</param>
        public VectorSequence(Complex[] values) : this(new ComplexVector(values)) { }

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public override CSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Gets the next number in the sequence.</summary>
        /// <param name="value">The next number in the sequence.</param>
        /// <returns><see langword="true"/>, when there is a next number.</returns>
        public override bool Next(out Complex value)
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
        public override Complex this[int index] => source[index];

        /// <summary>Gets the value at the specified index.</summary>
        /// <param name="idx">A position inside the sequence.</param>
        /// <returns>The value at the given position.</returns>
        public override Complex this[Index idx] => source[idx];

        /// <summary>Gets a range from the sequence.</summary>
        /// <param name="range">A range inside the sequence.</param>
        /// <returns>The sequence for the given range.</returns>
        public override CSequence this[Range range] => new VectorSequence(source[range]);

        /// <summary>Gets the first value in the sequence.</summary>
        /// <returns>The first value, or <see cref="double.NaN"/> when empty.</returns>
        public override Complex First() => source[0];

        /// <summary>Gets the last value in the sequence.</summary>
        /// <returns>The last value, or <see cref="double.NaN"/> when empty.</returns>
        public override Complex Last() => source[^1];

        /// <summary>Gets the sum of all the values in the sequence.</summary>
        /// <returns>The sum of all the values in the sequence.</returns>
        public override Complex Sum() => source.Sum();

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public override int Length() => source.Length;

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected override bool HasLength => true;

        /// <summary>Checks the sequence has a storage.</summary>
        protected override bool HasStorage => true;

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected override Complex[] Materialize() => (Complex[])source;
    }

    /// <summary>
    /// Implements a sequence of complex values based in a generator function.
    /// </summary>
    /// <param name="length">Number of items in the sequence.</param>
    private abstract class GenerativeSequence(int length) : CSequence
    {
        /// <summary>The length of the sequence.</summary>
        protected readonly int length = length;
        /// <summary>The current index in the sequence.</summary>
        protected int current;

        /// <summary>Gets the total number of values in the sequence.</summary>
        /// <returns>The total number of values in the sequence.</returns>
        public sealed override int Length() => length;

        /// <summary>Checks if we can get the length without iterating.</summary>
        protected sealed override bool HasLength => true;

        /// <summary>Resets the sequence.</summary>
        /// <returns>Echoes this sequence.</returns>
        public sealed override CSequence Reset()
        {
            current = 0;
            return this;
        }

        /// <summary>Creates an array with all values from the sequence.</summary>
        /// <returns>The values as an array.</returns>
        protected sealed override Complex[] Materialize()
        {
            Complex[] result = GC.AllocateUninitializedArray<Complex>(length);
            Materialize(result.AsSpan());
            return result;
        }
    }

    /// <summary>Implements a sequence using random values.</summary>
    /// <param name="length">Size of the sequence.</param>
    /// <param name="random">Random generator.</param>
    private sealed class RandomSequence(int length, Random random) : GenerativeSequence(length)
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
        GenerativeSequence(length)
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
}