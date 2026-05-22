namespace Benchmarks;

public class MatrixMultBenchmark : BenchmarkControl
{
    private readonly int size;
    private readonly Matrix cm1, cm2;
    private readonly DVector cv1;

    public MatrixMultBenchmark()
    {
        size = Configure();
        var rnd = new Random();
        cm1 = new Matrix(size, size, rnd, 0.1, 1);
        cm2 = new Matrix(size, size, rnd, 0.1, 1);
        cv1 = new DVector(size, rnd);
    }

    [Benchmark]
    public Matrix AustraMulMatrix() => cm1 * cm2;

    [Benchmark]
    public Matrix AustraTransMatrix() => cm1.Transpose();

    [Benchmark]
    public Matrix AustraMulTMatrix() => cm1.MultiplyTranspose(cm2);

    [Benchmark]
    public DVector AustraTransMatrixVector() => cm1.TransposeMultiply(cv1);

    [Benchmark]
    public DVector AustraMatrixVector() => cm1 * cv1;
}
