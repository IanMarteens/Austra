namespace Austra.Library.MVO;

/// <summary>Represents the status of variables in the CLA.</summary>
internal enum State
{
    /// <summary>The asset's weight is clipped at its lower bound.</summary>
    LOW = 0,
    /// <summary>The asset's weight is clipped at its upper bound.</summary>
    HIGH = 1,
    /// <summary>Free ranging asset.</summary>
    IN = 2,
}

/// <summary>State management for the CLA algorithm.</summary>
internal class States
{
    ///<summary>Variable states.</summary>
    private readonly State[] state;
    ///<summary>Set of IN variables.</summary>
    private CSet inVars;
    ///<summary>Set of OUT variables.</summary>
    private CSet outVars;

    /// <summary>Gets the inverse of IN columns of A.</summary>
    public double[,] Ai { get; }

    /// <summary>Gets or sets the vector with portfolio weights.</summary>
    public double[] Weights { get; }

    /// <summary>Gets or sets the mean of the current portfolio.</summary>
    public double Mean { get; set; }

    /// <summary>Gets or sets the variance of the current portfolio.</summary>
    public double Variance { get; set; }

    /// <summary>Gets or sets the current lambda.</summary>
    public double LambdaE { get; set; }

    /// <summary>Initializes state variables.</summary>
    /// <param name="input">Input variables.</param>
    public States(Inputs input)
    {
        int m = input.Constraints;
        int size = input.Variables + m;
        (state, inVars, outVars) = (new State[size], new CSet(size), new CSet(size));
        (Ai, Weights) = (new double[m, m], new double[size]);
    }

    /// <summary>Checks if an OUT variable is at its upper limit.</summary>
    /// <param name="j">Variable number.</param>
    /// <returns>True when variable j is OUT and at its upper limit.</returns>
    public bool IsUp(int j) => state[j] == State.HIGH;

    /// <summary>Checks if an OUT variable is at its lower limit.</summary>
    /// <param name="j">Variable number.</param>
    /// <returns>True when variable j is OUT and at its lower limit.</returns>
    public bool IsLo(int j) => state[j] == State.LOW;

    /// <summary>Moves a variable to the IN set.</summary>
    /// <param name="jIn">The free ranging variable.</param>
    public void GoIn(int jIn)
    {
        outVars.Remove(jIn);    // Delete from OUT set
        inVars.Add(jIn);        // Add to IN set
    }

    /// <summary>Moves a variable to the OUT set.</summary>
    /// <param name="jOut">Variable going OUT.</param>
    /// <param name="newState">Is the variable LOW or HIGH?</param>
    /// <param name="inputVars">Input variables.</param>
    public void GoOut(int jOut, State newState, Inputs inputVars)
    {
        inVars.Remove(jOut);
        if (jOut < inputVars.Variables)
            outVars.Add(jOut);
        state[jOut] = newState;
    }

    /// <summary>Gets the number of variables in the OUT set.</summary>
    public int OutVarCount => outVars.Count;

    /// <summary>Gets the number of variables in the IN set.</summary>
    public int InVarCount => inVars.Count;

    /// <summary>Finds the position of a variable inside the IN set.</summary>
    /// <param name="member">Variable to be located.</param>
    /// <returns>The position in the IN set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetInVarPosition(int member) => inVars.Find(member);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOutVar(int member) => outVars[member];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetInVar(int member) => inVars[member];

    /// <summary>Adds a variable to the IN set.</summary>
    /// <param name="i">The variable to add.</param>
    public void AddInVar(int i) => inVars.Add(i);

    /// <summary>Adds a variable to the OUT set.</summary>
    /// <param name="i">The variable to add.</param>
    public void AddOutVar(int i) => outVars.Add(i);
}
