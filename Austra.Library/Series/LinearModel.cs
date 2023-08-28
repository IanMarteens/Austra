namespace Austra.Library;

/// <summary>Represents the result of a linear regression.</summary>
public abstract class LinearModelBase<T>
{
    /// <summary>Initializes and computes a linear regression model.</summary>
    /// <param name="original">The samples to be predicted.</param>
    /// <param name="variables">The names of the samples used as predictors.</param>
    protected LinearModelBase(T original, IReadOnlyList<string> variables) => 
        (Original, Variables, Prediction) = (original, variables, default!);

    /// <summary>The samples to be explained.</summary>
    public T Original { get; }
    /// <summary>Predicted samples.</summary>
    public T Prediction { get; protected set; }

    /// <summary>The names of the samples used as predictors.</summary>
    public IReadOnlyList<string> Variables { get; }

    /// <summary>Calculated weights for the predictors.</summary>
    public Vector Weights { get; protected set; }
    /// <summary>Student-t statistics for the weights.</summary>
    public Vector TStats { get; protected set; }

    /// <summary>Gets the total sum of squares.</summary>
    public double TotalSumSquares { get; protected set; }
    /// <summary>Gets the residual sum of squares.</summary>
    public double ResidualSumSquares { get; protected set; }
    /// <summary>Explained variance versus total variance.</summary>
    public double R2 { get; protected set; }
    /// <summary>Gets the standard error.</summary>
    public double StandardError { get; protected set; }

    /// <summary>Solves the basic OLS problem.</summary>
    /// <param name="mRows">Rows for creating the matrix.</param>
    /// <param name="rightSide">The right side vector.</param>
    /// <returns>The computed weights and the Cholesky factorization.</returns>
    protected (Vector, Cholesky) ComputeWeights(Vector[] mRows, Vector rightSide)
    {
        Matrix x = new(mRows);
        Matrix x1x = x.MultiplyTranspose(x);
        Cholesky c = x1x.Cholesky();
        return (c.Solve(x * rightSide), c);
    }

    /// <inheritdoc/>
    public sealed override string ToString()
    {
        const double ε = 1E-15;
        StringBuilder sb = new(1024);
        sb.Append("Original = ");
        if (Abs(Weights[0]) > ε)
            sb.Append(Weights[0].ToString("G6"));
        for (int i = 0; i < Variables.Count; i++)
        {
            double w = Weights[i + 1], aw = Abs(w);
            if (aw <= ε)
                continue;
            if (sb.Length > 0)
            {
                sb.Append(w < 0 ? " - " : " + ");
                if (Abs(aw - 1) <= ε)
                    sb.Append(Variables[i]);
                else
                    sb.Append(aw.ToString("G6")).Append(" * ").Append(Variables[i]);
            }
            else if (Abs(aw - 1) <= ε)
                sb.Append(Variables[i]);
            else
                sb.Append(w.ToString("G6")).Append(" * ").Append(Variables[i]);
        }
        sb.AppendLine().Append("(R² = ").Append(R2.ToString("G6")).Append(')').AppendLine();
        return sb.ToString();
    }
}

/// <summary>Represents the result of a linear regression from series.</summary>
public sealed class LinearSModel : LinearModelBase<Series>
{
    /// <summary>Initializes and computes a linear regression model.</summary>
    /// <param name="original">The series to be predicted.</param>
    /// <param name="predictors">The set of series used as predictors.</param>
    public LinearSModel(Series original, params Series[] predictors) : base(
        original.Prune(predictors.Select(s => s.Count).Min()),
        predictors.Select(s => s.Name).ToList())
    {
        int size = Original.Count;
        Vector[] rows = new Vector[predictors.Length + 1];
        rows[0] = new(size, 1.0);
        for (int i = 0; i < predictors.Length; i++)
        {
            double[] row = predictors[i].values;
            rows[i + 1] = new Vector(row.Length == size ? row : row[0..size]);
        }
        (Weights, Cholesky chol) = ComputeWeights(rows,
            new(original.Count == size ? original.values : original.values[0..size]));
        Prediction = Series.Combine(Weights, predictors);
        (TotalSumSquares, ResidualSumSquares, R2) =
            Original.GetValues().GetSumSquares(Prediction.GetValues());
        double s2 = ResidualSumSquares / (size - predictors.Length - 1);
        StandardError = Sqrt(s2);
        Matrix olsVariance = s2 * chol.Solve(Matrix.Identity(predictors.Length + 1));
        double[] tStats = new double[Weights.Length];
        for (int i = 0; i < Weights.Length; i++)
            tStats[i] = Weights[i] / Sqrt(olsVariance[i, i]);
        TStats = tStats;
    }
}

/// <summary>Represents the result of a linear regression from vectors.</summary>
public sealed class LinearVModel : LinearModelBase<Vector>
{
    /// <summary>Initializes and computes a linear regression model.</summary>
    /// <param name="original">Data to be predicted.</param>
    /// <param name="predictors">The set of vectors used as predictors.</param>
    public LinearVModel(Vector original, params Vector[] predictors) : base(
        original, Enumerable.Range(1, predictors.Length).Select(i => "v" + i).ToList())
    {
        int size = original.Length;
        if (predictors.Any(p => p.Length != size))
            throw new VectorLengthException();
        Vector[] rows = new Vector[predictors.Length + 1];
        rows[0] = new(size, 1.0);
        for (int i = 0; i < predictors.Length; i++)
            rows[i + 1] = predictors[i];
        (Weights, Cholesky chol) = ComputeWeights(rows, original);

        Prediction = Vector.Combine(Weights, predictors);
        (TotalSumSquares, ResidualSumSquares, R2) = Original.GetSumSquares(Prediction);
        double s2 = ResidualSumSquares / (size - predictors.Length - 1);
        StandardError = Sqrt(s2);
        Matrix olsVariance = s2 * chol.Solve(Matrix.Identity(predictors.Length + 1));
        double[] tStats = new double[Weights.Length];
        for (int i = 0; i < Weights.Length; i++)
            tStats[i] = Weights[i] / Sqrt(olsVariance[i, i]);
        TStats = tStats;
    }
}
