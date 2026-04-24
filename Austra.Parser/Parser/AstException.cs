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

/// <summary>Contains extension methods acting on <see cref="Type"/> instances.</summary>
internal static class ParserExtensions
{
    extension(Type type)
    {
        /// <summary>Avoids repeating the bang operator (!) in the code.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MethodInfo Get(string method) =>
             type.GetMethod(method)!;

        /// <summary>Gets the property getter method for the specified property.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MethodInfo Prop(string property) =>
             type.GetProperty(property)!.GetGetMethod()!;

        public MethodData MD(params Type[] argTypes) =>
            new(type, null, argTypes);

        public MethodData MD(string member, params Type[] argTypes) =>
            new(type, member, argTypes);

        /// <summary>
        /// Gets the constructor for the specified types and creates a new expression.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NewExpression New(params Expression[] args) =>
            Expression.New(type.GetConstructor([.. args.Select(a => a.Type)])!, args);

        /// <summary>
        /// Gets the parameterless constructor for the specified type and creates a new expression.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NewExpression New() =>
            Expression.New(type.GetConstructor(Type.EmptyTypes)!);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NewArrayExpression Make(IEnumerable<Expression> args) =>
            Expression.NewArrayInit(type, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MethodCallExpression Call(Expression? instance, string method, Expression arg) =>
            Expression.Call(instance, type.GetMethod(method, [arg.Type])!, arg);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MethodCallExpression Call(string method, Expression a) =>
            Expression.Call(type.GetMethod(method, [a.Type])!, a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MethodCallExpression Call(string method, Expression a1, Expression a2) =>
            Expression.Call(type.GetMethod(method, [a1.Type, a2.Type])!, a1, a2);
    }
}
