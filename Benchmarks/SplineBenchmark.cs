namespace Benchmarks;

public class SplineBenchmark: BenchmarkControl
{
    private readonly DVector v;

    public SplineBenchmark() : base() => v = new DVector(10, new NormalRandom());

    [Benchmark]
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1822 // Mark members as static
    public VectorSpline AustraGrid() => new(0, Math.Tau, 1024, Math.Sin);

    [Benchmark]
    public Complex AustraPolyDer() => Polynomials.PolyDerivative(new Complex(2.1, 0.1), v);

    [Benchmark]
    public CVector AustraPolySolve3() => Polynomials.PolySolve(1, 3, -1, -3);
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0079 // Remove unnecessary suppression
}
