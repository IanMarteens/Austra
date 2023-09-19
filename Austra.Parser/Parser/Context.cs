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

/// <summary>Represents a lexical token.</summary>
/// <param name="Kind">Token kind.</param>
/// <param name="Text">Token text.</param>
/// <param name="Position">Token position inside the expression.</param>
internal readonly record struct Lexeme(
    Token Kind,
    string Text,
    int Position)
{
    private readonly LexValue val;

    /// <summary>Creates a symbolic lexeme with no text information.</summary>
    /// <param name="kind">Token kind.</param>
    /// <param name="position">Position inside the expression.</param>
    public Lexeme(Token kind, int position) : this(kind, "", position) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out Token kind, out int position) =>
        (kind, position) = (Kind, Position);

    /// <summary>Value of the lexeme as a real number.</summary>
    public double AsReal { get => val.AsReal; init => val.AsReal = value; }
    /// <summary>Value of the lexeme as an integer.</summary>
    public int AsInt { get => val.AsInt; init => val.AsInt = value; }
    /// <summary>Value of the lexeme as a date literal.</summary>
    public Date AsDate { get => val.AsDate; init => val.AsDate = value; }

    /// <summary>Checks if the lexeme is a given token.</summary>
    /// <param name="text">Text to check.</param>
    /// <returns><see langword="true"/> when there's a match.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Is(string text) =>
        text.Equals(Text, StringComparison.OrdinalIgnoreCase);

    /// <summary>C# union to save space in the lexeme.</summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct LexValue
    {
        [FieldOffset(0)] public double AsReal;
        [FieldOffset(0)] public int AsInt;
        [FieldOffset(0)] public Date AsDate;
    }
}

/// <summary>Provides lexical analysis and inherited attributes for the parser.</summary>
internal sealed partial class AstContext
{
    /// <summary>The text being scanned.</summary>
    private readonly string text;
    /// <summary>Used by the scanner to build string literals.</summary>
    private StringBuilder? sb;
    /// <summary>Current position in the text.</summary>
    private int i;

    /// <summary>Initializes a parsing context.</summary>
    /// <param name="source">Environment variables.</param>
    /// <param name="text">Text of the formula.</param>
    public AstContext(IDataSource source, string text)
    {
        (Source, this.text) = (source, text);
        MoveNext();
    }

    /// <summary>Gets the outer scope for variables.</summary>
    public IDataSource Source { get; }

    /// <summary>The current lexeme. Updated by the <see cref="MoveNext"/> method.</summary>
    public Lexeme Current { get; private set; }

    /// <summary>Gets the parameter referencing the outer scope.</summary>
    public static ParameterExpression SourceParameter { get; } =
        Expression.Parameter(typeof(IDataSource), "datasource");

    /// <summary>Place holder for the first lambda parameter, if any.</summary>
    public ParameterExpression? LambdaParameter { get; set; }
    /// <summary>Place holder for the second lambda parameter, if any.</summary>
    public ParameterExpression? LambdaParameter2 { get; set; }

    /// <summary>Gets the type of the active token.</summary>
    public Token Kind
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Current.Kind;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (string lowerText, int position) GetTextAndPos() =>
        (Current.Text.ToLower(), Current.Position);

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
        if (Current.Kind != kind)
            throw new AstException(errorMessage, Current.Position);
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
        char ch = Add(ref c, i);
        // Check keywords, functors, class names and identifiers.
        if (char.IsLetter(ch))
        {
            int first = i;
            do ch = Add(ref c, ++i);
            while (char.IsLetterOrDigit(ch) || ch == '_');
            // Check for keywords and function identifiers
            if (Avx.IsSupported)
            {
                Token tok = IsIntelKeyword(ref Add(ref c, first), i - first);
                if (tok != Token.Id)
                {
                    Current = new(tok, first);
                    return;
                }
                // Skip blanks after the identifier.
                string id = text[first..i];
                while (char.IsWhiteSpace(Add(ref c, i)))
                    i++;
                ch = Add(ref c, i);
                Current = ch == '('
                    ? new(Token.Functor, id, first)
                    : ch == ':' && Add(ref c, i + 1) == ':'
                    ? new(Token.ClassName, id, first)
                    : new(Token.Id, id, first);
            }
            else
            {
                ReadOnlySpan<char> s = text.AsSpan()[first..i];
                Token tok = IsKeyword(s);
                if (tok != Token.Id)
                {
                    Current = new(tok, first);
                    return;
                }
                // Skip blanks after the identifier.
                while (char.IsWhiteSpace(Add(ref c, i)))
                    i++;
                ch = Add(ref c, i);
                Current = ch == '('
                    ? new(Token.Functor, s.ToString(), first)
                    : ch == ':' && Add(ref c, i + 1) == ':'
                    ? new(Token.ClassName, s.ToString(), first)
                    : new(Token.Id, s.ToString(), first);
            }
        }
        else if ((uint)(ch - '0') < 10u)
        {
            int first = i;
            do i++;
            while ((uint)(Add(ref c, i) - '0') < 10u);
            ch = Add(ref c, i);
            if (ch == '@')
            {
                do i++;
                while (char.IsLetterOrDigit(Add(ref c, i)));
                Current = new(Token.Date, first)
                {
                    AsDate = ParseDateLiteral(text.AsSpan()[first..i], first)
                };
            }
            else if (ch == '.')
            {
                do i++;
                while ((uint)(Add(ref c, i) - '0') < 10u);
                if (Add(ref c, i) is 'E' or 'e')
                {
                    i++;
                    if (Add(ref c, i) is '+' or '-')
                        i++;
                    while ((uint)(Add(ref c, i) - '0') < 10u)
                        i++;
                }
                if (Add(ref c, i) == 'i' && !char.IsLetterOrDigit(Add(ref c, i + 1)))
                    Current = new(Token.Imag, first) { AsReal = AsReal(text, first, i++) };
                else if (char.IsLetter(Add(ref c, i)) && IsVariableSuffix(text, i, out int j))
                {
                    Current = new(Token.MultVarR, text[i..j], first) { AsReal = AsReal(text, first, i) };
                    i = j;
                }
                else
                    Current = new(Token.Real, first) { AsReal = AsReal(text, first, i) };
            }
            else if (ch is 'E' or 'e')
            {
                if (Add(ref c, ++i) is '+' or '-')
                    i++;
                while ((uint)(Add(ref c, i) - '0') < 10u)
                    i++;
                if (Add(ref c, i) == 'i' && !char.IsLetterOrDigit(Add(ref c, i + 1)))
                    Current = new(Token.Imag, first) { AsReal = AsReal(text, first, i++) };
                if (char.IsLetter(Add(ref c, i)) && IsVariableSuffix(text, i, out int j))
                {
                    Current = new(Token.MultVarR, text[i..j], first) { AsReal = AsReal(text, first, i) };
                    i = j;
                }
                else
                    Current = new(Token.Real, first) { AsReal = AsReal(text, first, i) };
            }
            else if (ch == 'i' && !char.IsLetterOrDigit(Add(ref c, i + 1)))
                Current = new(Token.Imag, first) { AsReal = AsReal(text, first, i++) };
            else if (char.IsLetter(ch) && IsVariableSuffix(text, i, out int k))
            {
                Current = new(Token.MultVarI, text[i..k], first)
                {
                    AsInt = int.Parse(text.AsSpan()[first..i])
                };
                i = k;
            }
            else
                Current = new(Token.Int, first) { AsInt = int.Parse(text.AsSpan()[first..i]) };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsVariableSuffix(string text, int i, out int j)
            {
                j = i;
                ref char c = ref As<Str>(text).FirstChar;
                do j++;
                while (char.IsLetterOrDigit(Add(ref c, j)) || Add(ref c, j) == '_');
                if (Avx.IsSupported)
                    return IsIntelKeyword(ref Add(ref c, i), j - i) == Token.Id;
                else
                    return IsKeyword(text.AsSpan()[i..j]) == Token.Id;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static double AsReal(string text, int from, int to) =>
                double.Parse(text.AsSpan()[from..to], CultureInfo.InvariantCulture);
        }
        else
            switch (ch)
            {
                case '\0': Current = new(Token.Eof, text.Length - 1); return;
                case ',': Current = new(Token.Comma, i++); return;
                case ';': Current = new(Token.Semicolon, i++); return;
                case '(': Current = new(Token.LPar, i++); return;
                case ')': Current = new(Token.RPar, i++); return;
                case '[': Current = new(Token.LBra, i++); return;
                case ']': Current = new(Token.RBra, i++); return;
                case '{': Current = new(Token.LBrace, i++); return;
                case '}': Current = new(Token.RBrace, i++); return;
                case '+': Current = new(Token.Plus, i++); return;
                case '*': Current = new(Token.Times, i++); return;
                case '/': Current = new(Token.Div, i++); return;
                case '%': Current = new(Token.Mod, i++); return;
                case '^': Current = new(Token.Caret, i++); return;
                case '\'': Current = new(Token.Transpose, i++); return;
                case '\\': Current = new(Token.Backslash, i++); return;
                case '-':
                    if (Add(ref c, ++i) == '-')
                    {
                        do ch = Add(ref c, ++i);
                        while (ch != '\r' && ch != '\n' && ch != '\0');
                        goto SKIP_BLANKS;
                    }
                    Current = new(Token.Minus, i - 1);
                    return;
                case '=':
                    Current = Add(ref c, ++i) == '>'
                        ? new(Token.Arrow, i++ - 1)
                        : new(Token.Eq, i - 1);
                    return;
                case '.':
                    Current = Add(ref c, ++i) == '*'
                        ? new(Token.PointTimes, i++ - 1)
                        : new(Token.Dot, i - 1);
                    return;
                case ':':
                    Current = Add(ref c, ++i) == ':'
                        ? new(Token.DoubleColon, i++ - 1)
                        : new(Token.Colon, i - 1);
                    return;
                case '!':
                    Current = Add(ref c, ++i) == '='
                        ? new(Token.Ne, i++ - 1)
                        : new(Token.Error, i - 1);
                    return;
                case '<':
                    Current = Add(ref c, ++i) == '='
                        ? new(Token.Le, i++ - 1)
                        : Add(ref c, i) == '>'
                        ? new(Token.Ne, i++ - 1)
                        : new(Token.Lt, i - 1);
                    return;
                case '>':
                    Current = Add(ref c, ++i) == '='
                        ? new(Token.Ge, i++ - 1)
                        : new(Token.Gt, i - 1);
                    return;
                case '"':
                    int start = i;
                    int first = ++i;
                    sb ??= new();
                    sb.Length = 0;
                MORE_STRING:
                    while (Add(ref c, i) != '"')
                        i++;
                    sb.Append(text.AsSpan()[first..i]);
                    if (Add(ref c, i + 1) == '"')
                    {
                        sb.Append('"');
                        i += 2;
                        first = i;
                        goto MORE_STRING;
                    }
                    Current = new(Token.Str, sb.ToString(), start);
                    i++;
                    return;
                default:
                    Current = new(Token.Error, i);
                    i = text.Length - 1;
                    return;
            }
    }

    /// <summary>Check AUSTRA keywords.</summary>
    /// <param name="c">Reference to first character.</param>
    /// <param name="len">Length of the identifier.</param>
    /// <returns>Token.Id, if not a keyword; otherwise, the corresponding keyword.</returns>
    private static Token IsIntelKeyword(ref char c, int len)
    {
        const uint kAnd = 'a' | ((uint)'n' << 16);
        const uint kDef = 'd' | ((uint)'e' << 16);
        const ulong kElse = 'e' | ((ulong)'l' << 16) | ((ulong)'s' << 32) | ((ulong)'e' << 48);
        const uint kIf = 'i' | ((uint)'f' << 16);
        const uint kIn = 'i' | ((uint)'n' << 16);
        const ulong kFalse = 'f' | ((ulong)'a' << 16) | ((ulong)'l' << 32) | ((ulong)'s' << 48);
        const uint kLet = 'l' | ((uint)'e' << 16);
        const uint kNot = 'n' | ((uint)'o' << 16);
        const uint kOr = 'o' | ((uint)'r' << 16);
        const uint kSet = 's' | ((uint)'e' << 16);
        const ulong kThen = 't' | ((ulong)'h' << 16) | ((ulong)'e' << 32) | ((ulong)'n' << 48);
        const ulong kTrue = 't' | ((ulong)'r' << 16) | ((ulong)'u' << 32) | ((ulong)'e' << 48);
        const ulong kUndef = 'u' | ((ulong)'n' << 16) | ((ulong)'d' << 32) | ((ulong)'e' << 48);

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
            if (month > 0)
                if (text.Length == 5 &&
                    int.TryParse(text[3..], out int year))
                {
                    date = new(2000 + year, month, 1);
                    Date top = Date.Today.AddYears(20);
                    if (date > top)
                        date = date.AddYears(-100);
                    return true;
                }
                else if (text.Length == 7 &&
                    int.TryParse(text[3..], out year))
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
        var (y, m, _) = date;
        return day <= Date.DaysInMonth(y, m) ? date + day - 1
            : throw new AstException("Invalid day of month", position);
    }

    /// <summary>Internal stub for accessing string internals.</summary>
    private sealed class Str
    {
#pragma warning disable CS0649
        public int Length;
#pragma warning restore CS0649
        public char FirstChar;
    }
}
