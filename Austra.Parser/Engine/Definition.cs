namespace Austra.Parser;

/// <summary>Represents a parameterless macro persisted in the database.</summary>
public sealed class Definition
{
    /// <summary>Creates a new definition.</summary>
    /// <param name="name">The symbolic name for the definition.</param>
    /// <param name="text">The body of the definition.</param>
    /// <param name="descr">An optional description.</param>
    /// <param name="expression">The LINQ expression for the definition.</param>
    public Definition(string name, string text, string descr, Expression expression) =>
        (Name, Text, Description, Expression, Type) =
        (name, text, !string.IsNullOrWhiteSpace(descr) ? descr : name, 
         expression, expression.Type);

    /// <summary>Gets the symbolic name for the definition.</summary>
    public string Name { get; }
    /// <summary>Gets the definition's body.</summary>
    public string Text { get; }
    /// <summary>Gets the definition's type.</summary>
    public Type Type { get; }
    /// <summary>Gets a textual description.</summary>
    public string Description { get; }
    /// <summary>Gets the expression tree.</summary>
    public Expression Expression { get; }
    /// <summary>Definitions that depends on this one.</summary>
    public IList<Definition> Children { get; } = new List<Definition>();

    /// <summary>Internal flag for recursive removal.</summary>
    internal bool Flag { get; set; }
}

/// <summary>Represents a list of definitions to be removed.</summary>
/// <param name="Definitions">The sorted list of definitions to remove.</param>
public sealed record class UndefineList(string[] Definitions);

