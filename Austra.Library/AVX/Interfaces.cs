namespace Austra.Library;

/// <summary>Common interface for all matrix types.</summary>
public interface IVector
{
}

/// <summary>Common interface for all matrix types.</summary>
public interface IMatrix
{
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
