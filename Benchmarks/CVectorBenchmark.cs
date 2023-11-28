namespace Benchmarks;

public class CVectorBenchmark : BenchmarkControl
{
    private readonly Complex[] cv = (Complex[])new CVector(1023, new Random());
    private readonly CVector cxv = new(1023, new Random(33));
    private readonly CVector cyv = new(1023, new Random(34));
    private readonly int size = 1023;
    private readonly double scale = Random.Shared.NextDouble() + 0.5;

    public CVectorBenchmark() { }

    [Benchmark]
    public CVector AustraComplexVectorCtor() => new(cv);

    [Benchmark]
    public double AustraComplexVectorSquared() => cxv.Squared();

    [Benchmark]
    public Vector AustraComplexVectorMagnitudes() => cxv.Magnitudes();

    [Benchmark]
    public Vector AustraComplexVectorPhases() => cxv.Phases();

    [Benchmark]
    public CVector AustraComplexVectorScale() => cxv * scale;

    [Benchmark]
    public CVector AustraComplexVectorMap() => cxv.Map(c => new(c.Imaginary, c.Real));

    [Benchmark]
    public CVector AustraComplexVectorFilter() => cxv.Filter(c => c.Real > c.Imaginary);

    [Benchmark]
    public CVector AustraRandomComplexVector() => new(size, NormalRandom.Shared);

    [Benchmark]
    public CVector AustraPointwiseMultComplexVector() => cxv.PointwiseMultiply(cyv);

    [Benchmark]
    public CVector AustraPointwiseDivComplexVector() => cxv.PointwiseDivide(cyv);

    [Benchmark]
    public CVector AustraRandomOffsetComplexVector() => new(size, Random.Shared, 0.5, 1.1);

    [Benchmark]
    public Complex[] AustraComplexVector2Array() => (Complex[])cxv;

    [Benchmark]
    public Complex AustraComplexVectorMultiply() => cxv * cyv;
}
