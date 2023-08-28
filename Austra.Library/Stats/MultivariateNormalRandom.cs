namespace Austra.Library;

/// <summary>Generates a normal multivariate distribution.</summary>
public sealed class MultivariateNormalRandom
{
    /// <summary>The mean vector.</summary>
    private readonly Vector mean;
    /// <summary>Cholesky decomposition of the covariance matrix.</summary>
    private readonly Cholesky cholesky;
    /// <summary>Storage for the initial distribution.</summary>
    private readonly double[] source;
    /// <summary>Storage for the sample.</summary>
    private readonly double[] result;
    /// <summary>Generates a standard normal distribution.</summary>
    private readonly NormalRandom random;

    /// <summary>Creates a multivariate generator.</summary>
    /// <param name="seed">Seed for the scalar generator.</param>
    /// <param name="mean">The mean vector.</param>
    /// <param name="covariance">The covariance matrix.</param>
    public MultivariateNormalRandom(int seed, Vector mean, Matrix covariance)
    {
        Contract.Requires(mean.IsInitialized);
        Contract.Requires(covariance.IsInitialized);
        Contract.Requires(covariance.IsSquare);
        Contract.Requires(covariance.Rows == mean.Length);

        this.mean = mean;
        cholesky = covariance.Cholesky();
        source = new double[mean.Length];
        result = new double[mean.Length];
        random = new(seed);
    }

    /// <summary>Creates a multivariate generator with a randomized seed.</summary>
    /// <param name="mean">The mean vector.</param>
    /// <param name="covariance">The covariance matrix.</param>
    public MultivariateNormalRandom(Vector mean, Matrix covariance)
    {
        Contract.Requires(mean.IsInitialized);
        Contract.Requires(covariance.IsInitialized);
        Contract.Requires(covariance.IsSquare);
        Contract.Requires(covariance.Rows == mean.Length);

        this.mean = mean;
        cholesky = covariance.Cholesky();
        source = new double[mean.Length];
        result = new double[mean.Length];
        random = new();
    }

    /// <summary>Retrieves the next vector from the distribution.</summary>
    /// <returns>A vector drawn from a multivariate normal distribution.</returns>
    public Vector Next()
    {
        for (int i = 0; i < source.Length; i++)
            source[i] = random.NextDouble();
        return cholesky.L.MultiplyAdd(source, mean, result);
    }
}
