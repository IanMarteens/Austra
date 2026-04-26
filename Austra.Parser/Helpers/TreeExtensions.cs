namespace Austra.Parser;

/// <summary>Extension methods for <see cref="Expression"/>.</summary>
internal static class TreeExtensions
{
    extension(Expression e)
    {
        public string Format()
        {
            string s = e.AsString()
                .Replace("datasource.Listener.Enqueue(", "enqueue(")
                .Replace("datasource.Item[", "datasource[");
            return s.StartsWith("(datasource => ")
                ? s["(datasource => ".Length..].TrimEnd(')')
                : s;
        }

        /// <summary>Gets a recursive string representation of the expression.</summary>
        /// <returns>The text equivalent to the expression.</returns>
        public string AsString() =>
            e switch
            {
                LambdaExpression lambda => $"({string.Join(", ", lambda.Parameters.Select(p => p.Name))} => {AsString(lambda.Body)})",
                UnaryExpression
                {
                    NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked,
                    Type: var t,
                    Operand: var operand
                } => $"({t.Name}){AsString(operand)}",
                BlockExpression b => Describe(b),
                MethodCallExpression m => $"{DescribeInstance(m.Object)}{m.Method.Name}({string.Join(", ", m.Arguments.Select(AsString))})",
                _ => e.ToString(),
            };
    }

    private static string DescribeInstance(Expression? e) =>
        e is null ? "" : $"{AsString(e)}.";

    private static string DescribeVariables(BlockExpression b) =>
        string.Join(", ", b.Variables.Select(v => $"{v.Type.Name} {v.Name}"));

    private static string DescribeBlock(BlockExpression b) =>
        string.Join("; ", b.Expressions.Select(AsString));

    private static string Describe(BlockExpression b) =>
        b.Variables.Count == 0
        ? "{" + DescribeBlock(b) + "}"
        : "{" + DescribeVariables(b) + "; " + DescribeBlock(b) + " }";
}
