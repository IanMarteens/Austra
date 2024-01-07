namespace Austra.Parser;

/// <summary>Controls the visibility of the lambda parameters.</summary>
internal sealed class LambdaBlock()
{
    /// <summary>Lambda parameters for all lambda levels.</summary>
    private readonly List<ParameterExpression> parameters = new(16);
    /// <summary>Number of parameters in each lambda level.</summary>
    private readonly List<int> paramCounts = new(16);

    /// <summary>Clears the lambda block for recycling.</summary>
    public void Clean()
    {
        paramCounts.Clear();
        parameters.Clear();
    }

    /// <summary>Creates a new lambda level with a single parameter.</summary>
    /// <param name="parameter">The lambda parameter.</param>
    public void Add(ParameterExpression parameter)
    {
        paramCounts.Add(1);
        parameters.Add(parameter);
    }

    /// <summary>Creates a new lambda level with two parameters.</summary>
    /// <param name="param1">The first lambda parameter.</param>
    /// <param name="param2">The second lambda parameter.</param>
    public void Add(ParameterExpression param1, ParameterExpression param2)
    {
        paramCounts.Add(2);
        parameters.Add(param1);
        parameters.Add(param2);
    }

    /// <summary>Creates a new lambda level with a list of parameters.</summary>
    /// <param name="parameters">The list of parameters.</param>
    public void Add(params ParameterExpression[] parameters)
    {
        paramCounts.Add(parameters.Length);
        this.parameters.AddRange(parameters);
    }

    /// <summary>
    /// Creates a lambda expression from the given body and return type.
    /// </summary>
    /// <param name="parser">The parser this control block is attached to.</param>
    /// <param name="body">The body of the expression.</param>
    /// <param name="retType">Expected return type.</param>
    /// <returns>The corresponding lambda expression, compilable to a delegate.</returns>
    public LambdaExpression Create(Parser parser, Expression body, Type retType) =>
        Create(parser, body, retType, false).expr;

    /// <summary>
    /// Creates a lambda expression from the given body and return type.
    /// </summary>
    /// <param name="parser">The parser this control block is attached to.</param>
    /// <param name="body">The body of the expression.</param>
    /// <param name="retType">Expected return type.</param>
    /// <param name="upgradeReturn">When <see langword="true"/>, the return type can be made real.</param>
    /// <returns>The corresponding lambda expression, compilable to a delegate.</returns>
    public (LambdaExpression expr, bool upgraded) Create(
        Parser parser, Expression body, Type retType, bool upgradeReturn)
    {
        int stackTop = paramCounts.LastOrDefault();
        try
        {
            bool upgraded = false;
            if (body.Type != retType)
                if (body.Type == typeof(double) && upgradeReturn)
                    upgraded = true;
                else
                    body = retType == typeof(Complex) && (body.Type == typeof(int) || body.Type == typeof(double))
                        ? Expression.Convert(body, typeof(Complex))
                        : retType == typeof(double) && body.Type == typeof(int)
                        ? IntToDouble(body)
                        : throw parser.Error($"Expected return type is {retType.Name}");
            return (stackTop == 0
                ? Expression.Lambda(body)
                : Expression.Lambda(body, parameters.GetRange(parameters.Count - stackTop, stackTop)),
                upgraded);
        }
        finally
        {
            // Clean this lambda level.
            parameters.RemoveRange(parameters.Count - stackTop, stackTop);
            paramCounts.RemoveAt(paramCounts.Count - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Expression IntToDouble(Expression e) =>
            e is ConstantExpression constExpr
            ? Expression.Constant((double)(int)constExpr.Value!)
            : Expression.Convert(e, typeof(double));
    }

    /// <summary>Retrieve alive parameters for Code Completion.</summary>
    /// <param name="members">The list of Code Completion members.</param>
    /// <returns><see langword="true"/> when any parameters were found.</returns>
    public void GatherParameters(List<Member> members)
    {
        for (int i = parameters.Count - 1; i >= 0; i--)
            members.Add(new(parameters[i].Name!, "Lambda parameter"));
    }

    /// <summary>Symbol lookup for lambda parameters.</summary>
    /// <param name="identifier">Symbol to identify.</param>
    /// <param name="parameter">When <see langword="true"/>, the matching parameter.</param>
    /// <returns><see langword="true"/> when the identifier corresponds to either of the parameters.</returns>
    public bool TryMatch(
        string identifier,
        [NotNullWhen(true)] out ParameterExpression? parameter)
    {
        for (int i = parameters.Count - 1; i >= 0; i--)
        {
            if (identifier.Equals(parameters[i].Name ?? "", StringComparison.OrdinalIgnoreCase))
            {
                parameter = parameters[i];
                return true;
            }
        }
        parameter = null;
        return false;
    }
}

