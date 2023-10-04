namespace Benchmarks;

public class VectorBenchmark : BenchmarkControl
{
    private readonly int size;
    private readonly Vector cv1, cv2, cv3, cv4, cv5;
    private readonly Complex[] cv = new Complex[1023];
    private readonly ComplexVector cxv;

    public VectorBenchmark()
    {
        size = Configure();
        var rnd = new Random();
        cv1 = new Vector(size, rnd);
        cv2 = new Vector(size, rnd);
        cv3 = cv1.Clone();
        cv4 = new Vector(1024, rnd);
        cv5 = new Vector(1024, rnd);
        for (int i = 0; i < cv.Length; i++)
            cv[i] = new Complex(rnd.NextDouble(), rnd.NextDouble());
        cxv = new(cv);
    }

    [Benchmark]
    public Vector AustraVectorSum() => cv4 + cv5;

    [Benchmark]
    public Vector AustraVectorScale() => 2d * cv4;

    [Benchmark]
    public double AustraDotProduct() => cv1 * cv2;

    [Benchmark]
    public bool AustraVectorEqualsFalse() => cv1 == cv2;

    [Benchmark]
    public bool AustraVectorEqualsTrue() => cv1 == cv3;

    [Benchmark]
    public ComplexVector AustraComplexVectorCtor() => new(cv);

    [Benchmark]
    public Vector AustraComplexVectorMagnitudes() => cxv.Magnitudes();

    [Benchmark]
    public Vector AustraComplexVectorPhases() => cxv.Phases();

    [Benchmark]
    public ComplexVector AustraComplexVectorMap() => cxv.Map(c => new(c.Imaginary, c.Real));

    [Benchmark]
    public ComplexVector AustraComplexVectorFilter() => cxv.Filter(c => c.Real > c.Imaginary);

    [Benchmark]
    public Vector AustraRawLineal() => 2 * cv4 + 3 * cv5;

    [Benchmark]
    public Vector AustraCombineLineal() => Vector.Combine2(2, 3, cv4, cv5);

    [Benchmark]
    public Vector AustraRandomVector() => new(size, NormalRandom.Shared);
}
