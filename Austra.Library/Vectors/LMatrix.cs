using System.ComponentModel.Design;

namespace Austra.Library;

/// <summary>Represents a lower triangular matrix.</summary>
/// <remarks>
/// <para>Having a separate type for lower-triangular matrices is not a matter of storage,
/// but of semantics. For instance, the Cholesky factorization always returns either
/// a lower or upper triangular matrix, so it's important for the API to make clear
/// which type of matrix is returning.</para>
/// <para>Lower-triangular matrices may have a different number of rows and columns.</para>
/// </remarks>
[JsonConverter(typeof(LMatrixJsonConverter))]
public readonly struct LMatrix :
    IFormattable,
    IEquatable<LMatrix>,
    IEqualityOperators<LMatrix, LMatrix, bool>,
    IEqualityOperators<LMatrix, RMatrix, bool>,
    IEqualityOperators<LMatrix, Matrix, bool>,
    IAdditionOperators<LMatrix, LMatrix, LMatrix>,
    IAdditionOperators<LMatrix, RMatrix, Matrix>,
    IAdditionOperators<LMatrix, double, LMatrix>,
    ISubtractionOperators<LMatrix, LMatrix, LMatrix>,
    ISubtractionOperators<LMatrix, RMatrix, Matrix>,
    ISubtractionOperators<LMatrix, double, LMatrix>,
    IMultiplyOperators<LMatrix, Matrix, Matrix>,
    IMultiplyOperators<LMatrix, DVector, DVector>,
    IMultiplyOperators<LMatrix, double, LMatrix>,
    IDivisionOperators<LMatrix, double, LMatrix>,
    IUnaryNegationOperators<LMatrix, LMatrix>,
    IMatrix
{
    /// <summary>Stores the cells of the matrix.</summary>
    private readonly double[] values;

    /// <summary>Creates an empty square matrix.</summary>
    /// <param name="size">Number of rows and columns.</param>
    public LMatrix(int size) => (Rows, Cols, values) = (size, size, new double[size * size]);

    /// <summary>Creates an empty rectangular matrix.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    public LMatrix(int rows, int cols) => (Rows, Cols, values) = (rows, cols, new double[rows * cols]);

    /// <summary>
    /// Creates a matrix with a given number of rows and columns, and its internal array.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of columns.</param>
    /// <param name="values">Internal storage.</param>
    public LMatrix(int rows, int columns, double[] values) =>
        (Rows, Cols, this.values) = (rows, columns, values);

    /// <summary>Creates a diagonal matrix given its diagonal.</summary>
    /// <param name="diagonal">Values in the diagonal.</param>
    public LMatrix(DVector diagonal) =>
        (Rows, Cols, values) = (diagonal.Length, diagonal.Length, diagonal.CreateDiagonal());

    /// <summary>Creates a matrix filled with a uniform distribution generator.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random number generator.</param>
    /// <param name="offset">An offset for the random numbers.</param>
    /// <param name="width">Width for the uniform distribution.</param>
    public LMatrix(
        int rows, int cols, Random random,
        double offset = 0.0, double width = 1.0)
    {
        (Rows, Cols, values) = (rows, cols, new double[rows * cols]);
        ref double cell = ref MM.GetArrayDataReference(values);
        for (int r = 0; r < rows; r++, cell = ref Add(ref cell, cols))
            for (int c = 0, top = Min(cols, r + 1); c < top; c++)
                Add(ref cell, c) = FusedMultiplyAdd(random.NextDouble(), width, offset);
    }

    /// <summary>
    /// Creates a matrix filled with a uniform distribution generator.
    /// </summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random number generator.</param>
    public LMatrix(int rows, int cols, Random random)
    {
        (Rows, Cols, values) = (rows, cols, new double[rows * cols]);
        ref double cell = ref MM.GetArrayDataReference(values);
        for (int r = 0; r < rows; r++, cell = ref Add(ref cell, cols))
            for (int c = 0, top = Min(cols, r + 1); c < top; c++)
                Add(ref cell, c) = random.NextDouble();
    }

    /// <summary>
    /// Creates a square lower matrix filled with a uniform distribution generator.
    /// </summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <param name="random">A random number generator.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LMatrix(int size, Random random) : this(size, size, random) { }

    /// <summary>
    /// Creates a matrix filled with a standard normal distribution.
    /// </summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random standard normal generator.</param>
    public LMatrix(int rows, int cols, NormalRandom random)
    {
        (Rows, Cols, values) = (rows, cols, new double[rows * cols]);
        ref double cell = ref MM.GetArrayDataReference(values);
        // First row is special!
        cell = random.NextDouble();
        for (int r = 1; r < rows; r++)
        {
            cell = ref Add(ref cell, cols);
            int c = 0, top = Min(cols, r + 1);
            for (int t = top & ~1; c < t; c += 2)
                random.NextDoubles(ref Add(ref cell, c));
            if (c < top)
                Add(ref cell, c) = random.NextDouble();
        }
    }

    /// <summary>Creates a squared matrix with a standard normal distribution.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <param name="random">A random standard normal generator.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LMatrix(int size, NormalRandom random) : this(size, size, random) { }

    /// <summary>Creates an identity matrix given its size.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <returns>An identity matrix with the requested size.</returns>
    public static LMatrix Identity(int size) =>
        new(size, size, CommonMatrix.CreateIdentity(size));

    /// <summary>Creates an identical lower triangular matrix.</summary>
    /// <returns>A deep clone of the instance.</returns>
    public LMatrix Clone() => new(Rows, Cols, (double[])values.Clone());

    /// <summary>
    /// Implicit conversion from a rectangular to a lower triangular matrix.
    /// </summary>
    /// <param name="m">A rectangular matrix.</param>
    /// <returns>A new lower-triangular matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LMatrix(Matrix m) => new(m.Rows, m.Cols, (double[])m);

    /// <summary>
    /// Explicit conversion from a matrix to a onedimensional array.
    /// </summary>
    /// <remarks>
    /// Use carefully: it returns the underlying onedimensional array.
    /// </remarks>
    /// <param name="m">The original matrix.</param>
    /// <returns>The underlying onedimensional array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator double[](LMatrix m) => m.values;

    /// <summary>
    /// Explicit conversion from a triangular matrix to a rectangular one.
    /// </summary>
    /// <param name="m">A lower-triangular matrix.</param>
    /// <returns>A new rectangular matrix.</returns>
    public static explicit operator Matrix(LMatrix m) => new(m.Rows, m.Cols, m.values);

    /// <summary>Has the matrix been properly initialized?</summary>
    /// <remarks>
    /// Since <see cref="LMatrix"/> is a struct, its default constructor doesn't
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
    public DVector Diagonal()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<DVector>().Length == Min(Rows, Cols));
        return values.Diagonal(Rows, Cols);
    }

    /// <summary>Calculates the trace of a matrix.</summary>
    /// <returns>The sum of the cells in the main diagonal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Trace() => values.Trace(Rows, Cols);

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
    public RMatrix Transpose()
    {
        Contract.Requires(IsInitialized);

        nuint c = (nuint)Cols, r = (nuint)Rows;
        double[] result = new double[values.Length];
        ref double pA = ref MM.GetArrayDataReference(values);
        ref double pB = ref MM.GetArrayDataReference(result);
        if (Avx.IsSupported && r == c && (r & 0b11) == 0)
        {
            // Columns and rows are equal and multiples of four.
            nuint r2 = r + r;
            for (nuint row = 0; row < r; row += 4)
            {
                nuint top = Min(row + 1, c);
                for (nuint col = 0; col < top; col += 4)
                {
                    ref double pp = ref Add(ref pA, row * r + col);
                    var row1 = V4.LoadUnsafe(ref pp);
                    var row2 = V4.LoadUnsafe(ref pp, r);
                    var row3 = V4.LoadUnsafe(ref pp, r2);
                    var row4 = V4.LoadUnsafe(ref pp, r2 + r);
                    var t1 = Avx.Shuffle(row1, row2, 0b0000);
                    var t2 = Avx.Shuffle(row1, row2, 0b1111);
                    var t3 = Avx.Shuffle(row3, row4, 0b0000);
                    var t4 = Avx.Shuffle(row3, row4, 0b1111);
                    row1 = Avx.Permute2x128(t1, t3, 0b00100000);
                    row2 = Avx.Permute2x128(t2, t4, 0b00100000);
                    row3 = Avx.Permute2x128(t1, t3, 0b00110001);
                    row4 = Avx.Permute2x128(t2, t4, 0b00110001);
                    ref double qq = ref Add(ref pB, col * r + row);
                    V4.StoreUnsafe(row1, ref qq);
                    V4.StoreUnsafe(row2, ref qq, r);
                    V4.StoreUnsafe(row3, ref qq, r2);
                    V4.StoreUnsafe(row4, ref qq, r2 + r);
                }
            }
        }
        else
            for (nuint row = 0; row < r; row++)
                for (nuint col = 0, top = Min(row + 1, c); col < top; col++)
                    Add(ref pB, col * r + row) = Add(ref pA, row * c + col);
        return new(Cols, Rows, result);
    }

    /// <summary>Sums two lower matrices with the same size.</summary>
    /// <param name="m1">First matrix operand.</param>
    /// <param name="m2">Second matrix operand.</param>
    /// <returns>The sum of the two operands.</returns>
    public static LMatrix operator +(LMatrix m1, LMatrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        Contract.Requires(m1.Rows == m2.Rows);
        Contract.Requires(m1.Cols == m2.Cols);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m1.Cols);

        int r = m1.Rows, c = m1.Cols;
        double[] result = new double[r * c];
        if (c < r)
            r = c;
        result[0] = m1.values[0] + m2.values[0];    // First row is special.
        for (int row = 1, offset = c; row < r; row++, offset += c)
            m1.values.AsSpan(offset, row + 1).Add(
                m2.values.AsSpan(offset, row + 1), result.AsSpan(offset, row + 1));
        if (m1.Rows > c)
        {
            int c2 = c * c;
            m1.values.AsSpan(c2).Add(m2.values.AsSpan(c2), result.AsSpan(c2));
        }
        return new(m1.Rows, m1.Cols, result);
    }

    /// <summary>Adds a lower-triangular matrix and an upper-triangular one.</summary>
    /// <param name="m1">First matrix operand.</param>
    /// <param name="m2">Second matrix operand.</param>
    /// <returns>The sum of the two operands.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator +(LMatrix m1, RMatrix m2) => new Matrix(m1.values) + m2;

    /// <summary>Adds a scalar value to a lower triangular matrix.</summary>
    /// <remarks>The value is just added to the lower triangular part.</remarks>
    /// <param name="m">The matrix summand.</param>
    /// <param name="d">The scalar summand.</param>
    /// <returns>The sum of the matrix and the scalar.</returns>
    public static LMatrix operator +(LMatrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[r * c];
        if (c < r)
            r = c;
        result[0] = m.values[0] + d;    // First row is special.
        for (int row = 1, offset = c; row < r; row++, offset += c)
            m.values.AsSpan(offset, row + 1).Add(d, result.AsSpan(offset, row + 1));
        if (m.Rows > c)
        {
            int c2 = c * c;
            m.values.AsSpan(c2).Add(d, result.AsSpan(c2));
        }
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Adds a scalar value to a lower triangular matrix.</summary>
    /// <remarks>The value is just added to the lower triangular part.</remarks>
    /// <param name="d">The scalar summand.</param>
    /// <param name="m">The matrix summand.</param>
    /// <returns>The sum of the matrix and the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LMatrix operator +(double d, LMatrix m) => m + d;

    /// <summary>Subtracts two lower matrices with the same size.</summary>
    /// <param name="m1">Matrix minuend.</param>
    /// <param name="m2">Matrix subtrahend.</param>
    /// <returns>The subtraction of the two operands.</returns>
    public static LMatrix operator -(LMatrix m1, LMatrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        Contract.Requires(m1.Rows == m2.Rows);
        Contract.Requires(m1.Cols == m2.Cols);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m1.Cols);

        int r = m1.Rows, c = m1.Cols;
        double[] result = new double[m1.values.Length];
        if (c < r)
            r = c;
        result[0] = m1.values[0] - m2.values[0];    // First row is special.
        for (int row = 1, offset = c; row < r; row++, offset += c)
            m1.values.AsSpan(offset, row + 1).Sub(
                m2.values.AsSpan(offset, row + 1), result.AsSpan(offset, row + 1));
        if (m1.Rows > c)
        {
            int c2 = c * c;
            m1.values.AsSpan(c2).Sub(m2.values.AsSpan(c2), result.AsSpan(c2));
        }
        return new(m1.Rows, m1.Cols, result);
    }

    /// <summary>Subtracts an upper triangular matrix from a lower triangular one.</summary>
    /// <param name="m1">Matrix minuend.</param>
    /// <param name="m2">Matrix subtrahend.</param>
    /// <returns>The subtraction of the two operands, as a rectangular matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator -(LMatrix m1, RMatrix m2) => new Matrix(m1.values) - m2;

    /// <summary>Subtracts a scalar value from a lower triangular matrix.</summary>
    /// <remarks>The value is just subtracted from the lower triangular part.</remarks>
    /// <param name="m">Matrix minuend.</param>
    /// <param name="d">A scalar subtrahend.</param>
    /// <returns>The subtraction of the matrix and the scalar.</returns>
    public static LMatrix operator -(LMatrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[m.values.Length];
        if (c < r)
            r = c;
        result[0] = m.values[0] - d;    // First row is special.
        for (int row = 1, offset = c; row < r; row++, offset += c)
            m.values.AsSpan(offset, row + 1).Sub(d, result.AsSpan(offset, row + 1));
        if (m.Rows > c)
        {
            int c2 = c * c;
            m.values.AsSpan(c2).Sub(d, result.AsSpan(c2));
        }
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Subtracts a lower triangular matrix from a scalar value.</summary>
    /// <remarks>The value is just subtracted from the lower triangular part.</remarks>
    /// <param name="d">A scalar minuend.</param>
    /// <param name="m">Matrix subtrahend.</param>
    /// <returns>The subtraction of the matrix and the scalar.</returns>
    public static LMatrix operator -(double d, LMatrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[m.values.Length];
        if (c < r)
            r = c;
        result[0] = d - m.values[0];    // First row is special.
        for (int row = 1, offset = c; row < r; row++, offset += c)
            CommonMatrix.Sub(d, m.values.AsSpan(offset, row + 1), result.AsSpan(offset, row + 1));
        if (m.Rows > c)
        {
            int c2 = c * c;
            CommonMatrix.Sub(d, m.values.AsSpan(c2), result.AsSpan(c2));
        }
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Negates a lower matrix.</summary>
    /// <param name="m">The matrix operand.</param>
    /// <returns>Cell-by-cell negation.</returns>
    public static LMatrix operator -(LMatrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[m.values.Length];
        if (c < r)
            r = c;
        result[0] = -m.values[0];    // First row is special.
        for (int row = 1, offset = c; row < r; row++, offset += c)
            m.values.AsSpan(offset, row + 1).Neg(result.AsSpan(offset, row + 1));
        if (m.Rows > c)
        {
            int c2 = c * c;
            m.values.AsSpan(c2).Neg(result.AsSpan(c2));
        }
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Multiplies a lower triangular matrix by a scalar value.</summary>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <param name="d">A scalar multiplicand.</param>
    /// <returns>The multiplication of the matrix by the scalar.</returns>
    public static LMatrix operator *(LMatrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[m.values.Length];
        if (c < r)
            r = c;
        result[0] = m.values[0] * d;    // First row is special.
        for (int row = 1, offset = c; row < r; row++, offset += c)
            m.values.AsSpan(offset, row + 1).Mul(d, result.AsSpan(offset, row + 1));
        if (m.Rows > c)
        {
            int c2 = c * c;
            m.values.AsSpan(c2).Mul(d, result.AsSpan(c2));
        }
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Multiplies a lower triangular matrix by a scalar value.</summary>
    /// <param name="d">A scalar multiplicand.</param>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <returns>The multiplication of the matrix by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LMatrix operator *(double d, LMatrix m) => m * d;

    /// <summary>Divides a matrix by a scalar value.</summary>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <param name="d">A scalar multiplicand.</param>
    /// <returns>The quotient of the matrix by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LMatrix operator /(LMatrix m, double d) => m * (1.0 / d);

    /// <summary>Multiplies a lower triangular matrix by a rectangular one.</summary>
    /// <param name="m1">A lower triangular matrix.</param>
    /// <param name="m2">A rectangular matrix.</param>
    /// <returns>The resulting rectangular matrix.</returns>
    public static unsafe Matrix operator *(LMatrix m1, Matrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        Contract.Requires(m1.Cols == m2.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m2.Cols);

        int m = m1.Rows, n = m1.Cols, p = m2.Cols;
        double[] result = new double[m * p];
        int last = p & Simd.MASK4;
        fixed (double* pA = m1.values, pB = (double[])m2, pC = result)
        {
            double* pAi = pA, pCi = pC;
            for (int i = 0; i < m; i++, pAi += n, pCi += n)
            {
                double* pBk = pB;
                Span<double> target = new(pCi, p);
                for (int k = 0, top = Min(i + 1, n); k < top; k++, pBk += p)
                    new Span<double>(pBk, p).MulAddStore(pAi[k], target);
            }
        }
        return new(m, p, result);
    }

    /// <summary>Multiplies a rectangular matrix by a lower triangular one.</summary>
    /// <param name="m1">A rectangular matrix.</param>
    /// <param name="m2">A lower triangular matrix.</param>
    /// <returns>The resulting rectangular matrix.</returns>
    public static unsafe Matrix operator *(Matrix m1, LMatrix m2)
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
            for (int i = 0; i < m; i++, pAi += n, pCi += n)
            {
                double* pBk = pB;
                *pCi += *pBk * *pAi;
                for (int k = 1; k < n; k++)
                {
                    pBk += p;
                    int top = Min(k + 1, n);
                    new Span<double>(pBk, top).MulAddStore(pAi[k], new Span<double>(pCi, top));
                }
            }
        }
        return new(m, p, result);
    }

    /// <summary>Multiplies a lower-triangular matrix with an upper-triangular one.</summary>
    /// <param name="m1">First matrix.</param>
    /// <param name="m2">Second matrix.</param>
    /// <returns>The product of the two operands.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator *(LMatrix m1, RMatrix m2) => m1 * (Matrix)m2;

    /// <summary>Multiplies an upper-triangular matrix with a lower-triangular one.</summary>
    /// <param name="m1">First matrix.</param>
    /// <param name="m2">Second matrix.</param>
    /// <returns>The product of the two operands.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator *(RMatrix m1, LMatrix m2) => (Matrix)m1 * m2;

    /// <summary>Gets the cell with the maximum absolute value.</summary>
    /// <remarks>
    /// The absolute maximum must always be zero or positive so, it is not a
    /// problem to scan the whole matrix, including the upper triangular part,
    /// which is filled with zeros.
    /// </remarks>
    /// <returns>The max-norm of the matrix.</returns>
    public double AMax() => values.AsSpan().AbsoluteMaximum();

    /// <summary>Gets the cell with the minimum absolute value.</summary>
    /// <returns>The minimum absolute value in the triangular matrix.</returns>
    public double AMin()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<double>() >= 0);

        int r = Rows, c = Cols;
        if (c < r)
            r = c;
        double min = Abs(values[0]);
        for (int row = 1, offset = c; row < r; row++, offset += c)
            min = Min(min, values.AsSpan(offset, row).AbsoluteMinimum());
        if (Rows > c)
            min = Min(min, values.AsSpan(c * c).AbsoluteMinimum());
        return min;
    }

    /// <summary>Gets the cell with the maximum value.</summary>
    /// <returns>The maximum value in the triangular matrix.</returns>
    public double Maximum() => new Matrix(Rows, Cols, values).Maximum();

    /// <summary>Gets the cell with the minimum value.</summary>
    /// <returns>The minimum value in the triangular matrix.</returns>
    public double Minimum() => new Matrix(Rows, Cols, values).Minimum();

    /// <summary>Multiplies this matrix by the transposed argument.</summary>
    /// <param name="m">Second operand.</param>
    /// <returns>The multiplication by the transposed argument.</returns>
    public Matrix MultiplyTranspose(LMatrix m)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(m.IsInitialized);
        if (Cols != m.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Rows);

        int r = Rows, c = m.Rows;
        double[] result = GC.AllocateUninitializedArray<double>(r * c);
        ref double pA = ref MM.GetArrayDataReference(values);
        ref double pB = ref MM.GetArrayDataReference(m.values);
        ref double pC = ref MM.GetArrayDataReference(result), r0 = ref pC;
        // The first row is special.
        double d = pA;
        for (int j = 0, jc = 0; j < c; j++, jc += m.Cols)
            Add(ref pC, j) = d * Add(ref pB, jc);
        // Iterate all the other rows of the result.
        d = pB;
        if (values == m.values)
            for (int i = 1; i < r; i++)
            {
                pA = ref Add(ref pA, Cols);
                pC = ref Add(ref pC, c);
                // The first column is also special.
                pC = pA * d;
                ref double pBj = ref Add(ref pB, m.Cols * (i - 1));
                // Iterate all other columns of the result.
                for (int j = 1; j < i; j++)
                    Add(ref pC, j) = Add(ref r0, j * c + i);
                for (int j = i; j < c; j++)
                {
                    pBj = ref Add(ref pBj, m.Cols);
                    int s = Min(m.Cols, i + 1);
                    Add(ref pC, j) = MM.CreateSpan(ref pA, s).Dot(MM.CreateSpan(ref pBj, s));
                }
            }
        else
            for (int i = 1; i < r; i++)
            {
                pA = ref Add(ref pA, Cols);
                pC = ref Add(ref pC, c);
                // The first column is also special.
                pC = pA * d;
                ref double pBj = ref pB;
                // Iterate all other columns of the result.
                for (int j = 1; j < c; j++)
                {
                    pBj = ref Add(ref pBj, m.Cols);
                    int s = Min(m.Cols, Min(i, j) + 1);
                    Add(ref pC, j) = MM.CreateSpan(ref pA, s).Dot(MM.CreateSpan(ref pBj, s));
                }
            }
        return new(r, c, result);
    }

    /// <summary>Multiplies this matrix by its own transposed.</summary>
    /// <returns>The multiplication by the transposed argument.</returns>
    public Matrix Square() => MultiplyTranspose(this);

    /// <summary>Transform a vector using a matrix.</summary>
    /// <param name="m">The transformation matrix.</param>
    /// <param name="v">Vector to transform.</param>
    /// <returns>The transformed vector.</returns>
    public static DVector operator *(LMatrix m, DVector v)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Requires(v.IsInitialized);
        Contract.Requires(m.Cols == v.Length);

        int r = m.Rows, c = m.Cols;
        double[] result = GC.AllocateUninitializedArray<double>(r);
        double[] vector = (double[])v;
        ref double pB = ref MM.GetArrayDataReference(result);
        // First row is special.
        pB = m.values[0] * vector[0];
        for (int i = 1, offset = c; i < r; i++, offset += c)
            Add(ref pB, i) = m.values.AsSpan(offset, Min(i + 1, c)).Dot(vector.AsSpan());
        return result;
    }

    /// <summary>Transforms a vector and adds an offset.</summary>
    /// <remarks>
    /// This overload is used by the <see cref="MultivariateNormalRandom"/> class.
    /// </remarks>
    /// <param name="v">Vector to transform.</param>
    /// <param name="add">Vector to add.</param>
    /// <param name="result">Preallocated buffer for the result.</param>
    /// <returns><c>this * multiplicand + add</c>.</returns>
    public DVector MultiplyAdd(DVector v, DVector add, double[] result)
    {
        int r = Rows, c = Cols;
        double[] vector = (double[])v;
        ref double pB = ref MM.GetArrayDataReference(result);
        ref double pC = ref MM.GetArrayDataReference((double[])add);
        // First row is special.
        pB = values[0] * vector[0] + pC;
        for (int i = 1, offset = c; i < r; i++, offset += c)
            Add(ref pB, i) = values.AsSpan(offset, Min(i + 1, c))
                .Dot(vector.AsSpan()) + Add(ref pC, i);
        return result;
    }

    /// <summary>Transforms a vector and adds an offset.</summary>
    /// <param name="v">Vector to transform.</param>
    /// <param name="add">Vector to add.</param>
    /// <returns><c>this * multiplicand + add</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector MultiplyAdd(DVector v, DVector add) =>
        MultiplyAdd(v, add, GC.AllocateUninitializedArray<double>(Rows));

    /// <summary>Transforms a vector and subtracts an offset.</summary>
    /// <param name="v">Vector to transform.</param>
    /// <param name="sub">Vector to subtract.</param>
    /// <param name="result">Preallocated buffer for the result.</param>
    /// <returns><c>this * multiplicand - sub</c>.</returns>
    public DVector MultiplySubtract(DVector v, DVector sub, double[] result)
    {
        int r = Rows, c = Cols;
        double[] vector = (double[])v;
        ref double pB = ref MM.GetArrayDataReference(result);
        ref double pC = ref MM.GetArrayDataReference((double[])sub);
        // First row is special.
        pB = values[0] * vector[0] - pC;
        for (int i = 1, offset = c; i < r; i++, offset += c)
            Add(ref pB, i) = values.AsSpan(offset, Min(i + 1, c))
                .Dot(vector.AsSpan()) - Add(ref pC, i);
        return result;
    }

    /// <summary>Transforms a vector and subtracts an offset.</summary>
    /// <param name="v">Vector to transform.</param>
    /// <param name="sub">Vector to subtract.</param>
    /// <returns><c>this * multiplicand - sub</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector MultiplySubtract(DVector v, DVector sub) =>
        MultiplySubtract(v, sub, GC.AllocateUninitializedArray<double>(Rows));

    /// <summary>Solves the equation Ax = b for x.</summary>
    /// <param name="v">The right side of the equation.</param>
    /// <returns>The solving vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector Solve(DVector v)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(IsSquare);
        Contract.Requires(v.Length == Rows);
        Contract.Ensures(Contract.Result<DVector>().Length == v.Length);

        DVector result = GC.AllocateUninitializedArray<double>(v.Length);
        Solve(v, result);
        return result;
    }

    /// <summary>Solves the equation Ax = b for x.</summary>
    /// <param name="input">The right side of the equation.</param>
    /// <param name="output">The solving vector.</param>
    public void Solve(DVector input, DVector output)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(IsSquare);
        Contract.Requires(input.Length == Rows);
        Contract.Requires(output.Length == Rows);

        int size = input.Length;
        ref double pA = ref MM.GetArrayDataReference(values);
        ref double pV = ref MM.GetArrayDataReference((double[])input);
        ref double pR = ref MM.GetArrayDataReference((double[])output);
        pR = pV / pA;    // First row is special.
        for (int i = 1; i < size; i++)
        {
            pA = ref Add(ref pA, size);
            double sum = Add(ref pV, i) - MM.CreateSpan(ref pA, i)
                .Dot(MM.CreateSpan(ref pR, i));
            Add(ref pR, i) = sum / Add(ref pA, i);
        }
    }

    /// <summary>Gets the determinant of the matrix.</summary>
    /// <returns>The product of the main diagonal.</returns>
    public double Determinant() => values.DiagonalProduct(Rows, Cols);

    /// <summary>Checks if the provided argument is a matrix with the same values.</summary>
    /// <param name="other">The matrix to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a matrix with the same values.</returns>
    public bool Equals(LMatrix other) => (Matrix)this == other;

    /// <summary>Checks if the provided argument is a matrix with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a matrix with the same values.</returns>
    public override bool Equals(object? obj) => obj is LMatrix matrix && Equals(matrix);

    /// <summary>Returns the hashcode for this matrix.</summary>
    /// <returns>A hashcode summarizing the content of the matrix.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)values).GetHashCode(EqualityComparer<double>.Default);

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First matrix to compare.</param>
    /// <param name="right">Second matrix to compare.</param>
    /// <returns><see langword="true"/> when all corresponding cells are equals.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(LMatrix left, LMatrix right) => (Matrix)left == right;

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First lower-triangular matrix to compare.</param>
    /// <param name="right">Second upper-triangular matrix to compare.</param>
    /// <returns><see langword="true"/>w hen all corresponding cells are equals.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(LMatrix left, RMatrix right) => (Matrix)left == right;

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First lower-triangular matrix to compare.</param>
    /// <param name="right">Second rectangular matrix to compare.</param>
    /// <returns><see langword="true"/> when all corresponding cells are equals.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(LMatrix left, Matrix right) => (Matrix)left == right;

    /// <summary>Checks two matrices for inequality.</summary>
    /// <param name="left">First lower-triangular matrix to compare.</param>
    /// <param name="right">Second lower-triangular matrix to compare.</param>
    /// <returns><see langword="true"/> when a pair of cells exists with different values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(LMatrix left, LMatrix right) => (Matrix)left != right;

    /// <summary>Checks two matrices for inequality.</summary>
    /// <param name="left">First lower-triangular matrix to compare.</param>
    /// <param name="right">Right lower-triangular matrix to compare.</param>
    /// <returns><see langword="true"/> when a pair of cells exists with different values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(LMatrix left, RMatrix right) => !(left == right);

    /// <summary>Checks two matrices for inequality.</summary>
    /// <param name="left">First lower-triangular matrix to compare.</param>
    /// <param name="right">Second rectangular matrix to compare.</param>
    /// <returns><see langword="true"/> when a pair of cells exists with different values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(LMatrix left, Matrix right) => (Matrix)left != right;

    /// <summary>Gets a textual representation of this matrix.</summary>
    /// <returns>One line for each row, with space separated columns.</returns>
    public override string ToString() =>
        values.ToString(Rows, Cols, v => v.ToString("G6"), -1);

    /// <summary>Gets a textual representation of this matrix.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>One line for each row, with space separated columns.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        values.ToString(Rows, Cols, v => v.ToString(format, provider), -1);
}

/// <summary>JSON converter for triangular matrices.</summary>
public class LMatrixJsonConverter : JsonConverter<LMatrix>
{
    /// <summary>Reads and convert JSON to a <see cref="LMatrix"/> instance.</summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type of the object to convert.</param>
    /// <param name="options">JSON options.</param>
    /// <returns>A triangular matrix with the values read from JSON.</returns>
    public override LMatrix Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        int rows = 0, cols = 0;
        double[]? values = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.ValueTextEquals(nameof(Matrix.Rows)))
                {
                    reader.Read();
                    rows = reader.GetInt32();
                }
                else if (reader.ValueTextEquals(nameof(Matrix.Cols)))
                {
                    reader.Read();
                    cols = reader.GetInt32();
                }
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                values = new double[rows * cols];
                int total = rows * cols;
                ref double p = ref MM.GetArrayDataReference(values);
                reader.Read();
                while (reader.TokenType == JsonTokenType.Number && total-- > 0)
                {
                    p = reader.GetDouble();
                    p = ref Add(ref p, 1);
                    reader.Read();
                }
            }
        }
        return new(rows, cols, values!);
    }

    /// <summary>Converts a lower-triangular matrix to JSON.</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The matrix to serialize.</param>
    /// <param name="options">JSON options.</param>    
    public override void Write(
        Utf8JsonWriter writer,
        LMatrix value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(nameof(LMatrix.Rows), value.Rows);
        writer.WriteNumber(nameof(LMatrix.Cols), value.Cols);
        writer.WriteStartArray("values");
        foreach (double v in (double[])value)
            writer.WriteNumberValue(v);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
