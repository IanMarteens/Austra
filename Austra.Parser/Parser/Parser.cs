namespace Austra.Parser;

/// <summary>Syntactic and lexical analysis for AUSTRA.</summary>
internal sealed partial class Parser : Scanner, IDisposable
{
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] VectorVectorArg = [typeof(DVector), typeof(DVector)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] DoubleVectorArg = [typeof(double), typeof(DVector)];

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
    /// <summary>The <see cref="Expression"/> for <c>0</c>.</summary>
    private static readonly ConstantExpression ZeroExpr = Expression.Constant(0);
    /// <summary>The <see cref="Expression"/> for <see cref="Complex.ImaginaryOne"/>.</summary>
    private static readonly ConstantExpression ImExpr = Expression.Constant(Complex.ImaginaryOne);
    /// <summary>The <see cref="Expression"/> for <see cref="Math.PI"/>.</summary>
    private static readonly ConstantExpression PiExpr = Expression.Constant(Math.PI);
    /// <summary>The <see cref="Expression"/> for <see langword="null"/>.</summary>
    private static readonly ConstantExpression NullExpr = Expression.Constant(null);
    /// <summary>Reference to the <see cref="Random.Shared"/> property.</summary>
    private static readonly Expression RandomExpr =
        Expression.Property(null, typeof(Random), nameof(Random.Shared));
    /// <summary>Method for multiplying by a transposed matrix.</summary>
    private static readonly MethodInfo MatrixMultiplyTranspose =
        typeof(Matrix).GetMethod(nameof(Matrix.MultiplyTranspose), [typeof(Matrix)])!;
    /// <summary>Method for multiplying a vector by a transposed matrix.</summary>
    private static readonly MethodInfo MatrixTransposeMultiply =
        typeof(Matrix).Get(nameof(Matrix.TransposeMultiply));
    /// <summary>Method for linear vector combinations.</summary>
    private static readonly MethodInfo VectorCombine2 =
        typeof(DVector).GetMethod(nameof(DVector.Combine2),
            [typeof(double), typeof(double), typeof(DVector), typeof(DVector)])!;
    /// <summary>Method for linear vector combinations.</summary>
    private static readonly MethodInfo MatrixCombine =
        typeof(Matrix).GetMethod(nameof(Matrix.MultiplyAdd),
            [typeof(DVector), typeof(double), typeof(DVector)])!;
    /// <summary>Method for squaring a matrix.</summary>
    private static readonly MethodInfo MatrixSquare =
        typeof(Matrix).GetMethod(nameof(Matrix.Square))!;
    /// <summary>Method for squaring a lower-triangular matrix.</summary>
    private static readonly MethodInfo LMatrixSquare =
        typeof(LMatrix).GetMethod(nameof(LMatrix.Square))!;
    /// <summary>Method for squaring an upper-triangular matrix.</summary>
    private static readonly MethodInfo RMatrixSquare =
        typeof(RMatrix).GetMethod(nameof(RMatrix.Square))!;
    /// <summary>Method for cloning complex sequences.</summary>
    private static readonly MethodInfo CSeqClone =
        typeof(CSequence).GetMethod(nameof(CSequence.Clone))!;
    /// <summary>Method for cloning real sequences.</summary>
    private static readonly MethodInfo DSeqClone =
        typeof(DSequence).GetMethod(nameof(DSequence.Clone))!;
    /// <summary>Method for cloning integer sequences.</summary>
    private static readonly MethodInfo NSeqClone =
        typeof(NSequence).GetMethod(nameof(NSequence.Clone))!;

    /// <summary>Predefined classes and methods.</summary>
    private readonly Bindings bindings;
    /// <summary>Gets the outer scope for variables.</summary>
    private readonly IDataSource source;
    /// <summary>Place holder for lambda arguments, if any.</summary>
    private LambdaBlock lambdaBlock;
    /// <summary>Referenced definitions.</summary>
    private readonly HashSet<Definition> references = [];

    /// <summary>All top-level locals, from LET clauses.</summary>
    private readonly List<ParameterExpression> letLocals = new(8);
    /// <summary>Top-level local asignment expressions.</summary>
    private readonly List<Expression> letExpressions;
    /// <summary>Transient local variable definitions.</summary>
    /// <remarks>
    /// This data structure is redundant with respect to <see cref="letLocals"/>, but
    /// it's faster to search, while <see cref="letLocals"/> is needed for code generation.
    /// </remarks>
    private readonly Dictionary<string, ParameterExpression> locals =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>User-defined lambdas, indexed by name.</summary>
    private readonly Dictionary<string, ParameterExpression> localLambdas =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Top-level SET expressions.</summary>
    private readonly List<Expression> setExpressions;
    /// <summary>New session variables that are not yet defined in the data source.</summary>
    private readonly Dictionary<string, Expression> pendingSets =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Controls that only persisted values are used.</summary>
    private bool isParsingDefinition;
    /// <summary>Are we parsing a lambda header?</summary>
    private bool parsingLambdaHeader;
    /// <summary>Is the current definition a recursive function?</summary>
    private bool isDefRecursive;
    /// <summary>Are we just trying to identify data types?</summary>
    private bool parsingTypes;

    /// <summary>Used for recursive definitions.</summary>
    private ParameterExpression? currentDefinitionLambda;

    /// <summary>Initializes a parsing context.</summary>
    /// <param name="bindings">Predefined classes and methods.</param>
    /// <param name="source">Environment variables.</param>
    /// <param name="text">Text of the formula.</param>
    public Parser(Bindings bindings, IDataSource source, string text) : base(text) =>
        (this.bindings, this.source, lambdaBlock, letExpressions, setExpressions)
            = (bindings, source, bindings.LambdaBlock, source.Rent(8), source.Rent(8));

    /// <summary>Returns allocated resources to the pool, in the data source.</summary>
    public void Dispose()
    {
        lambdaBlock.Clean();
        source.Return(setExpressions);
        source.Return(letExpressions);
    }

    /// <summary>Compiles a list of statements into a block expression.</summary>
    /// <returns>A block expression.</returns>
    public Expression ParseStatement()
    {
        for (; kind != Token.Eof; Move())
        {
            if (kind != Token.Semicolon)
                if (kind != Token.Set)
                {
                    int from = start;
                    Expression e = ParseFormula("", true);
                    if (e is not ConstantExpression { Value: null })
                    {
                        int to = kind == Token.Eof ? start + 1 : start;
                        letExpressions.Add(source.GetEnqueueExpression(e));
                        source.Listener?.EnqueueRange(new Range(from, to));
                    }
                }
                else
                    letExpressions.Add(ParseAssignment());
            if (kind != Token.Semicolon)
                break;
        }
        if (kind != Token.Eof)
            throw Error("Extra input after expression");
        return letExpressions.Count switch
        {
            0 => NullExpr,
            1 when letLocals.Count == 0 => letExpressions[0],
            _ => Expression.Block(letLocals, letExpressions),
        };
    }

    /// <summary>Parses a block expression without generating code.</summary>
    /// <returns>The type of the block expression.</returns>
    public Type[] ParseType(int abortPosition = int.MaxValue)
    {
        this.abortPosition = abortPosition;
        parsingTypes = true;
        // Check first for a definition header and parse it.
        if (kind == Token.Def)
        {
            Move();
            if (kind != Token.Id && kind != Token.Functor)
                throw Error("Definition name expected");
            string defName = id;
            Move();
            if (kind == Token.Colon)
            {
                Move();
                CheckAndMove(Token.Str, "Definition description expected");
            }
            Expression e;
            if (kind == Token.LPar)
            {
                List<ParameterExpression> parameters = ParseParameters();
                if (kind == Token.Colon)
                {
                    Type retType = ParseTypeRef();
                    currentDefinitionLambda = Expression.Variable(BindResultType(parameters, retType), defName);
                    CheckAndMove(Token.Eq, "= expected");
                    isParsingDefinition = true;
                    e = lambdaBlock.Create(this, ParseFormula("", false, false, true), retType);
                    if (isDefRecursive)
                    {
                        e = Expression.Block([currentDefinitionLambda], Expression.Assign(currentDefinitionLambda, e));
                        e = Expression.Lambda(Expression.Invoke(e, parameters), parameters);
                    }
                }
                else
                {
                    CheckAndMove(Token.Eq, "= expected");
                    isParsingDefinition = true;
                    e = ParseFormula("", false, false, true);
                    e = lambdaBlock.Create(this, e, e.Type);
                }
            }
            else
            {
                CheckAndMove(Token.Eq, "= expected");
                isParsingDefinition = true;
                e = ParseFormula("", false);
            }
            return kind != Token.Eof
                ? throw Error("Extra input after expression")
                : ([e.Type]);
        }
        List<Type> result = new(8);
        for (; kind != Token.Eof; Move())
        {
            if (kind != Token.Semicolon)
                if (kind != Token.Set)
                {
                    int from = start;
                    Expression e = ParseFormula("", false);
                    if (e is not ConstantExpression { Value: null })
                        result.Add(e.Type);
                }
                else
                {
                    do
                    {
                        // Skip either the SET keyword or the comma.
                        Move();
                        string leftValue = id;
                        CheckAndMove(Token.Id, "Left side variable expected");
                        if (kind != Token.Eof && kind != Token.Comma && kind != Token.Semicolon)
                        {
                            CheckAndMove(Token.Eq, "= expected");
                            Expression e = ParseFormula(leftValue, true);
                            if (e is BlockExpression bl && bl.Expressions.Count > 0)
                                e = bl.Expressions[^1];
                            if (e is BinaryExpression { NodeType: ExpressionType.Assign } be
                                && be.Right is UnaryExpression { NodeType: ExpressionType.Convert } ue)
                            {
                                pendingSets[leftValue] = ue.Operand;
                                result.Add(ue.Operand.Type);
                            }
                        }
                    }
                    while (kind == Token.Comma);
                }
            if (kind != Token.Semicolon)
                break;
        }
        return kind != Token.Eof
            ? throw Error("Extra input after expression")
            : [.. result];
    }

    /// <summary>Parse the formula up to a position and return local variables.</summary>
    /// <param name="position">Last position to parse.</param>
    /// <param name="parsingHeader">Are we inside a lambda header?</param>
    /// <returns>The list of LET variables and any possible active lambda parameter.</returns>
    public List<Member> ParseContext(int position, out bool parsingHeader)
    {
        try
        {
            ParseType(position);
        }
        catch { /* Ignore */ }
        if (parsingHeader = parsingLambdaHeader)
            return [];
        List<Member> result = new(locals.Count + 2);
        foreach (KeyValuePair<string, ParameterExpression> pair in locals)
            if (!pair.Key.StartsWith('$'))
                result.Add(new(pair.Key, $"Local variable: {pair.Value.Type.Name}"));
        lambdaBlock.GatherParameters(result);
        return result;
    }

    /// <summary>Parses a definition and adds it to the source.</summary>
    /// <returns>A new definition, on success.</returns>
    public Definition ParseDefinition()
    {
        CheckAndMove(Token.Def, "DEF expected");
        if (kind != Token.Id && kind != Token.Functor)
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
        Expression e;
        int first;
        string paramText = "";
        if (kind == Token.LPar)
        {
            int s0 = start;
            // A local function definition.
            List<ParameterExpression> parameters = ParseParameters();
            if (kind == Token.Colon)
            {
                Type retType = ParseTypeRef();
                currentDefinitionLambda = Expression.Variable(BindResultType(parameters, retType), defName);
                paramText = text[s0..start];
                CheckAndMove(Token.Eq, "= expected");
                first = start;
                isParsingDefinition = true;
                e = lambdaBlock.Create(this, ParseFormula("", false, false, true), retType);
                if (isDefRecursive)
                {
                    e = Expression.Block([currentDefinitionLambda], Expression.Assign(currentDefinitionLambda, e));
                    e = Expression.Lambda(Expression.Invoke(e, parameters), true, parameters);
                }
            }
            else
            {
                paramText = text[s0..start];
                CheckAndMove(Token.Eq, "= expected");
                first = start;
                isParsingDefinition = true;
                e = ParseFormula("", false, false, true);
                e = lambdaBlock.Create(this, e, e.Type);
            }
        }
        else
        {
            CheckAndMove(Token.Eq, "= expected");
            first = start;
            isParsingDefinition = true;
            e = ParseFormula("", false);
        }
        if (kind == Token.Semicolon)
            Move();
        if (kind != Token.Eof)
            throw Error("Extra input after expression");
        if (e.Type == typeof(Series))
            e = typeof(Series).Call(e, nameof(Series.SetName), Expression.Constant(defName));
        Definition def = new(defName, paramText, text[first..], description, e);
        foreach (Definition referenced in references)
            referenced.Children.Add(def);
        return def;
    }

    /// <summary>Compiles an assignment statement.</summary>
    /// <returns>A block expression.</returns>
    private Expression ParseAssignment()
    {
        do
        {
            // Skip either the SET keyword or the comma.
            Move();
            if (kind != Token.Id)
                throw Error("Left side variable expected");
            int namePos = start;
            string leftValue = id;
            Move();
            if (kind == Token.Eof || kind == Token.Comma || kind == Token.Semicolon)
            {
                setExpressions.Add(source.SetExpression(leftValue, NullExpr));
                if (!parsingTypes)
                    pendingSets.Remove(leftValue);
            }
            else
            {
                CheckAndMove(Token.Eq, "= expected");
                // Always allow deleting a session variable.
                if (source.GetDefinition(leftValue) != null)
                    throw Error($"{leftValue} already in use", namePos);
                setExpressions.Add(ParseFormula(leftValue, true));
                if (setExpressions[^1] is BinaryExpression { NodeType: ExpressionType.Assign } be
                    && be.Right is UnaryExpression { NodeType: ExpressionType.Convert } ue)
                    pendingSets[leftValue] = ue.Operand;
            }
        }
        while (kind == Token.Comma);
        return setExpressions.Count switch
        {
            0 => NullExpr,
            1 => setExpressions[0],
            _ => Expression.Block(setExpressions)
        };
    }

    /// <summary>Compiles a formula that returns a result.</summary>
    /// <param name="leftValue">When not empty, contains a variable name.</param>
    /// <param name="forceCast">Whether to force a cast to object.</param>
    /// <param name="checkEof">Whether to check for extra input.</param>
    /// <param name="valueExpected">Is a value expected after the IN keyword?</param>
    /// <returns>A block expression.</returns>
    private Expression ParseFormula(
        string leftValue,
        bool forceCast,
        bool checkEof = false,
        bool valueExpected = false)
    {
        int llCount = letLocals.Count, leCount = letExpressions.Count;
        if (kind == Token.Let)
        {
            ParseLetClause();
            if (kind == Token.Semicolon && !valueExpected)
                // LET variables remain in scope until the end of the script.
                return NullExpr;
            CheckAndMove(Token.In, "IN expected");
        }
        try
        {
            Expression rvalue = ParseConditional();
            if (forceCast)
                rvalue = Expression.Convert(rvalue, typeof(object));
            if (leftValue != "")
                rvalue = source.SetExpression(leftValue, rvalue);
            letExpressions.Add(rvalue);
            return checkEof && kind != Token.Eof
                ? throw Error("Extra input after expression")
                : letLocals.Count == llCount && letExpressions.Count == leCount + 1
                ? letExpressions[^1]
                : Expression.Block(
                    letLocals.GetRange(llCount, letLocals.Count - llCount),
                    letExpressions.GetRange(leCount, letExpressions.Count - leCount));
        }
        finally
        {
            while (llCount < letLocals.Count)
            {
                string name = letLocals[^1].Name!;
                localLambdas.Remove(name);
                locals.Remove(name);
                letLocals.RemoveAt(letLocals.Count - 1);
            }
            while (leCount < letExpressions.Count)
                letExpressions.RemoveAt(letExpressions.Count - 1);
        }
    }

    /// <summary>Parses a LET clause, either a script-level one or a statement-level one.</summary>
    /// <returns>The number of local variables defined by this clause.</returns>
    private void ParseLetClause()
    {
        do
        {
            Move();
            if (kind != Token.Id && kind != Token.Functor)
                throw Error("Identifier expected");
            string localId = id;
            Move();
            ParameterExpression le;
            Expression init;
            if (kind == Token.LPar)
            {
                // A local function definition.
                List<ParameterExpression> parameters = ParseParameters();
                if (kind == Token.Colon)
                {
                    Type retType = ParseTypeRef();
                    le = Expression.Variable(BindResultType(parameters, retType), localId);
                    localLambdas[localId] = le;
                    CheckAndMove(Token.Eq, "= expected");
                    init = ParseFormula("", false, false, true);
                    init = lambdaBlock.Create(this, init, retType);
                }
                else
                {
                    CheckAndMove(Token.Eq, "= expected");
                    init = ParseFormula("", false, false, true);
                    init = lambdaBlock.Create(this, init, init.Type);
                    le = Expression.Variable(init.Type, localId);
                    localLambdas[localId] = le;
                }
            }
            else
            {
                // A local variable definition.
                CheckAndMove(Token.Eq, "= expected");
                init = ParseConditional();
                le = Expression.Variable(init.Type, localId);
                locals[localId] = le;
            }
            letLocals.Add(le);
            letExpressions.Add(Expression.Assign(le, init));
        }
        while (kind == Token.Comma);
    }

    private List<ParameterExpression> ParseParameters()
    {
        parsingLambdaHeader = true;
        Move();
        List<ParameterExpression> result = new(4);
        List<string> names = new(4);
        while (true)
        {
            if (kind != Token.Id)
                throw Error("Parameter name expected");
            names.Add(id);
            Move();
            while (kind == Token.Comma)
            {
                Move();
                if (kind != Token.Id)
                    throw Error("Parameter name expected");
                names.Add(id);
                Move();
            }
            Type type = ParseTypeRef();
            foreach (string param in names)
                result.Add(Expression.Parameter(type, param));
            if (kind != Token.Comma)
                break;
            Move();
        }
        lambdaBlock.Add(result);
        CheckAndMove(Token.RPar, ") expected");
        parsingLambdaHeader = false;
        return result;
    }

    private Type ParseTypeRef()
    {
        CheckAndMove(Token.Colon, ": expected");
        if (kind != Token.Id)
            throw Error("Type name expected");
        if (!bindings.TryGetTypeName(id, out Type? type))
            throw Error($"Invalid type name: {id}");
        Move();
        return type;
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
        if (kind == Token.Else)
        {
            // Quick path.
            Move();
            Expression e3 = ParseConditional();
            return DifferentTypes(ref e1, ref e3)
                ? throw Error("Conditional operands are not compatible")
                : Expression.Condition(c, e1, e3);
        }
        List<Expression> expressions = source.Rent(8);
        try
        {
            expressions.Add(c);
            expressions.Add(e1);
            while (kind == Token.Elif)
            {
                Move();
                c = ParseDisjunctionConjunction();
                if (c.Type != typeof(bool))
                    throw Error("Condition must be boolean");
                expressions.Add(c);
                CheckAndMove(Token.Then, "THEN expected");
                expressions.Add(ParseConditional());
            }
            CheckAndMove(Token.Else, "ELSE expected");
            Expression last = ParseConditional();
            for (int i = 1; i < expressions.Count; i += 2)
            {
                Expression e = expressions[i];
                if (e.Type != last.Type)
                    if (last.Type == typeof(Complex) && IsArithmetic(e))
                        expressions[i] = Expression.Convert(e, typeof(Complex));
                    else if (e.Type == typeof(Complex) && IsArithmetic(last))
                        expressions[^1] = last = Expression.Convert(last, typeof(Complex));
                    else if (IsArithmetic(last) && IsArithmetic(e))
                    {
                        expressions[i] = ToDouble(expressions[i]);
                        expressions[^1] = last = ToDouble(last);
                    }
                    else
                        throw Error("Conditional operands are not compatible");
            }
            last = Expression.Condition(expressions[^2], expressions[^1], last);
            for (int i = expressions.Count - 3; i > 0; i -= 2)
                last = Expression.Condition(expressions[i - 1], expressions[i], last);
            return last;
        }
        finally
        {
            source.Return(expressions);
        }
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

            case Token.Element:
                {
                    Move();
                    Expression e2 = ParseAdditiveMultiplicative();
                    if (IsIntVecOrSeq(e2))
                        return e1.Type != typeof(int)
                            ? throw Error("Left side of IN must be an integer", pos)
                            : Expression.Call(e2, e2.Type.Get(nameof(NSequence.Contains)), e1);
                    if (e1.Type == typeof(Date) && e2.Type == typeof(Series))
                        return Expression.Call(e2,
                            e2.Type.GetMethod(nameof(Series.Contains), [typeof(Date)])!, e1);
                    if (e2.Type == typeof(DVector) || e2.Type == typeof(Series) ||
                        e2.Type.IsAssignableTo(typeof(IMatrix)) ||
                        e2.Type.IsAssignableTo(typeof(DSequence)))
                    {
                        e1 = ToDouble(e1);
                        return e1.Type != typeof(double)
                            ? throw Error("Left side of IN must be numeric", pos)
                            : Expression.Call(e2,
                                e2.Type.GetMethod(nameof(NSequence.Contains), [typeof(double)])!, e1);
                    }
                    if (e2.Type == typeof(CVector) || e2.Type.IsAssignableTo(typeof(CSequence)))
                    {
                        if (e1.Type != typeof(Complex))
                            if (IsArithmetic(e1))
                                e1 = Expression.Convert(e1, typeof(Complex));
                        return e1.Type != typeof(Complex)
                            ? throw Error("Left side of IN must be numeric", pos)
                            : Expression.Call(e2, e2.Type.Get(nameof(CSequence.Contains)), e1);
                    }
                    throw Error("Invalid ∈ operation");
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
                if (opMul == Token.PointTimes || opMul == Token.PointDiv)
                    e2 = e2.Type == e3.Type && e2.Type.IsAssignableTo(
                            typeof(IPointwiseOperators<>).MakeGenericType(e2.Type))
                        ? e2.Type.Call(e2, opMul == Token.PointTimes
                            ? nameof(DVector.PointwiseMultiply) : nameof(DVector.PointwiseDivide),
                            e3)
                        : throw Error("Invalid operator", opMPos);
                else
                {
                    if (e2.Type != e3.Type && !IsIntVecOrSeq(e2) && !IsIntVecOrSeq(e3))
                        (e2, e3) = (ToDouble(e2), ToDouble(e3));
                    try
                    {
                        // Try to optimize matrix transpose multiplying a vector.
                        e2 = opMul == Token.Times && e2.Type == typeof(Matrix)
                            ? (e3.Type == typeof(DVector) && e2 is MethodCallExpression
                            { Method.Name: nameof(Matrix.Transpose) } mca
                                ? Expression.Call(mca.Object, MatrixTransposeMultiply, e3)
                                : e3.Type == typeof(Matrix) && e3 is MethodCallExpression
                                { Method.Name: nameof(Matrix.Transpose) } mcb
                                ? Expression.Call(e2, MatrixMultiplyTranspose, mcb.Object!)
                                : e2 == e3
                                ? Expression.Call(e2, typeof(Matrix).Get(nameof(Matrix.Square)))
                                : Expression.Multiply(e2, e3))
                            : e2 is ConstantExpression { Value: double d1 } &&
                                e3 is ConstantExpression { Value: double d2 }
                            ? Expression.Constant(opMul switch
                            {
                                Token.Times => d1 * d2,
                                Token.Div => d1 / d2,
                                _ => d1 % d2
                            })
                            : opMul == Token.Times
                            ? (e2 == e3 && e2.Type == typeof(DVector)
                                ? Expression.Call(e2, typeof(DVector).Get(nameof(DVector.Squared)))
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
                    !IsIntVecOrSeq(e1) && !IsIntVecOrSeq(e2) &&
                    e1.Type != typeof(Date) && e2.Type != typeof(Date))
                    (e1, e2) = (ToDouble(e1), ToDouble(e2));
                try
                {
                    if (e1.Type == typeof(DVector) && e2.Type == typeof(DVector))
                    {
                        string method = opAdd == Token.Plus
                            ? nameof(DVector.MultiplyAdd)
                            : nameof(DVector.MultiplySubtract);
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
                                        typeof(DVector).GetMethod(method, DoubleVectorArg)!,
                                        be1.Right, e2)
                                    : be1.Left.Type == typeof(double)
                                    ? Expression.Call(be1.Right,
                                        typeof(DVector).GetMethod(method, DoubleVectorArg)!,
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
                                    typeof(DVector).GetMethod(method, DoubleVectorArg)!,
                                    be2.Right, e1)
                                : be2.Left.Type == typeof(double)
                                ? Expression.Call(be2.Right,
                                    typeof(DVector).GetMethod(method, DoubleVectorArg)!,
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
                        e1 = e1 is ConstantExpression { Value: double d1 } &&
                                e2 is ConstantExpression { Value: double d2 }
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
        return kind == Token.Caret || kind == Token.Caret2 ? ParsePower(e) : e;
    }

    private Expression ParsePower(Expression e)
    {
        int pos = start;
        Token k = kind;
        Move();
        Expression e1 = k == Token.Caret ? ParseFactor() : Expression.Constant(2);
        if (IsArithmetic(e) && IsArithmetic(e1))
            return OptimizePowerOf() ? e : Expression.Power(ToDouble(e), ToDouble(e1));
        if (e.Type == typeof(Complex))
        {
            if (e1.Type == typeof(Complex))
                return Expression.Call(typeof(Complex), nameof(Complex.Pow), null, e, e1);
            else if (IsArithmetic(e1))
                return OptimizePowerOf() ? e : Expression.Call(
                    typeof(Complex), nameof(Complex.Pow), null, e, ToDouble(e1));
        }
        else if (k == Token.Caret2 || e1 is ConstantExpression { Value: 2 })
            if (e.Type == typeof(Matrix))
                return Expression.Call(e, MatrixSquare);
            else if (e.Type == typeof(LMatrix))
                return Expression.Call(e, LMatrixSquare);
            else if (e.Type == typeof(RMatrix))
                return Expression.Call(e, RMatrixSquare);
        return e.Type == typeof(DVector) && e1.Type == typeof(DVector)
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
                    e = Expression.Constant(value);
                    break;
                }
            case Token.Imag:
                {
                    double value = asReal;
                    Move();
                    e = Expression.Constant(new Complex(0, value));
                    break;
                }
            case Token.Str:
                {
                    string text = id;
                    Move();
                    return Expression.Constant(text);
                }
            case Token.Date:
                {
                    Date value = new((uint)asInt);
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
                    if (kind == Token.Caret || kind == Token.Caret2)
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
                    if (kind == Token.Caret || kind == Token.Caret2)
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
                    e = e.Type == typeof(CVector)
                        ? Expression.Call(e, e.Type.Get(nameof(CVector.Conjugate)))
                        : IsMatrix(e)
                        ? Expression.Call(e, e.Type.Get(nameof(Matrix.Transpose)))
                        : e.Type == typeof(Complex)
                        ? e.Type.Call(null, nameof(Complex.Conjugate), e)
                        : throw Error("Can only transpose a matrix or conjugate a complex vector");
                    Move();
                    break;
                case Token.LBra:
                    Move();
                    e = e.Type == typeof(Series<int>) || e.Type.IsAssignableTo(typeof(IIndexable))
                        ? ParseIndexer(e, true)
                        : IsMatrix(e)
                        ? ParseMatrixIndexer(e)
                        : e.Type == typeof(Series)
                        ? ParseSeriesIndexer(e)
                        : e.Type == typeof(MvoModel)
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
            : e.Type.Call(e, nameof(DVector.SafeThis), e1);
    }

    private Expression ParseSplineIndexer(Expression e, Type expected)
    {
        Expression e1 = ParseLightConditional();
        CheckAndMove(Token.RBra, "] expected in indexer");
        return e1.Type != expected && (expected != typeof(double) || e1.Type != typeof(int))
            && (expected != typeof(Date) || !IsArithmetic(e1))
            ? throw Error("Invalid index type")
            : Expression.Property(e, "Item", ToDouble(e1));
    }

    private Expression ParseIndexer(Expression e, bool allowSlice)
    {
        bool fromEnd1 = false;
        Expression e1 = kind == Token.Range && allowSlice
            ? ZeroExpr
            : ParseIndex(ref fromEnd1);
        if (allowSlice && kind == Token.Range)
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
            e1 = kind == Token.Range
                ? Expression.Constant(Index.Start)
                : ParseIndex(ref fromEnd11);
            if (kind == Token.Range)
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
            e2 = kind == Token.Range
                ? Expression.Constant(Index.Start)
                : ParseIndex(ref fromEnd21);
            if (kind == Token.Range)
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
        if (kind != Token.Range)
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
            if (kind != Token.Range)
                throw Error(".. expected in slice");
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
            : ZeroExpr;
        e2 ??= e1.Type == typeof(Date)
            ? Expression.Constant(new Date(3000, 1, 1))
            : Expression.Constant(int.MaxValue);
        return Expression.Call(e,
            typeof(Series).GetMethod(nameof(Series.Slice), [e1.Type, e2.Type])!,
            e1, e2);
    }

    private Expression ParseMethod(Expression e)
    {
        (string method, int pos) = (id, start);
        // Skip method name and left parenthesis.
        SkipFunctor();
        if (bindings.TryGetMethod(e.Type, method, out MethodInfo? mInfo))
        {
            ParameterInfo[] paramInfo = mInfo.GetParameters();
            List<Expression> args = source.Rent(paramInfo.Length);
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
            source.Return(args);
            return result;
        }
        if (bindings.TryGetOverloads(e.Type, method, out MethodList info))
            return ParseClassMultiMethod(info, e);
        throw Error($"Invalid method: {method}", pos);
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
                // Three possibilities: x => f(x), f, class::f.
                string saveId = id;
                Expression? lambda;
                if (kind == Token.ClassName)
                {
                    SkipFunctor();
                    if (kind != Token.Id ||
                        !GetLambdaFromFunctionName(saveId + "." + id, out lambda))
                        throw Error("Function name expected");
                    Move();
                    return lambda;
                }
                if (kind != Token.Id)
                    throw Error("Lambda parameter name expected");
                Move();
                if (kind != Token.Arrow
                    && GetLambdaFromFunctionName("math." + saveId, out lambda))
                    return lambda;
                lambdaBlock.Add(Expression.Parameter(t1, saveId));
            }
            else
            {
                CheckAndMove(Token.LPar, "Lambda parameters expected");
                if (kind != Token.Id)
                    throw Error("First lambda parameter name expected");
                ParameterExpression p1 = Expression.Parameter(t1, id);
                Move();
                CheckAndMove(Token.Comma, "Comma expected");
                if (kind != Token.Id)
                    throw Error("Second lambda parameter name expected");
                ParameterExpression p2 = Expression.Parameter(t2, id);
                lambdaBlock.Add(p1, p2);
                Move();
                CheckAndMove(Token.RPar, ") expected in lambda header");
            }
            CheckAndMove(Token.Arrow, "=> expected");
            parsingLambdaHeader = false;
            return lambdaBlock.Create(this, ParseConditional(), retType);
        }
        finally
        {
            if (abortPosition == int.MaxValue)
                parsingLambdaHeader = false;
        }

        bool GetLambdaFromFunctionName(string qualifiedName,
            [NotNullWhen(true)] out Expression? lambda)
        {
            if (!bindings.TryGetClassMethod(qualifiedName, out MethodList info))
            {
                lambda = null;
                return false;
            }
            // Check signature.
            MethodData? candidate = null;
            foreach (MethodData m in info.Methods)
                if (m.IsMatch(t1, retType))
                    if (candidate != null)
                        throw Error("Ambiguous function name");
                    else
                        candidate = m;
            if (candidate == null)
                throw Error("Invalid function name while expecting lambda.");
            lambda = candidate.Value.GetAsLambda(t1);
            return true;
        }
    }

    private Expression ParseProperty(Expression e)
    {
        if (!bindings.TryGetProperty(e.Type, id, out MethodInfo? mInfo))
        {
            if (e.Type == typeof(double))
            {
                if (id.Equals("toint", StringComparison.OrdinalIgnoreCase))
                {
                    Move();
                    return Expression.Convert(e, typeof(int));
                }
            }
            else if (e.Type == typeof(int))
            {
                if (id.Equals("even", StringComparison.OrdinalIgnoreCase))
                {
                    Move();
                    return Expression.Equal(Expression.Modulo(e, Expression.Constant(2)), ZeroExpr);
                }
                else if (id.Equals("odd", StringComparison.OrdinalIgnoreCase))
                {
                    Move();
                    return Expression.NotEqual(Expression.Modulo(e, Expression.Constant(2)), ZeroExpr);
                }
            }
            throw Error($"Invalid property: {id}");
        }
        Move();
        return Expression.Call(e, mInfo);
    }

    /// <summary>Parses a global function call.</summary>
    /// <returns>An expression representing the function call.</returns>
    private Expression ParseFunction()
    {
        (string function, int pos) = (id.ToLower(), start);
        SkipFunctor();
        // Check for a local lambda in a LET clause.
        if (TryGetLambda(function, out ParameterExpression? lambda))
            return ParseArgumentsAndBind(lambda);
        // Check macro definitions.
        Definition? def = source.GetDefinition(function);
        if (def != null && def.Type.IsAssignableTo(typeof(Delegate)))
        {
            if (isParsingDefinition)
                references.Add(def);
            return ParseArgumentsAndBind(def.Expression);
        }
        if (bindings.TryGetClassMethod("math." + function, out MethodList info))
            return info.Methods.Length == 1
                ? ParseClassSingleMethod(info.Methods[0])
                : ParseClassMultiMethod(info);
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

        Expression ParseArgumentsAndBind(Expression expression)
        {
            Type[] types = expression.Type.GenericTypeArguments;
            List<Expression> args = source.Rent(types.Length);
            try
            {
                for (int i = 0; i < types.Length - 1; i++)
                {
                    args.Add(ParseByType(types[i]));
                    if (i < types.Length - 2)
                        CheckAndMove(Token.Comma, "Comma expected");
                }
                CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
                Expression result = Expression.Invoke(expression, args);
                return result;
            }
            finally
            {
                source.Return(args);
            }

        }
    }

    private bool TryGetLambda(string functionName, [NotNullWhen(true)] out ParameterExpression? lambda)
    {
        if (localLambdas.TryGetValue(functionName, out lambda))
            return true;
        if (currentDefinitionLambda is not null &&
            functionName.Equals(currentDefinitionLambda.Name, StringComparison.OrdinalIgnoreCase))
        {
            lambda = currentDefinitionLambda;
            isDefRecursive = true;
            return true;
        }
        lambda = null;
        return false;
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
        List<Expression> args = source.Rent(types.Length);
        try
        {
            for (int i = 0; i < types.Length; i++, Move())
            {
                args.Add(ParseByType(types[i]));
                if (kind != Token.Comma)
                {
                    if (++i == types.Length - 1 && types[i] is Type t)
                        if (t == typeof(Random))
                            args.Add(RandomExpr);
                        else if (t == typeof(NormalRandom))
                            args.Add(t.New());
                        else if (t == typeof(One))
                            args.Add(Expression.Constant(1d));
                        else
                            throw Error($"Invalid number of arguments");
                    break;
                }
            }
            Expression result = method.GetExpression(args);
            CheckAndMove(Token.RPar, "Right parenthesis expected after function call");
            return result;
        }
        finally
        {
            source.Return(args);
        }
    }

    private Expression ParseByType(Type expected)
    {
        if (expected.IsAssignableTo(typeof(Delegate)))
            return ParseLambda(expected);
        if (expected == typeof(Index))
            return ParseIndex();
        if (expected.IsArray && expected.GetElementType() is Type subType)
            for (List<Expression> items = source.Rent(16); ; Move())
            {
                Expression it = ParseLightConditional();
                if (it.Type != subType)
                    throw Error($"Expected {subType.Name}");
                items.Add(it);
                if (kind != Token.Comma)
                {
                    Expression result = subType.Make(items);
                    source.Return(items);
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

    private Expression ParseClassMultiMethod(in MethodList info, Expression? instance = null)
    {
        List<Expression> args = source.Rent(16);
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
                throw Error("No method accepts this argument list.");
            if (PopCount((uint)mask) != 1)
                throw Error("Ambiguous method call.");
        }
        // Get selected method overload and check conversions.
        MethodData mth = info.Methods[Log2((uint)mask)];
        if (mth.ExpectedArgs < mth.Args.Length && mth.Args[^1] is Type t)
            args.Add(t == typeof(Random)
                ? RandomExpr
                : t == typeof(NormalRandom)
                ? t.New()
                : Expression.Constant(t == typeof(One) ? 1d : 0d));
        if (mth.ExpectedArgs != int.MaxValue && args.Count < mth.ExpectedArgs)
            throw Error("No method accepts this argument list.");
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
        CheckAndMove(Token.RPar, "Right parenthesis expected in method call");
        Expression result = instance is null
            ? mth.GetExpression(args)
            : mth.GetExpression(instance, args);
        source.Return(args);
        return result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CanConvert(Type actual, Type expected) =>
            expected == actual ||
            expected == typeof(double) && actual == typeof(int) ||
            expected == typeof(Complex) && (actual == typeof(double) || actual == typeof(int)) ||
            expected.IsArray && expected.GetElementType() is var et
                && (actual == et || et == typeof(double) && actual == typeof(int));
    }

    /// <summary>Parses a vector or matrix literal, or a list comprehension.</summary>
    /// <returns>An expression creating the vector or the matrix.</returns>
    private Expression ParseVectorLiteral()
    {
        Move();
        if (kind == Token.RBra)
        {
            Move();
            return typeof(DVector).New(ZeroExpr);
        }
        // Verify if it's a list comprehension.
        if (kind == Token.Id)
        {
            string saveId = id;
            int saveCursor = lexCursor;
            Move();
            if (kind == Token.Element)
            {
                id = saveId;
                lexCursor = saveCursor;
                return ParseListComprehension("");
            }
            else if (kind == Token.Id)
            {
                string qual = saveId.ToLower();
                Move();
                if ((qual == "all" || qual == "any") && kind == Token.Element)
                {
                    id = saveId;
                    lexCursor = saveCursor;
                    return ParseListComprehension(qual);
                }
            }
            id = saveId;
            lexCursor = saveCursor;
            kind = Token.Id;
        }
        // It's a vector or a matrix constructor.
        List<Expression> items = source.Rent(16);
        int period = 0, lastPeriod = 0, vectors = 0, matrices = 0;
        for (; ; )
        {
            Expression e = ParseLightConditional();
            if (IsArithmetic(e))
                if (items.Count == 0 && kind == Token.Range)
                {
                    Move();
                    e = ParseGenerator(e);
                    CheckAndMove(Token.RBra, "] expected in sequence generator");
                    return e;
                }
                else
                    items.Add(ToDouble(e));
            else if (e.Type == typeof(DVector))
            {
                if (period != 0 && matrices == 0)
                    throw Error("Invalid vector in matrix constructor");
                vectors++;
                items.Add(e);
            }
            else if (e.Type == typeof(Matrix))
            {
                if (period > 1 || vectors + matrices != items.Count || items.Count >= 2 && vectors > 0)
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
            else
                throw Error("Vector item separator expected");
        }
        Expression result;
        if (matrices > 0)
            result = period > 1 || vectors + matrices != items.Count || items.Count > 2 && vectors > 0
                ? throw Error("Invalid matrix concatenation")
                : items.Count == 1
                ? items[0]
                : items.Count > 2
                ? typeof(Matrix).Call(period == 0 ? nameof(Matrix.HCat) : nameof(Matrix.VCat),
                    typeof(Matrix).Make(items))
                : typeof(Matrix).Call(period == 0 ? nameof(Matrix.HCat) : nameof(Matrix.VCat),
                    items[0], items[1]);
        else if (vectors > 0)
        {
            for (int i = 0; vectors < items.Count; vectors++)
            {
                for (; items[i].Type == typeof(DVector); i++) ;
                int j = i + 1;
                for (; j < items.Count && items[j].Type != typeof(DVector); j++) ;
                int count = j - i;
                if (count == 1 && items.Count == 2)
                    return typeof(DVector).New(items[0], items[1]);
                items[i] = typeof(DVector).New(typeof(double).Make(items.GetRange(i, count)));
                items.RemoveRange(i + 1, count - 1);
            }
            result = typeof(DVector).New(typeof(DVector).Make(items));
        }
        else
        {
            if (period != 0 && items.Count - lastPeriod != period)
                throw Error("Inconsistent matrix size");
            Expression args = typeof(double).Make(items);
            result = period != 0
                ? typeof(Matrix).New(
                    Expression.Constant(items.Count / period), Expression.Constant(period), args)
                : typeof(DVector).New(args);
        }
        source.Return(items);
        return result;
    }

    /// <summary>Parses a list comprehension expression.</summary>
    /// <param name="qualifier">Optional qualifier: could be <c>any</c> or <c>all</c>.</param>
    /// <returns>A sequence or vector type expression.</returns>
    private Expression ParseListComprehension(string qualifier)
    {
        if (qualifier != "")
            Move();
        // Remember the lambda parameter name and skip it and the IN keyword.
        string paramName = id;
        Move(); Move();
        Expression? filter = null, mapper = null;
        // Parse the expression that generates the sequence.
        Expression e = ParseGenerator();
        // Verify that the expression is a sequence and get its type.
        (Type eType, Type iType) = GetTypes(e);
        // Check a colon for a filter expression.
        if (kind == Token.Colon)
        {
            bool backtrack = false;
            Move();
            lambdaBlock.Add(Expression.Parameter(
                eType == typeof(Series) ? typeof(Point<Date>) : iType, paramName));
            string qual = id.ToLower();
            if (kind == Token.Id && (qual == "all" || qual == "any"))
            {
                int savePos = lexCursor;
                Move();
                if (kind == Token.Id)
                {
                    string paramName1 = id;
                    Move();
                    if (kind == Token.In)
                    {
                        Move();
                        Expression f = ParseGenerator();
                        // Verify that the expression is a sequence and get its type.
                        (Type fType, Type ifType) = GetTypes(e);
                        CheckAndMove(Token.Colon, ": expected in qualified filter in list comprehension");
                        lambdaBlock.Add(Expression.Parameter(
                            fType == typeof(Series) ? typeof(Point<Date>) : ifType, paramName1));
                        filter = lambdaBlock.Create(this, ParseConditional(), typeof(bool));
                        filter = lambdaBlock.Create(this, Expression.Call(
                            f, qual == "all" ? "All" : "Any", Type.EmptyTypes, filter), typeof(bool));
                    }
                    else
                        backtrack = true;
                }
                if (backtrack)
                {
                    lexCursor = savePos;
                    id = qual;
                    kind = Token.Id;
                }
            }
            // Parse the expression that filters the sequence.
            filter ??= lambdaBlock.Create(this, ParseConditional(), typeof(bool));
        }
        // Qualifiers do not allow a mapping expression.
        if (qualifier != "")
        {
            // Check and skip a right bracket.
            CheckAndMove(Token.RBra, "] expected in list comprehension");
            if (filter != null)
                e = Expression.Call(e, qualifier == "all" ? "All" : "Any", Type.EmptyTypes, filter);
            return e;
        }
        // Check an arrow for a mapping expression.
        bool upgraded = false;
        if (kind == Token.Arrow)
        {
            Move();
            // Parse the expression that generates the result.
            lambdaBlock.Add(Expression.Parameter(iType, paramName));
            (mapper, upgraded) = lambdaBlock.Create(
                this, ParseLightConditional(), iType, iType == typeof(int));
        }
        // Check and skip a right bracket.
        CheckAndMove(Token.RBra, "] expected in list comprehension");
        if (filter != null)
            if (e.Type == typeof(DVector) && mapper != null)
                return Expression.Call(e, "FilterMap", Type.EmptyTypes, filter, mapper);
            else
                e = Expression.Call(e, "Filter", Type.EmptyTypes, filter);
        if (mapper != null)
            e = Expression.Call(e, upgraded ? "MapReal" : "Map", Type.EmptyTypes, mapper);
        return e;

        (Type eType, Type iType) GetTypes(Expression e) =>
            e.Type == typeof(DVector) || e.Type == typeof(DSequence) || e.Type == typeof(Series)
            ? (e.Type, typeof(double))
            : e.Type == typeof(NVector) || e.Type == typeof(NSequence)
            ? (e.Type, typeof(int))
            : e.Type == typeof(CVector) || e.Type == typeof(CSequence)
            ? (e.Type, typeof(Complex))
            : throw Error("Invalid sequence type");
    }

    private Expression ParseGenerator()
    {
        Expression first = ParseLightConditional();
        if (first.Type != typeof(int) && first.Type != typeof(double))
            return first;
        // It may be a range expression.
        CheckAndMove(Token.Range, "Expected range in list comprehension");
        return ParseGenerator(first);
    }

    private Expression ParseGenerator(Expression first)
    {
        Expression? middle = ParseLightConditional();
        Expression? last = null;
        if (kind == Token.Range)
        {
            Move();
            last = ParseLightConditional();
        }
        else
            (middle, last) = (null, middle);
        if (last!.Type != first.Type)
            if (last.Type == typeof(int))
                last = IntToDouble(last);
            else
                first = first.Type == typeof(int)
                    ? IntToDouble(first)
                    : throw Error("Range bounds must be of the same type");
        if (middle is not null && middle.Type != typeof(int))
            throw Error("Range step must be an integer");
        return first.Type == typeof(int)
            ? middle != null
                ? Expression.Call(typeof(NSequence), nameof(NSequence.Create),
                    Type.EmptyTypes, first, middle, last)
                : Expression.Call(typeof(NSequence), nameof(NSequence.Create),
                    Type.EmptyTypes, first, last)
            : middle != null
                ? Expression.Call(typeof(DSequence), nameof(DSequence.Create),
                    Type.EmptyTypes, first, middle, last)
                : Expression.Call(typeof(DSequence), nameof(DSequence.Create),
                    Type.EmptyTypes, first, last);
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

    /// <summary>Translate an identifier into a variable reference.</summary>
    /// <returns>An expression for the value of the variable.</returns>
    private Expression ParseVariable()
    {
        (int pos, string ident) = (start, id);
        Move();
        // Check lambda parameters when present.
        if (lambdaBlock.TryMatch(ident, out ParameterExpression? param))
            return param;
        // Check the local scope.
        if (locals.TryGetValue(ident, out ParameterExpression? local))
            return local.Type == typeof(DSequence)
                ? Expression.Call(local, DSeqClone)
                : local.Type == typeof(CSequence)
                ? Expression.Call(local, CSeqClone)
                : local.Type == typeof(NSequence)
                ? Expression.Call(local, NSeqClone)
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
                letLocals.Add(local);
                letExpressions.Add(Expression.Assign(local, def.Expression));
            }
            return local;
        }
        // Check the global scope.
        Expression? e = ParsePendingVariables(ident)
            ?? source.GetExpression(ident, isParsingDefinition)
            ?? ParseGlobals(ident);
        if (e != null)
        {
            return e.Type.IsAssignableTo(typeof(DSequence))
                ? Expression.Call(e, DSeqClone)
                : e.Type.IsAssignableTo(typeof(CSequence))
                ? Expression.Call(e, CSeqClone)
                : e.Type.IsAssignableTo(typeof(NSequence))
                ? Expression.Call(e, NSeqClone)
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
            "tau" => Expression.Constant(Math.Tau),
            "today" => Expression.Constant(Date.Today),
            "pearl" => Expression.Call(typeof(Functions).Get(nameof(Functions.Austra))),
            "random" => Expression.Call(typeof(Functions).GetMethod(nameof(Functions.Random))!),
            "nrandom" => Expression.Call(typeof(Functions).GetMethod(nameof(Functions.NRandom))!),
            _ => null,
        };

    /// <summary>Checks if the expression's type is either a double or an integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsArithmetic(Expression e) =>
        e.Type == typeof(int) || e.Type == typeof(double);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMatrix(Expression e) =>
        e.Type.IsAssignableTo(typeof(IMatrix));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsVector(Expression e) =>
        e.Type.IsAssignableTo(typeof(IVector));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsIntVecOrSeq(Expression e) =>
        e.Type == typeof(NVector) || e.Type == typeof(NSequence);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Expression ToDouble(Expression e) =>
        e.Type != typeof(int)
        ? e
        : e is ConstantExpression constExpr
        ? Expression.Constant((double)(int)constExpr.Value!)
        : Expression.Convert(e, typeof(double));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Expression IntToDouble(Expression e) =>
        e is ConstantExpression constExpr
        ? Expression.Constant((double)(int)constExpr.Value!)
        : Expression.Convert(e, typeof(double));

    private static bool DifferentTypes(ref Expression e1, ref Expression e2)
    {
        if (e1.Type != e2.Type)
        {
            if (e1.Type == typeof(Complex) && IsArithmetic(e2))
                e2 = Expression.Convert(e2, typeof(Complex));
            else if (e2.Type == typeof(Complex) && IsArithmetic(e1))
                e1 = Expression.Convert(e1, typeof(Complex));
            else
            {
                if (!IsArithmetic(e1) || !IsArithmetic(e2))
                    return true;
                (e1, e2) = (ToDouble(e1), ToDouble(e2));
            }
        }
        return false;
    }

    /// <summary>Gets a regex that matches a lambda header with one parameter.</summary>
    [GeneratedRegex(@"^\w+\s*\=\>")]
    private static partial Regex LambdaHeader1();

    /// <summary>Gets a regex that matches a lambda header with two parameters.</summary>
    [GeneratedRegex(@"^\(\s*\w+\s*\,\s*\w+\s*\)\s*\=\>")]
    private static partial Regex LambdaHeader2();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsLambda() => kind switch
    {
        Token.ClassName => IsQualifiedLambdaFunctor(),
        Token.Id => LambdaHeader1().IsMatch(text.AsSpan()[start..])
        || bindings.ContainsClassMethod("math." + id),
        _ => LambdaHeader2().IsMatch(text.AsSpan()[start..]),
    };

    private bool IsQualifiedLambdaFunctor()
    {
        int saveCursor = lexCursor;
        string saveClassName = id;
        try
        {
            SkipFunctor();
            return kind == Token.Id && bindings.ContainsClassMethod(saveClassName + "." + id);
        }
        finally
        {
            // Backtrack to the original position.
            lexCursor = saveCursor;
            id = saveClassName;
            kind = Token.ClassName;
        }
    }

    public Type BindResultType(List<ParameterExpression> parameters, Type retType) =>
        parameters.Count switch
        {
            0 => typeof(Func<>).MakeGenericType(retType),
            1 => typeof(Func<,>).MakeGenericType(parameters[0].Type, retType),
            2 => typeof(Func<,,>).MakeGenericType(parameters.Select(p => p.Type)
                .Concat([retType]).ToArray()),
            3 => typeof(Func<,,,>).MakeGenericType(parameters.Select(p => p.Type)
                .Concat([retType]).ToArray()),
            4 => typeof(Func<,,,,>).MakeGenericType(parameters.Select(p => p.Type)
                .Concat([retType]).ToArray()),
            5 => typeof(Func<,,,,,>).MakeGenericType(parameters.Select(p => p.Type)
                .Concat([retType]).ToArray()),
            _ => throw Error("Unsupported number of arguments")
        };
}
