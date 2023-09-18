using System.Runtime.InteropServices;
using static System.Runtime.CompilerServices.Unsafe;

namespace Austra.Parser;

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

    [StructLayout(LayoutKind.Explicit)]
    private struct LexValue
    {
        [FieldOffset(0)] public double AsReal;
        [FieldOffset(0)] public int AsInt;
        [FieldOffset(0)] public Date AsDate;
    }
}

/// <summary>The Lexical Analyzer.</summary>
internal sealed class Lexer
{
    private readonly string text;
    private StringBuilder? sb;
    private int i;

    public Lexer(string text) => this.text = text;

    public Lexeme Current { get; private set; }

    public void Move()
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
            do i++;
            while (char.IsLetterOrDigit(Add(ref c, i)) || Add(ref c, i) == '_');
            // Check for keywords and function identifiers
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
            return;
        }
        if (char.IsDigit(ch))
        {
            int first = i;
            do i++;
            while (char.IsDigit(Add(ref c, i)));
            ch = Add(ref c, i);
            if (ch == '@')
            {
                do i++;
                while (char.IsLetterOrDigit(Add(ref c, i)));
                Current = new(Token.Date, first)
                {
                    AsDate = ParseDateLiteral(text.AsSpan()[first..i], first)
                };

                static Date ParseDateLiteral(ReadOnlySpan<char> text, int position)
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
            }
            else if (ch == '.')
            {
                do i++;
                while (char.IsDigit(Add(ref c, i)));
                if (Add(ref c, i) is 'E' or 'e')
                {
                    i++;
                    if (Add(ref c, i) is '+' or '-')
                        i++;
                    while (char.IsDigit(Add(ref c, i)))
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
                while (char.IsDigit(Add(ref c, i)))
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
                Current = new(Token.MultVarI, text[i..k], first) { AsInt = AsInt(text, first, i) };
                i = k;
            }
            else
                Current = new(Token.Int, first) { AsInt = AsInt(text, first, i) };
            return;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsVariableSuffix(string text, int i, out int j)
            {
                j = i;
                do j++;
                while (char.IsLetterOrDigit(text, j) || text[j] == '_');
                return IsKeyword(text.AsSpan()[i..j]) == Token.Id;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static double AsReal(string text, int from, int to) =>
                double.Parse(text.AsSpan()[from..to], CultureInfo.InvariantCulture);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int AsInt(string text, int from, int to) =>
                int.Parse(text.AsSpan()[from..to]);
        }
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
    /// <param name="text">Text span to check.</param>
    /// <returns>Token.Id, if not a keyword; otherwise, the corresponding keyword.</returns>
    private static Token IsKeyword(ReadOnlySpan<char> text) => (text[0] | 0x20) switch
    {
        'a' =>
            text.Length == 3 && (text[1] | 0x20) is 'n' && (text[2] | 0x20) is 'd'
                ? Token.And : Token.Id,
        'd' =>
            text.Length == 3 && (text[1] | 0x20) is 'e' && (text[2] | 0x20) is 'f'
                ? Token.Def : Token.Id,
        'e' =>
            text[1..].Equals("lse", StringComparison.OrdinalIgnoreCase) ? Token.Else : Token.Id,
        'f' =>
            text[1..].Equals("alse", StringComparison.OrdinalIgnoreCase) ? Token.False : Token.Id,
        'i' =>
            text.Length != 2 ? Token.Id :
            (text[1] | 0x20) is 'f' ? Token.If :
            (text[1] | 0x20) is 'n' ? Token.In : Token.Id,
        'l' =>
            text.Length == 3 && (text[1] | 0x20) is 'e' && (text[2] | 0x20) is 't'
                ? Token.Let : Token.Id,
        'n' =>
            text.Length == 3 && text[1] is 'o' or 'O' && text[2] is 't' or 'T'
                ? Token.Not : Token.Id,
        'o' =>
            text.Length == 2 && (text[1] | 0x20) is 'r' ? Token.Or : Token.Id,
        's' =>
            text.Length == 3 && text[1] is 'e' or 'E' && text[2] is 't' or 'T'
                ? Token.Set : Token.Id,
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

    internal static bool TryParseMonthYear(ReadOnlySpan<char> text, out Date date)
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

    private sealed class Str
    {
#pragma warning disable CS0649
        public int Length;
#pragma warning restore CS0649
        public char FirstChar;
    }
}
