namespace Austra.Library.MVO;

/// <summary>Mean variance optimizer.</summary>
/// <remarks>Interface methods.</remarks>
public static class Optimizer
{
    /// <summary>Close enough to zero.</summary>
    public const double ε = 1E-08;

    /// <summary>Executes the mean-variance optimizer in order
    /// to find the efficient frontier.</summary>
    /// <param name="input">Contains all data for the algorithm.</param>
    /// <returns>A list of portfolio weights plus the associated lambda.</returns>
    public static Portfolio[] GetEfficientFrontier(Inputs input) =>
        Markowitz.Optimize(input);

    /// <summary>
    /// Looks at the efficient frontier for an efficient portfolio with
    /// an expected return.
    /// </summary>
    /// <param name="efficientFrontier">The list of efficient portfolios.</param>
    /// <param name="sigma">The covariance matrix.</param>
    /// <param name="expectedReturn">The expected return.</param>
    /// <returns>An interpolated portfolio, or null, if it does not exist.</returns>
    public static InterpolatedPortfolio? GetTargetReturnEfficientPortfolio(
        Portfolio[] efficientFrontier,
        Matrix sigma,
        double expectedReturn)
    {
        Contract.Requires(sigma.IsInitialized);
        Contract.Requires(sigma.IsSquare);

        if (efficientFrontier == null || efficientFrontier.Length == 0)
            return null;
        if (Abs(efficientFrontier[0].Mean - expectedReturn) < ε)
            return new(efficientFrontier[0], 0);
        if (Abs(efficientFrontier[^1].Mean - expectedReturn) < ε)
            return new(efficientFrontier[^1], efficientFrontier.Length - 1);
        if (expectedReturn > efficientFrontier[0].Mean ||
            expectedReturn < efficientFrontier[^1].Mean)
            return null;
        int low = 0, high = efficientFrontier.Length - 1;
        while (high - low != 1)
        {
            int middle = (low + high) / 2;
            Portfolio p = efficientFrontier[middle];
            double δ = p.Mean - expectedReturn;
            if (δ > ε)
                low = middle;
            else if (δ < -ε)
                high = middle;
            else
                return new(p, middle);
        }
        Portfolio lowP = efficientFrontier[low];
        Portfolio highP = efficientFrontier[high];
        double f = (expectedReturn - highP.Mean) / (lowP.Mean - highP.Mean);
        return InterpolatedPortfolio.Interpolate(
            frontier: efficientFrontier,
            sigma: sigma,
            interpolationFactor: f,
            lowIndex: low,
            highIndex: high);
    }

    /// <summary>
    /// Looks at the efficient frontier for an efficient portfolio with
    /// an expected volatility.
    /// </summary>
    /// <param name="efficientFrontier">The list of efficient portfolios.</param>
    /// <param name="sigma">The covariance matrix.</param>
    /// <param name="expectedVolatility">The expected volatility.</param>
    /// <returns>An interpolated portfolio, or null, if it does not exist.</returns>
    public static InterpolatedPortfolio? GetTargetVolatilityEfficientPortfolio(
        Portfolio[] efficientFrontier,
        Matrix sigma,
        double expectedVolatility)
    {
        Contract.Requires(sigma.IsInitialized);
        Contract.Requires(sigma.IsSquare);
        Contract.Requires(expectedVolatility > 0);

        if (efficientFrontier == null || efficientFrontier.Length == 0)
            return null;
        if (Abs(efficientFrontier[0].Variance - expectedVolatility) < ε)
            return new(efficientFrontier[0], 0);
        if (Abs(efficientFrontier[^1].Variance - expectedVolatility) < ε)
            return new(efficientFrontier[^1], efficientFrontier.Length - 1);
        if (expectedVolatility > efficientFrontier[0].Variance ||
            expectedVolatility < efficientFrontier[^1].Variance)
            return null;
        int low = 0, high = efficientFrontier.Length - 1;
        while (high - low != 1)
        {
            int middle = (low + high) / 2;
            Portfolio p = efficientFrontier[middle];
            double δ = p.Variance - expectedVolatility;
            if (δ > ε)
                low = middle;
            else if (δ < -ε)
                high = middle;
            else
                return new(p, middle);
        }
        Portfolio lowP = efficientFrontier[low];
        Portfolio highP = efficientFrontier[high];
        double crossV = (sigma * lowP.Weights) * highP.Weights;
        double varMax = lowP.Variance * lowP.Variance;
        double varMin = highP.Variance * highP.Variance;
        double a = varMin + varMax - crossV - crossV;
        double b = crossV - varMax;
        double c = varMax - expectedVolatility * expectedVolatility;
        double discr = b * b - a * c;
        if (discr < 0)
            return null;
        double q = -(b + Sign(b) * Sqrt(discr));
        double f = q / a;
        if (f < 0 || f > 1)
        {
            f = c / q;
            if (f < 0 || f > 1)
                return null;
        }
        return InterpolatedPortfolio.Interpolate(
            frontier: efficientFrontier,
            sigma: sigma,
            interpolationFactor: 1 - f,
            lowIndex: low,
            highIndex: high);
    }

    /// <summary>
    /// Looks at the efficient frontier for an efficient portfolio with
    /// minimum variance.
    /// </summary>
    /// <param name="efficientFrontier">The list of efficient portfolios.</param>
    /// <returns>The last portfolio in the efficient frontier collection.</returns>
    public static InterpolatedPortfolio GetMinimumVarianceEfficientPortfolio(
        Portfolio[] efficientFrontier) =>
        new(efficientFrontier[^1], efficientFrontier.Length - 1);

    /// <summary>Gets the efficient portfolio with maximum Sharpe ratio.</summary>
    /// <param name="efficientFrontier">The list of efficient portfolios.</param>
    /// <param name="sigma">The covariance matrix.</param>
    /// <param name="riskFreeReturn">The risk free return.</param>
    /// <returns>An interpolated portfolio, or null, if it does not exist.</returns>
    public static InterpolatedPortfolio? GetMaxSharpeRatio(
        Portfolio[] efficientFrontier,
        Matrix sigma,
        double riskFreeReturn)
    {
        Contract.Requires(sigma.IsInitialized);
        Contract.Requires(sigma.IsSquare);
        Contract.Requires(riskFreeReturn > 0);

        if (efficientFrontier == null ||
            efficientFrontier.Length == 0 ||
            efficientFrontier[0].Mean - riskFreeReturn < 0)
            return null;
        Portfolio[] frontier = (Portfolio[])efficientFrontier.Clone();
        if (frontier[^1].Mean - riskFreeReturn < 0)
        {
            InterpolatedPortfolio? ip = GetTargetReturnEfficientPortfolio(
                frontier, sigma, riskFreeReturn + ε);
            if (ip == null)
                return null;
            frontier[ip.SourceIndex2] = ip;
            frontier = frontier.Take(ip.SourceIndex2 + 1).ToArray();
        }
        if (frontier.Length == 1)
            return new(frontier[0], 0);
        int middle = Search();
        InterpolatedPortfolio result = new(
            portfolio: frontier[middle],
            sourceIndex: middle);
        double ratio = result.GetSharpeRatio(riskFreeReturn);
        if (middle + 1 < frontier.Length)
        {
            InterpolatedPortfolio? p = Interpolate(middle + 1, middle);
            if (p != null)
            {
                double r = p.GetSharpeRatio(riskFreeReturn);
                if (r > ratio)
                {
                    result = p;
                    ratio = r;
                }
            }
        }
        if (middle - 1 >= 0)
        {
            InterpolatedPortfolio? p = Interpolate(middle, middle - 1);
            if (p != null && p.GetSharpeRatio(riskFreeReturn) > ratio)
                result = p;
        }
        return result;

        InterpolatedPortfolio? Interpolate(int min, int max)
        {
            Portfolio pMin = frontier[min];
            double sharpeMin = pMin.GetSharpeRatio(riskFreeReturn);
            double retMin = pMin.Mean;
            double volMin = pMin.Variance;
            double varMin = volMin * volMin;

            Portfolio pMax = frontier[max];
            double sharpeMax = pMax.GetSharpeRatio(riskFreeReturn);
            double retMax = pMax.Mean;
            double volMax = pMax.Variance;
            double varMax = volMax * volMax;

            double retMinMax = retMin - retMax;
            double retMaxRf = retMax - riskFreeReturn;
            double a = retMinMax * retMinMax;
            double b = 2 * retMinMax * retMaxRf;
            double c = retMaxRf * retMaxRf;

            double varCross = (sigma * pMin.Weights) * pMax.Weights;
            var d = varMin + varMax - 2 * varCross;
            double e = -2 * (varMax - varCross);

            double aa = a * e - b * d;
            double bb = a * varMax - c * d;
            double cc = b * varMax - c * e;

            double discr = bb * bb - aa * cc;
            if (discr < 0)
                return null;

            double qq = -(bb + Sign(bb) * Sqrt(discr));
            double t1 = qq / aa;
            double t2 = cc / qq;

            InterpolatedPortfolio result = sharpeMin > sharpeMax
                ? new(pMin, min) : new(pMax, max);

            if (t1 > 0 && t1 < 1)
            {
                InterpolatedPortfolio p1 = InterpolatedPortfolio.Interpolate(
                    frontier: frontier,
                    sigma: sigma,
                    interpolationFactor: t1,
                    lowIndex: min,
                    highIndex: max);
                if (p1.GetSharpeRatio(riskFreeReturn) > result.GetSharpeRatio(riskFreeReturn))
                    result = p1;
            }

            if (t2 > 0 && t2 < 1)
            {
                InterpolatedPortfolio p2 = InterpolatedPortfolio.Interpolate(
                    frontier: frontier,
                    sigma: sigma,
                    interpolationFactor: t2,
                    lowIndex: min,
                    highIndex: max);
                if (p2.GetSharpeRatio(riskFreeReturn) > result.GetSharpeRatio(riskFreeReturn))
                    result = p2;
            }
            return result;
        }

        int Search()
        {
            int low = 0, high = frontier.Length - 1;
            while (high - low != 1)
            {
                int middle = (low + high) / 2;
                Portfolio p = frontier[middle];
                double sr = p.GetSharpeRatio(riskFreeReturn);
                Portfolio p1 = frontier[middle + 1];
                double sr1 = p1.GetSharpeRatio(riskFreeReturn);
                if (sr > sr1)
                    high = middle;
                else if (sr < sr1)
                    low = middle;
                else
                    return middle + 1;
            }
            return high;
        }
    }
}
