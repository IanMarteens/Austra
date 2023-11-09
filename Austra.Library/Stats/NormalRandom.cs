namespace Austra.Library.Stats;

/// <summary>Generates values from the standard normal distribution.</summary>
public sealed class NormalRandom
{
    /// <summary>A shared instance of the generator using a randomized seed.</summary>
    [ThreadStatic]
    private static NormalRandom? shared;

    /// <summary>Source for the uniform distribution in [0, 1).</summary>
    private readonly Random random;
    /// <summary>Mean of the distribution.</summary>
    private readonly double mean = 0.0;
    /// <summary>Standard deviation of the distribution.</summary>
    private readonly double stdDev = 1.0;
    /// <summary>Pending value to return.</summary>
    private double item;
    /// <summary>Do we have a pending value to return.</summary>
    private bool hasItem;

    /// <summary>Creates a generator using a randomized seed.</summary>
    public NormalRandom() => random = new();

    /// <summary>Creates a generator using a randomized seed.</summary>
    /// <param name="mean">Mean of the distribution.</param>
    /// <param name="stdDev">Standard deviation.</param>
    public NormalRandom(double mean, double stdDev) =>
        (random, this.mean, this.stdDev) = (new(), mean, stdDev);

    /// <summary>Creates a generator using a seed.</summary>
    /// <param name="seed">The seed of the generator.</param>
    public NormalRandom(int seed) => random = new(seed);

    /// <summary>Creates a generator using a seed.</summary>
    /// <param name="seed">The seed of the generator.</param>
    /// <param name="mean">Mean of the distribution.</param>
    /// <param name="stdDev">Standard deviation.</param>
    public NormalRandom(int seed, double mean, double stdDev) =>
        (random, this.mean, this.stdDev) = (new(seed), mean, stdDev);

    /// <summary>
    /// Creates a generator from a uniform distribution generator.
    /// </summary>
    /// <param name="random">A generator for the uniform distribution.</param>
    public NormalRandom(Random random) => this.random = random;

    /// <summary>
    /// Creates a generator from a uniform distribution generator.
    /// </summary>
    /// <param name="random">A generator for the uniform distribution.</param>
    /// <param name="mean">Mean of the distribution.</param>
    /// <param name="stdDev">Standard deviation.</param>
    public NormalRandom(Random random, double mean, double stdDev) => 
        (this.random, this.mean, this.stdDev) = (random, mean, stdDev);

    /// <summary>A shared instance of the generator using a randomized seed.</summary>
    public static NormalRandom Shared => shared ??= new(Random.Shared);

    /// <summary>
    /// Returns a random value according to the standard normal distribution.
    /// </summary>
    /// <returns>A value from the standard normal distribution.</returns>
    public double NextDouble()
    {
        if (hasItem)
        {
            hasItem = false;
            return item;
        }
        hasItem = true;
        double u = Log(1 - random.NextDouble());
        double r = Sqrt(-u - u) * stdDev;
        double v = Tau * random.NextDouble();
        item = FusedMultiplyAdd(Sin(v), r, mean);
        return FusedMultiplyAdd(Cos(v), r, mean);
    }

    /// <summary>
    /// Returns a couple of random values according to the standard normal distribution.
    /// </summary>
    /// <returns>>Two values from the standard normal distribution.</returns>
    public (double, double) NextDoubles()
    {
        double u = Log(1 - random.NextDouble());
        double r = Sqrt(-u - u) * stdDev;
        double v = Tau * random.NextDouble();       
        return (FusedMultiplyAdd(Sin(v), r, mean), FusedMultiplyAdd(Cos(v), r, mean));
    }

    /// <summary>
    /// Returns a couple of random values according to the standard normal distribution.
    /// </summary>
    /// <param name="target">A reference to a tuple to store the values.</param>
    public void NextDoubles(ref double target)
    {
        double u = Log(1 - random.NextDouble());
        double r = Sqrt(-u - u) * stdDev;
        double v = Tau * random.NextDouble();
        target = FusedMultiplyAdd(Sin(v), r, mean);
        Add(ref target, 1) = FusedMultiplyAdd(Cos(v), r, mean);
    }

    /// <summary>
    /// Returns a couple of random values according to the standard normal distribution.
    /// </summary>
    /// <param name="target">A reference to a tuple to store the values.</param>
    public unsafe void NextDoubles(double* target)
    {
        double u = Log(1 - random.NextDouble());
        double r = Sqrt(-u - u) * stdDev;
        double v = Tau * random.NextDouble();
        *target = FusedMultiplyAdd(Sin(v), r, mean);
        *(target + 1) = FusedMultiplyAdd(Cos(v), r, mean);
    }
}
