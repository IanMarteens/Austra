using Austra.Library;
using Austra.Parser;
using System.Diagnostics;
using System.Reflection;
using static System.Console;

OutputEncoding = System.Text.Encoding.UTF8;
WriteLine("AUSTRA REPL");
WriteLine("v" + GetVersion());
WriteLine();
IDataSource source = new DataSource();
IAustraEngine engine = new AustraEngine(source);
if (File.Exists(GetDefaultDataFile()))
    LoadFile(ref engine, ref source, false);
bool includeTime = false;
for (; ; )
{
    using (new ColorChanger(ConsoleColor.DarkGray))
        Write("> ");
    string? line = ReadLine();
    if (string.IsNullOrEmpty(line) || line.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;
    try
    {
        const string IMPORT = "import ";
        const string LOAD = "load";
        const string SAVE = "save";
        const string SHOW = "show";

        if (line.Trim().Equals(SHOW, StringComparison.OrdinalIgnoreCase))
            ShowSeriesNames(source);
        else if (line.StartsWith(SHOW + " ", StringComparison.OrdinalIgnoreCase))
            ShowHelpOnExpression(engine, line[SHOW.Length..].Trim(), ref includeTime);
        else if (line.Equals("@time", StringComparison.OrdinalIgnoreCase))
            SwitchTime(ref includeTime);
        else if (line.Equals(LOAD, StringComparison.OrdinalIgnoreCase))
            LoadFile(ref engine, ref source, includeTime);
        else if (line.StartsWith(LOAD + " ", StringComparison.OrdinalIgnoreCase))
            LoadFileFrom(ref engine, ref source, line[LOAD.Length..].Trim(), includeTime);
        else if (line.StartsWith(IMPORT, StringComparison.OrdinalIgnoreCase))
            ImportCsvFile(source, line[IMPORT.Length..].Trim());
        else if (line.Equals(SAVE, StringComparison.OrdinalIgnoreCase))
            SaveFile(engine, null, includeTime);
        else if (line.StartsWith(SAVE + " ", StringComparison.OrdinalIgnoreCase))
            SaveFile(engine, line[SAVE.Length..].Trim(), includeTime);
        else
            EvaluateAndShow(engine, line, includeTime);
    }
    catch (AstException ex)
    {
        using (new ColorChanger(ConsoleColor.Magenta))
        {
            if (line.Length <= 75 && ex.Position >= 0)
                WriteLine(new string(' ', ex.Position + 2) + "^");
            WriteLine($"Error: {ex.Message}");
            WriteLine();
        }
    }
    catch (Exception ex)
    {
        using (new ColorChanger(ConsoleColor.Magenta))
        {
            WriteLine($"Error: {ex.Message}");
            WriteLine();
        }
    }
}

static string GetVersion() =>
    Assembly.GetAssembly(typeof(Vector))!.GetName().Version!.ToString(3);

static void EvaluateAndShow(IAustraEngine engine, string line, bool includeTime)
{
    AustraAnswer answer = engine.Eval(line);
    if (answer.Value is Definition def)
        WriteLine($"{def.Name} has been added as a definition.");
    else if (answer.Value is Tuple<Vector, Vector> tuple)
    {
        Write(tuple.Item1);
        Write(tuple.Item2);
    }
    else if (answer.Value is Tuple<ComplexVector, ComplexVector> ctuple)
    {
        Write(ctuple.Item1);
        Write(ctuple.Item2);
    }
    else if (answer.Value != null)
    {
        string text = answer.Value?.ToString() ?? "";
        if (!text.EndsWith(Environment.NewLine))
            WriteLine(text);
        else
            Write(text);
    }
    else
        WriteLine("null");
    if (includeTime)
    {
        double? ct = engine.CompileTime;
        double? et = engine.ExecutionTime;
        if (et != null)
            using (new ColorChanger(ConsoleColor.DarkGray))
                WriteLine(
                    $"Compile: {engine.FormatTime(ct!.Value)}, execute: {engine.FormatTime(et.Value)}");
    }
    WriteLine();
}

static void ImportCsvFile(IDataSource source, string file)
{
    if (file.Length == 0)
        throw new Exception("Error: Missing file name.");
    if (string.IsNullOrEmpty(Path.GetDirectoryName(file)))
    {
        string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        file = Path.Combine(Path.Combine(dir, "Austra"), file);
    }
    if (!File.Exists(file))
    {
        file = Path.ChangeExtension(file, ".csv");
        if (!File.Exists(file))
            throw new Exception($"Error: File not found: {file}");
    }
    int count = 0;
    foreach (var series in CsvLoader.Load(file))
        if (source[series.Name] == null)
        {
            source.Add(series.Name, series);
            count++;
        }
    using (new ColorChanger(ConsoleColor.DarkGray))
    {
        WriteLine($"Imported {file}, {count} series.");
        WriteLine();
    }
}

static void SaveFile(IAustraEngine engine, string? file, bool includeTime)
{
    string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    if (string.IsNullOrWhiteSpace(file))
        file = Path.Combine(Path.Combine(dir, "Austra"), "data.austra");
    else if (string.IsNullOrEmpty(Path.GetDirectoryName(file)))
        file = Path.Combine(Path.Combine(dir, "Austra"), file);
    Stopwatch stopwatch = Stopwatch.StartNew();
    engine.Serialize(file);
    stopwatch.Stop();
    using (new ColorChanger(ConsoleColor.DarkGray))
    {
        WriteLine($"File saved: {file}");
        if (includeTime)
            WriteLine(engine.FormatTime(stopwatch.ElapsedTicks * 1E9 / Stopwatch.Frequency));
        WriteLine();
    }
}

static void LoadFile(ref IAustraEngine engine, ref IDataSource source, bool includeTime)
{
    string file = GetDefaultDataFile();
    if (!File.Exists(file))
        throw new Exception($"Error: File not found: {file}");
    Deserialize(out engine, out source, file, includeTime);
}

static void LoadFileFrom(ref IAustraEngine engine, ref IDataSource source, string file, bool includeTime)
{
    if (string.IsNullOrWhiteSpace(file))
    {
        LoadFile(ref engine, ref source, includeTime);
        return;
    }
    if (!File.Exists(file))
        if (!string.IsNullOrEmpty(Path.GetDirectoryName(file)))
            throw new Exception($"Error: File not found: {file}");
        else
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            file = Path.Combine(Path.Combine(dir, "Austra"), file);
            if (!File.Exists(file))
                throw new Exception($"Error: File not found: {file}");
        }
    Deserialize(out engine, out source, file, includeTime);
}

static void ShowSeriesNames(IDataSource source)
{
    using (new ColorChanger(ConsoleColor.DarkGray))
    {
        bool hasContent = false;
        List<Series> allSeries = source.Series.ToList();
        if (allSeries.Count > 0)
        {
            WriteLine($"{allSeries.Count} series:");
            foreach (Series series in allSeries)
                WriteLine($"  {series}");
            hasContent = true;
        }
        HashSet<string> allNames = new(allSeries.Select(s => s.Name),
            StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<Definition> defs = source.AllDefinitions;
        if (defs.Count > 0)
        {
            WriteLine($"{defs.Count} definitions:");
            foreach (Definition def in defs)
                if (def.Description != def.Name)
                    WriteLine($"  {def.Name}: {def.Type.Name}: {def.Description}");
                else
                    WriteLine($"  {def.Name}: {def.Type.Name}");
            hasContent = true;
        }
        allNames.Add("ans");
        List<(string name, Type? type)> allVars = source.Variables
            .Where(t => !allNames.Contains(t.name))
            .ToList();
        if (allVars.Count > 0)
        {
            WriteLine($"{allVars.Count} variables:");
            foreach ((string name, Type? type) in allVars)
                WriteLine($"  {name}: {type?.Name}");
            hasContent = true;
        }
        if (!hasContent)
            WriteLine("No content found.");
        WriteLine();
    }
}

static void SwitchTime(ref bool includeTime)
{
    using (new ColorChanger(ConsoleColor.DarkGray))
    {
        includeTime = !includeTime;
        if (includeTime)
            WriteLine("Time will be shown.");
        else
            WriteLine("Time will not be shown.");
        WriteLine();
    }
}

static void ShowHelpOnExpression(IAustraEngine engine, string expression, ref bool includeTime)
{
    if (expression.StartsWith('@'))
    {
        expression = expression[1..].TrimStart();
        if (expression.Equals("TIME", StringComparison.OrdinalIgnoreCase))
        {
            SwitchTime(ref includeTime);
            return;
        }
    }
    if (expression.EndsWith('.'))
        expression = expression[..^1].TrimEnd();
    if (expression.Length == 0)
        throw new Exception("Error: Missing expression.");
    IOrderedEnumerable<MemberList> members;
    bool isClass = false;
    Type? type = null;
    if (engine.IsClass(expression))
    {
        isClass = true;
        members = engine.GetClassMembers(expression + ":").OrderBy(t => t.Member);
    }
    else
    {
        members = engine.GetMembers(expression, out type).OrderBy(t => t.Member);
        if (type == null)
            throw new Exception($"Unknown expression: {expression}");
    }
    using (new ColorChanger(ConsoleColor.DarkGray))
    {
        if (isClass)
            WriteLine($"{expression} class members:");
        else if (type == typeof(Series<int>))
            WriteLine("Series<int> members:");
        else if (type == typeof(Series<double>))
            WriteLine("Series<double> members:");
        else
            WriteLine($"{type!.Name} members:");
        foreach ((string member, string description) in members)
            WriteLine($"  {Transform(member)}: {description}");
        WriteLine();
    }

    static string Transform(string member)
    {
        int idx = member.IndexOf('(');
        return idx < 0 ? member : member[..(idx + 1)] + ")";
    }
}

static string GetDefaultDataFile() => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    @"Austra\data.austra");

static void Deserialize(out IAustraEngine engine, out IDataSource source, string file, bool includeTime)
{
    Stopwatch stopwatch = Stopwatch.StartNew();
    IAustraEngine newEngine = AustraEngine.Deserialize(file) ??
        throw new Exception($"Error: Failed to load {file}");
    stopwatch.Stop();
    engine = newEngine;
    source = newEngine.Source;
    using (new ColorChanger(ConsoleColor.DarkGray))
    {
        WriteLine($"Loaded {file}");
        WriteLine($"{source.Series.Count()} series, {source.AllDefinitions.Count} definitions.");
        if (includeTime)
            WriteLine(engine.FormatTime(stopwatch.ElapsedTicks * 1E9 / Stopwatch.Frequency));
        WriteLine();
    }
}