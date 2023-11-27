﻿namespace Austra.Library;

/// <summary>Common base class for all sequences.</summary>
/// <typeparam name="T">The type for the returned items.</typeparam>
/// <typeparam name="TSelf">The covariant type of the sequence.</typeparam>
public abstract class Sequence<T, TSelf>
    where TSelf : Sequence<T, TSelf>
    where T :
        unmanaged,
        IAdditionOperators<T, T, T>,
        IAdditiveIdentity<T, T>,
        ISubtractionOperators<T, T, T>,
        IMultiplyOperators<T, T, T>,
        IMultiplicativeIdentity<T, T>,
        IDivisionOperators<T, T, T>
{
    /// <summary>Gets the next item in the sequence.</summary>
    /// <param name="value">The next item in the sequence.</param>
    /// <returns><see langword="true"/>, when there is a next item.</returns>
    public abstract bool Next(out T value);

    /// <summary>Resets the sequence.</summary>
    /// <returns>Echoes this sequence.</returns>
    public abstract TSelf Reset();

    /// <summary>Performs a shallow copy of the sequence.</summary>
    /// <returns>A shallow copy of the sequence.</returns>
    public TSelf Clone() => (TSelf)MemberwiseClone();

    /// <summary>Gets the value at the specified index.</summary>
    /// <param name="index">A position inside the sequence.</param>
    /// <returns>The value at the given position.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// When <paramref name="index"/> is out of range.
    /// </exception>
    public virtual T this[int index]
    {
        get
        {
            while (index-- > 0)
                if (!Next(out _))
                    throw new IndexOutOfRangeException();
            if (!Next(out T value))
                throw new IndexOutOfRangeException();
            return value;
        }
    }

    /// <summary>Gets the value at the specified index.</summary>
    /// <param name="idx">A position inside the sequence.</param>
    /// <returns>The value at the given position.</returns>
    public abstract T this[Index idx] { get; }

    /// <summary>Gets a range from the sequence.</summary>
    /// <param name="range">A range inside the sequence.</param>
    /// <returns>The sequence for the given range.</returns>
    public abstract TSelf this[Range range] { get; }

    /// <summary>Checks whether the predicate is satisfied by all items.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if all items satisfy the predicate.</returns>
    public bool All(Func<T, bool> predicate)
    {
        while (Next(out T value))
            if (!predicate(value))
                return false;
        return true;
    }

    /// <summary>Checks whether the predicate is satisfied by at least one item.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if there exists a item satisfying the predicate.</returns>
    public bool Any(Func<T, bool> predicate)
    {
        while (Next(out T value))
            if (predicate(value))
                return true;
        return false;
    }

    /// <summary>Gets only the unique values in this sequence.</summary>
    /// <returns>A sequence with unique values.</returns>
    public abstract TSelf Distinct();

    /// <summary>Transform a sequence acording to the predicate passed as parameter.</summary>
    /// <param name="filter">A predicate for selecting surviving values</param>
    /// <returns>The filtered sequence.</returns>
    public abstract TSelf Filter(Func<T, bool> filter);

    /// <summary>Transform a sequence acording to the function passed as parameter.</summary>
    /// <param name="mapper">The transforming function.</param>
    /// <returns>The transformed sequence.</returns>
    public abstract TSelf Map(Func<T, T> mapper);

    /// <summary>Joins the common part of two sequence with the help of a lambda.</summary>
    /// <param name="other">The second sequence.</param>
    /// <param name="zipper">The joining sequence.</param>
    /// <returns>The combined sequence.</returns>
    public abstract TSelf Zip(TSelf other, Func<T, T, T> zipper);

    /// <summary>Gets the sum of all the values in the sequence.</summary>
    /// <returns>The sum of all the values in the sequence.</returns>
    public virtual T Sum()
    {
        T total = T.AdditiveIdentity;
        while (Next(out T value))
            total += value;
        return total;
    }

    /// <summary>Gets the product of all the values in the sequence.</summary>
    /// <returns>The product of all the values in the sequence.</returns>
    public virtual T Product()
    {
        T product = T.MultiplicativeIdentity;
        while (Next(out T value))
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

    /// <summary>Gets the first value in the sequence.</summary>
    /// <returns>The first value, or a special value, when empty.</returns>
    public abstract T First();

    /// <summary>Gets the last value in the sequence.</summary>
    /// <returns>The last value, or a special value, when empty.</returns>
    public abstract T Last();

    /// <summary>Item by item multiplication of two sequences.</summary>
    /// <param name="other">The second sequence.</param>
    /// <returns>A sequence with all the multiplication results.</returns>
    public abstract TSelf PointwiseMultiply(TSelf other);

    /// <summary>Item by item division of sequences.</summary>
    /// <param name="other">The second sequence.</param>
    /// <returns>A sequence with all the quotient results.</returns>
    public abstract TSelf PointwiseDivide(TSelf other);

    /// <summary>Reduces a sequence to a single number.</summary>
    /// <param name="seed">The seed value.</param>
    /// <param name="reducer">A function that combines two elements into one.</param>
    /// <returns>The reduced values.</returns>
    public virtual T Reduce(T seed, Func<T, T, T> reducer)
    {
        while (Next(out T value))
            seed = reducer(seed, value);
        return seed;
    }

    /// <summary>Checks if we can get the length without iterating.</summary>
    protected virtual bool HasLength => false;

    /// <summary>Checks if the sequence has a storage.</summary>
    protected virtual bool HasStorage => false;

    /// <summary>Creates an array with all values from the sequence.</summary>
    /// <returns>The values as an array.</returns>
    protected abstract T[] Materialize();

    /// <summary>Fills a span with all values from the sequence.</summary>
    /// <param name="span">The span to fill.</param>
    protected void Materialize(Span<T> span)
    {
        for (ref T d = ref MM.GetReference(span); Next(out T v); d = ref Add(ref d, 1))
            d = v;
    }
}
