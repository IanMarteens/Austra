﻿using System.Drawing;

namespace Benchmarks;

public class CVectorBenchmark : BenchmarkControl
{
    private readonly Complex[] cv = (Complex[])new ComplexVector(1023, new Random());
    private readonly ComplexVector cxv = new ComplexVector(1023, new Random());

    public CVectorBenchmark()
    {
    }

    [Benchmark]
    public ComplexVector AustraComplexVectorCtor() => new(cv);

    //[Benchmark]
    public Vector AustraComplexVectorMagnitudes() => cxv.Magnitudes();

    //[Benchmark]
    public Vector AustraComplexVectorPhases() => cxv.Phases();

    //[Benchmark]
    public ComplexVector AustraComplexVectorMap() => cxv.Map(c => new(c.Imaginary, c.Real));

    //[Benchmark]
    public ComplexVector AustraComplexVectorFilter() => cxv.Filter(c => c.Real > c.Imaginary);

    //[Benchmark]
    public ComplexVector AustraRandomComplexVector() => new(1024, NormalRandom.Shared);
}
