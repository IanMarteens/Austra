namespace Benchmarks;

public class EvdBenchmark : BenchmarkControl
{
    private readonly MdMatrix mm1, amm1;
    private readonly Matrix cm1, acm1;

    public EvdBenchmark()
    {
        int size = Configure();
        var rnd = new Random();
        var lm = new LMatrix(size, size, rnd, 0.1);
        cm1 = lm.MultiplyTranspose(lm);
        mm1 = MdMatrix.Build.DenseOfArray((double[,])cm1);
        acm1 = new Matrix(size, size, rnd, 0.1);
        amm1 = MdMatrix.Build.DenseOfArray((double[,])acm1);
    }

    [Benchmark]
    public EVD AustraEvdMatrix() => cm1.EVD(true);

    [Benchmark]
    public EVD AustraEvdAsymMatrix() => acm1.EVD(false);

    [Benchmark]
    public bool AustraEvdIsSymmetric() => cm1.IsSymmetric();

    [Benchmark]
    public bool AustraEvdIsNotSymmetric() => acm1.IsSymmetric();

    [Benchmark]
    public MdEvd MdEvdMatrix() => mm1.Evd(MathNet.Numerics.LinearAlgebra.Symmetricity.Symmetric);

    [Benchmark]
    public MdEvd MdEvdAsymMatrix() => amm1.Evd(MathNet.Numerics.LinearAlgebra.Symmetricity.Asymmetric);

    [Benchmark]
    public bool MdEvdIsSymmetric() => mm1.IsSymmetric();

    [Benchmark]
    public bool MdEvdIsNotSymmetric() => amm1.IsSymmetric();

    internal static void Trace(int rank)
    {
        Console.WriteLine("Austra EVD");
        Matrix m = new(rank, rank, new Random(12), 1);
        EVD e = m.EVD();
        Console.WriteLine(e.Vectors);
        foreach (Complex c in e.Values)
            Console.WriteLine(c);
        Console.WriteLine("---------------------------");
        double sum1 = 0.0;
        for (int i = 0; i < e.Vectors.Cols; i++)
        {
            if (rank <= 8)
                Console.WriteLine(e.Vectors.GetColumn(i));
            sum1 += e.Vectors.GetColumn(i).Sum();
        }
        if (rank <= 8)
            Console.WriteLine("---------------------------");
        Console.WriteLine($"Asymmetric checksum: {sum1}");
        LMatrix lm = new(rank, rank, new Random(12), 1);
        m = lm * lm.Transpose();
        e = m.EVD();
        sum1 = 0.0;
        for (int i = 0; i < e.Vectors.Cols; i++)
            sum1 += e.Vectors.GetColumn(i).Sum();
        Console.WriteLine($"Symmetric checksum: {sum1}");
    }

    internal static void Warm(int times = 200_000)
    {
        EvdBenchmark evd = new();
        for (int i = 0; i < times; i++)
        {
            evd.AustraEvdMatrix();
            evd.AustraEvdAsymMatrix();
        }
    }
}
