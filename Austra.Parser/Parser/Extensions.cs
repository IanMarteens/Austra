namespace Austra.Parser;

/// <summary>Contains extension methods acting on <see cref="Type"/> instances.</summary>
internal static class ParserExtensions
{
    /// <summary>Avoids repeating the bang operator (!) in the code.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo Get(this Type type, string method) =>
         type.GetMethod(method)!;

    /// <summary>Gets the property getter method for the specified property.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo Prop(this Type type, string property) =>
         type.GetProperty(property)!.GetGetMethod()!;

    public static MethodData MD(this Type type, params Type[] argTypes) =>
        new(type, null, argTypes);

    public static MethodData MD(this Type type, string member, params Type[] argTypes) =>
        new(type, member, argTypes);

    /// <summary>
    /// Gets the constructor for the specified types and creates a new expression.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NewExpression New(this Type type, params Expression[] args) =>
        Expression.New(type.GetConstructor(args.Select(a => a.Type).ToArray())!, args);

    /// <summary>
    /// Gets the parameterless constructor for the specified type and creates a new expression.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NewExpression New(this Type type) =>
        Expression.New(type.GetConstructor(Type.EmptyTypes)!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NewArrayExpression Make(this Type type, IEnumerable<Expression> args) =>
        Expression.NewArrayInit(type, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodCallExpression Call(this Type type,
        Expression? instance, string method, Expression arg) =>
        Expression.Call(instance, type.GetMethod(method, [arg.Type])!, arg);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodCallExpression Call(this Type type,
        string method, Expression a1, Expression a2) =>
        Expression.Call(type.GetMethod(method, [a1.Type, a2.Type])!, a1, a2);
}
