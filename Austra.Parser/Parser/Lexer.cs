using System.Runtime.InteropServices;

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
internal static class Lexer
{
    /// <summary>Decomposes a rule into a stream of tokens.</summary>
    /// <param name="text">Text to analyze.</param>
    /// <returns>A stream of lexemes.</returns>
    public static IEnumerable<Lexeme> Lex(string text)
    {
        text += '\0'; // Add a sentinel
        StringBuilder sb = new();
        for (int i = 0; ;)
        {
            // Skip blanks.
            while (char.IsWhiteSpace(text, i))
                i++;
            if (char.IsLetter(text, i))
            {
                int first = i;
                do i++;
                while (char.IsLetterOrDigit(text, i) || text[i] == '_');
                int last = i;
                // Skip blanks once again
                while (char.IsWhiteSpace(text, i))
                    i++;
                // Check for keywords and function identifiers
                ReadOnlySpan<char> s = text.AsSpan()[first..last];
                Token tok = IsKeyword(s);
                if (tok != Token.Id)
                    yield return new(tok, first);
                else if (text[i] == '(')
                    yield return new(Token.Functor, s.ToString(), first);
                else if (text[i] == ':' && text[i + 1] == ':')
                    yield return new(Token.ClassName, s.ToString(), first);
                else
                    yield return new(Token.Id, s.ToString(), first);
            }
            else if (char.IsDigit(text, i))
            {
                int first = i;
                do i++;
                while (char.IsDigit(text, i));
                if (text[i] == '@')
                {
                    do i++;
                    while (char.IsLetterOrDigit(text, i));
                    yield return new(Token.Date, first)
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
                else if (text[i] == '.')
                {
                    do i++;
                    while (char.IsDigit(text, i));
                    if (text[i] is 'E' or 'e')
                    {
                        i++;
                        if (text[i] is '+' or '-')
                            i++;
                        while (char.IsDigit(text, i))
                            i++;
                    }
                    if (IsImaginarySuffix())
                        yield return new(Token.Imag, first) { AsReal = AsReal(i++) };
                    else if (IsVariableSuffix(out int j))
                    {
                        yield return new(Token.MultVarR, text[i..j], first) { AsReal = AsReal(i) };
                        i = j;
                    }
                    else
                        yield return new(Token.Real, first) { AsReal = AsReal(i) };
                }
                else if (text[i] is 'E' or 'e')
                {
                    if (text[++i] is '+' or '-')
                        i++;
                    while (char.IsDigit(text, i))
                        i++;
                    if (IsImaginarySuffix())
                        yield return new(Token.Imag, first) { AsReal = AsReal(i++) };
                    else if (IsVariableSuffix(out int j))
                    {
                        yield return new(Token.MultVarR, text[i..j], first) { AsReal = AsReal(i) };
                        i = j;
                    }
                    else
                        yield return new(Token.Real, first) { AsReal = AsReal(i) };
                }
                else if (IsImaginarySuffix())
                    yield return new(Token.Imag, first) { AsReal = AsReal(i++) };
                else if (IsVariableSuffix(out int j))
                {
                    yield return new(Token.MultVarI, text[i..j], first) { AsInt = AsInt(i) };
                    i = j;
                }
                else
                    yield return new(Token.Int, first) { AsInt = AsInt(i) };

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                bool IsImaginarySuffix() =>
                    text[i] == 'i' && !char.IsLetterOrDigit(text, i + 1);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                bool IsVariableSuffix(out int j)
                {
                    j = i;
                    if (!char.IsLetter(text, i))
                        return false;
                    do j++;
                    while (char.IsLetterOrDigit(text, j) || text[j] == '_');
                    return IsKeyword(text.AsSpan()[i..j]) == Token.Id;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                int AsInt(int to) => int.Parse(text.AsSpan()[first..to]);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                double AsReal(int to) =>
                    double.Parse(text.AsSpan()[first..to], CultureInfo.InvariantCulture);
            }
            else
                switch (text[i])
                {
                    case '\0':
                        // Acknowledge end of expression.
                        yield return new(Token.Eof, text.Length - 1);
                        yield break;
                    case ',': yield return new(Token.Comma, i++); break;
                    case ';': yield return new(Token.Semicolon, i++); break;
                    case '(': yield return new(Token.LPar, i++); break;
                    case ')': yield return new(Token.RPar, i++); break;
                    case '[': yield return new(Token.LBra, i++); break;
                    case ']': yield return new(Token.RBra, i++); break;
                    case '{': yield return new(Token.LBrace, i++); break;
                    case '}': yield return new(Token.RBrace, i++); break;
                    case '+': yield return new(Token.Plus, i++); break;
                    case '*': yield return new(Token.Times, i++); break;
                    case '/': yield return new(Token.Div, i++); break;
                    case '%': yield return new(Token.Mod, i++); break;
                    case '^': yield return new(Token.Caret, i++); break;
                    case '\'': yield return new(Token.Transpose, i++); break;
                    case '\\': yield return new(Token.Backslash, i++); break;
                    case '-':
                        if (++i < text.Length && text[i] == '-')
                        {
                            do i++;
                            while (i < text.Length && text[i] != '\r' && text[i] != '\n');
                            if (i < text.Length && text[i] == '\r')
                                i++;
                            if (i < text.Length && text[i] == '\n')
                                i++;
                            continue;
                        }
                        else
                            yield return new(Token.Minus, i - 1); break;
                    case '=':
                        if (text[++i] == '>')
                            yield return new(Token.Arrow, i++ - 1);
                        else
                            yield return new(Token.Eq, i - 1); break;
                    case '.':
                        if (text[++i] == '*')
                            yield return new(Token.PointTimes, i++ - 1);
                        else
                            yield return new(Token.Dot, i - 1);
                        break;
                    case ':':
                        if (text[++i] == ':')
                            yield return new(Token.DoubleColon, i++ - 1);
                        else
                            yield return new(Token.Colon, i - 1);
                        break;
                    case '!':
                        if (text[++i] == '=')
                            yield return new(Token.Ne, i++ - 1);
                        else
                            yield return new(Token.Error, i - 1);
                        break;
                    case '<':
                        if (text[++i] == '=')
                            yield return new(Token.Le, i++ - 1);
                        else if (text[i] == '>')
                            yield return new(Token.Ne, i++ - 1);
                        else
                            yield return new(Token.Lt, i - 1);
                        break;
                    case '>':
                        if (text[++i] == '=')
                            yield return new(Token.Ge, i++ - 1);
                        else
                            yield return new(Token.Gt, i - 1);
                        break;
                    case '"':
                        int start = i;
                        int first = ++i;
                        sb.Length = 0;
                    MORE_STRING:
                        while (text[i] != '"')
                            i++;
                        sb.Append(text.AsSpan()[first..i]);
                        if (text[i + 1] == '"')
                        {
                            sb.Append('"');
                            i += 2;
                            first = i;
                            goto MORE_STRING;
                        }
                        yield return new(Token.Str, sb.ToString(), start);
                        i++;
                        break;
                    default:
                        yield return new(Token.Error, i);
                        yield break;
                }
        }
    }

    /// <summary>Check AUSTRA keywords.</summary>
    /// <param name="text">Text span to check.</param>
    /// <returns>Token.Id, if not a keyword; otherwise, the corresponding keyword.</returns>
    private static Token IsKeyword(ReadOnlySpan<char> text) => text[0] switch
    {
        'a' or 'A' =>
            text.Length == 3 && text[1] is 'n' or 'N' && text[2] is 'd' or 'D'
                ? Token.And : Token.Id,
        'd' or 'D' =>
            text.Length == 3 && text[1] is 'e' or 'E' && text[2] is 'f' or 'F'
                ? Token.Def : Token.Id,
        'e' or 'E' =>
            text[1..].Equals("lse", StringComparison.OrdinalIgnoreCase) ? Token.Else : Token.Id,
        'f' or 'F' =>
            text[1..].Equals("alse", StringComparison.OrdinalIgnoreCase) ? Token.False : Token.Id,
        'i' or 'I' =>
            text.Length != 2 ? Token.Id :
            text[1] is 'f' or 'F' ? Token.If :
            text[1] is 'n' or 'N' ? Token.In : Token.Id,
        'l' or 'L' =>
            text.Length == 3 && text[1] is 'e' or 'E' && text[2] is 't' or 'T'
                ? Token.Let : Token.Id,
        'n' or 'N' =>
            text.Length == 3 && text[1] is 'o' or 'O' && text[2] is 't' or 'T'
                ? Token.Not : Token.Id,
        'o' or 'O' =>
            text.Length == 2 && text[1] is 'r' or 'R' ? Token.Or : Token.Id,
        's' or 'S' =>
            text.Length == 3 && text[1] is 'e' or 'E' && text[2] is 't' or 'T'
                ? Token.Set : Token.Id,
        't' or 'T' =>
            text[1..].Equals("hen", StringComparison.OrdinalIgnoreCase) ? Token.Then :
            text[1..].Equals("rue", StringComparison.OrdinalIgnoreCase) ? Token.True : Token.Id,
        'u' or 'U' =>
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
}