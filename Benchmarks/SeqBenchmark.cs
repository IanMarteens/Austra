namespace Benchmarks;

public class SeqBenchmark: BenchmarkControl
{
    private readonly DVector v1;

    public SeqBenchmark()
    {
        Configure();
        v1 = new DVector(1024, new Random(133));
    }

    [Benchmark]
    public double AustraSeqSum()
    {
        DSequence seq = v1;
        double total = 0.0;
        while (seq.Next(out double value))
            total += value;
        return total;
    }
}
