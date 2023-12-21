namespace Austra.Parser;

/// <summary>Syntactic and lexical analysis for AUSTRA.</summary>
internal sealed partial class Parser
{
    /// <summary>Controls the visibility of the lambda parameters.</summary>
    /// <param name="parser">The parser this control block is attached to.</param>
    private sealed class LambdaBlock(Parser parser)
    {
        /// <summary>
        /// Lambda parameters from lambda functions wrapping the current one.
        /// </summary>
        private readonly ParameterExpression?[] parameters1 = new ParameterExpression?[8];
        private readonly ParameterExpression?[] parameters2 = new ParameterExpression?[8];
        /// <summary>Current lambda level.</summary>
        private int stackTop = -1;

        public void Add(Type type, string name)
        {
            stackTop++;
            parameters1[stackTop] = Expression.Parameter(type, name);
        }

        public void Add(Type type1, string name1, Type type2, string name2)
        {
            stackTop++;
            parameters1[stackTop] = Expression.Parameter(type1, name1);
            parameters2[stackTop] = Expression.Parameter(type2, name2);
        }

        /// <summary>
        /// Creates a lambda expression from the given body and return type.
        /// </summary>
        /// <param name="body">The body of the expression.</param>
        /// <param name="retType">Expected return type.</param>
        /// <returns>The corresponding lambda expression, compilable to a delegate.</returns>
        public Expression Create(Expression body, Type retType)
        {
            try
            {
                if (body.Type != retType)
                    body = retType == typeof(Complex) && IsArithmetic(body)
                        ? Expression.Convert(body, typeof(Complex))
                        : retType == typeof(double) && body.Type == typeof(int)
                        ? IntToDouble(body)
                        : throw parser.Error($"Expected return type is {retType.Name}");
                return parameters1[stackTop] is null
                    ? Expression.Lambda(body)
                    : parameters2[stackTop] is null
                    ? Expression.Lambda(body, parameters1[stackTop]!)
                    : Expression.Lambda(body, parameters1[stackTop]!, parameters2[stackTop]!);
            }
            finally
            {
                // Clean this lambda level.
                parameters1[stackTop] = parameters2[stackTop] = null;
                stackTop--;
            }
        }

        /// <summary>Retrieve alive parameters for Code Completion.</summary>
        /// <param name="members">The list of Code Completion members.</param>
        /// <returns><see langword="true"/> when any parameters were found.</returns>
        public void GatherParameters(List<Member> members)
        {
            for (int i = stackTop; i >= 0; i--)
            {
                if (parameters1[i] is not null)
                    members.Add(new(parameters1[i]!.Name!, "Lambda parameter"));
                if (parameters2[i] is not null)
                    members.Add(new(parameters2[i]!.Name!, "Lambda parameter"));
            }
        }

        /// <summary>Symbol lookup for lambda parameters.</summary>
        /// <param name="identifier">Symbol to identify.</param>
        /// <param name="parameter">When <see langword="true"/>, the matching parameter.</param>
        /// <returns><see langword="true"/> when the identifier corresponds to either of the parameters.</returns>
        public bool TryMatch(
            string identifier,
            [NotNullWhen(true)] out ParameterExpression? parameter)
        {
            for (int i = stackTop; i >= 0; i--)
            {
                if (identifier.Equals(parameters1[i]?.Name ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    parameter = parameters1[i]!;
                    return true;
                }
                if (identifier.Equals(parameters2[i]?.Name ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    parameter = parameters2[i]!;
                    return true;
                }
            }
            parameter = null;
            return false;
        }
    }
}
