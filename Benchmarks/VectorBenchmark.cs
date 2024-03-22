namespace Benchmarks;

public class VectorBenchmark : BenchmarkControl
{
    private readonly int size;
    private readonly DVector cv1, cv2, cv3, cv4, cv5, cv6, cv7, cv8;
    private readonly NVector nv1;
    private readonly Random rnd = new(133);

    public VectorBenchmark()
    {
        size = Configure();
        cv1 = new DVector(size, rnd);
        cv2 = new DVector(size, rnd);
        cv3 = cv1.Clone();
        cv4 = new DVector(1024, rnd);
        cv5 = new DVector(1024, rnd, 0.6, 1);
        cv6 = new DVector(1024, rnd);
        cv7 = new DVector(1024, new NormalRandom(rnd));
        cv8 = cv7.Clone();
        nv1 = new NVector(size, 100, rnd);
    }

    //[Benchmark]
    public DVector AustraVectorSum() => cv4 + cv5;

    //[Benchmark]
    public DVector AustraVectorSub() => cv4 - cv5;

    //[Benchmark]
    public DVector AustraVectorScale() => 2d * cv4;

    //[Benchmark]
    public DVector AustraVectorAddScalar() => cv4 + 2d;

    //[Benchmark]
    public DVector AustraVectorPointMult() => cv4.PointwiseMultiply(cv5);

    //[Benchmark]
    public DVector AustraVectorPointMultAdd() => cv4.MultiplyAdd(cv5, cv6);

    //[Benchmark]
    public DVector AustraVectorMultAdd() => cv4.MultiplyAdd(Math.PI, cv6);

    //[Benchmark]
    public DVector AustraVectorMultAddRaw() => cv4 * Math.PI + cv6;

    //[Benchmark]
    public double AustraDotProduct() => cv6 * cv7;

    //[Benchmark]
    public double AustraVectorProduct() => cv5.Product();

    [Benchmark]
    public double AustraVectorSumItems() => cv5.Sum();

    //[Benchmark]
    public bool AustraVectorEqualsFalse() => cv6 == cv7;

    //[Benchmark]
    public bool AustraVectorEqualsTrue() => cv7 == cv8;

    //[Benchmark]
    public DVector AustraRawLineal() => 2 * cv4 + 3 * cv5;

    //[Benchmark]
    public DVector AustraCombineLineal() => DVector.Combine2(2, 3, cv4, cv5);

    //[Benchmark]
    public DVector AustraNegate() => -cv4;

    //[Benchmark]
    public DVector AustraRandomVector() => new(size, NormalRandom.Shared);

    //[Benchmark]
    public DVector AustraURandomVector() => new(size, Random.Shared);

    //[Benchmark]
    public double AustraSquareVector() => cv4.Squared();

    //[Benchmark]
    public DVector AustraVectorSqrt() => cv4.Sqrt();

    //[Benchmark]
    public DVector AustraVectorAbs() => cv7.Abs();

    //[Benchmark]
    public DVector AustraVectorMap() => cv7.Map(Math.Abs);

    //[Benchmark]
    public Matrix AustraExternalProduct() => cv1 ^ cv3;

    //[Benchmark]
    public int AustraIndexOfMiddle() => cv7.IndexOf(cv7[cv7.Length / 2]);

    //[Benchmark]
    public int AustraIndexOfLast() => cv7.IndexOf(cv7[^1]);

    //[Benchmark]
    public double AustraAutocorrelation() => cv7.AutoCorrelation(4);

    //[Benchmark]
    public DVector AustraVectorReverse() => cv7.Reverse();

    //[Benchmark]
    public Accumulator AustraVectorAccumulator() => nv1.Stats();

    //[Benchmark]
    public int AustraNIndexOfLast() => nv1.IndexOf(nv1[^1]);

    //[Benchmark]
    public bool AustraAny() => cv7.Any(x => x == double.Pi);

    //[Benchmark]
    public bool AustraAll() => cv6.All(x => x >= 0);
}
