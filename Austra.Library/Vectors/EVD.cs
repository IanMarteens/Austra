namespace Austra.Library;

/// <summary>Eigenvalue decomposition.</summary>
public readonly struct EVD : IFormattable
{
    /// <summary>Gets eigenvalues as a block diagonal matrix.</summary>
    private readonly Lazy<Matrix> diagonal;

    /// <summary>Creates an eigenvalue decomposition.</summary>
    /// <param name="m">Matrix to decompose.</param>
    /// <param name="isSymmetric">Is the first parameter a symmetric matrix?</param>
    [SkipLocalsInit]
    public unsafe EVD(Matrix m, bool isSymmetric)
    {
        int r = m.Rows;
        double[] d = new double[r], e = new double[r];
        if (isSymmetric)
        {
            Vectors = m.Clone();
            fixed (double* pA = (double[])Vectors, pd = d, pe = e)
            {
                Buffer.MemoryCopy(
                    source: pA + (r - 1) * r,
                    destination: pd,
                    destinationSizeInBytes: r * sizeof(double),
                    sourceBytesToCopy: r * sizeof(double));
                SymmetricTridiagonalize(pA, pd, pe, r);
                SymmetricDiagonalize(pA, pd, pe, r);
                CommonMatrix.Transpose(pA, r);
            }
        }
        else
        {
            Vectors = Matrix.Identity(r);
            fixed (double* pA = (double[])Vectors, pH = (double[])m.Transpose(),
                pd = d, pe = e)
            {
                ReduceToHessenberg(pA, pH, pd, r);
                ReduceToSchur(pA, pH, pd, pe, r);
                CommonMatrix.Transpose(pA, r);
            }
        }
        Values = new(d, e);
        ComplexVector values = Values;
        diagonal = new(() => CreateDiagonal(values), true);
    }

    /// <summary>Gets the eigenvector's matrix.</summary>
    public Matrix Vectors { get; }

    /// <summary>Gets all eigenvalues.</summary>
    public ComplexVector Values { get; }

    /// <summary>Gets eigenvalues as a block diagonal matrix.</summary>
    public Matrix D => diagonal.Value;

    /// <summary>Symmetric Householder reduction to tridiagonal form.</summary>
    /// <param name="a">Matrix with eigenvectors.</param>
    /// <param name="d">Real part of eigenvalues.</param>
    /// <param name="e">Imaginary part of eigenvalues.</param>
    /// <param name="r">Rank of the matrix.</param>
    private unsafe static void SymmetricTridiagonalize(
        double* a, double* d, double* e, int r)
    {
        // Householder reduction to tridiagonal form.
        for (int i = r - 1; i > 0; i--)
        {
            int top = i & Simd.MASK4;
            // Scale to avoid under/overflow.
            double scale = 0.0;
            int kk = 0;
            if (Avx.IsSupported)
            {
                V4d mask = V4.Create(-0d);
                V4d sum = V4d.Zero;
                for (; kk < top; kk += 4)
                    sum += Avx.AndNot(mask, Avx.LoadVector256(d + kk));
                scale = sum.Sum();
            }
            for (; kk < i; kk++)
                scale += Abs(d[kk]);
            if (scale == 0.0)
            {
                e[i] = d[i - 1];
                double* ai = a + i * r;
                for (int j = 0; j < i; j++)
                {
                    d[j] = a[j * r + i - 1];
                    a[j * r + i] = ai[j] = 0.0;
                }
                d[i] = 0;
            }
            else
            {
                // Generate Householder vector.
                double h = 0.0;
                int m = 0;
                if (Avx.IsSupported)
                {
                    V4d sum = V4d.Zero;
                    for (V4d scl = V4.Create(1.0 / scale); m < top; m += 4)
                    {
                        V4d dvec = Avx.LoadVector256(d + m) * scl;
                        sum = sum.MultiplyAdd(dvec, dvec);
                        Avx.Store(d + m, dvec);
                    }
                    h = sum.Sum();
                }
                for (; m < i; m++)
                {
                    d[m] /= scale;
                    h += d[m] * d[m];
                }

                double f = d[i - 1];
                double g = Sqrt(h);
                if (f > 0)
                    g = -g;

                e[i] = scale * g;
                h -= f * g;
                d[i - 1] = f - g;
                m = 0;
                if (Avx.IsSupported)
                {
                    V4d zero = V4d.Zero;
                    for (; m < top; m += 4)
                        Avx.Store(e + m, zero);
                }
                for (; m < i; m++)
                    e[m] = 0.0;

                // Apply similarity transformation to remaining columns.
                double* ai = a + i * r, aj = a;
                for (int j = 0; j < i; j++, aj += r)
                {
                    f = d[j];
                    ai[j] = f;
                    g = FusedMultiplyAdd(aj[j], f, e[j]);
                    int k = j + 1;
                    if (Avx.IsSupported)
                    {
                        V4d vg = V4d.Zero;
                        for (V4d vf = V4.Create(f); k < i - 4; k += 4)
                        {
                            V4d va = Avx.LoadVector256(aj + k);
                            vg = vg.MultiplyAdd(Avx.LoadVector256(d + k), va);
                            Avx.Store(e + k, Avx.LoadVector256(e + k).MultiplyAdd(va, vf));
                        }
                        g += vg.Sum();
                    }
                    for (; k < i; k++)
                    {
                        g += aj[k] * d[k];
                        e[k] += aj[k] * f;
                    }
                    e[j] = g;
                }

                f = 0.0;
                m = 0;
                if (Avx.IsSupported)
                {
                    V4d s = V4d.Zero;
                    for (V4d scl = V4.Create(1.0 / h); m < top; m += 4)
                    {
                        V4d v = Avx.LoadVector256(e + m) * scl;
                        Avx.Store(e + m, v);
                        s = s.MultiplyAdd(v, Avx.LoadVector256(d + m));
                    }
                    f = s.Sum();
                }
                for (; m < i; m++)
                {
                    e[m] /= h;
                    f += e[m] * d[m];
                }

                double hh = f / (h + h);
                m = 0;
                if (Avx.IsSupported)
                    for (V4d v = V4.Create(-hh); m < top; m += 4)
                        Avx.Store(e + m, Avx.LoadVector256(e + m).MultiplyAdd(d + m, v));
                for (; m < i; m++)
                    e[m] -= hh * d[m];

                aj = a;
                for (int j = 0; j < i; j++, aj += r)
                {
                    f = d[j]; g = e[j];
                    int k = j;
                    if (Avx.IsSupported)
                    {
                        V4d vf = V4.Create(f), vg = V4.Create(g);
                        for (; k < i - 4; k += 4)
                            Avx.Store(aj + k, Avx.LoadVector256(aj + k) -
                                (Avx.LoadVector256(d + k) * vg).MultiplyAdd(e + k, vf));
                    }
                    for (; k < i; k++)
                        aj[k] -= f * e[k] + g * d[k];

                    d[j] = aj[i - 1];
                    aj[i] = 0.0;
                }
                d[i] = h;
            }
        }

        // Accumulate transformations.
        double* ai1 = a + r;
        for (int i = 0; i < r - 1; i++, ai1 += r)
        {
            a[i * r + r - 1] = a[i * r + i];
            a[i * r + i] = 1.0;
            double h = d[i + 1];
            int t = (i + 1) & Simd.MASK4;
            if (h != 0.0)
            {
                h = 1.0 / h;
                int k = 0;
                if (Avx.IsSupported)
                    for (V4d v = V4.Create(h); k < t; k += 4)
                        Avx.Store(d + k, Avx.LoadVector256(ai1 + k) * v);
                for (; k <= i; k++)
                    d[k] = ai1[k] * h;
                double* aj = a;
                for (int j = 0; j <= i; j++, aj += r)
                {
                    double g = 0;
                    k = 0;
                    if (Avx.IsSupported)
                    {
                        V4d v = V4d.Zero;
                        for (; k < t; k += 4)
                            v = v.MultiplyAdd(ai1 + k, aj + k);
                        g = v.Sum();
                    }
                    for (; k <= i; k++)
                        g += ai1[k] * aj[k];
                    k = 0;
                    if (Avx.IsSupported)
                        for (V4d vmg = V4.Create(-g); k < t; k += 4)
                            Avx.Store(aj + k, Avx.LoadVector256(aj + k).MultiplyAdd(d + k, vmg));
                    for (; k <= i; k++)
                        aj[k] -= g * d[k];
                }
            }
            int kk = 0;
            if (Avx.IsSupported)
                for (V4d z = V4d.Zero; kk < t; kk += 4)
                    Avx.Store(ai1 + kk, z);
            for (; kk <= i; kk++)
                ai1[kk] = 0.0;
        }

        for (int j = 0, idx = r - 1; j < r; j++, idx += r)
        {
            d[j] = a[idx];
            a[idx] = 0.0;
        }
        a[r * r - 1] = 1.0;
        e[0] = 0.0;
    }

    /// <summary>Symmetric tridiagonal QL algorithm.</summary>
    /// <param name="a">Matrix with eigenvectors.</param>
    /// <param name="d">Real part of eigenvalues.</param>
    /// <param name="e">Imaginary part of eigenvalues.</param>
    /// <param name="r">Rank of the matrix.</param>
    private unsafe static void SymmetricDiagonalize(
        double* a, double* d, double* e, int r)
    {
        const int MAX_ITER = 1000;

        Buffer.MemoryCopy(e + 1, e, (r - 1) * sizeof(double), (r - 1) * sizeof(double));
        e[r - 1] = 0.0;

        double ff = 0.0, tst1 = 0.0, ε = Tolerance.DoublePrecision;
        for (int l = 0; l < r; l++)
        {
            // Find small subdiagonal element.
            tst1 = Max(tst1, Abs(d[l]) + Abs(e[l]));
            int m = l;
            while (m < r & Abs(e[m]) > ε * tst1)
                m++;

            // If m == l, d[l] is an eigenvalue; otherwise, iterate.
            if (m > l)
            {
                int iter = 0;
                do
                {
                    // Compute implicit shift.
                    double g = d[l], el = e[l];
                    double p = (d[l + 1] - g) / (el + el);
                    double rad = Functions.Hypotenuse(p);
                    if (p < 0)
                        rad = -rad;
                    d[l] = el / (p + rad);
                    d[l + 1] = el * (p + rad);

                    double dl1 = d[l + 1], h = g - d[l];
                    int i = l + 2;
                    if (Avx.IsSupported)
                    {
                        V4d vh = V4.Create(h);
                        for (int top = (r - i) & Simd.MASK4 + i; i < top; i += 4)
                            Avx.Store(d + i, Avx.LoadVector256(d + i) - vh);
                    }
                    for (; i < r; i++)
                        d[i] -= h;
                    ff += h;

                    // Implicit QL transformation.
                    p = d[m];
                    double c = 1.0, c2 = 1.0, c3 = 1.0, s = 0.0, s2 = 0.0;
                    double el1 = e[l + 1];
                    double* ai = a + (m - 1) * r;
                    for (i = m - 1; i >= l; i--, ai -= r)
                    {
                        c3 = c2; c2 = c; s2 = s;
                        g = c * e[i];
                        h = c * p;
                        rad = Functions.Hypotenuse(p, e[i]);
                        e[i + 1] = s * rad;
                        s = e[i] / rad;
                        c = p / rad;
                        p = c * d[i] - s * g;
                        d[i + 1] = FusedMultiplyAdd(c * g + s * d[i], s, h);

                        // Accumulate transformation.
                        double* ai1 = ai + r;
                        int k = 0;
                        if (Avx.IsSupported)
                        {
                            V4d vs = V4.Create(s);
                            V4d vc = V4.Create(c);
                            for (int top = r & Simd.MASK4; k < top; k += 4)
                            {
                                V4d vh = Avx.LoadVector256(ai1 + k);
                                V4d vk = Avx.LoadVector256(ai + k);
                                Avx.Store(ai1 + k, (vc * vh).MultiplyAdd(vs, vk));
                                Avx.Store(ai + k, (vs * vh).MultiplySub(vc, vk));
                            }
                        }
                        for (; k < r; k++)
                        {
                            h = ai1[k];
                            ai1[k] = s * ai[k] + c * h;
                            ai[k] = c * ai[k] - s * h;
                        }
                    }
                    p = (-s) * s2 * c3 * el1 * e[l] / dl1;
                    e[l] = s * p;
                    d[l] = c * p;

                    // Check for convergence.
                    if (++iter >= MAX_ITER)
                        throw new ConvergenceException();
                } while (Abs(e[l]) > ε * tst1);
            }
            d[l] += ff;
            e[l] = 0.0;
        }

        // Sort eigenvalues and corresponding vectors.
        for (int i = 0; i < r - 1; i++)
        {
            int k = i;
            double p = d[i];
            for (int j = i + 1; j < r; j++)
                if (d[j] < p)
                    (k, p) = (j, d[j]);
            if (k != i)
            {
                d[k] = d[i];
                d[i] = p;
                double* ai = a + i * r, ak = a + k * r;
                int j = 0;
                if (Avx.IsSupported)
                    for (int t = r & Simd.MASK4; j < t; j += 4)
                    {
                        V4d v = Avx.LoadVector256(ai + j);
                        Avx.Store(ai + j, Avx.LoadVector256(ak + j));
                        Avx.Store(ak + j, v);
                    }
                for (; j < r; j++)
                    (ai[j], ak[j]) = (ak[j], ai[j]);
            }
        }
    }

    /// <summary>Reduces a non-symmetric matrix to Hessenberg form.</summary>
    /// <param name="a">Matrix with eigenvectors.</param>
    /// <param name="h">Internal storage for Hessenberg form.</param>
    /// <param name="ort">Scratch storage.</param>
    /// <param name="rank">The rank of the matrix</param>
    private unsafe static void ReduceToHessenberg(double* a, double* h, double* ort, int rank)
    {
        for (int m = 1, high = rank - 1; m < high; m++)
        {
            int mm1O = (m - 1) * rank;
            int top = (rank - m) & Simd.MASK4 + m;
            // Scale column.
            double scale = 0.0;
            int ii = m;
            if (Avx.IsSupported)
            {
                V4d sum = V4d.Zero;
                V4d mask = V4.Create(-0d);
                for (; ii < top; ii += 4)
                    sum += Avx.AndNot(mask, Avx.LoadVector256(h + mm1O + ii));
                scale = sum.Sum();
            }
            for (; ii < rank; ii++)
                scale += Abs(h[mm1O + ii]);

            if (scale != 0.0)
            {
                // Compute Householder transformation.
                double hh = 0.0;
                int i = m;
                if (Avx.IsSupported)
                {
                    V4d vsc = V4.Create(1d / scale);
                    V4d vhh = V4d.Zero;
                    for (; i < top; i += 4)
                    {
                        V4d v = Avx.LoadVector256(h + mm1O + i) * vsc;
                        Avx.Store(ort + i, v);
                        vhh = vhh.MultiplyAdd(v, v);
                    }
                    hh = vhh.Sum();
                }
                for (; i < rank; i++)
                {
                    ort[i] = h[mm1O + i] / scale;
                    hh += ort[i] * ort[i];
                }
                double g = Sqrt(hh);
                if (ort[m] > 0)
                    g = -g;
                hh -= ort[m] * g;
                ort[m] -= g;

                // Apply Householder similarity transformation.
                for (int j = m, jO = m * rank; j < rank; j++, jO += rank)
                {
                    double f = 0.0;
                    i = m;
                    if (Avx.IsSupported)
                    {
                        V4d vf = V4d.Zero;
                        for (; i < top; i += 4)
                            vf = vf.MultiplyAdd(ort + i, h + jO + i);
                        f = vf.Sum();
                    }
                    for (; i < rank; i++)
                        f += ort[i] * h[jO + i];
                    f /= hh;

                    i = m;
                    if (Avx.IsSupported)
                        for (V4d vf = V4.Create(f); i < top; i += 4)
                            Avx.Store(h + jO + i,
                                Avx.LoadVector256(h + jO + i).MultiplyAddNeg(ort + i, vf));
                    for (; i < rank; i++)
                        h[jO + i] -= f * ort[i];
                }

                for (i = 0; i < rank; i++)
                {
                    double f = 0.0;
                    for (int j = high; j >= m; j--)
                        f += ort[j] * h[j * rank + i];
                    f /= hh;

                    for (int j = m; j < rank; j++)
                        h[j * rank + i] -= f * ort[j];
                }

                ort[m] *= scale;
                h[mm1O + m] = scale * g;
            }
        }

        // Accumulate transformations.
        for (int m = rank - 2, mm1O = (m - 1) * rank; m >= 1; m--, mm1O -= rank)
        {
            if (h[mm1O + m] != 0.0)
            {
                int k = m + 1;
                if (Avx.IsSupported)
                    for (int t = (rank - m - 1) & Simd.MASK4 + m + 1; k < t; k += 4)
                        Avx.Store(ort + k, Avx.LoadVector256(h + mm1O + k));
                for (; k < rank; k++)
                    ort[k] = h[mm1O + k];
                int top = (rank - m) & Simd.MASK4 + m;
                for (int j = m, jO = m * rank; j < rank; j++, jO += rank)
                {
                    double g = 0.0;
                    int i = m;
                    if (Avx.IsSupported)
                    {
                        V4d vg = V4d.Zero;
                        for (; i < top; i += 4)
                            vg = vg.MultiplyAdd(ort + i, a + jO + i);
                        g = vg.Sum();
                    }
                    for (; i < rank; i++)
                        g += ort[i] * a[jO + i];
                    // Double division avoids possible underflow
                    g = g / ort[m] / h[mm1O + m];
                    i = m;
                    if (Avx.IsSupported)
                        for (V4d vg = V4.Create(g); i < top; i += 4)
                            Avx.Store(a + jO + i,
                                Avx.LoadVector256(a + jO + i).MultiplyAdd(ort + i, vg));
                    for (; i < rank; i++)
                        a[jO + i] += g * ort[i];
                }
            }
        }
    }

    /// <summary>Reduction from Hessenberg to real Schur form.</summary>
    /// <param name="a">Matrix with eigenvectors.</param>
    /// <param name="h">Internal storage for Hessenberg form.</param>
    /// <param name="d">Real part of eigenvalues.</param>
    /// <param name="e">Imaginary part of eigenvalues.</param>
    /// <param name="rank">The rank of the original matrix.</param>
    internal unsafe static void ReduceToSchur(
        double* a, double* h, double* d, double* e, int rank)
    {
        // Initialize
        int n = rank - 1;
        double ε = Pow(2.0, -52.0), exshift = 0.0;
        double p = 0, q = 0, r = 0, s = 0, z = 0;

        // Compute matrix norm.
        double norm = 0.0;
        for (int j = 0, k = 0; j < rank; j++, k += rank)
            norm += Abs(h[k]);
        for (int i = 1; i < rank; i++)
            for (int j = i - 1, k = j * rank + i; j < rank; j++, k += rank)
                norm += Abs(h[k]);

        // Outer loop over eigenvalue index
        for (int iter = 0; n >= 0;)
        {
            // Look for single small sub-diagonal element
            int l = n;
            for (int lm1O = (l - 1) * rank; l > 0; l--, lm1O -= rank)
            {
                s = Abs(h[lm1O + l - 1]) + Abs(h[lm1O + rank + l]);
                if (s == 0.0)
                    s = norm;
                if (Abs(h[lm1O + l]) < ε * s)
                    break;
            }

            // Check for convergence
            if (l == n)
            {
                // One root found
                d[n] = h[n * rank + n] += exshift;
                e[n] = 0.0;
                n--;
                iter = 0;
            }
            else if (l == n - 1)
            {
                // Two roots found
                int nO = n * rank, nm1 = n - 1, nm1O = nm1 * rank, nOn = nO + n;
                double w = h[nm1O + n] * h[nO + nm1];
                p = (h[nm1O + nm1] - h[nOn]) * 0.5;
                q = FusedMultiplyAdd(p, p, w);
                z = Sqrt(Abs(q));
                h[nOn] += exshift;
                h[nm1O + nm1] += exshift;
                double x = h[nOn];
                if (q >= 0)
                {
                    // Real pair
                    z = p >= 0 ? p + z : p - z;
                    d[nm1] = x + z;
                    d[n] = d[nm1];
                    if (z != 0.0)
                        d[n] = x - w / z;

                    e[n - 1] = e[n] = 0.0;
                    x = h[nm1O + n];
                    s = Abs(x) + Abs(z);
                    p = x / s; q = z / s;
                    r = Sqrt(p * p + q * q);
                    p /= r; q /= r;

                    // Row modification
                    for (int j = n - 1, jO = j * rank; j < rank; j++, jO += rank)
                    {
                        int jOn = jO + n;
                        z = h[jO + nm1];
                        h[jO + nm1] = q * z + p * h[jOn];
                        h[jOn] = q * h[jOn] - p * z;
                    }

                    // Column modification
                    int i = 0;
                    if (Avx.IsSupported)
                    {
                        V4d vp = V4.Create(p);
                        V4d vq = V4.Create(q);
                        for (int top = (n + 1) & Simd.MASK4; i < top; i += 4)
                        {
                            V4d vz = Avx.LoadVector256(h + nm1O + i);
                            V4d va = Avx.LoadVector256(h + nO + i);
                            Avx.Store(h + nm1O + i, (vp * va).MultiplyAdd(vq, vz));
                            Avx.Store(h + nO + i, (vq * va).MultiplyAddNeg(vp, vz));
                        }
                    }
                    for (; i <= n; i++)
                    {
                        int nOi = nO + i;
                        z = h[nm1O + i];
                        h[nm1O + i] = q * z + p * h[nOi];
                        h[nOi] = q * h[nOi] - p * z;
                    }

                    // Accumulate transformations
                    i = 0;
                    if (Avx.IsSupported)
                    {
                        V4d vp = V4.Create(p);
                        V4d vq = V4.Create(q);
                        for (int top = rank & Simd.MASK4; i < top; i += 4)
                        {
                            V4d vz = Avx.LoadVector256(a + nm1O + i);
                            Avx.Store(a + nm1O + i, (vq * vz).MultiplyAdd(a + nO + i, vp));
                            Avx.Store(a + nO + i,
                                (vq * Avx.LoadVector256(a + nO + i)).MultiplyAddNeg(vp, vz));
                        }
                    }
                    for (; i < rank; i++)
                    {
                        int nOi = nO + i;
                        z = a[nm1O + i];
                        a[nm1O + i] = q * z + p * a[nOi];
                        a[nOi] = q * a[nOi] - p * z;
                    }
                }
                else
                {
                    // Complex pair
                    d[n - 1] = d[n] = x + p;
                    e[n - 1] = z;
                    e[n] = -z;
                }
                n -= 2;
                iter = 0;
            }
            else
            {
                // No convergence yet
                int nO = n * rank, nm1 = n - 1, nm1O = nm1 * rank, nOn = nO + n;
                // Form shift
                double x = h[nOn], y = 0.0, w = 0.0;
                if (l < n)
                {
                    y = h[nm1O + nm1];
                    w = h[nm1O + n] * h[nO + nm1];
                }
                // An ad hoc shift.
                if (iter == 10)
                {
                    exshift += x;
                    for (int i = 0; i <= n; i++)
                        h[i * rank + i] -= x;
                    s = Abs(h[nm1O + n]) + Abs(h[(n - 2) * rank + nm1]);
                    x = y = 0.75 * s;
                    w = (-0.4375) * s * s;
                }
                // And then, another ad hoc shift.
                else if (iter == 30)
                {
                    s = (y - x) * 0.5;
                    s = s * s + w;
                    if (s > 0)
                    {
                        s = Sqrt(s);
                        if (y < x)
                            s = -s;
                        s = x - w / FusedMultiplyAdd(y - x, 0.5, s);
                        for (int i = 0; i <= n; i++)
                            h[i * rank + i] -= s;
                        exshift += s;
                        x = y = w = 0.964;
                    }
                }

                if (++iter >= 30 * rank)
                    throw new ConvergenceException();

                // Look for two consecutive small sub-diagonal elements
                int m = n - 2;
                for (; m >= l; m--)
                {
                    int mp1 = m + 1, mm1 = m - 1;
                    int mO = m * rank, mp1O = mp1 * rank, mm1O = mm1 * rank;

                    z = h[mO + m];
                    r = x - z;
                    s = y - z;
                    p = (r * s - w) / h[mO + mp1] + h[mp1O + m];
                    q = h[mp1O + mp1] - z - r - s;
                    r = h[mp1O + m + 2];
                    s = Abs(p) + Abs(q) + Abs(r);
                    p /= s; q /= s; r /= s;

                    if (m == l ||
                        Abs(h[mm1O + m]) * (Abs(q) + Abs(r)) < ε * (Abs(p)
                        * (Abs(h[mm1O + mm1]) + Abs(z) + Abs(h[mp1O + mp1]))))
                    {
                        break;
                    }
                }

                h[m * rank + m + 2] = 0.0;
                for (int i = m + 3; i <= n; i++)
                    h[(i - 2) * rank + i] = h[(i - 3) * rank + i] = 0.0;

                // Double QR step involving rows l:n and columns m:n
                for (int k = m; k < n; k++)
                {
                    bool notlast = k != n - 1;
                    int kO = k * rank, kp1 = k + 1, kp2 = k + 2;
                    int kp1O = kp1 * rank, kp2O = kp2 * rank, km1O = (k - 1) * rank;
                    if (k != m)
                    {
                        p = h[km1O + k];
                        q = h[km1O + kp1];
                        r = notlast ? h[km1O + kp2] : 0.0;
                        x = Abs(p) + Abs(q) + Abs(r);
                        if (x == 0.0)
                            continue;
                        p /= x; q /= x; r /= x;
                    }

                    s = Sqrt(p * p + q * q + r * r);
                    if (p < 0)
                        s = -s;

                    if (s != 0.0)
                    {
                        if (k != m)
                            h[km1O + k] = (-s) * x;
                        else if (l != m)
                            h[km1O + k] = -h[km1O + k];

                        p += s;
                        x = p / s; y = q / s; z = r / s;
                        q /= p; r /= p;

                        // Row modification
                        for (int j = k; j < rank; j++)
                        {
                            int jO = j * rank, jOk = jO + k, jOkp1 = jO + kp1, jOkp2 = jO + kp2;
                            p = h[jOk] + q * h[jOkp1];
                            if (notlast)
                            {
                                p += r * h[jOkp2];
                                h[jOkp2] -= p * z;
                            }
                            h[jOk] -= p * x;
                            h[jOkp1] -= p * y;
                        }

                        // Column modification
                        int upper = Min(n, k + 3);
                        int i = 0;
                        if (Avx.IsSupported)
                        {
                            V4d vx = V4.Create(x);
                            V4d vy = V4.Create(y);
                            V4d vz = V4.Create(z);
                            V4d vr = V4.Create(r);
                            V4d vq = V4.Create(q);
                            for (int top = (upper + 1) & Simd.MASK4; i < top; i += 4)
                            {
                                V4d v1 = Avx.LoadVector256(h + kO + i);
                                V4d v2 = Avx.LoadVector256(h + kp1O + i);
                                V4d vp = (vx * v1).MultiplyAdd(vy, v2);
                                if (notlast)
                                {
                                    V4d v3 = Avx.LoadVector256(h + kp2O + i);
                                    vp = vp.MultiplyAdd(vz, v3);
                                    Avx.Store(h + kp2O + i, v3.MultiplyAddNeg(vp, vr));
                                }
                                Avx.Store(h + kO + i, v1 - vp);
                                Avx.Store(h + kp1O + i, v2.MultiplyAddNeg(vp, vq));
                            }
                        }
                        for (; i <= upper; i++)
                        {
                            p = x * h[kO + i] + y * h[kp1O + i];
                            if (notlast)
                            {
                                p = FusedMultiplyAdd(z, h[kp2O + i], p);
                                h[kp2O + i] -= p * r;
                            }
                            h[kO + i] -= p;
                            h[kp1O + i] -= p * q;
                        }

                        // Accumulate transformations
                        i = 0;
                        if (Avx.IsSupported)
                        {
                            V4d vx = V4.Create(x), vy = V4.Create(y), vz = V4.Create(z);
                            V4d vr = V4.Create(r), vq = V4.Create(q);
                            for (int top = rank & Simd.MASK4; i < top; i += 4)
                            {
                                V4d v1 = Avx.LoadVector256(a + kO + i);
                                V4d v2 = Avx.LoadVector256(a + kp1O + i);
                                V4d vp = (vy * v2).MultiplyAdd(vx, v1);
                                if (notlast)
                                {
                                    V4d v3 = Avx.LoadVector256(a + kp2O + i);
                                    vp = vp.MultiplyAdd(vz, v3);
                                    Avx.Store(a + kp2O + i, v3.MultiplyAddNeg(vp, vr));
                                }
                                Avx.Store(a + kO + i, v1 - vp);
                                Avx.Store(a + kp1O + i, v2.MultiplyAddNeg(vp, vq));
                            }
                        }
                        for (; i < rank; i++)
                        {
                            p = x * a[kO + i] + y * a[kp1O + i];
                            if (notlast)
                            {
                                p += z * a[kp2O + i];
                                a[kp2O + i] -= p * r;
                            }
                            a[kO + i] -= p;
                            a[kp1O + i] -= p * q;
                        }
                    } // (s != 0)
                } // k loop
            } // Check convergence
        } // for (int iter = 0; n >= 0;)

        // Backsubstitute to find vectors of upper triangular form
        if (norm == 0.0)
            return;

        for (n = rank - 1; n >= 0; n--)
        {
            int nO = n * rank, nm1 = n - 1, nm1O = nm1 * rank;
            p = d[n];
            q = e[n];
            if (q == 0.0)
            {
                // Real vector
                int l = n;
                h[nO + n] = 1.0;
                for (int i = n - 1; i >= 0; i--)
                {
                    int ip1 = i + 1, iO = i * rank, ip1O = ip1 * rank;
                    double w = h[iO + i] - p;
                    r = 0.0;
                    for (int j = l; j <= n; j++)
                        r += h[j * rank + i] * h[nO + j];

                    if (e[i] < 0.0)
                        (z, s) = (w, r);
                    else
                    {
                        l = i;
                        if (e[i] == 0.0)
                            h[nO + i] = w != 0.0 ? (-r) / w : (-r) / (ε * norm);
                        else
                        {
                            // Solve real equations
                            double x = h[ip1O + i], y = h[iO + ip1];
                            q = (d[i] - p) * (d[i] - p) + e[i] * e[i];
                            double u = (x * s - z * r) / q;
                            h[nO + i] = u;
                            h[nO + ip1] = Abs(x) > Abs(z)
                                ? (-r - w * u) / x : (-s - y * u) / z;
                        }

                        // Overflow control
                        double t = Abs(h[nO + i]);
                        if (ε * t * t > 1)
                        {
                            int j = i;
                            if (Avx.IsSupported)
                                for (V4d vt = V4.Create(1 / t); j + 4 <= n; j += 4)
                                    Avx.Store(h + nO + j, Avx.LoadVector256(h + nO + j) * vt);
                            for (; j <= n; j++)
                                h[nO + j] /= t;
                        }
                    }
                }
            }
            else if (q < 0)
            {
                // Complex vector
                int l = n - 1;
                // Last vector component is imaginary, so the matrix is triangular.
                if (Abs(h[nm1O + n]) > Abs(h[nO + nm1]))
                {
                    h[nm1O + nm1] = q / h[nm1O + n];
                    h[nO + nm1] = (-(h[nO + n] - p)) / h[nm1O + n];
                }
                else
                    (h[nm1O + nm1], h[nO + nm1]) = CDv(0.0, -h[nO + nm1], h[nm1O + nm1] - p, q);

                h[nm1O + n] = 0.0;
                h[nO + n] = 1.0;
                for (int i = n - 2; i >= 0; i--)
                {
                    int ip1 = i + 1, iO = i * rank, ip1O = ip1 * rank;
                    double ra = 0.0, sa = 0.0;
                    for (int j = l; j <= n; j++)
                    {
                        double hjOi = h[j * rank + i];
                        ra += hjOi * h[nm1O + j];
                        sa += hjOi * h[nO + j];
                    }

                    double w = h[iO + i] - p;
                    if (e[i] < 0.0)
                        (z, r, s) = (w, ra, sa);
                    else
                    {
                        l = i;
                        if (e[i] == 0.0)
                            (h[nm1O + i], h[nO + i]) = CDv(-ra, -sa, w, q);
                        else
                        {
                            // Solve complex equations
                            double x = h[ip1O + i], y = h[iO + ip1];
                            double vr = ((d[i] - p) * (d[i] - p)) + e[i] * e[i] - q * q;
                            double vi = (d[i] - p) * 2.0 * q;
                            if (vr == 0.0 && vi == 0.0)
                                vr = ε * norm * (Abs(w) + Abs(q) + Abs(x) + Abs(y) + Abs(z));

                            (h[nm1O + i], h[nO + i]) = CDv(
                                x * r - z * ra + q * sa, x * s - z * sa - q * ra, vr, vi);
                            if (Abs(x) > Abs(z) + Abs(q))
                            {
                                h[nm1O + ip1] = (-ra - w * h[nm1O + i] + q * h[nO + i]) / x;
                                h[nO + ip1] = (-sa - w * h[nO + i] - q * h[nm1O + i]) / x;
                            }
                            else
                                (h[nm1O + ip1], h[nO + ip1]) = CDv(
                                    -r - y * h[nm1O + i], -s - y * h[nO + i], z, q);
                        }

                        // Overflow control
                        double t = Max(Abs(h[nm1O + i]), Abs(h[nO + i]));
                        if (ε * t * t > 1)
                        {
                            int j = i;
                            if (Avx.IsSupported)
                                for (V4d vt = V4.Create(1 / t); j + 4 <= n; j += 4)
                                {
                                    Avx.Store(h + nm1O + j, Avx.LoadVector256(h + nm1O + j) * vt);
                                    Avx.Store(h + nO + j, Avx.LoadVector256(h + nO + j) * vt);
                                }
                            for (; j <= n; j++)
                            {
                                h[nm1O + j] /= t;
                                h[nO + j] /= t;
                            }
                        }
                    }
                }
            }
        }

        // Back transformation to get eigenvectors of original matrix
        for (int j = rank - 1; j >= 0; j--)
        {
            int jO = j * rank;
            for (int i = 0; i < rank; i++)
            {
                z = 0.0;
                int k = 0;
                if (Avx2.IsSupported)
                {
                    V4d acc = V4d.Zero;
                    Vector128<int> vx = Vector128.Create(0, rank, 2 * rank, 3 * rank);
                    for (double* pa = a + i; k + 4 <= j; k += 4, pa += 4 * rank)
                        acc = acc.MultiplyAdd(h + jO + k, Avx2.GatherVector256(pa, vx, 8));
                    z = acc.Sum();
                }
                for (; k <= j; k++)
                    z += a[k * rank + i] * h[jO + k];
                a[jO + i] = z;
            }
        }

        // Complex division.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (double, double) CDv(double xr, double xi, double yr, double yi)
        {
            if (Abs(yi) < Abs(yr))
            {
                double d = yi / yr, den = yr + yi * d;
                return ((xr + xi * d) / den, (xi - xr * d) / den);
            }
            else
            {
                double d = yr / yi, den = yi + yr * d;
                return ((xi + xr * d) / den, (-xr + xi * d) / den);
            }
        }
    }

    /// <summary>Creates a block diagonal matrix from the eigenvalues.</summary>
    /// <param name="values">The calculated eigenvalues.</param>
    /// <returns>A block diagonal matrix with eigenvalues.</returns>
    private static Matrix CreateDiagonal(ComplexVector values)
    {
        int order = values.Length;
        Matrix result = new(order, order);
        for (int i = 0; i < order; i++)
        {
            Complex c = values[i];
            result[i, i] = c.Real;
            if (c.Imaginary > 0)
                result[i, i + 1] = c.Imaginary;
            else if (c.Imaginary < 0)
                result[i, i - 1] = c.Imaginary;
        }
        return result;
    }

    /// <summary>
    /// Gets the absolute value of determinant of the square matrix 
    /// for which the EVD was computed.
    /// </summary>
    /// <returns>The product of the magnitudes of eigenvalues.</returns>
    public double Determinant()
    {
        Complex det = Complex.One;
        foreach (Complex v in Values)
        {
            det *= v;
            if (v.AlmostZero())
                return 0;
        }
        return det.Magnitude;
    }

    /// <summary>Gets the effective numerical matrix rank.</summary>
    /// <returns>The number of non-negligible singular values.</returns>
    public int Rank()
    {
        int rank = 0;
        foreach (Complex v in Values)
            if (!v.AlmostZero())
                rank++;
        return rank;
    }

    /// <summary>Gets a textual representation of this factorization.</summary>
    /// <returns>The diagonal eigenvalues matrix plus the eigenvectors matrix.</returns>
    public override string ToString() =>
        "Eigenvalues:" + Environment.NewLine +
        D.ToString() + Environment.NewLine +
        "Eigenvectors:" + Environment.NewLine +
        Vectors.ToString();

    /// <summary>Gets a textual representation of this factorization.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>The diagonal eigenvalues matrix plus the eigenvectors matrix.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        "Eigenvalues:" + Environment.NewLine +
        D.ToString(format, provider) + Environment.NewLine +
        "Eigenvectors:" + Environment.NewLine +
        Vectors.ToString(format, provider);
}
