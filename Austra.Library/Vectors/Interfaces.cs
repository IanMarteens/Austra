namespace Austra.Library;

/// <summary>Common interface for all vector types.</summary>
public interface IVector
{
}

/// <summary>Common interface for all numeric vector types.</summary>
public interface INumericVector : IVector
{  
}

/// <summary>Common interface for all matrix types.</summary>
public interface IMatrix
{
}

/// <summary>Common interface for all types with indexes like a vector.</summary>
public interface IIndexable
{
}

/// <summary>
/// Common interface for all types that can be used as containers, such as vectors and matrices.
/// </summary>
/// <typeparam name="T">The type of elements in the container.</typeparam>
public interface IContainer<T>
{
    /// <summary>Determines whether the container contains a specific element.</summary>
    /// <param name="item">The element to locate in the container.</param>
    /// <returns><see langword="true"/> if the element is found; otherwise, <see langword="false"/>.</returns>
    bool Contains(T item);
}

/// <summary>Defines a type with a pointwise multiplication operation.</summary>
/// <typeparam name="T">The type that implements this interface.</typeparam>
public interface IPointwiseOperators<T>
{
    /// <summary>Item by item multiplication of two data structures.</summary>
    /// <param name="other">The second operand.</param>
    /// <returns>A new data structure with all the multiplication results.</returns>
    T PointwiseMultiply(T other);

    /// <summary>Item by item division of two data structures.</summary>
    /// <param name="other">The second operand.</param>
    /// <returns>A new data structure with all the quotient results.</returns>
    T PointwiseDivide(T other);
}

/// <summary>Defines a type with a SafeIndex method.</summary>
public interface ISafeIndexed
{
}
