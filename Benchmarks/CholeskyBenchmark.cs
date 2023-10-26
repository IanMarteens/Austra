namespace Benchmarks;

public class CholeskyBenchmark : BenchmarkControl
{
    private readonly Matrix cm1, cid;
    private readonly Vector cv1;
    private readonly Cholesky cch;

    public CholeskyBenchmark()
    {
        int size = Configure();
        var rnd = new Random();
        var lm = new LMatrix(size, size, rnd, 0.51);
        for (int i = 0; i < size; i++)
            ((double[])lm)[i * size + i] += rnd.NextDouble() * 0.5;
        cm1 = lm.MultiplyTranspose(lm);
        cid = Matrix.Identity(size);
        cv1 = new Vector(size, rnd);
        cch = cm1.Cholesky();
    }

    [Benchmark]
    public Cholesky AustraCholMatrix() => cm1.Cholesky();

    [Benchmark]
    public Vector AustraCholSolve() => cch.Solve(cv1);

    [Benchmark]
    public Matrix AustraCholSolveMat() => cch.Solve(cid);
}
