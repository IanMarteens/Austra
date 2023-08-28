namespace Austra.Library;

/// <summary>Represents a LU decomposition.</summary>
public readonly struct LU
{
    /// <summary>Storage for the L and the U parts.</summary>
    private readonly double[,] values;
    /// <summary>Storage for the permutation matrix.</summary>
    private readonly int[] perm;
    /// <summary>Effective permutations.</summary>
    private readonly int[] pivots;
    /// <summary>Lazy initialization for the L part.</summary>
    private readonly Lazy<LMatrix> lowerMatrix;
    /// <summary>Lazy initialization for the U part.</summary>
    private readonly Lazy<RMatrix> upperMatrix;

    /// <summary>Creates an empty LU decomposition from a matrix.</summary>
    /// <param name="m">The underlying matrix.</param>
    public LU(Matrix m)
    {
        var data = (double[,])m.Clone();
        values = data;
        perm = new int[m.Rows];
        for (int i = 0; i < perm.Length; i++)
            perm[i] = i;
        pivots = (int[])perm.Clone();
        lowerMatrix = new(() => CreateLowerMatrix(data), true);
        upperMatrix = new(() => CreateUpperMatrix(data), true);
        CalculateDecomposition(m.Rows);
    }

    /// <summary>Materializes the effective permutation.</summary>
    [SkipLocalsInit]
    private unsafe void CalculateDecomposition(int r)
    {
        fixed (double* data = values)
        fixed (int* pp = perm, pv = pivots)
        {
            double* buf = stackalloc double[r];
            // The outer loop.
            for (int j = 0; j < r; j++)
            {
                // Make a copy of the j-th column to localize references.
                for (int i = 0; i < r; i++)
                    buf[i] = data[i * r + j];
                double* pAi = data;
                for (int i = 0; i < r; i++)
                {
                    int top = Min(i, j);
                    int lastBlock = top & Simd.AVX_MASK;
                    double s = 0.0;
                    int k = 0;
                    if (Avx.IsSupported)
                    {
                        var ac = Vector256<double>.Zero;
                        for (; k < lastBlock; k += 4)
                            ac = ac.MultiplyAdd(pAi + k, buf + k);
                        s = ac.Sum();
                    }
                    else
                        for (; k < lastBlock; k += 4)
                            s += pAi[k] * buf[k] +
                                pAi[k + 1] * buf[k + 1] +
                                pAi[k + 2] * buf[k + 2] +
                                pAi[k + 3] * buf[k + 3];
                    for (; k < top; k++)
                        s += pAi[k] * buf[k];
                    pAi[j] = buf[i] -= s;
                    pAi += r;
                }

                // Find pivot and exchange if necessary.
                int pivot = j;
                double best = Abs(buf[pivot]);
                for (int i = j + 1; i < r; i++)
                {
                    double v = Abs(buf[i]);
                    if (v > best)
                        (pivot, best) = (i, v);
                }
                if (pivot != j)
                {
                    // Exchange rows.
                    double* pAp = data + pivot * r;
                    double* pAj = data + j * r;
                    int k = 0;
                    if (Avx.IsSupported)
                    {
                        // Unroll the loop.
                        for (int lastBlock = r & Simd.AVX512_MASK; k < lastBlock; k += 8)
                        {
                            var v1 = Avx.LoadVector256(pAp + k);
                            var v2 = Avx.LoadVector256(pAp + (k + 4));
                            var w1 = Avx.LoadVector256(pAj + k);
                            var w2 = Avx.LoadVector256(pAj + (k + 4));
                            // Try to take advantage of the cache.
                            Avx.Store(pAj + k, v1);
                            Avx.Store(pAj + (k + 4), v2);
                            Avx.Store(pAp + k, w1);
                            Avx.Store(pAp + (k + 4), w2);
                        }
                        if (k < (r & Simd.AVX_MASK))
                        {
                            var v1 = Avx.LoadVector256(pAp + k);
                            var w1 = Avx.LoadVector256(pAj + k);
                            Avx.Store(pAj + k, v1);
                            Avx.Store(pAp + k, w1);
                            k += 4;
                        }
                    }
                    for (; k < r; k++)
                        (pAj[k], pAp[k]) = (pAp[k], pAj[k]);
                    pp[j] = pivot;
                }

                // Compute multipliers.
                double cell = data[(r + 1) * j];
                if (j < r & cell != 0.0)
                {
                    double c = 1.0 / cell;
                    for (int i = j + 1; i < r; i++)
                        data[i * r + j] *= c;
                }
            }
            // Create pivots.
            for (int i = 0; i < r; i++)
            {
                int p = pp[i];
                if (p != i)
                    (pv[p], pv[i]) = (pv[i], pv[p]);
            }
        }
    }

    /// <summary>Extracts the lower triangle of a matrix.</summary>
    /// <param name="values">Packed matrix with L &amp; U parts.</param>
    /// <returns>The lower half, with ones in the diagonal.</returns>
    private static LMatrix CreateLowerMatrix(double[,] values)
    {
        int size = values.GetLength(0);
        double[,] result = new double[size, size];
        result[0, 0] = 1.0;
        for (int row = 1; row < size; row++)
        {
            for (int col = 0; col < row; col++)
                result[row, col] = values[row, col];
            result[row, row] = 1.0;
        }
        return new(result);
    }

    /// <summary>Extracts the upper triangle of a matrix.</summary>
    /// <param name="values">Packed matrix with L &amp; U parts.</param>
    /// <returns>The upper half of the matrix.</returns>
    private static RMatrix CreateUpperMatrix(double[,] values)
    {
        int size = values.GetLength(0);
        double[,] result = new double[size, size];
        for (int row = 0; row < size; row++)
            for (int col = row; col < size; col++)
                result[row, col] = values[row, col];
        return new(result);
    }

    /// <summary>Gets the dimension of the LU decomposition.</summary>
    public int Size => perm.Length;

    /// <summary>Gets the storage for the LU parts.</summary>
    /// <param name="lu">The LU decomposition.</param>
    public static explicit operator double[,](LU lu) => lu.values;

    /// <summary>Gets the storage for the permutation part.</summary>
    /// <param name="lu">The LU decomposition.</param>
    public static explicit operator int[](LU lu) => lu.perm;

    /// <summary>Gets the value at a single cell.</summary>
    /// <param name="row">The row number, between 0 and Rows - 1.</param>
    /// <param name="column">The column number, between 0 and Cols - 1.</param>
    /// <returns>The value at the given cell.</returns>
    public double this[int row, int column]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => values[row, column];
    }

    /// <summary>Gets the L part of the decomposition.</summary>
    public LMatrix L => lowerMatrix.Value;

    /// <summary>Gets the U part of the decomposition.</summary>
    public RMatrix U => upperMatrix.Value;

    /// <summary>Solves the equation Ax = b for x.</summary>
    /// <param name="v">The right side of the equation.</param>
    /// <returns>The solving vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Solve(Vector v)
    {
        Contract.Requires(v.IsInitialized);
        Contract.Requires(Size == v.Length);
        Contract.Ensures(Contract.Result<Vector>().Length == v.Length);

        Vector result = new double[Size];
        Solve(v, result);
        return result;
    }

    /// <summary>Solves the equation Ax = b for x, in place.</summary>
    /// <param name="input">The right side of the equation.</param>
    /// <param name="output">The solving vector.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Solve(Vector input, Vector output)
    {
        Contract.Requires(input.IsInitialized);
        Contract.Requires(Size == input.Length);
        Contract.Requires(output.Length == input.Length);

        int size = Size;
        fixed (double* a = values, c = (double[])output)
        {
            fixed (double* p = (double[])input)
            fixed (int* q = pivots)
            {
                int i = 0;
                if (Avx2.IsSupported)
                {
                    for (int top = size & Simd.AVX_MASK; i < top; i += 4)
                        Avx.Store(c + i, Avx2.GatherVector256(p, Sse2.LoadVector128(q + i), 8));
                }
                for (; i < size; i++)
                    c[i] = p[q[i]];
            }
            for (int k = 0; k < size; k++)
            {
                double m = c[k];
                int i = k + 1;
                if (Avx2.IsSupported)
                {
                    var vm = Vector256.Create(m);
                    var vx = Vector128.Create(0, size, 2 * size, 3 * size);
                    for (double* p = a + i * size + k; i < size - 4; i += 4, p += 4 * size)
                        Avx.Store(c + i, Avx.Subtract(Avx.LoadVector256(c + i),
                            Avx.Multiply(Avx2.GatherVector256(p, vx, 8), vm)));
                }
                for (; i < size; i++)
                    c[i] -= m * a[i * size + k];
            }
            for (int k = size - 1; k >= 0; k--)
            {
                double m = c[k] /= a[k * size + k];
                int i = 0;
                if (Avx2.IsSupported)
                {
                    var vm = Vector256.Create(m);
                    var vx = Vector128.Create(0, size, 2 * size, 3 * size);
                    for (double* p = a + k; i < k - 4; i += 4, p += size * 4)
                        Avx.Store(c + i, Avx.Subtract(Avx.LoadVector256(c + i),
                            Avx.Multiply(Avx2.GatherVector256(p, vx, 8), vm)));
                }
                for (; i < k; i++)
                    c[i] -= m * a[i * size + k];
            }
        }
    }

    /// <summary>Solves the equation AX = B for the matrix X.</summary>
    /// <param name="m">The right side of the equation.</param>
    /// <returns>The solving matrix.</returns>
    public Matrix Solve(Matrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Requires(m.IsSquare);
        Contract.Requires(Size == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);

        Matrix result = new(Size);
        Solve(m, result);
        return result;
    }

    /// <summary>Solves the equation AX = B for the matrix X, in place.</summary>
    /// <param name="input">The right side of the equation.</param>
    /// <param name="output">The solving matrix.</param>
    public unsafe void Solve(Matrix input, Matrix output)
    {
        Contract.Requires(input.IsInitialized);
        Contract.Requires(input.IsSquare);
        Contract.Requires(Size == input.Rows);
        Contract.Requires(output.Rows == input.Rows);
        Contract.Ensures(output.Cols == input.Cols);

        int size = Size;
        fixed (double* pA = values, pC = (double[,])output)
        {
            int top = size & Simd.AVX_MASK;
            // Apply permutations to each column of the input.
            fixed (double* pB = (double[,])input)
            fixed (int* pP = pivots)
                for (int i = 0; i < size; i++)
                    Buffer.MemoryCopy(
                        pB + pP[i] * size,
                        pC + i * size,
                        size * sizeof(double), size * sizeof(double));
            for (int k = 0; k < size; k++)
            {
                double* pck = pC + k * size;
                for (int i = k + 1; i < size; i++)
                {
                    double* pci = pC + i * size;
                    double mult = pA[i * size + k];
                    int j = 0;
                    if (Avx.IsSupported)
                        for (var vm = Vector256.Create(mult); j < top; j += 4)
                            Avx.Store(pci + j,
                                Avx.LoadVector256(pci + j).MultiplyAddNeg(pck + j, vm));
                    for (; j < size; j++)
                        pci[j] -= pck[j] * mult;
                }
            }
            for (int k = size - 1; k >= 0; k--)
            {
                double* pck = pC + k * size;
                double mult = pA[k * size + k];
                int l = 0;
                if (Avx.IsSupported)
                    for (var vm = Vector256.Create(1.0 / mult); l < top; l += 4)
                        Avx.Store(pck + l, Avx.Multiply(Avx.LoadVector256(pck + l), vm));
                for (; l < size; l++)
                    pck[l] /= mult;
                double* pai = pA + k;
                double* pci = pC;
                for (int i = 0; i < k; i++)
                {
                    mult = *pai;
                    int j = 0;
                    if (Avx.IsSupported)
                        for (var vm = Vector256.Create(mult); j < top; j += 4)
                            Avx.Store(pci + j,
                                Avx.LoadVector256(pci + j).MultiplyAddNeg(pck + j, vm));
                    for (; j < size; j++)
                        pci[j] -= pck[j] * mult;
                    pai += size;
                    pci += size;
                }
            }
        }
    }

    /// <summary>Gets the determinant of the underlying matrix.</summary>
    /// <returns>The signed product of the main diagonal.</returns>
    public unsafe double Determinant()
    {
        fixed (double* pa = values)
        fixed (int* pp = perm)
        {
            double* p = pa;
            int size = Size;
            double det = 1;
            for (int i = 0, r = size + 1; i < size; i++, p += r)
                det *= pp[i] != i ? -*p : *p;
            return det;
        }
    }

    /// <summary>Gets a textual representation of this decomposition.</summary>
    /// <returns>One line for each row, with space separated columns.</returns>
    public override string ToString() =>
        CommonMatrix.ToString(values, v => v.ToString("G6"));

    /// <summary>Gets a textual representation of this matrix.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>One line for each row, with space separated columns.</returns>
    public string ToString(string format, IFormatProvider? provider = null) =>
        CommonMatrix.ToString(values, v => v.ToString(format, provider));
}
