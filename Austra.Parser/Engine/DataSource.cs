namespace Austra.Parser;

/// <summary>Represents the outer scope in AUSTRA formulas.</summary>
public interface IDataSource
{
    /// <summary>Retrieves a global variable given its name.</summary>
    /// <param name="name">A case insensitive identifier.</param>
    /// <returns>The value, when exists, or null, otherwise.</returns>
    object? this[string name] { get; set; }

    /// <summary>Retrieves a persisted global variable given its name.</summary>
    /// <param name="name">A case insensitive identifier.</param>
    /// <returns>The value, when exists, or null, otherwise.</returns>
    Series? GetPersistedValue(string name);

    /// <summary>Adds a global variable definition.</summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    void Add(string name, Series value);

    /// <summary>Retrieves the list of persisted series in the global scope.</summary>
    IEnumerable<Series> Series { get; }

    /// <summary>Retrieves the list of transient stored values.</summary>
    IEnumerable<(string name, Type? type)> Locals { get; }

    /// <summary>Gets all variables.</summary>
    IEnumerable<(string name, Type? type)> Variables { get; }

    /// <summary>Retrieves a definition given its name.</summary>
    /// <param name="name">Name of the definition.</param>
    /// <returns>The definition, if found. Null, otherwise.</returns>
    Definition? GetDefinition(string name);

    /// <summary>Adds a parameterless macro definition to the source.</summary>
    /// <param name="definition">A definition to be added.</param>
    void AddDefinition(Definition definition);

    /// <summary>Removes a definition, given its name.</summary>
    /// <param name="name"></param>
    /// <returns>The effective ordered set of definitions to remove.</returns>
    IList<string> DeleteDefinition(string name);

    /// <summary>Clears all definitions for reloading.</summary>
    void ClearDefinitions();

    /// <summary>Clears definitions and series for reloading.</summary>
    void ClearAll();

    /// <summary>Gets a topologically sorted list of definitions.</summary>
    IReadOnlyList<Definition> AllDefinitions { get; }

    /// <summary>Get a list of definitions that are not fully resolved.</summary>
    IList<Definition> TroubledDefinitions { get; }
}

/// <summary>A simple, synchronous implementation for the global scope.</summary>
public class DataSource : IDataSource
{
    /// <summary>First scope of session variables.</summary>
    private readonly Dictionary<string, Series> variables;
    /// <summary>Outer scope of session variables.</summary>
    private readonly Dictionary<string, object> variables2 =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Maps identifiers into definitions.</summary>
    private readonly Dictionary<string, Definition> definitions =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Topologically-sorted list of definitions.</summary>
    private readonly List<Definition> allDefinitions = new();
    /// <summary>Synchronizes access to definitions.</summary>
    private readonly object defLock = new();

    /// <summary>Creates an empty datasource.</summary>
    public DataSource() =>
        variables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a datasource using a shared seed.</summary>
    /// <param name="source">An existing seed.</param>
    /// <remarks>Only the first scope is shared.</remarks>
    public DataSource(DataSource source) =>
        (variables, definitions, allDefinitions) =
        (source.variables, source.definitions, source.allDefinitions);

    /// <summary>Adds a global variable definition.</summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public void Add(string name, Series value) => variables[name] = value;

    /// <summary>Retrieves a persisted global variable given its name.</summary>
    /// <param name="name">A case insensitive identifier.</param>
    /// <returns>The value, when exists, or null, otherwise.</returns>
    public Series? GetPersistedValue(string name) =>
        variables.TryGetValue(name, out Series? result) ? result : null;

    /// <summary>Retrieves a global variable given its name.</summary>
    /// <param name="name">A case insensitive identifier.</param>
    /// <returns>The value, when exists, or null, otherwise.</returns>
    public object? this[string name]
    {
        get
        {
            if (variables2.TryGetValue(name, out object? result))
                return result;
            variables.TryGetValue(name, out Series? series);
            return series;
        }
        set
        {
            if (value == null)
                variables2.Remove(name);
            else
                variables2[name] = value;
        }
    }

    /// <summary>Retrieves a definition given its name.</summary>
    /// <param name="name">Name of the definition.</param>
    /// <returns>The definition, if found. Null, otherwise.</returns>
    public Definition? GetDefinition(string name)
    {
        lock (defLock)
            return definitions.TryGetValue(name, out Definition? def) ? def : null;
    }

    /// <summary>Adds a parameterless macro definition to the source.</summary>
    /// <param name="definition">A definition to be added.</param>
    public void AddDefinition(Definition definition)
    {
        lock (defLock)
        {
            definitions[definition.Name] = definition;
            allDefinitions.Add(definition);
        }
    }

    /// <summary>Removes a definition, given its name.</summary>
    /// <param name="name"></param>
    /// <returns>The effective ordered set of definitions to remove.</returns>
    public IList<string> DeleteDefinition(string name)
    {
        lock (defLock)
        {
            Stack<Definition> defs = new();
            if (definitions.TryGetValue(name, out Definition? def))
                Delete(defs, def);
            List<string> result = new(defs.Count);
            while (defs.Count > 0)
            {
                Definition d = defs.Pop();
                result.Add(d.Name);
                definitions.Remove(d.Name);
                allDefinitions.Remove(d);
            }
            foreach (Definition d in allDefinitions)
            {
                for (int i = 0; i < d.Children.Count;)
                    if (d.Children[i].Flag)
                        d.Children.RemoveAt(i);
                    else
                        i++;
            }
            return result;
        }

        static void Delete(Stack<Definition> defs, Definition def)
        {
            defs.Push(def);
            def.Flag = true;
            foreach (Definition child in def.Children)
                if (!child.Flag)
                    Delete(defs, child);
        }
    }

    /// <summary>Clears all definitions for reloading.</summary>
    public void ClearDefinitions()
    {
        lock (defLock)
        {
            definitions.Clear();
            allDefinitions.Clear();
            TroubledDefinitions.Clear();
        }
    }

    /// <summary>Clears definitions and series for reloading.</summary>
    public void ClearAll()
    {
        lock (defLock)
        {
            variables.Clear();
            definitions.Clear();
            allDefinitions.Clear();
            TroubledDefinitions.Clear();
        }
    }

    /// <summary>Gets a topologically sorted list of definitions.</summary>
    public IReadOnlyList<Definition> AllDefinitions
    {
        get
        {
            lock (defLock)
                return allDefinitions;
        }
    }

    /// <summary>Retrieves the list of series in the global scope.</summary>
    public IEnumerable<Series> Series =>
        variables.Values.OfType<Series>().OrderBy(s => s.Name);

    /// <summary>Retrieves the list of transient stored values.</summary>
    public IEnumerable<(string name, Type? type)> Locals =>
        variables2
            .Select(kp => (kp.Key, kp.Value?.GetType() ?? null))
            .OrderBy(t => t.Key);

    /// <summary>Gets all variables.</summary>
    public IEnumerable<(string name, Type? type)> Variables =>
        variables.Where(kp => !variables2.ContainsKey(kp.Key))
            .Select(kp => (kp.Key, kp.Value?.GetType() ?? null))
            .Concat(variables2.Select(kp => (kp.Key, kp.Value?.GetType() ?? null)))
            .OrderBy(t => t.Key);

    /// <summary>A list with non-compilable definitions.</summary>
    public IList<Definition> TroubledDefinitions { get; } = new List<Definition>();
}