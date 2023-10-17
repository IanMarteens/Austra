namespace Benchmarks;

public class FunctionsBenchmark: BenchmarkControl
{
    private readonly double erfArg = 0.5;
    private readonly double gammaArg = 7.0;

    [Benchmark]
    public double AustraErf() => Functions.Erf(erfArg);

    [Benchmark]
    public double AustraGamma() => Functions.Gamma(gammaArg);
}
