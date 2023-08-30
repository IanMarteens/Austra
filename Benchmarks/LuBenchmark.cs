namespace Benchmarks;

public class LuBenchmark : BenchmarkControl
{
    private readonly Matrix cm1, cid;
    private readonly Vector cv1;
    private readonly LU clu;

    public LuBenchmark()
    {
        int size = Configure();
        var rnd = new Random();
        cm1 = new Matrix(size, size, rnd, 0.1);
        cid = Matrix.Identity(size);
        cv1 = new Vector(size, rnd);
        clu = cm1.LU();
    }

    [Benchmark]
    public LU AustraLuMatrix() => cm1.LU();

    [Benchmark]
    public Vector AustraLuSolve() => clu.Solve(cv1);

    [Benchmark]
    public Matrix AustraLuSolveMat() => clu.Solve(cid);
}
