﻿using System.Runtime.Intrinsics.X86;
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

/// <summary>Syntactic and lexical analysis for AUSTRA.</summary>
internal sealed partial class Parser
{
    /// <summary>Gets the parameter referencing the outer scope.</summary>
    private static readonly ParameterExpression sourceParameter =
        Expression.Parameter(typeof(IDataSource), "datasource");

    /// <summary>Gets the outer scope for variables.</summary>
    private readonly IDataSource source;
    /// <summary>The text being scanned.</summary>
    private readonly string text;
    /// <summary>Referenced definitions.</summary>
    private readonly HashSet<Definition> references = new();
    /// <summary>Transient local variable definitions.</summary>
    private readonly Dictionary<string, ParameterExpression> locals =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Controls that only persisted values are used.</summary>
    private bool isParsingDefinition;
    /// <summary>Place holder for the first lambda parameter, if any.</summary>
    private ParameterExpression? lambdaParameter;
    /// <summary>Place holder for the second lambda parameter, if any.</summary>
    private ParameterExpression? lambdaParameter2;

    /// <summary>Memoized expressions.</summary>
    private readonly Dictionary<string, Expression> memos = new();
    /// <summary>Used by the scanner to build string literals.</summary>
    private StringBuilder? sb;
    /// <summary>Gets the type of the current lexeme.</summary>
    /// <remarks>Updated by the <see cref="Move"/> method.</remarks>
    private Token kind;
    /// <summary>Gets the start position of the current lexeme.</summary>
    /// <remarks>Updated by the <see cref="Move"/> method.</remarks>
    private int start;
    /// <summary>Gets the string associated with the current lexeme.</summary>
    /// <remarks>Updated by the <see cref="Move"/> method.</remarks>
    private string id;
    /// <summary>Value of the current lexeme as a real number.</summary>
    private double asReal;
    /// <summary>Value of the lexeme as an integer.</summary>
    private int asInt;
    /// <summary>Value of the lexeme as a date literal.</summary>
    private Date asDate;
    /// <summary>Current position in the text.</summary>
    /// <remarks>Updated by the <see cref="Move"/> method.</remarks>
    private int i;

    /// <summary>Initializes a parsing context.</summary>
    /// <param name="source">Environment variables.</param>
    /// <param name="text">Text of the formula.</param>
    public Parser(IDataSource source, string text)
    {
        (this.source, this.text, id) = (source, text, "");
        Move();
    }

    /// <summary>Name of the left side value, if any.</summary>
    public string LeftValue { get; set; } = "";

    /// <summary>Skips two tokens with a single call.</summary>
    private void Skip2() { Move(); Move(); }

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
            throw new AstException(errorMessage, start);
        Move();
    }

    /// <summary>Advances the lexical analyzer one token.</summary>
    private void Move()
    {
        ref char c = ref As<Str>(text).FirstChar;
    SKIP_BLANKS:
        while (char.IsWhiteSpace(Add(ref c, i)))
            i++;
        start = i;
        char ch = Add(ref c, i);
        // Check keywords, functors, class names and identifiers.
        if (char.IsLetter(ch))
        {
            do ch = Add(ref c, ++i);
            while (char.IsLetterOrDigit(ch) || ch == '_');
            // Check for keywords and function identifiers
            Token tok = Avx.IsSupported
                ? IsIntelKeyword(ref Add(ref c, start), i - start)
                : IsKeyword(text.AsSpan()[start..i]);
            if (tok != Token.Id)
                kind = tok;
            else
            {
                // Skip blanks after the identifier.
                id = text[start..i];
                while (char.IsWhiteSpace(Add(ref c, i)))
                    i++;
                ch = Add(ref c, i);
                kind = ch == '(' ? Token.Functor
                    : ch == ':' && Add(ref c, i + 1) == ':' ? Token.ClassName
                    : Token.Id;
            }
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
                kind = Token.Date;
                asDate = ParseDateLiteral(text.AsSpan()[start..i], start);
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
                    (kind, asReal) = (Token.Imag, ToReal(text, start, i++));
                else if (char.IsLetter(Add(ref c, i)) && IsVariableSuffix(text, i, out int j))
                    (kind, id, asReal, i) = (Token.MultVarR, text[i..j], ToReal(text, start, i), j);
                else
                    (kind, asReal) = (Token.Real, ToReal(text, start, i));
            }
            else if ((ch | 0x20) == 'e')
            {
                if (Add(ref c, ++i) is '+' or '-')
                    i++;
                while ((uint)(Add(ref c, i) - '0') < 10u)
                    i++;
                if (Add(ref c, i) == 'i' && !char.IsLetterOrDigit(Add(ref c, i + 1)))
                    (kind, asReal) = (Token.Imag, ToReal(text, start, i++));
                else if (char.IsLetter(Add(ref c, i)) && IsVariableSuffix(text, i, out int j))
                    (kind, id, asReal, i) = (Token.MultVarR, text[i..j], ToReal(text, start, i), j);
                else
                    (kind, asReal) = (Token.Real, ToReal(text, start, i));
            }
            else if (ch == 'i' && !char.IsLetterOrDigit(Add(ref c, i + 1)))
                (kind, asReal) = (Token.Imag, ToReal(text, start, i++));
            else if (char.IsLetter(ch) && IsVariableSuffix(text, i, out int k))
                (kind, id, asInt, i)
                    = (Token.MultVarI, text[i..k], int.Parse(text.AsSpan()[start..i]), k);
            else
                (kind, asInt) = (Token.Int, int.Parse(text.AsSpan()[start..i]));

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
                case '\'': kind = Token.Transpose; return;
                case '\\': kind = Token.Backslash; return;
                case '-':
                    if (Add(ref c, i) == '-')
                    {
                        do ch = Add(ref c, ++i);
                        while (ch is not '\r' and not '\n' and not '\0');
                        goto SKIP_BLANKS;
                    }
                    kind = Token.Minus;
                    return;
                case '=':
                    (kind, start) = Add(ref c, i) == '>'
                        ? (Token.Arrow, i++ - 1)
                        : (Token.Eq, start);
                    return;
                case '.':
                    (kind, start) = Add(ref c, i) == '*'
                        ? (Token.PointTimes, i++ - 1)
                        : Add(ref c, i) == '/'
                        ? (Token.PointDiv, i++ - 1)
                        : (Token.Dot, start);
                    return;
                case ':':
                    (kind, start) = Add(ref c, i) == ':'
                        ? (Token.DoubleColon, i++ - 1)
                        : (Token.Colon, start);
                    return;
                case '!':
                    (kind, start) = Add(ref c, i) == '='
                        ? (Token.Ne, i++ - 1)
                        : (Token.Error, start);
                    return;
                case '<':
                    (kind, start) = Add(ref c, i) == '='
                        ? (Token.Le, i++ - 1)
                        : Add(ref c, i) == '>'
                        ? (Token.Ne, i++ - 1)
                        : (Token.Lt, start);
                    return;
                case '>':
                    (kind, start) = Add(ref c, i) == '='
                        ? (Token.Ge, i++ - 1)
                        : (Token.Gt, start);
                    return;
                case '"':
                    int first = i--;
                    do
                    {
                        ch = Add(ref c, ++i);
                        if (ch == '\0')
                            throw new AstException("Unterminated string literal", start);
                    }
                    while (ch != '"');
                    if (Add(ref c, i + 1) != '"')
                    {
                        (kind, id) = (Token.Str, text[first..i++]);
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
                            throw new AstException("Unterminated string literal", start);
                    }
                    while (ch != '"');
                    sb.Append(text.AsSpan()[first..(i - 1)]);
                    if (Add(ref c, i) == '"')
                    {
                        sb.Append('"');
                        first = ++i;
                        goto MORE_STRING;
                    }
                    (kind, id) = (Token.Str, sb.ToString());
                    return;
                default:
                    kind = Token.Error;
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
        const ulong kAnd = 'A' | ('N' << 16) | ((ulong)'D' << 32);
        const ulong kDef = 'D' | ('E' << 16) | ((ulong)'F' << 32);
        const ulong kElse = 'e' | ('l' << 16) | ((ulong)'s' << 32) | ((ulong)'e' << 48);
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

    private static int TryGetMonth(ReadOnlySpan<char> name) => (name[0] | 0x20) switch
    {
        'a' =>
            (name[1] | 0x20) is 'p' && (name[2] | 0x20) is 'r' ? 4
            : (name[1] | 0x20) is 'u' && (name[2] | 0x20) is 'g' ? 8 : 0,
        'd' =>
            (name[1] | 0x20) is 'e' && (name[2] | 0x20) is 'c' ? 12 : 0,
        'f' =>
            (name[1] | 0x20) is 'e' && (name[2] | 0x20) is 'b' ? 2 : 0,
        'j' =>
            (name[1] | 0x20) is 'a' && (name[2] | 0x20) is 'n' ? 1
            : (name[1] | 0x20) is not 'u' ? 0
            : (name[2] | 0x20) is 'n' ? 6
            : (name[2] | 0x20) is 'l' ? 7 : 0,
        'm' =>
            (name[1] | 0x20) is not 'a' ? 0
            : (name[2] | 0x20) is 'r' ? 3 : (name[2] | 0x20) is 'y' ? 5 : 0,
        'n' =>
            (name[1] | 0x20) is 'o' && (name[2] | 0x20) is 'v' ? 11 : 0,
        'o' =>
            (name[1] | 0x20) is 'c' && (name[2] | 0x20) is 't' ? 10 : 0,
        's' =>
            (name[1] | 0x20) is 'e' && (name[2] | 0x20) is 'p' ? 9 : 0,
        _ => 0,
    };

    /// <summary>Parses date constants like <c>jan2020</c> and <c>dec20</c>.</summary>
    /// <param name="text">Text span to analyze.</param>
    /// <param name="date">When succeeds, returns the first date of the month.</param>
    /// <returns><see langword="true"/> if succeeds.</returns>
    private static bool TryParseMonthYear(ReadOnlySpan<char> text, out Date date)
    {
        if (text.Length >= 5)
        {
            int month = TryGetMonth(text);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsArithmetic(Expression e1) =>
        e1.Type == typeof(int) || e1.Type == typeof(double);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AreArithmeticTypes(Expression e1, Expression e2) =>
        IsArithmetic(e1) && IsArithmetic(e2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsVectorOrMatrix(Expression e1) =>
        e1.Type == typeof(Vector) || IsMatrix(e1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMatrix(Expression e1) =>
        e1.Type.IsAssignableTo(typeof(IMatrix));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Expression ToDouble(Expression e) =>
        e.Type != typeof(int)
        ? e
        : e is ConstantExpression constExpr
        ? Expression.Constant((double)(int)constExpr.Value!, typeof(double))
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
                if (!AreArithmeticTypes(e1, e2))
                    return true;
                (e1, e2) = (ToDouble(e1), ToDouble(e2));
            }
        }
        return false;
    }

    private static AstException Error(string message, int position) =>
        new(message, position);

    private AstException Error(string message) =>
        new(message, start);

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

/// <summary>Contains extension methods acting on <see cref="Type"/> instances.</summary>
internal static class ParserExtensions
{
    /// <summary>Avoids repeating the bang operator (!) in the code.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo Get(this Type type, string method) =>
         type.GetMethod(method)!;

    /// <summary>Gets the property getter method for the specified property.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo Prop(this Type type, string property) =>
         type.GetProperty(property)!.GetGetMethod()!;

    /// <summary>
    /// Gets the constructor for the specified types and creates a new expression.
    /// </summary>
    public static NewExpression New(this Type type, params Expression[] args) =>
        Expression.New(type.GetConstructor(args.Select(a => a.Type).ToArray())!, args);

    public static NewExpression New(this Type type, List<Expression> args) =>
        Expression.New(type.GetConstructor(args.Select(a => a.Type).ToArray())!, args);

    public static NewArrayExpression Make(this Type type, IEnumerable<Expression> args) =>
        Expression.NewArrayInit(type, args);

    public static MethodCallExpression Call(this Type type,
        Expression? instance, string method, Expression arg) =>
        Expression.Call(instance, type.GetMethod(method, new[] { arg.Type })!, arg);

    public static MethodCallExpression Call(this Type type,
        string method, Expression a1, Expression a2) =>
        Expression.Call(type.GetMethod(method, new[] { a1.Type, a2.Type })!, a1, a2);

    public static List<Expression> AddExp(this List<Expression> args, Expression exp)
    {
        args.Add(exp);
        return args;
    }

    public static List<Expression> AddRandom(this List<Expression> args) =>
        args.AddExp(Expression.New(typeof(Random).GetConstructor(Array.Empty<Type>())!));

    public static List<Expression> AddNormalRandom(this List<Expression> args) =>
        args.AddExp(Expression.New(typeof(NormalRandom).GetConstructor(Array.Empty<Type>())!));
}
