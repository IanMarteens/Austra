namespace Austra.Library.Transforms;

/// <summary>Provides both complex and real Fast Fourier Transforms.</summary>
public static class FFT
{
    /// <summary>Forward complex Fast Fourier Transform.</summary>
    /// <remarks>The array length may be either composite or prime.</remarks>
    /// <param name="a">Samples to be transformed in-place.</param>
    /// <param name="n">The size of the problem.</param>
    public static unsafe void Transform(Complex[] a, int n)
    {
        if (n <= 1)
            return;
        FftPlan plan = new(n);
        fixed (Complex* pa = a)
            plan.Execute((double*)pa);
    }

    /// <summary>Forward complex Fast Fourier Transform.</summary>
    /// <remarks>The array length may be either composite or prime.</remarks>
    /// <param name="a">Samples to be transformed in-place.</param>
    public static void Transform(Complex[] a) => Transform(a, a.Length);

    /// <summary>Inverse complex Fast Fourier Transform.</summary>
    /// <remarks><code>Inverse(a) = Transform(a')'/N</code></remarks>
    /// <param name="a">Samples to be transformed in-place.</param>
    /// <param name="n">The size of the problem.</param>
    public static void Inverse(Complex[] a, int n)
    {
        for (int i = 0; i < n; i++)
            a[i] = Complex.Conjugate(a[i]);
        Transform(a, n);
        for (int i = 0; i < n; i++)
            a[i] = new(a[i].Real / n, -(a[i].Imaginary / n));
    }

    /// <summary>Inverse complex Fast Fourier Transform.</summary>
    /// <param name="a">Samples to be transformed in-place.</param>
    public static void Inverse(Complex[] a) => Inverse(a, a.Length);

    /// <summary>Forward real Fast Fourier Transform.</summary>
    /// <param name="a">Samples to be transformed.</param>
    /// <param name="n">The size of the problem.</param>
    /// <returns>The transformed samples.</returns>
    public static unsafe Complex[] Transform(double[] a, int n)
    {
        // Special cases:
        if (n == 1)
            return [a[0]];
        if (n == 2)
            return [a[0] + a[1], a[0] - a[1]];

        // Choose between odd-size and even-size FFTs
        Complex[] f = GC.AllocateUninitializedArray<Complex>(n);
        if (n % 2 == 0)
        {
            int n2 = n / 2;
            FftPlan plan = new(n2);
            fixed (double* pb = a.AsSpan()[..n].ToArray())
            fixed (Complex* pf = f)
            {
                plan.Execute(pb);
                var cnj = Vector128.Create(0.0, -0.0);
                var half = Vector128.Create(0.5, 0.5);
                double* pfd = (double*)pf;
                if (Avx.IsSupported)
                {
                    for (int i = 0; i <= n2; i++)
                    {
                        var x = Sse2.LoadVector128(pb + 2 * (i % n2));
                        var y = Sse2.Xor(Sse2.LoadVector128(pb + 2 * ((n2 - i) % n2)), cnj);
                        double θ = -Tau * i / n;
                        var v = Vector128.Create(-Sin(θ), Cos(θ));
                        var t1 = Sse2.Subtract(y, x);
                        var t2 = Sse3.HorizontalSubtract(
                            Sse2.Multiply(v, t1),
                            Sse2.Multiply(v, Sse2.Xor(Avx.Permute(t1, 5), cnj)));
                        Sse2.Store(pfd + 2 * i,
                            Sse2.Multiply(Sse2.Add(Sse2.Add(x, y), t2), half));
                    }
                    for (int i = n2 + 1; i < n; i++)
                        Sse2.Store(pfd + 2 * i,
                            Sse2.Xor(Sse2.LoadVector128(pfd + 2 * (n - i)), cnj));
                }
                else
                {
                    for (int i = 0; i <= n2; i++)
                    {
                        int idx = 2 * (i % n2);
                        Complex x = new(pb[idx + 0], pb[idx + 1]);
                        idx = 2 * ((n2 - i) % n2);
                        Complex y = new(pb[idx + 0], -pb[idx + 1]);
                        double θ = -Tau * i / n;
                        pf[i] = (x + y - new Complex(-Sin(θ), Cos(θ)) * (x - y)) * 0.5;
                    }
                    for (int i = n2 + 1; i < n; i++)
                        pf[i] = Complex.Conjugate(pf[n - i]);
                }
            }
        }
        else
        {
            fixed (Complex* pf = f)
            fixed (double* pa = a)
            {
                double* pfd = (double*)pf;
                int i = 0;
                if (Avx.IsSupported)
                {
                    V4d z = V4d.Zero;
                    for (int top = n & ~1; i < top; i += 4)
                    {
                        V4d source = Avx.LoadVector256(pa + i);
                        double* target = pfd + i + i;
                        Avx.Store(target,
                            Avx.Shuffle(Avx.Permute2x128(source, source, 0), z, 12));
                        Avx.Store(target + 4,
                            Avx.Shuffle(Avx.Permute2x128(source, source, 17), z, 12));
                    }
                }
                for (; i < n; i++)
                    (pfd[i + i], pfd[i + i + 1]) = (pa[i], 0d);
            }
            Transform(f, n);
        }
        return f;
    }

    /// <summary>Forward real Fast Fourier Transform.</summary>
    /// <param name="a">Samples to be transformed.</param>
    /// <returns>The transformed samples.</returns>
    public static Complex[] Transform(double[] a) => Transform(a, a.Length);

    /// <summary>Inverse real Fast Fourier Transform.</summary>
    /// <remarks>
    /// <paramref name="f"/> should satisfy f[k] = f[n - k]', so just one
    /// half of frequencies array is needed.<br/>
    /// f[0] is always real.<br/>If n is even, f[n/2] is real too.
    /// </remarks>
    /// <param name="f">Samples to be transformed.</param>
    /// <param name="n">The size of the problem.</param>
    /// <returns>The original samples.</returns>
    public static double[] InverseReal(Complex[] f, int n)
    {
        // When n=1, FFT is just the identity transform.
        if (n == 1)
            return [f[0].Real];

        // Inverse real FFT is reduced to the inverse real FHT,
        // which is reduced to the forward real FHT,
        // which is reduced to the forward real FFT.
        double[] h = new double[n];
        h[0] = f[0].Real;
        for (int i = 1; i < n / 2; i++)
        {
            h[i] = f[i].Real - f[i].Imaginary;
            h[n - i] = f[i].Real + f[i].Imaginary;
        }
        int idx = n / 2;
        if (n % 2 == 0)
            h[idx] = f[idx].Real;
        else
        {
            h[idx] = f[idx].Real - f[idx].Imaginary;
            h[idx + 1] = f[idx].Real + f[idx].Imaginary;
        }
        Complex[] fh = Transform(h, n);
        for (int i = 0; i < n; i++)
            h[i] = (fh[i].Real - fh[i].Imaginary) / n;
        return h;
    }

    /// <summary>Inverse real Fast Fourier Transform.</summary>
    /// <param name="a">Samples to be transformed.</param>
    /// <returns>Original samples.</returns>
    public static double[] InverseReal(Complex[] a) => InverseReal(a, a.Length);
}
