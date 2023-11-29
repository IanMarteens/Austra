namespace Benchmarks;

public class SplineBenchmark: BenchmarkControl
{
    private readonly DVector v;

    public SplineBenchmark() : base()
    {
        v = new DVector(10, new NormalRandom());    
    }

    [Benchmark]
    public VectorSpline AustraGrid() => new(0, Math.Tau, 1024, Math.Sin);

    [Benchmark]
    public Complex AustraPolyDer() => Polynomials.PolyDerivative(new Complex(2.1, 0.1), v);

    [Benchmark]
    public CVector AustraPolySolve3() => Polynomials.PolySolve(1, 3, -1, -3);
}
