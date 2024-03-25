namespace Austra.Parser;

/// <summary>Extension methods for <see cref="Expression"/>.</summary>
internal static class TreeExtensions
{
    /// <summary>Gets a recursive string representation of the expression.</summary>
    /// <param name="e">Expression to describe.</param>
    /// <returns>The text equivalent to the expression.</returns>
    public static string AsString(this Expression e) =>
        e switch
        {
            LambdaExpression lambda => $"({string.Join(", ", lambda.Parameters.Select(p => p.Name))} => {AsString(lambda.Body)})",
            UnaryExpression { NodeType: ExpressionType.Convert, Type: var t, Operand: var operand } => $"({t.Name}){AsString(operand)}",
            BlockExpression b => Describe(b),
            MethodCallExpression m => $"{m.Method.Name}({string.Join(", ", m.Arguments.Select(AsString))})",
            _ => e.ToString(),
        };

    private static string DescribeVariables(BlockExpression b) =>
        string.Join(", ", b.Variables.Select(v => $"{v.Type.Name} {v.Name}"));

    private static string DescribeBlock(BlockExpression b) =>
        string.Join("; ", b.Expressions.Select(AsString));

    private static string Describe(BlockExpression b) =>
        b.Variables.Count == 0
        ? "{" + DescribeBlock(b) + "}"
        : "{" + DescribeVariables(b) + "; " + DescribeBlock(b) + " }";
}
