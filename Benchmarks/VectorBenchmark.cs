namespace Benchmarks;

public class VectorBenchmark : BenchmarkControl
{
    private readonly int size;
    private readonly Vector cv1, cv2, cv3, cv4, cv5, cv6, cv7;

    public VectorBenchmark()
    {
        size = Configure();
        var rnd = new Random(133);
        cv1 = new Vector(size, rnd);
        cv2 = new Vector(size, rnd);
        cv3 = cv1.Clone();
        cv4 = new Vector(1024, rnd);
        cv5 = new Vector(1024, rnd, 0.6, 1);
        cv6 = new Vector(1024, rnd);
        cv7 = new Vector(1024, new NormalRandom(rnd));
    }

    [Benchmark]
    public Vector AustraVectorSum() => cv4 + cv5;

    [Benchmark]
    public Vector AustraVectorSub() => cv4 - cv5;

    [Benchmark]
    public Vector AustraVectorScale() => 2d * cv4;

    [Benchmark]
    public Vector AustraVectorAddScalar() => cv4 + 2d;

    [Benchmark]
    public Vector AustraVectorPointMult() => cv4.PointwiseMultiply(cv5);

    [Benchmark]
    public Vector AustraVectorPointMultAdd() => cv4.MultiplyAdd(cv5, cv6);

    [Benchmark]
    public double AustraDotProduct() => cv1 * cv2;

    [Benchmark]
    public double AustraVectorProduct() => cv5.Product();

    [Benchmark]
    public double AustraVectorSumItems() => cv5.Sum();

    [Benchmark]
    public bool AustraVectorEqualsFalse() => cv1 == cv2;

    [Benchmark]
    public bool AustraVectorEqualsTrue() => cv1 == cv3;

    [Benchmark]
    public Vector AustraRawLineal() => 2 * cv4 + 3 * cv5;

    [Benchmark]
    public Vector AustraCombineLineal() => Vector.Combine2(2, 3, cv4, cv5);

    [Benchmark]
    public Vector AustraNegate() => -cv4;

    [Benchmark]
    public Vector AustraRandomVector() => new(size, NormalRandom.Shared);

    [Benchmark]
    public double AustraSquareVector() => cv4.Squared();

    [Benchmark]
    public Vector AustraVectorSqrt() => cv4.Sqrt();

    [Benchmark]
    public Vector AustraVectorAbs() => cv7.Abs();

    [Benchmark]
    public Vector AustraVectorMap() => cv7.Map(Math.Abs);
}
