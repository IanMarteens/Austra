namespace Austra.Library;

/// <summary>Vector length mismatch.</summary>
[Serializable]
public class VectorLengthException : Exception
{
    /// <summary>Initializes an exception with the default message.</summary>
    public VectorLengthException() : base("Vector length mismatch") { }

    /// <summary>Initializes an exception with a given message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public VectorLengthException(string message) : base(message) { }

    /// <summary>Initializes an exception with a given message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public VectorLengthException(string message, Exception innerException) :
        base(message, innerException)
    { }
}

/// <summary>Matrix size mismatch.</summary>
[Serializable]
public class MatrixSizeException : Exception
{
    /// <summary>Initializes an exception with the default message.</summary>
    public MatrixSizeException() : base("Matrix size mismatch") { }

    /// <summary>Initializes an exception with a given message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public MatrixSizeException(string message) : base(message) { }

    /// <summary>Initializes an exception with a given message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public MatrixSizeException(string message, Exception innerException) :
        base(message, innerException)
    { }
}

/// <summary>An algorithm failed to converge.</summary>
[Serializable]
public class ConvergenceException : Exception
{
    /// <summary>Initializes an exception with the default message.</summary>
    public ConvergenceException() : base("Convergence failed") { }

    /// <summary>Initializes an exception with a given message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public ConvergenceException(string message) : base(message) { }

    /// <summary>Initializes an exception with a given message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ConvergenceException(string message, Exception innerException) :
        base(message, innerException)
    { }
}

/// <summary>A matrix is not positive definite.</summary>
[Serializable]
public class NonPositiveDefiniteException : Exception
{
    /// <summary>Initializes an exception with the default message.</summary>
    public NonPositiveDefiniteException() : base("Matrix must be positive definite")
    { }

    /// <summary>Initializes an exception with a given message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public NonPositiveDefiniteException(string message) : base(message) { }

    /// <summary>Initializes an exception with a given message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public NonPositiveDefiniteException(string message, Exception innerException) :
        base(message, innerException)
    { }
}

/// <summary>A series slice cannot be empty.</summary>
[Serializable]
public class EmptySliceException : Exception
{
    /// <summary>Initializes an exception with the default message.</summary>
    public EmptySliceException() : base("Slice is empty") { }

    /// <summary>Initializes an exception with a given message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public EmptySliceException(string message) : base(message) { }

    /// <summary>Initializes an exception with a given message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public EmptySliceException(string message, Exception innerException) :
        base(message, innerException)
    { }
}

/// <summary>No roots were found while solving a polynomial equation.</summary>
[Serializable]
public class PolynomialRootsException : Exception
{
    /// <summary>Initializes an exception with the default message.</summary>
    public PolynomialRootsException() : base("No roots were found") { }

    /// <summary>Initializes an exception with a given message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public PolynomialRootsException(string message) : base(message) { }

    /// <summary>Initializes an exception with a given message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public PolynomialRootsException(string message, Exception innerException) :
        base(message, innerException)
    { }
}
