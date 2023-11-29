namespace Benchmarks;

public class MatrixBenchmark : BenchmarkControl
{
    private readonly int size;
    private readonly Matrix cm1, cm2, sym;
    private readonly DVector cv1, cv2;
    private readonly LMatrix lm1, lm2;
    private readonly RMatrix rm1;

    public MatrixBenchmark()
    {
        size = Configure();
        var rnd = new Random();
        cm1 = new Matrix(size, size, rnd, 0.1);
        cm2 = new Matrix(size, size, rnd, 0.1);
        sym = cm1.MultiplyTranspose(cm1);
        cv1 = new DVector(size, rnd);
        cv2 = new DVector(size, rnd);
        lm1 = new LMatrix(size, rnd);
        lm2 = new LMatrix(size, rnd);
        rm1 = new RMatrix(size, rnd);
    }

    //[Benchmark]
    public Matrix AustraAddMatrix() => cm1 + cm2;
    
    //[Benchmark]
    public Matrix AustraSubMatrix() => cm1 - cm2;

    //[Benchmark]
    public LMatrix AustraAddLMatrix() => lm1 + lm2;

    //[Benchmark]
    public LMatrix AustraSubLMatrix1() => lm1 - lm2;

    //[Benchmark]
    public Matrix AustraMulMatrix() => cm1 * cm2;

    //[Benchmark]
    public Matrix AustraTransMatrix() => cm1.Transpose();

    //[Benchmark]
    public Matrix AustraMulTMatrix() => cm1.MultiplyTranspose(cm2);

    //[Benchmark]
    public DVector AustraTransMatrixVector() => cm1.TransposeMultiply(cv1);

    //[Benchmark]
    public DVector AustraMatrixVector() => cm1 * cv1;

    //[Benchmark]
    public DVector AustraLMatrixVector() => lm1 * cv1;

    //[Benchmark]
    public DVector AustraRMatrixVector() => rm1 * cv1;

    //[Benchmark]
    public DVector AustraLMatrixMultAdd() => lm1.MultiplyAdd(cv1, cv2);

    //[Benchmark]
    public DVector AustraMatrixMultiplyAddRaw() => cm1 * cv1 + cv2;

    //[Benchmark]
    public DVector AustraMatrixMultiplyAdd() => cm1.MultiplyAdd(cv1, cv2);

    //[Benchmark]
    public DVector AustraMatrixMultiplySub() => cm1.MultiplySubtract(cv1, cv2);

    //[Benchmark]
    public LMatrix AustraLowerTriangular() => new(size, NormalRandom.Shared);

    //[Benchmark]
    public Matrix AustraRandomMatrix() => new(size, Random.Shared);

    //[Benchmark]
    public bool AustraSymmetricMatrix() => sym.IsSymmetric();

    //[Benchmark]
    public DVector AustraMatrixGetRow() => cm1.GetRow(size / 2);

    //[Benchmark]
    public Matrix AustraMatrixMap() => cm1.Map(x => x * x);

    //[Benchmark]
    public LMatrix AustraTransposeRMatrix() => rm1.Transpose();

    //[Benchmark]
    public LMatrix AustraLMultiplyTranspose() => lm1.MultiplyTranspose(lm1);

    //[Benchmark]
    public Matrix AustraLRMatrixMult() => cm1 * lm1;

    //[Benchmark]
    public DVector AustraLMatrixSolve() => lm1.Solve(cv1);

    [Benchmark]
    public DVector AustraGetDiagonal() => cm1.Diagonal();
}
