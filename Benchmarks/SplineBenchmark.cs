namespace Benchmarks;

public class SplineBenchmark: BenchmarkControl
{
    [Benchmark]
    public VectorSpline AustraGrid() => new(0, Math.Tau, 1024, Math.Sin);
}
