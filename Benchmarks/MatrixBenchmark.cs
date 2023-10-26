﻿namespace Benchmarks;

public class MatrixBenchmark : BenchmarkControl
{
    private readonly int size;
    private readonly Matrix cm1, cm2;
    private readonly Vector cv1, cv2;

    public MatrixBenchmark()
    {
        size = Configure();
        var rnd = new Random();
        cm1 = new Matrix(size, size, rnd, 0.1);
        cm2 = new Matrix(size, size, rnd, 0.1);
        cv1 = new Vector(size, rnd);
        cv2 = new Vector(size, rnd);
    }

    [Benchmark]
    public Matrix AustraAddMatrix() => cm1 + cm2;

    [Benchmark]
    public Matrix AustraSubMatrix() => cm1 - cm2;

    [Benchmark]
    public Matrix AustraMulMatrix() => cm1 * cm2;

    //[Benchmark]
    //public Matrix AustraTransMatrix() => cm1.Transpose();

    //[Benchmark]
    //public Matrix AustraMulTMatrix() => cm1.MultiplyTranspose(cm2);

    //[Benchmark]
    //public Vector AustraTransMatrixVector() => cm1.TransposeMultiply(cv1);

    [Benchmark]
    public Vector AustraMatrixVector() => cm1 * cv1;

    //[Benchmark]
    //public Vector AustraMatrixMultiplyAddRaw() => cm1 * cv1 + cv2;

    [Benchmark]
    public Vector AustraMatrixMultiplyAdd() => cm1.MultiplyAdd(cv1, cv2);

    [Benchmark]
    public LMatrix AustraLowerTriangular() => new(size, NormalRandom.Shared);

    [Benchmark]
    public Matrix AustraRandomMatrix() => new(size, NormalRandom.Shared);
}
