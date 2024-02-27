namespace Austra.Library;

/// <summary>Represents an upper triangular matrix.</summary>
/// <remarks>
/// <para>Having a separate type for upper-triangular matrices is not a matter of storage,
/// but of semantics.</para>
/// <para>Upper-triangular matrices may have a different number of rows and columns.</para>
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
    IMultiplyOperators<RMatrix, DVector, DVector>,
    IMultiplyOperators<RMatrix, double, RMatrix>,
    IDivisionOperators<RMatrix, double, RMatrix>,
    IUnaryNegationOperators<RMatrix, RMatrix>,
    IMatrix
{
    /// <summary>Stores the cells of the matrix.</summary>
    private readonly double[] values;

    /// <summary>Creates an empty square matrix.</summary>
    /// <param name="size">Number of rows and columns.</param>
    public RMatrix(int size) => (Rows, Cols, values) = (size, size, new double[size * size]);

    /// <summary>Creates an empty rectangular matrix.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    public RMatrix(int rows, int cols) => (Rows, Cols, values) = (rows, cols, new double[rows * cols]);

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
    public RMatrix(DVector diagonal) =>
        (Rows, Cols, values) = (diagonal.Length, diagonal.Length, diagonal.CreateDiagonal());

    /// <summary>Creates a matrix filled with a uniform distribution generator.</summary>
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
        ref double cell = ref MM.GetArrayDataReference(values);
        for (int r = 0; r < rows; r++, cell = ref Add(ref cell, cols))
            for (int c = r; c < cols; c++)
                Add(ref cell, c) = FusedMultiplyAdd(random.NextDouble(), width, offset);
    }

    /// <summary>Creates a matrix filled with a uniform distribution generator.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random number generator.</param>
    public RMatrix(int rows, int cols, Random random)
    {
        (Rows, Cols, values) = (rows, cols, new double[rows * cols]);
        ref double cell = ref MM.GetArrayDataReference(values);
        for (int r = 0; r < rows; r++, cell = ref Add(ref cell, cols))
            for (int c = r; c < cols; c++)
                Add(ref cell, c) = random.NextDouble();
    }

    /// <summary>
    /// Creates a squared matrix filled with a uniform distribution generator.
    /// </summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <param name="random">A random number generator.</param>
    public RMatrix(int size, Random random) : this(size, size, random) { }

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

    /// <summary>
    /// Creates a squared matrix filled with a standard normal distribution.
    /// </summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <param name="random">A random standard normal generator.</param>
    public RMatrix(int size, NormalRandom random) : this(size, size, random) { }

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
    public DVector Diagonal() => values.Diagonal(Rows, Cols);

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

    /// <summary>Creates a new matrix with different dimensions.</summary>
    /// <param name="rows">New number of rows.</param>
    /// <param name="columns">New number of columns.</param>
    /// <returns>A new matrix, or the same one, when no resizing is needed.</returns>
    public RMatrix Redim(int rows, int columns) => (RMatrix)((Matrix)this).Redim(rows, columns);

    /// <summary>Creates a new matrix with different dimensions.</summary>
    /// <param name="size">New number of rows and columns.</param>
    /// <returns>A new matrix, or the same one, when no resizing is needed.</returns>
    public RMatrix Redim(int size) => Redim(size, size);

    /// <summary>Transposes the matrix.</summary>
    /// <returns>A new matrix with swapped rows and cells.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LMatrix Transpose() => ((Matrix)this).Transpose();

    /// <summary>Sums two upper matrices with the same size.</summary>
    /// <param name="m1">First matrix operand.</param>
    /// <param name="m2">Second matrix operand.</param>
    /// <returns>The sum of the two operands.</returns>
    public static RMatrix operator +(RMatrix m1, RMatrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        Contract.Requires(m1.Rows == m2.Rows);
        Contract.Requires(m1.Cols == m2.Cols);
        Contract.Ensures(Contract.Result<RMatrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<RMatrix>().Cols == m1.Cols);

        int r = m1.Rows, c = m1.Cols;
        double[] result = new double[m1.values.Length];
        for (int row = 0, offset = 0; row < r; row++, offset += c)
            m1.values.AsSpan(offset + row, c - row).Add(
                m2.values.AsSpan(offset + row, c - row), result.AsSpan(offset + row, c - row));
        return new(r, c, result);
    }

    /// <summary>Adds a scalar value to an upper triangular matrix.</summary>
    /// <remarks>The value is just added to the upper triangular part.</remarks>
    /// <param name="m">The matrix summand.</param>
    /// <param name="d">The scalar summand.</param>
    /// <returns>The sum of the matrix by the scalar.</returns>
    public static RMatrix operator +(RMatrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[m.values.Length];
        for (int row = 0, offset = 0; row < r; row++, offset += c)
            m.values.AsSpan(offset + row, c - row).Add(
                d, result.AsSpan(offset + row, c - row));
        return new(r, c, result);
    }

    /// <summary>Adds a scalar value to an upper triangular matrix.</summary>
    /// <param name="d">The scalar summand.</param>
    /// <param name="m">The matrix summand.</param>
    /// <returns>The pointwise sum of the matrix and the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RMatrix operator +(double d, RMatrix m) => m + d;

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
    public static RMatrix operator -(RMatrix m1, RMatrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        Contract.Requires(m1.Rows == m2.Rows);
        Contract.Requires(m1.Cols == m2.Cols);
        Contract.Ensures(Contract.Result<RMatrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<RMatrix>().Cols == m1.Cols);

        int r = m1.Rows, c = m1.Cols;
        double[] result = new double[m1.values.Length];
        for (int row = 0, offset = 0; row < r; row++, offset += c)
            m1.values.AsSpan(offset + row, c - row).Sub(
                m2.values.AsSpan(offset + row, c - row), result.AsSpan(offset + row, c - row));
        return new(r, c, result);
    }

    /// <summary>Subtracts a scalar value from an upper triangular matrix.</summary>
    /// <remarks>The value is just subtracted from the upper triangular part.</remarks>
    /// <param name="m">The matrix minuend.</param>
    /// <param name="d">The scalar subtrahend.</param>
    /// <returns>The substraction of the scalar from the matrix.</returns>
    public static RMatrix operator -(RMatrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[m.values.Length];
        for (int row = 0, offset = 0; row < r; row++, offset += c)
            m.values.AsSpan(offset + row, c - row).Sub(
                d, result.AsSpan(offset + row, c - row));
        return new(r, c, result);
    }

    /// <summary>Subtracts an upper triangular matrix from a scalar value.</summary>
    /// <remarks>The value is just subtracted from the upper triangular part.</remarks>
    /// <param name="d">The scalar minuend.</param>
    /// <param name="m">The matrix subtrahend.</param>
    /// <returns>The substraction of the matrix from the scalar.</returns>
    public static RMatrix operator -(double d, RMatrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[m.values.Length];
        for (int row = 0, offset = 0; row < r; row++, offset += c)
            CommonMatrix.Sub(d, m.values.AsSpan(offset + row, c - row),
                result.AsSpan(offset + row, c - row));
        return new(r, c, result);
    }

    /// <summary>Negates an upper right matrix.</summary>
    /// <param name="m">The matrix operand.</param>
    /// <returns>Cell-by-cell negation.</returns>
    public static RMatrix operator -(RMatrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<RMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<RMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[m.values.Length];
        for (int row = 0, offset = 0; row < r; row++, offset += c)
            m.values.AsSpan(offset + row, c - row).Neg(result.AsSpan(offset + row, c - row));
        return new(r, c, result);
    }

    /// <summary>Multiplies two upper-triangular matrices.</summary>
    /// <param name="m1">First matrix.</param>
    /// <param name="m2">Second matrix.</param>
    /// <returns>The resulting upper-triangular matrix.</returns>
    public static RMatrix operator *(RMatrix m1, RMatrix m2) => ((Matrix)m1) * m2;

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
            for (int i = 0; i < m; i++, pAi += n, pCi += n)
            {
                double* pBk = pB;
                for (int k = 0; k < n; k++, pBk += p)
                    new Span<double>(pBk + k, p - k + 1)
                        .MulAddStore(pAi[k], new Span<double>(pCi + k, p - k + 1));
            }
        }
        return new(m, p, result);
    }

    /// <summary>Multiplies an upper triangular matrix by a rectangular one.</summary>
    /// <param name="m1">An upper triangular matrix.</param>
    /// <param name="m2">A rectangular matrix.</param>
    /// <returns>The resulting rectangular matrix.</returns>
    public static Matrix operator *(RMatrix m1, Matrix m2) => ((Matrix)m1) * m2;

    /// <summary>Multiplies an upper triangular matrix by a scalar value.</summary>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <param name="d">A scalar multiplicand.</param>
    /// <returns>The multiplication of the matrix by the scalar.</returns>
    public static RMatrix operator *(RMatrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[m.values.Length];
        for (int row = 0, offset = 0; row < r; row++, offset += c)
            m.values.AsSpan(offset + row, c - row).Mul(
                d, result.AsSpan(offset + row, c - row));
        return new(r, c, result);
    }

    /// <summary>Multiplies an upper triangular matrix by a scalar value.</summary>
    /// <param name="d">A scalar multiplicand.</param>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <returns>The multiplication of the matrix by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RMatrix operator *(double d, RMatrix m) => m * d;

    /// <summary>Solves the equation m2*x = m1 for the matrix x.</summary>
    /// <param name="m1">The matrix at the right side.</param>
    /// <param name="m2">The matrix at the left side.</param>
    /// <returns>The solving matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator /(Matrix m1, RMatrix m2) => ((Matrix)m2).Solve(m1);

    /// <summary>Solves the equation m*x = v for the vector x.</summary>
    /// <param name="v">The vector at the right side.</param>
    /// <param name="m">The matrix at the left side.</param>
    /// <returns>The solving vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DVector operator /(DVector v, RMatrix m) => m.Solve(v);

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
    public static DVector operator *(RMatrix m, DVector v)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Requires(v.IsInitialized);
        Contract.Requires(m.Cols == v.Length);
        Contract.Ensures(Contract.Result<DVector>().Length == m.Rows);

        int r = m.Rows, c = m.Cols;
        double[] result = new double[r];
        double[] vector = (double[])v;
        ref double pB = ref MM.GetArrayDataReference(result);
        for (int row = 0, offset = 0; row < r; row++, offset += c)
            Add(ref pB, row) = m.values.AsSpan(offset + row, c - row)
                .Dot(vector.AsSpan(row));
        return result;
    }

    /// <summary>Gets the cell with the maximum absolute value.</summary>
    /// <remarks>
    /// The absolute maximum must always be zero or positive so, it is not a
    /// problem to scan the whole matrix, including the lower triangular part,
    /// which is filled with zeros.
    /// </remarks>
    /// <returns>The max-norm of the matrix.</returns>
    public double AMax() => values.AsSpan().AMax();

    /// <summary>Gets the cell with the minimum absolute value.</summary>
    /// <remarks>Cells below the diagonal are ignored.</remarks>
    /// <returns>The minimum absolute value in the triangular matrix.</returns>
    public double AMin()
    {
        int r = Rows, c = Cols;
        double min = values.AsSpan(0, c).AMin();
        for (int row = 1, offset = c; row < r; row++, offset += c)
            min = Min(min, values.AsSpan(offset + row, c - row).AMin());
        return min;
    }

    /// <summary>Gets the cell with the maximum value.</summary>
    /// <remarks>Zeros below the diagonal are ignored.</remarks>
    /// <returns>The maximum value in the triangular matrix.</returns>
    public double Maximum()
    {
        int r = Rows, c = Cols;
        double max = values.AsSpan(0, c).Max();
        for (int row = 1, offset = c; row < r; row++, offset += c)
            max = Max(max, values.AsSpan(offset + row, c - row).Max());
        return max;
    }

    /// <summary>Gets the cell with the minimum value.</summary>
    /// <remarks>Zeros below the diagonal are ignored.</remarks>
    /// <returns>The minimum value in the triangular matrix.</returns>
    public double Minimum()
    {
        int r = Rows, c = Cols;
        double min = values.AsSpan(0, c).Min();
        for (int row = 1, offset = c; row < r; row++, offset += c)
            min = Min(min, values.AsSpan(offset + row, c - row).Min());
        return min;
    }

    /// <summary>Multiplies this matrix by the transposed argument.</summary>
    /// <param name="m">Second operand.</param>
    /// <returns>The multiplication by the transposed argument.</returns>
    public Matrix MultiplyTranspose(RMatrix m)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(m.IsInitialized);
        if (Cols != m.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Rows);

        int r = Rows, n = Cols, c = m.Rows;
        double[] result = GC.AllocateUninitializedArray<double>(r * c);
        ref double x = ref MM.GetArrayDataReference(values);
        ref double y = ref MM.GetArrayDataReference(m.values);
        ref double z = ref MM.GetArrayDataReference(result), r0 = ref z;
        if (values == m.values)
            for (int i = 0; i < r; i++, x = ref Add(ref x, n), z = ref Add(ref z, c))
            {
                ref double pBj = ref y;
                for (int j = 0; j < i; j++, pBj = ref Add(ref pBj, n))
                    Add(ref z, j) = Add(ref r0, j * c + i);
                for (int j = i, len = n - i; j < c; j++, len--, pBj = ref Add(ref pBj, n))
                    if (len > 0)
                        Add(ref z, j) = MM.CreateSpan(ref Add(ref x, j), len)
                            .Dot(MM.CreateSpan(ref Add(ref pBj, j), len));
                    else
                        Add(ref z, j) = 0;
            }
        else
            for (int i = 0; i < r; i++, x = ref Add(ref x, n), z = ref Add(ref z, c))
            {
                ref double pBj = ref y;
                for (int j = 0; j < c; j++, pBj = ref Add(ref pBj, n))
                {
                    int s = Max(i, j), len = n - s;
                    if (len > 0)
                        Add(ref z, j) = MM.CreateSpan(ref Add(ref x, s), len)
                            .Dot(MM.CreateSpan(ref Add(ref pBj, s), len));
                    else
                        Add(ref z, j) = 0;
                }
            }
        return new(r, c, result);
    }

    /// <summary>Multiplies this matrix by its own transposed.</summary>
    /// <returns>The multiplication by the transposed argument.</returns>
    public Matrix Square() => MultiplyTranspose(this);

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
        ref double pA = ref Add(ref MM.GetArrayDataReference(values), size * size - 1);
        ref double pV = ref Add(ref MM.GetArrayDataReference((double[])input), size - 1);
        ref double pR = ref Add(ref MM.GetArrayDataReference((double[])output), size - 1);
        pR = pV / pA;    // Start from the last row/corner.
        for (int i = size - 2; i >= 0; i--)
        {
            pA = ref Subtract(ref pA, size + 1);
            pV = ref Subtract(ref pV, 1);
            pR = ref Subtract(ref pR, 1);
            double sum = pV - MM.CreateSpan(ref Add(ref pA, 1), size - i - 1)
                .Dot(MM.CreateSpan(ref Add(ref pR, 1), size - i - 1));
            pR = sum / pA;
        }
    }

    /// <summary>Gets the determinant of the matrix.</summary>
    /// <returns>The product of the main diagonal.</returns>
    public double Determinant() => values.Det(Rows, Cols);

    /// <summary>Checks if the matrix contains the given value.</summary>
    /// <param name="value">Value to locate.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(double value)
    {
        Contract.Requires(IsInitialized);
        return new ReadOnlySpan<double>(values).IndexOf(value) != -1;
    }

    /// <summary>Checks if the provided argument is a matrix with the same values.</summary>
    /// <param name="other">The matrix to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a matrix with the same values.</returns>
    public bool Equals(RMatrix other) => (Matrix)this == other;

    /// <summary>Checks if the provided argument is a matrix with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a matrix with the same values.</returns>
    public override bool Equals(object? obj) => obj is RMatrix matrix && Equals(matrix);

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
        values.ToString(Rows, Cols, v => v.ToString("G6"), 1);

    /// <summary>Gets a textual representation of this matrix.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>One line for each row, with space separated columns.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        values.ToString(Rows, Cols, v => v.ToString(format, provider), 1);
}
