namespace Benchmarks;

public class CVectorBenchmark : BenchmarkControl
{
    private readonly Complex[] cv = (Complex[])new ComplexVector(1023, new Random());
    private readonly ComplexVector cxv = new(1023, new Random());
    private readonly ComplexVector cyv = new(1023, new Random());
    private readonly int size = 1023;
    private readonly double scale = Random.Shared.NextDouble() + 0.5;

    public CVectorBenchmark() { }

    [Benchmark]
    public ComplexVector AustraComplexVectorCtor() => new(cv);

    [Benchmark]
    public Vector AustraComplexVectorMagnitudes() => cxv.Magnitudes();

    [Benchmark]
    public Vector AustraComplexVectorPhases() => cxv.Phases();

    [Benchmark]
    public ComplexVector AustraComplexVectorScale() => cxv * scale;

    [Benchmark]
    public ComplexVector AustraComplexVectorMap() => cxv.Map(c => new(c.Imaginary, c.Real));

    [Benchmark]
    public ComplexVector AustraComplexVectorFilter() => cxv.Filter(c => c.Real > c.Imaginary);

    [Benchmark]
    public ComplexVector AustraRandomComplexVector() => new(size, NormalRandom.Shared);

    [Benchmark]
    public ComplexVector AustraPointwiseMultComplexVector() => cxv.PointwiseMultiply(cyv);

    [Benchmark]
    public ComplexVector AustraPointwiseDivComplexVector() => cxv.PointwiseDivide(cyv);

    [Benchmark]
    public ComplexVector AustraRandomOffsetComplexVector() => new(size, Random.Shared, 0.5, 1.1);

    [Benchmark]
    public Complex[] AustraComplexVector2Array() => (Complex[])cxv;
}
