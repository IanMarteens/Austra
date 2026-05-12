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
                ? s["(datasource => ".Length..][..^1]
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

        /// <summary>Checks if the expression's type is either a double or an integer.</summary>
        public bool IsArithmetic
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => e.Type == typeof(int) || e.Type == typeof(double);
        }

        /// <summary>Checks if the expression's type0 is either a double, an integer or a long.</summary>
        public bool IsNumeric
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => e.Type == typeof(int) || e.Type == typeof(double) || e.Type == typeof(long);
        }

        public bool IsMatrix
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => e.Type.IsAssignableTo(typeof(IMatrix));
        }

        public bool IsVector
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => e.Type.IsAssignableTo(typeof(IVector));
        }

        public bool IsIntVecOrSeq
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => e.Type == typeof(NVector) || e.Type == typeof(NSequence)
                || e.Type == typeof(DateVector);
        }

        public Expression ToLong
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get =>
                e.Type == typeof(int)
                ? (e is ConstantExpression { Value: int v }
                    ? Expression.Constant((long)v)
                    : Expression.Convert(e, typeof(long)))
                : e;
        }

        public Expression ToDouble
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get =>
                e.Type == typeof(int)
                ? (e is ConstantExpression { Value: int i }
                    ? Expression.Constant((double)i)
                    : Expression.Convert(e, typeof(double)))
                : e.Type == typeof(long)
                ? (e is ConstantExpression { Value: long li }
                    ? Expression.Constant((double)li)
                    : Expression.Convert(e, typeof(double)))
                : e;
        }

        public Expression IntToDouble
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get =>
                e is ConstantExpression { Value: int i }
                ? Expression.Constant((double)i)
                : Expression.Convert(e, typeof(double));
        }

        public bool TryMembership(ref Expression e2)
        {
            if (!e2.Type.IsAssignableTo(typeof(IContainer<>).MakeGenericType(e.Type)))
                return false;
            e2 = Expression.Call(e2,
                e2.Type.GetMethod(nameof(IContainer<>.Contains), [e.Type])!, e);
            return true;
        }
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
