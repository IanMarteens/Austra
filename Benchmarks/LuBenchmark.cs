namespace Benchmarks;

public class LuBenchmark : BenchmarkControl
{
    private readonly MdMatrix mm1, mid;
    private readonly MdVector mv1;
    private readonly MdLU mlu;
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
        mm1 = MdMatrix.Build.DenseOfArray((double[,])cm1);
        mid = MdMatrix.Build.DenseIdentity(size);
        mv1 = MdVector.Build.DenseOfArray((double[])cv1);
        mlu = mm1.LU();
    }

    [Benchmark]
    public LU AustraLuMatrix() => cm1.LU();

    [Benchmark]
    public Vector AustraLuSolve() => clu.Solve(cv1);

    [Benchmark]
    public Matrix AustraLuSolveMat() => clu.Solve(cid);

    [Benchmark]
    public MdLU MdLuMatrix() => mm1.LU();

    [Benchmark]
    public MdVector MdLuSolve() => mlu.Solve(mv1);

    [Benchmark]
    public MdMatrix MdLuSolveMat() => mlu.Solve(mid);
}
