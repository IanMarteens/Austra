﻿using System.Threading;

namespace Austra.Parser;

/// <summary>Listens to variable changes in the session scope.</summary>
public interface IVariableListener
{
    /// <summary>Notifies a variable change.</summary>
    /// <param name="name">Name of the variable.</param>
    /// <param name="value">
    /// Value of new variable, or <see langword="null"/> for variable removal.
    /// </param>
    void OnVariableChanged(string name, object? value);

    /// <summary>The listener should enqueue one answer from a script.</summary>
    /// <param name="value">The result to enqueue.</param>
    void Enqueue(object? value);

    /// <summary>The listener should enqueue a text range from a script.</summary>
    /// <param name="range">The range to enqueue.</param>
    void EnqueueRange(Range range);
}

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
    /// <param name="name">A name to attach to the series.</param>
    /// <param name="value">A time series.</param>
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
    /// <returns>Must echo the incoming definition.</returns>
    Definition AddDefinition(Definition definition);

    /// <summary>Removes a definition, given its name.</summary>
    /// <param name="name">Definition to be deleted.</param>
    /// <returns>The effective ordered list of definitions to remove.</returns>
    string[] DeleteDefinition(string name);

    /// <summary>Clears all definitions for reloading.</summary>
    void ClearDefinitions();

    /// <summary>Clears definitions and series for reloading.</summary>
    void ClearAll();

    /// <summary>Gets a topologically sorted list of definitions.</summary>
    IReadOnlyList<Definition> AllDefinitions { get; }

    /// <summary>Get a list of definitions that are not fully resolved.</summary>
    List<Definition> TroubledDefinitions { get; }

    /// <summary>Gets an expression tree for a given identifier.</summary>
    /// <param name="identifier">The name of a variable.</param>
    /// <param name="parsingDefinition">Are we parsing a definition.</param>
    /// <returns>May return a null expression if the identifier is not defined.</returns>
    Expression? GetExpression(string identifier, bool parsingDefinition);

    /// <summary>Gets an expression tree for a variable not yet created.</summary>
    /// <param name="identifier">The name of a variable.</param>
    /// <param name="source">Future value to be assigned to the variable.</param>
    /// <returns>An expression tree, when the identifier exists.</returns>
    Expression? GetExpression(string identifier, Expression source);

    /// <summary>Gets an expression tree for a given identifier.</summary>
    /// <param name="identifier">The name of a variable.</param>
    /// <param name="value">A new expression for the identifier.</param>
    /// <returns>An assignment expression.</returns>
    Expression SetExpression(string identifier, Expression value);

    /// <summary>Gets an expression tree for enqueuing an answer from a script.</summary>
    /// <param name="answer">The expression tree with an answer.</param>
    /// <returns>
    /// Another expression tree calling the <see cref="Listener"/> for enqueuing the answer.
    /// </returns>
    Expression GetEnqueueExpression(Expression answer);

    /// <summary>Creates a lambda expression from a given body.</summary>
    /// <param name="body">An expression returning an object.</param>
    /// <returns>The corresponding lambda expression.</returns>
    Expression<Action<IDataSource>> CreateLambda(Expression body);

    /// <summary>Gets a list of expressions from the pool, or creates a new one.</summary>
    /// <param name="length">Preferred list capacity, when creating anew.</param>
    /// <returns>Either a list from the pool or a newly created one.</returns>
    List<Expression> Rent(int length);

    /// <summary>Gets a list of parameter expressions from the pool, or creates a new one.</summary>
    /// <param name="length">Preferred list capacity, when creating anew.</param>
    /// <returns>Either a list from the pool or a newly created one.</returns>
    List<ParameterExpression> RentParams(int length);

    /// <summary>Returns a list to the pool. The list is cleared here.</summary>
    /// <param name="list">The expression list for recycling.</param>
    public void Return(List<Expression> list);

    /// <summary>Returns a list to the pool. The list is cleared here.</summary>
    /// <param name="list">The expression list for recycling.</param>
    public void ReturnParams(List<ParameterExpression> list);

    /// <summary>References a listener for variable changes in the session scope.</summary>
    /// <remarks>
    /// This property effectively decouples the engine from the datasource.
    /// The engine is responsible for setting the listener, and the datasource
    /// use the listener mainly from enqueing answers from scripts.
    /// </remarks>
    IVariableListener? Listener { get; set; }
}

/// <summary>A simple, synchronous implementation for the global scope.</summary>
public class DataSource : IDataSource
{
    /// <summary>Gets a parameter referencing a <see cref="IDataSource"/>.</summary>
    private readonly ParameterExpression sourceParameter =
        Expression.Parameter(typeof(IDataSource), "datasource");
    /// <summary>
    /// Gets a property reference to the <see cref="IDataSource.Listener"/> property.
    /// </summary>
    private readonly PropertyInfo listenerProperty =
        typeof(IDataSource).GetProperty(nameof(IDataSource.Listener))!;
    /// <summary>
    /// Gets a property reference to the <see cref="IVariableListener.Enqueue"/> method.
    /// </summary>
    private readonly MethodInfo listenerEnqueue =
        typeof(IVariableListener).GetMethod(nameof(IVariableListener.Enqueue))!;
    /// <summary>First scope of session variables.</summary>
    private readonly Dictionary<string, Series> variables;
    /// <summary>Outer scope of session variables.</summary>
    private readonly Dictionary<string, object> variables2 =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Maps identifiers into definitions.</summary>
    private readonly Dictionary<string, Definition> definitions =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Topologically-sorted list of definitions.</summary>
    private readonly List<Definition> allDefinitions = [];
    /// <summary>Memoized expressions.</summary>
    private readonly Dictionary<string, Expression> memos = [];
    /// <summary>An expression list pool.</summary>
    private readonly Stack<List<Expression>> listPool = new(4);
    /// <summary>An parameter expression list pool.</summary>
    private readonly Stack<List<ParameterExpression>> paramListPool = new(4);
    /// <summary>Synchronizes access to definitions.</summary>
    private readonly Lock defLock = new();

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
    /// <param name="name">Name of the series.</param>
    /// <param name="value">The series to be added.</param>
    public void Add(string name, Series value) => variables[name] = value;

    /// <summary>Retrieves a persisted global variable given its name.</summary>
    /// <param name="name">A case insensitive identifier.</param>
    /// <returns>The value, when exists, or null, otherwise.</returns>
    public Series? GetPersistedValue(string name) =>
        variables.TryGetValue(name, out Series? result) ? result : null;

    /// <summary>Retrieves the value of a global variable given its name.</summary>
    /// <param name="name">A case insensitive identifier.</param>
    /// <returns>The value, when exists, or null, otherwise.</returns>
    public object? this[string name]
    {
        get => variables2.TryGetValue(name, out object? result)
            ? result
            : variables.TryGetValue(name, out Series? series)
            ? series : null;
        set
        {
            if (value == null)
                variables2.Remove(name);
            else
                variables2[name] = value;
            memos.Remove(name);
            if (name != "ans")
                Listener?.OnVariableChanged(name, value);
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
    /// <returns>The incoming definition.</returns>
    public Definition AddDefinition(Definition definition)
    {
        lock (defLock)
        {
            definitions[definition.Name] = definition;
            allDefinitions.Add(definition);
            return definition;
        }
    }

    /// <summary>Removes a definition, given its name.</summary>
    /// <param name="name">Name of the definition to remove.</param>
    /// <returns>The effective ordered list of definitions to remove.</returns>
    public string[] DeleteDefinition(string name)
    {
        lock (defLock)
        {
            Stack<Definition> defs = new();
            if (definitions.TryGetValue(name, out Definition? def))
                Delete(defs, def);
            string[] result = new string[defs.Count];
            int idx = 0;
            while (defs.Count > 0)
            {
                Definition d = defs.Pop();
                result[idx++] = d.Name;
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
            memos.Clear();
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
        variables.Values.OrderBy(s => s.Name);

    /// <summary>Retrieves the list of transient stored values.</summary>
    public IEnumerable<(string name, Type? type)> Locals =>
        variables2
            .Select(kp => (kp.Key, kp.Value?.GetType()))
            .OrderBy(t => t.Key);

    /// <summary>Gets all variables.</summary>
    public IEnumerable<(string name, Type? type)> Variables =>
        variables.Where(kp => !variables2.ContainsKey(kp.Key))
            .Select(kp => (kp.Key, kp.Value?.GetType()))
            .Concat(variables2.Select(kp => (kp.Key, kp.Value?.GetType())))
            .OrderBy(t => t.Key);

    /// <summary>A list with non-compilable definitions.</summary>
    public List<Definition> TroubledDefinitions { get; } = [];

    /// <summary>Gets an expression tree for a given identifier.</summary>
    /// <param name="identifier">The name of a variable.</param>
    /// <param name="parsingDefinition">Are we parsing a definition.</param>
    /// <returns>An expression tree, when the identifier exists.</returns>
    public Expression? GetExpression(string identifier, bool parsingDefinition)
    {
        object? val = parsingDefinition ? GetPersistedValue(identifier) : this[identifier];
        return val switch
        {
            null => null,
            double dv => Expression.Constant(dv),
            int iv => Expression.Constant(iv),
            bool bv => Expression.Constant(bv),
            string sv => Expression.Constant(sv),
            _ => memos.TryGetValue(identifier, out Expression? memo)
                ? memo
                : identifier.Equals("ans", StringComparison.OrdinalIgnoreCase)
                // "ans" cannot be memoized because of its dynamic type.
                ? Expression.Convert(
                    Expression.Property(sourceParameter, "Item",
                    Expression.Constant("ans")), GetAnswerBestType(val))
                : memos[identifier] = Expression.Convert(
                    Expression.Property(sourceParameter, "Item",
                    Expression.Constant(identifier)), val.GetType())
        };

        // Make sure sequences have their root type.
        static Type GetAnswerBestType(object value) => value switch
        {
            NSequence => typeof(NSequence),
            DSequence => typeof(DSequence),
            CSequence => typeof(CSequence),
            _ => value.GetType()
        };
    }

    /// <summary>Gets an expression tree for a variable not yet created.</summary>
    /// <param name="identifier">The name of a variable.</param>
    /// <param name="source">Future value to be assigned to the variable.</param>
    /// <returns>An expression tree, when the identifier exists.</returns>
    public Expression? GetExpression(string identifier, Expression source) =>
        source is ConstantExpression
        ? source
        : memos[identifier] = Expression.Convert(
            Expression.Property(sourceParameter, "Item",
            Expression.Constant(identifier)), source.Type);

    /// <summary>Gets an expression tree for a given identifier.</summary>
    /// <param name="identifier">The name of a variable.</param>
    /// <param name="value">A new expression for the identifier.</param>
    /// <returns>An assignment expression.</returns>
    public Expression SetExpression(string identifier, Expression value) =>
        Expression.Assign(Expression.Property(sourceParameter, "Item",
            Expression.Constant(identifier)), value);

    /// <summary>Gets an expression tree for enqueuing an answer from a script.</summary>
    /// <param name="answer">The expression tree with an answer.</param>
    /// <returns>
    /// Another expression tree calling the <see cref="Listener"/> for enqueuing the answer.
    /// </returns>
    public Expression GetEnqueueExpression(Expression answer) =>
        Expression.Call(
            Expression.Property(sourceParameter, listenerProperty),
            listenerEnqueue, answer);

    /// <summary>Creates a lambda expression from a given body.</summary>
    /// <param name="body">An expression returning an object.</param>
    /// <returns>The corresponding lambda expression.</returns>
    public Expression<Action<IDataSource>> CreateLambda(Expression body) =>
        Expression.Lambda<Action<IDataSource>>(body, sourceParameter);

    /// <summary>Gets a list of expressions from the pool, or creates a new one.</summary>
    /// <param name="length">Preferred list capacity, when creating anew.</param>
    /// <returns>Either a list from the pool or a newly created one.</returns>
    public List<Expression> Rent(int length) =>
        listPool.Count == 0 ? new(length) : listPool.Pop();

    /// <summary>Gets a list of parameter expressions from the pool, or creates a new one.</summary>
    /// <param name="length">Preferred list capacity, when creating anew.</param>
    /// <returns>Either a list from the pool or a newly created one.</returns>
    public List<ParameterExpression> RentParams(int length) =>
        paramListPool.Count == 0 ? new(length) : paramListPool.Pop();

    /// <summary>Returns a list to the pool. The list is cleared here.</summary>
    /// <param name="list">The expression list for recycling.</param>
    public void Return(List<Expression> list)
    {
        list.Clear();
        listPool.Push(list);
    }

    /// <summary>Returns a list to the pool. The list is cleared here.</summary>
    /// <param name="list">The expression list for recycling.</param>
    public void ReturnParams(List<ParameterExpression> list)
    {
        list.Clear();
        paramListPool.Push(list);
    }

    /// <summary>References a listener for variable changes in the session scope.</summary>
    public IVariableListener? Listener { get; set; }
}