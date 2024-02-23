namespace Benchmarks;

public class SeqBenchmark: BenchmarkControl
{
    private readonly DVector v1;
    private readonly int size;
    private readonly DSequence mseq;
    private readonly DSequence rseq;
    private readonly NSequence nseq;

    public SeqBenchmark()
    {
        size = Configure();
        v1 = new DVector(1024, new Random(133));
        mseq = DSequence.Create(0, 1023, Math.Tau).Map(Math.Sin);
        rseq = DSequence.Random(1024);
        nseq = NSequence.Create(2, 12);
    }

    [Benchmark]
    public double AustraVSeqSum() => ((DSequence)v1).Sum();

    [Benchmark]
    public double AustraMSeqSum() => mseq.Sum();

    [Benchmark]
    public double AustraRSeqSum() => rseq.Sum();

    [Benchmark]
    public DVector AustraRandom() => DSequence.Random(size).ToVector();

    [Benchmark]
    public int AustraFactorial() => nseq.Product();
}
