using System.IO;
using System.Text.Json;

namespace Austra.Parser;

/// <summary>Defines an answer from the AUSTRA engine.</summary>
/// <param name="Value">The returned value.</param>
/// <param name="Type">The type of the returned value.</param>
/// <param name="Variable">If a variable has been defined, this is its name.</param>
public readonly record struct AustraAnswer(object? Value, Type? Type, string Variable);

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
    /// <returns>A list of variables and definitions.</returns>
    IList<(string member, string description)> GetRoots();

    /// <summary>Gets a list of root classes.</summary>
    /// <returns>A list of classes that accepts class methods.</returns>
    IList<(string member, string description)> GetRootClasses();

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>An empty list, if not a valid type.</returns>
    IList<(string member, string description)> GetMembers(string text);

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <param name="type">The type of the expression fragment.</param>
    /// <returns>An empty list, if not a valid type.</returns>
    IList<(string member, string description)> GetMembers(string text, out Type? type);

    /// <summary>Gets a list of class members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>Null if not a valid type.</returns>
    IList<(string member, string description)> GetClassMembers(string text);

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
    /// <summary>Creates an evaluation engine from a datasource.</summary>
    /// <param name="source">A scope for variables.</param>
    public AustraEngine(IDataSource source) => Source = source;

    /// <summary>The data source associated with the engine.</summary>
    public IDataSource Source { get; }

    /// <summary>Parses and evaluates an AUSTRA formula.</summary>
    /// <param name="formula">Any acceptable text for an AUSTRA formula.</param>
    /// <returns>The result of the evaluation.</returns>
    public AustraAnswer Eval(string formula)
    {
        ExecutionTime = CompileTime = null;
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
        Func<IDataSource, object> lambda = parser.Parse();
        sw.Stop();
        CompileTime = sw.ElapsedTicks * 1E9 / Stopwatch.Frequency;
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
        ExecutionTime = CompileTime = null;
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
    /// <returns>A list of variables and definitions.</returns>
    public IList<(string member, string description)> GetRoots() =>
        Source.Variables.Select(t => (t.name, "Variable: " + t.type?.Name))
            .Concat(Source.AllDefinitions
                .Select(d => (name: d.Name, "Definition: " + d.Type.Name)))
            .Concat(GetRootClasses())
            .Concat(GetGlobalFunctions())
            .OrderBy(x => x.Item1)
            .ToList();

    private IList<(string member, string definition)> GetGlobalFunctions() =>
        new[]
        {
            ("abs(", "Absolute value"),
            ("sqrt(", "Squared root"),
            ("gamma(", "The Gamma function"),
            ("beta(", "The Beta function"),
            ("erf(", "Error function"),
            ("ncdf(", "Normal cummulative function"),
            ("probit(", "Probit function"),
            ("log(", "Natural logarithm"),
            ("log10(", "Base 10 logarithm"),
            ("exp(", "Exponential function"),
            ("sin(", "Sine function"),
            ("cos(", "Cosine function"),
            ("tan(", "Tangent function"),
            ("asin(", "Arcsine function"),
            ("acos(", "Arccosine function"),
            ("atan(", "Arctangent function"),
            ("min(", "Minimum function"),
            ("max(", "Maximum function"),
            ("pi", "The constant π"),
            ("e", "The constant e"),
            ("i", "The imaginary unit"),
            ("today", "The current date"),
            ("compare(", "Compares two series or vectors"),
            ("polyEval(", "Evaluates a polynomial at a given point"),
            ("polyDerivative(", "Evaluates a polynomial first derivative at a given point"),
            ("polySolve(", "Returns all real and complex roots of a polynomial"),
            ("solve(", "Approximates a root with the Newton-Raphson algorithm"),
            ("complex(", "Creates a complex number from its real and imaginary components"),
            ("polar(", "Creates a complex number from its magnitude and phase components"),
            ("set", "Assigns a value to a variable"),
            ("let", "Declares local variables"),
        };  

    /// <summary>Gets a list of root classes.</summary>
    /// <returns>A list of classes that accepts class methods.</returns>
    public IList<(string member, string description)> GetRootClasses() =>
        new[]
        {
            ("complexvector::", "Allows access to complex vector constructors"),
            ("matrix::", "Allows access to matrix constructors"),
            ("model::", "Allows access to model constructors"),
            ("series::", "Allows access to series constructors"),
            ("spline::", "Allows access to spline constructors"),
            ("vector::", "Allows access to vector constructors"),
        };

    /// <summary>Checks if the name is a valid class accepting class methods.</summary>
    /// <param name="text">Class name to check.</param>
    /// <returns><see langword="true"/> if the text is a valid class name.</returns>
    public bool IsClass(string text) => text.ToUpperInvariant() switch
    {
        "COMPLEXVECTOR" or "MATRIX" or "MODEL" or "SERIES" or "VECTOR" or "SPLINE" => true,
        _ => false,
    };

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>An empty list, if not a valid type.</returns>
    public IList<(string member, string description)> GetMembers(string text) =>
        Parser.GetMembers(Source, text, out _);

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <param name="type">The type of the expression fragment.</param>
    /// <returns>An empty list, if not a valid type.</returns>
    public IList<(string member, string description)> GetMembers(string text, out Type? type) =>
        Parser.GetMembers(Source, text, out type);

    /// <summary>Gets a list of class members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>Null if not a valid type.</returns>
    public IList<(string member, string description)> GetClassMembers(string text) =>
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

    /// <summary>Gets the compiling time, in nanoseconds, of the last evaluation.</summary>
    public double? CompileTime { get; private set; }

    /// <summary>DTO for serializing a definition.</summary>
    protected sealed record DataDef(string Name, string Text, string Description);
    
    /// <summary>DTO for serializing a series.</summary>
    protected sealed record DataSeries(
        string Name, string? Ticker, int Type, int Frequency, 
        Date[] Dates, double[] Values);

    /// <summary>DTO for serializing a datasource.</summary>
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
