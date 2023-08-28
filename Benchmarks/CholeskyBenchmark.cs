using System.Runtime.CompilerServices;

namespace Benchmarks;

public class CholeskyBenchmark : BenchmarkControl
{
    private readonly MdMatrix mm1, mid;
    private readonly MdVector mv1;
    private readonly MdCholesky mch;
    private readonly Matrix cm1, cid;
    private readonly Vector cv1;
    private readonly Cholesky cch;

    public CholeskyBenchmark()
    {
        int size = Configure();
        var rnd = new Random();
        var lm = new LMatrix(size, size, rnd, 0.4);
        cm1 = lm.MultiplyTranspose(lm);
        cid = Matrix.Identity(size);
        cv1 = new Vector(size, rnd);
        cch = cm1.Cholesky();
        mm1 = MdMatrix.Build.DenseOfArray((double[,])cm1);
        mid = MdMatrix.Build.DenseIdentity(size);
        mv1 = MdVector.Build.DenseOfArray((double[])cv1);
        mch = mm1.Cholesky();
    }

    [Benchmark]
    public Cholesky AustraCholMatrix() => cm1.Cholesky();

    [Benchmark]
    public Vector AustraCholSolve() => cch.Solve(cv1);

    [Benchmark]
    public Matrix AustraCholSolveMat() => cch.Solve(cid);

    [Benchmark]
    public MdCholesky MdCholMatrix() => mm1.Cholesky();

    [Benchmark]
    public MdVector MdCholSolve() => mch.Solve(mv1);

    [Benchmark]
    public MdMatrix MdCholSolveMat() => mch.Solve(mid);
}
