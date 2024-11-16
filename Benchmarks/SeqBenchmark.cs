namespace Benchmarks;

public class SeqBenchmark: BenchmarkControl
{
    private readonly DVector v1 = new(1024, new Random(133));
    private readonly int size;
    private readonly DSequence mseq = DSequence.Create(0, 1023, Math.Tau).Map(Math.Sin);
    private readonly DSequence rseq = DSequence.Random(1024);
    private readonly NSequence nseq = NSequence.Create(2, 12);

    public SeqBenchmark() => size = Configure();

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
