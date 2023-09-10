#nullable disable

namespace Austra.Parser;

/// <summary>Syntactic analysis for AUSTRA.</summary>
internal static partial class Parser
{
    /// <summary>Compiles a block expression as a lambda function.</summary>
    /// <param name="ctx">Compiling context.</param>
    /// <returns>A lambda method, when successful.</returns>
    public static Func<IDataSource, object> Parse(AstContext ctx) =>
        Expression.Lambda<Func<IDataSource, object>>(
            ParseStatement(ctx, true), AstContext.SourceParameter).Compile();

    /// <summary>Parses a block expression without compiling it.</summary>
    /// <param name="ctx">Compiling context.</param>
    /// <returns>The type of the block expression.</returns>
    public static Type ParseType(AstContext ctx) =>
        ParseStatement(ctx, false).Type;

    /// <summary>Parses a definition and adds it to the source.</summary>
    /// <param name="ctx">Compiling context.</param>
    /// <param name="text">The text to be parsed.</param>
    /// <param name="description">A description for the definition.</param>
    /// <returns>A new definition, on success.</returns>
    public static Definition ParseDefinition(AstContext ctx,
        string text, string description)
    {
        ctx.CheckAndMoveNext(Token.Def, "DEF expected");
        if (ctx.Kind != Token.Id)
            throw Error("Definition name expected", ctx);
        string defName = ctx.Lex.Current.Text;
        if (ctx.Source.GetDefinition(defName) != null ||
            ctx.Source[defName] != null)
            throw Error($"{defName} already in use", ctx);
        ctx.MoveNext();
        if (ctx.Kind == Token.Colon)
        {
            ctx.MoveNext();
            if (ctx.Kind != Token.Str)
                throw Error("Definition description expected", ctx);
            description = ctx.Lex.Current.Text;
            ctx.MoveNext();
        }
        ctx.CheckAndMoveNext(Token.Eq, "= expected");
        int first = ctx.Lex.Current.Position;
        ctx.ParsingDefinition = true;
        Expression e = ParseFormula(ctx, false);
        if (e.Type == typeof(Series))
            e = Expression.Call(e, typeof(Series).Get(nameof(Series.SetName)),
                Expression.Constant(defName));
        return new(defName, text[first..], description, e);
    }

    /// <summary>Compiles a block expression.</summary>
    /// <param name="ctx">Compiling context.</param>
    /// <param name="forceCast">Whether to force a cast to object.</param>
    /// <returns>A block expression.</returns>
    private static Expression ParseStatement(AstContext ctx, bool forceCast)
    {
        if (ctx.Kind == Token.Set)
        {
            ctx.MoveNext();
            if (ctx.Kind != Token.Id)
                throw Error("Left side variable expected", ctx);
            Lexeme name = ctx.Lex.Current;
            ctx.LeftValue = name.Text;
            ctx.MoveNext();
            if (ctx.Kind == Token.Eof)
            {
                ctx.Source[ctx.LeftValue] = null;
                return Expression.Constant(null);
            }
            ctx.CheckAndMoveNext(Token.Eq, "= expected");
            // Always allow deleting a session variable.
            if (ctx.Source.GetDefinition(name.Text) != null)
                throw Error($"{name.Text} already in use", name);
        }
        return ParseFormula(ctx, forceCast);
    }

    private static Expression ParseFormula(AstContext ctx, bool forceCast)
    {
        List<ParameterExpression> locals = new();
        List<Expression> expressions = new();
        if (ctx.Kind == Token.Let)
        {
            do
            {
                ctx.MoveNext();
                if (ctx.Kind != Token.Id)
                    throw Error("Identifier expected", ctx);
                string localId = ctx.Lex.Current.Text;
                ctx.MoveNext();
                ctx.CheckAndMoveNext(Token.Eq, "= expected");
                Expression init = ParseConditional(ctx);
                ParameterExpression le = Expression.Variable(init.Type, localId);
                locals.Add(le);
                expressions.Add(Expression.Assign(le, init));
                ctx.Locals[localId] = le;
            }
            while (ctx.Kind == Token.Comma);
            ctx.CheckAndMoveNext(Token.In, "IN expected");
        }
        Expression rvalue = ParseConditional(ctx);
        if (forceCast)
            rvalue = Expression.Convert(rvalue, typeof(object));
        if (ctx.LeftValue != "")
            rvalue = Expression.Assign(Expression.Property(
                AstContext.SourceParameter, "Item", Expression.Constant(ctx.LeftValue)), rvalue);
        expressions.Add(rvalue);
        return ctx.Kind != Token.Eof
            ? throw Error("Extra input after expression", ctx)
            : locals.Count == 0 && expressions.Count == 1
            ? expressions[0]
            : Expression.Block(locals, expressions);
    }

    /// <summary>Compiles a ternary conditional expression.</summary>
    /// <param name="ctx">Compiling context.</param>
    private static Expression ParseConditional(AstContext ctx)
    {
        if (ctx.Kind != Token.If)
            return ParseDisjunction(ctx);
        ctx.MoveNext();
        Expression c = ParseDisjunction(ctx);
        if (c.Type != typeof(bool))
            throw Error("Condition must be boolean", ctx);
        ctx.CheckAndMoveNext(Token.Then, "THEN expected");
        Expression e1 = ParseConditional(ctx);
        ctx.CheckAndMoveNext(Token.Else, "ELSE expected");
        Expression e2 = ParseConditional(ctx);
        return DifferentTypes(ref e1, ref e2)
            ? throw Error("Conditional operands are not compatible", ctx)
            : Expression.Condition(c, e1, e2);
    }

    private static Expression ParseLightConditional(AstContext ctx) =>
        ctx.Kind == Token.If ? ParseConditional(ctx) : ParseAdditive(ctx);

    /// <summary>Compiles an OR/AND expression.</summary>
    /// <param name="ctx">Compiling context.</param>
    private static Expression ParseDisjunction(AstContext ctx)
    {
        Expression e1 = null;
        int orLex = ctx.Lex.Current.Position;
        for (; ; )
        {
            Expression e2 = ParseLogicalFactor(ctx);
            while (ctx.Kind == Token.And)
            {
                int andLex = ctx.Lex.Current.Position;
                ctx.MoveNext();
                Expression e3 = ParseLogicalFactor(ctx);
                e2 = e2.Type != typeof(bool) || e3.Type != typeof(bool)
                    ? throw Error("AND operands must be boolean", andLex)
                    : Expression.AndAlso(e2, e3);
            }
            e1 = e1 is null
                ? e2
                : e1.Type != typeof(bool) || e2.Type != typeof(bool)
                ? throw Error("OR operands must be boolean", orLex)
                : Expression.OrElse(e1, e2);
            if (ctx.Kind != Token.Or)
                break;
            orLex = ctx.Lex.Current.Position;
            ctx.MoveNext();
        }
        return e1;
    }

    /// <summary>Compiles a [NOT] comparison expression.</summary>
    /// <param name="ctx">Compiling context.</param>
    private static Expression ParseLogicalFactor(AstContext ctx)
    {
        if (ctx.Kind == Token.Not)
        {
            int notLex = ctx.Lex.Current.Position;
            ctx.MoveNext();
            Expression e = ParseLogicalFactor(ctx);
            return e.Type != typeof(bool)
                ? throw Error("NOT operand must be boolean", notLex)
                : Expression.Not(e);
        }
        Expression e1 = ParseAdditive(ctx);
        (Token opKind, int pos) = ctx.Lex.Current;
        switch (opKind)
        {
            case Token.Eq:
            case Token.Ne:
                {
                    ctx.MoveNext();
                    Expression e2 = ParseAdditive(ctx);
                    return DifferentTypes(ref e1, ref e2)
                        ? throw Error("Equality operands are not compatible", pos)
                        : opKind == Token.Eq ? Expression.Equal(e1, e2)
                        : Expression.NotEqual(e1, e2);
                }

            case Token.Lt:
            case Token.Gt:
            case Token.Le:
            case Token.Ge:
                {
                    ctx.MoveNext();
                    Expression e2 = ParseAdditive(ctx);
                    if (e1.Type != e2.Type)
                    {
                        if (!AreArithmeticTypes(e1, e2))
                            throw Error("Comparison operators are not compatible", pos);
                        (e1, e2) = (ToDouble(e1), ToDouble(e2));
                    }
                    try
                    {
                        if (IsArithmetic(e2))
                        {
                            Token op2 = ctx.Kind;
                            if ((opKind == Token.Lt || opKind == Token.Le) &&
                                (op2 == Token.Lt || op2 == Token.Le) ||
                                (opKind == Token.Gt || opKind == Token.Ge) &&
                                (op2 == Token.Gt || op2 == Token.Ge))
                            {
                                ctx.MoveNext();
                                Expression e3 = ParseAdditive(ctx);
                                if (!IsArithmetic(e3))
                                    throw Error("Upper bound must be numeric", ctx);
                                if (e3.Type != e2.Type)
                                    if (e3.Type == typeof(int))
                                        e3 = ToDouble(e3);
                                    else
                                        (e1, e2) = (ToDouble(e1), ToDouble(e2));
                                return Expression.AndAlso(
                                    Comp(opKind, e1, e2), Comp(op2, e2, e3));
                            }
                        }
                        return Comp(opKind, e1, e2);

                        static Expression Comp(Token t, Expression e1, Expression e2) => t switch
                        {
                            Token.Lt => Expression.LessThan(e1, e2),
                            Token.Gt => Expression.GreaterThan(e1, e2),
                            Token.Le => Expression.LessThanOrEqual(e1, e2),
                            _ => Expression.GreaterThanOrEqual(e1, e2)
                        };
                    }
                    catch
                    {
                        throw Error("Comparison operators are not compatible", pos);
                    }
                }
            default:
                return e1;
        }
    }

    private static Expression ParseAdditive(AstContext ctx)
    {
        Expression e1 = ParseMultiplicative(ctx);
        while (ctx.Kind == Token.Plus || ctx.Kind == Token.Minus)
        {
            Lexeme opLex = ctx.Lex.Current;
            ctx.MoveNext();
            Expression e2 = ParseMultiplicative(ctx);
            if (opLex.Kind == Token.Plus && e1.Type == typeof(string))
            {
                if (e2.Type != typeof(string))
                    e2 = Expression.Call(e2,
                        e2.Type.GetMethod(nameof(ToString), Array.Empty<Type>()));
                e1 = typeof(string).Call(nameof(string.Concat), e1, e2);
            }
            else
            {
                if (e1.Type != e2.Type &&
                    e1.Type != typeof(Date) && e2.Type != typeof(Date))
                    (e1, e2) = (ToDouble(e1), ToDouble(e2));
                try
                {
                    if (opLex.Kind == Token.Plus &&
                        e1.Type == typeof(Vector) && e2.Type == typeof(Vector) &&
                        e1 is BinaryExpression { NodeType: ExpressionType.Multiply } be1)
                        e1 = be1.Right.Type == typeof(double)
                            ? Expression.Call(be1.Left,
                                typeof(Vector).GetMethod(nameof(Vector.MultiplyAdd),
                                new[] { typeof(double), typeof(Vector) }), be1.Right, e2)
                            : be1.Left.Type == typeof(double)
                            ? Expression.Call(be1.Right,
                                typeof(Vector).GetMethod(nameof(Vector.MultiplyAdd),
                                new[] { typeof(double), typeof(Vector) }), be1.Left, e2)
                            : be1.Left.Type == typeof(Matrix)
                            ? Expression.Call(be1.Left,
                                typeof(Matrix).GetMethod(nameof(Matrix.MultiplyAdd),
                                new[] { typeof(Vector), typeof(Vector) }), be1.Right, e2)
                            : Expression.Add(e1, e2);
                    else
                        e1 = e1 is ConstantExpression c1 && c1.Value is double d1 &&
                                e2 is ConstantExpression c2 && c2.Value is double d2
                            ? Expression.Constant(opLex.Kind == Token.Plus ? d1 + d2 : d1 - d2)
                            : opLex.Kind == Token.Plus
                            ? Expression.Add(e1, e2)
                            : Expression.Subtract(e1, e2);
                }
                catch
                {
                    throw Error($"Operator {opLex.Text} not supported for these types", opLex);
                }
            }
        }
        return e1;
    }

    private static Expression ParseMultiplicative(AstContext ctx)
    {
        Expression e1 = ParseUnary(ctx);
        while (ctx.Kind >= Token.Times && ctx.Kind <= Token.Mod)
        {
            Lexeme opLex = ctx.Lex.Current;
            ctx.MoveNext();
            Expression e2 = ParseUnary(ctx);
            if (opLex.Kind == Token.Backslash)
                e1 = e1.Type != typeof(Matrix)
                    ? throw Error("First operand must be a matrix", opLex)
                    : e2.Type != typeof(Vector) && e2.Type != typeof(Matrix)
                    ? throw Error("Second operand must be a vector or a matrix", opLex)
                    : Expression.Call(e1, typeof(Matrix).GetMethod(
                        nameof(Matrix.Solve), new[] { e2.Type }), e2); 
            else if (opLex.Kind == Token.PointTimes)
                e1 = e1.Type == e2.Type && e1.Type.IsAssignableTo(
                        typeof(IPointwiseMultiply<>).MakeGenericType(e1.Type))
                    ? Expression.Call(
                        e1, e2.Type.Get(nameof(Vector.PointwiseMultiply)), e2)
                    : throw Error("Invalid operator", opLex);
            else
            {
                if (e1.Type != e2.Type)
                    (e1, e2) = (ToDouble(e1), ToDouble(e2));
                try
                {
                    // Try to optimize matrix transpose multiplying a vector.
                    e1 = opLex.Kind == Token.Times && e1.Type == typeof(Matrix)
                        ? (e2.Type == typeof(Vector) && e1 is MethodCallExpression
                        { Method.Name: nameof(Matrix.Transpose) } mca
                            ? Expression.Call(mca.Object,
                                typeof(Matrix).Get(nameof(Matrix.TransposeMultiply)), e2)
                            : e2.Type == typeof(Matrix) && e2 is MethodCallExpression
                            { Method.Name: nameof(Matrix.Transpose) } mcb
                            ? Expression.Call(e1,
                                typeof(Matrix).Get(nameof(Matrix.MultiplyTranspose)), mcb.Object)
                            : Expression.Multiply(e1, e2))
                        : e1 is ConstantExpression c1 && c1.Value is double d1 &&
                            e2 is ConstantExpression c2 && c2.Value is double d2
                        ? Expression.Constant(opLex.Kind switch
                        {
                            Token.Times => d1 * d2,
                            Token.Div => d1 / d2,
                            _ => d1 % d2
                        })
                        : opLex.Kind == Token.Times
                        ? Expression.Multiply(e1, e2)
                        : opLex.Kind == Token.Div
                        ? Expression.Divide(e1, e2)
                        : Expression.Modulo(e1, e2);
                }
                catch
                {
                    throw Error($"Operator {opLex.Text} not supported for these types", opLex);
                }
            }
        }
        return e1;
    }

    private static Expression ParseUnary(AstContext ctx)
    {
        if (ctx.Kind == Token.Minus || ctx.Kind == Token.Plus)
        {
            (Token opKind, int opPos) = ctx.Lex.Current;
            ctx.MoveNext();
            Expression e1 = ParseUnary(ctx);
            return e1.Type != typeof(Complex) && !IsArithmetic(e1)
                && !IsVectorMatrix(e1) && e1.Type != typeof(Series)
                ? throw Error("Unary operand must be numeric", opPos)
                : opKind == Token.Plus ? e1 : Expression.Negate(e1);
        }
        Expression e = ParseFactor(ctx);
        return ctx.Kind == Token.Caret ? ParsePower(ctx, e) : e;
    }

    private static Expression ParsePower(AstContext ctx, Expression e)
    {
        int pos = ctx.Lex.Current.Position;
        ctx.MoveNext();
        Expression e1 = ParseFactor(ctx);
        if (AreArithmeticTypes(e, e1))
            return OptimizePowerOf() ? e : Expression.Power(ToDouble(e), ToDouble(e1));
        if (e.Type == typeof(Complex))
            if (e1.Type == typeof(Complex))
                return Expression.Call(typeof(Complex), nameof(Complex.Pow), null, e, e1);
            else if (IsArithmetic(e1))
                return OptimizePowerOf() ? e : Expression.Call(
                    typeof(Complex), nameof(Complex.Pow), null, e, ToDouble(e1));
        return e.Type == typeof(Vector) && e1.Type == typeof(Vector)
            ? Expression.ExclusiveOr(e, e1)
            : throw Error("Operands must be numeric", pos);

        bool OptimizePowerOf()
        {
            if (e1 is ConstantExpression ce && ce.Value is int power)
                if (power == 2)
                {
                    e = Expression.Multiply(e, e);
                    return true;
                }
                else if (power == 3)
                {
                    e = Expression.Multiply(Expression.Multiply(e, e), e);
                    return true;
                }
                else if (power == 4)
                {
                    e = Expression.Multiply(e, e);
                    e = Expression.Multiply(e, e);
                    return true;
                }
            return false;
        }
    }

    private static Expression ParseFactor(AstContext ctx)
    {
        Expression e;
        switch (ctx.Kind)
        {
            case Token.Int:
                {
                    int value = ctx.Lex.Current.AsInt;
                    ctx.MoveNext();
                    return Expression.Constant(value);
                }
            case Token.Real:
                {
                    double value = ctx.Lex.Current.AsReal;
                    ctx.MoveNext();
                    return Expression.Constant(value);
                }
            case Token.Imag:
                {
                    double value = ctx.Lex.Current.AsReal;
                    ctx.MoveNext();
                    return Expression.Constant(new Complex(0, value));
                }
            case Token.Str:
                {
                    var text = ctx.Lex.Current.Text;
                    ctx.MoveNext();
                    return Expression.Constant(text);
                }
            case Token.Date:
                {
                    Date value = ctx.Lex.Current.AsDate;
                    ctx.MoveNext();
                    e = Expression.Constant(value);
                    break;
                }
            case Token.False:
                ctx.MoveNext();
                return falseExpr;
            case Token.True:
                ctx.MoveNext();
                return trueExpr;
            case Token.LPar:
                ctx.MoveNext();
                e = ParseConditional(ctx);
                ctx.CheckAndMoveNext(Token.RPar, "Right parenthesis expected");
                break;
            case Token.Id:
                e = ParseVariable(ctx);
                break;
            case Token.MultVarR:
                {
                    Expression e1 = Expression.Constant(ctx.Lex.Current.AsReal);
                    int pos = ctx.Lex.Current.Position;
                    e = ParseVariable(ctx);
                    if (e.Type == typeof(int))
                        e = ToDouble(e);
                    else if (e.Type != typeof(double) && e.Type != typeof(Complex))
                        throw Error("Variable must be numeric", pos);
                    if (ctx.Kind == Token.Caret)
                        e = ParsePower(ctx, e);
                    e = Expression.Multiply(e1, e);
                }
                break;
            case Token.MultVarI:
                {
                    Expression e1 = Expression.Constant(ctx.Lex.Current.AsInt);
                    int pos = ctx.Lex.Current.Position;
                    e = ParseVariable(ctx);
                    if (e.Type == typeof(double))
                        e1 = ToDouble(e1);
                    else if (e.Type == typeof(Complex))
                        e1 = Expression.Convert(e1, typeof(Complex));
                    else if (e.Type != typeof(int))
                        throw Error("Variable must be numeric", pos);
                    if (ctx.Kind == Token.Dot)
                    {
                        ctx.MoveNext();
                        e = ParseProperty(ctx, e);
                    }
                    if (ctx.Kind == Token.Caret)
                        e = ParsePower(ctx, e);
                    e = Expression.Multiply(e1, e);
                }
                break;
            case Token.Functor:
                e = ParseFunction(ctx);
                break;
            case Token.LBra:
                e = ParseVectorLiteral(ctx);
                break;
            case Token.ClassName:
                {
                    var (className, p) = ctx.GetTextAndPos();
                    // Skip class name and double colon.
                    ctx.Skip2();
                    e = ctx.Kind != Token.Functor
                        ? throw Error("Method name expected", ctx)
                        : className switch
                        {
                            "matrix" => ParseMatrixMethod(ctx),
                            "vector" => ParseVectorMethod(ctx),
                            "complexvector" => ParseComplexVectorMethod(ctx),
                            "series" => ParseSeriesMethod(ctx),
                            "model" => ParseModelMethod(ctx),
                            "spline" => ParseSplineMethod(ctx),
                            _ => throw Error("Unknown class name", p)
                        };
                    break;
                }
            default:
                throw Error("Value expected", ctx);
        }
        for (; ; )
            switch (ctx.Kind)
            {
                case Token.Dot:
                    // Parse a method or property from an object.
                    ctx.MoveNext();
                    e = ctx.Kind switch
                    {
                        Token.Functor => ParseMethod(ctx, e),
                        Token.Id => ParseProperty(ctx, e),
                        _ => throw Error("Property name expected", ctx)
                    };
                    break;
                case Token.Transpose:
                    e = e.Type == typeof(ComplexVector)
                        ? Expression.Call(e, e.Type.Get(nameof(ComplexVector.Conjugate)))
                        : e.Type == typeof(Matrix) || e.Type == typeof(LMatrix)
                        ? Expression.Call(e, e.Type.Get(nameof(Matrix.Transpose)))
                        : e.Type == typeof(Complex)
                        ? Expression.Call(null, e.Type.Get(nameof(Complex.Conjugate)), e)
                        : throw Error("Can only transpose a matrix or conjugate a complex vector", ctx);
                    ctx.MoveNext();
                    break;
                case Token.LBra:
                    ctx.MoveNext();
                    e = e.Type == typeof(Vector) || e.Type == typeof(Series<int>)
                            || e.Type == typeof(ComplexVector)
                            || e.Type.IsAssignableTo(typeof(FftModel))
                        ? ParseIndexer(ctx, e, true)
                        : e.Type == typeof(Matrix)
                        ? ParseMatrixIndexer(ctx, e)
                        : e.Type == typeof(Series)
                        ? ParseSeriesIndexer(ctx, e)
                        : e.Type == typeof(Library.MVO.MvoModel)
                        ? ParseIndexer(ctx, e, false)
                        : e.Type == typeof(DateSpline)
                        ? ParseSplineIndexer(ctx, e, typeof(Date))
                        : e.Type == typeof(VectorSpline)
                        ? ParseSplineIndexer(ctx, e, typeof(double))
                        : throw Error("Invalid indexer", ctx);
                    break;
                case Token.LBrace:
                    e = e.Type == typeof(Vector) || e.Type == typeof(Series)
                        || e.Type == typeof(ComplexVector) || e.Type == typeof(Series<int>)
                        ? ParseSafeIndexer(ctx, e)
                        : throw Error("Safe indexes are only allowed for vectors and series", ctx);
                    break;
                default:
                    return e;
            }
    }

    private static Expression ParseSafeIndexer(AstContext ctx, Expression e)
    {
        ctx.MoveNext();
        Expression e1 = ParseLightConditional(ctx);
        if (e1.Type != typeof(int))
            throw Error("Index must be an integer", ctx);
        ctx.CheckAndMoveNext(Token.RBrace, "} expected in indexer");
        return Expression.Call(e, e.Type.Get(nameof(Vector.SafeThis)), e1);
    }

    private static Expression ParseSplineIndexer(AstContext ctx, Expression e, Type expected)
    {
        Expression e1 = ParseLightConditional(ctx);
        ctx.CheckAndMoveNext(Token.RBra, "] expected in indexer");
        return e1.Type != expected && (expected != typeof(double) || !IsArithmetic(e1))
            ? throw Error("Invalid index type", ctx)
            : Expression.Property(e, "Item", ToDouble(e1));
    }

    private static Expression ParseIndexer(AstContext ctx, Expression e, bool allowSlice)
    {
        bool fromEnd1 = false, fromEnd2 = false;
        Expression e1 = ctx.Kind == Token.Colon && allowSlice
            ? Expression.Constant(0)
            : ParseIndex(ctx, ref fromEnd1);
        if (allowSlice && ctx.Kind == Token.Colon)
        {
            ctx.MoveNext();
            Expression e2 = ctx.Kind == Token.RBra
                ? Expression.Constant(Index.End)
                : Expression.New(indexCtor,
                    ParseIndex(ctx, ref fromEnd2), Expression.Constant(fromEnd2));
            ctx.CheckAndMoveNext(Token.RBra, "] expected in indexer");
            return Expression.Property(e, "Item", Expression.New(rangeCtor,
                Expression.New(indexCtor, e1, Expression.Constant(fromEnd1)), e2));
        }
        ctx.CheckAndMoveNext(Token.RBra, "] expected in indexer");
        if (fromEnd1)
            e1 = Expression.New(indexCtor, e1, Expression.Constant(fromEnd1));
        return Expression.Property(e, "Item", e1);
    }

    private static Expression ParseMatrixIndexer(AstContext ctx, Expression e)
    {
        Expression e1 = null, e2 = null;
        bool fromEnd11 = false, fromEnd21 = false, isRange = false;
        if (ctx.Kind == Token.Comma)
            ctx.MoveNext();
        else
        {
            e1 = ctx.Kind == Token.Colon
                ? Expression.Constant(Index.Start)
                : ParseIndex(ctx, ref fromEnd11);
            if (ctx.Kind == Token.Colon)
            {
                ctx.MoveNext();
                bool fromEnd12 = false;
                Expression e12 = ctx.Kind == Token.Comma
                    ? Expression.Constant(Index.End)
                    : Expression.New(indexCtor,
                        ParseIndex(ctx, ref fromEnd12), Expression.Constant(fromEnd12));
                if (e1.Type != typeof(Index))
                    e1 = Expression.New(indexCtor, e1, Expression.Constant(fromEnd11));
                e1 = Expression.New(rangeCtor, e1, e12);
                isRange = true;
            }
            if (ctx.Kind != Token.RBra)
                ctx.CheckAndMoveNext(Token.Comma, "Comma expected");
        }
        if (ctx.Kind == Token.RBra)
            ctx.MoveNext();
        else
        {
            e2 = ctx.Kind == Token.Colon
                ? Expression.Constant(Index.Start)
                : ParseIndex(ctx, ref fromEnd21);
            if (ctx.Kind == Token.Colon)
            {
                ctx.MoveNext();
                bool fromEnd22 = false;
                Expression e22 = ctx.Kind == Token.RBra
                    ? Expression.Constant(Index.End)
                    : Expression.New(indexCtor,
                        ParseIndex(ctx, ref fromEnd22), Expression.Constant(fromEnd22));
                if (e2.Type != typeof(Index))
                    e2 = Expression.New(indexCtor, e2, Expression.Constant(fromEnd21));
                e2 = Expression.New(rangeCtor, e2, e22);
                if (!isRange && fromEnd11)
                    e1 = Expression.New(indexCtor, e1, trueExpr);
                isRange = true;
            }
            else if (isRange && fromEnd21)
                e2 = Expression.New(indexCtor, e2, trueExpr);
            ctx.CheckAndMoveNext(Token.RBra, "] expected");
        }
        if (isRange)
        {
            e1 ??= Expression.Constant(Range.All);
            e2 ??= Expression.Constant(Range.All);
        }
        else if (fromEnd11 || fromEnd21)
        {
            if (e1 != null)
                e1 = Expression.New(indexCtor, e1, Expression.Constant(fromEnd11));
            if (e2 != null)
                e2 = Expression.New(indexCtor, e2, Expression.Constant(fromEnd21));
        }
        return
            e1 != null && e2 != null
            ? Expression.Property(e, "Item", e1, e2)
            : e2 != null
            ? Expression.Call(e, typeof(Matrix).GetMethod(nameof(Matrix.GetColumn),
                new[] { e2.Type }), e2)
            : e1 != null
            ? Expression.Call(e, typeof(Matrix).GetMethod(nameof(Matrix.GetRow),
                new[] { e1.Type }), e1)
            : e;
    }

    private static Expression ParseSeriesIndexer(AstContext ctx, Expression e)
    {
        Expression e1 = null, e2 = null;
        bool fromEnd1 = false, fromEnd2 = false;
        int pos = ctx.Lex.Current.Position;
        if (ctx.Kind != Token.Colon)
        {
            e1 = ParseIndex(ctx, ref fromEnd1, false);
            if (e1.Type == typeof(int))
            {
                if (ctx.Kind == Token.RBra)
                {
                    ctx.MoveNext();
                    return Expression.Property(e, "Item", fromEnd1
                        ? Expression.New(indexCtor, e1, trueExpr)
                        : e1);
                }
            }
            else if (e1.Type != typeof(Date))
                throw Error("Lower bound must be a date or integer", ctx);
            else if (fromEnd1)
                throw Error("Relative indexes not supported for dates", pos);
            if (ctx.Kind != Token.Colon)
                throw Error(": expected in slice", ctx);
        }
        ctx.MoveNext();
        if (ctx.Kind != Token.RBra)
        {
            if (ctx.Kind == Token.Caret)
                pos = ctx.Lex.Current.Position;
            e2 = ParseIndex(ctx, ref fromEnd2, false);
            if (e2.Type != typeof(Date) && e2.Type != typeof(int))
                throw Error("Upper bound must be a date or integer", ctx);
            if (fromEnd2 && e2.Type == typeof(Date))
                throw Error("Relative indexes not supported for dates", pos);
            if (ctx.Kind != Token.RBra)
                throw Error("] expected in slice", ctx);
        }
        ctx.MoveNext();
        if (e1 == null && e2 == null)
            return e;
        if (e1 != null && e2 != null && e1.Type != e2.Type)
            throw Error("Both indexers must be of the same type", ctx);
        if (fromEnd1 || fromEnd2)
        {
            e1 = e1 != null
                ? Expression.New(indexCtor, e1, Expression.Constant(fromEnd1))
                : Expression.Constant(Index.Start);
            e2 = e2 != null
                ? Expression.New(indexCtor, e2, Expression.Constant(fromEnd2))
                : Expression.Constant(Index.End);
            return Expression.Property(e,
                typeof(Series).GetProperty("Item", new[] { typeof(Range) }),
                Expression.New(rangeCtor, e1, e2));
        }
        e1 ??= e2.Type == typeof(Date)
            ? Expression.Constant(Date.Zero)
            : Expression.Constant(0);
        e2 ??= e1.Type == typeof(Date)
            ? Expression.Constant(new Date(3000, 1, 1))
            : Expression.Constant(int.MaxValue);
        return Expression.Call(e,
            typeof(Series).GetMethod(nameof(Series.Slice), new[] { e1.Type, e2.Type }),
            e1, e2);
    }

    private static Expression ParseSeriesMethod(AstContext ctx)
    {
        (string method, int pos) = ctx.GetTextAndPos();
        (List<Expression> args, List<int> p) = ParseArguments(ctx);
        return method switch
        {
            "new" => args.Count < 2
                ? throw Error("NEW expects a vector and a list of series", ctx)
                : args[0].Type != typeof(Vector)
                ? throw Error("Vector expected", p[0])
                : args.Skip(1).Any(e => e.Type != typeof(Series))
                ? throw Error("NEW expects a vector and a list of series", ctx)
                : typeof(Series).Call(nameof(Series.Combine),
                    args[0], typeof(Series).Make(args.Skip(1))),
            _ => throw Error("Unknown method name", pos),
        };
    }

    private static Expression ParseSplineMethod(AstContext ctx)
    {
        (string method, int pos) = ctx.GetTextAndPos();
        if (method == "grid")
        {
            ctx.Skip2();
            Expression e1 = ParseLightConditional(ctx);
            if (!IsArithmetic(e1))
                throw Error("Lower bound must be double", ctx);
            ctx.CheckAndMoveNext(Token.Comma, "Comma expected");
            Expression e2 = ParseLightConditional(ctx);
            if (!IsArithmetic(e2))
                throw Error("Upper bound must be double", ctx);
            ctx.CheckAndMoveNext(Token.Comma, "Comma expected");
            Expression e3 = ParseLightConditional(ctx);
            if (e3.Type != typeof(int))
                throw Error("The number of segments must be an integer", ctx);
            ctx.CheckAndMoveNext(Token.Comma, "Comma expected");
            return typeof(VectorSpline).New(ToDouble(e1), ToDouble(e2), e3,
                ParseLambda(ctx, typeof(double), null, typeof(double)));

        }
        (List<Expression> a, List<int> _) = ParseArguments(ctx);
        return method switch
        {
            "new" => a.Count != 2 || a[0].Type != typeof(Vector) || a[1].Type != typeof(Vector)
                ? throw Error("Two vectors expected", ctx)
                : typeof(VectorSpline).New(a[0], a[1]),
            _ => throw Error("Unknown method name", pos),
        }; ;
    }

    private static Expression ParseModelMethod(AstContext ctx)
    {
        (string method, int pos) = ctx.GetTextAndPos();
        (List<Expression> a, List<int> p) = ParseArguments(ctx);
        return method switch
        {
            "compare" or "comp" => a.Count != 2 || a[0].Type != a[1].Type
                ? throw Error("Two arguments expected for comparison", ctx)
                : a[0].Type == typeof(Vector)
                ? typeof(Tuple<Vector, Vector>).New(a)
                : a[0].Type == typeof(ComplexVector)
                ? typeof(Tuple<ComplexVector, ComplexVector>).New(a)
                : a[0].Type == typeof(Series)
                ? typeof(Tuple<Series, Series>).New(a)
                : throw Error("Invalid argument type", ctx),
            "mvo" => a.Count < 2
                ? throw Error("Invalid number of parameters", ctx)
                : a[0].Type != typeof(Vector)
                ? throw Error("Vector expected", p[0])
                : a[1].Type != typeof(Matrix)
                ? throw Error("Covariance matrix expected", p[1])
                : a.Count >= 4 && a[2].Type == typeof(Vector) && a[3].Type == typeof(Vector)
                ? a.Skip(4).All(a => a.Type == typeof(Series))
                    ? typeof(Library.MVO.MvoModel).New(
                        a[0], a[1], a[2], a[3], typeof(Series).Make(a.Skip(4)))
                    : a.Skip(4).All(a => a.Type == typeof(string))
                    ? typeof(Library.MVO.MvoModel).New(
                        a[0], a[1], a[2], a[3], typeof(string).Make(a.Skip(4)))
                    : throw Error("A list of series was expected", p[^1])
                : (a.Skip(2).All(a => a.Type == typeof(Series))
                ? typeof(Library.MVO.MvoModel).New(a[0], a[1], typeof(Series).Make(a.Skip(2)))
                : a.Skip(2).All(a => a.Type == typeof(string))
                ? typeof(Library.MVO.MvoModel).New(a[0], a[1], typeof(string).Make(a.Skip(2)))
                : throw Error("A list of series was expected", p[^1])),
            _ => throw Error("Unknown method name", pos),
        };
    }

    private static Expression ParseVectorMethod(AstContext ctx)
    {
        (string method, int pos) = ctx.GetTextAndPos();
        if (method == "new")
            return ParseNewVectorLambda(ctx, typeof(Vector));
        (List<Expression> a, _) = ParseArguments(ctx);
        return method switch
        {
            "nrandom" => a.Count != 1 || a[0].Type != typeof(int)
                ? throw Error("Vector size expected", ctx)
                : typeof(Vector).New(a.AddNormalRandom()),
            "random" => a.Count != 1 || a[0].Type != typeof(int)
                ? throw Error("Vector size expected", ctx)
                : typeof(Vector).New(a.AddRandom()),
            "zero" => a.Count != 1 || a[0].Type != typeof(int)
                ? throw Error("Vector size expected", ctx)
                : typeof(Vector).New(a[0]),
            "ones" => a.Count != 1 || a[0].Type != typeof(int)
                ? throw Error("Vector size expected", ctx)
                : typeof(Vector).New(a.AddExp(Expression.Constant(1.0))),
            _ => throw Error("Unknown method name", pos),
        };
    }

    private static Expression ParseComplexVectorMethod(AstContext ctx)
    {
        (string method, int pos) = ctx.GetTextAndPos();
        if (method == "new")
            return ParseNewVectorLambda(ctx, typeof(ComplexVector));
        (List<Expression> e, _) = ParseArguments(ctx);
        return method switch
        {
            "nrandom" => e.Count != 1 || e[0].Type != typeof(int)
                ? throw Error("Vector size expected", ctx)
                : typeof(ComplexVector).New(e.AddNormalRandom()),
            "random" => e.Count != 1 || e[0].Type != typeof(int)
                ? throw Error("Vector size expected", ctx)
                : typeof(ComplexVector).New(e.AddRandom()),
            "zero" => e.Count != 1 || e[0].Type != typeof(int)
                ? throw Error("Vector size expected", ctx)
                : typeof(ComplexVector).New(e[0]),
            "from" => e.Count == 1 && e[0].Type == typeof(Vector)
                ? typeof(ComplexVector).New(e[0])
                : e.Count == 2 || e[0].Type == typeof(Vector)
                    || e[1].Type == typeof(Vector)
                ? typeof(ComplexVector).New(e[0], e[1])
                : throw Error("One or two vectors expected", ctx),
            _ => throw Error("Unknown method name", pos),
        };
    }

    private static Expression ParseMatrixMethod(AstContext ctx)
    {
        (string method, int pos) = ctx.GetTextAndPos();
        if (method == "new")
            return ParseNewMatrixLambda(ctx);
        (List<Expression> a, _) = ParseArguments(ctx);
        return method switch
        {
            "rows" => a.Any(e => e.Type != typeof(Vector))
                ? throw Error("List of vectors expected", ctx)
                : typeof(Matrix).New(typeof(Vector).Make(a)),
            "cols" => a.Any(e => e.Type != typeof(Vector))
                ? throw Error("List of vectors expected", ctx)
                : Expression.Call(typeof(Matrix).New(typeof(Vector).Make(a)),
                    typeof(Matrix).Get(nameof(Matrix.Transpose))),
            "diag" => a.Count == 1 && a[0].Type == typeof(Vector)
                ? typeof(Matrix).New(a[0])
                : a.Count > 1 && a.All(IsArithmetic)
                ? typeof(Matrix).New(typeof(Vector).New(
                    typeof(double).Make(a.Select(ToDouble))))
                : throw Error("Vector expected", ctx),
            "eye" or "i" => a.Count != 1 || a[0].Type != typeof(int)
                ? throw Error("Matrix size expected", ctx)
                : Expression.Call(typeof(Matrix).Get(nameof(Matrix.Identity)), a[0]),
            "random" => CheckMatrixSize(a)
                ? typeof(Matrix).New(a.AddRandom())
                : throw Error("Matrix size expected", ctx),
            "nrandom" => CheckMatrixSize(a)
                ? typeof(Matrix).New(a.AddNormalRandom())
                : throw Error("Matrix size expected", ctx),
            "lrandom" => CheckMatrixSize(a)
                ? //Expression.Convert(Expression.Convert(
                    typeof(LMatrix).New(a.AddRandom())//,
                    //typeof(double[,])), typeof(Matrix))
                : throw Error("Matrix size expected", ctx),
            "lnrandom" or "nlrandom" => CheckMatrixSize(a)
                ? //Expression.Convert(Expression.Convert(
                    typeof(LMatrix).New(a.AddNormalRandom())//,
                    //typeof(double[,])), typeof(Matrix))
                : throw Error("Matrix size expected", ctx),
            "zero" or "zeros" => CheckMatrixSize(a)
                ? typeof(Matrix).New(a)
                : throw Error("Matrix size expected", ctx),
            "cov" or "covariance" => a.Any(e => e.Type != typeof(Series))
                ? throw Error("List of series expected", ctx)
                : Expression.Call(
                    typeof(Series<Date>).Get(nameof(Series.CovarianceMatrix)),
                    typeof(Series).Make(a)),
            "corr" or "correlation" => a.Any(e => e.Type != typeof(Series))
                ? throw Error("List of series expected", ctx)
                : Expression.Call(
                    typeof(Series<Date>).Get(nameof(Series.CorrelationMatrix)),
                    typeof(Series).Make(a)),
            _ => throw Error("Unknown method name", pos),
        };

        static bool CheckMatrixSize(List<Expression> a) =>
            a.Count is 1 or 2 || a.All(e => e.Type == typeof(int));
    }

    private static Expression ParseNewMatrixLambda(AstContext ctx)
    {
        // Skip method name and left parenthesis.
        ctx.Skip2();
        Expression e1 = ParseLightConditional(ctx);
        if (e1.Type != typeof(int))
            throw Error($"Rows must be integer", ctx);
        ctx.CheckAndMoveNext(Token.Comma, "Comma expected");
        Expression e2 = ParseLightConditional(ctx);
        if (e2.Type != typeof(int))
            throw Error($"Columns must be integer", ctx);
        ctx.CheckAndMoveNext(Token.Comma, "Comma expected");
        return typeof(Matrix).New(e1, e2,
            ParseLambda(ctx, typeof(int), typeof(int), typeof(double)));
    }

    private static Expression ParseNewVectorLambda(AstContext ctx, Type type)
    {
        // Skip method name and left parenthesis.
        ctx.Skip2();
        Expression e1 = ParseLightConditional(ctx);
        if (e1.Type == typeof(Vector))
        {
            List<Expression> args = new() { e1 };
            while (ctx.Kind == Token.Comma)
            {
                ctx.MoveNext();
                args.Add(ParseLightConditional(ctx));
            }
            // Check and skip right parenthesis.
            ctx.CheckAndMoveNext(Token.RPar, "Right parenthesis expected after function call");
            return args.Count == 1 || args.Skip(1).Any(e => e.Type != typeof(Vector))
                ? throw Error("NEW expects a list of vectors", ctx)
                : typeof(Vector).Call(nameof(Vector.Combine),
                    args[0], typeof(Vector).Make(args.Skip(1)));
        }
        if (e1.Type != typeof(int))
            throw Error($"Vector size must be integer", ctx);
        ctx.CheckAndMoveNext(Token.Comma, "Comma expected");
        Type retType = type == typeof(Vector) ? typeof(double) : typeof(Complex);
        return ctx.Kind == Token.Id
            ? type.New(e1, ParseLambda(ctx, typeof(int), null, retType))
            : type.New(e1, ParseLambda(ctx, typeof(int), type, retType));
    }

    private static Expression ParseMethod(AstContext ctx, Expression e)
    {
        string meth = ctx.Lex.Current.Text;
        if (!methods.TryGetValue(e.Type, out Dictionary<string, MethodInfo> dict) ||
            !dict.TryGetValue(meth, out MethodInfo mInfo))
            throw Error($"Invalid method: {meth}", ctx);
        ParameterInfo[] paramInfo = mInfo.GetParameters();
        Type firstParam = paramInfo[0].ParameterType;
        if (paramInfo.Length == 2 &&
            paramInfo[1].ParameterType.IsAssignableTo(typeof(Delegate)))
        {
            // This is a zip or reduce method call.
            ctx.Skip2();
            Expression e1 = ParseConditional(ctx);
            if (e1.Type != firstParam)
                if (firstParam == typeof(double) && IsArithmetic(e1))
                    e1 = ToDouble(e1);
                else if (firstParam == typeof(Complex) && IsArithmetic(e1))
                    e1 = Expression.Convert(ToDouble(e1), typeof(Complex));
                else
                    throw Error($"{firstParam.Name} expected", ctx);
            ctx.CheckAndMoveNext(Token.Comma, "Comma expected");
            Type[] genTypes = paramInfo[1].ParameterType.GenericTypeArguments;
            Expression λ = ParseLambda(ctx, genTypes[0], genTypes[1], genTypes[^1]);
            return Expression.Call(e, mInfo, e1, λ);
        }
        if (firstParam.IsAssignableTo(typeof(Delegate)))
        {
            // Skip method name and left parenthesis.
            ctx.Skip2();
            Type[] genTypes = firstParam.GenericTypeArguments;
            return Expression.Call(e, mInfo, ParseLambda(ctx, genTypes[0], null, genTypes[^1]));
        }
        if (firstParam == typeof(Index))
        {
            // Skip method name and left parenthesis.
            ctx.Skip2();
            bool fromEnd = false;
            Expression e1 = ParseIndex(ctx, ref fromEnd);
            ctx.CheckAndMoveNext(Token.RPar, "Right parenthesis expected after function call");
            return Expression.Call(e, mInfo,
                Expression.New(indexCtor, e1, Expression.Constant(fromEnd)));
        }
        (List<Expression> a, var p) = ParseArguments(ctx);
        if (firstParam == typeof(Series[]) || firstParam == typeof(Vector[]))
            return a.Any(a => a.Type != e.Type)
                ? throw Error(e.Type == typeof(Series) ?
                    "Series list expected" : "Vector list expected", ctx)
                : Expression.Call(e, mInfo, e.Type.Make(a));
        if (a.Count != paramInfo.Length)
            throw Error("Invalid number of arguments", ctx);
        if (a[0].Type != firstParam)
            if (firstParam == typeof(Date))
                throw Error("Date expression expected", p[0]);
            else if (firstParam == typeof(Series<Date>))
            {
                if (a[0].Type != typeof(Series))
                    throw Error("Series expected", p[0]);
            }
            else if (firstParam == typeof(int))
                throw Error("Integer expression expected", p[0]);
            else if (firstParam == typeof(Complex))
                a[0] = IsArithmetic(a[0])
                    ? Expression.Convert(a[0], typeof(Complex))
                    : throw Error("Complex expression expected", p[0]);
            else
                a[0] = IsArithmetic(a[0])
                    ? ToDouble(a[0])
                    : throw Error("Real expression expected", p[0]);
        return Expression.Call(e, mInfo, a[0]);
    }

    private static Expression ParseIndex(AstContext ctx, ref bool fromEnd, bool check = true)
    {
        if (ctx.Kind == Token.Caret)
        {
            fromEnd = true;
            ctx.MoveNext();
        }
        Expression e = ParseLightConditional(ctx);
        if (check && e.Type != typeof(int))
            throw Error("Index must be integer", ctx);
        return e;
    }

    private static Expression ParseLambda(AstContext ctx, Type t1, Type t2, Type retType,
        bool isLast = true)
    {
        try
        {
            if (t2 == null)
            {
                if (ctx.Kind != Token.Id)
                    throw Error("Lambda parameter name expected", ctx);
                ctx.LambdaParameter = Expression.Parameter(t1, ctx.Lex.Current.Text);
                ctx.MoveNext();
            }
            else
            {
                ctx.CheckAndMoveNext(Token.LPar, "Lambda parameters expected");
                if (ctx.Kind != Token.Id)
                    throw Error("First lambda parameter name expected", ctx);
                ctx.LambdaParameter = Expression.Parameter(t1, ctx.Lex.Current.Text);
                ctx.MoveNext();
                ctx.CheckAndMoveNext(Token.Comma, "Comma expected");
                if (ctx.Kind != Token.Id)
                    throw Error("Second lambda parameter name expected", ctx);
                ctx.LambdaParameter2 = Expression.Parameter(t2, ctx.Lex.Current.Text);
                ctx.MoveNext();
                ctx.CheckAndMoveNext(Token.RPar, ") expected in lambda header");
            }
            ctx.CheckAndMoveNext(Token.Arrow, "=> expected");
            Expression body = ParseConditional(ctx);
            if (body.Type != retType)
                body = retType == typeof(Complex) && IsArithmetic(body)
                    ? Expression.Convert(body, typeof(Complex))
                    : retType == typeof(double) && IsArithmetic(body)
                    ? ToDouble(body)
                    : throw Error($"Expected return type is {retType.Name}", ctx);
            if (isLast)
                ctx.CheckAndMoveNext(Token.RPar, "Right parenthesis expected after function call");
            else
                ctx.CheckAndMoveNext(Token.Comma, "Comma expected");
            return ctx.LambdaParameter2 != null
                ? Expression.Lambda(body, ctx.LambdaParameter, ctx.LambdaParameter2)
                : Expression.Lambda(body, ctx.LambdaParameter);
        }
        finally
        {
            ctx.LambdaParameter = null;
            ctx.LambdaParameter2 = null;
        }
    }

    private static Expression ParseProperty(AstContext ctx, Expression e)
    {
        string prop = ctx.Lex.Current.Text;
        if (allProps.TryGetValue(e.Type, out var dict) &&
            dict.TryGetValue(prop, out MethodInfo mInfo))
        {
            ctx.MoveNext();
            return Expression.Call(e, mInfo);
        }
        throw Error($"Invalid property: {prop}", ctx);
    }

    /// <summary>Parses a global function call.</summary>
    /// <param name="ctx">The compiling call.</param>
    /// <returns>An expression representing the function call.</returns>
    private static Expression ParseFunction(AstContext ctx)
    {
        var (function, pos) = ctx.GetTextAndPos();
        if (function == "solve")
        {
            ctx.Skip2();
            Expression λf = ParseLambda(ctx, typeof(double), null, typeof(double), false);
            Expression λdf = ParseLambda(ctx, typeof(double), null, typeof(double), false);
            var (a1, p1) = CollectArguments(ctx);
            a1.Insert(0, λdf);
            a1.Insert(0, λf);
            if (!IsArithmetic(a1[2]))
                throw Error("The initial guess must be numeric", p1[0]);
            a1[2] = ToDouble(a1[2]);
            if (a1.Count == 3)
                a1.Add(Expression.Constant(1e-9));
            else if (!IsArithmetic(a1[3]))
                throw Error("Accuracy must be real", p1[1]);
            a1[3] = ToDouble(a1[3]);
            if (a1.Count == 4)
                a1.Add(Expression.Constant(100));
            else if (a1[4].Type != typeof(int))
                throw Error("Maximum number of iterations must be integer", p1[2]);
            return a1.Count > 5
                ? throw Error("Too many arguments", ctx)
                : Expression.Call(typeof(Solver).Get("Solve"), a1);
        }
        (List<Expression> a, List<int> p) = ParseArguments(ctx);
        switch (function)
        {
            case "round":
                return a.Count is < 1 or > 2
                    ? throw Error("Function 'round' requires 2 or 3 arguments", pos)
                    : !IsArithmetic(a[0])
                    ? throw Error("First argument must be numeric", ctx)
                    : a.Count == 2 && a[1].Type != typeof(int)
                    ? throw Error("Second argument must be integer", ctx)
                    : a.Count == 1
                    ? Expression.Call(typeof(Math).GetMethod("Round", doubleArg), ToDouble(a[0]))
                    : typeof(Math).Call(nameof(Math.Round), ToDouble(a[0]), a[1]);
            case "iff":
                {
                    if (a.Count != 3)
                        throw Error("Function 'iff' requires 3 arguments", pos);
                    if (a[0].Type != typeof(bool))
                        throw Error("First argument must be boolean", p[0]);
                    Expression a2 = a[1], a3 = a[2];
                    return DifferentTypes(ref a2, ref a3)
                        ? throw Error("Second and third arguments must have the same type", p[2])
                        : Expression.Condition(a[0], a2, a3);
                }
            case "min":
            case "max":
                {
                    string fName = function[^1] == 'n' ? nameof(Math.Min) : nameof(Math.Max);
                    return a.Count == 2
                        && a[0].Type == typeof(Date) && a[1].Type == typeof(Date)
                        ? typeof(Date).Call(fName, a[0], a[1])
                        : a.Count != 2 || !IsArithmetic(a[0]) || !IsArithmetic(a[1])
                        ? throw Error("Arguments must be numeric", ctx)
                        : a[0].Type == typeof(int) && a[1].Type == typeof(int)
                        ? typeof(Math).Call(fName, a[0], a[1])
                        : typeof(Math).Call(fName, ToDouble(a[0]), ToDouble(a[1]));
                }
            case "beta":
                return a.Count != 2 || !IsArithmetic(a[0]) || !IsArithmetic(a[1])
                    ? throw Error("Arguments must be numeric", ctx)
                    : typeof(F).Call(nameof(F.Beta), ToDouble(a[0]), ToDouble(a[1]));
            case "compare":
            case "comp":
                return a.Count != 2 || a[0].Type != a[1].Type
                    ? throw Error("Two arguments expected for comparison", ctx)
                    : a[0].Type == typeof(Vector)
                    ? typeof(Tuple<Vector, Vector>).New(a)
                    : a[0].Type == typeof(ComplexVector)
                    ? typeof(Tuple<ComplexVector, ComplexVector>).New(a)
                    : a[0].Type == typeof(Series)
                    ? typeof(Tuple<Series, Series>).New(a)
                    : throw Error("Invalid argument type", ctx);
            case "complex":
                return a.Count == 1 && IsArithmetic(a[0])
                    ? Expression.Convert(a[0], typeof(Complex))
                    : a.Count != 2 || !AreArithmeticTypes(a[0], a[1])
                    ? throw Error("Arguments must be numeric", ctx)
                    : typeof(Complex).New(ToDouble(a[0]), ToDouble(a[1]));
            case "polyeval":
                return ParsePolyMethod(nameof(Polynomials.PolyEval));
            case "polyderiv":
            case "polyderivative":
                return ParsePolyMethod(nameof(Polynomials.PolyDerivative));
            case "polysolve":
                {
                    MethodInfo info = typeof(Polynomials).GetMethod(
                        nameof(Polynomials.PolySolve),
                        new[] { typeof(Vector) });
                    return a.Count > 0 && a.All(IsArithmetic)
                        ? Expression.Call(info, typeof(Vector).New(
                            typeof(double).Make(a.Select(ToDouble).ToArray())))
                        : a.Count != 1 || a[0].Type != typeof(Vector)
                        ? throw Error("Argument must be a vector", ctx)
                        : Expression.Call(info, a[0]);
                }
        }
        if (a.Count == 1 && a[0].Type == typeof(Complex))
        {
            MethodInfo info = typeof(Complex).GetMethod(function,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase,
                new[] { typeof(Complex) });
            return info is null
                ? throw Error("Invalid function name", pos)
                : Expression.Call(info, a[0]);
        }
        return !functions.TryGetValue(function, out MethodInfo mInfo)
            ? throw Error("Invalid function name", pos)
            : a.Count != 1 || !IsArithmetic(a[0])
            ? throw Error("Argument must be numeric", ctx)
            : Expression.Call(mInfo, ToDouble(a[0]));

        Expression ParsePolyMethod(string methodName)
        {
            if (a.Count < 2)
                throw Error($"{methodName} requires two arguments", pos);
            if (IsArithmetic(a[0]))
                a[0] = ToDouble(a[0]);
            else if (a[0].Type != typeof(Complex))
                throw Error($"First argument of {methodName} must be numeric", p[0]);
            if (a[1].Type != typeof(Vector))
                if (a.Skip(1).All(IsArithmetic))
                {
                    a[1] = typeof(Vector).New(
                        typeof(double).Make(a.Skip(1).Select(ToDouble).ToList()));
                    a.RemoveRange(2, a.Count - 2);
                }
                else
                    throw Error($"Second argument of {methodName} must be a vector", p[1]);
            return typeof(Polynomials).Call(methodName, a[0], a[1]);
        }
    }

    private static (List<Expression>, List<int>) ParseArguments(AstContext ctx)
    {
        // Skip method name and left parenthesis.
        ctx.Skip2();
        return CollectArguments(ctx);
    }

    private static (List<Expression>, List<int>) CollectArguments(AstContext ctx)
    {
        List<Expression> arguments = new();
        List<int> positions = new();
        for (; ; ctx.MoveNext())
        {
            arguments.Add(ParseConditional(ctx));
            positions.Add(ctx.Lex.Current.Position);
            if (ctx.Kind != Token.Comma)
                break;
        }
        // Check and skip right parenthesis.
        ctx.CheckAndMoveNext(Token.RPar, "Right parenthesis expected after function call");
        return (arguments, positions);
    }

    private static Expression ParseVectorLiteral(AstContext ctx)
    {
        ctx.MoveNext();
        List<Expression> items = new();
        int period = 0, lastPeriod = 0, vectors = 0, matrices = 0;
        for (; ; )
        {
            Expression e = ParseLightConditional(ctx);
            if (IsArithmetic(e))
                items.Add(ToDouble(e));
            else if (e.Type == typeof(Vector))
            {
                if (period != 0 && matrices == 0)
                    throw Error("Invalid vector in matrix constructor", ctx);
                vectors++;
                items.Add(e);
            }
            else if (e.Type == typeof(Matrix))
            {
                if (period > 1 || vectors + matrices != items.Count || items.Count >= 2)
                    throw Error("Invalid matrix concatenation", ctx);
                matrices++;
                items.Add(e);
            }
            else
                throw Error("Vector item must be numeric", ctx);
            if (ctx.Kind == Token.Semicolon)
            {
                if (period == 0)
                    period = lastPeriod = items.Count;
                else if (items.Count - lastPeriod != period)
                    throw Error("Inconsistent matrix size", ctx);
                else
                    lastPeriod = items.Count;
                ctx.MoveNext();
            }
            else if (ctx.Kind == Token.Comma)
                ctx.MoveNext();
            else if (ctx.Kind == Token.RBra)
            {
                ctx.MoveNext();
                break;
            }
        }
        if (matrices > 0)
            return period > 1 || vectors + matrices != items.Count || items.Count > 2
                ? throw Error("Invalid matrix concatenation", ctx)
                : typeof(Matrix).Call(period == 0 ? nameof(Matrix.HCat) : nameof(Matrix.VCat),
                    items[0], items[1]);
        if (vectors > 0)
        {
            for (int i = 0; vectors < items.Count; vectors++)
            {
                for (; items[i].Type == typeof(Vector); i++) ;
                int j = i + 1;
                for (; j < items.Count && items[j].Type != typeof(Vector); j++) ;
                int count = j - i;
                if (count == 1 && items.Count == 2)
                    return typeof(Vector).New(items[0], items[1]);
                items[i] = typeof(Vector).New(typeof(double).Make(items.GetRange(i, count)));
                items.RemoveRange(i + 1, count - 1);
            }
            return typeof(Vector).New(typeof(Vector).Make(items));
        }
        if (period != 0 && items.Count - lastPeriod != period)
            throw Error("Inconsistent matrix size", ctx);
        Expression args = typeof(double).Make(items);
        return period != 0
            ? typeof(Matrix).New(
                Expression.Constant(items.Count / period), Expression.Constant(period), args)
            : typeof(Vector).New(args);
    }

    private static Expression ParseVariable(AstContext ctx)
    {
        Lexeme lex = ctx.Lex.Current;
        ctx.MoveNext();
        // Check lambda parameters when present.
        if (ctx.LambdaParameter != null)
        {
            if (lex.Is(ctx.LambdaParameter.Name))
                return ctx.LambdaParameter;
            if (ctx.LambdaParameter2 != null &&
                lex.Is(ctx.LambdaParameter2.Name))
                return ctx.LambdaParameter2;
        }
        // Check the local scope.
        if (ctx.Locals.TryGetValue(lex.Text, out ParameterExpression local))
            return local;
        // Check macro definitions.
        Definition def = ctx.Source.GetDefinition(lex.Text);
        if (def != null)
        {
            if (ctx.ParsingDefinition)
                ctx.References.Add(def);
            return def.Expression;
        }
        // Check the global scope.
        object val = ctx.ParsingDefinition
            ? ctx.Source.GetPersistedValue(lex.Text)
            : ctx.Source[lex.Text];
        if (val != null)
            return val switch
            {
                double dv => Expression.Constant(dv),
                int iv => Expression.Constant(iv),
                bool bv => Expression.Constant(bv),
                string sv => Expression.Constant(sv),
                _ => AstContext.GetFromDataSource(lex.Text, val.GetType())
            };
        switch (lex.Text.ToLower())
        {
            case "e": return Expression.Constant(Math.E);
            case "i": return Expression.Constant(Complex.ImaginaryOne);
            case "pi": return Expression.Constant(Math.PI);
            case "today": return Expression.Constant(Date.Today);
            case "pearl": return Expression.Call(typeof(F).Get(nameof(F.Austra)));
            case "random": return Expression.Call(typeof(F).GetMethod(nameof(F.Random)));
            case "nrandom": return Expression.Call(typeof(F).GetMethod(nameof(F.NRandom)));
        }
        if (Lexer.TryParseMonthYear(lex.Text, out Date d))
            return Expression.Constant(d);
        // Check if we tried to reference a SET variable in a DEF.
        if (ctx.ParsingDefinition && ctx.Source[lex.Text] != null)
            throw Error("SET variables cannot be used in persistent definitions", lex);
        throw Error($"Unknown variable: {lex.Text}", lex);
    }
}
