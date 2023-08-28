namespace Austra.Library.MVO;

/// <summary>The Mean-Variance Optimizer core.</summary>
internal sealed class CriticalLines
{
    private readonly Inputs input;
    private readonly States state;

    private readonly double[] α;
    private readonly double[] β;
    private readonly double[] xi;
    private readonly double[] bBar;
    private readonly double[,] mi;

    private int idxOut;
    private int idxIn;
    private State outDir;
    private double λOut;
    private double λIn;
    private double oldλE;

    /// <summary>Initializes the Critical Lines algorithm.</summary>
    /// <param name="input">Input variables.</param>
    /// <param name="state">State variables.</param>
    public CriticalLines(Inputs input, States state)
    {
        (this.input, this.state) = (input, state);

        int n = input.Variables;
        int m = input.Constraints;
        α = new double[n + m];
        β = new double[n + m];
        xi = new double[n + m];
        bBar = new double[n + m];
        mi = new double[n + m, n + m];

        for (int j0 = 0; j0 < state.OutVarCount; j0++)
        {
            int j = state.GetOutVar(j0);
            α[j] = state.Weights[j];
        }

        for (int j = n; j < n + m; j++)
            state.AddInVar(j);

        // <C4> Add A and A' to MMat (already contains C)
        for (int i = 0; i < m; i++)
            for (int j = 0; j < n; j++)
                input.Cov[j, n + i] = input.Cov[n + i, j] = input.ConstraintsLHS[i, j];

        // <C5> Compute bBar vector.
        for (int j0 = 0; j0 < state.InVarCount; j0++)
        {
            int j = state.GetInVar(j0);
            double sum = j < n ? 0.0 : input.ConstraintsRHS[j - n];
            for (int k0 = 0; k0 < state.OutVarCount; k0++)
            {
                int k = state.GetOutVar(k0);
                sum -= input.Cov[j, k] * state.Weights[k];
            }
            bBar[j] = sum;
        }

        //<C6> Set up initial Mi (M-bar-inverse)
        // Mi = |  0                Ai       |
        //      | Ai'   -Ai' * C(IN,IN) * Ai |
        // First copy Ai and Ai'
        for (int j0 = 0; j0 < m; j0++)
        {
            int j = state.GetInVar(j0);
            for (int i = 0; i < m; i++)
                mi[j, n + i] = mi[n + i, j] = state.Ai[j0, i];
        }
        // T = Ai' * C(IN,IN)
        double[,] T = new double[m, m];
        for (int i = 0; i < m; i++)
            for (int j0 = 0; j0 < m; j0++)
            {
                int j = state.GetInVar(j0);
                double sum = 0;
                for (int k = 0; k < m; k++)
                    sum -= state.Ai[k, i] * input.Cov[state.GetInVar(k), j];
                T[i, j0] = sum;
            }
        // Lower right portion of Mi is then T * Ai
        for (int i = 0; i < m; i++)
            for (int j = 0; j < m; j++)
            {
                double sum = 0.0;
                for (int k = 0; k < m; k++)
                    sum += T[i, k] * state.Ai[k, j];
                mi[n + i, n + j] = sum;
            }
    }

    /// <summary>Iteration cycle.</summary>
    /// <param name="step">Step number for the current iteration.</param>
    public void Iteration(int step)
    {
        // <C10> If this is not the first iteration, then add or delete the
        // variable determined in previous iteration.
        if (step > 1)
            if (λOut > λIn)
                DeleteVariable(idxOut, outDir);
            else
                AddVariable(idxIn);

        // <C11> Determine which IN variable wants to go OUT first.
        idxOut = -1;
        λOut = 0;
        for (int i0 = 0; i0 < state.InVarCount; i0++)
        {
            // Compute alpha and beta for variable.
            int i = state.GetInVar(i0);
            double α_i = 0, β_i = 0;
            for (int j0 = 0; j0 < state.InVarCount; j0++)
            {
                int j = state.GetInVar(j0);
                double m_ij = mi[i, j];
                α_i += m_ij * bBar[j];
                if (j < input.Variables)
                    β_i += m_ij * input.Mean[j];
            }
            α[i] = α_i;
            β[i] = β_i;
            if (i < input.Variables)
            {
                // For non-lambda variable check for going OUT.
                if (β_i > Optimizer.ε)
                {
                    // Check for hitting lower limit.
                    double tempλ = (input.LowerLimits[i] - α_i) / β_i;
                    if (tempλ >= λOut)
                        (idxOut, λOut, outDir) = (i, tempλ, State.LOW);
                }
                else if (input.UpperLimits[i] < Simplex.INFINITY && β_i < -Optimizer.ε)
                {
                    // Check for hitting upper limit.
                    double tempλ = (input.UpperLimits[i] - α_i) / β_i;
                    if (tempλ >= λOut)
                        (idxOut, λOut, outDir) = (i, tempλ, State.HIGH);
                }
            }
        }

        // <C12> Determine which OUT variable wants to come IN first.
        idxIn = -1;
        λIn = 0;
        for (int i0 = 0; i0 < state.OutVarCount; i0++)
        {
            // Compute gamma and delta for variable.
            int i = state.GetOutVar(i0);
            double γ_i = 0, δ_i = -input.Mean[i];

            for (int j = 0; j < input.Variables + input.Constraints; j++)
            {
                double c_ij = input.Cov[i, j];
                γ_i += c_ij * α[j];
                δ_i += c_ij * β[j];
            }
            if (state.IsLo(i) ? δ_i > Optimizer.ε : δ_i < -Optimizer.ε)
            {
                // Check for variable coming off lower/upper limit.
                double tempλ = -γ_i / δ_i;
                if (tempλ >= λIn)
                    (idxIn, λIn) = (i, tempλ);
            }
        }

        // <C13> The new λE is the greater of λIn and λOut. If λOut
        // is greater, then a variable goes OUT as λE is  decreased.
        // Otherwise, a variable comes IN as λE is decreased.
        state.LambdaE = Max(Max(λIn, λOut), 0);

        // <C14> Calculate the new corner portfolio, the E and v for
        // new corner portfolio, and a0, al, and a2 between this and
        // previous corner portfolio.
        CalcCornerPortfolio();
    }

    private void CalcCornerPortfolio()
    {
        // <C40> Calculate the new corner portfolio.
        // <C41> Calculate dE/dλE.
        double dE_dλE = 0;
        for (int i0 = 0; i0 < state.InVarCount - input.Constraints; i0++)
        {
            int i = state.GetInVar(i0);
            state.Weights[i] = α[i] + β[i] * state.LambdaE;
            dE_dλE += β[i] * input.Mean[i];
        }

        if (dE_dλE < 0.000_000_001)
        {
            // <C42> "kink" in curve, compute Mean and Variance from scratch.
            state.Mean = 0;
            state.Variance = 0;
            for (int i = 0; i < input.Securities; i++)
            {
                double wj = state.Weights[i];
                state.Mean += input.Mean[i] * wj;
                state.Variance += input.Cov[i, i] * wj * wj;
                for (int j = 0; j < i; j++)
                    state.Variance += 2 * input.Cov[i, j] * wj * state.Weights[j];
            }
        }
        else
        {
            // <C43> Compute Mean and Variance incrementally.
            double a2 = 1.0 / dE_dλE;
            double a1 = 2.0 * (oldλE - a2 * state.Mean);
            double a0 = state.Variance - (a1 + a2 * state.Mean) * state.Mean;
            state.Mean += (state.LambdaE - oldλE) * dE_dλE;
            state.Variance = a0 + (a1 + a2 * state.Mean) * state.Mean;
        }
        oldλE = state.LambdaE;
    }

    private void AddVariable(int jAdd)
    {
        // <C20> update Mi for variable coming IN.
        // xi = Mi(IN,IN) * M(IN,jAdd);
        double xij = input.Cov[jAdd, jAdd];
        for (int i0 = 0; i0 < state.InVarCount; i0++)
        {
            int i = state.GetInVar(i0);
            double s = 0;
            for (int j0 = 0; j0 < state.InVarCount; j0++)
            {
                int j = state.GetInVar(j0);
                s += mi[i, j] * input.Cov[j, jAdd];
            }
            xi[i] = s;
            xij -= input.Cov[jAdd, i] * xi[i];
        }

        // Mi(IN,IN) += Xi*xi.T/xij
        // Mi(jAdd,IN) = Mi(IN,jAdd) = -xi / xij
        for (int i0 = 0; i0 < state.InVarCount; i0++)
        {
            int i = state.GetInVar(i0);
            for (int j0 = 0; j0 < i0; j0++)
            {
                int j = state.GetInVar(j0);
                mi[j, i] = mi[i, j] += xi[i] * xi[j] / xij;
            }
            mi[i, i] += xi[i] * xi[i] / xij;
            mi[jAdd, i] = mi[i, jAdd] = -xi[i] / xij;
            // <C21> Update bBar for the current IN variables.
            bBar[i] += input.Cov[i, jAdd] * state.Weights[jAdd];
        }
        mi[jAdd, jAdd] = 1.0 / xij;

        // Variable jAdd goes IN
        state.GoIn(jAdd);

        // <C22> Compute bBar for new IN variable.
        double sum = 0;
        for (int i0 = 0; i0 < state.OutVarCount; i0++)
        {
            int i = state.GetOutVar(i0);
            sum -= input.Cov[jAdd, i] * state.Weights[i];
        }
        bBar[jAdd] = sum;
    }

    private void DeleteVariable(int jDel, State direction)
    {
        // <C30> Update alpha and beta vectors for variable going OUT.
        α[jDel] = state.Weights[jDel];
        β[jDel] = 0;
        // <C31> Variable jDel goes OUT
        state.GoOut(jDel, direction, input);
        // <C32> Update Mi and bBar for variable going OUT.
        // <C33> Update bBar(IN)
        for (int i0 = 0; i0 < state.InVarCount; i0++)
        {
            int i = state.GetInVar(i0);
            for (int j0 = 0; j0 < state.InVarCount; j0++)
            {
                int j = state.GetInVar(j0);
                mi[i, j] -= mi[i, jDel] * mi[jDel, j] / mi[jDel, jDel];
            }
            bBar[i] -= input.Cov[i, jDel] * state.Weights[jDel];
        }
    }
}
