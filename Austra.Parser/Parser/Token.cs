namespace Austra.Parser;

/// <summary>Lexical token kinds.</summary>
public enum Token
{
    /// <summary>Austra identifiers.</summary>
    Id,
    /// <summary>String literals.</summary>
    Str,
    /// <summary>Integer literals.</summary>
    Int,
    /// <summary>Real literals.</summary>
    Real,
    /// <summary>Imaginary literals, suffixed with <c>i</c>.</summary>
    Imag,
    /// <summary>Date literal.</summary>
    Date,
    /// <summary>Keyword <c>def</c> for defining functions.</summary>
    Def,
    /// <summary>Keyword <c>undef</c> for undefining functions.</summary>
    Undef,
    /// <summary>Keyword <c>let</c> for defining temporal variables.</summary>
    Let,
    /// <summary>Keyword <c>set</c> for defining session variables.</summary>
    Set,
    /// <summary>Keyword <c>and</c> for logical conjunctions.</summary>
    And,
    /// <summary>Keyword <c>or</c> for logical disjunctions.</summary>
    Or,
    /// <summary>Keyword <c>not</c> for logical negation.</summary>
    Not,
    /// <summary>Keyword <c>if</c> for conditional expressions.</summary>
    If,
    /// <summary>Keyword <c>then</c> for conditional expressions.</summary>
    Then,
    /// <summary>Keyword <c>else</c> for conditional expressions.</summary>
    Else,
    /// <summary>Keyword <c>in</c> for <c>let</c> clauses.</summary>
    In,
    /// <summary>Keyword <c>true</c>, as a boolean constant.</summary>
    True,
    /// <summary>Keyword <c>false</c>, as a boolean constant.</summary>
    False,
    /// <summary>An identifier that is a function name, preceding a left parenthesis.</summary>
    Functor,
    /// <summary>An identifier that is a class name, preceding two consecutive colons.</summary>
    ClassName,
    /// <summary>A dot, as a member accessor.</summary>
    Dot,
    /// <summary>The comma separator.</summary>
    Comma,
    /// <summary>Colon, as separator for slices.</summary>
    Colon,
    /// <summary>Semicolon, as a row separator in matrix literals.</summary>
    Semicolon,
    /// <summary>Two consecutive colons, as the scope resolution operator.</summary>
    DoubleColon,
    /// <summary>A single quote, as the transpose operator.</summary>
    Transpose,
    /// <summary>Left parenthesis.</summary>
    LPar,
    /// <summary>Right parenthesis.</summary>
    RPar,
    /// <summary>Left bracket.</summary>
    LBra,
    /// <summary>Right bracket.</summary>
    RBra,
    /// <summary>Left brace.</summary>
    LBrace,
    /// <summary>Right brace.</summary>
    RBrace,
    /// <summary>The plus <c>+</c> operator.</summary>
    Plus,
    /// <summary>The minus <c>-</c> operator.</summary>
    Minus,
    /// <summary>The times <c>*</c> operator.</summary>
    Times,
    /// <summary>The pointwise multiplication.</summary>
    PointTimes,
    /// <summary>The pointwise division.</summary>
    PointDiv,
    /// <summary>The backslash \ is the matrix solver operator.</summary>
    Backslash,
    /// <summary>The division <c>/</c> operator.</summary>
    Div,
    /// <summary>The module <c>%</c> operator.</summary>
    Mod,
    /// <summary>The caret ^ is the power operator.</summary>
    Caret,
    /// <summary>The equal sign, doubling as the assignment operator.</summary>
    Eq,
    /// <summary>The non-equal sign.</summary>
    Ne,
    /// <summary>The lesser-than operator.</summary>
    Lt,
    /// <summary>The greater-than operator.</summary>
    Gt,
    /// <summary>The lesser-or-equal operator.</summary>
    Le,
    /// <summary>The greater-or-equal operator.</summary>
    Ge,
    /// <summary>The arrow =&gt;, for defining lambdas function.</summary>
    Arrow,
    /// <summary>A multiplied variable.</summary>
    /// <remarks>A juxtaposed pair of a real number and an identifier.</remarks>
    MultVarR,
    /// <summary>A multiplied variable.</summary>
    /// <remarks>A juxtaposed pair of an integer number and an identifier.</remarks>
    MultVarI,
    /// <summary>An unrecognized token.</summary>
    Error,
    /// <summary>End of input.</summary>
    Eof
}
