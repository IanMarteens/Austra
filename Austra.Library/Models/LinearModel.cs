namespace Austra.Library;

/// <summary>Represents the result of a linear regression.</summary>
/// <typeparam name="T">The type of the data source.</typeparam>
public abstract class LinearModel<T>: IFormattable
{
    /// <summary>Initializes and computes a linear regression model.</summary>
    /// <param name="original">The samples to be predicted.</param>
    /// <param name="variables">The names of the samples used as predictors.</param>
    protected LinearModel(T original, IReadOnlyList<string> variables) => 
        (Original, Variables, Prediction) = (original, variables, default!);

    /// <summary>The samples to be explained.</summary>
    public T Original { get; }
    /// <summary>Predicted samples.</summary>
    public T Prediction { get; protected set; }

    /// <summary>The names of the samples used as predictors.</summary>
    public IReadOnlyList<string> Variables { get; }

    /// <summary>Calculated weights for the predictors.</summary>
    public DVector Weights { get; protected set; }
    /// <summary>Student-t statistics for the weights.</summary>
    public DVector TStats { get; protected set; }

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
    protected (DVector, Cholesky) ComputeWeights(DVector[] mRows, DVector rightSide)
    {
        Matrix x = new(mRows);
        Cholesky c = x.MultiplyTranspose(x).Cholesky();
        return (c.Solve(x * rightSide), c);
    }

    /// <summary>Gets a textual representation of the model.</summary>
    /// <returns>The calculated lineal combination, and the R² statistics.</returns>
    public sealed override string ToString() => ToString("G6", null);

    /// <summary>Gets the string representation of the autoregressive model.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>The calculated lineal combination, and the R² statistics.</returns>
    public string ToString(string? format, IFormatProvider? provider)
    {
        const double ε = 1E-15;
        StringBuilder sb = new(1024);
        sb.Append("Original = ");
        double w0 = Weights.UnsafeThis(0);
        if (Abs(w0) > ε)
            sb.Append(w0.ToString(format, provider));
        for (int i = 0; i < Variables.Count; i++)
        {
            double w = Weights.UnsafeThis(i + 1), aw = Abs(w);
            if (aw <= ε)
                continue;
            if (sb.Length > 0)
            {
                sb.Append(w < 0 ? " - " : " + ");
                if (Abs(aw - 1) <= ε)
                    sb.Append(Variables[i]);
                else
                    sb.Append(aw.ToString(format, provider)).Append(" * ").Append(Variables[i]);
            }
            else if (Abs(aw - 1) <= ε)
                sb.Append(Variables[i]);
            else
                sb.Append(w.ToString(format, provider)).Append(" * ").Append(Variables[i]);
        }
        sb.AppendLine()
            .Append("TStats: ").Append(Vec.ToString((double[])TStats, d => d.ToString(format, provider)))
            .Append("R² = ").Append(R2.ToString(format, provider)).AppendLine();
        return sb.ToString();
    }
}

/// <summary>Represents the result of a linear regression from series.</summary>
public sealed class LinearSModel : LinearModel<Series>
{
    /// <summary>Initializes and computes a linear regression model.</summary>
    /// <param name="original">The series to be predicted.</param>
    /// <param name="predictors">The set of series used as predictors.</param>
    public LinearSModel(Series original, params Series[] predictors) : base(
        original.Prune(predictors.Select(s => s.Count).Min()),
        [.. predictors.Select(s => s.Name)])
    {
        int size = Original.Count;
        DVector[] rows = new DVector[predictors.Length + 1];
        rows[0] = new(size, 1.0);
        for (int i = 0; i < predictors.Length; i++)
        {
            double[] row = predictors[i].values;
            rows[i + 1] = new DVector(row.Length == size ? row : row[0..size]);
        }
        (Weights, Cholesky chol) = ComputeWeights(rows,
            new(original.Count == size ? original.values : original.values[0..size]));
        Prediction = Series.Combine(Weights, predictors);
        (TotalSumSquares, ResidualSumSquares, R2) =
            Original.Values.GetSumSquares(Prediction.Values);
        double s2 = ResidualSumSquares / (size - predictors.Length - 1);
        StandardError = Sqrt(s2);
        Matrix olsVariance = s2 * chol.Solve(Matrix.Identity(predictors.Length + 1));
        double[] tStats = new double[Weights.Length];
        for (int i = 0; i < Weights.Length; i++)
            tStats[i] = Weights.UnsafeThis(i) / Sqrt(olsVariance[i, i]);
        TStats = tStats;
    }
}

/// <summary>Represents the result of a linear regression from vectors.</summary>
public sealed class LinearVModel : LinearModel<DVector>
{
    /// <summary>Initializes and computes a linear regression model.</summary>
    /// <param name="original">Data to be predicted.</param>
    /// <param name="predictors">The set of vectors used as predictors.</param>
    public LinearVModel(DVector original, params DVector[] predictors) : base(
        original, [.. Enumerable.Range(1, predictors.Length).Select(i => "v" + i)])
    {
        int size = original.Length;
        if (predictors.Any(p => p.Length != size))
            throw new VectorLengthException();
        DVector[] rows = new DVector[predictors.Length + 1];
        rows[0] = new(size, 1.0);
        for (int i = 0; i < predictors.Length; i++)
            rows[i + 1] = predictors[i];
        (Weights, Cholesky chol) = ComputeWeights(rows, original);

        Prediction = DVector.Combine(Weights, predictors);
        (TotalSumSquares, ResidualSumSquares, R2) = Original.GetSumSquares(Prediction);
        double s2 = ResidualSumSquares / (size - predictors.Length - 1);
        StandardError = Sqrt(s2);
        Matrix olsVariance = s2 * chol.Solve(Matrix.Identity(predictors.Length + 1));
        double[] tStats = new double[Weights.Length];
        for (int i = 0; i < Weights.Length; i++)
            tStats[i] = Weights.UnsafeThis(i) / Sqrt(olsVariance[i, i]);
        TStats = tStats;
    }
}
