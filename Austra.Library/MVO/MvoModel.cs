namespace Austra.Library.MVO;

/// <summary>Represents the result of a Mean-Variance Optimization.</summary>
public class MvoModel
{
    /// <summary>Creates and calculates a MVO model.</summary>
    /// <param name="returns">Expected returns.</param>
    /// <param name="covariance">Covariance of the assets.</param>
    /// <param name="lowerLimits">Lower limits for the weights.</param>
    /// <param name="upperLimits">Upper limits for the weights.</param>
    /// <param name="labels">Asset labels.</param>
    /// <exception cref="MatrixSizeException"></exception>
    /// <exception cref="VectorLengthException"></exception>
    public MvoModel(DVector returns, Matrix covariance, DVector lowerLimits, DVector upperLimits, string[] labels)
    {
        int len = returns.Length;
        if (returns.Length != covariance.Rows || !covariance.IsSquare)
            throw new MatrixSizeException($"Matrix should be {len}x{len}");
        if (lowerLimits.Length != len)
            throw new VectorLengthException("Invalid lower limits");
        if (upperLimits.Length != len)
            throw new VectorLengthException("Invalid upper limits");
        Returns = returns;
        Covariance = covariance;
        LowerLimits = lowerLimits;
        UpperLimits = upperLimits;
        if (labels == null || labels.Length == 0)
            labels = Enumerable.Range(0, len).Select(i => i.ToString()).ToArray();
        else if (labels.Length > len)
            labels = labels[0..len];
        else if (labels.Length < len)
        {
            string[] newLabels = new string[len];
            Array.Copy(labels, newLabels, labels.Length);
            for (int i = labels.Length; i < len; i++)
                newLabels[i] = i.ToString();
            labels = newLabels;
        }
        Labels = labels;
        Inputs input = new(Returns, []);
        input.SetCovariance(Covariance);
        input.SetLowerBoundaries(LowerLimits);
        input.SetUpperBoundaries(UpperLimits);
        Portfolios = Optimizer.GetEfficientFrontier(input);
    }

    /// <summary>Add constraints to the model.</summary>
    /// <param name="constraintLHS">A matrix of Constraints * Securities size.</param>
    /// <param name="constraintRHS">A vector of Constraints size.</param>
    /// <param name="constraintTypes">One constraint sign for each constraint.</param>
    /// <returns>The updated model with a recalculated efficient frontier.</returns>
    public MvoModel SetConstraints(Matrix constraintLHS, DVector constraintRHS, NVector constraintTypes)
    {
        if (constraintLHS.Cols != Returns.Length)
            throw new MatrixSizeException($"Matrix should be {constraintLHS.Rows}x{Returns.Length}");
        if (constraintRHS.Length != constraintLHS.Rows)
            throw new VectorLengthException($"Vector length should be {constraintLHS.Rows}");
        if (constraintRHS.Length != constraintTypes.Length)
            throw new VectorLengthException($"The must be a constraint type for each constraint");
        Inputs input = new(Returns, constraintTypes);
        input.SetCovariance(Covariance);
        input.SetLowerBoundaries(LowerLimits);
        input.SetUpperBoundaries(UpperLimits);
        input.SetConstraints(constraintLHS, constraintRHS);
        Portfolios = Optimizer.GetEfficientFrontier(input);
        return this;
    }

    /// <summary>Add equality constraints to the model.</summary>
    /// <param name="constraintLHS">A matrix of Constraints * Securities size.</param>
    /// <param name="constraintRHS">A vector of Constraints size.</param>
    /// <returns>The updated model with a recalculated efficient frontier.</returns>
    public MvoModel SetConstraints(Matrix constraintLHS, DVector constraintRHS) =>
        SetConstraints(constraintLHS, constraintRHS, new NVector(constraintRHS.Length));

    /// <summary>Creates and calculates a MVO model.</summary>
    /// <param name="returns">Expected returns.</param>
    /// <param name="covariance">Covariance of the assets.</param>
    /// <param name="lowerLimits">Lower limits for the weights.</param>
    /// <param name="upperLimits">Upper limits for the weights.</param>
    /// <param name="series">A list of series to provide the asset names.</param>
    public MvoModel(DVector returns, Matrix covariance, DVector lowerLimits, DVector upperLimits, params Series[] series) :
        this(returns, covariance, lowerLimits, upperLimits, series.Select(s => s.Name).ToArray())
    { }

    /// <summary>Creates and calculates a MVO model.</summary>
    /// <param name="returns">Expected returns.</param>
    /// <param name="covariance">Covariance of the assets.</param>
    /// <param name="lowerLimits">Lower limits for the weights.</param>
    /// <param name="upperLimits">Upper limits for the weights.</param>
    public MvoModel(DVector returns, Matrix covariance, DVector lowerLimits, DVector upperLimits) :
        this(returns, covariance, lowerLimits, upperLimits, Array.Empty<string>())
    { }

    /// <summary>Creates and calculates a MVO model.</summary>
    /// <param name="returns">Expected returns.</param>
    /// <param name="covariance">Covariance of the assets.</param>
    /// <param name="labels">Asset labels.</param>
    public MvoModel(DVector returns, Matrix covariance, params string[] labels) :
        this(returns, covariance, new DVector(returns.Length, 0.0), new DVector(returns.Length, 1.0), labels)
    { }

    /// <summary>Creates and calculates a MVO model.</summary>
    /// <param name="returns">Expected returns.</param>
    /// <param name="covariance">Covariance of the assets.</param>
    /// <param name="series">A list of series to provide the asset names.</param>
    public MvoModel(DVector returns, Matrix covariance, params Series[] series) :
        this(returns, covariance, new DVector(returns.Length, 0.0), new DVector(returns.Length, 1.0), series)
    { }

    /// <summary>Creates and calculates a MVO model.</summary>
    /// <param name="returns">Expected returns.</param>
    /// <param name="covariance">Covariance of the assets.</param>
    public MvoModel(DVector returns, Matrix covariance) :
        this(returns, covariance, new DVector(returns.Length, 0.0), new DVector(returns.Length, 1.0), Array.Empty<string>())
    { }

    /// <summary>Creates and calculates a MVO model.</summary>
    /// <param name="returns">Expected returns.</param>
    /// <param name="series">A list of series to provide the asset names and the covariance matrix.</param>
    public MvoModel(DVector returns, params Series[] series) :
        this(returns, Series.CovarianceMatrix(series),
            new DVector(returns.Length, 0.0), new DVector(returns.Length, 1.0),
            series)
    { }

    /// <summary>Number of assets in the model.</summary>
    public int Size => Returns.Length;

    /// <summary>Expected returns.</summary>
    public DVector Returns { get; }
    /// <summary>The covariance matrix.</summary>
    public Matrix Covariance { get; }
    /// <summary>Lower limits for the weights.</summary>
    public DVector LowerLimits { get; }
    /// <summary>Upper limits for the weights.</summary>
    public DVector UpperLimits { get; }
    /// <summary>The name of the assets.</summary>
    public string[] Labels { get; }
    /// <summary>Portfolios in the efficient frontier.</summary>
    public Portfolio[] Portfolios { get; private set; } = [];

    /// <summary>Gets the number of portfolios in the efficient frontier.</summary>
    public int Length => Portfolios.Length;

    /// <summary>Gets a portfolio in the efficient frontier.</summary>
    /// <param name="index">The portfolio's position.</param>
    public Portfolio this[int index] => Portfolios[index];
    /// <summary>Gets a portfolio in the efficient frontier.</summary>
    /// <param name="index">The portfolio's position.</param>
    public Portfolio this[Index index] => Portfolios[index];
    /// <summary>Gets the first portfolio in the efficient frontier.</summary>
    public Portfolio First => this[0];
    /// <summary>Gets the last portfolio in the efficient frontier.</summary>
    public Portfolio Last => this[^1];

    /// <summary>Gets a textual representation of the models.</summary>
    /// <returns>A list with all portfolios in the efficient frontier.</returns>
    public override string ToString()
    {
        var sb = new StringBuilder($"MVO Model ({Size} assets)")
            .AppendLine();
        foreach (Portfolio portfolio in Portfolios)
            sb.AppendLine(portfolio.ToString());
        return sb.ToString();
    }
}
