﻿namespace Benchmarks;

public class MatrixBenchmark : BenchmarkControl
{
    private readonly Matrix cm1, cm2;
    private readonly Vector cv1;
    private readonly MdMatrix mm1, mm2;
    private readonly MdVector mv1;

    public MatrixBenchmark()
    {
        int size = Configure();
        var rnd = new Random();
        cm1 = new Matrix(size, size, rnd, 0.1);
        cm2 = new Matrix(size, size, rnd, 0.1);
        cv1 = new Vector(size, rnd);
        mm1 = MdMatrix.Build.DenseOfArray((double[,])cm1);
        mm2 = MdMatrix.Build.DenseOfArray((double[,])cm2);
        mv1 = MdVector.Build.DenseOfArray((double[])cv1);
    }

    [Benchmark]
    public Matrix AustraAddMatrix() => cm1 + cm2;

    [Benchmark]
    public Matrix AustraSubMatrix() => cm1 - cm2;

    [Benchmark]
    public Matrix AustraMulMatrix() => cm1 * cm2;

    [Benchmark]
    public Matrix AustraTransMatrix() => cm1.Transpose();

    [Benchmark]
    public Matrix AustraMulTMatrix() => cm1.MultiplyTranspose(cm2);

    [Benchmark]
    public Vector AustraTransMatrixVector() => cm1.TransposeMultiply(cv1);

    [Benchmark]
    public MdMatrix MdAddMatrix() => mm1 + mm2;

    [Benchmark]
    public MdMatrix MdSubMatrix() => mm1 - mm2;

    [Benchmark]
    public MdMatrix MdMulMatrix() => mm1 * mm2;

    [Benchmark]
    public MdMatrix MdTransMatrix() => mm1.Transpose();

    [Benchmark]
    public MdMatrix MdMulTMatrix() => mm1.TransposeAndMultiply(mm2);

    [Benchmark]
    public MdVector MdTransMatrixVector() => mm1.TransposeThisAndMultiply(mv1);
}
