using System.Runtime.InteropServices;

namespace Austra.Parser;

/// <summary>Syntactic and lexical analysis for AUSTRA.</summary>
internal sealed partial class Parser
{
    /// <summary>Compiles a block expression.</summary>
    /// <returns>A block expression.</returns>
    public Expression ParseStatement()
    {
        if (kind == Token.Set)
        {
            List<Expression> setExpressions = new(8);
            do
            {
                Move();
                if (kind != Token.Id)
                    throw Error("Left side variable expected");
                int namePos = start;
                string leftValue = id;
                if (pendingSets.ContainsKey(leftValue))
                    throw Error($"{leftValue} already in use", namePos);
                Move();
                if (kind == Token.Eof || kind == Token.Comma)
                    source[leftValue] = null;
                else
                {
                    CheckAndMove(Token.Eq, "= expected");
                    // Always allow deleting a session variable.
                    if (source.GetDefinition(leftValue) != null)
                        throw Error($"{leftValue} already in use", namePos);
                    setExpressions.Add(ParseFormula(true, leftValue, false));
                    if (setExpressions[^1] is BinaryExpression { NodeType: ExpressionType.Assign } be
                        && be.Right is UnaryExpression { NodeType: ExpressionType.Convert } ue)
                        pendingSets[leftValue] = ue.Operand;
                }
            }
            while (kind == Token.Comma);
            return kind != Token.Eof
                ? throw Error("Extra input after expression")
                : setExpressions.Count == 0
                ? Expression.Constant(null)
                : Expression.Block(setExpressions);
        }
        else
            return ParseFormula(true, "", true);
    }

    /// <summary>Parses a block expression without generating code.</summary>
    /// <returns>The type of the block expression.</returns>
    public Type[] ParseType()
    {
        // Check first for a definition header and skip it.
        if (kind == Token.Def)
        {
            Move();
            CheckAndMove(Token.Id, "Definition name expected");
            if (kind == Token.Colon)
            {
                Move();
                CheckAndMove(Token.Str, "Definition description expected");
            }
            CheckAndMove(Token.Eq, "= expected");
            return [ParseFormula(false, "", true).Type];
        }
        // Check now for a set header and skip it.
        else if (kind == Token.Set)
        {
            List<Type> result = new(8);
            do
            {
                Move();
                CheckAndMove(Token.Id, "Left side variable expected");
                if (kind != Token.Eof && kind != Token.Comma)
                {
                    CheckAndMove(Token.Eq, "= expected");
                    result.Add(ParseFormula(false, "", false).Type);
                }
            } while (kind == Token.Comma);
            return kind != Token.Eof
                ? throw Error("Extra input after expression")
                : [.. result];
        }
        return [ParseFormula(false, "", true).Type];
    }

    /// <summary>Parse the formula up to a position and return local variables.</summary>
    /// <param name="position">Last position to parse.</param>
    /// <param name="parsingHeader">Are we inside a lambda header?</param>
    /// <returns>The list of LET variables and any possible active lambda parameter.</returns>
    public List<Member> ParseContext(int position, out bool parsingHeader)
    {
        abortPosition = position;
        try
        {
            ParseType();
        }
        catch { /* Ignore */ }
        finally
        {
            abortPosition = int.MaxValue;
        }
        parsingHeader = parsingLambdaHeader;
        List<Member> result;
        if (parsingHeader)
            result = [];
        else
        {
            result = new(locals.Count + 2);
            foreach (var local in locals)
                result.Add(new(local.Key, "Local variable"));
            if (lambdaParameter != null)
            {
                if (!string.IsNullOrEmpty(lambdaParameter.Name))
                    result.Add(new(lambdaParameter.Name, "Lambda parameter"));
                if (lambdaParameter2 != null && !string.IsNullOrEmpty(lambdaParameter2.Name))
                    result.Add(new(lambdaParameter2.Name, "Lambda parameter"));
            }
        }
        parsingLambdaHeader = false;
        lambdaParameter = lambdaParameter2 = null;
        return result;
    }

    /// <summary>Parses a definition and adds it to the source.</summary>
    /// <returns>A new definition, on success.</returns>
    public Definition ParseDefinition()
    {
        CheckAndMove(Token.Def, "DEF expected");
        if (kind != Token.Id)
            throw Error("Definition name expected");
        string defName = id;
        if (source.GetDefinition(defName) != null ||
            source[defName] != null)
            throw Error($"{defName} already in use");
        Move();
        string description = "";
        if (kind == Token.Colon)
        {
            Move();
            if (kind != Token.Str)
                throw Error("Definition description expected");
            description = id;
            Move();
        }
        CheckAndMove(Token.Eq, "= expected");
        int first = start;
        isParsingDefinition = true;
        Expression e = ParseFormula(false, "", true);
        if (e.Type == typeof(Series))
            e = typeof(Series).Call(e, nameof(Series.SetName), Expression.Constant(defName));
        Definition def = new(defName, text[first..], description, e);
        foreach (Definition referenced in references)
            referenced.Children.Add(def);
        return def;
    }

    /// <summary>Checks if the current token is the <c>SET</c> keyword.</summary>
    /// <returns><see langword="true"/> if the current token is <c>SET</c>.</returns>
    public bool IsSet() => kind == Token.Set;

    /// <summary>Compiles a block expression.</summary>
    /// <param name="forceCast">Whether to force a cast to object.</param>
    /// <param name="leftValue">When not empty, contains a variable name.</param>
    /// <param name="checkEof">Whether to check for extra input.</param>
    /// <returns>A block expression.</returns>
    private Expression ParseFormula(bool forceCast, string leftValue, bool checkEof)
    {
        if (kind == Token.Let)
        {
            do
            {
                Move();
                if (kind != Token.Id)
                    throw Error("Identifier expected");
                string localId = id;
                Move();
                CheckAndMove(Token.Eq, "= expected");
                Expression init = ParseConditional();
                ParameterExpression le = Expression.Variable(init.Type, localId);
                topLocals.Add(le);
                topExpressions.Add(Expression.Assign(le, init));
                locals[localId] = le;
            }
            while (kind == Token.Comma);
            CheckAndMove(Token.In, "IN expected");
        }
        Expression rvalue = ParseConditional();
        if (forceCast)
            rvalue = Expression.Convert(rvalue, typeof(object));
        if (leftValue != "")
            rvalue = source.SetExpression(leftValue, rvalue);
        topExpressions.Add(rvalue);
        return checkEof && kind != Token.Eof
            ? throw Error("Extra input after expression")
            : topLocals.Count == 0 && topExpressions.Count == 1
            ? topExpressions[0]
            : Expression.Block(topLocals, topExpressions);
    }

    /// <summary>Compiles a ternary conditional expression.</summary>
    private Expression ParseConditional()
    {
        if (kind != Token.If)
            return ParseDisjunctionConjunction();
        Move();
        Expression c = ParseDisjunctionConjunction();
        if (c.Type != typeof(bool))
            throw Error("Condition must be boolean");
        CheckAndMove(Token.Then, "THEN expected");
        Expression e1 = ParseConditional();
        CheckAndMove(Token.Else, "ELSE expected");
        Expression e2 = ParseConditional();
        return DifferentTypes(ref e1, ref e2)
            ? throw Error("Conditional operands are not compatible")
            : Expression.Condition(c, e1, e2);
    }

    /// <summary>Compiles a ternary conditional expression not returning a boolean.</summary>
    private Expression ParseLightConditional() =>
        kind == Token.If ? ParseConditional() : ParseAdditiveMultiplicative();

    /// <summary>Compiles an OR/AND expression.</summary>
    private Expression ParseDisjunctionConjunction()
    {
        for (Expression? e1 = null; ; Move())
        {
            int orLex = start;
            Expression e2 = ParseLogicalFactor();
            while (kind == Token.And)
            {
                int andLex = start;
                Move();
                Expression e3 = ParseLogicalFactor();
                e2 = e2.Type != typeof(bool) || e3.Type != typeof(bool)
                    ? throw Error("AND operands must be boolean", andLex)
                    : Expression.AndAlso(e2, e3);
            }
            e1 = e1 is null
                ? e2
                : e1.Type != typeof(bool) || e2.Type != typeof(bool)
                ? throw Error("OR operands must be boolean", orLex)
                : Expression.OrElse(e1, e2);
            if (kind != Token.Or)
                return e1;
        }
    }

    /// <summary>Compiles a [NOT] comparison expression.</summary>
    private Expression ParseLogicalFactor()
    {
        if (kind == Token.Not)
        {
            int notLex = start;
            Move();
            Expression e = ParseLogicalFactor();
            return e.Type != typeof(bool)
                ? throw Error("NOT operand must be boolean", notLex)
                : Expression.Not(e);
        }
        Expression e1 = ParseAdditiveMultiplicative();
        (Token opKind, int pos) = (kind, start);
        switch (opKind)
        {
            case Token.Eq:
            case Token.Ne:
                {
                    Move();
                    Expression e2 = ParseAdditiveMultiplicative();
                    return DifferentTypes(ref e1, ref e2) && !(IsMatrix(e1) && IsMatrix(e2))
                        ? throw Error("Equality operands are not compatible", pos)
                        : opKind == Token.Eq ? Expression.Equal(e1, e2)
                        : Expression.NotEqual(e1, e2);
                }

            case Token.Lt:
            case Token.Gt:
            case Token.Le:
            case Token.Ge:
                {
                    Move();
                    Expression e2 = ParseAdditiveMultiplicative();
                    if (e1.Type != e2.Type)
                    {
                        if (!IsArithmetic(e1) || !IsArithmetic(e2))
                            throw Error("Comparison operators are not compatible", pos);
                        (e1, e2) = (ToDouble(e1), ToDouble(e2));
                    }
                    try
                    {
                        if (IsArithmetic(e2))
                        {
                            Token op2 = kind;
                            if ((opKind == Token.Lt || opKind == Token.Le) &&
                                (op2 == Token.Lt || op2 == Token.Le) ||
                                (opKind == Token.Gt || opKind == Token.Ge) &&
                                (op2 == Token.Gt || op2 == Token.Ge))
                            {
                                Move();
                                Expression e3 = ParseAdditiveMultiplicative();
                                if (!IsArithmetic(e3))
                                    throw Error("Upper bound must be numeric");
                                if (e3.Type != e2.Type)
                                    if (e3.Type == typeof(int))
                                        e3 = IntToDouble(e3);
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

    /// <summary>
    /// Parses expressions which combine addition, subtraction, multiplication, and division.
    /// </summary>
    /// <remarks>
    /// Most algebraic optimizations are performed by this method.
    /// </remarks>
    /// <returns>The expression tree corresponding to the text.</returns>
    private Expression ParseAdditiveMultiplicative()
    {
        (Token opAdd, int opAPos) = (default, default);
        for (Expression? e1 = null; ; Move())
        {
            Expression e2 = ParseUnary();
            while ((uint)(kind - Token.Times) <= (Token.Mod - Token.Times))
            {
                (Token opMul, int opMPos) = (kind, start);
                Move();
                Expression e3 = ParseUnary();
                if (opMul == Token.Backslash)
                    e2 = e2.Type != typeof(Matrix)
                        ? throw Error("First operand must be a matrix", opMPos)
                        : e3.Type != typeof(Vector) && e3.Type != typeof(Matrix)
                        ? throw Error("Second operand must be a vector or a matrix", opMPos)
                        : typeof(Matrix).Call(e2, nameof(Matrix.Solve), e3);
                else if (opMul == Token.PointTimes || opMul == Token.PointDiv)
                    e2 = e2.Type == e3.Type && e2.Type.IsAssignableTo(
                            typeof(IPointwiseOperators<>).MakeGenericType(e2.Type))
                        ? e2.Type.Call(e2, opMul == Token.PointTimes
                            ? nameof(Vector.PointwiseMultiply) : nameof(Vector.PointwiseDivide),
                            e3)
                        : throw Error("Invalid operator", opMPos);
                else
                {
                    if (e2.Type != e3.Type)
                        (e2, e3) = (ToDouble(e2), ToDouble(e3));
                    try
                    {
                        // Try to optimize matrix transpose multiplying a vector.
                        e2 = opMul == Token.Times && e2.Type == typeof(Matrix)
                            ? (e3.Type == typeof(Vector) && e2 is MethodCallExpression
                            { Method.Name: nameof(Matrix.Transpose) } mca
                                ? Expression.Call(mca.Object, MatrixTransposeMultiply, e3)
                                : e3.Type == typeof(Matrix) && e3 is MethodCallExpression
                                { Method.Name: nameof(Matrix.Transpose) } mcb
                                ? Expression.Call(e2, MatrixMultiplyTranspose, mcb.Object!)
                                : e2 == e3
                                ? Expression.Call(e2, typeof(Matrix).Get(nameof(Matrix.Square)))
                                : Expression.Multiply(e2, e3))
                            : e2 is ConstantExpression c1 && c1.Value is double d1 &&
                                e3 is ConstantExpression c2 && c2.Value is double d2
                            ? Expression.Constant(opMul switch
                            {
                                Token.Times => d1 * d2,
                                Token.Div => d1 / d2,
                                _ => d1 % d2
                            })
                            : opMul == Token.Times
                            ? (e2 == e3 && e2.Type == typeof(Vector)
                                ? Expression.Call(e2, typeof(Vector).Get(nameof(Vector.Squared)))
                                : Expression.Multiply(e2, e3))
                            : opMul == Token.Div
                            ? Expression.Divide(e2, e3)
                            : Expression.Modulo(e2, e3);
                    }
                    catch
                    {
                        throw Error($"Operator not supported for these types", opMPos);
                    }
                }
            }
            if (e1 is null)
                e1 = e2;
            else if (opAdd == Token.Plus && e1.Type == typeof(string))
            {
                if (e2.Type != typeof(string))
                    e2 = Expression.Call(e2,
                        e2.Type.GetMethod(nameof(ToString), [])!);
                e1 = typeof(string).Call(nameof(string.Concat), e1, e2);
            }
            else
            {
                if (e1.Type != e2.Type &&
                    e1.Type != typeof(Date) && e2.Type != typeof(Date))
                    (e1, e2) = (ToDouble(e1), ToDouble(e2));
                try
                {
                    if (e1.Type == typeof(Vector) && e2.Type == typeof(Vector))
                    {
                        string method = opAdd == Token.Plus
                            ? nameof(Vector.MultiplyAdd)
                            : nameof(Vector.MultiplySubtract);
                        if (e1 is BinaryExpression { NodeType: ExpressionType.Multiply } be1)
                        {
                            // any * v ± v
                            if (e2 is BinaryExpression { NodeType: ExpressionType.Multiply } be2
                                && be1.Left.Type == typeof(double)
                                && be2.Left.Type == typeof(double))
                                // d1 * v1 + d2 * v2
                                e1 = Expression.Call(VectorCombine2,
                                    be1.Left,
                                    opAdd == Token.Plus ? be2.Left : Expression.Negate(be2.Left),
                                    be1.Right, be2.Right);
                            else if (e2 is BinaryExpression { NodeType: ExpressionType.Multiply } bee2
                                && be1.Left.Type == typeof(Matrix)
                                && bee2.Left.Type == typeof(double))
                                // m * v1 + d * v2
                                e1 = Expression.Call(be1.Left, MatrixCombine,
                                    be1.Right,
                                    opAdd == Token.Plus ? bee2.Left : Expression.Negate(bee2.Left),
                                    bee2.Right);
                            else
                                e1 = be1.Right.Type == typeof(double)
                                    ? Expression.Call(be1.Left,
                                        typeof(Vector).GetMethod(method, DoubleVectorArg)!,
                                        be1.Right, e2)
                                    : be1.Left.Type == typeof(double)
                                    ? Expression.Call(be1.Right,
                                        typeof(Vector).GetMethod(method, DoubleVectorArg)!,
                                        be1.Left, e2)
                                    : be1.Left.Type == typeof(Matrix)
                                    ? Expression.Call(be1.Left,
                                        typeof(Matrix).GetMethod(method, VectorVectorArg)!,
                                        be1.Right, e2)
                                    : opAdd == Token.Plus
                                    ? Expression.Add(e1, e2)
                                    : Expression.Subtract(e1, e2);
                        }
                        else if (opAdd == Token.Plus &&
                            e2 is BinaryExpression { NodeType: ExpressionType.Multiply } be2)
                        {
                            // v + any * v
                            e1 = be2.Right.Type == typeof(double)
                                ? Expression.Call(be2.Left,
                                    typeof(Vector).GetMethod(method, DoubleVectorArg)!,
                                    be2.Right, e1)
                                : be2.Left.Type == typeof(double)
                                ? Expression.Call(be2.Right,
                                    typeof(Vector).GetMethod(method, DoubleVectorArg)!,
                                    be2.Left, e1)
                                : be2.Left.Type == typeof(Matrix)
                                ? Expression.Call(be2.Left,
                                    typeof(Matrix).GetMethod(method, VectorVectorArg)!,
                                    be2.Right, e1)
                                : Expression.Add(e1, e2);
                        }
                        else
                            e1 = opAdd == Token.Plus
                                ? Expression.Add(e1, e2) : Expression.Subtract(e1, e2);
                    }
                    else
                        e1 = e1 is ConstantExpression c1 && c1.Value is double d1 &&
                                e2 is ConstantExpression c2 && c2.Value is double d2
                            ? Expression.Constant(opAdd == Token.Plus ? d1 + d2 : d1 - d2)
                            : opAdd == Token.Plus
                            ? Expression.Add(e1, e2)
                            : Expression.Subtract(e1, e2);
                }
                catch
                {
                    throw Error($"Operator not supported for these types", opAPos);
                }
            }
            if ((uint)(kind - Token.Plus) > (Token.Minus - Token.Plus))
                return e1;
            (opAdd, opAPos) = (kind, start);
        }
    }

    private Expression ParseUnary()
    {
        if ((uint)(kind - Token.Plus) <= (Token.Minus - Token.Plus))
        {
            (Token opKind, int opPos) = (kind, start);
            Move();
            Expression e1 = ParseUnary();
            return !IsArithmetic(e1)
                && !e1.Type.IsAssignableTo(typeof(System.Numerics.IUnaryNegationOperators<,>)
                    .MakeGenericType(e1.Type, e1.Type))
                ? throw Error("Unary operator not supported", opPos)
                : opKind == Token.Plus ? e1 : Expression.Negate(e1);
        }
        Expression e = ParseFactor();
        return kind == Token.Caret ? ParsePower(e) : e;
    }

    private Expression ParsePower(Expression e)
    {
        int pos = start;
        Move();
        Expression e1 = ParseFactor();
        if (IsArithmetic(e) && IsArithmetic(e1))
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
                    ParameterExpression p = Expression.Parameter(e.Type);
                    e = Expression.Block(new[] { p },
                        Expression.Assign(p, e),
                        Expression.Multiply(p, p));
                    return true;
                }
                else if (power == 3)
                {
                    ParameterExpression p = Expression.Parameter(e.Type);
                    e = Expression.Block(new[] { p },
                        Expression.Assign(p, e),
                        Expression.Multiply(Expression.Multiply(p, p), p));
                    return true;
                }
                else if (power == 4)
                {
                    ParameterExpression p = Expression.Parameter(e.Type);
                    e = Expression.Block(new[] { p },
                        Expression.Assign(p, e),
                        Expression.Assign(p, Expression.Multiply(p, p)),
                        Expression.Multiply(p, p));
                    return true;
                }
            return false;
        }
    }

    private Expression ParseFactor()
    {
        Expression e;
        switch (kind)
        {
            case Token.Int:
                {
                    int value = asInt;
                    Move();
                    return Expression.Constant(value);
                }
            case Token.Real:
                {
                    double value = asReal;
                    Move();
                    return Expression.Constant(value);
                }
            case Token.Imag:
                {
                    double value = asReal;
                    Move();
                    return Expression.Constant(new Complex(0, value));
                }
            case Token.Str:
                {
                    string text = id;
                    Move();
                    return Expression.Constant(text);
                }
            case Token.Date:
                {
                    Date value = asDate;
                    Move();
                    e = Expression.Constant(value);
                    break;
                }
            case Token.False:
                Move();
                return FalseExpr;
            case Token.True:
                Move();
                return TrueExpr;
            case Token.LPar:
                Move();
                e = ParseConditional();
                CheckAndMove(Token.RPar, "Right parenthesis expected");
                break;
            case Token.Id:
                e = ParseVariable();
                break;
            case Token.IdBang:
                e = ParseIdBang();
                break;
            case Token.MultVarR:
                {
                    Expression e1 = Expression.Constant(asReal);
                    int pos = start;
                    e = ParseVariable();
                    if (e.Type == typeof(int))
                        e = IntToDouble(e);
                    else if (e.Type != typeof(double) && e.Type != typeof(Complex))
                        throw Error("Variable must be numeric", pos);
                    if (kind == Token.Caret)
                        e = ParsePower(e);
                    e = Expression.Multiply(e1, e);
                }
                break;
            case Token.MultVarI:
                {
                    Expression e1 = Expression.Constant(asInt);
                    int pos = start;
                    e = ParseVariable();
                    if (e.Type == typeof(double))
                        e1 = ToDouble(e1);
                    else if (e.Type == typeof(Complex))
                        e1 = Expression.Convert(e1, typeof(Complex));
                    else if (e.Type != typeof(int))
                        throw Error("Variable must be numeric", pos);
                    if (kind == Token.Dot)
                    {
                        Move();
                        e = ParseProperty(e);
                    }
                    if (kind == Token.Caret)
                        e = ParsePower(e);
                    e = Expression.Multiply(e1, e);
                }
                break;
            case Token.Functor:
                if (bindings.IsClassName(id))
                {
                    string className = id.ToLower();
                    SkipFunctor();
                    e = !bindings.TryGetClassMethod(className + ".new", out MethodList info)
                        ? throw Error($"Invalid class method name: {className}::new")
                        : info.Methods.Length == 1
                        ? ParseClassSingleMethod(info.Methods[0])
                        : ParseClassMultiMethod(info);
                }
                else
                    e = ParseFunction();
                break;
            case Token.LBra:
                e = ParseVectorLiteral();
                break;
            case Token.ClassName:
                {
                    string className = id.ToLower();
                    SkipFunctor();
                    if (kind == Token.Id && className == "math")
                    {
                        Expression? e1 = ParseGlobals(id);
                        if (e1 != null)
                        {
                            Move();
                            return e1;
                        }
                    }
                    e = kind != Token.Functor
                        ? throw Error("Method name expected")
                        : className == "math"
                        ? ParseFunction()
                        : ParseClassMethod(className, id.ToLower());
                    break;
                }
            default:
                throw Error("Value expected");
        }
        for (; ; )
            switch (kind)
            {
                case Token.Dot:
                    // Parse a method or property from an object.
                    Move();
                    e = kind switch
                    {
                        Token.Functor => ParseMethod(e),
                        Token.Id => ParseProperty(e),
                        _ => throw Error("Property name expected")
                    };
                    break;
                case Token.Transpose:
                    e = e.Type == typeof(ComplexVector)
                        ? Expression.Call(e, e.Type.Get(nameof(ComplexVector.Conjugate)))
                        : IsMatrix(e)
                        ? Expression.Call(e, e.Type.Get(nameof(Matrix.Transpose)))
                        : e.Type == typeof(Complex)
                        ? e.Type.Call(null, nameof(Complex.Conjugate), e)
                        : throw Error("Can only transpose a matrix or conjugate a complex vector");
                    Move();
                    break;
                case Token.LBra:
                    Move();
                    e = IsVector(e) || e.Type == typeof(Series<int>)
                        || e.Type.IsAssignableTo(typeof(FftModel))
                        ? ParseIndexer(e, true)
                        : IsMatrix(e)
                        ? ParseMatrixIndexer(e)
                        : e.Type == typeof(Series)
                        ? ParseSeriesIndexer(e)
                        : e.Type == typeof(Library.MVO.MvoModel)
                        ? ParseIndexer(e, false)
                        : e.Type == typeof(DateSpline)
                        ? ParseSplineIndexer(e, typeof(Date))
                        : e.Type == typeof(VectorSpline)
                        ? ParseSplineIndexer(e, typeof(double))
                        : throw Error("Invalid indexer");
                    break;
                case Token.LBrace:
                    Move();
                    e = e.Type.IsAssignableTo(typeof(ISafeIndexed))
                        ? ParseSafeIndexer(e)
                        : throw Error("Safe indexes are only allowed for vectors and series");
                    break;
                default:
                    return e;
            }
    }

    private Expression ParseSafeIndexer(Expression e)
    {
        Expression e1 = ParseLightConditional();
        CheckAndMove(Token.RBrace, "} expected in indexer");
        return e1.Type != typeof(int)
            ? throw Error("Index must be an integer")
            : e.Type.Call(e, nameof(Vector.SafeThis), e1);
    }

    private Expression ParseSplineIndexer(Expression e, Type expected)
    {
        Expression e1 = ParseLightConditional();
        CheckAndMove(Token.RBra, "] expected in indexer");
        return e1.Type != expected && (expected != typeof(double) || e1.Type != typeof(int))
            ? throw Error("Invalid index type")
            : Expression.Property(e, "Item", ToDouble(e1));
    }

    private Expression ParseIndexer(Expression e, bool allowSlice)
    {
        bool fromEnd1 = false;
        Expression e1 = kind == Token.Colon && allowSlice
            ? Expression.Constant(0)
            : ParseIndex(ref fromEnd1);
        if (allowSlice && kind == Token.Colon)
        {
            Move();
            Expression e2 = kind == Token.RBra
                ? Expression.Constant(Index.End)
                : ParseIndex();
            CheckAndMove(Token.RBra, "] expected in indexer");
            return Expression.Property(e, "Item", Expression.New(RangeCtor,
                Expression.New(IndexCtor, e1, Expression.Constant(fromEnd1)), e2));
        }
        CheckAndMove(Token.RBra, "] expected in indexer");
        if (fromEnd1)
            e1 = Expression.New(IndexCtor, e1, Expression.Constant(fromEnd1));
        return Expression.Property(e, "Item", e1);
    }

    private Expression ParseMatrixIndexer(Expression e)
    {
        if (e.Type != typeof(Matrix))
            e = Expression.Convert(e, typeof(Matrix));
        Expression? e1 = null, e2 = null;
        bool fromEnd11 = false, fromEnd21 = false, isRange = false;
        if (kind == Token.Comma)
            Move();
        else
        {
            e1 = kind == Token.Colon
                ? Expression.Constant(Index.Start)
                : ParseIndex(ref fromEnd11);
            if (kind == Token.Colon)
            {
                Move();
                Expression e12 = kind == Token.Comma
                    ? Expression.Constant(Index.End)
                    : ParseIndex();
                if (e1.Type != typeof(Index))
                    e1 = Expression.New(IndexCtor, e1, Expression.Constant(fromEnd11));
                e1 = Expression.New(RangeCtor, e1, e12);
                isRange = true;
            }
            if (kind != Token.RBra)
                CheckAndMove(Token.Comma, "Comma expected");
        }
        if (kind == Token.RBra)
            Move();
        else
        {
            e2 = kind == Token.Colon
                ? Expression.Constant(Index.Start)
                : ParseIndex(ref fromEnd21);
            if (kind == Token.Colon)
            {
                Move();
                Expression e22 = kind == Token.RBra
                    ? Expression.Constant(Index.End)
                    : ParseIndex();
                if (e2.Type != typeof(Index))
                    e2 = Expression.New(IndexCtor, e2, Expression.Constant(fromEnd21));
                e2 = Expression.New(RangeCtor, e2, e22);
                if (!isRange && fromEnd11)
                    e1 = Expression.New(IndexCtor, e1!, TrueExpr);
                isRange = true;
            }
            else if (isRange && fromEnd21)
                e2 = Expression.New(IndexCtor, e2, TrueExpr);
            CheckAndMove(Token.RBra, "] expected");
        }
        if (isRange)
        {
            e1 ??= Expression.Constant(Range.All);
            e2 ??= Expression.Constant(Range.All);
        }
        else if (fromEnd11 || fromEnd21)
        {
            if (e1 != null)
                e1 = Expression.New(IndexCtor, e1, Expression.Constant(fromEnd11));
            if (e2 != null)
                e2 = Expression.New(IndexCtor, e2, Expression.Constant(fromEnd21));
        }
        return
            e1 != null && e2 != null ? Expression.Property(e, "Item", e1, e2)
            : e2 != null ? typeof(Matrix).Call(e, nameof(Matrix.GetColumn), e2)
            : e1 != null ? typeof(Matrix).Call(e, nameof(Matrix.GetRow), e1) : e;
    }

    private Expression ParseSeriesIndexer(Expression e)
    {
        Expression? e1 = null, e2 = null;
        bool fromEnd1 = false, fromEnd2 = false;
        int pos = start;
        if (kind != Token.Colon)
        {
            e1 = ParseIndex(ref fromEnd1, false);
            if (e1.Type == typeof(int))
            {
                if (kind == Token.RBra)
                {
                    Move();
                    return Expression.Property(e, "Item", fromEnd1
                        ? Expression.New(IndexCtor, e1, TrueExpr)
                        : e1);
                }
            }
            else if (e1.Type != typeof(Date))
                throw Error("Lower bound must be a date or integer");
            else if (fromEnd1)
                throw Error("Relative indexes not supported for dates", pos);
            if (kind != Token.Colon)
                throw Error(": expected in slice");
        }
        Move();
        if (kind != Token.RBra)
        {
            if (kind == Token.Caret)
                pos = start;
            e2 = ParseIndex(ref fromEnd2, false);
            if (e2.Type != typeof(Date) && e2.Type != typeof(int))
                throw Error("Upper bound must be a date or integer");
            if (fromEnd2 && e2.Type == typeof(Date))
                throw Error("Relative indexes not supported for dates", pos);
            if (kind != Token.RBra)
                throw Error("] expected in slice");
        }
        Move();
        if (e1 == null && e2 == null)
            return e;
        if (e1 != null && e2 != null && e1.Type != e2.Type)
            throw Error("Both indexers must be of the same type");
        if (fromEnd1 || fromEnd2)
        {
            e1 = e1 != null
                ? Expression.New(IndexCtor, e1, Expression.Constant(fromEnd1))
                : Expression.Constant(Index.Start);
            e2 = e2 != null
                ? Expression.New(IndexCtor, e2, Expression.Constant(fromEnd2))
                : Expression.Constant(Index.End);
            return Expression.Property(e,
                typeof(Series).GetProperty("Item", [typeof(Range)])!,
                Expression.New(RangeCtor, e1, e2));
        }
        e1 ??= e2!.Type == typeof(Date)
            ? Expression.Constant(Date.Zero)
            : Expression.Constant(0);
        e2 ??= e1.Type == typeof(Date)
            ? Expression.Constant(new Date(3000, 1, 1))
            : Expression.Constant(int.MaxValue);
        return Expression.Call(e,
            typeof(Series).GetMethod(nameof(Series.Slice), [e1.Type, e2.Type])!,
            e1, e2);
    }

    private Expression ParseMethod(Expression e)
    {
        if (!bindings.TryGetMethod(e.Type, id, out MethodInfo? mInfo))
            throw Error($"Invalid method: {id}");
        // Skip method name and left parenthesis.
        SkipFunctor();
        ParameterInfo[] paramInfo = mInfo.GetParameters();
        List<Expression> args = Rent(paramInfo.Length);
        for (int i = 0; i < paramInfo.Length; i++, Move())
        {
            args.Add(ParseByType(paramInfo[i].ParameterType));
            if (kind != Token.Comma)
                break;
        }
        if (args.Count != paramInfo.Length)
            throw Error("Invalid number of arguments");
        CheckAndMove(Token.RPar, "Right parenthesis expected");
        Expression result = Expression.Call(e, mInfo, args);
        Return(args);
        return result;
    }

    private Expression ParseIndex(ref bool fromEnd, bool check = true)
    {
        if (kind == Token.Caret)
        {
            fromEnd = true;
            Move();
        }
        Expression e = ParseLightConditional();
        return check && e.Type != typeof(int)
            ? throw Error("Index must be integer") : e;
    }

    private Expression ParseIndex()
    {
        bool fromEnd = false;
        return Expression.New(IndexCtor, ParseIndex(ref fromEnd), Expression.Constant(fromEnd));
    }

    private Expression ParseLambda(Type funcType)
    {
        Type[] genTypes = funcType.GenericTypeArguments;
        Type t1 = genTypes[0], retType = genTypes[^1];
        Type? t2 = genTypes.Length == 3 ? genTypes[1] : null;
        try
        {
            parsingLambdaHeader = true;
            if (t2 == null)
            {
                if (kind != Token.Id)
                    throw Error("Lambda parameter name expected");
                lambdaParameter = Expression.Parameter(t1, id);
                Move();
            }
            else
            {
                CheckAndMove(Token.LPar, "Lambda parameters expected");
                if (kind != Token.Id)
                    throw Error("First lambda parameter name expected");
                lambdaParameter = Expression.Parameter(t1, id);
                Move();
                CheckAndMove(Token.Comma, "Comma expected");
                if (kind != Token.Id)
                    throw Error("Second lambda parameter name expected");
                lambdaParameter2 = Expression.Parameter(t2, id);
                Move();
                CheckAndMove(Token.RPar, ") expected in lambda header");
            }
            CheckAndMove(Token.Arrow, "=> expected");
            parsingLambdaHeader = false;
            Expression body = ParseConditional();
            if (body.Type != retType)
                body = retType == typeof(Complex) && IsArithmetic(body)
                    ? Expression.Convert(body, typeof(Complex))
                    : retType == typeof(double) && body.Type == typeof(int)
                    ? IntToDouble(body)
                    : throw Error($"Expected return type is {retType.Name}");
            Expression result = lambdaParameter2 != null
                ? Expression.Lambda(body, lambdaParameter, lambdaParameter2)
                : Expression.Lambda(body, lambdaParameter);
            lambdaParameter = lambdaParameter2 = null;
            return result;
        }
        finally
        {
            if (abortPosition == int.MaxValue)
            {
                lambdaParameter = lambdaParameter2 = null;
                parsingLambdaHeader = false;
            }
        }
    }

    private Expression ParseProperty(Expression e)
    {
        if (!bindings.TryGetProperty(e.Type, id, out MethodInfo? mInfo))
            throw Error($"Invalid property: {id}");
        Move();
        return Expression.Call(e, mInfo);
    }

    /// <summary>Parses a global function call.</summary>
    /// <returns>An expression representing the function call.</returns>
    private Expression ParseFunction()
    {
        (string function, int pos) = (id.ToLower(), start);
        SkipFunctor();
        if (bindings.TryGetClassMethod("math." + function, out MethodList inf))
            return inf.Methods.Length == 1
                ? ParseClassSingleMethod(inf.Methods[0])
                : ParseClassMultiMethod(inf);
        if (function != "iff")
            throw Error("Invalid function name", pos);
        Expression a0 = ParseConditional();
        if (a0.Type != typeof(bool))
            throw Error("First argument must be boolean");
        CheckAndMove(Token.Comma, "Comma expected");
        Expression a1 = ParseConditional();
        CheckAndMove(Token.Comma, "Comma expected");
        Expression a2 = ParseConditional();
        if (DifferentTypes(ref a1, ref a2))
            throw Error("Second and third arguments must have the same type");
        CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
        return Expression.Condition(a0, a1, a2);
    }

    private Expression ParseClassMethod(string className, string methodName)
    {
        SkipFunctor();
        if (!bindings.TryGetClassMethod(className + "." + methodName, out MethodList info))
            throw Error($"Invalid class method name: {className}::{methodName}");
        return info.Methods.Length == 1
            ? ParseClassSingleMethod(info.Methods[0])
            : ParseClassMultiMethod(info);
    }

    private Expression ParseClassSingleMethod(in MethodData method)
    {
        Type[] types = method.Args;
        List<Expression> args = Rent(types.Length);
        for (int i = 0; i < types.Length; i++, Move())
        {
            args.Add(ParseByType(types[i]));
            if (kind != Token.Comma)
            {
                if (++i == types.Length - 1 && types[i] is Type t)
                    if (t == typeof(Random) || t == typeof(NormalRandom))
                        args.Add(t.New());
                    else if (t == typeof(One))
                        args.Add(Expression.Constant(1d));
                    else
                        throw Error($"Invalid number of arguments");
                break;
            }
        }
        Expression result = method.GetExpression(args);
        Return(args);
        CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
        return result;
    }

    private Expression ParseByType(Type expected)
    {
        if (expected.IsAssignableTo(typeof(Delegate)))
            return ParseLambda(expected);
        if (expected == typeof(Index))
            return ParseIndex();
        if (expected.IsArray && expected.GetElementType() is Type subType)
            for (List<Expression> items = Rent(16); ; Move())
            {
                Expression it = ParseLightConditional();
                if (it.Type != subType)
                    throw Error($"Expected {subType.Name}");
                items.Add(it);
                if (kind != Token.Comma)
                {
                    Expression result = subType.Make(items);
                    Return(items);
                    return result;
                }
            }
        Expression e = ParseLightConditional();
        return e.Type == expected ||
            expected == typeof(Series<Date>) && e.Type == typeof(Series) ||
            expected.IsClass && e.Type.IsAssignableTo(expected)
            ? e
            : expected == typeof(double) && e.Type == typeof(int)
            ? IntToDouble(e)
            : expected == typeof(Complex) && IsArithmetic(e)
            ? Expression.Convert(ToDouble(e), typeof(Complex))
            : throw Error($"Expected {expected.Name}");
    }

    private Expression ParseClassMultiMethod(in MethodList info)
    {
        List<Expression> args = Rent(16);
        List<int> starts = new(16);
        // All overloads are alive at start.
        int mask = (0x1 << info.Methods.Length) - 1;
        // Create the initial list of actual arguments.
        for (int i = 0; ; i++, Move())
        {
            starts.Add(start);
            // Discard easy detectable types before parsing the argument.
            if (i >= info.IsLambda.Length)
                args.Add(ParseLightConditional());
            else if (info.IsLambda[i] && IsLambda())
            {
                Type? lambda = null;
                uint lambdaType = kind == Token.LPar ? MethodData.Mλ2 : MethodData.Mλ1;
                for (int j = TrailingZeroCount((uint)mask), m = 1 << j;
                    j < info.Methods.Length; j++, m <<= 1)
                    if ((mask & m) != 0 && info.Methods[j] is MethodData md)
                        if (md.GetMask(i) != lambdaType)
                            mask &= ~m;
                        else if (lambda == null)
                            lambda = md.Args[i];
                        else if (md.Args[i] != lambda)
                            throw Error("Inconsistent lambda types");
                if (lambda == null)
                    throw Error("Invalid number of arguments in lambda function");
                args.Add(ParseLambda(lambda));
            }
            else
            {
                Expression last = kind == Token.Caret ? ParseIndex() : ParseLightConditional();
                args.Add(last);
                for (int j = TrailingZeroCount((uint)mask), m = 1 << j;
                    j < info.Methods.Length; j++, m <<= 1)
                    if ((mask & m) != 0 && info.Methods[j] is MethodData md)
                        if (md.ExpectedArgs < i || md.GetMask(i) >= MethodData.Mλ1 ||
                            !CanConvert(last.Type, md.Args[Math.Min(i, md.Args.Length - 1)]))
                            mask &= ~m;
                if (mask == 0)
                    throw Error("Invalid argument type");
            }
            if (kind != Token.Comma)
                break;
        }
        // Discard overloads according to the number of arguments.
        if (PopCount((uint)mask) != 1)
        {
            for (int j = TrailingZeroCount((uint)mask), m = 1 << j,
                last = 32 - LeadingZeroCount((uint)mask); j < last; j++, m <<= 1)
                if ((mask & m) != 0 &&
                    Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(info.Methods), j) is var md
                    && md.ExpectedArgs != args.Count)
                {
                    Type? act = args[^1].Type, form = md.Args[^1].GetElementType();
                    if (md.ExpectedArgs != int.MaxValue ||
                        form != act && (form != typeof(double) || act != typeof(int)))
                        mask &= ~m;
                }
            for (int bits = PopCount((uint)mask); bits > 1;)
            {
                int mth1 = TrailingZeroCount((uint)mask);
                int mth2 = 32 - LeadingZeroCount((uint)mask) - 1;
                MethodData m1 = info.Methods[mth1], m2 = info.Methods[mth2];
                Type t0 = args[0].Type, tm1 = m1.Args[0], tm2 = m2.Args[0];
                if (tm1 != tm2)
                    if (t0 == tm1)
                        mask &= ~(1 << mth2);
                    else if (t0 == tm2)
                        mask &= ~(1 << mth1);
                    else if (t0 == typeof(int))
                        if (tm1 == typeof(double))
                            mask &= ~(1 << mth2);
                        else if (tm2 == typeof(double))
                            mask &= ~(1 << mth1);
                int bits1 = PopCount((uint)mask);
                if (bits1 <= 1 || bits1 == bits)
                    break;
                bits = bits1;
            }
            if (mask == 0)
                throw Error("No class method accepts this argument list.");
            if (PopCount((uint)mask) != 1)
                throw Error("Ambiguous class method call.");
        }
        // Get selected method overload and check conversions.
        MethodData mth = info.Methods[Log2((uint)mask)];
        if (mth.ExpectedArgs < mth.Args.Length && mth.Args[^1] is Type t)
            args.Add(t == typeof(Random) || t == typeof(NormalRandom)
                ? t.New() : Expression.Constant(t == typeof(One) ? 1d : 0d));
        if (mth.ExpectedArgs != int.MaxValue && args.Count < mth.ExpectedArgs)
            throw Error("No class method accepts this argument list.");
        for (int i = 0; i < mth.ExpectedArgs; i++)
        {
            Type expected = mth.Args[i], actual = args[i].Type;
            if (actual != expected)
            {
                if (expected == typeof(double) && actual == typeof(int))
                    args[i] = IntToDouble(args[i]);
                else if (expected == typeof(Complex) &&
                    (actual == typeof(int) || actual == typeof(double)))
                    args[i] = Expression.Convert(ToDouble(args[i]), typeof(Complex));
                else if (expected.IsArray && expected.GetElementType() is Type et)
                {
                    for (int j = i; j < args.Count; j++)
                        if (args[i].Type is var a && a != et &&
                            (et != typeof(double) || a != typeof(int)))
                            throw Error($"Expected {expected.Name}", starts[i]);
                    args[i] = et.Make(args.Skip(i).Select(a => a.Type == et ? a : ToDouble(a)));
                    args.RemoveRange(i + 1, args.Count - i - 1);
                    break;
                }
                else
                    throw Error($"Expected {expected.Name}", starts[i]);
            }
        }
        CheckAndMove(Token.RPar, "Right parenthesis expected in class method call");
        Expression result = mth.GetExpression(args);
        Return(args);
        return result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CanConvert(Type actual, Type expected) =>
            expected == actual ||
            expected == typeof(double) && actual == typeof(int) ||
            expected == typeof(Complex) && (actual == typeof(double) || actual == typeof(int)) ||
            expected.IsArray && expected.GetElementType() is var et
                && (actual == et || et == typeof(double) && actual == typeof(int));
    }

    private Expression ParseVectorLiteral()
    {
        Move();
        List<Expression> items = Rent(16);
        int period = 0, lastPeriod = 0, vectors = 0, matrices = 0;
        for (; ; )
        {
            Expression e = ParseLightConditional();
            if (IsArithmetic(e))
                items.Add(ToDouble(e));
            else if (e.Type == typeof(Vector))
            {
                if (period != 0 && matrices == 0)
                    throw Error("Invalid vector in matrix constructor");
                vectors++;
                items.Add(e);
            }
            else if (e.Type == typeof(Matrix))
            {
                if (period > 1 || vectors + matrices != items.Count || items.Count >= 2)
                    throw Error("Invalid matrix concatenation");
                matrices++;
                items.Add(e);
            }
            else
                throw Error("Vector item must be numeric");
            if (kind == Token.Semicolon)
            {
                if (period == 0)
                    period = lastPeriod = items.Count;
                else if (items.Count - lastPeriod != period)
                    throw Error("Inconsistent matrix size");
                else
                    lastPeriod = items.Count;
                Move();
            }
            else if (kind == Token.Comma)
                Move();
            else if (kind == Token.RBra)
            {
                Move();
                break;
            }
        }
        Expression result;
        if (matrices > 0)
            result = period > 1 || vectors + matrices != items.Count || items.Count > 2
                ? throw Error("Invalid matrix concatenation")
                : typeof(Matrix).Call(period == 0 ? nameof(Matrix.HCat) : nameof(Matrix.VCat),
                    items[0], items[1]);
        else if (vectors > 0)
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
            result = typeof(Vector).New(typeof(Vector).Make(items));
        }
        else
        {
            if (period != 0 && items.Count - lastPeriod != period)
                throw Error("Inconsistent matrix size");
            Expression args = typeof(double).Make(items);
            result = period != 0
                ? typeof(Matrix).New(
                    Expression.Constant(items.Count / period), Expression.Constant(period), args)
                : typeof(Vector).New(args);
        }
        Return(items);
        return result;
    }

    private Expression ParseIdBang()
    {
        // Check macro definitions.
        Definition? def = source.GetDefinition(id)
            ?? throw Error($"{id} is not a definition.");
        Move();
        if (isParsingDefinition)
            references.Add(def);
        return def.Expression;
    }

    private Expression ParseVariable()
    {
        (int pos, string ident) = (start, id);
        Move();
        // Check lambda parameters when present.
        if (lambdaParameter != null)
        {
            if (ident.Equals(lambdaParameter.Name, StringComparison.OrdinalIgnoreCase))
                return lambdaParameter;
            if (lambdaParameter2 != null &&
                ident.Equals(lambdaParameter2.Name, StringComparison.OrdinalIgnoreCase))
                return lambdaParameter2;
        }
        // Check the local scope.
        if (locals.TryGetValue(ident, out ParameterExpression? local))
            return local.Type == typeof(DoubleSequence)
                ? Expression.Call(local, typeof(DoubleSequence).GetMethod(nameof(DoubleSequence.Clone))!)
                : local;
        // Check macro definitions.
        Definition? def = source.GetDefinition(ident);
        if (def != null)
        {
            if (isParsingDefinition)
                references.Add(def);
            string ident1 = "$" + def.Name;
            if (!locals.TryGetValue(ident1, out local))
            {
                locals.Add(ident1, local = Expression.Parameter(def.Type, ident));
                topLocals.Add(local);
                topExpressions.Add(Expression.Assign(local, def.Expression));
            }
            return local;
        }
        // Check the global scope.
        Expression? e = ParsePendingVariables(ident)
            ?? source.GetExpression(ident, isParsingDefinition)
            ?? ParseGlobals(ident);
        if (e != null)
        {
            return e.Type.IsAssignableTo(typeof(DoubleSequence))
                ? Expression.Call(Expression.Call(e,
                    typeof(DoubleSequence).GetMethod(nameof(DoubleSequence.Clone))!),
                    typeof(DoubleSequence).GetMethod(nameof(DoubleSequence.Reset))!)
                : e;
        }
        if (TryParseMonthYear(ident, out Date d))
            return Expression.Constant(d);
        // Check if we tried to reference a SET variable in a DEF.
        if (isParsingDefinition && source[ident] != null)
            throw Error("SET variables cannot be used in persistent definitions", pos);
        throw Error($"Unknown variable: {ident}", pos);

        Expression? ParsePendingVariables(string ident) =>
            !pendingSets.TryGetValue(ident, out Expression? sourceExpr)
            ? null
            : source.GetExpression(ident, sourceExpr);
    }

    private Expression? ParseGlobals(string ident) =>
        ident == "π" ? PiExpr
        : ident == "τ" ? Expression.Constant(Math.Tau)
        : ident.ToLower() switch
        {
            "e" => Expression.Constant(Math.E),
            "i" => ImExpr,
            "pi" => PiExpr,
            "today" => Expression.Constant(Date.Today),
            "pearl" => Expression.Call(typeof(Functions).Get(nameof(Functions.Austra))),
            "random" => Expression.Call(typeof(Functions).GetMethod(nameof(Functions.Random))!),
            "nrandom" => Expression.Call(typeof(Functions).GetMethod(nameof(Functions.NRandom))!),
            _ => null,
        };
}
