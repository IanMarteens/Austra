﻿using System.Runtime.InteropServices;

namespace Austra.Parser;

/* THIS FILE IMPLEMENTS THE RECURSIVE DESCENT PARSING METHODS */

/// <summary>Syntactic and lexical analysis for AUSTRA.</summary>
internal sealed partial class Parser
{
    /// <summary>Compiles a block expression as a lambda function.</summary>
    /// <returns>A lambda method, when successful.</returns>
    public Func<IDataSource, object> Parse() =>
        Expression.Lambda<Func<IDataSource, object>>(
            ParseStatement(), sourceParameter).Compile();

    /// <summary>Parses a block expression without compiling it.</summary>
    /// <returns>The type of the block expression.</returns>
    public Type ParseType()
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
        }
        // Check now for a set header and skip it.
        else if (kind == Token.Set)
        {
            Move();
            CheckAndMove(Token.Id, "Left side variable expected");
            if (kind == Token.Eof)
                return typeof(void);
            CheckAndMove(Token.Eq, "= expected");
        }
        Expression e = ParseFormula(false);
        Debug.WriteLine(e.ToString());
        return e.Type;
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
            result = new();
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
    /// <param name="description">A description for the definition.</param>
    /// <returns>A new definition, on success.</returns>
    public Definition ParseDefinition(string description)
    {
        CheckAndMove(Token.Def, "DEF expected");
        if (kind != Token.Id)
            throw Error("Definition name expected");
        string defName = id;
        if (source.GetDefinition(defName) != null ||
            source[defName] != null)
            throw Error($"{defName} already in use");
        Move();
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
        Expression e = ParseFormula(false);
        if (e.Type == typeof(Series))
            e = typeof(Series).Call(e, nameof(Series.SetName), Expression.Constant(defName));
        Definition def = new(defName, text[first..], description, e);
        foreach (Definition referenced in references)
            referenced.Children.Add(def);
        return def;
    }

    /// <summary>Compiles a block expression.</summary>
    /// <returns>A block expression.</returns>
    private Expression ParseStatement()
    {
        if (kind == Token.Set)
        {
            Move();
            if (kind != Token.Id)
                throw Error("Left side variable expected");
            int namePos = start;
            LeftValue = id;
            Move();
            if (kind == Token.Eof)
            {
                source[LeftValue] = null;
                return Expression.Constant(null);
            }
            CheckAndMove(Token.Eq, "= expected");
            // Always allow deleting a session variable.
            if (source.GetDefinition(LeftValue) != null)
                throw Error($"{LeftValue} already in use", namePos);
        }
        return ParseFormula(true);
    }

    /// <summary>Compiles a block expression.</summary>
    /// <param name="forceCast">Whether to force a cast to object.</param>
    /// <returns>A block expression.</returns>
    private Expression ParseFormula(bool forceCast)
    {
        List<ParameterExpression> locals = new();
        List<Expression> expressions = new();
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
                locals.Add(le);
                expressions.Add(Expression.Assign(le, init));
                this.locals[localId] = le;
            }
            while (kind == Token.Comma);
            CheckAndMove(Token.In, "IN expected");
        }
        Expression rvalue = ParseConditional();
        if (forceCast)
            rvalue = Expression.Convert(rvalue, typeof(object));
        if (LeftValue != "")
            rvalue = Expression.Assign(Expression.Property(
                sourceParameter, "Item", Expression.Constant(LeftValue)), rvalue);
        expressions.Add(rvalue);
        return kind != Token.Eof
            ? throw Error("Extra input after expression")
            : locals.Count == 0 && expressions.Count == 1
            ? expressions[0]
            : Expression.Block(locals, expressions);
    }

    /// <summary>Compiles a ternary conditional expression.</summary>
    private Expression ParseConditional()
    {
        if (kind != Token.If)
            return ParseDisjunction();
        Move();
        Expression c = ParseDisjunction();
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
        kind == Token.If ? ParseConditional() : ParseAdditive();

    /// <summary>Compiles an OR/AND expression.</summary>
    private Expression ParseDisjunction()
    {
        Expression? e1 = null;
        for (; ; Move())
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
                break;
        }
        return e1;
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
        Expression e1 = ParseAdditive();
        (Token opKind, int pos) = (kind, start);
        switch (opKind)
        {
            case Token.Eq:
            case Token.Ne:
                {
                    Move();
                    Expression e2 = ParseAdditive();
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
                    Expression e2 = ParseAdditive();
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
                                Expression e3 = ParseAdditive();
                                if (!IsArithmetic(e3))
                                    throw Error("Upper bound must be numeric");
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

    private Expression ParseAdditive()
    {
        Expression e1 = ParseMultiplicative();
        while ((uint)(kind - Token.Plus) <= (Token.Minus - Token.Plus))
        {
            (Token opLex, int opPos) = (kind, start);
            Move();
            Expression e2 = ParseMultiplicative();
            if (opLex == Token.Plus && e1.Type == typeof(string))
            {
                if (e2.Type != typeof(string))
                    e2 = Expression.Call(e2,
                        e2.Type.GetMethod(nameof(ToString), Array.Empty<Type>())!);
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
                        string method = opLex == Token.Plus
                            ? nameof(Vector.MultiplyAdd)
                            : nameof(Vector.MultiplySubtract);
                        if (e1 is BinaryExpression { NodeType: ExpressionType.Multiply } be1)
                        {
                            if (e2 is BinaryExpression { NodeType: ExpressionType.Multiply } be2
                                && be1.Left.Type == typeof(double)
                                && be2.Left.Type == typeof(double))
                                e1 = Expression.Call(
                                        typeof(Vector).GetMethod(nameof(Vector.Combine2),
                                        new[] { typeof(double), typeof(double),
                                                typeof(Vector), typeof(Vector)})!,
                                    be1.Left,
                                    opLex == Token.Plus ? be2.Left : Expression.Negate(be2.Left),
                                    be1.Right, be2.Right);
                            else
                                e1 = be1.Right.Type == typeof(double)
                                    ? Expression.Call(be1.Left,
                                        typeof(Vector).GetMethod(method,
                                        new[] { typeof(double), typeof(Vector) })!, be1.Right, e2)
                                    : be1.Left.Type == typeof(double)
                                    ? Expression.Call(be1.Right,
                                        typeof(Vector).GetMethod(method,
                                        new[] { typeof(double), typeof(Vector) })!, be1.Left, e2)
                                    : be1.Left.Type == typeof(Matrix)
                                    ? Expression.Call(be1.Left,
                                        typeof(Matrix).GetMethod(method,
                                        new[] { typeof(Vector), typeof(Vector) })!, be1.Right, e2)
                                    : opLex == Token.Plus
                                    ? Expression.Add(e1, e2)
                                    : Expression.Subtract(e1, e2);
                        }
                        else if (opLex == Token.Plus &&
                            e2 is BinaryExpression { NodeType: ExpressionType.Multiply } be2)
                        {
                            e1 = be2.Right.Type == typeof(double)
                                ? Expression.Call(be2.Left,
                                    typeof(Vector).GetMethod(method,
                                    new[] { typeof(double), typeof(Vector) })!, be2.Right, e1)
                                : be2.Left.Type == typeof(double)
                                ? Expression.Call(be2.Right,
                                    typeof(Vector).GetMethod(method,
                                    new[] { typeof(double), typeof(Vector) })!, be2.Left, e1)
                                : be2.Left.Type == typeof(Matrix)
                                ? Expression.Call(be2.Left,
                                    typeof(Matrix).GetMethod(method,
                                    new[] { typeof(Vector), typeof(Vector) })!, be2.Right, e1)
                                : Expression.Add(e1, e2);
                        }
                        else
                            e1 = opLex == Token.Plus
                                ? Expression.Add(e1, e2) : Expression.Subtract(e1, e2);
                    }
                    else
                        e1 = e1 is ConstantExpression c1 && c1.Value is double d1 &&
                                e2 is ConstantExpression c2 && c2.Value is double d2
                            ? Expression.Constant(opLex == Token.Plus ? d1 + d2 : d1 - d2)
                            : opLex == Token.Plus
                            ? Expression.Add(e1, e2)
                            : Expression.Subtract(e1, e2);
                }
                catch
                {
                    throw Error($"Operator not supported for these types", opPos);
                }
            }
        }
        return e1;
    }

    private Expression ParseMultiplicative()
    {
        Expression e1 = ParseUnary();
        while ((uint)(kind - Token.Times) <= (Token.Mod - Token.Times))
        {
            (Token opLex, int opPos) = (kind, start);
            Move();
            Expression e2 = ParseUnary();
            if (opLex == Token.Backslash)
                e1 = e1.Type != typeof(Matrix)
                    ? throw Error("First operand must be a matrix", opPos)
                    : e2.Type != typeof(Vector) && e2.Type != typeof(Matrix)
                    ? throw Error("Second operand must be a vector or a matrix", opPos)
                    : typeof(Matrix).Call(e1, nameof(Matrix.Solve), e2);
            else if (opLex == Token.PointTimes || opLex == Token.PointDiv)
                e1 = e1.Type == e2.Type && e1.Type.IsAssignableTo(
                        typeof(IPointwiseOperators<>).MakeGenericType(e1.Type))
                    ? e1.Type.Call(e1, opLex == Token.PointTimes
                        ? nameof(Vector.PointwiseMultiply) : nameof(Vector.PointwiseDivide),
                        e2)
                    : throw Error("Invalid operator", opPos);
            else
            {
                if (e1.Type != e2.Type)
                    (e1, e2) = (ToDouble(e1), ToDouble(e2));
                try
                {
                    // Try to optimize matrix transpose multiplying a vector.
                    e1 = opLex == Token.Times && e1.Type == typeof(Matrix)
                        ? (e2.Type == typeof(Vector) && e1 is MethodCallExpression
                        { Method.Name: nameof(Matrix.Transpose) } mca
                            ? typeof(Matrix).Call(mca.Object, nameof(Matrix.TransposeMultiply), e2)
                            : e2.Type == typeof(Matrix) && e2 is MethodCallExpression
                            { Method.Name: nameof(Matrix.Transpose) } mcb
                            ? typeof(Matrix).Call(e1, nameof(Matrix.MultiplyTranspose), mcb.Object!)
                            : e1 == e2
                            ? Expression.Call(e1, typeof(Matrix).Get(nameof(Matrix.Square)))
                            : Expression.Multiply(e1, e2))
                        : e1 is ConstantExpression c1 && c1.Value is double d1 &&
                            e2 is ConstantExpression c2 && c2.Value is double d2
                        ? Expression.Constant(opLex switch
                        {
                            Token.Times => d1 * d2,
                            Token.Div => d1 / d2,
                            _ => d1 % d2
                        })
                        : opLex == Token.Times
                        ? (e1 == e2 && e1.Type == typeof(Vector)
                            ? Expression.Call(e1, typeof(Vector).Get(nameof(Vector.Squared)))
                            : Expression.Multiply(e1, e2))
                        : opLex == Token.Div
                        ? Expression.Divide(e1, e2)
                        : Expression.Modulo(e1, e2);
                }
                catch
                {
                    throw Error($"Operator not supported for these types", opPos);
                }
            }
        }
        return e1;
    }

    private Expression ParseUnary()
    {
        if ((uint)(kind - Token.Plus) <= (Token.Minus - Token.Plus))
        {
            (Token opKind, int opPos) = (kind, start);
            Move();
            Expression e1 = ParseUnary();
            return e1.Type != typeof(Complex) && !IsArithmetic(e1)
                && !IsVector(e1) && !IsMatrix(e1) && e1.Type != typeof(Series)
                ? throw Error("Unary operand must be numeric", opPos)
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
            case Token.MultVarR:
                {
                    Expression e1 = Expression.Constant(asReal);
                    int pos = start;
                    e = ParseVariable();
                    if (e.Type == typeof(int))
                        e = ToDouble(e);
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
                e = ParseFunction();
                break;
            case Token.LBra:
                e = ParseVectorLiteral();
                break;
            case Token.ClassName:
                {
                    string className = id.ToLower();
                    // Skip class name and double colon.
                    SkipFunctor();
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
        Move();
        Expression e1 = ParseLightConditional();
        if (e1.Type != typeof(int))
            throw Error("Index must be an integer");
        CheckAndMove(Token.RBrace, "} expected in indexer");
        return e.Type.Call(e, nameof(Vector.SafeThis), e1);
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
            e1 != null && e2 != null
            ? Expression.Property(e, "Item", e1, e2)
            : e2 != null
            ? typeof(Matrix).Call(e, nameof(Matrix.GetColumn), e2)
            : e1 != null
            ? typeof(Matrix).Call(e, nameof(Matrix.GetRow), e1)
            : e;
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
                typeof(Series).GetProperty("Item", new[] { typeof(Range) })!,
                Expression.New(RangeCtor, e1, e2));
        }
        e1 ??= e2!.Type == typeof(Date)
            ? Expression.Constant(Date.Zero)
            : Expression.Constant(0);
        e2 ??= e1.Type == typeof(Date)
            ? Expression.Constant(new Date(3000, 1, 1))
            : Expression.Constant(int.MaxValue);
        return Expression.Call(e,
            typeof(Series).GetMethod(nameof(Series.Slice), new[] { e1.Type, e2.Type })!,
            e1, e2);
    }

    private Expression ParseMethod(Expression e)
    {
        if (!methods.TryGetValue(e.Type, out Dictionary<string, MethodInfo>? dict) ||
            !dict.TryGetValue(id, out MethodInfo? mInfo))
            throw Error($"Invalid method: {id}");
        // Skip method name and left parenthesis.
        SkipFunctor();
        ParameterInfo[] paramInfo = mInfo.GetParameters();
        Type firstParam = paramInfo[0].ParameterType;
        if (paramInfo.Length == 2 &&
            paramInfo[1].ParameterType.IsAssignableTo(typeof(Delegate)))
        {
            // This is a zip or reduce method call.
            Expression e1 = ParseConditional();
            if (e1.Type != firstParam)
                if (firstParam == typeof(double) && e1.Type == typeof(int))
                    e1 = ToDouble(e1);
                else if (firstParam == typeof(Complex) && IsArithmetic(e1))
                    e1 = Expression.Convert(ToDouble(e1), typeof(Complex));
                else
                    throw Error($"{firstParam.Name} expected");
            CheckAndMove(Token.Comma, "Comma expected");
            return Expression.Call(e, mInfo, e1, ParseLambda(paramInfo[1].ParameterType, true));
        }
        if (firstParam.IsAssignableTo(typeof(Delegate)))
            return Expression.Call(e, mInfo, ParseLambda(firstParam, true));
        if (firstParam == typeof(Index))
        {
            Expression e1 = ParseIndex();
            CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
            return Expression.Call(e, mInfo, e1);
        }
        (List<Expression> a, List<int> p) = CollectArguments();
        Expression result;
        if (firstParam == typeof(Series[]) || firstParam == typeof(Vector[]))
            result = a.Any(a => a.Type != e.Type)
                ? throw Error(e.Type == typeof(Series) ?
                    "Series list expected" : "Vector list expected")
                : Expression.Call(e, mInfo, e.Type.Make(a));
        else
        {
            if (a.Count != paramInfo.Length)
                throw Error("Invalid number of arguments");
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
            result = Expression.Call(e, mInfo, a[0]);
        }
        Return(a);
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
        if (check && e.Type != typeof(int))
            throw Error("Index must be integer");
        return e;
    }

    private Expression ParseIndex()
    {
        bool fromEnd = false;
        return Expression.New(IndexCtor, ParseIndex(ref fromEnd), Expression.Constant(fromEnd));
    }

    private Expression ParseLambda(Type funcType, bool isLast = false)
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
                    ? ToDouble(body)
                    : throw Error($"Expected return type is {retType.Name}");
            if (isLast)
                CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
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
        if (!allProps.TryGetValue(e.Type, out Dictionary<string, MethodInfo>? dict) ||
            !dict.TryGetValue(id, out MethodInfo? mInfo))
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
        if (classMethods.TryGetValue("math." + function, out MethodList inf))
            return inf.Methods.Length == 1
                ? ParseClassSingleMethod(inf.Methods[0])
                : ParseClassMultiMethod(inf);
        (List<Expression> a, List<int> p) = CollectArguments();
        Expression result;
        if (function == "iff")
        {
            // This is a generic function!
            if (a.Count != 3)
                throw Error("Function 'iff' requires 3 arguments", pos);
            if (a[0].Type != typeof(bool))
                throw Error("First argument must be boolean", p[0]);
            Expression a2 = a[1], a3 = a[2];
            result = DifferentTypes(ref a2, ref a3)
                ? throw Error("Second and third arguments must have the same type", p[2])
                : Expression.Condition(a[0], a2, a3);
        }
        else
            throw Error("Invalid function name", pos);
        Return(a);
        return result;
    }

    private (List<Expression>, List<int>) CollectArguments()
    {
        for ((List<Expression> arguments, List<int> positions) = (Rent(), new()); ; Move())
        {
            arguments.Add(ParseConditional());
            positions.Add(start);
            if (kind != Token.Comma)
            {
                // Check and skip right parenthesis.
                CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
                return (arguments, positions);
            }
        }
    }

    private Expression ParseClassMethod(string className, string methodName)
    {
        SkipFunctor();
        if (!classMethods.TryGetValue(className + "." + methodName, out MethodList info))
            throw Error($"Invalid class method name: {className}::{methodName}");
        return info.Methods.Length == 1
            ? ParseClassSingleMethod(info.Methods[0])
            : ParseClassMultiMethod(info);
    }

    private Expression ParseClassSingleMethod(in MethodData method)
    {
        Type[] types = method.Args;
        Type delType = typeof(Delegate), idxType = typeof(Index);
        Type dType = typeof(double), iType = typeof(int);
        List<Expression> args = Rent(types.Length);
        for (int i = 0; i < types.Length; i++, Move())
        {
            Type currentType = types[i];
            if (currentType.IsAssignableTo(delType))
                args.Add(ParseLambda(currentType));
            else if (currentType == idxType)
                args.Add(ParseIndex());
            else if (currentType.IsArray)
            {
                Type subType = currentType.GetElementType()!;
                for (List<Expression> items = Rent(); ; Move())
                {
                    Expression e = ParseLightConditional();
                    if (e.Type != subType)
                        throw Error($"Expected {subType.Name}");
                    items.Add(e);
                    if (kind != Token.Comma)
                    {
                        args.Add(subType.Make(items));
                        Return(items);
                        break;
                    }
                }
                break;
            }
            else
            {
                Expression e = ParseLightConditional();
                if (e.Type == currentType)
                    args.Add(e);
                else if (currentType == dType && e.Type == iType)
                    args.Add(ToDouble(e));
                else if (currentType == typeof(Complex) && IsArithmetic(e))
                    args.Add(Expression.Convert(ToDouble(e), typeof(Complex)));
                else
                    throw Error($"Expected {currentType.Name}");
            }
            if (kind != Token.Comma)
            {
                if (++i == types.Length - 1)
                {
                    currentType = types[i];
                    if (currentType == typeof(Random) || currentType == typeof(NormalRandom))
                        args.Add(currentType.New());
                    else if (currentType == typeof(One))
                        args.Add(Expression.Constant(1d));
                    else
                        throw Error($"Invalid number of arguments");
                }
                break;
            }
        }
        Expression result = method.GetExpression(args);
        Return(args);
        // Check and skip right parenthesis.
        CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
        return result;
    }

    private Expression ParseClassMultiMethod(in MethodList info)
    {
        List<Expression> args = Rent();
        List<int> starts = new();
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
                    if ((mask & m) != 0)
                    {
                        MethodData md = info.Methods[j];
                        if (md.GetMask(i) != lambdaType)
                            mask &= ~m;
                        else if (lambda == null)
                            lambda = md.Args[i];
                        else if (md.Args[i] != lambda)
                            throw Error("Inconsistent lambda types");
                    }
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
                    if ((mask & m) != 0)
                    {
                        MethodData md = info.Methods[j];
                        if (md.ExpectedArgs < i || md.GetMask(i) >= MethodData.Mλ1 ||
                            !CanConvert(last.Type, md.Args[Math.Min(i, md.Args.Length - 1)]))
                            mask &= ~m;
                    }
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
                if ((mask & m) != 0)
                {
                    var md = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(info.Methods), j);
                    if (md.ExpectedArgs != args.Count)
                    {
                        Type? act = args[^1].Type, form = md.Args[^1].GetElementType();
                        if (md.ExpectedArgs != int.MaxValue ||
                            form != act && (form != typeof(double) || act != typeof(int)))
                            mask &= ~m;
                    }
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
        if (mth.ExpectedArgs < mth.Args.Length)
            args.Add(mth.Args[^1] is var t && (t == typeof(Random) || t == typeof(NormalRandom))
                ? t.New()
                : Expression.Constant(mth.Args[^1] == typeof(One) ? 1d : 0d));
        for (int i = 0; i < mth.ExpectedArgs; i++)
        {
            Type expected = mth.Args[i], actual = args[i].Type;
            if (actual != expected)
            {
                if (expected == typeof(double) && actual == typeof(int))
                    args[i] = ToDouble(args[i]);
                else if (expected == typeof(Complex) &&
                    (actual == typeof(int) || actual == typeof(double)))
                    args[i] = Expression.Convert(ToDouble(args[i]), typeof(Complex));
                else if (expected.IsArray)
                {
                    Type et = expected.GetElementType()!;
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
        List<Expression> items = Rent();
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
            return local;
        // Check macro definitions.
        Definition? def = source.GetDefinition(ident);
        if (def != null)
        {
            if (isParsingDefinition)
                references.Add(def);
            return def.Expression;
        }
        // Check the global scope.
        object? val = isParsingDefinition
            ? source.GetPersistedValue(ident)
            : source[ident];
        if (val != null)
            return val switch
            {
                double dv => Expression.Constant(dv),
                int iv => Expression.Constant(iv),
                bool bv => Expression.Constant(bv),
                string sv => Expression.Constant(sv),
                _ => memos.TryGetValue(ident, out Expression? memo)
                    ? memo
                    : memos[ident] = Expression.Convert(
                        Expression.Property(sourceParameter, "Item",
                        Expression.Constant(ident)), val.GetType())
            };
        if (ident == "π")
            return Expression.Constant(Math.PI);
        if (ident == "τ")
            return Expression.Constant(Math.Tau);
        switch (ident.ToLower())
        {
            case "e": return Expression.Constant(Math.E);
            case "i": return Expression.Constant(Complex.ImaginaryOne);
            case "pi": return Expression.Constant(Math.PI);
            case "today": return Expression.Constant(Date.Today);
            case "pearl": return Expression.Call(typeof(F).Get(nameof(F.Austra)));
            case "random": return Expression.Call(typeof(F).GetMethod(nameof(F.Random))!);
            case "nrandom": return Expression.Call(typeof(F).GetMethod(nameof(F.NRandom))!);
        }
        if (TryParseMonthYear(ident, out Date d))
            return Expression.Constant(d);
        // Check if we tried to reference a SET variable in a DEF.
        if (isParsingDefinition && source[ident] != null)
            throw Error("SET variables cannot be used in persistent definitions", pos);
        throw Error($"Unknown variable: {ident}", pos);
    }
}
