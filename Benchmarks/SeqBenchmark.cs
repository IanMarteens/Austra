namespace Benchmarks;

public class SeqBenchmark: BenchmarkControl
{
    private readonly DVector v1;
    private readonly int size;

    public SeqBenchmark()
    {
        size = Configure();
        v1 = new DVector(1024, new Random(133));
    }

    //[Benchmark]
    public double AustraSeqSum()
    {
        DSequence seq = v1;
        double total = 0.0;
        while (seq.Next(out double value))
            total += value;
        return total;
    }

    [Benchmark]
    public DVector AustraRandom() =>
        DSequence.Random(size).ToVector();
}
