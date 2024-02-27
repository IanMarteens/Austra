namespace Austra.Library.MVO;

/// <summary>Mean variance optimizer full implementation.</summary>
internal static class Markowitz
{
    public static Portfolio[] Optimize(Inputs input)
    {
        // <M3> Set up inequality constraints and slack variables
        // Index to next slack variable.
        int j = input.Securities;
        for (int i = 0; i < input.Constraints; i++)
        {
            if (input.ConstraintTypes[i] != ConstraintType.EQUAL)
            {
                if (input.ConstraintTypes[i] == ConstraintType.GREATER_THAN)
                {
                    // Convert "greater than" constraint to "less than"
                    for (int k = 0; k < input.Securities; k++)
                        input.ConstraintsLHS[i, k] = -input.ConstraintsLHS[i, k];
                    input.ConstraintsRHS[i] = -input.ConstraintsRHS[i];
                }
                // Slack variable coefficient
                input.ConstraintsLHS[i, j] = 1;
                j++;
            }
        }
        // <M5> Run simplex algorithm.
        States state = new(input);
        switch (Simplex.Run(input, state))
        {
            case Simplex.Result.INFEASIBLE:
                throw new ApplicationException(
                    "Infeasible problem. Check constraints and limits.");
            case Simplex.Result.UNBOUNDED:
                throw new ApplicationException(
                    "Unbounded E. Make sure you have a valid budget constraint.");
            case Simplex.Result.DEGENERATE:
                throw new ApplicationException("Degenerate Problem.");
        }

        // <M7> Set up for critical line algorithm.
        CriticalLines cla = new(input, state);
        // <M8> Trace out the efficient frontier.
        var result = new List<Portfolio>();
        for (int step = 1; step <= input.MaxCornerPortfolios; step++)
        {
            cla.Iteration(step);
            result.Add(Clean(new Portfolio(
                weights: state.Weights[0..input.Securities],
                lambda: state.LambdaE,
                mean: state.Mean,
                variance: state.Variance), input));
            if (state.LambdaE < input.EndLambda)
                break;
        }
        // Clean up identical portfolios.
        for (int i = 1; i < result.Count;)
        {
            Portfolio p = result[i];
            if (DVector.Equals(p.Weights, result[i - 1].Weights, Optimizer.ε))
            {
                result[i - 1] = p;
                result.RemoveAt(i);
            }
            else
                i++;
        }
        return [.. result];
    }

    private static Portfolio Clean(Portfolio p, Inputs input)
    {
        double[] weights = (double[])p.Weights;
        for (int i = 0; i< weights.Length; i++)
        {
            double w = weights[i];
            if (Abs(w - input.LowerLimits[i]) < Optimizer.ε)
                weights[i] = input.LowerLimits[i];
            else if (Abs(w - input.UpperLimits[i]) < Optimizer.ε)
                weights[i] = input.UpperLimits[i];
        }
        return p;
    }
}