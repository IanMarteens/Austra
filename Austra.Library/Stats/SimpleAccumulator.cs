namespace Austra.Library;

/// <summary>Calculates statistics by adding samples.</summary>
/// <remarks>This class supports hardware-acceleration.</remarks>
public sealed class SimpleAccumulator
{
    /// <summary>Minimum value.</summary>
    private double min = double.PositiveInfinity;
    /// <summary>Maximum value.</summary>
    private double max = double.NegativeInfinity;
    /// <summary>Estimated mean.</summary>
    private double m1;
    /// <summary>Accumulated second moment.</summary>
    private double m2;

    /// <summary>Creates an empty accumulator.</summary>
    public SimpleAccumulator() { }

    /// <summary>Creates an empty accumulator from a full source.</summary>
    /// <param name="source">Full-fledged accumulator.</param>
    public SimpleAccumulator(Accumulator source)
        => (min, max, m1, m2, Count) 
        = (source.Minimum, source.Maximum, source.m1, source.m2, source.Count);

    /// <summary>Gets the total number of samples.</summary>
    public long Count { get; private set; }

    /// <summary>Returns the minimum value in the sample data.</summary>
    public double Minimum => Count > 0 ? min : double.NaN;

    /// <summary>Returns the maximum value in the sample data.</summary>
    public double Maximum => Count > 0 ? max : double.NaN;

    /// <summary>Gets the sample mean.</summary>
    public double Mean => Count > 0 ? m1 : double.NaN;

    /// <summary>Gets the unbiased variance.</summary>
    public double Variance =>
        Count < 2 ? double.NaN : m2 / (Count - 1);

    /// <summary>Gets the variance from the full population.</summary>
    public double PopulationVariance =>
        Count < 2 ? double.NaN : m2 / Count;

    /// <summary>Gets the unbiased standard deviation.</summary>
    public double StandardDeviation =>
        Count < 2 ? double.NaN : Sqrt(m2 / (Count - 1));

    /// <summary>Gets the standard deviation from the full population.</summary>
    public double PopulationStandardDeviation =>
        Count < 2 ? double.NaN : Sqrt(m2 / Count);

    /// <summary>Adds a sample to this accumulator.</summary>
    /// <param name="sample">The new sample.</param>
    public void Add(double sample)
    {
        ++Count;
        double d = sample - m1;
        double s = d / Count;
        m1 += s;
        m2 += d * s * (Count - 1);

        if (sample < min)
            min = sample;
        if (sample > max)
            max = sample;
    }

    /// <summary>Adds samples from a memory zone to this accumulator.</summary>
    /// <remarks>This operation can be hardware-accelerated.</remarks>
    /// <param name="samples">The new samples.</param>
    /// <param name="size">The number of samples.</param>
    public unsafe void Add(double* samples, int size)
    {
        int i = 0;
        if (Avx.IsSupported && size >= 16)
        {
            var vMin = Vector256.Create(double.PositiveInfinity);
            var vMax = Vector256.Create(double.NegativeInfinity);
            var vM1 = Vector256<double>.Zero;
            var vM2 = Vector256<double>.Zero;
            long c = 0;
            for (int top = size & Simd.AVX_MASK; i < top; i += 4)
            {
                c++;
                var vSample = Avx.LoadVector256(samples + i);
                vMin = Avx.Min(vMin, vSample);
                vMax = Avx.Max(vMax, vSample);
                var vd = Avx.Subtract(vSample, vM1);
                var vs = Avx.Divide(vd, Vector256.Create((double)c));
                var vt = Avx.Multiply(Avx.Multiply(vd, vs), Vector256.Create((double)(c - 1)));
                vM1 = Avx.Add(vM1, vs);
                vM2 = Avx.Add(vM2, vt);
            }
            var acc01 = Mix(c,
                vM1.ToScalar(), vM2.ToScalar(),
                vM1.GetElement(1), vM2.GetElement(1));
            var acc23 = Mix(c,
                vM1.GetElement(2), vM2.GetElement(2),
                vM1.GetElement(3), vM2.GetElement(3));
            var acc = Mix(c + c, acc01.m1, acc01.m2, acc23.m1, acc23.m2);
            if (Count == 0)
                (Count, m1, m2) = (4 * c, acc.m1, acc.m2);
            else
            {
                long accCount = 4 * c, n = Count + accCount, n2 = n * n;
                double d = acc.m1 - m1, d2 = d * d, d3 = d2 * d, d4 = d2 * d2;

                double nm1 = (Count * m1 + accCount * acc.m1) / n;
                double nm2 = m2 + acc.m2 + d2 * Count * accCount / n;
                (m1, m2, Count) = (nm1, nm2, Count + accCount);
            }
            min = Min(min, vMin.Min());
            max = Max(max, vMax.Max());

            static (double m1, double m2) Mix(long c, double a1, double a2, double b1, double b2)
            {
                long n = c + c, n2 = n * n;
                double d = b1 - a1;
                return ((a1 + b1) / 2, a2 + b2 + (d * d) * c / 2);
            }
        }
        for (; i < size; ++i)
            Add(samples[i]);
    }

    /// <summary>Adds an array of samples to this accumulator.</summary>
    /// <param name="samples">The new samples.</param>
    public unsafe void Add(double[] samples)
    {
        fixed (double* p = samples)
            Add(p, samples.Length);
    }

    /// <summary>Adds a sample to an accumulator.</summary>
    /// <param name="a">The accumulator.</param>
    /// <param name="sample">A new sample.</param>
    /// <returns>The original updated accumulator.</returns>
    public static SimpleAccumulator operator +(SimpleAccumulator a, double sample)
    {
        a.Add(sample);
        return a;
    }

    /// <summary>Combines two accumulators.</summary>
    /// <param name="a1">The first accumulator.</param>
    /// <param name="a2">The second accumulator.</param>
    /// <returns>A new combined accumulator.</returns>
    public static SimpleAccumulator operator +(
        SimpleAccumulator a1, SimpleAccumulator a2)
    {
        if (a1.Count == 0)
            return a2;
        else if (a2.Count == 0)
            return a1;

        long n = a1.Count + a2.Count;
        double d = a2.m1 - a1.m1;
        double d2 = d * d;
        double m1 = (a1.Count * a1.m1 + a2.Count * a2.m1) / n;
        double m2 = a1.m2 + a2.m2 + d2 * a1.Count * a2.Count / n;

        return new()
        {
            Count = n,
            m1 = m1,
            m2 = m2,
            min = Min(a1.min, a2.min),
            max = Max(a1.max, a2.max),
        };
    }

    /// <summary>Gets a textual representation of the accumulator.</summary>
    /// <returns>The most important properties in tabular format.</returns>

    public override string ToString() => new StringBuilder(512)
        .Append("Count: ").Append(Count).AppendLine()
        .Append("Min  : ").Append(Minimum).AppendLine()
        .Append("Max  : ").Append(Maximum).AppendLine()
        .Append("Mean : ").Append(Mean).AppendLine()
        .Append("Var  : ").Append(Variance).AppendLine()
        .Append("StdDv: ").Append(StandardDeviation).AppendLine()
        .ToString();
}
