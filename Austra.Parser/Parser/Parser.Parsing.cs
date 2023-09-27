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
        for (int orLex = start; ; Move())
        {
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
            orLex = start;
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
                        if (!AreArithmeticTypes(e1, e2))
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
        while (kind == Token.Plus || kind == Token.Minus)
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
                                        typeof(Vector).GetMethod(nameof(Vector.Combine),
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
        while (kind >= Token.Times && kind <= Token.Mod)
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
        if (kind == Token.Minus || kind == Token.Plus)
        {
            (Token opKind, int opPos) = (kind, start);
            Move();
            Expression e1 = ParseUnary();
            return e1.Type != typeof(Complex) && !IsArithmetic(e1)
                && !IsVectorOrMatrix(e1) && e1.Type != typeof(Series)
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
                return falseExpr;
            case Token.True:
                Move();
                return trueExpr;
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
                    (string className, int p) = (id.ToLower(), start);
                    // Skip class name and double colon.
                    Skip2();
                    e = kind != Token.Functor
                        ? throw Error("Method name expected")
                        : className switch
                        {
                            "matrix" => ParseMatrixMethod(),
                            "vector" => ParseVectorMethod(),
                            "complexvector" => ParseComplexVectorMethod(),
                            "series" => ParseSeriesMethod(),
                            "model" => ParseModelMethod(),
                            "spline" => ParseSplineMethod(),
                            _ => throw Error("Unknown class name", p)
                        };
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
                        : e.Type == typeof(Matrix) || e.Type == typeof(LMatrix)
                            || e.Type == typeof(RMatrix)
                        ? Expression.Call(e, e.Type.Get(nameof(Matrix.Transpose)))
                        : e.Type == typeof(Complex)
                        ? e.Type.Call(null, nameof(Complex.Conjugate), e)
                        : throw Error("Can only transpose a matrix or conjugate a complex vector");
                    Move();
                    break;
                case Token.LBra:
                    Move();
                    e = e.Type == typeof(Vector) || e.Type == typeof(Series<int>)
                            || e.Type == typeof(ComplexVector)
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
                    e = e.Type == typeof(Vector) || e.Type == typeof(Series)
                        || e.Type == typeof(ComplexVector) || e.Type == typeof(Series<int>)
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
        return e1.Type != expected && (expected != typeof(double) || !IsArithmetic(e1))
            ? throw Error("Invalid index type")
            : Expression.Property(e, "Item", ToDouble(e1));
    }

    private Expression ParseIndexer(Expression e, bool allowSlice)
    {
        bool fromEnd1 = false, fromEnd2 = false;
        Expression e1 = kind == Token.Colon && allowSlice
            ? Expression.Constant(0)
            : ParseIndex(ref fromEnd1);
        if (allowSlice && kind == Token.Colon)
        {
            Move();
            Expression e2 = kind == Token.RBra
                ? Expression.Constant(Index.End)
                : Expression.New(indexCtor,
                    ParseIndex(ref fromEnd2), Expression.Constant(fromEnd2));
            CheckAndMove(Token.RBra, "] expected in indexer");
            return Expression.Property(e, "Item", Expression.New(rangeCtor,
                Expression.New(indexCtor, e1, Expression.Constant(fromEnd1)), e2));
        }
        CheckAndMove(Token.RBra, "] expected in indexer");
        if (fromEnd1)
            e1 = Expression.New(indexCtor, e1, Expression.Constant(fromEnd1));
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
                bool fromEnd12 = false;
                Expression e12 = kind == Token.Comma
                    ? Expression.Constant(Index.End)
                    : Expression.New(indexCtor,
                        ParseIndex(ref fromEnd12), Expression.Constant(fromEnd12));
                if (e1.Type != typeof(Index))
                    e1 = Expression.New(indexCtor, e1, Expression.Constant(fromEnd11));
                e1 = Expression.New(rangeCtor, e1, e12);
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
                bool fromEnd22 = false;
                Expression e22 = kind == Token.RBra
                    ? Expression.Constant(Index.End)
                    : Expression.New(indexCtor,
                        ParseIndex(ref fromEnd22), Expression.Constant(fromEnd22));
                if (e2.Type != typeof(Index))
                    e2 = Expression.New(indexCtor, e2, Expression.Constant(fromEnd21));
                e2 = Expression.New(rangeCtor, e2, e22);
                if (!isRange && fromEnd11)
                    e1 = Expression.New(indexCtor, e1!, trueExpr);
                isRange = true;
            }
            else if (isRange && fromEnd21)
                e2 = Expression.New(indexCtor, e2, trueExpr);
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
                e1 = Expression.New(indexCtor, e1, Expression.Constant(fromEnd11));
            if (e2 != null)
                e2 = Expression.New(indexCtor, e2, Expression.Constant(fromEnd21));
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
                        ? Expression.New(indexCtor, e1, trueExpr)
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
                ? Expression.New(indexCtor, e1, Expression.Constant(fromEnd1))
                : Expression.Constant(Index.Start);
            e2 = e2 != null
                ? Expression.New(indexCtor, e2, Expression.Constant(fromEnd2))
                : Expression.Constant(Index.End);
            return Expression.Property(e,
                typeof(Series).GetProperty("Item", new[] { typeof(Range) })!,
                Expression.New(rangeCtor, e1, e2));
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

    private Expression ParseSeriesMethod()
    {
        (string method, int pos) = (id.ToLower(), start);
        (List<Expression> args, List<int> p) = ParseArguments();
        return method switch
        {
            "new" => args.Count < 2
                ? throw Error("NEW expects a vector and a list of series")
                : args[0].Type != typeof(Vector)
                ? throw Error("Vector expected", p[0])
                : args.Skip(1).Any(e => e.Type != typeof(Series))
                ? throw Error("NEW expects a vector and a list of series")
                : typeof(Series).Call(nameof(Series.Combine),
                    args[0], typeof(Series).Make(args.Skip(1))),
            _ => throw Error("Unknown method name", pos),
        };
    }

    private Expression ParseSplineMethod()
    {
        (string method, int pos) = (id.ToLower(), start);
        if (method == "grid")
        {
            Skip2();
            Expression e1 = ParseLightConditional();
            if (!IsArithmetic(e1))
                throw Error("Lower bound must be double");
            CheckAndMove(Token.Comma, "Comma expected");
            Expression e2 = ParseLightConditional();
            if (!IsArithmetic(e2))
                throw Error("Upper bound must be double");
            CheckAndMove(Token.Comma, "Comma expected");
            Expression e3 = ParseLightConditional();
            if (e3.Type != typeof(int))
                throw Error("The number of segments must be an integer");
            CheckAndMove(Token.Comma, "Comma expected");
            return typeof(VectorSpline).New(ToDouble(e1), ToDouble(e2), e3,
                ParseLambda(typeof(double), null, typeof(double)));

        }
        (List<Expression> a, List<int> _) = ParseArguments();
        return method switch
        {
            "new" => a.Count != 2 || a[0].Type != typeof(Vector) || a[1].Type != typeof(Vector)
                ? throw Error("Two vectors expected")
                : typeof(VectorSpline).New(a[0], a[1]),
            _ => throw Error("Unknown method name", pos),
        }; ;
    }

    private Expression ParseModelMethod()
    {
        (string method, int pos) = (id.ToLower(), start);
        (List<Expression> a, List<int> p) = ParseArguments();
        return method switch
        {
            "compare" or "comp" => a.Count != 2 || a[0].Type != a[1].Type
                ? throw Error("Two arguments expected for comparison")
                : a[0].Type == typeof(Vector)
                ? typeof(Tuple<Vector, Vector>).New(a)
                : a[0].Type == typeof(ComplexVector)
                ? typeof(Tuple<ComplexVector, ComplexVector>).New(a)
                : a[0].Type == typeof(Series)
                ? typeof(Tuple<Series, Series>).New(a)
                : throw Error("Invalid argument type"),
            "mvo" => a.Count < 2
                ? throw Error("Invalid number of parameters")
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

    private Expression ParseVectorMethod()
    {
        (string method, int pos) = (id.ToLower(), start);
        if (method == "new")
            return ParseNewVectorLambda(typeof(Vector));
        (List<Expression> a, _) = ParseArguments();
        return method switch
        {
            "nrandom" => a.Count != 1 || a[0].Type != typeof(int)
                ? throw Error("Vector size expected")
                : typeof(Vector).New(a.AddNormalRandom()),
            "random" => a.Count != 1 || a[0].Type != typeof(int)
                ? throw Error("Vector size expected")
                : typeof(Vector).New(a.AddRandom()),
            "zero" => a.Count != 1 || a[0].Type != typeof(int)
                ? throw Error("Vector size expected")
                : typeof(Vector).New(a[0]),
            "ones" => a.Count != 1 || a[0].Type != typeof(int)
                ? throw Error("Vector size expected")
                : typeof(Vector).New(a.AddExp(Expression.Constant(1.0))),
            _ => throw Error("Unknown method name", pos),
        };
    }

    private Expression ParseComplexVectorMethod()
    {
        (string method, int pos) = (id.ToLower(), start);
        if (method == "new")
            return ParseNewVectorLambda(typeof(ComplexVector));
        (List<Expression> e, _) = ParseArguments();
        return method switch
        {
            "nrandom" => e.Count != 1 || e[0].Type != typeof(int)
                ? throw Error("Vector size expected")
                : typeof(ComplexVector).New(e.AddNormalRandom()),
            "random" => e.Count != 1 || e[0].Type != typeof(int)
                ? throw Error("Vector size expected")
                : typeof(ComplexVector).New(e.AddRandom()),
            "zero" => e.Count != 1 || e[0].Type != typeof(int)
                ? throw Error("Vector size expected")
                : typeof(ComplexVector).New(e[0]),
            "from" => e.Count == 1 && e[0].Type == typeof(Vector)
                ? typeof(ComplexVector).New(e[0])
                : e.Count == 2 || e[0].Type == typeof(Vector)
                    || e[1].Type == typeof(Vector)
                ? typeof(ComplexVector).New(e[0], e[1])
                : throw Error("One or two vectors expected"),
            _ => throw Error("Unknown method name", pos),
        };
    }

    private Expression ParseMatrixMethod()
    {
        (string method, int pos) = (id.ToLower(), start);
        if (method == "new")
            return ParseNewMatrixLambda();
        (List<Expression> a, _) = ParseArguments();
        return method switch
        {
            "rows" => a.Any(e => e.Type != typeof(Vector))
                ? throw Error("List of vectors expected")
                : typeof(Matrix).New(typeof(Vector).Make(a)),
            "cols" => a.Any(e => e.Type != typeof(Vector))
                ? throw Error("List of vectors expected")
                : Expression.Call(typeof(Matrix).New(typeof(Vector).Make(a)),
                    typeof(Matrix).Get(nameof(Matrix.Transpose))),
            "diag" => a.Count == 1 && a[0].Type == typeof(Vector)
                ? typeof(Matrix).New(a[0])
                : a.Count > 1 && a.All(IsArithmetic)
                ? typeof(Matrix).New(typeof(Vector).New(
                    typeof(double).Make(a.Select(ToDouble))))
                : throw Error("Vector expected"),
            "eye" or "i" => a.Count != 1 || a[0].Type != typeof(int)
                ? throw Error("Matrix size expected")
                : Expression.Call(typeof(Matrix).Get(nameof(Matrix.Identity)), a[0]),
            "random" => CheckMatrixSize(a)
                ? typeof(Matrix).New(a.AddRandom())
                : throw Error("Matrix size expected"),
            "nrandom" => CheckMatrixSize(a)
                ? typeof(Matrix).New(a.AddNormalRandom())
                : throw Error("Matrix size expected"),
            "lrandom" => CheckMatrixSize(a)
                ? typeof(LMatrix).New(a.AddRandom())
                : throw Error("Matrix size expected"),
            "lnrandom" or "nlrandom" => CheckMatrixSize(a)
                ? typeof(LMatrix).New(a.AddNormalRandom())
                : throw Error("Matrix size expected"),
            "zero" or "zeros" => CheckMatrixSize(a)
                ? typeof(Matrix).New(a)
                : throw Error("Matrix size expected"),
            "cov" or "covariance" => a.Any(e => e.Type != typeof(Series))
                ? throw Error("List of series expected")
                : Expression.Call(
                    typeof(Series<Date>).Get(nameof(Series.CovarianceMatrix)),
                    typeof(Series).Make(a)),
            "corr" or "correlation" => a.Any(e => e.Type != typeof(Series))
                ? throw Error("List of series expected")
                : Expression.Call(
                    typeof(Series<Date>).Get(nameof(Series.CorrelationMatrix)),
                    typeof(Series).Make(a)),
            _ => throw Error("Unknown method name", pos),
        };

        static bool CheckMatrixSize(List<Expression> a) =>
            a.Count is 1 or 2 || a.All(e => e.Type == typeof(int));
    }

    private Expression ParseNewMatrixLambda()
    {
        // Skip method name and left parenthesis.
        Skip2();
        Expression e1 = ParseLightConditional();
        if (e1.Type != typeof(int))
            throw Error($"Rows must be integer");
        CheckAndMove(Token.Comma, "Comma expected");
        if (IsLambda())
            return typeof(Matrix).New(e1,
                ParseLambda(typeof(int), typeof(int), typeof(double)));
        Expression e2 = ParseLightConditional();
        if (e2.Type != typeof(int))
            throw Error($"Columns must be integer");
        CheckAndMove(Token.Comma, "Comma expected");
        return typeof(Matrix).New(e1, e2,
            ParseLambda(typeof(int), typeof(int), typeof(double)));
    }

    private Expression ParseNewVectorLambda(Type type)
    {
        // Skip method name and left parenthesis.
        Skip2();
        Expression e1 = ParseLightConditional();
        if (e1.Type == typeof(Vector))
        {
            List<Expression> args = new() { e1 };
            while (kind == Token.Comma)
            {
                Move();
                args.Add(ParseLightConditional());
            }
            // Check and skip right parenthesis.
            CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
            return args.Count == 1 || args.Skip(1).Any(e => e.Type != typeof(Vector))
                ? throw Error("NEW expects a list of vectors")
                : typeof(Vector).Call(nameof(Vector.Combine),
                    args[0], typeof(Vector).Make(args.Skip(1)));
        }
        if (e1.Type != typeof(int))
            throw Error($"Vector size must be integer");
        CheckAndMove(Token.Comma, "Comma expected");
        Type retType = type == typeof(Vector) ? typeof(double) : typeof(Complex);
        return kind == Token.Id
            ? type.New(e1, ParseLambda(typeof(int), null, retType))
            : type.New(e1, ParseLambda(typeof(int), type, retType));
    }

    private Expression ParseMethod(Expression e)
    {
        string meth = id;
        if (!methods.TryGetValue(e.Type, out Dictionary<string, MethodInfo>? dict) ||
            !dict.TryGetValue(meth, out MethodInfo? mInfo))
            throw Error($"Invalid method: {meth}");
        ParameterInfo[] paramInfo = mInfo.GetParameters();
        Type firstParam = paramInfo[0].ParameterType;
        if (paramInfo.Length == 2 &&
            paramInfo[1].ParameterType.IsAssignableTo(typeof(Delegate)))
        {
            // This is a zip or reduce method call.
            Skip2();
            Expression e1 = ParseConditional();
            if (e1.Type != firstParam)
                if (firstParam == typeof(double) && IsArithmetic(e1))
                    e1 = ToDouble(e1);
                else if (firstParam == typeof(Complex) && IsArithmetic(e1))
                    e1 = Expression.Convert(ToDouble(e1), typeof(Complex));
                else
                    throw Error($"{firstParam.Name} expected");
            CheckAndMove(Token.Comma, "Comma expected");
            Type[] genTypes = paramInfo[1].ParameterType.GenericTypeArguments;
            Expression λ = ParseLambda(genTypes[0], genTypes[1], genTypes[^1]);
            return Expression.Call(e, mInfo, e1, λ);
        }
        if (firstParam.IsAssignableTo(typeof(Delegate)))
        {
            // Skip method name and left parenthesis.
            Skip2();
            Type[] genTypes = firstParam.GenericTypeArguments;
            return Expression.Call(e, mInfo, ParseLambda(genTypes[0], null, genTypes[^1]));
        }
        if (firstParam == typeof(Index))
        {
            // Skip method name and left parenthesis.
            Skip2();
            bool fromEnd = false;
            Expression e1 = ParseIndex(ref fromEnd);
            CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
            return Expression.Call(e, mInfo,
                Expression.New(indexCtor, e1, Expression.Constant(fromEnd)));
        }
        (List<Expression> a, List<int> p) = ParseArguments();
        if (firstParam == typeof(Series[]) || firstParam == typeof(Vector[]))
            return a.Any(a => a.Type != e.Type)
                ? throw Error(e.Type == typeof(Series) ?
                    "Series list expected" : "Vector list expected")
                : Expression.Call(e, mInfo, e.Type.Make(a));
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
        return Expression.Call(e, mInfo, a[0]);
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

    private Expression ParseLambda(Type t1, Type? t2, Type retType, bool isLast = true)
    {
        try
        {
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
            Expression body = ParseConditional();
            if (body.Type != retType)
                body = retType == typeof(Complex) && IsArithmetic(body)
                    ? Expression.Convert(body, typeof(Complex))
                    : retType == typeof(double) && IsArithmetic(body)
                    ? ToDouble(body)
                    : throw Error($"Expected return type is {retType.Name}");
            if (isLast)
                CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
            else
                CheckAndMove(Token.Comma, "Comma expected");
            return lambdaParameter2 != null
                ? Expression.Lambda(body, lambdaParameter, lambdaParameter2)
                : Expression.Lambda(body, lambdaParameter);
        }
        finally
        {
            lambdaParameter = null;
            lambdaParameter2 = null;
        }
    }

    private Expression ParseProperty(Expression e)
    {
        string prop = id;
        if (allProps.TryGetValue(e.Type, out Dictionary<string, MethodInfo>? dict) &&
            dict.TryGetValue(prop, out MethodInfo? mInfo))
        {
            Move();
            return Expression.Call(e, mInfo);
        }
        throw Error($"Invalid property: {prop}");
    }

    /// <summary>Parses a global function call.</summary>
    /// <returns>An expression representing the function call.</returns>
    private Expression ParseFunction()
    {
        (string function, int pos) = (id.ToLower(), start);
        if (function == "solve")
        {
            Skip2();
            Expression λf = ParseLambda(typeof(double), null, typeof(double), false);
            Expression λdf = ParseLambda(typeof(double), null, typeof(double), false);
            (List<Expression> a1, List<int> p1) = CollectArguments();
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
                ? throw Error("Too many arguments")
                : Expression.Call(typeof(Solver).Get("Solve"), a1);
        }
        (List<Expression> a, List<int> p) = ParseArguments();
        switch (function)
        {
            case "round":
                return a.Count is < 1 or > 2
                    ? throw Error("Function 'round' requires 2 or 3 arguments", pos)
                    : !IsArithmetic(a[0])
                    ? throw Error("First argument must be numeric")
                    : a.Count == 2 && a[1].Type != typeof(int)
                    ? throw Error("Second argument must be integer")
                    : a.Count == 1
                    ? Expression.Call(typeof(Math).GetMethod("Round", doubleArg)!, ToDouble(a[0]))
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
                        ? throw Error("Arguments must be numeric")
                        : a[0].Type == typeof(int) && a[1].Type == typeof(int)
                        ? typeof(Math).Call(fName, a[0], a[1])
                        : typeof(Math).Call(fName, ToDouble(a[0]), ToDouble(a[1]));
                }
            case "beta":
                return a.Count != 2 || !IsArithmetic(a[0]) || !IsArithmetic(a[1])
                    ? throw Error("Arguments must be numeric")
                    : typeof(F).Call(nameof(F.Beta), ToDouble(a[0]), ToDouble(a[1]));
            case "compare":
            case "comp":
                return a.Count != 2 || a[0].Type != a[1].Type
                    ? throw Error("Two arguments expected for comparison")
                    : a[0].Type == typeof(Vector)
                    ? typeof(Tuple<Vector, Vector>).New(a)
                    : a[0].Type == typeof(ComplexVector)
                    ? typeof(Tuple<ComplexVector, ComplexVector>).New(a)
                    : a[0].Type == typeof(Series)
                    ? typeof(Tuple<Series, Series>).New(a)
                    : throw Error("Invalid argument type");
            case "complex":
                return a.Count == 1 && IsArithmetic(a[0])
                    ? Expression.Convert(a[0], typeof(Complex))
                    : a.Count != 2 || !AreArithmeticTypes(a[0], a[1])
                    ? throw Error("Arguments must be numeric")
                    : typeof(Complex).New(ToDouble(a[0]), ToDouble(a[1]));
            case "polar":
                return a.Count == 1 && IsArithmetic(a[0])
                    ? Expression.Convert(a[0], typeof(Complex))
                    : a.Count != 2 || !AreArithmeticTypes(a[0], a[1])
                    ? throw Error("Arguments must be numeric")
                    : typeof(Complex).Call(nameof(Complex.FromPolarCoordinates), ToDouble(a[0]), ToDouble(a[1]));
            case "polyeval":
                return ParsePolyMethod(nameof(Polynomials.PolyEval));
            case "polyderiv":
            case "polyderivative":
                return ParsePolyMethod(nameof(Polynomials.PolyDerivative));
            case "polysolve":
                {
                    MethodInfo info = typeof(Polynomials).GetMethod(
                        nameof(Polynomials.PolySolve),
                        new[] { typeof(Vector) })!;
                    return a.Count > 0 && a.All(IsArithmetic)
                        ? Expression.Call(info, typeof(Vector).New(
                            typeof(double).Make(a.Select(ToDouble).ToArray())))
                        : a.Count != 1 || a[0].Type != typeof(Vector)
                        ? throw Error("Argument must be a vector")
                        : Expression.Call(info, a[0]);
                }
        }
        if (a.Count == 1 && a[0].Type == typeof(Complex))
        {
            MethodInfo? info = typeof(Complex).GetMethod(function,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase,
                new[] { typeof(Complex) });
            return info is null
                ? throw Error("Invalid function name", pos)
                : Expression.Call(info, a[0]);
        }
        return !functions.TryGetValue(function, out MethodInfo? mInfo)
            ? throw Error("Invalid function name", pos)
            : a.Count != 1 || !IsArithmetic(a[0])
            ? throw Error("Argument must be numeric")
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

    private (List<Expression>, List<int>) ParseArguments()
    {
        // Skip method name and left parenthesis.
        Skip2();
        return CollectArguments();
    }

    private (List<Expression>, List<int>) CollectArguments()
    {
        (List<Expression> arguments, List<int> positions) = (new(), new());
        for (; ; Move())
        {
            arguments.Add(ParseConditional());
            positions.Add(start);
            if (kind != Token.Comma)
                break;
        }
        // Check and skip right parenthesis.
        CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
        return (arguments, positions);
    }

    private Expression ParseVectorLiteral()
    {
        Move();
        List<Expression> items = new();
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
        if (matrices > 0)
            return period > 1 || vectors + matrices != items.Count || items.Count > 2
                ? throw Error("Invalid matrix concatenation")
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
            throw Error("Inconsistent matrix size");
        Expression args = typeof(double).Make(items);
        return period != 0
            ? typeof(Matrix).New(
                Expression.Constant(items.Count / period), Expression.Constant(period), args)
            : typeof(Vector).New(args);
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
