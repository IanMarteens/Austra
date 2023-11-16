namespace Austra.Parser;

/// <summary>Syntactic and lexical analysis for AUSTRA.</summary>
internal sealed partial class Parser
{
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] VectorVectorArg = [typeof(Vector), typeof(Vector)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] DoubleVectorArg = [typeof(double), typeof(Vector)];
    /// <summary>Constructor for <see cref="Index"/>.</summary>
    private static readonly ConstructorInfo IndexCtor =
        typeof(Index).GetConstructor([typeof(int), typeof(bool)])!;
    /// <summary>Constructor for <see cref="Range"/>.</summary>
    private static readonly ConstructorInfo RangeCtor =
        typeof(Range).GetConstructor([typeof(Index), typeof(Index)])!;
    /// <summary>The <see cref="Expression"/> for <see langword="false"/>.</summary>
    private static readonly ConstantExpression FalseExpr = Expression.Constant(false);
    /// <summary>The <see cref="Expression"/> for <see langword="true"/>.</summary>
    private static readonly ConstantExpression TrueExpr = Expression.Constant(true);
    /// <summary>The <see cref="Expression"/> for <see cref="Complex.ImaginaryOne"/>.</summary>
    private static readonly ConstantExpression ImExpr = Expression.Constant(Complex.ImaginaryOne);
    /// <summary>The <see cref="Expression"/> for <see cref="Math.PI"/>.</summary>
    private static readonly ConstantExpression PiExpr = Expression.Constant(Math.PI);
    /// <summary>Method for multiplying by a transposed matrix.</summary>
    private static readonly MethodInfo MatrixMultiplyTranspose =
        typeof(Matrix).Get(nameof(Matrix.MultiplyTranspose));
    /// <summary>Method for multiplying a vector by a transposed matrix.</summary>
    private static readonly MethodInfo MatrixTransposeMultiply =
        typeof(Matrix).Get(nameof(Matrix.TransposeMultiply));
    /// <summary>Method for linear vector combinations.</summary>
    private static readonly MethodInfo VectorCombine2 =
        typeof(Vector).GetMethod(nameof(Vector.Combine2),
            [typeof(double), typeof(double), typeof(Vector), typeof(Vector)])!;
    /// <summary>Method for linear vector combinations.</summary>
    private static readonly MethodInfo MatrixCombine =
        typeof(Matrix).GetMethod(nameof(Matrix.MultiplyAdd),
            [typeof(Vector), typeof(double), typeof(Vector)])!;
}
