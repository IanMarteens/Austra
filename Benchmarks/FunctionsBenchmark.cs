namespace Benchmarks;

public class FunctionsBenchmark: BenchmarkControl
{
    private readonly double erfArg = 0.5;
    private readonly double gammaArg = 7.0;
    private readonly Vector512<double> x = Vector512.Create(1.0, 2.0, 3.0, 4.0, 5.0, 4, 3, 2);
    private readonly Vector256<double> y = Vector256.Create(1.0, 2.0, 3.0, 4.0);

    [Benchmark]
    public double AustraErf() => Functions.Erf(erfArg);

    [Benchmark]
    public double AustraGamma() => Functions.Gamma(gammaArg);

    [Benchmark]
    public (Vector512<double>, Vector512<double>) MsSinCos() => Vector512.SinCos(x);

    [Benchmark]
    public (Vector512<double>, Vector512<double>) AustraSinCos() => Austra.Library.Helpers.Simd.SinCos(x);
}
