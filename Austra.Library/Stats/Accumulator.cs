namespace Austra.Library.Stats;

/// <summary>Calculates statistics by adding samples.</summary>
/// <remarks>This class supports hardware-acceleration.</remarks>
public sealed class Accumulator
{
    /// <summary>Minimum value.</summary>
    private double min = double.PositiveInfinity;
    /// <summary>Maximum value.</summary>
    private double max = double.NegativeInfinity;
    /// <summary>Estimated mean.</summary>
    internal double m1;
    /// <summary>Accumulated second moment.</summary>
    internal double m2;
    /// <summary>Accumulated third moment.</summary>
    private double m3;
    /// <summary>Accumulated fourth moment.</summary>
    private double m4;

    /// <summary>Creates an empty accumulator.</summary>
    public Accumulator() { }

    /// <summary>Creates an accumulator from an array of samples.</summary>
    /// <param name="values">Samples for initialization.</param>
    public Accumulator(double[] values) => Add(values);

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

    /// <summary>Gets the unbiased population skewness.</summary>
    public double Skewness =>
        Count < 3
        ? double.NaN
        : Count * m3 * Sqrt(m2 / (Count - 1))
            / (m2 * m2 * (Count - 2)) * (Count - 1);

    /// <summary>Get the skewness from the full population.</summary>
    public double PopulationSkewness =>
        Count < 2
        ? double.NaN
        : Sqrt(Count) * m3 / Pow(m2, 1.5);

    /// <summary>Gets the unbiased population kurtosis.</summary>
    public double Kurtosis =>
        Count < 4
        ? double.NaN
        : ((double)Count * Count - 1) / ((Count - 2) * (Count - 3))
            * (Count * m4 / (m2 * m2) - 3 + 6.0 / (Count + 1));

    /// <summary>Gets the kurtosis from the full population.</summary>
    public double PopulationKurtosis =>
        Count < 3
        ? double.NaN
        : Count * m4 / (m2 * m2) - 3.0;

    /// <summary>Adds a sample to this accumulator.</summary>
    /// <param name="sample">The new sample.</param>
    public void Add(double sample)
    {
        ++Count;
        double d = sample - m1, s = d / Count, t = d * s * (Count - 1);
        m1 += s;
        m4 += (t * s * (Count * (Count - 3) + 3) + 6 * s * m2 - 4 * m3) * s;
        m3 += (t * (Count - 2) - 3 * m2) * s;
        m2 += t;
        if (sample < min) min = sample;
        if (sample > max) max = sample;
    }

    /// <summary>Adds samples from a memory zone to this accumulator.</summary>
    /// <remarks>This method supports hardware-acceleration.</remarks>
    /// <param name="samples">The new samples.</param>
    /// <param name="size">The number of samples.</param>
    public unsafe void Add(double* samples, int size)
    {
        int i = 0;
        if (Avx.IsSupported && size >= 16)
        {
            var vMin = Vector256.Create(double.PositiveInfinity);
            var vMax = Vector256.Create(double.NegativeInfinity);
            var μ1 = Vector256<double>.Zero;
            var μ2 = Vector256<double>.Zero;
            var μ3 = Vector256<double>.Zero;
            var μ4 = Vector256<double>.Zero;
            var v3 = Vector256.Create(3.0);
            var v4 = Vector256.Create(4.0);
            var v6 = Vector256.Create(6.0);
            long c = 0;
            for (int top = size & Simd.AVX_MASK; i < top; i += 4)
            {
                c++;
                var vSample = Avx.LoadVector256(samples + i);
                vMin = Avx.Min(vMin, vSample);
                vMax = Avx.Max(vMax, vSample);
                var vd = Avx.Subtract(vSample, μ1);
                var vs = Avx.Divide(vd, Vector256.Create((double)c));
                var vt = Avx.Multiply(Avx.Multiply(vd, vs),
                    Vector256.Create((double)(c - 1)));
                μ1 = Avx.Add(μ1, vs);
                var t1 = Avx.Multiply(Avx.Multiply(vt, vs),
                    Vector256.Create((double)(c * (c - 3) + 3)));
                var t2 = Avx.Multiply(Avx.Multiply(vs, μ2), v6);
                var t3 = Avx.Multiply(v4, μ3);
                μ4 = μ4.MultiplyAdd(Avx.Subtract(Avx.Add(t1, t2), t3), vs);
                t1 = Avx.Multiply(vt, Vector256.Create((double)(c - 2)));
                t2 = Avx.Multiply(μ2, v3);
                μ3 = μ3.MultiplyAdd(Avx.Subtract(t1, t2), vs);
                μ2 = Avx.Add(μ2, vt);
            }
            var a01 = Mix(c,
                μ1.ToScalar(), μ2.ToScalar(), μ3.ToScalar(), μ4.ToScalar(),
                μ1.GetElement(1), μ2.GetElement(1), μ3.GetElement(1), μ4.GetElement(1));
            var a23 = Mix(c,
                μ1.GetElement(2), μ2.GetElement(2), μ3.GetElement(2), μ4.GetElement(2),
                μ1.GetElement(3), μ2.GetElement(3), μ3.GetElement(3), μ4.GetElement(3));
            var a = Mix(c + c,
                a01.m1, a01.m2, a01.m3, a01.m4,
                a23.m1, a23.m2, a23.m3, a23.m4);
            if (Count == 0)
                (Count, m1, m2, m3, m4) = (4 * c, a.m1, a.m2, a.m3, a.m4);
            else
            {
                long acCnt = 4 * c, n = Count + acCnt, n2 = n * n;
                double d = a.m1 - m1, d2 = d * d, d3 = d2 * d, d4 = d2 * d2;
                double nm1 = (Count * m1 + acCnt * a.m1) / n;
                double nm2 = m2 + a.m2 + d2 * Count * acCnt / n;
                double nm3 = m3 + a.m3
                    + d3 * Count * acCnt * (Count - acCnt) / n2
                    + 3 * d * (Count * a.m2 - acCnt * m2) / n;
                m4 += a.m4 + d4 * Count * acCnt
                        * (Count * (Count - acCnt) + acCnt * acCnt) / (n2 * n)
                    + 6 * d2 * (Count * Count * a.m2 + acCnt * acCnt * m2) / n2
                    + 4 * d * (Count * a.m3 - acCnt * m3) / n;
                (m1, m2, m3, Count) = (nm1, nm2, nm3, n);
            }
            min = Min(min, vMin.Min());
            max = Max(max, vMax.Max());

            static (double m1, double m2, double m3, double m4) Mix(long c,
                double a1, double a2, double a3, double a4,
                double b1, double b2, double b3, double b4)
            {
                long n = c + c, n2 = n * n;
                double d = b1 - a1, d2 = d * d, d4 = d2 * d2;
                return (
                    (a1 + b1) / 2,
                    a2 + b2 + d2 * c / 2,
                    a3 + b3 + 3 * d * (b2 - a2) / 2,
                    a4 + b4 + d4 * c / 8 + 3 * d2 * (b2 + a2) / 2 + 2 * d * (b3 - a3));
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
    public static Accumulator operator +(Accumulator a, double sample)
    {
        a.Add(sample);
        return a;
    }

    /// <summary>Combines two accumulators.</summary>
    /// <param name="a1">The first accumulator.</param>
    /// <param name="a2">The second accumulator.</param>
    /// <returns>A new combined accumulator.</returns>
    public static Accumulator operator +(Accumulator a1, Accumulator a2)
    {
        if (a1.Count == 0) return a2;
        if (a2.Count == 0) return a1;

        long n = a1.Count + a2.Count, n2 = n * n;
        double d = a2.m1 - a1.m1, d2 = d * d, d3 = d2 * d, d4 = d2 * d2;
        double m1 = (a1.Count * a1.m1 + a2.Count * a2.m1) / n;
        double m2 = a1.m2 + a2.m2 + d2 * a1.Count * a2.Count / n;
        double m3 = a1.m3 + a2.m3
            + d3 * a1.Count * a2.Count * (a1.Count - a2.Count) / n2
            + 3 * d * (a1.Count * a2.m2 - a2.Count * a1.m2) / n;
        double m4 = a1.m4 + a2.m4 + d4 * a1.Count * a2.Count
                * (a1.Count * a1.Count - a1.Count * a2.Count + a2.Count * a2.Count)
                / (n2 * n)
            + 6 * d2 * (a1.Count * a1.Count * a2.m2 + a2.Count * a2.Count * a1.m2)
                / n2
            + 4 * d * (a1.Count * a2.m3 - a2.Count * a1.m3) / n;
        return new()
        {
            Count = n,
            m1 = m1, m2 = m2, m3 = m3, m4 = m4,
            min = Min(a1.min, a2.min),
            max = Max(a1.max, a2.max),
        };
    }

    /// <summary>
    /// Gets a short hint string describing the contents of this accumulator.
    /// </summary>
    /// <returns>The most important properties in tabular format.</returns>
    public string Hint => new StringBuilder(256)
        .Append("Count:\t").Append(Count).AppendLine()
        .Append("Min:\t").Append(Minimum.ToString("G6")).AppendLine()
        .Append("Max:\t").Append(Maximum.ToString("G6")).AppendLine()
        .Append("Mean:\t").Append(Mean.ToString("G6")).AppendLine()
        .Append("Var:\t").Append(Variance.ToString("G6")).AppendLine()
        .Append("StdDv:\t").Append(StandardDeviation.ToString("G6"))
        .ToString();

    /// <summary>Gets a textual representation of the accumulator.</summary>
    /// <returns>The most important properties in tabular format.</returns>
    public override string ToString() => new StringBuilder(512)
        .Append("Count: ").Append(Count).AppendLine()
        .Append("Min  : ").Append(Minimum).AppendLine()
        .Append("Max  : ").Append(Maximum).AppendLine()
        .Append("Mean : ").Append(Mean).AppendLine()
        .Append("Var  : ").Append(Variance).AppendLine()
        .Append("StdDv: ").Append(StandardDeviation).AppendLine()
        .Append("Skew : ").Append(Skewness).AppendLine()
        .Append("Kurt : ").Append(Kurtosis).AppendLine()
        .ToString();
}
