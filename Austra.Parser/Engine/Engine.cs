using System.IO;
using System.Text.Json;

namespace Austra.Parser;

/// <summary>Defines an answer from the AUSTRA engine.</summary>
/// <param name="Value">The returned value.</param>
/// <param name="Type">The type of the returned value.</param>
/// <param name="Variable">If a variable has been defined, this is its name.</param>
public readonly record struct AustraAnswer(object? Value, Type? Type, string Variable)
{
    /// <summary>Create an answer from a value and deduce its type from the value.</summary>
    /// <param name="value">The value to be returned.</param>
    /// <param name="variable">Optional variable name.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AustraAnswer(object? value, string variable = "")
        : this(value, value?.GetType(), variable) { }
}

/// <summary>Represents a member with its description for code completion.</summary>
/// <param name="Name">The text of the member.</param>
/// <param name="Description">A human-readable description.</param>
public readonly record struct Member(string Name, string Description);

/// <summary>Represents the AUSTRA engine.</summary>
public interface IAustraEngine : IVariableListener
{
    /// <summary>Parses and evaluates an AUSTRA formula.</summary>
    /// <param name="formula">Any acceptable text for an AUSTRA formula.</param>
    void Eval(string formula);

    /// <summary>Parses an AUSTRA formula and returns its type.</summary>
    /// <remarks>No code is generated.</remarks>
    /// <param name="formula">Any acceptable text for an AUSTRA formula.</param>
    /// <returns>The type resulting from the evaluation.</returns>
    Type[] EvalType(string formula);

    /// <summary>The data source associated with the engine.</summary>
    IDataSource Source { get; }

    /// <summary>Gets the queue of answers.</summary>
    Queue<AustraAnswer> AnswerQueue { get; }

    /// <summary>Gets the queue of fragment's ranges.</summary>
    Queue<Range> RangeQueue { get; }

    /// <summary>Gets a list of root variables.</summary>
    /// <param name="position">The position of the cursor.</param>
    /// <param name="text">The text of the expression.</param>
    /// <returns>A list of variables and definitions.</returns>
    IList<Member> GetRoots(int position, string text);

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
    /// <summary>Global bindings for the parser.</summary>
    private readonly Bindings bindings = new();
    /// <summary>The list of classes and global variables, for code completion.</summary>
    private readonly Member[] classesAndGlobals;
    /// <summary>The last value returned by the engine.</summary>
    private object? lastValue;

    /// <summary>Creates an evaluation engine from a datasource.</summary>
    /// <param name="source">A scope for variables.</param>
    public AustraEngine(IDataSource source)
    {
        Source = source;
        Source.Listener = this;
        classesAndGlobals = bindings.GetGlobalRoots();
    }

    /// <summary>The data source associated with the engine.</summary>
    public IDataSource Source { get; }

    /// <summary>Gets the queue of answers.</summary>
    public Queue<AustraAnswer> AnswerQueue { get; } = new();

    /// <summary>Gets the queue of fragment's ranges.</summary>
    public Queue<Range> RangeQueue { get; } = new();

    /// <summary>Parses and evaluates an AUSTRA formula.</summary>
    /// <param name="formula">Any acceptable text for an AUSTRA formula.</param>
    public void Eval(string formula)
    {
        ExecutionTime = GenerationTime = CompileTime = null;
        AnswerQueue.Clear();
        RangeQueue.Clear();
        lastValue = null;
        if (DefineRegex().IsMatch(formula))
        {
            Definition def = ParseDefinition(formula);
            Define(def);
            AnswerQueue.Enqueue(new(def, def.Name));
            return;
        }

        Match match = UndefineRegex().Match(formula);
        if (match.Success)
        {
            string name = match.Groups["name"].Value;
            string[] dList = Source.DeleteDefinition(name);
            if (dList.Length == 0)
                throw new Exception($"Definition {name} not found.");
            Undefine(dList);
            AnswerQueue.Enqueue(new(new UndefineList(dList), name));
            return;
        }

        using Parser parser = CreateParser(formula);
        Stopwatch sw = Stopwatch.StartNew();
        Expression<Action<IDataSource>> expression =
            Source.CreateLambda(parser.ParseStatement());
        sw.Stop();
        CompileTime = sw.ElapsedTicks * 1E9 / Stopwatch.Frequency;
        sw.Restart();
        Action<IDataSource> lambda = expression.Compile();
        sw.Stop();
        GenerationTime = sw.ElapsedTicks * 1E9 / Stopwatch.Frequency;
        sw.Restart();
        lambda(Source);
        sw.Stop();
        ExecutionTime = sw.ElapsedTicks * 1E9 / Stopwatch.Frequency;
        if (lastValue != null)
            Source["ans"] = lastValue;
    }

    /// <summary>Parses an AUSTRA formula and returns its type.</summary>
    /// <remarks>No code is generated.</remarks>
    /// <param name="formula">Any acceptable text for an AUSTRA formula.</param>
    /// <returns>The type resulting from the evaluation.</returns>
    public Type[] EvalType(string formula)
    {
        ExecutionTime = GenerationTime = CompileTime = null;
        AnswerQueue.Clear();
        RangeQueue.Clear();
        lastValue = null;
        Stopwatch sw = Stopwatch.StartNew();
        using Parser parser = CreateParser(formula);
        Type[] result = parser.ParseType();
        sw.Stop();
        CompileTime = sw.ElapsedTicks * 1E9 / Stopwatch.Frequency;
        return result;
    }

    /// <summary>Validates a definition (no SET clause) and returns its type.</summary>
    /// <param name="definition">An AUSTRA definition.</param>
    /// <returns>The new definition.</returns>
    private Definition ParseDefinition(string definition)
    {
        using Parser parser = CreateParser(definition);
        return Source.AddDefinition(parser.ParseDefinition());
    }

    /// <summary>Gets a list of root variables.</summary>
    /// <param name="position">The position of the cursor.</param>
    /// <param name="text">The text of the expression.</param>
    /// <returns>A list of variables and definitions.</returns>
    public IList<Member> GetRoots(int position, string text)
    {
        if (position > 0)
        {
            using Parser parser = CreateParser(text);
            List<Member> newMembers = parser.ParseContext(position, out bool parsingHeader);
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

    /// <summary>Checks if the name is a valid class accepting class methods.</summary>
    /// <param name="text">Class name to check.</param>
    /// <returns><see langword="true"/> if the text is a valid class name.</returns>
    public bool IsClass(string text) => bindings.IsClassName(text);

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>An empty list, if not a valid type.</returns>
    public IList<Member> GetMembers(string text) =>
        bindings.GetMembers(Source, text, out _);

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <param name="type">The type of the expression fragment.</param>
    /// <returns>An empty list, if not a valid type.</returns>
    public IList<Member> GetMembers(string text, out Type? type) =>
        bindings.GetMembers(Source, text, out type);

    /// <summary>Gets a list of class members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>Null if not a valid type.</returns>
    public IList<Member> GetClassMembers(string text) =>
        bindings.GetClassMembers(text);

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
    /// <param name="Parameters">The parameter list of the definition.</param>
    /// <param name="Text">The formula of the definition.</param>
    /// <param name="Description">A textual description.</param>
    protected sealed record DataDef(string Name, string Parameters, string Text, string Description);

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
            Source.AllDefinitions
                .Select(d => new DataDef(d.Name, d.Parameters, d.Text, d.Description))
                .ToList(),
            Source.Series.Select(s =>
                new DataSeries(s.Name, s.Ticker, (int)s.Type, (int)s.Freq, s.Args.ToArray(),
                    s.EnumValues.ToArray())).ToList()));

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
                try
                {
                    if (string.IsNullOrWhiteSpace(d.Description))
                        ParseDefinition($"def {d.Name}{d.Parameters} = {d.Text}");
                    else
                    {
                        string description = d.Description.Replace("\"", "\"\"");
                        ParseDefinition($"def {d.Name}:\"{description}\"{d.Parameters} = {d.Text}");
                    }
                }
                catch
                {
                    Source.TroubledDefinitions.Add(
                        new(d.Name, d.Parameters, d.Text, d.Description, Expression.Constant(0)));
                }
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

    /// <summary>Reacts to changes in the session scope of the datasource.</summary>
    /// <param name="name">Name of the affected variable.</param>
    /// <param name="value">
    /// Value of new variable, or <see langword="null"/> for variable removal.
    /// </param>
    void IVariableListener.OnVariableChanged(string name, object? value) =>
        AnswerQueue.Enqueue(new AustraAnswer(value, name));

    /// <summary>The listener should enqueue one answer from a script.</summary>
    /// <param name="value">The result to enqueue.</param>
    void IVariableListener.Enqueue(object? value)
    {
        AnswerQueue.Enqueue(new AustraAnswer(value));
        lastValue = value ?? lastValue;
    }

    /// <summary>The listener should enqueue a text range from a script.</summary>
    /// <param name="range">The range to enqueue.</param>
    void IVariableListener.EnqueueRange(Range range) => RangeQueue.Enqueue(range);

    /// <summary>
    /// Creates a new parser using the current bindings and the current datasource.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A new instance of a parser.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Parser CreateParser(string text) => new(bindings, Source, text);
}
