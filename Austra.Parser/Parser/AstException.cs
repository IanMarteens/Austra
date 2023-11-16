namespace Austra.Parser;

/// <summary>A parsing exception associated with a position.</summary>
/// <remarks>Creates a new exception with a message and a position.</remarks>
/// <param name="message">Error message.</param>
/// <param name="position">Error position.</param>
public class AstException(string message, int position) : ApplicationException(message)
{
    /// <summary>Gets the position inside the source code.</summary>
    public int Position { get; } = position;
}

/// <summary>Silently aborts parsing for code-completion purposes.</summary>
/// <remarks>This exception must always be catched inside the library.</remarks>
/// <remarks>Creates a new exception with an attached message.</remarks>
/// <param name="message">Error message.</param>
internal sealed class AbortException(string message) : ApplicationException(message)
{
}