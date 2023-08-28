namespace Austra.Library;

/// <summary>Represents a lower triangular matrix.</summary>
/// <remarks>
/// Triangular matrices may have a different number of rows and columns.
/// </remarks>
[JsonConverter(typeof(LMatrixJsonConverter))]
public readonly struct LMatrix :
    IAdditionOperators<LMatrix, LMatrix, LMatrix>,
    ISubtractionOperators<LMatrix, LMatrix, LMatrix>,
    IMultiplyOperators<LMatrix, Matrix, Matrix>,
    IMultiplyOperators<LMatrix, Vector, Vector>,
    IMultiplyOperators<LMatrix, double, LMatrix>,
    IDivisionOperators<LMatrix, double, LMatrix>,
    IUnaryNegationOperators<LMatrix, LMatrix>
{
    /// <summary>Stores the cells of the matrix.</summary>
    private readonly double[,] values;

    /// <summary>Creates an empty square matrix.</summary>
    /// <param name="size">Number of rows and columns.</param>
    public LMatrix(int size) => values = new double[size, size];

    /// <summary>Creates an empty rectangular matrix.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    public LMatrix(int rows, int cols) => values = new double[rows, cols];

    /// <summary>Creates a matrix from a bidimensional array.</summary>
    /// <param name="values">The array with cell values.</param>
    public LMatrix(double[,] values) => this.values = values;

    /// <summary>Creates a diagonal matrix given its diagonal.</summary>
    /// <param name="diagonal">Values in the diagonal.</param>
    public LMatrix(Vector diagonal) =>
        values = CommonMatrix.CreateDiagonal(diagonal);

    /// <summary>
    /// Creates a matrix filled with a uniform distribution generator.
    /// </summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random number generator.</param>
    /// <param name="offset">An offset for the random numbers.</param>
    /// <param name="width">Width for the uniform distribution.</param>
    public LMatrix(
        int rows, int cols, Random random,
        double offset = 0.0, double width = 1.0)
    {
        values = new double[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            int top = Min(cols, r + 1);
            for (int c = 0; c < top; c++)
                values[r, c] = FusedMultiplyAdd(random.NextDouble(), width, offset);
        }
    }

    /// <summary>
    /// Creates a matrix filled with a uniform distribution generator.
    /// </summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random number generator.</param>
    public LMatrix(
        int rows, int cols, Random random)
    {
        values = new double[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            int top = Min(cols, r + 1);
            for (int c = 0; c < top; c++)
                values[r, c] = random.NextDouble();
        }
    }

    /// <summary>
    /// Creates a square lower matrix filled with a uniform distribution generator.
    /// </summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <param name="random">A random number generator.</param>
    public LMatrix(int size, Random random) :
        this(size, size, random)
    { }

    /// <summary>
    /// Creates a matrix filled with a standard normal distribution.
    /// </summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random standard normal generator.</param>
    public LMatrix(int rows, int cols, NormalRandom random)
    {
        values = new double[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            int top = Min(cols, r + 1);
            for (int c = 0; c < top; c++)
                values[r, c] = random.NextDouble();
        }
    }

    /// <summary>Creates a squared matrix with a standard normal distribution.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <param name="random">A random standard normal generator.</param>
    public LMatrix(int size, NormalRandom random) : this(size, size, random)
    { }

    /// <summary>Creates an identity matrix given its size.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <returns>An identity matrix with the requested size.</returns>
    public static LMatrix Identity(int size) => CommonMatrix.CreateIdentity(size);

    /// <summary>Creates an identical lower triangular matrix.</summary>
    /// <returns>A deep clone of the instance.</returns>
    public LMatrix Clone() => (double[,])values.Clone();

    /// <summary>
    /// Implicit conversion from a bidimensional array to a matrix.
    /// </summary>
    /// <param name="values">A bidimensional array.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LMatrix(double[,] values) => new(values);

    /// <summary>
    /// Implicit conversion from a rectangular to a lower triangular matrix.
    /// </summary>
    /// <param name="m">A rectangular matrix.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LMatrix(Matrix m) => new((double[,])m);

    /// <summary>
    /// Explicit conversion from a matrix to a bidimensional array.
    /// </summary>
    /// <remarks>
    /// Use carefully: it returns the underlying bidimensional array.
    /// </remarks>
    /// <param name="m">The original matrix.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator double[,](LMatrix m) => m.values;

    /// <summary>Has the matrix been properly initialized?</summary>
    /// <remarks>
    /// Since Matrix is a struct, its default constructor doesn't
    /// initializes the underlying bidimensional array.
    /// </remarks>
    public bool IsInitialized => values != null;

    /// <summary>Gets the number of rows.</summary>
    public int Rows => values.GetLength(0);
    /// <summary>Gets the number of columns.</summary>
    public int Cols => values.GetLength(1);

    /// <summary>Checks if the matrix is a square one.</summary>
    public bool IsSquare => Rows == Cols;

    /// <summary>Gets the main diagonal.</summary>
    /// <returns>A vector containing values in the main diagonal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Diagonal()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<Vector>().Length == Min(Rows, Cols));

        return CommonMatrix.Diagonal(values);
    }

    /// <summary>Calculates the trace of a matrix.</summary>
    /// <returns>The sum of the cells in the main diagonal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Trace()
    {
        Contract.Requires(IsInitialized);

        return CommonMatrix.Trace(values);
    }

    /// <summary>Gets the value at a single cell.</summary>
    /// <param name="row">The row number, between 0 and Rows - 1.</param>
    /// <param name="column">The column number, between 0 and Cols - 1.</param>
    /// <returns>The value at the given cell.</returns>
    public double this[int row, int column]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => values[row, column];
    }

    /// <summary>Transposes the matrix.</summary>
    /// <returns>A new matrix with swapped rows and cells.</returns>
    public unsafe RMatrix Transpose()
    {
        Contract.Requires(IsInitialized);

        int c = Cols, r = Rows;
        double[,] result = new double[c, r];
        fixed (double* pA = values, pB = result)
        {
            if (Avx.IsSupported && r == c && (r & 0b11) == 0)
            {
                // Columns and rows are multiple of four.
                int r2 = r + r, c2 = c + c;
                for (int row = 0; row < r; row += 4)
                {
                    int top = Min(row + 1, c);
                    for (int col = 0; col < top; col += 4)
                    {
                        double* pp = pA + (row * r + col);
                        var row1 = Avx.LoadVector256(pp);
                        var row2 = Avx.LoadVector256(pp + r);
                        var row3 = Avx.LoadVector256(pp + r2);
                        var row4 = Avx.LoadVector256(pp + r2 + r);
                        var t1 = Avx.Shuffle(row1, row2, 0b0000);
                        var t2 = Avx.Shuffle(row1, row2, 0b1111);
                        var t3 = Avx.Shuffle(row3, row4, 0b0000);
                        var t4 = Avx.Shuffle(row3, row4, 0b1111);
                        row1 = Avx.Permute2x128(t1, t3, 0b00100000);
                        row2 = Avx.Permute2x128(t2, t4, 0b00100000);
                        row3 = Avx.Permute2x128(t1, t3, 0b00110001);
                        row4 = Avx.Permute2x128(t2, t4, 0b00110001);
                        double* qq = pB + (col * r + row);
                        Avx.Store(qq, row1);
                        Avx.Store(qq + c, row2);
                        Avx.Store(qq + c2, row3);
                        Avx.Store(qq + c2 + c, row4);
                    }
                }
            }
            else
                for (int row = 0; row < r; row++)
                {
                    int top = Min(row + 1, c);
                    for (int col = 0; col < top; col++)
                        pB[col * r + row] = pA[row * c + col];
                }
        }
        return result;
    }

    /// <summary>Sums two lower matrices with the same size.</summary>
    /// <param name="m1">First matrix operand.</param>
    /// <param name="m2">Second matrix operand.</param>
    /// <returns>The sum of the two operands.</returns>
    public static unsafe LMatrix operator +(LMatrix m1, LMatrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        Contract.Requires(m1.Rows == m2.Rows);
        Contract.Requires(m1.Cols == m2.Cols);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m1.Cols);

        int r = m1.Rows;
        int c = m1.Cols;
        double[,] result = new double[r, c];
        if (c < r)
            r = c;
        fixed (double* pA = m1.values, pB = m2.values, pC = result)
        {
            *pC = *pA + *pB;
            int offset = 0;
            for (int row = 1; row < r; row++)
            {
                offset += c;
                int col = 0, k = offset;
                if (Avx.IsSupported)                  
                    for (int top = (row + 1) & Simd.AVX_MASK; col < top; col += 4, k += 4)
                        Avx.Store(
                            address: pC + k,
                            source: Avx.Add(
                                left: Avx.LoadVector256(pA + k),
                                right: Avx.LoadVector256(pB + k)));
                for (; col <= row; col++, k++)
                    pC[k] = pA[k] + pB[k];
            }
            if (m1.Rows > c)
            {
                int idx = c * c;
                int len = m1.values.Length;
                if (Avx.IsSupported)
                    for (int top = len & Simd.AVX_MASK; idx < top; idx += 4)
                        Avx.Store(
                            address: pC + idx,
                            source: Avx.Add(
                                left: Avx.LoadVector256(pA + idx),
                                right: Avx.LoadVector256(pB + idx)));
                for (; idx < len; idx++)
                    pC[idx] = pA[idx] + pB[idx];
            }
        }
        return result;
    }

    /// <summary>Subtracts two lower matrices with the same size.</summary>
    /// <param name="m1">First matrix operand.</param>
    /// <param name="m2">Second matrix operand.</param>
    /// <returns>The subtraction of the two operands.</returns>
    public static unsafe LMatrix operator -(LMatrix m1, LMatrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        Contract.Requires(m1.Rows == m2.Rows);
        Contract.Requires(m1.Cols == m2.Cols);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m1.Cols);

        int r = m1.Rows;
        int c = m1.Cols;
        double[,] result = new double[r, c];
        if (c < r)
            r = c;
        fixed (double* pA = m1.values, pB = m2.values, pC = result)
        {
            *pC = *pA - *pB;
            int offset = 0;
            for (int row = 1; row < r; row++)
            {
                offset += c;
                int col = 0;
                if (Avx.IsSupported)
                    for (int top = (row + 1) & Simd.AVX_MASK; col < top; col += 4)
                    {
                        int k = offset + col;
                        Avx.Store(
                            address: pC + k,
                            source: Avx.Subtract(
                                left: Avx.LoadVector256(pA + k),
                                right: Avx.LoadVector256(pB + k)));
                    }
                for (; col <= row; col++)
                {
                    int k = offset + col;
                    pC[k] = pA[k] - pB[k];
                }
            }
            if (m1.Rows > c)
            {
                int idx = c * c;
                int len = m1.values.Length;
                if (Avx.IsSupported)
                    for (int top = len & Simd.AVX_MASK; idx < top; idx += 4)
                        Avx.Store(
                            address: pC + idx,
                            source: Avx.Subtract(
                                left: Avx.LoadVector256(pA + idx),
                                right: Avx.LoadVector256(pB + idx)));
                for (; idx < len; idx++)
                    pC[idx] = pA[idx] - pB[idx];
            }
        }
        return result;
    }

    /// <summary>Negates a lower matrix.</summary>
    /// <param name="m">The matrix operand.</param>
    /// <returns>Cell-by-cell negation.</returns>
    public static unsafe LMatrix operator -(LMatrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows;
        int c = m.Cols;
        double[,] result = new double[r, c];
        if (c < r)
            r = c;
        fixed (double* pA = m.values, pC = result)
        {
            *pC = -*pA;
            int offset = 0;
            for (int row = 1; row < r; row++)
            {
                offset += c;
                int col = 0;
                if (Avx.IsSupported)
                {
                    var z = Vector256<double>.Zero;
                    for (int top = (row + 1) & Simd.AVX_MASK; col < top; col += 4)
                    {
                        int k = offset + col;
                        Avx.Store(
                            address: pC + k,
                            source: Avx.Subtract(
                                left: z,
                                right: Avx.LoadVector256(pA + k)));
                    }
                }
                for (; col <= row; col++)
                {
                    int k = offset + col;
                    pC[k] = -pA[k];
                }
            }
            if (m.Rows > c)
            {
                int idx = c * c;
                int len = m.values.Length;
                if (Avx.IsSupported)
                {
                    var z = Vector256<double>.Zero;
                    for (int top = len & Simd.AVX_MASK; idx < top; idx += 4)
                        Avx.Store(
                            address: pC + idx,
                            source: Avx.Subtract(
                                left: z,
                                right: Avx.LoadVector256(pA + idx)));
                }
                for (; idx < len; idx++)
                    pC[idx] = -pA[idx];
            }
        }
        return result;
    }

    /// <summary>Multiplies a lower triangular matrix by a scalar value.</summary>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <param name="d">A scalar multiplicand.</param>
    /// <returns>The multiplication of the matrix by the scalar.</returns>
    public static unsafe LMatrix operator *(LMatrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<LMatrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<LMatrix>().Cols == m.Cols);

        int r = m.Rows;
        int c = m.Cols;
        double[,] result = new double[r, c];
        if (c < r)
            r = c;
        fixed (double* pA = m.values, pC = result)
        {
            *pC = *pA * d;
            int offset = 0;
            Vector256<double> vec = Vector256.Create(d);
            for (int row = 1; row < r; row++)
            {
                offset += c;
                int col = 0, k = offset;
                if (Avx.IsSupported) 
                    for (int top = (row + 1) & Simd.AVX_MASK; col < top; col += 4)
                    {
                        Avx.Store(
                            address: pC + k,
                            source: Avx.Multiply(
                                left: Avx.LoadVector256(pA + k),
                                right: vec));
                        k += 4;
                    }
                for (; col <= row; col++, k++)
                    pC[k] = pA[k] * d;
            }
            if (m.Rows > c)
            {
                int idx = c * c;
                int len = m.values.Length;
                if (Avx.IsSupported)
                    for (int top = len & Simd.AVX_MASK; idx < top; idx += 4)
                        Avx.Store(
                            address: pC + idx,
                            source: Avx.Multiply(
                                left: Avx.LoadVector256(pA + idx),
                                right: vec));
                for (; idx < len; idx++)
                    pC[idx] = pA[idx] * d;
            }
        }
        return result;
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

        int m = m1.Rows;
        int n = m1.Cols;
        int p = m2.Cols;
        double[,] result = new double[m, p];
        int lastBlockIndex = p & Simd.AVX_MASK;
        fixed (double* pA = m1.values, pB = (double[,])m2, pC = result)
        {
            double* pAi = pA;
            double* pCi = pC;
            for (int i = 0; i < m; i++)
            {
                double* pBk = pB;
                int top = Min(i + 1, n);
                for (int k = 0; k < top; k++)
                {
                    double d = pAi[k];
                    int j = 0;
                    if (Avx.IsSupported)
                    {
                        var vd = Vector256.Create(d);
                        for (; j < lastBlockIndex; j += 4)
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
        return result;
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

        int m = m1.Rows;
        int n = m1.Cols;
        int p = m2.Cols;
        double[,] result = new double[m, p];
        fixed (double* pA = (double[,])m1, pB = m2.values, pC = result)
        {
            double* pAi = pA;
            double* pCi = pC;
            for (int i = 0; i < m; i++)
            {
                double* pBk = pB;
                *pCi += *pBk * *pAi;
                for (int k = 1; k < n; k++)
                {
                    pBk += p;
                    double d = pAi[k];
                    int top = Min(k + 1, n);
                    int j = 0;
                    if (Avx.IsSupported)
                    {
                        var vd = Vector256.Create(d);
                        for (int last = top & Simd.AVX_MASK; j < last; j += 4)
                            Avx.Store(pCi + j,
                                Avx.LoadVector256(pCi + j).MultiplyAdd(pBk + j, vd));
                    }
                    for (; j < top; j++)
                        pCi[j] += pBk[j] * d;
                }
                pAi += n;
                pCi += n;
            }
        }
        return result;
    }

    /// <summary>Adds a lower triangular matrix to an upper triangular one.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator *(LMatrix m1, RMatrix m2) =>
        m1 * new Matrix((double[,])m2);

    /// <summary>Multiplies an upper triangular matrix with a lower triangular one.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator *(RMatrix m1, LMatrix m2) =>
        new Matrix((double[,])m1) * m2;

    /// <summary>Gets the cell with the maximum absolute value.</summary>
    /// <returns>The max-norm of the matrix.</returns>
    public double AMax() => new Matrix((double[,])this).AMax();

    /// <summary>Gets the cell with the minimum absolute value.</summary>
    /// <returns>The minimum absolute value in the triangular matrix.</returns>
    public unsafe double AMin()
    {
        Contract.Requires(IsInitialized);
        Contract.Ensures(Contract.Result<double>() >= 0);

        int r = Rows;
        int c = Cols;
        double min = Abs(values[0, 0]);
        if (c < r)
            r = c;
        fixed (double* a = values)
        {
            double* pA = a;
            for (int row = 0; row < r; row++, pA += c)
                for (int col = 0; col <= row; col++)
                    min = Min(min, Abs(pA[col]));
            if (Rows > c)
            {
                int c2 = c * c;
                min = Min(min, CommonMatrix.AbsoluteMinimum(a + c2, values.Length - c2));
            }
        }
        return min;
    }

    /// <summary>Gets the cell with the maximum value.</summary>
    public double Maximum() => new Matrix((double[,])this).Maximum();
    /// <summary>Gets the cell with the minimum value.</summary>
    public double Minimum() => new Matrix((double[,])this).Minimum();

    /// <summary>Multiplies this matrix by the transposed argument.</summary>
    /// <param name="m">Second operand.</param>
    /// <returns>The multiplication by the transposed argument.</returns>
    public unsafe Matrix MultiplyTranspose(LMatrix m)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(m.IsInitialized);
        Contract.Requires(Cols == m.Cols);
        Contract.Ensures(Contract.Result<Matrix>().Rows == Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Rows);

        int r = Rows;
        int n = Cols;
        int c = m.Rows;
        double[,] result = new double[r, c];
        fixed (double* pA = values, pB = m.values, pC = result)
        {
            double* pAi = pA;
            double* pCi = pC;
            for (int i = 0; i < r; i++)
            {
                double* pBj = pB;
                *pCi = *pAi * *pBj;
                pBj += c;
                for (int j = 1; j < c; j++)
                {
                    int size = Min(i, j) + 1;
                    double acc = 0;
                    int k = 0;
                    if (Avx.IsSupported)
                    {
                        Vector256<double> sum = Vector256<double>.Zero;
                        for (int top = size & Simd.AVX_MASK; k < top; k += 4)
                            sum = sum.MultiplyAdd(pAi + k, pBj + k);
                        acc = sum.Sum();
                    }
                    for (; k < size; k++)
                        acc += pAi[k] * pBj[k];
                    pCi[j] = acc;
                    pBj += c;
                }
                pAi += r;
                pCi += r;
            }
        }
        return result;
    }

    /// <summary>Transform a vector using a matrix.</summary>
    /// <param name="m">The transformation matrix.</param>
    /// <param name="v">Vector to transform.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator *(LMatrix m, Vector v) =>
        m.Multiply(v, new double[m.Rows]);

    /// <summary>Transforms a vector using this matrix.</summary>
    /// <param name="v">Vector to transform.</param>
    /// <param name="result">Preallocated buffer for the transformed vector.</param>
    /// <returns>The transformed vector.</returns>
    public unsafe double[] Multiply(Vector v, double[] result)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        Contract.Requires(Cols == v.Length);
        Contract.Requires(result.Length == Rows);

        int r = Rows;
        int c = Cols;
        fixed (double* pA = values, pX = (double[])v, pB = result)
        {
            double* pA1 = pA;
            double* pB1 = pB;
            *pB1++ = *pA1 * *pX;
            pA1 += c;
            if (c >= 8 && Avx.IsSupported)
                for (int i = 1; i < r; i++)
                {
                    Vector256<double> vec = Vector256<double>.Zero;
                    int top = Min(i + 1, c), j = 0;
                    for (int last = top & Simd.AVX_MASK; j < last; j += 4)
                        vec = vec.MultiplyAdd(pA1 + j, pX + j);
                    double d = vec.Sum();
                    for (; j < top; j++)
                        d += pA1[j] * pX[j];
                    *pB1++ = d;
                    pA1 += c;
                }
            else
                for (int i = 1; i < r; i++)
                {
                    double d = 0;
                    int top = Min(i + 1, c), j = 0;
                    for (int last = top & Simd.AVX_MASK; j < last; j += 4)
                        d += (pA1[j] * pX[j]) + (pA1[j + 1] * pX[j + 1]) +
                            (pA1[j + 2] * pX[j + 2]) + (pA1[j + 3] * pX[j + 3]);
                    for (; j < top; j++)
                        d += pA1[j] * pX[j];
                    *pB1++ = d;
                    pA1 += c;
                }
        }
        return result;
    }

    /// <summary>Solves the equation Ax = b for x.</summary>
    /// <param name="v">The right side of the equation.</param>
    /// <returns>The solving vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Vector Solve(Vector v)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(IsSquare);
        Contract.Requires(v.Length == Rows);
        Contract.Ensures(Contract.Result<Vector>().Length == v.Length);

        Vector result = new(v.Length);
        Solve(v, result);
        return result;
    }

    /// <summary>Solves the equation Ax = b for x.</summary>
    /// <param name="input">The right side of the equation.</param>
    /// <param name="output">The solving vector.</param>
    public unsafe void Solve(Vector input, Vector output)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(IsSquare);
        Contract.Requires(input.Length == Rows);
        Contract.Requires(output.Length == Rows);

        int size = input.Length;
        fixed (double* pA = values, pV = (double[])input, pR = (double[])output)
        {
            *pR = *pV / *pA;
            double* pai = pA;
            for (int i = 1; i < size; i++)
            {
                pai += size;
                double sum = pV[i];
                int j = 0;
                if (Avx.IsSupported)
                {
                    var acc = Vector256<double>.Zero;
                    for (int top = i & Simd.AVX_MASK; j < top; j += 4)
                        acc = acc.MultiplyAdd(pai + j, pR + j);
                    sum -= acc.Sum();
                }
                for (; j < i; j++)
                    sum -= pR[j] * pai[j];
                pR[i] = sum / pai[i];
            }
        }
    }

    /// <summary>Transforms a vector and adds an offset.</summary>
    /// <param name="multiplicand">Vector to transform.</param>
    /// <param name="add">Vector to add.</param>
    /// <param name="result">Preallocated buffer for the result.</param>
    /// <returns>this * multiplicand + add.</returns>
    public Vector MultiplyAdd(Vector multiplicand, Vector add, double[] result) =>
        add.Add(Multiply(multiplicand, result), result);

    /// <summary>Gets the determinant of the matrix.</summary>
    /// <returns>The product of the main diagonal.</returns>
    public double Determinant() => CommonMatrix.DiagonalProduct(values);

    /// <summary>Gets a textual representation of this matrix.</summary>
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

/// <summary>JSON converter for triangular matrices.</summary>
public class LMatrixJsonConverter : JsonConverter<LMatrix>
{
    /// <inheritdoc/>
    public unsafe override LMatrix Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        int rows = 0, cols = 0;
        double[,]? values = null;
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
                values = new double[rows, cols];
                fixed (double* p = values)
                {
                    int total = rows * cols;
                    double* pa = p;
                    reader.Read();
                    while (reader.TokenType == JsonTokenType.Number && total-- > 0)
                    {
                        *pa++ = reader.GetDouble();
                        reader.Read();
                    }
                }
            }
        }
        return new(values!);
    }

    /// <inheritdoc/>
    public unsafe override void Write(
        Utf8JsonWriter writer,
        LMatrix value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(nameof(LMatrix.Rows), value.Rows);
        writer.WriteNumber(nameof(LMatrix.Cols), value.Cols);
        writer.WriteStartArray("values");
        fixed (double* pV = (double[,])value)
        {
            double* pEnd = pV + value.Rows * value.Cols;
            for (double* p = pV; p < pEnd; p++)
                writer.WriteNumberValue(*p);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
