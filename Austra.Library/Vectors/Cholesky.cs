namespace Austra.Library;

/// <summary>Represents the result of a Cholesky decomposition.</summary>
/// <remarks>Initializes a Cholesky decomposition.</remarks>
/// <param name="matrix">A lower triangular matrix.</param>
public readonly struct Cholesky(LMatrix matrix) : IFormattable
{
    /// <summary>Gets the Cholesky lower triangular matrix.</summary>
    public LMatrix L { get; } = matrix;

    /// <summary>Tentative Cholesky decomposition of a matrix.</summary>
    /// <param name="matrix">The matrix to decompose.</param>
    /// <param name="cholesky">Contains a full or partial decomposition.</param>
    /// <returns><see langword="true"/> when successful.</returns>
    [SkipLocalsInit]
    internal unsafe static bool TryDecompose(Matrix matrix, out Cholesky cholesky)
    {
        int n = matrix.Rows;
        cholesky = new(new(n));
        fixed (double* pS = (double[])matrix, pD = (double[])cholesky.L)
        {
            // Allocate a buffer.
            double* tmp = stackalloc double[n + n];
            // First column is special.
            double ajj = *pS;
            if (ajj <= 0)
            {
                *pD = double.NaN;
                return false;
            }
            *pD = ajj = Sqrt(ajj);
            double r = 1 / ajj;
            int rows = n--, max = n * rows;
            for (int i = rows; i <= max; i += rows)
                pD[i] = pS[i] * r;
            for (int j = 1; j <= n; j++)
            {
                // Compute the diagonal cell.
                double* pDj = pD + j * rows;
                double v = 0.0;
                int m = 0;
                if (Avx512F.IsSupported)
                {
                    V8d acc = V8d.Zero;
                    for (int top = j & Simd.MASK8; m < top; m += V8d.Count)
                    {
                        V8d vec = Avx512F.LoadVector512(pDj + m);
                        acc = Avx512F.FusedMultiplyAdd(vec, vec, acc);
                    }
                    v = V8.Sum(acc);
                }
                else if (Avx.IsSupported)
                {
                    V4d acc = V4d.Zero;
                    for (int top = j & Simd.MASK4; m < top; m += V4d.Count)
                    {
                        V4d vec = Avx.LoadVector256(pDj + m);
                        acc = acc.MultiplyAdd(vec, vec);
                    }
                    v = acc.Sum();
                }
                for (; m < j; m++)
                {
                    double a = pDj[m];
                    v += a * a;
                }
                ajj = pS[j * rows + j] - v;
                if (ajj <= 0)
                {
                    pDj[j] = double.NaN;
                    return false;
                }
                pDj[j] = ajj = Sqrt(ajj);

                // Compute the other cells of column J.
                if (j < n)
                {
                    r = 1 / ajj;
                    Buffer.MemoryCopy(
                        pDj, tmp, sizeof(double) * j, sizeof(double) * j);
                    for (int i = j; i < n; i++)
                    {
                        double* pDi = pD + (i + 1) * rows;
                        tmp[i] = new Span<double>(pDi, j).DotProduct(new Span<double>(tmp, j));
                    }
                    for (int i = j, idx = (j + 1) * rows + j; i < n; i++, idx += rows)
                        pD[idx] = (pS[idx] - tmp[i]) * r;
                }
            }
        }
        return true;
    }

    /// <summary>Solves the equation Ax = b for x.</summary>
    /// <param name="v">The right side of the equation.</param>
    /// <returns>The solving vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector Solve(DVector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Requires(L.Rows == v.Length);

        return SolveInPlace(v.Clone());
    }

    /// <summary>Solves the equation Ax = b for x.</summary>
    /// <param name="input">The right side of the equation.</param>
    /// <param name="output">Receives the solution.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Solve(DVector input, DVector output)
    {
        Contract.Requires(input.IsInitialized);
        Contract.Requires(L.Rows == input.Length);
        Contract.Requires(output.IsInitialized);
        Contract.Requires(L.Rows == output.Length);

        input.CopyTo(output);
        SolveInPlace(output);
    }

    /// <summary>Solves the equation AX = B for the matrix X.</summary>
    /// <param name="m">The right side of the equation.</param>
    /// <returns>The solving matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Solve(Matrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Requires(m.IsSquare);
        Contract.Requires(L.Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);

        return SolveInPlace(m.Clone());
    }

    /// <summary>Solves the equation AX = B for the matrix X.</summary>
    /// <param name="input">The right side of the equation.</param>
    /// <param name="output">Receives the solution.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Solve(Matrix input, Matrix output)
    {
        Contract.Requires(input.IsInitialized);
        Contract.Requires(input.IsSquare);
        Contract.Requires(L.Rows == input.Rows);
        Contract.Requires(output.IsInitialized);
        Contract.Requires(output.IsSquare);
        Contract.Requires(L.Rows == output.Rows);

        input.CopyTo(output);
        SolveInPlace(output);
    }

    /// <summary>Solves the equation Ax = b for x, in place.</summary>
    /// <param name="v">The right side of the equation.</param>
    /// <returns>Echoes the input vector.</returns>
    private unsafe DVector SolveInPlace(DVector v)
    {
        fixed (double* pA = (double[])L, pB = (double[])v)
        {
            int size = L.Rows;
            double* pAi = pA;
            for (int i = 0; i < size; i++, pAi += size)
                pB[i] = (pB[i] - new Span<double>(pAi, i)
                    .DotProduct(new Span<double>(pB, i))) / pAi[i];
            int s4 = 4 * size, s8 = s4 + s4;
            for (int i = size - 1; i >= 0; i--)
            {
                double sum = pB[i];
                double* p = pA + ((i + 1) * size + i);
                int k = i + 1;
                if (Avx512F.IsSupported)
                {
                    V8d acc = V8d.Zero;
                    Vector128<int> vx = Vector128.Create(0, size, 2 * size, 3 * size);
                    Vector128<int> vy = vx + Vector128.Create(s4);
                    for (int t = (size - i - 1) & Simd.MASK8 + i + 1; k < t; k += 8, p += s8)
                        acc = Avx512F.FusedMultiplyAdd(Avx512F.LoadVector512(pB + k),
                            V8.Create(Avx2.GatherVector256(p, vx, 8),
                            Avx2.GatherVector256(p, vy, 8)), acc);
                    sum -= V8.Sum(acc);
                }
                else if (Avx2.IsSupported)
                {
                    V4d acc = V4d.Zero;
                    Vector128<int> vx = Vector128.Create(0, size, 2 * size, 3 * size);
                    for (; k < size - 4; k += 4, p += s4)
                        acc = acc.MultiplyAdd(pB + k, Avx2.GatherVector256(p, vx, 8));
                    sum -= acc.Sum();
                }
                for (; k < size; k++, p += size)
                    sum = FusedMultiplyAdd(-*p, pB[k], sum);
                pB[i] = sum / pA[i * size + i];
            }
        }
        return v;
    }

    /// <summary>Solves the equation AX = B for the matrix X, in place.</summary>
    /// <param name="m">The right side of the equation.</param>
    /// <returns>Echoes the input matrix.</returns>
    private unsafe Matrix SolveInPlace(Matrix m)
    {
        int size = L.Rows;
        fixed (double* pA = (double[])L, pB = (double[])m)
        {
            int top = Avx512F.IsSupported ? (size & Simd.MASK8) : (size & Simd.MASK4);
            for (int i = 0, isize = 0; i < size; i++, isize += size)
            {
                double* pbi = pB + isize;
                for (int k = i - 1; k >= 0; k--)
                    new Span<double>(pB + k * size, size)
                        .MulNegStore(pA[isize + k], new Span<double>(pbi, size));
                double m1 = 1.0 / pA[isize + i];
                int j = 0;
                if (Avx512F.IsSupported)
                    for (V8d vm1 = V8.Create(m1); j < top; j += V8d.Count)
                        Avx512F.Store(pbi + j, Avx512F.LoadVector512(pbi + j) * vm1);
                else if (Avx.IsSupported)
                    for (V4d vm1 = V4.Create(m1); j < top; j += V4d.Count)
                        Avx.Store(pbi + j, Avx.LoadVector256(pbi + j) * vm1);
                for (; j < size; j++)
                    pbi[j] *= m1;
            }
            for (int i = size - 1, isize = i * size; i >= 0; i--, isize -= size)
            {
                double* pbi = pB + isize;
                for (int k = i + 1; k < size; k++)
                {
                    int ksize = k * size;
                    new Span<double>(pB + ksize, size)
                        .MulNegStore(pA[ksize + i], new Span<double>(pbi, size));
                }
                double m1 = 1.0 / pA[isize + i];
                int j = 0;
                if (Avx512F.IsSupported)
                    for (V8d vm1 = V8.Create(m1); j < top; j += V8d.Count)
                        Avx512F.Store(pbi + j, Avx512F.LoadVector512(pbi + j) * vm1);
                else if (Avx.IsSupported)
                    for (V4d vm1 = V4.Create(m1); j < top; j += V4d.Count)
                        Avx.Store(pbi + j, Avx.LoadVector256(pbi + j) * vm1);
                for (; j < size; j++)
                    pbi[j] *= m1;
            }
        }
        return m;
    }

    /// <summary>Gets the determinant of the underlying matrix.</summary>
    /// <returns>The squared product of the main diagonal.</returns>
    public double Determinant()
    {
        double det = L.Determinant();
        return det * det;
    }

    /// <summary>Gets a textual representation of this decomposition.</summary>
    /// <returns>One line for each row, with space separated columns.</returns>
    public override string ToString() => L.ToString();

    /// <summary>Gets a textual representation of this decomposition.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>One line for each row, with space separated columns.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        L.ToString(format, provider);
}
