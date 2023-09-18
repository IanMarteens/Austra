namespace Austra.Parser;

/// <summary>Inherited attributes for the parser.</summary>
internal sealed class AstContext
{
    /// <summary>Initializes a parsing context.</summary>
    /// <param name="source">Environment variables.</param>
    /// <param name="lex">Lexical analyzer.</param>
    public AstContext(IDataSource source, Lexer lex)
    {
        (Source, Lex) = (source, lex);
        lex.Move();
    }

    /// <summary>Gets the outer scope for variables.</summary>
    public IDataSource Source { get; }
    /// <summary>Gets the lexical analyzer.</summary>
    public Lexer Lex { get; }

    /// <summary>Gets the parameter referencing the outer scope.</summary>
    public static ParameterExpression SourceParameter { get; } =
        Expression.Parameter(typeof(IDataSource), "datasource");

    /// <summary>Place holder for the first lambda parameter, if any.</summary>
    public ParameterExpression? LambdaParameter { get; set; }
    /// <summary>Place holder for the second lambda parameter, if any.</summary>
    public ParameterExpression? LambdaParameter2 { get; set; }

    /// <summary>Gets the type of the active token.</summary>
    public Token Kind
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Lex.Current.Kind;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (string lowerText, int position) GetTextAndPos() =>
        (Lex.Current.Text.ToLower(), Lex.Current.Position);

    /// <summary>Transient local variable definitions.</summary>
    public Dictionary<string, ParameterExpression> Locals { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Advances the lexical analyzer one token.</summary>
    public void MoveNext() => Lex.Move();

    /// <summary>Skips two tokens with a single call.</summary>
    public void Skip2() { Lex.Move(); Lex.Move(); }

    /// <summary>
    /// Checks that the current token is of the expected kind and advances the cursor.
    /// </summary>
    /// <param name="kind">Expected type of token.</param>
    /// <param name="errorMessage">Error message to use in the exception.</param>
    /// <exception cref="AstException">Thrown when the token doesn't match.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CheckAndMoveNext(Token kind, string errorMessage)
    {
        if (Lex.Current.Kind != kind)
            throw new AstException(errorMessage, Lex.Current.Position);
        Lex.Move();
    }

    /// <summary>Controls that only persisted values are used.</summary>
    public bool ParsingDefinition { get; set; }

    /// <summary>Name of the left side value, if any.</summary>
    public string LeftValue { get; set; } = "";

    /// <summary>Referenced definitions.</summary>
    public HashSet<Definition> References { get; } = new();

    /// <summary>
    /// Creates an expression that retrieves a series from the data source.
    /// </summary>
    /// <param name="id">Series name.</param>
    /// <param name="type">Result type.</param>
    public static Expression GetFromDataSource(string id, Type type) =>
        Expression.Convert(
            Expression.Property(SourceParameter, "Item", Expression.Constant(id)), type);
}

/// <summary>A parsing exception associated with a position.</summary>
public class AstException : ApplicationException
{
    /// <summary>Gets the position inside the source code.</summary>
    public int Position { get; }

    /// <summary>Creates a new exception with a message and a position.</summary>
    /// <param name="message">Error message.</param>
    /// <param name="position">Error position.</param>
    public AstException(string message, int position)
        : base(message) => Position = position;
}
