﻿namespace Benchmarks;

public class FftBenchmark : BenchmarkControl
{
    private readonly DVector v533 = new(533, new Random(1));
    private readonly DVector v562 = new(562, new Random(1));
    private readonly DVector v579 = new(579, new Random(1));
    private readonly DVector v580 = new(580, new Random(1));
    private readonly DVector v583 = new(583, new Random(1));
    private readonly DVector v1024 = new(1024, new Random(1));
    private readonly DVector v2048 = new(2048, new Random(1));

    public FftBenchmark() => Configure();

    [Benchmark]
    public Complex[] Fft533() => FFT.Transform((double[])v533);

    [Benchmark]
    public Complex[] Fft562() => FFT.Transform((double[])v562);

    [Benchmark]
    public Complex[] Fft579() => FFT.Transform((double[])v579);

    [Benchmark]
    public Complex[] Fft580() => FFT.Transform((double[])v580);

    [Benchmark]
    public Complex[] Fft583() => FFT.Transform((double[])v583);

    [Benchmark]
    public Complex[] Fft1024() => FFT.Transform((double[])v1024);

    [Benchmark]
    public Complex[] Fft2048() => FFT.Transform((double[])v2048);

    internal static void Trace()
    {
        Trace(FftTest(533));
        Trace(FftTest(562));
        Trace(FftTest(579));
        Trace(FftTest(580));
        Trace(FftTest(583));
        Trace(FftTest(1024));
        Trace(FftTest(2048));

        static Complex[] FftTest(int size)
        {
            DVector v = new(size, new Random(1));
            return FFT.Transform((double[])v);
        }

        static void Trace(Complex[] r)
        {
            WriteLine($"Length: {r.Length}\tChecksum: {r.Sum(c => c.Magnitude)}");
            WriteLine($"{r[0].Magnitude}, {r[1].Magnitude}, {r[2].Magnitude}, {r[3].Magnitude}");
            WriteLine($"{r[^4].Magnitude}, {r[^3].Magnitude}, {r[^2].Magnitude}, {r[^1].Magnitude}");
            WriteLine();
        }
    }

    internal static void Warm(int times = 200_000)
    {
        FftBenchmark fft = new();
        for (int i = 0; i < times; i++)
        {
            fft.Fft533();
            fft.Fft562();
            fft.Fft579();
            fft.Fft580();
            fft.Fft583();
            fft.Fft1024();
        }
    }
}
