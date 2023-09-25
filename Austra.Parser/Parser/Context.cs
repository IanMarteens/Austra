using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using static System.Runtime.CompilerServices.Unsafe;

namespace Austra.Parser;

/// <summary>A parsing exception associated with a position.</summary>
public class AstException : ApplicationException
{
    /// <summary>Gets the position inside the source code.</summary>
    public int Position { get; }

    /// <summary>Creates a new exception with a message and a position.</summary>
    /// <param name="message">Error message.</param>
    /// <param name="position">Error position.</param>
    public AstException(string message, int position)
        : base(message) => Position = position;
}

/// <summary>Provides lexical analysis and inherited attributes for the parser.</summary>
internal sealed partial class AstContext
{
    /// <summary>The text being scanned.</summary>
    private readonly string text;
    /// <summary>Compressed value of the current lexeme.</summary>
    private LexValue val;
    /// <summary>Used by the scanner to build string literals.</summary>
    private StringBuilder? sb;
    /// <summary>Current position in the text.</summary>
    /// <remarks>Updated by the <see cref="MoveNext"/> method.</remarks>
    private int i;

    /// <summary>Initializes a parsing context.</summary>
    /// <param name="source">Environment variables.</param>
    /// <param name="text">Text of the formula.</param>
    public AstContext(IDataSource source, string text)
    {
        (Source, this.text, Id) = (source, text, "");
        MoveNext();
    }

    /// <summary>Gets the outer scope for variables.</summary>
    public IDataSource Source { get; }

    /// <summary>Gets the text being scanned.</summary>
    public string Text => text;

    /// <summary>Gets the type of the current lexeme.</summary>
    /// <remarks>Updated by the <see cref="MoveNext"/> method.</remarks>
    public Token Kind { get; private set; }

    /// <summary>Gets the start position of the current lexeme.</summary>
    /// <remarks>Updated by the <see cref="MoveNext"/> method.</remarks>
    public int Start { get; private set; }

    /// <summary>Gets the string associated with the current lexeme.</summary>
    /// <remarks>Updated by the <see cref="MoveNext"/> method.</remarks>
    public string Id { get; private set; }

    /// <summary>Value of the current lexeme as a real number.</summary>
    public double AsReal { get => val.AsReal; private set => val.AsReal = value; }
    /// <summary>Value of the lexeme as an integer.</summary>
    public int AsInt { get => val.AsInt; private set => val.AsInt = value; }
    /// <summary>Value of the lexeme as a date literal.</summary>
    public Date AsDate { get => val.AsDate; private set => val.AsDate = value; }

    /// <summary>Gets the parameter referencing the outer scope.</summary>
    public static ParameterExpression SourceParameter { get; } =
        Expression.Parameter(typeof(IDataSource), "datasource");

    /// <summary>Place holder for the first lambda parameter, if any.</summary>
    public ParameterExpression? LambdaParameter { get; set; }
    /// <summary>Place holder for the second lambda parameter, if any.</summary>
    public ParameterExpression? LambdaParameter2 { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (string lowerText, int position) GetTextAndPos() =>
        (Id.ToLower(), Start);

    /// <summary>Transient local variable definitions.</summary>
    public Dictionary<string, ParameterExpression> Locals { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Skips two tokens with a single call.</summary>
    public void Skip2() { MoveNext(); MoveNext(); }

    /// <summary>
    /// Checks that the current token is of the expected kind and advances the cursor.
    /// </summary>
    /// <param name="kind">Expected type of token.</param>
    /// <param name="errorMessage">Error message to use in the exception.</param>
    /// <exception cref="AstException">Thrown when the token doesn't match.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CheckAndMoveNext(Token kind, string errorMessage)
    {
        if (Kind != kind)
            throw new AstException(errorMessage, Start);
        MoveNext();
    }

    /// <summary>Controls that only persisted values are used.</summary>
    public bool ParsingDefinition { get; set; }

    /// <summary>Name of the left side value, if any.</summary>
    public string LeftValue { get; set; } = "";

    /// <summary>Referenced definitions.</summary>
    public HashSet<Definition> References { get; } = new();

    /// <summary>
    /// Creates an expression that retrieves a series from the data source.
    /// </summary>
    /// <param name="id">Series name.</param>
    /// <param name="type">Result type.</param>
    public static Expression GetFromDataSource(string id, Type type) =>
        Expression.Convert(
            Expression.Property(SourceParameter, "Item", Expression.Constant(id)), type);

    /// <summary>Advances the lexical analyzer one token.</summary>
    public void MoveNext()
    {
        ref char c = ref As<Str>(text).FirstChar;
    SKIP_BLANKS:
        while (char.IsWhiteSpace(Add(ref c, i)))
            i++;
        Start = i;
        char ch = Add(ref c, i);
        // Check keywords, functors, class names and identifiers.
        if (char.IsLetter(ch))
        {
            do ch = Add(ref c, ++i);
            while (char.IsLetterOrDigit(ch) || ch == '_');
            // Check for keywords and function identifiers
            Token tok;
            if (Avx.IsSupported)
                tok = IsIntelKeyword(ref Add(ref c, Start), i - Start);
            else
                tok = IsKeyword(text.AsSpan()[Start..i]);
            if (tok != Token.Id)
            {
                Kind = tok;
                return;
            }
            // Skip blanks after the identifier.
            string id = text[Start..i];
            while (char.IsWhiteSpace(Add(ref c, i)))
                i++;
            ch = Add(ref c, i);
            Id = id;
            Kind =
                ch == '(' ? Token.Functor
                : ch == ':' && Add(ref c, i + 1) == ':' ? Token.ClassName
                : Token.Id;
        }
        else if ((uint)(ch - '0') < 10u)
        {
            do i++;
            while ((uint)(Add(ref c, i) - '0') < 10u);
            ch = Add(ref c, i);
            if (ch == '@')
            {
                do i++;
                while (char.IsLetterOrDigit(Add(ref c, i)));
                Kind = Token.Date;
                AsDate = ParseDateLiteral(text.AsSpan()[Start..i], Start);
            }
            else if (ch == '.')
            {
                do i++;
                while ((uint)(Add(ref c, i) - '0') < 10u);
                if ((Add(ref c, i) | 0x20) == 'e')
                {
                    i++;
                    if (Add(ref c, i) is '+' or '-')
                        i++;
                    while ((uint)(Add(ref c, i) - '0') < 10u)
                        i++;
                }
                if (Add(ref c, i) == 'i' && !char.IsLetterOrDigit(Add(ref c, i + 1)))
                    (Kind, AsReal) = (Token.Imag, ToReal(text, Start, i++));
                else if (char.IsLetter(Add(ref c, i)) && IsVariableSuffix(text, i, out int j))
                    (Kind, Id, AsReal, i) = (Token.MultVarR, text[i..j], ToReal(text, Start, i), j);
                else
                    (Kind, AsReal) = (Token.Real, ToReal(text, Start, i));
            }
            else if ((ch | 0x20) == 'e')
            {
                if (Add(ref c, ++i) is '+' or '-')
                    i++;
                while ((uint)(Add(ref c, i) - '0') < 10u)
                    i++;
                if (Add(ref c, i) == 'i' && !char.IsLetterOrDigit(Add(ref c, i + 1)))
                    (Kind, AsReal) = (Token.Imag, ToReal(text, Start, i++));
                else if (char.IsLetter(Add(ref c, i)) && IsVariableSuffix(text, i, out int j))
                    (Kind, Id, AsReal, i) = (Token.MultVarR, text[i..j], ToReal(text, Start, i), j);
                else
                    (Kind, AsReal) = (Token.Real, ToReal(text, Start, i));
            }
            else if (ch == 'i' && !char.IsLetterOrDigit(Add(ref c, i + 1)))
                (Kind, AsReal) = (Token.Imag, ToReal(text, Start, i++));
            else if (char.IsLetter(ch) && IsVariableSuffix(text, i, out int k))
                (Kind, Id, AsInt, i)
                    = (Token.MultVarI, text[i..k], int.Parse(text.AsSpan()[Start..i]), k);
            else
                (Kind, AsInt) = (Token.Int, int.Parse(text.AsSpan()[Start..i]));

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
            i++;
            switch (ch)
            {
                case '\0': (Kind, Start) = (Token.Eof, text.Length - 1); return;
                case ',': Kind = Token.Comma; return;
                case ';': Kind = Token.Semicolon; return;
                case '(': Kind = Token.LPar; return;
                case ')': Kind = Token.RPar; return;
                case '[': Kind = Token.LBra; return;
                case ']': Kind = Token.RBra; return;
                case '{': Kind = Token.LBrace; return;
                case '}': Kind = Token.RBrace; return;
                case '+': Kind = Token.Plus; return;
                case '*': Kind = Token.Times; return;
                case '/': Kind = Token.Div; return;
                case '%': Kind = Token.Mod; return;
                case '^': Kind = Token.Caret; return;
                case '\'': Kind = Token.Transpose; return;
                case '\\': Kind = Token.Backslash; return;
                case '-':
                    if (Add(ref c, i) == '-')
                    {
                        do ch = Add(ref c, ++i);
                        while (ch is not '\r' and not '\n' and not '\0');
                        goto SKIP_BLANKS;
                    }
                    Kind = Token.Minus;
                    return;
                case '=':
                    (Kind, Start) = Add(ref c, i) == '>'
                        ? (Token.Arrow, i++ - 1)
                        : (Token.Eq, Start);
                    return;
                case '.':
                    (Kind, Start) = Add(ref c, i) == '*'
                        ? (Token.PointTimes, i++ - 1)
                        : Add(ref c, i) == '/'
                        ? (Token.PointDiv, i++ - 1)
                        : (Token.Dot, Start);
                    return;
                case ':':
                    (Kind, Start) = Add(ref c, i) == ':'
                        ? (Token.DoubleColon, i++ - 1)
                        : (Token.Colon, Start);
                    return;
                case '!':
                    (Kind, Start) = Add(ref c, i) == '='
                        ? (Token.Ne, i++ - 1)
                        : (Token.Error, Start);
                    return;
                case '<':
                    (Kind, Start) = Add(ref c, i) == '='
                        ? (Token.Le, i++ - 1)
                        : Add(ref c, i) == '>'
                        ? (Token.Ne, i++ - 1)
                        : (Token.Lt, Start);
                    return;
                case '>':
                    (Kind, Start) = Add(ref c, i) == '='
                        ? (Token.Ge, i++ - 1)
                        : (Token.Gt, Start);
                    return;
                case '"':
                    int first = i--;
                    do
                    {
                        ch = Add(ref c, ++i);
                        if (ch == '\0')
                            throw new AstException("Unterminated string literal", Start);
                    }
                    while (ch != '"');
                    if (Add(ref c, i + 1) != '"')
                    {
                        (Kind, Id) = (Token.Str, text[first..i++]);
                        return;
                    }
                    // This is a string literal with embedded quotes.
                    sb ??= new();
                    sb.Length = 0;
                    sb.Append(text.AsSpan()[first..(i + 1)]);
                    first = i += 2;
                MORE_STRING:
                    do
                    {
                        ch = Add(ref c, i++);
                        if (ch == '\0')
                            throw new AstException("Unterminated string literal", Start);
                    }
                    while (ch != '"');
                    sb.Append(text.AsSpan()[first..(i - 1)]);
                    if (Add(ref c, i) == '"')
                    {
                        sb.Append('"');
                        first = ++i;
                        goto MORE_STRING;
                    }
                    (Kind, Id) = (Token.Str, sb.ToString());
                    return;
                default:
                    Kind = Token.Error;
                    i = text.Length - 1;
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
        const uint kAnd = 'a' | ('n' << 16);
        const uint kDef = 'd' | ('e' << 16);
        const ulong kElse = 'e' | ('l' << 16) | ((ulong)'s' << 32) | ((ulong)'e' << 48);
        const uint kIf = 'i' | ('f' << 16);
        const uint kIn = 'i' | ('n' << 16);
        const ulong kFalse = 'f' | ('a' << 16) | ((ulong)'l' << 32) | ((ulong)'s' << 48);
        const uint kLet = 'l' | ('e' << 16);
        const uint kNot = 'n' | ('o' << 16);
        const uint kOr = 'o' | ('r' << 16);
        const uint kSet = 's' | ('e' << 16);
        const ulong kThen = 't' | ('h' << 16) | ((ulong)'e' << 32) | ((ulong)'n' << 48);
        const ulong kTrue = 't' | ('r' << 16) | ((ulong)'u' << 32) | ((ulong)'e' << 48);
        const ulong kUndef = 'u' | ('n' << 16) | ((ulong)'d' << 32) | ((ulong)'e' << 48);

        return len switch
        {
            2 => (As<char, uint>(ref c) | 0x00200020) switch
            {
                kIf => Token.If,
                kIn => Token.In,
                kOr => Token.Or,
                _ => Token.Id,
            },
            3 => (As<char, uint>(ref c) | 0x00200020) switch
            {
                kAnd => (Add(ref c, 2) | 0x20) == 'd' ? Token.And : Token.Id,
                kDef => (Add(ref c, 2) | 0x20) == 'f' ? Token.Def : Token.Id,
                kLet => (Add(ref c, 2) | 0x20) == 't' ? Token.Let : Token.Id,
                kNot => (Add(ref c, 2) | 0x20) == 't' ? Token.Not : Token.Id,
                kSet => (Add(ref c, 2) | 0x20) == 't' ? Token.Set : Token.Id,
                _ => Token.Id,
            },
            4 => (As<char, ulong>(ref c) | 0x0020002000200020) switch
            {
                kElse => Token.Else,
                kThen => Token.Then,
                kTrue => Token.True,
                _ => Token.Id,
            },
            5 => (As<char, ulong>(ref c) | 0x0020002000200020) switch
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
            text[1..].Equals("lse", StringComparison.OrdinalIgnoreCase) ? Token.Else : Token.Id,
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

    private static int TryGetMonth(ReadOnlySpan<char> name) => name[0] switch
    {
        'a' or 'A' =>
            name[1..].Equals("pr", StringComparison.OrdinalIgnoreCase) ? 4
            : name[1..].Equals("ug", StringComparison.OrdinalIgnoreCase) ? 8 : 0,
        'd' or 'D' =>
            name[1] is 'e' or 'E' && name[2] is 'c' or 'C' ? 12 : 0,
        'f' or 'F' =>
            name[1..].Equals("eb", StringComparison.OrdinalIgnoreCase) ? 2 : 0,
        'j' or 'J' =>
            name[1..].Equals("an", StringComparison.OrdinalIgnoreCase) ? 1
            : name[1..].Equals("un", StringComparison.OrdinalIgnoreCase) ? 6
            : name[1..].Equals("ul", StringComparison.OrdinalIgnoreCase) ? 7 : 0,
        'm' or 'M' =>
            name[1..].Equals("ar", StringComparison.OrdinalIgnoreCase) ? 3
            : name[1..].Equals("ay", StringComparison.OrdinalIgnoreCase) ? 5 : 0,
        'n' or 'N' =>
            name[1] is 'o' or 'O' && name[2] is 'v' or 'V' ? 11 : 0,
        'o' or 'O' =>
            name[1] is 'c' or 'C' && name[2] is 't' or 'T' ? 10 : 0,
        's' or 'S' =>
            name[1] is 'e' or 'E' && name[2] is 'p' or 'P' ? 9 : 0,
        _ => 0,
    };

    /// <summary>Parses date constants like <c>jan2020</c> and <c>dec20</c>.</summary>
    /// <param name="text">Text span to analyze.</param>
    /// <param name="date">When succeeds, returns the first date of the month.</param>
    /// <returns><see langword="true"/> if succeeds.</returns>
    public static bool TryParseMonthYear(ReadOnlySpan<char> text, out Date date)
    {
        if (text.Length >= 5)
        {
            int month = TryGetMonth(text[..3]);
            if (month > 0 && int.TryParse(text[3..], out int year))
                if (text.Length == 5 )
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

    /// <summary>C# union to save space in the lexeme.</summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct LexValue
    {
        [FieldOffset(0)] public double AsReal;
        [FieldOffset(0)] public int AsInt;
        [FieldOffset(0)] public Date AsDate;
    }

    /// <summary>Internal stub for accessing string internals.</summary>
    private sealed class Str
    {
#pragma warning disable CS0649
        /// <summary>The length of the string.</summary>
        public int Length;
#pragma warning restore CS0649
        /// <summary>The first character in the string.</summary>
        public char FirstChar;
    }
}
