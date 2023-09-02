namespace Austra.Library;

/// <summary>Represents the result of a Cholesky decomposition.</summary>
public readonly struct Cholesky
{
    /// <summary>Initializes a Cholesky decomposition.</summary>
    /// <param name="matrix">A lower triangular matrix.</param>
    public Cholesky(LMatrix matrix) => L = matrix;

    /// <summary>Gets the Cholesky lower triangular matrix.</summary>
    public LMatrix L { get; }

    /// <summary>Tentative Cholesky decomposition of a matrix.</summary>
    /// <param name="matrix">The matrix to decompose.</param>
    /// <param name="cholesky">Contains a full or partial decomposition.</param>
    /// <returns><see langword="true"/> when successful.</returns>
    [SkipLocalsInit]
    internal unsafe static bool TryDecompose(Matrix matrix, out Cholesky cholesky)
    {
        int n = matrix.Rows;
        double[,] dest = new double[n, n];
        cholesky = new(dest);
        fixed (double* pS = (double[,])matrix, pD = dest)
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
                if (Avx.IsSupported)
                {
                    Vector256<double> acc = Vector256<double>.Zero;
                    for (int top = j & Simd.AVX_MASK; m < top; m += 4)
                    {
                        Vector256<double> vec = Avx.LoadVector256(pDj + m);
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
                        v = 0.0;
                        double* pDi = pD + (i + 1) * rows;
                        int k = 0;
                        if (Avx.IsSupported)
                        {
                            Vector256<double> acc = Vector256<double>.Zero;
                            for (int top = j & Simd.AVX_MASK; k < top; k += 4)
                                acc = acc.MultiplyAdd(pDi + k, tmp + k);
                            v = acc.Sum();
                        }
                        for (; k < j; k++)
                            v = FusedMultiplyAdd(pDi[k], tmp[k], v);
                        tmp[i] = v;
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
    public Vector Solve(Vector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Requires(L.Rows == v.Length);

        return SolveInPlace(v.Clone());
    }

    /// <summary>Solves the equation Ax = b for x.</summary>
    /// <param name="input">The right side of the equation.</param>
    /// <param name="output">Receives the solution.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Solve(Vector input, Vector output)
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
    private unsafe Vector SolveInPlace(Vector v)
    {
        fixed (double* pA = (double[,])L, pB = (double[])v)
        {
            int size = L.Rows;
            double* pAi = pA;
            for (int i = 0; i < size; i++, pAi += size)
            {
                double sum = pB[i];
                int k = 0;
                if (Avx.IsSupported)
                {
                    var acc = Vector256<double>.Zero;
                    for (int top = i & Simd.AVX_MASK; k < top; k += 4)
                        acc = acc.MultiplyAdd(pAi + k, pB + k);
                    sum -= acc.Sum();
                }
                for (; k < i; k++)
                    sum -= pAi[k] * pB[k];
                pB[i] = sum / pAi[i];
            }
            for (int i = size - 1; i >= 0; i--)
            {
                double sum = pB[i];
                double* p = pA + ((i + 1) * size + i);
                int k = i + 1;
                if (Avx2.IsSupported)
                {
                    var acc = Vector256<double>.Zero;
                    var vx = Vector128.Create(0, size, 2 * size, 3 * size);
                    for (; k < size - 4; k += 4, p += 4 * size)
                        acc = acc.MultiplyAdd(pB + k, Avx2.GatherVector256(p, vx, 8));
                    sum -= acc.Sum();
                }
                for (; k < size; k++, p += size)
                    sum -= *p * pB[k];
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
        fixed (double* pA = (double[,])L, pB = (double[,])m)
        {
            int top = size & Simd.AVX_MASK;
            for (int i = 0, isize = 0; i < size; i++, isize += size)
            {
                double* pbi = pB + isize;
                for (int k = i - 1; k >= 0; k--)
                {
                    double mult = pA[isize + k];
                    double* pbk = pB + k * size;
                    int l = 0;
                    if (Avx.IsSupported)
                    {
                        Vector256<double> vm = Vector256.Create(mult);
                        for (; l < top; l += 4)
                            Avx.Store(pbi + l,
                                Avx.LoadVector256(pbi + l).MultiplyAddNeg(pbk + l, vm));
                    }
                    for (; l < size; l++)
                        pbi[l] -= pbk[l] * mult;
                }
                double m1 = 1.0 / pA[isize + i];
                int j = 0;
                if (Avx.IsSupported)
                    for (Vector256<double> vm1 = Vector256.Create(m1); j < top; j += 4)
                        Avx.Store(pbi + j, Avx.Multiply(Avx.LoadVector256(pbi + j), vm1));
                for (; j < size; j++)
                    pbi[j] *= m1;
            }
            for (int i = size - 1, isize = i * size; i >= 0; i--, isize -= size)
            {
                double* pbi = pB + isize;
                for (int k = i + 1; k < size; k++)
                {
                    int ksize = k * size;
                    double* pbk = pB + ksize;
                    double mult = pA[ksize + i];
                    int l = 0;
                    if (Avx.IsSupported)
                        for (Vector256<double> vm = Vector256.Create(mult); l < top; l += 4)
                            Avx.Store(pbi + l,
                                Avx.LoadVector256(pbi + l).MultiplyAddNeg(pbk + l, vm));
                    for (; l < size; l++)
                        pbi[l] -= pbk[l] * mult;
                }
                double m1 = 1.0 / pA[isize + i];
                int j = 0;
                if (Avx.IsSupported)
                    for (Vector256<double> vm1 = Vector256.Create(m1); j < top; j += 4)
                        Avx.Store( pbi + j, Avx.Multiply(Avx.LoadVector256(pbi + j), vm1));
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
}
