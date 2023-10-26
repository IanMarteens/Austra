﻿namespace Austra.Library;

using Austra.Library.Stats;

/// <summary>Represents an upper triangular matrix.</summary>
/// <remarks>
/// Triangular matrices may have a different number of rows and columns.
/// </remarks>
public readonly struct RMatrix :
    IFormattable,
    IEquatable<RMatrix>,
    IEqualityOperators<RMatrix, RMatrix, bool>,
    IEqualityOperators<RMatrix, LMatrix, bool>,
    IEqualityOperators<RMatrix, Matrix, bool>,
    IAdditionOperators<RMatrix, RMatrix, RMatrix>,
    IAdditionOperators<RMatrix, double, RMatrix>,
    ISubtractionOperators<RMatrix, RMatrix, RMatrix>,
    ISubtractionOperators<RMatrix, double, RMatrix>,
    IMultiplyOperators<RMatrix, Vector, Vector>,
    IMultiplyOperators<RMatrix, double, RMatrix>,
    IDivisionOperators<RMatrix, double, RMatrix>,
    IUnaryNegationOperators<RMatrix, RMatrix>,
    IMatrix
{
    /// <summary>Stores the cells of the matrix.</summary>
    private readonly double[] values;

    /// <summary>Creates an empty square matrix.</summary>
    /// <param name="size">Number of rows and columns.</param>
    public RMatrix(int size) =>
        (Rows, Cols, values) = (size, size, new double[size * size]);

    /// <summary>Creates an empty rectangular matrix.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    public RMatrix(int rows, int cols) =>
        (Rows, Cols, values) = (rows, cols, new double[rows * cols]);

    /// <summary>
    /// Creates a matrix with a given number of rows and columns, and its internal array.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of columns.</param>
    /// <param name="values">Internal storage.</param>
    public RMatrix(int rows, int columns, double[] values) =>
        (Rows, Cols, this.values) = (rows, columns, values);

    /// <summary>Creates a diagonal matrix given its diagonal.</summary>
    /// <param name="diagonal">Values in the diagonal.</param>
    public RMatrix(Vector diagonal) =>
        (Rows, Cols, values) = (diagonal.Length, diagonal.Length, CommonMatrix.CreateDiagonal(diagonal));

    /// <summary>
    /// Creates a matrix filled with a uniform distribution generator.
    /// </summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random number generator.</param>
    /// <param name="offset">An offset for the random numbers.</param>
    /// <param name="width">Width for the uniform distribution.</param>
    public RMatrix(
        int rows, int cols, Random random,
        double offset = 0.0, double width = 1.0)
    {
        (Rows, Cols, values) = (rows, cols, new double[rows * cols]);
        for (int r = 0; r < rows; r++)
            for (int c = r; c < cols; c++)
                values[r * Cols + c] = random.NextDouble() * width + offset;
    }

    /// <summary>
    /// Creates a matrix filled with a standard normal distribution.
    /// </summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random standard normal generator.</param>
    public RMatrix(int rows, int cols, NormalRandom random)
    {
        (Rows, Cols, values) = (rows, cols, new double[rows * cols]);
        for (int r = 0; r < rows; r++)
            for (int c = r; c < cols; c++)
                values[r * Cols + c] = random.NextDouble();
    }

    /// <summary>Creates an identity matrix given its size.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <returns>An identity matrix with the requested size.</returns>
    public static RMatrix Identity(int size) =>
        new(size, size, CommonMatrix.CreateIdentity(size));

    /// <summary>Creates an identical lower triangular matrix.</summary>
    /// <returns>A deep clone of the instance.</returns>
    public RMatrix Clone() => new(Rows, Cols, (double[])values.Clone());

    /// <summary>
    /// Implicit conversion from a rectangular to an upper triangular matrix.
    /// </summary>
    /// <param name="m">A rectangular matrix.</param>
    /// <returns>A new upper-triangular matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RMatrix(Matrix m) => new(m.Rows, m.Cols, (double[])m);

    /// <summary>
    /// Explicit conversion from a triangular matrix to a rectangular one.
    /// </summary>
    /// <param name="m">The original upper-triangular matrix.</param>
    /// <returns>A new rectangular matrix.</returns>
    public static explicit operator Matrix(RMatrix m) => new(m.Rows, m.Cols, m.values);

    /// <summary>Has the matrix been properly initialized?</summary>
    /// <remarks>
    /// Since <see cref="RMatrix"/> is a struct, its default constructor doesn't
    /// initializes the underlying bidimensional array.
    /// </remarks>
    public bool IsInitialized => values != null;

    /// <summary>Gets the number of rows.</summary>
    public int Rows { get; }
    /// <summary>Gets the number of columns.</summary>
    public int Cols { get; }
    /// <summary>Checks if the matrix is a square one.</summary>
    public bool IsSquare => Rows == Cols;

    /// <summary>Gets the main diagonal.</summary>
    /// <returns>A vector containing values in the main diagonal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Diagonal()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == Min(Rows, Cols));

        return CommonMatrix.Diagonal(Rows, Cols, values);
    }

    /// <summary>Calculates the trace of a matrix.</summary>
    /// <returns>The sum of the cells in the main diagonal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Trace() => CommonMatrix.Trace(Rows, Cols, values);

    /// <summary>Gets the value at a single cell.</summary>
    /// <param name="row">The row number, between 0 and Rows - 1.</param>
    /// <param name="column">The column number, between 0 and Cols - 1.</param>
    /// <returns>The value at the given cell.</returns>
    public double this[int row, int column]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => values[row * Cols + column];
    }

    /// <summary>Transposes the matrix.</summary>
    /// <returns>A new matrix with swapped rows and cells.</returns>
    public unsafe LMatrix Transpose()
    {
        Contract.Requires(IsInitialized);

        int c = Cols, r = Rows;
        double[] result = new double[c * r];
        fixed (double* pA = values, pB = result)
            for (int row = 0; row < r; row++)
                for (int col = row; col < c; col++)
                    pB[col * r + row] = pA[row * c + col];
        return new(c, r, result);
    }

    /// <summary>Sums two upper matrices with the same size.</summary>
    /// <param name="m1">First matrix operand.</param>
    /// <param name="m2">Second matrix operand.</param>
    /// <returns>The sum of the two operands.</returns>
    public static unsafe RMatrix operator +(RMatrix m1, RMatrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        Contract.Requires(m1.Rows == m2.Rows);
        Contract.Requires(m1.Cols == m2.Cols);
        Contract.Ensures(Contract.Result<RMatrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<RMatrix>().Cols == m1.Cols);

        int r = m1.Rows, c = m1.Cols;
        double[] result = new double[r * c];
        fixed (double* pA = m1.values, pB = m2.values, pC = result)
            for (int row = 0, offset = 0; row < r; row++, offset += c)
            {
                int col = row, k = offset + col;
                if (Avx.IsSupported)
                    for (int top = (c - row) & Simd.AVX_MASK + row; col < top; col += 4, k += 4)
                        Avx.Store(pC + k,
                            Avx.Add(Avx.LoadVector256(pA + k), Avx.LoadVector256(pB + k)));
                for (; col < c; col++, k++)
                    pC[k] = pA[k] + pB[k];
            }
        return new(m1.Rows, m1.Cols, result);
    }

    /// <summary>Adds a scalar value to an upper triangular matrix.</summary>
    /// <remarks>The value is just added to the upper triangular part.</remarks>
    /// <param name="m">The matrix summand.</param>
    /// <param name="d">The scalar summand.</param>
    /// <returns>The sum of the matrix by the scalar.</returns>
    public static unsafe RMatrix operator +(RMatrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[r * c];
        fixed (double* pA = m.values, pC = result)
        {
            Vector256<double> vec = Vector256.Create(d);
            for (int row = 0, offset = 0; row < r; row++, offset += c)
            {
                int col = row, k = offset + col;
                if (Avx.IsSupported)
                    for (int top = (c - row) & Simd.AVX_MASK + row; col < top; col += 4, k += 4)
                        Avx.Store(pC + k, Avx.Add(Avx.LoadVector256(pA + k), vec));
                for (; col < c; col++, k++)
                    pC[k] = pA[k] + d;
            }
        }
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Adds a scalar value to an upper triangular matrix.</summary>
    /// <param name="d">The scalar summand.</param>
    /// <param name="m">The matrix summand.</param>
    /// <returns>The pointwise sum of the matrix and the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RMatrix operator +(double d, RMatrix m) => m + d;

    /// <summary>Subtracts a scalar value from an upper triangular matrix.</summary>
    /// <remarks>The value is just subtracted from the upper triangular part.</remarks>
    /// <param name="m">The matrix minuend.</param>
    /// <param name="d">The scalar subtrahend.</param>
    /// <returns>The substraction of the scalar from the matrix.</returns>
    public static unsafe RMatrix operator -(RMatrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[r * c];
        fixed (double* pA = m.values, pC = result)
        {
            Vector256<double> vec = Vector256.Create(d);
            for (int row = 0, offset = 0; row < r; row++, offset += c)
            {
                int col = row, k = offset + col;
                if (Avx.IsSupported)
                    for (int top = (c - row) & Simd.AVX_MASK + row; col < top; col += 4, k += 4)
                        Avx.Store(pC + k, Avx.Subtract(Avx.LoadVector256(pA + k), vec));
                for (; col < c; col++, k++)
                    pC[k] = pA[k] - d;
            }
        }
        return new(r, c, result);
    }

    /// <summary>Adds an upper triangular matrix and a lower triangular one.</summary>
    /// <param name="m1">The upper-triangular summand.</param>
    /// <param name="m2">The lower-triangular summand.</param>
    /// <returns>The sum of these two matrices.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator +(RMatrix m1, LMatrix m2) =>
        new Matrix(m1.Rows, m1.Cols, m1.values) + m2;

    /// <summary>Subtracts two upper matrices with the same size.</summary>
    /// <param name="m1">First matrix operand.</param>
    /// <param name="m2">Second matrix operand.</param>
    /// <returns>The subtraction of the two operands.</returns>
    public static unsafe RMatrix operator -(RMatrix m1, RMatrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        Contract.Requires(m1.Rows == m2.Rows);
        Contract.Requires(m1.Cols == m2.Cols);
        Contract.Ensures(Contract.Result<RMatrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<RMatrix>().Cols == m1.Cols);

        int r = m1.Rows, c = m1.Cols;
        double[] result = new double[r * c];
        fixed (double* pA = m1.values, pB = m2.values, pC = result)
            for (int row = 0, offset = 0; row < r; row++, offset += c)
            {
                int col = row, k = offset + col;
                if (Avx.IsSupported)
                    for (int top = (c - row) & Simd.AVX_MASK + row; col < top; col += 4, k += 4)
                        Avx.Store(pC + k,
                            Avx.Subtract(Avx.LoadVector256(pA + k), Avx.LoadVector256(pB + k)));
                for (; col < c; col++, k++)
                    pC[k] = pA[k] - pB[k];
            }
        return new(r, c, result);
    }

    /// <summary>Negates an upper right matrix.</summary>
    /// <param name="m">The matrix operand.</param>
    /// <returns>Cell-by-cell negation.</returns>
    public static unsafe RMatrix operator -(RMatrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<RMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<RMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[r * c];
        fixed (double* pA = m.values, pC = result)
        {
            Vector256<double> z = Vector256<double>.Zero;
            for (int row = 0, offset = 0; row < r; row++, offset += c)
            {
                int col = row, k = offset + col;
                if (Avx.IsSupported)
                    for (int top = (c - row) & Simd.AVX_MASK + row; col < top; col += 4, k += 4)
                        Avx.Store(pC + k, Avx.Subtract(z, Avx.LoadVector256(pA + k)));
                for (; col < c; col++, k++)
                    pC[k] = -pA[k];
            }
        }
        return new(r, c, result);
    }

    /// <summary>Multiplies a rectangular matrix by an upper triangular one.</summary>
    /// <param name="m1">A rectangular matrix.</param>
    /// <param name="m2">An upper triangular matrix.</param>
    /// <returns>The resulting rectangular matrix.</returns>
    public static unsafe Matrix operator *(Matrix m1, RMatrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        Contract.Requires(m1.Cols == m2.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m2.Cols);

        int m = m1.Rows, n = m1.Cols, p = m2.Cols;
        double[] result = new double[m * p];
        fixed (double* pA = (double[])m1, pB = m2.values, pC = result)
        {
            double* pAi = pA, pCi = pC;
            for (int i = 0; i < m; i++)
            {
                double* pBk = pB;
                for (int k = 0; k < n; k++)
                {
                    double d = pAi[k];
                    int j = k;
                    if (Avx.IsSupported)
                    {
                        Vector256<double> vd = Vector256.Create(d);
                        for (int top = (p - k) & Simd.AVX_MASK + k; j < top; j += 4)
                            Avx.Store(pCi + j,
                                Avx.LoadVector256(pCi + j).MultiplyAdd(pBk + j, vd));
                    }
                    for (; j < p; j++)
                        pCi[j] += pBk[j] * d;
                    pBk += p;
                }
                pAi += n;
                pCi += n;
            }
        }
        return new(m, p, result);
    }

    /// <summary>Multiplies an upper triangular matrix by a scalar value.</summary>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <param name="d">A scalar multiplicand.</param>
    /// <returns>The multiplication of the matrix by the scalar.</returns>
    public static unsafe RMatrix operator *(RMatrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[r * c];
        fixed (double* pA = m.values, pC = result)
        {
            Vector256<double> vec = Vector256.Create(d);
            for (int row = 0, offset = 0; row < r; row++, offset += c)
            {
                int col = row, k = offset + col;
                if (Avx.IsSupported)
                    for (int top = (c - row) & Simd.AVX_MASK + row; col < top; col += 4, k += 4)
                        Avx.Store(pC + k, Avx.Multiply(Avx.LoadVector256(pA + k), vec));
                for (; col < c; col++, k++)
                    pC[k] = pA[k] * d;
            }
        }
        return new(r, c, result);
    }

    /// <summary>Multiplies an upper triangular matrix by a scalar value.</summary>
    /// <param name="d">A scalar multiplicand.</param>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <returns>The multiplication of the matrix by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RMatrix operator *(double d, RMatrix m) => m * d;

    /// <summary>Divides a matrix by a scalar value.</summary>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <param name="d">A scalar multiplicand.</param>
    /// <returns>The quotient of the matrix by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RMatrix operator /(RMatrix m, double d) => m * (1.0 / d);

    /// <summary>Transform a vector using a matrix.</summary>
    /// <param name="m">The transformation matrix.</param>
    /// <param name="v">Vector to transform.</param>
    /// <returns>The transformed vector.</returns>
    public static unsafe Vector operator *(RMatrix m, Vector v)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Requires(v.IsInitialized);
        Contract.Requires(m.Cols == v.Length);
        Contract.Ensures(Contract.Result<Vector>().Length == m.Rows);

        int r = m.Rows, c = m.Cols;
        double[] b = new double[r];
        fixed (double* pA = m.values, pX = (double[])v, pB = b)
        {
            double* pA1 = pA, pB1 = pB;
            if (c >= 12 && Avx.IsSupported)
                for (int i = 0; i < r; i++)
                {
                    Vector256<double> vec = Vector256<double>.Zero;
                    int j = i;
                    for (int top = (c - i) & Simd.AVX_MASK + i; j < top; j += 4)
                        vec = vec.MultiplyAdd(pA1 + j, pX + j);
                    double d = vec.Sum();
                    for (; j < c; j++)
                        d = FusedMultiplyAdd(pA1[j], pX[j], d);
                    *pB1 = d;
                    pA1 += c;
                    pB1++;
                }
            else
                for (int i = 0; i < r; i++)
                {
                    double d = 0;
                    int j = i;
                    for (int top = (c - i) & Simd.AVX_MASK + i; j < top; j += 4)
                        d += (pA1[j] * pX[j]) + (pA1[j + 1] * pX[j + 1]) +
                            (pA1[j + 2] * pX[j + 2]) + (pA1[j + 3] * pX[j + 3]);
                    for (; j < c; j++)
                        d = FusedMultiplyAdd(pA1[j], pX[j], d);
                    *pB1 = d;
                    pA1 += c;
                    pB1++;
                }
        }
        return b;
    }

    /// <summary>Gets the determinant of the matrix.</summary>
    /// <returns>The product of the main diagonal.</returns>
    public double Determinant() => CommonMatrix.DiagonalProduct(Rows, Cols, values);

    /// <summary>Checks if the provided argument is a matrix with the same values.</summary>
    /// <param name="other">The matrix to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a matrix with the same values.</returns>
    public bool Equals(RMatrix other) => (Matrix)this == other;

    /// <summary>Checks if the provided argument is a matrix with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a matrix with the same values.</returns>
    public override bool Equals(object? obj) =>
        obj is RMatrix matrix && Equals(matrix);

    /// <summary>Returns the hashcode for this matrix.</summary>
    /// <returns>A hashcode summarizing the content of the matrix.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)values).GetHashCode(EqualityComparer<double>.Default);

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First matrix operand.</param>
    /// <param name="right">First matrix operand.</param>
    /// <returns><see langword="true"/> when all corresponding cells are equals.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RMatrix left, RMatrix right) => (Matrix)left == right;

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First matrix operand.</param>
    /// <param name="right">First matrix operand.</param>
    /// <returns><see langword="true"/> when all corresponding cells are equals.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RMatrix left, LMatrix right) => (Matrix)left == right;

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First matrix operand.</param>
    /// <param name="right">First matrix operand.</param>
    /// <returns><see langword="true"/> when all corresponding cells are equals.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RMatrix left, Matrix right) => right == left;

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First matrix operand.</param>
    /// <param name="right">First matrix operand.</param>
    /// <returns><see langword="true"/> when all corresponding cells are equals.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RMatrix left, RMatrix right) => !(left == right);

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First matrix operand.</param>
    /// <param name="right">First matrix operand.</param>
    /// <returns><see langword="true"/> when all corresponding cells are equals.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RMatrix left, LMatrix right) => !(left == right);

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First matrix operand.</param>
    /// <param name="right">First matrix operand.</param>
    /// <returns><see langword="true"/> when all corresponding cells are equals.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RMatrix left, Matrix right) => !(left == right);

    /// <summary>Gets a textual representation of this matrix.</summary>
    /// <returns>One line for each row, with space separated columns.</returns>
    public override string ToString() =>
        CommonMatrix.ToString(Rows, Cols, values, v => v.ToString("G6"), 1);

    /// <summary>Gets a textual representation of this matrix.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>One line for each row, with space separated columns.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        CommonMatrix.ToString(Rows, Cols, values, v => v.ToString(format, provider), 1);
}
