namespace Benchmarks;

public class VectorBenchmark : BenchmarkControl
{
    private readonly Vector cv1, cv2, cv3;
    private readonly Complex[] cv = new Complex[1023];
    private readonly ComplexVector cxv;

    public VectorBenchmark()
    {
        int size = Configure();
        var rnd = new Random();
        cv1 = new Vector(size, rnd);
        cv2 = new Vector(size, rnd);
        cv3 = cv1.Clone();
        for (int i = 0; i < cv.Length; i++)
            cv[i] = new Complex(rnd.NextDouble(), rnd.NextDouble());
        cxv = new(cv);
    }

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
}
