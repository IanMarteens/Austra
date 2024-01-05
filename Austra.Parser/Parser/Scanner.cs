using System.Runtime.Intrinsics.X86;
using static System.Runtime.CompilerServices.Unsafe;

namespace Austra.Parser;

/// <summary>Syntactic and lexical analysis for AUSTRA.</summary>
internal sealed partial class Parser : IDisposable
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
    /// <summary>The text being scanned.</summary>
    private readonly string text;
    /// <summary>Referenced definitions.</summary>
    private readonly HashSet<Definition> references = [];

    /// <summary>All top-level locals, from LET/IN clauses.</summary>
    private readonly List<ParameterExpression> letLocals = new(8);
    /// <summary>All top-level locals, from LET; clauses (script-scoped).</summary>
    private readonly List<ParameterExpression> scriptLetLocals = new(8);
    /// <summary>Top-level local asignment expressions.</summary>
    private readonly List<Expression> letExpressions;
    /// <summary>Top-level local asignment expressions (script-scoped).</summary>
    private readonly List<Expression> scriptExpressions;
    /// <summary>Transient local variable definitions.</summary>
    /// <remarks>
    /// This data structure is redundant with respect to <see cref="letLocals"/>, but
    /// it's faster to search, while <see cref="letLocals"/> is needed for code generation.
    /// </remarks>
    private readonly Dictionary<string, ParameterExpression> locals =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>User-defined lambdas, indexed by name, statement level.</summary>
    private readonly Dictionary<string, ParameterExpression> localLambdas =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Transient local variable definitions (script-scoped).</summary>
    /// <remarks>
    /// This data structure is redundant with respect to <see cref="scriptLetLocals"/>.
    /// </remarks>
    private readonly Dictionary<string, ParameterExpression> scriptLocals =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>User-defined lambdas, indexed by name, script level.</summary>
    private readonly Dictionary<string, ParameterExpression> scriptLambdas =
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

    /// <summary>Used by the scanner to build string literals.</summary>
    private StringBuilder? sb;
    /// <summary>Gets the type of the current lexeme.</summary>
    /// <remarks>Updated by the <see cref="Move"/> method.</remarks>
    private Token kind;
    /// <summary>Gets the start position of the current lexeme.</summary>
    /// <remarks>
    /// <para>Updated by the <see cref="Move"/> method.</para>
    /// <para><see cref="start"/> is always lesser than <see cref="lexCursor"/>.</para>
    /// </remarks>
    private int start;
    /// <summary>Gets the string associated with the current lexeme.</summary>
    /// <remarks>Updated by the <see cref="Move"/> method.</remarks>
    private string id = "";
    /// <summary>Value of the current lexeme as a real number.</summary>
    private double asReal;
    /// <summary>Value of the lexeme as an integer.</summary>
    private int asInt;
    /// <summary>Value of the lexeme as a date literal.</summary>
    private Date asDate;
    /// <summary>Current position in the text.</summary>
    /// <remarks>Updated by the <see cref="Move"/> method.</remarks>
    private int lexCursor;
    /// <summary>Position where the parsing should be aborted.</summary>
    /// <remarks>This is checked by the scanner.</remarks>
    private int abortPosition = int.MaxValue;

    /// <summary>Initializes a parsing context.</summary>
    /// <param name="bindings">Predefined classes and methods.</param>
    /// <param name="source">Environment variables.</param>
    /// <param name="text">Text of the formula.</param>
    public Parser(Bindings bindings, IDataSource source, string text)
    {
        (this.bindings, this.source, this.text, lambdaBlock,
            letExpressions, setExpressions, scriptExpressions)
            = (bindings, source, text, bindings.LambdaBlock,
                source.Rent(8), source.Rent(8), source.Rent(8));
        Move();
    }

    /// <summary>Returns allocated resources to the pool, in the data source.</summary>
    public void Dispose()
    {
        lambdaBlock.Clean();
        source.Return(scriptExpressions);
        source.Return(setExpressions);
        source.Return(letExpressions);
    }

    /// <summary>Skips two tokens with a single call.</summary>
    private void SkipFunctor()
    {
        if (kind == Token.Functor) lexCursor++; else lexCursor += 2;
        Move();
    }

    /// <summary>
    /// Checks that the current token is of the expected kind and advances the cursor.
    /// </summary>
    /// <param name="kind">Expected type of token.</param>
    /// <param name="errorMessage">Error message to use in the exception.</param>
    /// <exception cref="AstException">Thrown when the token doesn't match.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckAndMove(Token kind, string errorMessage)
    {
        if (this.kind != kind)
            throw Error(errorMessage);
        Move();
    }

    /// <summary>Advances the lexical analyzer one token.</summary>
    private void Move()
    {
        if (lexCursor >= abortPosition)
            throw new AbortException("Aborted by the scanner");
        ref char c = ref As<Str>(text).FirstChar;
    SKIP_BLANKS:
        while (char.IsWhiteSpace(Add(ref c, lexCursor)))
            lexCursor++;
        start = lexCursor;
        char ch = Add(ref c, lexCursor);
        // Check keywords, functors, class names and identifiers.
        if (char.IsLetter(ch))
        {
            do ch = Add(ref c, ++lexCursor);
            while (char.IsLetterOrDigit(ch) || ch == '_');
            // Check for keywords and function identifiers
            Token tok = Avx.IsSupported
                ? IsIntelKeyword(ref Add(ref c, start), lexCursor - start)
                : IsKeyword(text.AsSpan()[start..lexCursor]);
            if (tok != Token.Id)
                kind = tok;
            else
            {
                // Skip blanks after the identifier.
                id = text[start..lexCursor];
                if (ch == '!' && Add(ref c, lexCursor + 1) != '=')
                {
                    kind = Token.IdBang;
                    lexCursor++;
                }
                else
                {
                    while (char.IsWhiteSpace(Add(ref c, lexCursor)))
                        lexCursor++;
                    kind = Add(ref c, lexCursor) switch
                    {
                        '(' => Token.Functor,
                        ':' => Add(ref c, lexCursor + 1) == ':' ? Token.ClassName : Token.Id,
                        _ => Token.Id
                    };
                }
            }
        }
        else if ((uint)(ch - '0') < 10u)
        {
            do lexCursor++;
            while ((uint)(Add(ref c, lexCursor) - '0') < 10u);
            ch = Add(ref c, lexCursor);
            if (ch == '@')
            {
                // It's a date literal.
                do lexCursor++;
                while (char.IsLetterOrDigit(Add(ref c, lexCursor)));
                kind = Token.Date;
                asDate = ParseDateLiteral(text.AsSpan()[start..lexCursor], start);
            }
            else if (ch == '.' && Add(ref c, lexCursor + 1) != '.')
            {
                do lexCursor++;
                while ((uint)(Add(ref c, lexCursor) - '0') < 10u);
                if ((Add(ref c, lexCursor) | 0x20) == 'e')
                {
                    lexCursor++;
                    if (Add(ref c, lexCursor) is '+' or '-')
                        lexCursor++;
                    while ((uint)(Add(ref c, lexCursor) - '0') < 10u)
                        lexCursor++;
                }
                if (Add(ref c, lexCursor) == 'i' && !char.IsLetterOrDigit(Add(ref c, lexCursor + 1)))
                    (kind, asReal) = (Token.Imag, ToReal(text, start, lexCursor++));
                else if (char.IsLetter(Add(ref c, lexCursor)) && IsVariableSuffix(text, lexCursor, out int j))
                    (kind, id, asReal, lexCursor) = (Token.MultVarR, text[lexCursor..j], ToReal(text, start, lexCursor), j);
                else
                    (kind, asReal) = (Token.Real, ToReal(text, start, lexCursor));
            }
            else if ((ch | 0x20) == 'e')
            {
                if (Add(ref c, ++lexCursor) is '+' or '-')
                    lexCursor++;
                while ((uint)(Add(ref c, lexCursor) - '0') < 10u)
                    lexCursor++;
                if (Add(ref c, lexCursor) == 'i' && !char.IsLetterOrDigit(Add(ref c, lexCursor + 1)))
                    (kind, asReal) = (Token.Imag, ToReal(text, start, lexCursor++));
                else if (char.IsLetter(Add(ref c, lexCursor)) && IsVariableSuffix(text, lexCursor, out int j))
                    (kind, id, asReal, lexCursor) = (Token.MultVarR, text[lexCursor..j], ToReal(text, start, lexCursor), j);
                else
                    (kind, asReal) = (Token.Real, ToReal(text, start, lexCursor));
            }
            else if (ch == 'i' && !char.IsLetterOrDigit(Add(ref c, lexCursor + 1)))
                (kind, asReal) = (Token.Imag, ToReal(text, start, lexCursor++));
            else if (char.IsLetter(ch) && IsVariableSuffix(text, lexCursor, out int k))
                (kind, id, asInt, lexCursor)
                    = (Token.MultVarI, text[lexCursor..k], int.Parse(text.AsSpan()[start..lexCursor]), k);
            else
                (kind, asInt) = (Token.Int, int.Parse(text.AsSpan()[start..lexCursor]));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsVariableSuffix(string text, int i, out int j)
            {
                j = i;
                ref char c = ref As<Str>(text).FirstChar;
                do j++;
                while (char.IsLetterOrDigit(Add(ref c, j)) || Add(ref c, j) == '_');
                return Avx.IsSupported
                    ? IsIntelKeyword(ref Add(ref c, i), j - i) == Token.Id
                    : IsKeyword(text.AsSpan()[i..j]) == Token.Id;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static double ToReal(string text, int from, int to) =>
                double.Parse(text.AsSpan()[from..to], CultureInfo.InvariantCulture);
        }
        else
        {
            lexCursor++;
            switch (ch)
            {
                case '\0': (kind, start) = (Token.Eof, text.Length - 1); return;
                case ',': kind = Token.Comma; return;
                case ';': kind = Token.Semicolon; return;
                case '(': kind = Token.LPar; return;
                case ')': kind = Token.RPar; return;
                case '[': kind = Token.LBra; return;
                case ']': kind = Token.RBra; return;
                case '{': kind = Token.LBrace; return;
                case '}': kind = Token.RBrace; return;
                case '+': kind = Token.Plus; return;
                case '*': kind = Token.Times; return;
                case '/': kind = Token.Div; return;
                case '%': kind = Token.Mod; return;
                case '^': kind = Token.Caret; return;
                case '²': kind = Token.Caret2; return;
                case '\'': kind = Token.Transpose; return;
                case '-':
                    if (Add(ref c, lexCursor) == '-')
                    {
                        do ch = Add(ref c, ++lexCursor);
                        while (ch is not '\r' and not '\n' and not '\0');
                        goto SKIP_BLANKS;
                    }
                    kind = Token.Minus;
                    return;
                case '=':
                    (kind, start) = Add(ref c, lexCursor) == '>'
                        ? (Token.Arrow, lexCursor++ - 1)
                        : (Token.Eq, start);
                    return;
                case '.':
                    (kind, start) = Add(ref c, lexCursor) switch
                    {
                        '*' => (Token.PointTimes, lexCursor++ - 1),
                        '/' => (Token.PointDiv, lexCursor++ - 1),
                        '.' => (Token.Range, lexCursor++ - 1),
                        _ => (Token.Dot, start),
                    };
                    return;
                case ':':
                    (kind, start) = Add(ref c, lexCursor) == ':'
                        ? (Token.DoubleColon, lexCursor++ - 1)
                        : (Token.Colon, start);
                    return;
                case '!':
                    (kind, start) = Add(ref c, lexCursor) == '='
                        ? (Token.Ne, lexCursor++ - 1)
                        : (Token.Error, start);
                    return;
                case '<':
                    (kind, start) = Add(ref c, lexCursor) switch
                    {
                        '=' => (Token.Le, lexCursor++ - 1),
                        '>' => (Token.Ne, lexCursor++ - 1),
                        _ => (Token.Lt, start),
                    };
                    return;
                case '>':
                    (kind, start) = Add(ref c, lexCursor) == '='
                        ? (Token.Ge, lexCursor++ - 1)
                        : (Token.Gt, start);
                    return;
                case '"':
                    int first = lexCursor--;
                    do
                    {
                        ch = Add(ref c, ++lexCursor);
                        if (ch == '\0')
                            throw Error("Unterminated string literal");
                    }
                    while (ch != '"');
                    if (Add(ref c, lexCursor + 1) != '"')
                    {
                        (kind, id) = (Token.Str, text[first..lexCursor++]);
                        return;
                    }
                    // This is a string literal with embedded quotes.
                    sb ??= new();
                    sb.Length = 0;
                    sb.Append(text.AsSpan()[first..(lexCursor + 1)]);
                    first = lexCursor += 2;
                MORE_STRING:
                    do
                    {
                        ch = Add(ref c, lexCursor++);
                        if (ch == '\0')
                            throw Error("Unterminated string literal");
                    }
                    while (ch != '"');
                    sb.Append(text.AsSpan()[first..(lexCursor - 1)]);
                    if (Add(ref c, lexCursor) == '"')
                    {
                        sb.Append('"');
                        first = ++lexCursor;
                        goto MORE_STRING;
                    }
                    (kind, id) = (Token.Str, sb.ToString());
                    return;
                default:
                    kind = Token.Error;
                    lexCursor = text.Length - 1;
                    return;
            }
        }
    }

    /// <summary>Check AUSTRA keywords.</summary>
    /// <param name="c">Reference to first character.</param>
    /// <param name="len">Length of the identifier.</param>
    /// <returns>Token.Id, if not a keyword; otherwise, the corresponding keyword.</returns>
    private static Token IsIntelKeyword(ref char c, int len)
    {
        const ulong kAnd = 'A' | ('N' << 16) | ((ulong)'D' << 32);
        const ulong kDef = 'D' | ('E' << 16) | ((ulong)'F' << 32);
        const ulong kElse = 'e' | ('l' << 16) | ((ulong)'s' << 32) | ((ulong)'e' << 48);
        const ulong kElif = 'e' | ('l' << 16) | ((ulong)'i' << 32) | ((ulong)'f' << 48);
        const uint kIf = 'i' | ('f' << 16);
        const uint kIn = 'i' | ('n' << 16);
        const ulong kFalse = 'f' | ('a' << 16) | ((ulong)'l' << 32) | ((ulong)'s' << 48);
        const ulong kLet = 'L' | ('E' << 16) | ((ulong)'T' << 32);
        const ulong kNot = 'N' | ('O' << 16) | ((ulong)'T' << 32);
        const uint kOr = 'o' | ('r' << 16);
        const ulong kSet = 'S' | ('E' << 16) | ((ulong)'T' << 32);
        const ulong kThen = 't' | ('h' << 16) | ((ulong)'e' << 32) | ((ulong)'n' << 48);
        const ulong kTrue = 't' | ('r' << 16) | ((ulong)'u' << 32) | ((ulong)'e' << 48);
        const ulong kUndef = 'u' | ('n' << 16) | ((ulong)'d' << 32) | ((ulong)'e' << 48);

        return len switch
        {
            2 => (As<char, uint>(ref c) | 0x0020_0020) switch
            {
                kIf => Token.If,
                kIn => Token.In,
                kOr => Token.Or,
                _ => Token.Id,
            },
            3 => (As<char, ulong>(ref c) & 0xFFDF_FFDF_FFDF) switch
            {
                kAnd => Token.And,
                kDef => Token.Def,
                kLet => Token.Let,
                kNot => Token.Not,
                kSet => Token.Set,
                _ => Token.Id,
            },
            4 => (As<char, ulong>(ref c) | 0x0020_0020_0020_0020) switch
            {
                kElse => Token.Else,
                kElif => Token.Elif,
                kThen => Token.Then,
                kTrue => Token.True,
                _ => Token.Id,
            },
            5 => (As<char, ulong>(ref c) | 0x0020_0020_0020_0020) switch
            {
                kFalse => (Add(ref c, 4) | 0x20) == 'e' ? Token.False : Token.Id,
                kUndef => (Add(ref c, 4) | 0x20) == 'f' ? Token.Undef : Token.Id,
                _ => Token.Id,
            },
            _ => Token.Id,
        };
    }

    /// <summary>Check AUSTRA keywords.</summary>
    /// <param name="text">Text span to check.</param>
    /// <returns>Token.Id, if not a keyword; otherwise, the corresponding keyword.</returns>
    private static Token IsKeyword(ReadOnlySpan<char> text) => (text[0] | 0x20) switch
    {
        'a' =>
            text[1..].Equals("nd", StringComparison.OrdinalIgnoreCase) ? Token.And : Token.Id,
        'd' =>
            text[1..].Equals("ef", StringComparison.OrdinalIgnoreCase) ? Token.Def : Token.Id,
        'e' =>
            text[1..].Equals("lse", StringComparison.OrdinalIgnoreCase) ? Token.Else
            : text[1..].Equals("lif", StringComparison.OrdinalIgnoreCase) ? Token.Elif : Token.Id,
        'f' =>
            text[1..].Equals("alse", StringComparison.OrdinalIgnoreCase) ? Token.False : Token.Id,
        'i' =>
            text.Length != 2 ? Token.Id :
            (text[1] | 0x20) is 'f' ? Token.If :
            (text[1] | 0x20) is 'n' ? Token.In : Token.Id,
        'l' =>
            text[1..].Equals("et", StringComparison.OrdinalIgnoreCase) ? Token.Let : Token.Id,
        'n' =>
            text[1..].Equals("ot", StringComparison.OrdinalIgnoreCase) ? Token.Not : Token.Id,
        'o' =>
            text.Length == 2 && (text[1] | 0x20) is 'r' ? Token.Or : Token.Id,
        's' =>
            text[1..].Equals("et", StringComparison.OrdinalIgnoreCase) ? Token.Set : Token.Id,
        't' =>
            text[1..].Equals("hen", StringComparison.OrdinalIgnoreCase) ? Token.Then :
            text[1..].Equals("rue", StringComparison.OrdinalIgnoreCase) ? Token.True : Token.Id,
        'u' =>
            text[1..].Equals("ndef", StringComparison.OrdinalIgnoreCase) ? Token.Undef : Token.Id,
        _ => Token.Id,
    };

    /// <summary>Parses date constants like <c>jan2020</c> and <c>dec20</c>.</summary>
    /// <param name="text">Text span to analyze.</param>
    /// <param name="date">When succeeds, returns the first date of the month.</param>
    /// <returns><see langword="true"/> if succeeds.</returns>
    private static bool TryParseMonthYear(ReadOnlySpan<char> text, out Date date)
    {
        if (text.Length >= 5)
        {
            int month = (text[0] | 0x20) switch
            {
                'a' =>
                    (text[1] | 0x20) is 'p' && (text[2] | 0x20) is 'r' ? 4
                    : (text[1] | 0x20) is 'u' && (text[2] | 0x20) is 'g' ? 8 : 0,
                'd' =>
                    (text[1] | 0x20) is 'e' && (text[2] | 0x20) is 'c' ? 12 : 0,
                'f' =>
                    (text[1] | 0x20) is 'e' && (text[2] | 0x20) is 'b' ? 2 : 0,
                'j' =>
                    (text[1] | 0x20) is 'a' && (text[2] | 0x20) is 'n' ? 1
                    : (text[1] | 0x20) is not 'u' ? 0
                    : (text[2] | 0x20) is 'n' ? 6
                    : (text[2] | 0x20) is 'l' ? 7 : 0,
                'm' =>
                    (text[1] | 0x20) is not 'a' ? 0
                    : (text[2] | 0x20) is 'r' ? 3 : (text[2] | 0x20) is 'y' ? 5 : 0,
                'n' =>
                    (text[1] | 0x20) is 'o' && (text[2] | 0x20) is 'v' ? 11 : 0,
                'o' =>
                    (text[1] | 0x20) is 'c' && (text[2] | 0x20) is 't' ? 10 : 0,
                's' =>
                    (text[1] | 0x20) is 'e' && (text[2] | 0x20) is 'p' ? 9 : 0,
                _ => 0,
            };
            if (month > 0 && int.TryParse(text[3..], out int year))
                if (text.Length == 5)
                {
                    date = new(2000 + year, month, 1);
                    Date top = Date.Today.AddYears(20);
                    if (date > top)
                        date = date.AddYears(-100);
                    return true;
                }
                else if (text.Length == 7)
                {
                    date = new Date(year, month, 1);
                    return true;
                }
        }
        date = Date.Zero;
        return false;
    }

    private static Date ParseDateLiteral(ReadOnlySpan<char> text, int position)
    {
        int i = text.IndexOf('@');
        if (!int.TryParse(text[..i], out int day) || day <= 0)
            throw new AstException("Invalid day of month", position);
        if (!TryParseMonthYear(text[(i + 1)..], out Date date))
            throw new AstException("Invalid month or year", position + i + 1);
        (int y, int m, int _) = date;
        return day <= Date.DaysInMonth(y, m) ? date + day - 1
            : throw new AstException("Invalid day of month", position);
    }

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

    internal Exception Error(string message, int position) =>
        abortPosition == int.MaxValue
        ? new AstException(message, position)
        : new AbortException(message);

    internal Exception Error(string message) =>
        abortPosition == int.MaxValue
        ? new AstException(message, start)
        : new AbortException(message);
}
