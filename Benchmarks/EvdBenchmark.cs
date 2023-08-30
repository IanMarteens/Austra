namespace Benchmarks;

public class EvdBenchmark : BenchmarkControl
{
    private readonly Matrix cm1, acm1;

    public EvdBenchmark()
    {
        int size = Configure();
        var rnd = new Random();
        var lm = new LMatrix(size, size, rnd, 0.1);
        cm1 = lm.MultiplyTranspose(lm);
        acm1 = new Matrix(size, size, rnd, 0.1);
    }

    [Benchmark]
    public EVD AustraEvdMatrix() => cm1.EVD(true);

    [Benchmark]
    public EVD AustraEvdAsymMatrix() => acm1.EVD(false);

    [Benchmark]
    public bool AustraEvdIsSymmetric() => cm1.IsSymmetric();

    [Benchmark]
    public bool AustraEvdIsNotSymmetric() => acm1.IsSymmetric();

    internal static void Trace(int rank)
    {
        WriteLine("Austra EVD");
        Matrix m = new(rank, rank, new Random(12), 1);
        EVD e = m.EVD();
        WriteLine(e.Vectors);
        foreach (Complex c in e.Values)
            WriteLine(c);
        WriteLine("---------------------------");
        double sum1 = 0.0;
        for (int i = 0; i < e.Vectors.Cols; i++)
        {
            if (rank <= 8)
                WriteLine(e.Vectors.GetColumn(i));
            sum1 += e.Vectors.GetColumn(i).Sum();
        }
        if (rank <= 8)
            WriteLine("---------------------------");
        WriteLine($"Asymmetric checksum: {sum1}");
        LMatrix lm = new(rank, rank, new Random(12), 1);
        m = lm * lm.Transpose();
        e = m.EVD();
        sum1 = 0.0;
        for (int i = 0; i < e.Vectors.Cols; i++)
            sum1 += e.Vectors.GetColumn(i).Sum();
        WriteLine($"Symmetric checksum: {sum1}");
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
