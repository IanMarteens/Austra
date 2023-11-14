using System.IO;
using System.Text.Json;

namespace Austra.Parser;

/// <summary>Defines an answer from the AUSTRA engine.</summary>
/// <param name="Value">The returned value.</param>
/// <param name="Type">The type of the returned value.</param>
/// <param name="Variable">If a variable has been defined, this is its name.</param>
public readonly record struct AustraAnswer(object? Value, Type? Type, string Variable);

/// <summary>Represents a member with its description for code completion.</summary>
/// <param name="Name">The text of the member.</param>
/// <param name="Description">A human-readable description.</param>
public readonly record struct Member(string Name, string Description);

/// <summary>Represents the AUSTRA engine.</summary>
public interface IAustraEngine
{
    /// <summary>Parses and evaluates an AUSTRA formula.</summary>
    /// <param name="formula">Any acceptable text for an AUSTRA formula.</param>
    /// <returns>The result of the evaluation.</returns>
    AustraAnswer Eval(string formula);

    /// <summary>Parses an AUSTRA formula and returns its type.</summary>
    /// <remarks>No code is generated.</remarks>
    /// <param name="formula">Any acceptable text for an AUSTRA formula.</param>
    /// <returns>The type resulting from the evaluation.</returns>
    Type EvalType(string formula);

    /// <summary>Validates a definition (no SET clause) and returns its type.</summary>
    /// <param name="definition">An AUSTRA definition.</param>
    /// <param name="description">Textual description.</param>
    /// <returns>The type resulting from the evaluation.</returns>
    Definition ParseDefinition(string definition, string description);

    /// <summary>The data source associated with the engine.</summary>
    IDataSource Source { get; }

    /// <summary>Gets a list of root variables.</summary>
    /// <param name="position">The position of the cursor.</param>
    /// <param name="text">The text of the expression.</param>
    /// <returns>A list of variables and definitions.</returns>
    IList<Member> GetRoots(int position, string text);

    /// <summary>Gets a list of root classes.</summary>
    /// <returns>A list of classes that accepts class methods.</returns>
    IList<Member> GetRootClasses();

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>An empty list, if not a valid type.</returns>
    IList<Member> GetMembers(string text);

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <param name="type">The type of the expression fragment.</param>
    /// <returns>An empty list, if not a valid type.</returns>
    IList<Member> GetMembers(string text, out Type? type);

    /// <summary>Gets a list of class members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>Null if not a valid type.</returns>
    IList<Member> GetClassMembers(string text);

    /// <summary>Checks if the name is a valid class accepting class methods.</summary>
    /// <param name="text">Class name to check.</param>
    /// <returns><see langword="true"/> if the text is a valid class name.</returns>
    bool IsClass(string text);

    /// <summary>Loads series and definitions into the data source.</summary>
    void Load();

    /// <summary>
    /// Clears the data source and reloads series and definitions from persistent storage.
    /// </summary>
    public void Reload()
    {
        Source.ClearAll();
        Load();
    }

    /// <summary>Saves a definition into the persistent storage.</summary>
    /// <param name="definition">A macro definition.</param>
    void Define(Definition definition);

    /// <summary>Deletes a list of definitions from the persistent storage.</summary>
    /// <param name="definitions">Definitions to be deleted.</param>
    void Undefine(IList<string> definitions);

    /// <summary>Gets the execution time, in nanoseconds, of the last evaluation.</summary>
    double? ExecutionTime { get; }
    /// <summary>Gets the code generation time, in nanoseconds, of the last evaluation.</summary>
    double? GenerationTime { get; }
    /// <summary>Gets the compiling time, in nanoseconds, of the last evaluation.</summary>
    double? CompileTime { get; }

    /// <summary>Formats a time in nanoseconds into a string with the appropriate unit.</summary>
    /// <param name="time">Time in nanoseconds.</param>
    /// <returns>Time with the appropriate unit.</returns>
    public string FormatTime(double time) =>
        time > 1E6
        ? $"{time * 1E-6:N0} ms"
        : time > 1E3
        ? $"{time * 1E-3:N0} μs"
        : $"{time:N0} ns";

    /// <summary>Serializes the datasource into a byte array.</summary>
    /// <returns>An UTF-8 representation of series and definitions.</returns>
    byte[] Serialize();

    /// <summary>Serializes the datasource into an UTF-8 file.</summary>
    /// <param name="fileName">The name of the output file.</param>
    public void Serialize(string fileName) =>
        File.WriteAllBytes(fileName, Serialize());

    /// <summary>Deserializes a datasource from an UTF-8 file.</summary>
    /// <param name="fileName">An UTF-8 file previously serialized.</param>
    /// <returns>An object implementing this interface.</returns>
    public static abstract IAustraEngine Deserialize(string fileName);
}

/// <summary>The simplest implementation for the AUSTRA engine.</summary>
/// <remarks>It does not supports persistency.</remarks>
public partial class AustraEngine : IAustraEngine
{
    private static readonly Member[] rootClasses =
    [
        new("complexvector::", "Allows access to complex vector constructors"),
        new("matrix::", "Allows access to matrix constructors"),
        new("model::", "Allows access to model constructors"),
        new("series::", "Allows access to series constructors"),
        new("spline::", "Allows access to spline constructors"),
        new("vector::", "Allows access to vector constructors"),
        new("seq::", "Allows access to sequence constructors"),
        new("math::", "Allows access to mathematical functions"),
    ];

    private static readonly Member[] globalFunctions =
    [
        new("abs(", "Absolute value"),
        new("sqrt(", "Squared root"),
        new("gamma(", "The Gamma function"),
        new("beta(", "The Beta function"),
        new("erf(", "Error function"),
        new("ncdf(", "Normal cummulative function"),
        new("probit(", "Probit function"),
        new("log(", "Natural logarithm"),
        new("log10(", "Base 10 logarithm"),
        new("exp(", "Exponential function"),
        new("sin(", "Sine function"),
        new("cos(", "Cosine function"),
        new("tan(", "Tangent function"),
        new("asin(", "Arcsine function"),
        new("acos(", "Arccosine function"),
        new("atan(", "Arctangent function"),
        new("min(", "Minimum function"),
        new("max(", "Maximum function"),
        new("round(", "Rounds a real value"),
        new("pi", "The constant π"),
        new("e", "The constant e"),
        new("i", "The imaginary unit"),
        new("today", "The current date"),
        new("compare(", "Compares two series or vectors"),
        new("polyEval(", "Evaluates a polynomial at a given point"),
        new("polyDerivative(", "Evaluates a polynomial first derivative at a given point"),
        new("polySolve(", "Returns all real and complex roots of a polynomial"),
        new ("solve(", "Approximates a root with the Newton-Raphson algorithm"),
        new("complex(", "Creates a complex number from its real and imaginary components"),
        new("polar(", "Creates a complex number from its magnitude and phase components"),
        new("set", "Assigns a value to a variable"),
        new("let", "Declares local variables"),
    ];

    private readonly Member[] classesAndGlobals;

    /// <summary>Creates an evaluation engine from a datasource.</summary>
    /// <param name="source">A scope for variables.</param>
    public AustraEngine(IDataSource source)
    {
        Source = source;
        classesAndGlobals = [.. rootClasses, .. globalFunctions];
    }

    /// <summary>The data source associated with the engine.</summary>
    public IDataSource Source { get; }

    /// <summary>Parses and evaluates an AUSTRA formula.</summary>
    /// <param name="formula">Any acceptable text for an AUSTRA formula.</param>
    /// <returns>The result of the evaluation.</returns>
    public AustraAnswer Eval(string formula)
    {
        ExecutionTime = GenerationTime = CompileTime = null;
        if (DefineRegex().IsMatch(formula))
        {
            Definition def = ParseDefinition(formula, "");
            Define(def);
            return new(def, def.GetType(), def.Name);
        }

        Match match = UndefineRegex().Match(formula);
        if (match.Success)
        {
            string name = match.Groups["name"].Value;
            IList<string> dList = Source.DeleteDefinition(name);
            if (dList.Count == 0)
                throw new Exception($"Definition {name} not found.");
            Undefine(dList);
            return new(dList, dList.GetType(), name);
        }

        Parser parser = new(Source, formula);
        Stopwatch sw = Stopwatch.StartNew();
        Expression<Func<IDataSource, object>> expression =
            Source.CreateLambda(parser.ParseStatement());
        sw.Stop();
        CompileTime = sw.ElapsedTicks * 1E9 / Stopwatch.Frequency;
        sw.Restart();
        Func<IDataSource, object> lambda = expression.Compile();
        sw.Stop();
        GenerationTime = sw.ElapsedTicks * 1E9 / Stopwatch.Frequency;
        sw.Restart();
        object answer = lambda(Source);
        sw.Stop();
        ExecutionTime = sw.ElapsedTicks * 1E9 / Stopwatch.Frequency;
        Type? lastType = null;
        if (answer != null)
        {
            Source["ans"] = answer;
            lastType = answer.GetType();
        }
        return new(answer, lastType, parser.LeftValue);
    }

    /// <summary>Parses an AUSTRA formula and returns its type.</summary>
    /// <remarks>No code is generated.</remarks>
    /// <param name="formula">Any acceptable text for an AUSTRA formula.</param>
    /// <returns>The type resulting from the evaluation.</returns>
    public Type EvalType(string formula)
    {
        ExecutionTime = GenerationTime = CompileTime = null;
        Stopwatch sw = Stopwatch.StartNew();
        Type result = new Parser(Source, formula).ParseType();
        sw.Stop();
        CompileTime = sw.ElapsedTicks * 1E9 / Stopwatch.Frequency;
        return result;
    }

    /// <summary>Validates a definition (no SET clause) and returns its type.</summary>
    /// <param name="definition">An AUSTRA definition.</param>
    /// <param name="description">Textual description.</param>
    /// <returns>The type resulting from the evaluation.</returns>
    public Definition ParseDefinition(string definition, string description)
    {
        Definition def = new Parser(Source, definition).ParseDefinition(description);
        Source.AddDefinition(def);
        return def;
    }

    /// <summary>Gets a list of root variables.</summary>
    /// <param name="position">The position of the cursor.</param>
    /// <param name="text">The text of the expression.</param>
    /// <returns>A list of variables and definitions.</returns>
    public IList<Member> GetRoots(int position, string text)
    {
        if (position > 0)
        {
            List<Member> newMembers = new Parser(Source, text).ParseContext(position, out bool parsingHeader);
            return parsingHeader
                ? newMembers
                : [
                    .. newMembers
                        .Concat(Source.Variables
                            .Select(t => new Member(t.name, "Variable: " + t.type?.Name)))
                        .Concat(Source.AllDefinitions
                            .Select(d => new Member(d.Name, "Definition: " + d.Type.Name)))
                        .Concat(classesAndGlobals)
                        .OrderBy(x => x.Name)
                ];
        }
        return Source.Variables
                .Select(t => new Member(t.name, "Variable: " + t.type?.Name))
            .Concat(Source.AllDefinitions
                .Select(d => new Member(d.Name, "Definition: " + d.Type.Name)))
            .Concat(classesAndGlobals)
            .OrderBy(x => x.Name)
            .ToList();
    }

    private IList<Member> GetGlobalFunctions() => globalFunctions;

    /// <summary>Gets a list of root classes.</summary>
    /// <returns>A list of classes that accepts class methods.</returns>
    public IList<Member> GetRootClasses() => rootClasses;

    /// <summary>Checks if the name is a valid class accepting class methods.</summary>
    /// <param name="text">Class name to check.</param>
    /// <returns><see langword="true"/> if the text is a valid class name.</returns>
    public bool IsClass(string text) => Parser.IsClassName(text);

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>An empty list, if not a valid type.</returns>
    public IList<Member> GetMembers(string text) =>
        Parser.GetMembers(Source, text, out _);

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <param name="type">The type of the expression fragment.</param>
    /// <returns>An empty list, if not a valid type.</returns>
    public IList<Member> GetMembers(string text, out Type? type) =>
        Parser.GetMembers(Source, text, out type);

    /// <summary>Gets a list of class members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>Null if not a valid type.</returns>
    public IList<Member> GetClassMembers(string text) =>
        Parser.GetClassMembers(text);

    /// <summary>Should load series and definitions from persistent storage.</summary>
    public virtual void Load() { }

    /// <summary>Should save a definition into the persistent storage.</summary>
    /// <param name="definition">A macro definition.</param>
    public virtual void Define(Definition definition) { }

    /// <summary>Should delete a list of definitions from the persistent storage.</summary>
    /// <param name="definitions">Definitions to be deleted.</param>
    public virtual void Undefine(IList<string> definitions) { }

    /// <summary>Gets the execution time, in nanoseconds, of the last evaluation.</summary>
    public double? ExecutionTime { get; private set; }

    /// <summary>Gets the code generation time, in nanoseconds, of the last evaluation.</summary>
    public double? GenerationTime { get; private set; }

    /// <summary>Gets the compiling time, in nanoseconds, of the last evaluation.</summary>
    public double? CompileTime { get; private set; }

    /// <summary>DTO for serializing a definition.</summary>
    /// <param name="Name">The name of the definition.</param>
    /// <param name="Text">The formula of the definition.</param>
    /// <param name="Description">A textual description.</param>
    protected sealed record DataDef(string Name, string Text, string Description);

    /// <summary>DTO for serializing a series.</summary>
    /// <param name="Name">A name for the series.</param>
    /// <param name="Ticker">Optional external name of the series.</param>
    /// <param name="Type">Type of the series.</param>
    /// <param name="Frequency">Sampling frequency.</param>
    /// <param name="Dates">Date arguments for the abscissa.</param>
    /// <param name="Values">Numerical arguments for the coordinates.</param>
    protected sealed record DataSeries(
        string Name, string? Ticker, int Type, int Frequency,
        Date[] Dates, double[] Values);

    /// <summary>DTO for serializing a whole datasource.</summary>
    /// <param name="Definitions">A list of definitions.</param>
    /// <param name="Series">A list of series.</param>
    protected sealed record DataObject(List<DataDef> Definitions, List<DataSeries> Series);

    /// <summary>Gets a regex for the DEFINE clause.</summary>
    /// <returns>A compiler-generated regular expression.</returns>
    [GeneratedRegex("^\\s*def\\s*(?'name'[\\w]+)\\s*", RegexOptions.IgnoreCase)]
    private static partial Regex DefineRegex();

    /// <summary>Gets a regex for the UNDEFINE clause.</summary>
    /// <returns>A compiler-generated regular expression.</returns>
    [GeneratedRegex("^\\s*undef\\s*(?'name'[\\w]+)\\s*", RegexOptions.IgnoreCase)]
    private static partial Regex UndefineRegex();

    /// <summary>Serializes the datasource into a byte array.</summary>
    /// <returns>An UTF-8 representation of series and definitions.</returns>
    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(
        new DataObject(
            Source.AllDefinitions.Select(d => new DataDef(d.Name, d.Text, d.Description)).ToList(),
            Source.Series.Select(s =>
                new DataSeries(s.Name, s.Ticker, (int)s.Type, (int)s.Freq, s.Args.ToArray(),
                    s.Values.ToArray())).ToList()));

    /// <summary>Deserializes a datasource from an UTF-8 file.</summary>
    /// <param name="fileName">An UTF-8 file previously serialized.</param>
    protected void DeserializeSource(string fileName)
    {
        DataObject? obj = JsonSerializer.Deserialize<DataObject>(File.ReadAllBytes(fileName));
        if (obj != null)
        {
            foreach (DataSeries series in obj.Series)
            {
                Series s = new(series.Name, series.Ticker, series.Dates, series.Values,
                    (SeriesType)series.Type, (Frequency)series.Frequency);
                Source.Add(s.Name, s);
            }
            foreach (DataDef d in obj.Definitions)
                ParseDefinition($"def {d.Name} = {d.Text}", d.Description);
        }
    }

    /// <summary>Deserializes a datasource from an UTF-8 file.</summary>
    /// <param name="fileName">An UTF-8 file previously serialized.</param>
    /// <returns>An object implementing this interface.</returns>
    public static IAustraEngine Deserialize(string fileName)
    {
        AustraEngine newEngine = new(new DataSource());
        newEngine.DeserializeSource(fileName);
        return newEngine;
    }
}
