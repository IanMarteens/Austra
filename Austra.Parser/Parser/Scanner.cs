using System.Runtime.Intrinsics.X86;
using static System.Runtime.CompilerServices.Unsafe;

namespace Austra.Parser;

/// <summary>The part of <see cref="Parser"/> that deals with lexical analysis.</summary>
internal class Scanner
{
    /// <summary>Used by the scanner to build string literals.</summary>
    private StringBuilder? sb;

    /// <summary>The text being scanned.</summary>
    protected readonly string text;
    /// <summary>Gets the type of the current lexeme.</summary>
    /// <remarks>Updated by the <see cref="Move"/> method.</remarks>
    protected Token kind;
    /// <summary>Gets the start position of the current lexeme.</summary>
    /// <remarks>
    /// <para>Updated by the <see cref="Move"/> method.</para>
    /// <para><see cref="start"/> is always lesser than <see cref="lexCursor"/>.</para>
    /// </remarks>
    protected int start;
    /// <summary>Gets the string associated with the current lexeme.</summary>
    /// <remarks>Updated by the <see cref="Move"/> method.</remarks>
    protected string id = "";
    /// <summary>Value of the current lexeme as a real number.</summary>
    protected double asReal;
    /// <summary>Value of the lexeme as an integer.</summary>
    protected int asInt;
    /// <summary>Value of the lexeme as a date literal.</summary>
    protected Date asDate;
    /// <summary>Current position in the text.</summary>
    /// <remarks>Updated by the <see cref="Move"/> method.</remarks>
    protected int lexCursor;
    /// <summary>Position where the parsing should be aborted.</summary>
    /// <remarks>
    /// This is checked by the scanner to throw an <see cref="AbortException"/>
    /// when the position is reached.
    /// When this field's value is different from <see cref="int.MaxValue"/>,
    /// the <see cref="Error(string)"/> helper creates also an <see cref="AbortException"/>
    /// instead of an <see cref="AstException"/>, that can be easily dismissed when debugging.
    /// </remarks>
    protected int abortPosition = int.MaxValue;

    /// <summary>Initializes a scanning context with the given text.</summary>
    /// <param name="text">Text to scan or parse.</param>
    public Scanner(string text)
    {
        this.text = text;
        Move();
    }

    /// <summary>Skips two tokens with a single call.</summary>
    protected void SkipFunctor()
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
    protected void CheckAndMove(Token kind, string errorMessage)
    {
        if (this.kind != kind)
            throw Error(errorMessage);
        Move();
    }

    /// <summary>Advances the lexical analyzer one token.</summary>
    protected void Move()
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
                case '∈': kind = Token.Element; return;
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
                        '-' => (Token.Element, lexCursor++ - 1),
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
    protected static bool TryParseMonthYear(ReadOnlySpan<char> text, out Date date)
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

    /// <summary>Creates an exception pointing to a given position in the text.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="position">The position in the text.</param>
    /// <returns>The resulting exception.</returns>
    internal Exception Error(string message, int position) =>
        abortPosition == int.MaxValue
        ? new AstException(message, position)
        : new AbortException(message);

    /// <summary>Creates an exception pointing to the current position in the text.</summary>
    /// <param name="message">The error message.</param>
    /// <returns>The resulting exception.</returns>
    internal Exception Error(string message) =>
        abortPosition == int.MaxValue
        ? new AstException(message, start)
        : new AbortException(message);
}
