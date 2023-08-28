namespace Austra.Library.MVO;

/// <summary>A linear programming solver.</summary>
internal sealed class Simplex
{
    public enum Result
    {
        OK,
        INFEASIBLE,
        DEGENERATE,
        UNBOUNDED
    }

    /// <summary>Internally used for infinity.</summary>
    public const double INFINITY = 1E+30;

    /// <summary>Objective function coefficients.</summary>
    private readonly double[] z;
    /// <summary>Price vector.</summary>
    private readonly double[] price;
    /// <summary>Rate of adjustment of IN variables.</summary>
    private readonly double[] adjRate;
    /// <summary>Profitability vector.</summary>
    private double[] profit;
    /// <summary>Number of IN of Artificial Basis Variables.</summary>
    private int numInABVs;

    public static Result Run(Inputs input, States state) =>
        new Simplex(input).Execute(input, state);

    private Simplex(Inputs input)
    {
        int n = input.Variables;
        int m = input.Constraints;
        z = new double[n + m];
        price = new double[m];
        adjRate = new double[m];
        profit = new double[n];
    }

    private Result Execute(Inputs input, States state)
    {
        int n = input.Variables;
        // <S1> Initialize all non-AB variables to be OUT at their lower limits.
        for (int j = 0; j < n; j++)
        {
            state.AddOutVar(j);
            state.Weights[j] = input.LowerLimits[j];
        }

        // <S2> Set up ABVs.
        int m = numInABVs = input.Constraints;
        for (int i = 0; i < m; i++)
        {
            double temp = input.ConstraintsRHS[i];
            for (int j = 0; j < n; j++)
                temp -= input.ConstraintsLHS[i, j] * input.LowerLimits[j];
            state.Ai[i, i] = input.ConstraintsLHS[i, n + i] = temp >= 0 ? 1 : -1;
            state.AddInVar(n + i);
            state.Weights[n + i] = Abs(temp);
            // Objective function "expected return".
            z[n + i] = -1;
        }

        // <S3> Run simplex phase 1
        Result returnCode = SimplexPhase(input, state, true);
        if (returnCode == Result.OK)
        {
            // <S4> No ABVs are IN (not degenerate).
            input.ConstraintsLHS = Redim(input.ConstraintsLHS, m, n);
        }
        else if (returnCode == Result.DEGENERATE && input.AllowDegenerate)
        {
            // <S5> Degenerate problem--One or more ABVs still IN.
            returnCode = Result.OK; // Allow program to continue
            n = input.Variables = n + m; // Add in ABVs to variable count.
            profit = new double[n];

            // Set upper limits of ABVs to zero.
            for (int i = 0; i < m; i++)
                input.UpperLimits[n - m + i] = Optimizer.ε;

            // Increase size of Cov while preserving contents.
            double[,] mTemp = new double[input.Securities, input.Securities];
            for (int i = 0; i < input.Securities; i++)
                for (int j = 0; j < input.Securities; j++)
                    mTemp[i, j] = input.Cov[i, j];

            int newSize = n + m;
            input.Cov = Redim(input.Cov, newSize, newSize);
            for (int i = 0; i < input.Securities; i++)
                for (int j = 0; j < input.Securities; j++)
                    input.Cov[i, j] = mTemp[i, j];
        }

        if (returnCode == Result.OK)
        {
            // <56> Run simplex phase 2.
            // Objective is now to maximize expected return.
            for (int j = 0; j < n; j++)
                z[j] = input.Mean[j];
            returnCode = SimplexPhase(input, state, false);
            if (returnCode == Result.OK)
                // <S7> Ensure unique solution
                for (int j0 = 0; j0 < state.OutVarCount; j0++)
                {
                    int j = state.GetOutVar(j0);
                    // <S30> Alter Mean as required to ensure unique solution.
                    if (profit[j] > -0.000_001)
                        if (state.IsLo(j))
                            input.Mean[j] -= 0.000_001;
                        else
                            input.Mean[j] += 0.000_001;
                }
        }
        return returnCode;
    }

    private Result SimplexPhase(Inputs input, States states, bool firstPhase)
    {
        int jMax = -1;
        Result result = Result.OK;

        while (true)
        {
            // <S10> Compute price for each constraint.
            // price[i]: Price for the i-th artificial basis variable.
            for (int i = 0; i < input.Constraints; i++)
            {
                double sum = 0;
                for (int j = 0; j < states.InVarCount; j++)
                    sum -= states.Ai[j, i] * z[states.GetInVar(j)];
                price[i] = sum;
            }

            // <S11> Compute profit for each "out" variable coming "in".
            // profit[j]: Profit for variable j.
            double profitMax = 0;
            for (int j0 = 0; j0 < states.OutVarCount; j0++)
            {
                int j = states.GetOutVar(j0);
                double sum = z[j];
                for (int i = 0; i < input.Constraints; i++)
                    sum += input.ConstraintsLHS[i, j] * price[i];
                if (states.IsUp(j))
                    sum = -sum;
                profit[j] = sum;
                if (profit[j] >= profitMax)
                {
                    jMax = j;
                    profitMax = profit[j];
                }
            }

            if (profitMax < Optimizer.ε)
            {
                // <S12> No profit from any OUT variable coming IN.
                if (firstPhase)
                {
                    // Degenerate or infeasible problem.
                    for (int j0 = 0; j0 < numInABVs; j0++)
                    {
                        int j = states.GetInVar(states.InVarCount + 1 - j0);
                        if (states.Weights[j] > Optimizer.ε)
                            // An IN ABV is not zero: infeasible problem.
                            return Result.INFEASIBLE;
                    }
                    // All IN ABVS are zero--degenerate problem
                    result = Result.DEGENERATE;  // (nIABV > 0)
                }
                break;
            }

            State inDir = states.IsUp(jMax) ? State.LOW : State.HIGH;

            // <S13> Compute rate of adjustment for each IN variable as
            // variable jMax comes IN (AdjRate = - Ai * A(ALL,jMax)).
            for (int i = 0; i < input.Constraints; i++)
            {
                double sum = 0;
                for (int k = 0; k < input.Constraints; k++)
                    sum -= states.Ai[i, k] * input.ConstraintsLHS[k, jMax];
                if (inDir == State.LOW)
                    sum = -sum;
                adjRate[i] = sum;
            }

            // <S14> Compute theta, the maximum amount that variable jMax
            // can change before another IN variable hits a limit and is
            // forced OUT. Also determine which variable is forced OUT.
            // Here we compute theta such that it will always be positive.
            int iOut = -1;
            State outDir = inDir;
            double theta = input.UpperLimits[jMax] == INFINITY
                ? INFINITY
                : input.UpperLimits[jMax] - input.LowerLimits[jMax];

            for (int i = 0; i < input.Constraints; i++)
            {
                int j = states.GetInVar(i);
                if (adjRate[i] < -Optimizer.ε)
                {
                    // Check for variable hitting lower limit
                    double tmp = (input.LowerLimits[j] - states.Weights[j]) / adjRate[i];
                    if (tmp < theta)
                        (theta, iOut, outDir) = (tmp, i, State.LOW);
                }
                else if (adjRate[i] > Optimizer.ε && input.UpperLimits[j] != INFINITY)
                {
                    // Check for variable hitting upper limit
                    double tmp = (input.UpperLimits[j] - states.Weights[j]) / adjRate[i];
                    if (tmp < theta)
                        (theta, iOut, outDir) = (tmp, i, State.HIGH);
                }
            }

            // <S15> Check for failure to find a variable to go OUT.
            if (theta == INFINITY)
                return Result.UNBOUNDED;

            // Get "j" Index of variable going OUT.
            int jOut = iOut >= 0 ? states.GetInVar(iOut) : jMax;

            // <S16> Update the IN variables (x's).
            for (int i0 = 0; i0 < input.Constraints; i0++)
            {
                int j = states.GetInVar(i0);
                states.Weights[j] += theta * adjRate[i0];
            }
            if (inDir == State.HIGH)
                states.Weights[jMax] += theta;
            else
                states.Weights[jMax] -= theta;

            // <S17> Variable iMax goes IN.
            states.GoIn(jMax);
            // <S18> Variable jOut goes OUT.
            states.GoOut(jOut, outDir, input);

            // <S19> Update Alnverse If var going OUT is not var coming IN.
            if (jMax != jOut)
                UpdateAInverse(iOut, jMax, inDir);

            if (firstPhase && jOut >= input.Variables)
                // Artificial basis variable went out.
                if (--numInABVs == 0)
                    break;
        }
        return result;

        // <S20> Update Ai (inverse of A(ALL,IN)) for new IN set.
        void UpdateAInverse(int iOut, int jMax, State inDir)
        {
            for (int i = 0; i < input.Constraints; i++)
                if (i != iOut)
                {
                    double t = adjRate[i] / adjRate[iOut];
                    for (int k = 0; k < input.Constraints; k++)
                        states.Ai[i, k] -= states.Ai[iOut, k] * t;
                }

            double temp = inDir == State.HIGH ? -adjRate[iOut] : adjRate[iOut];
            for (int k = 0; k < input.Constraints; k++)
                states.Ai[iOut, k] /= temp;

            // <S21> Reorder rows of Ai to stay consistent with inVars.
            int delRow = iOut;
            int addRow = states.GetInVarPosition(jMax);
            if (addRow > delRow)
                for (int j = 0; j < input.Constraints; j++)
                {
                    temp = states.Ai[delRow, j];
                    for (int i = delRow; i <= addRow - 1; i++)
                        states.Ai[i, j] = states.Ai[i + 1, j];
                    states.Ai[addRow, j] = temp;
                }
            else if (addRow < delRow)
                for (int j = 0; j < input.Constraints; j++)
                {
                    temp = states.Ai[delRow, j];
                    for (int i = delRow; i >= addRow + 1; i--)
                        states.Ai[i, j] = states.Ai[i - 1, j];
                    states.Ai[addRow, j] = temp;
                }
        }
    }

    private static double[,] Redim(double[,] m, int newRows, int newCols)
    {
        var r = new double[newRows, newCols];
        int minRows = Min(m.GetLength(0), newRows);
        int minCols = Min(m.GetLength(1), newCols);
        for (int row = 0; row < minRows; row++)
            for (int col = 0; col < minCols; col++)
                r[row, col] = m[row, col];
        return r;
    }
}
