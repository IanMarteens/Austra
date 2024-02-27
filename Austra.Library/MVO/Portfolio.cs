namespace Austra.Library.MVO;

/// <summary>Represents a portfolio in the efficient frontier.</summary>
/// <param name="weights">Asset weights.</param>
/// <param name="lambda">Risk tolerance.</param>
/// <param name="mean">Expected return.</param>
/// <param name="variance">Expected volatility.</param>
public class Portfolio(
    DVector weights,
    double lambda,
    double mean,
    double variance)
{
    /// <summary>Asset weights.</summary>
    public DVector Weights { get; } = weights;
    /// <summary>Risk tolerance.</summary>
    public double Lambda { get; } = lambda;
    /// <summary>Expected return.</summary>
    public double Mean { get; } = mean;
    /// <summary>Expected volatility.</summary>
    public double Variance { get; } = variance;
    /// <summary>Standard deviation.</summary>
    public double StdDev { get; } = Sqrt(variance);

    /// <summary>Calculates the Sharpe ratio of this portfolio.</summary>
    /// <param name="riskFreeReturn">The risk free return.</param>
    /// <returns>The reward-to-variability ratio.</returns>
    public double GetSharpeRatio(double riskFreeReturn) => (Mean - riskFreeReturn) / Variance;

    /// <summary>Gets a textual representation of the portfolio.</summary>
    /// <returns>Tab-separated information from the portfolio.</returns>
    public override string ToString() =>
        $"{Mean:F5}\t{StdDev:F5}\t{Lambda:F5}\t" +
        $"{string.Join('\t', Weights.Select(d => d.ToString("F3")))}";

    /// <summary>Gets a textual representation of the portfolio.</summary>
    /// <returns>Tab-separated information from the portfolio.</returns>
    public string ToLongString() =>
        $"{Mean}\t{StdDev}\t{Variance}\t{Lambda}\t" +
        $"{string.Join('\t', Weights.Select(d => d.ToString()))}";
}

/// <summary>Represents a portfolio interpolated from two turning points.</summary>
public sealed class InterpolatedPortfolio : Portfolio
{
    /// <summary>First index in the efficient frontier.</summary>
    public int SourceIndex1 { get; }
    /// <summary>Second index in the efficient frontier.</summary>
    public int SourceIndex2 { get; }

    /// <summary>Creates an interpolated portfolio from a single source.</summary>
    /// <param name="portfolio">The source portfolio.</param>
    /// <param name="sourceIndex">The source index.</param>
    public InterpolatedPortfolio(
        Portfolio portfolio,
        int sourceIndex) :
        base(portfolio.Weights, portfolio.Lambda, portfolio.Mean, portfolio.Variance) =>
        (SourceIndex1, SourceIndex2) = (sourceIndex, sourceIndex);

    /// <summary>Creates an interpolated portfolio from two turning points.</summary>
    /// <param name="weights">Effective weights.</param>
    /// <param name="mean">Effective return.</param>
    /// <param name="variance">Effective variance.</param>
    /// <param name="sourceIndex1">First source index.</param>
    /// <param name="sourceIndex2">Second source index.</param>
    public InterpolatedPortfolio(
        double[] weights,
        double mean,
        double variance,
        int sourceIndex1,
        int sourceIndex2) : base(weights, -1, mean, variance) =>
        (SourceIndex1, SourceIndex2) = (sourceIndex1, sourceIndex2);

    /// <summary>Interpolates a portfolio from two efficient portfolios.</summary>
    /// <param name="frontier">The efficient frontier array.</param>
    /// <param name="sigma">The covariance matrix.</param>
    /// <param name="interpolationFactor">The interpolation factor.</param>
    /// <param name="lowIndex">Lowest source index.</param>
    /// <param name="highIndex">Highest source index.</param>
    internal static InterpolatedPortfolio Interpolate(
        Portfolio[] frontier,
        Matrix sigma,
        double interpolationFactor,
        int lowIndex,
        int highIndex)
    {
        Portfolio lowP = frontier[lowIndex];
        Portfolio highP = frontier[highIndex];
        double g = 1 - interpolationFactor;
        double[] weights = new double[highP.Weights.Length];
        for (int i = 0; i < weights.Length; i++)
            weights[i] = g * highP.Weights.UnsafeThis(i) +
                interpolationFactor * lowP.Weights.UnsafeThis(i);
        return new(
            weights: weights,
            mean: g * highP.Mean + interpolationFactor * lowP.Mean,
            variance: (sigma * weights) * weights,
            sourceIndex1: lowIndex,
            sourceIndex2: highIndex);
    }
}
