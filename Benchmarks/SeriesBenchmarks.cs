namespace Benchmarks;

public class SeriesBenchmark : BenchmarkControl
{
    private readonly Series aapl, msft, dax;
    private readonly Vector weights;

    public SeriesBenchmark()
    {
        Configure();
        NormalRandom nr = new(1964);
        Vector ar1 = new(1000,
            (i, v) => nr.NextDouble() + 0.7 * v.SafeThis(i - 1) + 0.1 * v.SafeThis(i - 2));
        Vector ar2 = new(1000,
            (i, v) => nr.NextDouble() + 0.4 * v.SafeThis(i - 1) + 0.4 * v.SafeThis(i - 2));
        Vector ar3 = new(1000,
            (i, v) => nr.NextDouble() + 0.1 * v.SafeThis(i - 1) + 0.7 * v.SafeThis(i - 2));
        Date[] args = Enumerable.Range(0, 1000).Select(i => new Date(2019, 1, 1) + i).ToArray();
        aapl = new Series("AAPL", null, args, (double[])ar1, SeriesType.Raw, Frequency.Daily);
        msft = new Series("MSFT", null, args, (double[])ar2, SeriesType.Raw, Frequency.Daily);
        dax = new Series("DAX", null, args, (double[])ar3, SeriesType.Raw, Frequency.Daily);
        weights = new(new double[] { 0.5, 0.3, 0.2 });
    }

    [Benchmark]
    public Series AustraCnvRets() =>
        aapl.AsReturns();

    [Benchmark]
    public Series AustraCnvLogs() =>
        aapl.AsLogReturns();

    [Benchmark]
    public Series AustraRawCombine() =>
        0.5 * aapl + 0.3 * msft + 0.2 * dax;

    [Benchmark]
    public Series AustraOptCombine() =>
        Series.Combine(weights, aapl, msft, dax);
}
