namespace Austra.Library;

using Austra.Library.Stats;
using static Unsafe;

/// <summary>Represents a dense rectangular matrix.</summary>
/// <remarks>
/// <para>Values are stored in a one-dimensional array, in row-major order.
/// Compatibility with bidimensional arrays, however, has been preserved.</para>
/// </remarks>
[JsonConverter(typeof(MatrixJsonConverter))]
public readonly struct Matrix :
    IFormattable,
    IEquatable<Matrix>,
    IEqualityOperators<Matrix, Matrix, bool>,
    IEqualityOperators<Matrix, LMatrix, bool>,
    IEqualityOperators<Matrix, RMatrix, bool>,
    IAdditionOperators<Matrix, Matrix, Matrix>,
    IAdditionOperators<Matrix, LMatrix, Matrix>,
    IAdditionOperators<Matrix, double, Matrix>,
    ISubtractionOperators<Matrix, Matrix, Matrix>,
    ISubtractionOperators<Matrix, LMatrix, Matrix>,
    ISubtractionOperators<Matrix, double, Matrix>,
    IMultiplyOperators<Matrix, Matrix, Matrix>,
    IMultiplyOperators<Matrix, Vector, Vector>,
    IMultiplyOperators<Matrix, double, Matrix>,
    IDivisionOperators<Matrix, double, Matrix>,
    IUnaryNegationOperators<Matrix, Matrix>,
    IPointwiseOperators<Matrix>,
    IMatrix
{
    /// <summary>Stores the cells of the matrix.</summary>
    private readonly double[] values;

    /// <summary>Creates an empty square matrix.</summary>
    /// <param name="size">Number of rows and columns.</param>
    public Matrix(int size) =>
        (Rows, Cols, values) = (size, size, new double[size * size]);

    /// <summary>Creates an empty rectangular matrix.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    public Matrix(int rows, int cols) =>
        (Rows, Cols, values) = (rows, cols, new double[rows * cols]);

    /// <summary>Creates a matrix using a formula to fill its cells.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="f">A function defining cell content.</param>
    public unsafe Matrix(int rows, int cols, Func<int, int, double> f)
    {
        (Rows, Cols, values) = (rows, cols, new double[rows * cols]);
        fixed (double* p = values)
        {
            int idx = 0;
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    p[idx++] = f(i, j);
        }
    }

    /// <summary>Creates a square matrix using a formula to fill its cells.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <param name="f">A function defining cell content.</param>
    public Matrix(int size, Func<int, int, double> f) : this(size, size, f) { }

    /// <summary>Creates a matrix given its rows.</summary>
    /// <param name="rows">The array of rows.</param>
    public unsafe Matrix(params Vector[] rows)
    {
        if (rows == null || rows.Length == 0)
            throw new MatrixSizeException();
        Rows = rows.Length;
        Cols = rows[0].Length;
        if (Cols == 0)
            throw new MatrixSizeException();
        for (int i = 1; i < rows.Length; i++)
            if (rows[i].Length != Cols)
                throw new MatrixSizeException();
        values = new double[Rows * Cols];
        fixed (double* pA = values)
        {
            long rowSize = (long)Cols * sizeof(double);
            double* p = pA;
            foreach (Vector row in rows)
                fixed (double* q = (double[])row)
                {
                    Buffer.MemoryCopy(q, p, rowSize, rowSize);
                    p += Cols;
                }
        }
    }

    /// <summary>
    /// Creates a matrix with a given number of rows and columns, and its internal array.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of columns.</param>
    /// <param name="values">Internal storage.</param>
    public Matrix(int rows, int columns, double[] values) =>
        (Rows, Cols, this.values) = (rows, columns, values);

    /// <summary>Creates a diagonal matrix given its diagonal.</summary>
    /// <param name="diagonal">Values in the diagonal.</param>
    public Matrix(Vector diagonal) =>
        (Rows, Cols, values) = (diagonal.Length, diagonal.Length, CommonMatrix.CreateDiagonal(diagonal));

    /// <summary>Creates a diagonal matrix given its diagonal.</summary>
    /// <param name="diagonal">Values in the diagonal.</param>
    public Matrix(double[] diagonal) =>
        (Rows, Cols, values) = (diagonal.Length, diagonal.Length, CommonMatrix.CreateDiagonal(diagonal));

    /// <summary>Creates a matrix filled with a uniform distribution generator.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random number generator.</param>
    /// <param name="offset">An offset for the random numbers.</param>
    /// <param name="width">Width for the uniform distribution.</param>
    public unsafe Matrix(
        int rows, int cols, Random random,
        double offset = 0.0, double width = 1.0)
    {
        (Rows, Cols, values) = (rows, cols, new double[rows * cols]);
        fixed (double* pA = values)
        {
            int len = rows * cols;
            for (int i = 0; i < len; i++)
                pA[i] = FusedMultiplyAdd(random.NextDouble(), width, offset);
        }
    }

    /// <summary>Creates a matrix filled with a uniform distribution generator.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random number generator.</param>
    public unsafe Matrix(int rows, int cols, Random random)
    {
        (Rows, Cols, values) = (rows, cols, new double[rows * cols]);
        fixed (double* pA = values)
        {
            int len = rows * cols;
            for (int i = 0; i < len; i++)
                pA[i] = random.NextDouble();
        }
    }

    /// <summary>
    /// Creates a square matrix filled with a uniform distribution generator.
    /// </summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <param name="random">A random number generator.</param>
    public Matrix(int size, Random random) : this(size, size, random) { }

    /// <summary>Creates a matrix filled with a standard normal distribution.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random standard normal generator.</param>
    public Matrix(int rows, int cols, NormalRandom random)
    {
        int i = 0, len = rows * cols;
        (Rows, Cols, values) = (rows, cols, GC.AllocateUninitializedArray<double>(len));
        ref double p = ref MemoryMarshal.GetArrayDataReference(values);
        for (int t = len & ~1; i < t; i += 2)
            random.NextDoubles(ref Add(ref p, i));
        if (i < len)
            Add(ref p, i) = random.NextDouble();
    }

    /// <summary>Creates a squared matrix filled with a standard normal distribution.</summary>
    /// <param name="size">Number of rows.</param>
    /// <param name="random">A random standard normal generator.</param>
    public Matrix(int size, NormalRandom random) : this(size, size, random) { }

    /// <summary>Creates an identity matrix given its size.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <returns>An identity matrix with the requested size.</returns>
    public static Matrix Identity(int size) =>
        new(size, size, CommonMatrix.CreateIdentity(size));

    /// <summary>Creates a matrix given its columns.</summary>
    /// <param name="columns">The array of columns.</param>
    /// <returns>A new matrix created with the provided columns.</returns>
    public static Matrix FromColumns(params Vector[] columns) =>
        new Matrix(columns).Transpose();

    /// <summary>Horizontal concatenation of two matrices.</summary>
    /// <param name="left">Left matrix.</param>
    /// <param name="right">Right matrix.</param>
    /// <returns>A new matrix with more columns.</returns>
    public unsafe static Matrix HCat(Matrix left, Matrix right)
    {
        if (left.Rows != right.Rows)
            throw new MatrixSizeException();
        int w = left.Rows, c1 = left.Cols, c2 = right.Cols, c = c1 + c2;
        double[] newValues = new double[w * c];
        fixed (double* l = left.values, r = right.values, t = newValues)
            for (int row = 0; row < w; row++)
            {
                Buffer.MemoryCopy(l + row * c1, t + row * c,
                    c1 * sizeof(double), c1 * sizeof(double));
                Buffer.MemoryCopy(r + row * c2, t + row * c + c1,
                    c2 * sizeof(double), c2 * sizeof(double));
            }
        return new(w, c, newValues);
    }

    /// <summary>Horizontal concatenation of a matrix and a new column.</summary>
    /// <param name="left">Left matrix.</param>
    /// <param name="newColumn">New column, as a vector.</param>
    /// <returns>A new matrix with one more column.</returns>
    public unsafe static Matrix HCat(Matrix left, Vector newColumn)
    {
        if (left.Rows != newColumn.Length)
            throw new MatrixSizeException();
        int w = left.Rows, c1 = left.Cols, c = c1 + 1;
        double[] newValues = new double[w * c];
        fixed (double* l = left.values, r = (double[])newColumn, t = newValues)
            for (int row = 0; row < w; row++)
            {
                Buffer.MemoryCopy(l + row * c1, t + row * c,
                    c1 * sizeof(double), c1 * sizeof(double));
                t[row * c + c1] = r[row];
            }
        return new(w, c, newValues);
    }

    /// <summary>Horizontal concatenation of a new column and a matrix.</summary>
    /// <param name="right">Left matrix.</param>
    /// <param name="newColumn">New column, as a vector.</param>
    /// <returns>A new matrix with one more column.</returns>
    public unsafe static Matrix HCat(Vector newColumn, Matrix right)
    {
        if (right.Rows != newColumn.Length)
            throw new MatrixSizeException();
        int w = right.Rows, c1 = right.Cols, c = c1 + 1;
        double[] newValues = new double[w * c];
        fixed (double* l = right.values, r = (double[])newColumn, t = newValues)
            for (int row = 0; row < w; row++)
            {
                t[row * c] = r[row];
                Buffer.MemoryCopy(l + row * c1, t + row * c + 1,
                    c1 * sizeof(double), c1 * sizeof(double));
            }
        return new(w, c, newValues);
    }

    /// <summary>Vertical concatenation of two matrices.</summary>
    /// <param name="upper">Upper matrix.</param>
    /// <param name="lower">Lower matrix.</param>
    /// <returns>A new matrix with more rows.</returns>
    public unsafe static Matrix VCat(Matrix upper, Matrix lower)
    {
        if (upper.Cols != lower.Cols)
            throw new MatrixSizeException();
        int w1 = upper.Rows, c = upper.Cols, w2 = lower.Rows, w = w1 + w2;
        double[] newValues = new double[w * c];
        fixed (double* u = upper.values, l = lower.values, t = newValues)
        {
            Buffer.MemoryCopy(u, t,
                w1 * c * sizeof(double), w1 * c * sizeof(double));
            Buffer.MemoryCopy(l, t + w1 * c,
                w2 * c * sizeof(double), w2 * c * sizeof(double));
        }
        return new(w, c, newValues);
    }

    /// <summary>Vertical concatenation of a matrix and a row vector.</summary>
    /// <param name="upper">Upper matrix.</param>
    /// <param name="lower">Lower row vector.</param>
    /// <returns>A new matrix with one more row.</returns>
    public unsafe static Matrix VCat(Matrix upper, Vector lower)
    {
        if (upper.Cols != lower.Length)
            throw new MatrixSizeException();
        int w1 = upper.Rows, c = upper.Cols, w = w1 + 1;
        double[] newValues = new double[w * c];
        fixed (double* u = upper.values, l = (double[])lower, t = newValues)
        {
            Buffer.MemoryCopy(u, t,
                w1 * c * sizeof(double), w1 * c * sizeof(double));
            Buffer.MemoryCopy(l, t + w1 * c,
                c * sizeof(double), c * sizeof(double));
        }
        return new(w, c, newValues);
    }

    /// <summary>Vertical concatenation of a row vector and a matrix.</summary>
    /// <param name="upper">Upper row vector.</param>
    /// <param name="lower">Lower matrix.</param>
    /// <returns>A new matrix with one more row.</returns>
    public unsafe static Matrix VCat(Vector upper, Matrix lower)
    {
        if (upper.Length != lower.Cols)
            throw new MatrixSizeException();
        int w2 = lower.Rows, c = lower.Cols, w = w2 + 1;
        double[] newValues = new double[w * c];
        fixed (double* u = (double[])upper, l = lower.values, t = newValues)
        {
            Buffer.MemoryCopy(l, t,
                c * sizeof(double), c * sizeof(double));
            Buffer.MemoryCopy(u, t + c,
                w2 * c * sizeof(double), w2 * c * sizeof(double));
        }
        return new(w, c, newValues);
    }

    /// <summary>Creates an identical rectangular matrix.</summary>
    /// <returns>A deep clone of the instance.</returns>
    public Matrix Clone() => new(Rows, Cols, (double[])values.Clone());

    /// <summary>
    /// Explicit conversion from a matrix to a bidimensional array.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy of the original matrix storage.
    /// </remarks>
    /// <param name="m">The original matrix.</param>
    /// <returns>The underlying bidimensional array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe explicit operator double[,](Matrix m)
    {
        double[,] result = new double[m.Rows, m.Cols];
        fixed (double* source = m.values, target = result)
        {
            int size = m.Rows * m.Cols * sizeof(double);
            Buffer.MemoryCopy(source, target, size, size);
        }
        return result;
    }

    /// <summary>
    /// Explicit conversion from a matrix to a one-dimensional array.
    /// </summary>
    /// <remarks>
    /// Use carefully: it returns the underlying one-dimensional array.
    /// </remarks>
    /// <param name="m">The original matrix.</param>
    /// <returns>The underlying bidimensional array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe explicit operator double[](Matrix m) => m.values;

    /// <summary>Has the matrix been properly initialized?</summary>
    /// <remarks>
    /// Since <see cref="Matrix"/> is a struct, its default constructor doesn't
    /// initializes the underlying bidimensional array.
    /// </remarks>
    public bool IsInitialized => values != null;

    /// <summary>Gets the number of rows.</summary>
    public int Rows { get; }

    /// <summary>Gets the number of columns.</summary>
    public int Cols { get; }

    /// <summary>Checks if the matrix is a square one.</summary>
    public bool IsSquare => Rows == Cols;

    /// <summary>Checks if the matrix is a symmetric one.</summary>
    /// <returns><see langword="true"/> when there's symmetry accross the diagonal.</returns>
    public unsafe bool IsSymmetric()
    {
        if (Rows != Cols)
            return false;
        int size = Rows;
        fixed (double* p = values)
            for (int row = 0; row < Rows; row++)
            {
                double* q = p + row * size;
                double* r = q + size + row;
                for (int col = row + 1; col < Cols; col++, r += size)
                    if (q[col] != *r)
                        return false;
            }
        return true;
    }

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

    /// <summary>Gets or sets the value of a single cell.</summary>
    /// <param name="row">The row number, between 0 and Rows - 1.</param>
    /// <param name="column">The column number, between 0 and Cols - 1.</param>
    /// <returns>The value at the given cell.</returns>
    public double this[int row, int column]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => values[row * Cols + column];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => values[row * Cols + column] = value;
    }

    /// <summary>Gets the value of a single cell using <see cref="Index"/>.</summary>
    /// <param name="row">The row number, between 0 and Rows - 1.</param>
    /// <param name="column">The column number, between 0 and Cols - 1.</param>
    /// <returns>The value at the given cell.</returns>
    public double this[Index row, Index column]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => values[row.GetOffset(Rows) * Cols + column.GetOffset(Cols)];
    }

    /// <summary>Gets a range of rows and columns as a new matrix.</summary>
    /// <param name="rowRange">Row range.</param>
    /// <param name="columnRange">Column range.</param>
    /// <returns>A new matrix.</returns>
    public unsafe Matrix this[Range rowRange, Range columnRange]
    {
        get
        {
            (int rOff, int rLen) = rowRange.GetOffsetAndLength(Rows);
            (int cOff, int cLen) = columnRange.GetOffsetAndLength(Cols);
            if (cLen == Cols)
            {
                if (rLen == Rows)
                    return this;
                if (rLen > 0)
                {
                    // The easiest case.
                    double[] newValues = new double[rLen * Cols];
                    fixed (double* p = values, q = newValues)
                        Buffer.MemoryCopy(p + rOff * Cols, q,
                            rLen * Cols * sizeof(double),
                            rLen * Cols * sizeof(double));
                    return new(rLen, Cols, newValues);
                }
            }
            if (cLen > 0 && rLen > 0)
            {
                double[] newValues = new double[rLen * cLen];
                int size = cLen * sizeof(double);
                fixed (double* p = values, q = newValues)
                {
                    double* source = p + rOff * Cols + cOff;
                    for (int row = 0; row < rLen; row++, source += Cols)
                        Buffer.MemoryCopy(source, q + row * cLen, size, size);
                }
                return new(rLen, cLen, newValues);
            }
            return new(0);
        }
    }

    /// <summary>Gets a range of rows and columns as a new matrix.</summary>
    /// <param name="rowRange">Row range.</param>
    /// <param name="column">Column index.</param>
    /// <returns>A new matrix.</returns>
    public Matrix this[Range rowRange, int column] => this[rowRange, column..(column + 1)];

    /// <summary>Gets a range of rows and columns as a new matrix.</summary>
    /// <param name="rowRange">Row range.</param>
    /// <param name="column">Column index.</param>
    /// <returns>A new matrix.</returns>
    public Matrix this[Range rowRange, Index column] =>
        this[rowRange, column.GetOffset(Cols)..(column.GetOffset(Cols) + 1)];

    /// <summary>Gets a range of rows and columns as a new matrix.</summary>
    /// <param name="row">Row index.</param>
    /// <param name="columnRange">Column range.</param>
    /// <returns>A new matrix.</returns>
    public Matrix this[int row, Range columnRange] => this[row..(row + 1), columnRange];

    /// <summary>Gets a range of rows and columns as a new matrix.</summary>
    /// <param name="row">Row index.</param>
    /// <param name="columnRange">Column range.</param>
    /// <returns>A new matrix.</returns>
    public Matrix this[Index row, Range columnRange] =>
        this[row.GetOffset(Rows)..(row.GetOffset(Rows) + 1), columnRange];

    /// <summary>Copies the content of this matrix into an existing one.</summary>
    /// <param name="dest">Destination matrix.</param>
    internal unsafe void CopyTo(Matrix dest)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(dest.IsInitialized);
        Contract.Requires(Rows == dest.Rows);
        Contract.Requires(Cols == dest.Cols);

        fixed (double* p = values, q = dest.values)
        {
            int size = values.Length * sizeof(double);
            Buffer.MemoryCopy(p, q, size, size);
        }
    }

    /// <summary>Gets a copy of a row as a vector.</summary>
    /// <param name="row">Row number, counting from 0.</param>
    /// <returns>A copy of the row.</returns>
    public Vector GetRow(int row)
    {
        double[] v = GC.AllocateUninitializedArray<double>(Cols);
        for (int c = 0; c < v.Length; c++)
            v[c] = values[row * Cols + c];
        return v;
    }

    /// <summary>Gets a copy of a row as a vector, using <see cref="Index"/>.</summary>
    /// <param name="row">Row number, counting from 0.</param>
    /// <returns>A copy of the row.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector GetRow(Index row) => GetRow(row.GetOffset(Rows));

    /// <summary>Gets a copy of a column as a vector.</summary>
    /// <param name="col">Column number, counting from 0.</param>
    /// <returns>A copy of the column.</returns>
    public Vector GetColumn(int col)
    {
        double[] v = GC.AllocateUninitializedArray<double>(Rows);
        for (int r = 0; r < v.Length; r++)
            v[r] = values[r * Cols + col];
        return v;
    }

    /// <summary>Gets a copy of a column as a vector, using <see cref="Index"/>.</summary>
    /// <param name="col">Column number, counting from 0.</param>
    /// <returns>A copy of the column.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector GetColumn(Index col) => GetColumn(col.GetOffset(Cols));

    /// <summary>Transposes the matrix.</summary>
    /// <returns>A new matrix with swapped rows and cells.</returns>
    public unsafe Matrix Transpose()
    {
        Contract.Requires(IsInitialized);

        int r = Rows, c = Cols;
        double[] result = new double[c * r];
        fixed (double* pA = values, pB = result)
            if (Avx.IsSupported)
            {
                // Blocks are multiple of four.
                int r1 = r & Simd.AVX_MASK;
                int c1 = c & Simd.AVX_MASK;
                int r2 = r + r, c2 = c + c;
                int r4 = r2 + r2, c4 = c2 + c2;
                double* p = pA;
                for (int row = 0; row < r1; row += 4)
                {
                    double* q = pB + row;
                    for (int col = 0; col < c1; col += 4)
                    {
                        double* pp = p + col;
                        var row1 = Avx.LoadVector256(pp);
                        var row2 = Avx.LoadVector256(pp + c);
                        var row3 = Avx.LoadVector256(pp + c2);
                        var row4 = Avx.LoadVector256(pp + c2 + c);
                        var t1 = Avx.Shuffle(row1, row2, 0b_0000);
                        var t2 = Avx.Shuffle(row1, row2, 0b_1111);
                        var t3 = Avx.Shuffle(row3, row4, 0b_0000);
                        var t4 = Avx.Shuffle(row3, row4, 0b_1111);
                        Avx.Store(q, Avx.Permute2x128(t1, t3, 0b_0010_0000));
                        Avx.Store(q + r, Avx.Permute2x128(t2, t4, 0b_0010_0000));
                        Avx.Store(q + r2, Avx.Permute2x128(t1, t3, 0b_0011_0001));
                        Avx.Store(q + r2 + r, Avx.Permute2x128(t2, t4, 0b_0011_0001));
                        q += r4;
                    }
                    p += c4;
                }
                for (int row = r1; row < r; row++)
                {
                    double* src = pA + row * c;
                    for (int col = 0; col < c; col++)
                        pB[col * r + row] = src[col];
                }
                for (int col = c1; col < c; col++)
                {
                    double* dst = pB + col * r;
                    for (int row = 0; row < r1; row++)
                        dst[row] = pA[row * c + col];
                }
            }
            else
            {
                double* pArow = pA;
                double* pBrow = pB;
                int r2 = r + r;
                int r4 = r2 + r2;
                for (int row = 0; row < r; row++)
                {
                    int idx = 0, col = 0;
                    for (int top = c & Simd.AVX_MASK; col < top; col += 4)
                    {
                        pBrow[idx] = pArow[col];
                        pBrow[idx + r] = pArow[col + 1];
                        pBrow[idx + r2] = pArow[col + 2];
                        pBrow[idx + r2 + r] = pArow[col + 3];
                        idx += r4;
                    }
                    for (; col < c; col++)
                    {
                        pBrow[idx] = pArow[col];
                        idx += r;
                    }
                    pArow += c;
                    pBrow++;
                }
            }
        return new(Cols, Rows, result);
    }

    /// <summary>Adds a full matrix to a lower triangular matrix.</summary>
    /// <param name="m1">Full matrix operand.</param>
    /// <param name="lm2">Lower-triangular matrix operand.</param>
    /// <returns>The sum of the two matrices.</returns>
    public static Matrix operator +(Matrix m1, LMatrix lm2) => m1 + (Matrix)lm2;

    /// <summary>Adds a full matrix to an upper triangular matrix.</summary>
    /// <param name="m1">Full matrix operand.</param>
    /// <param name="rm2">Upper-triangular matrix operand.</param>
    /// <returns>The sum of the two matrices.</returns>
    public static Matrix operator +(Matrix m1, RMatrix rm2) => m1 + (Matrix)rm2;

    /// <summary>Adds a lower triangular matrix to a full matrix.</summary>
    /// <param name="lm1">Lower-triangular matrix operand.</param>
    /// <param name="m2">Full matrix operand.</param>
    /// <returns>The sum of the two matrices.</returns>
    public static Matrix operator +(LMatrix lm1, Matrix m2) => (Matrix)lm1 + m2;

    /// <summary>Adds an upper triangular matrix to a full matrix.</summary>
    /// <param name="rm1">Upper-triangular matrix operand.</param>
    /// <param name="m2">Full matrix operand.</param>
    /// <returns>The sum of the two matrices.</returns>
    public static Matrix operator +(RMatrix rm1, Matrix m2) => (Matrix)rm1 + m2;

    /// <summary>Subtracts a lower-triangular matrix from a full matrix.</summary>
    /// <param name="m1">Full matrix minuend.</param>
    /// <param name="lm2">Lower-triangular matrix subtrahend.</param>
    /// <returns>The difference of the two matrices.</returns>
    public static Matrix operator -(Matrix m1, LMatrix lm2) => m1 - (Matrix)lm2;

    /// <summary>Subtracts an upper-triangular matrix from a full matrix.</summary>
    /// <param name="m1">Full matrix minuend.</param>
    /// <param name="rm2">Upper-triangular matrix subtrahend.</param>
    /// <returns>The difference of the two matrices.</returns>
    public static Matrix operator -(Matrix m1, RMatrix rm2) => m1 - (Matrix)rm2;

    /// <summary>Subtracts a full matrix from a lower-triangular matrix.</summary>
    /// <param name="lm1">Lower-triangular matrix minuend.</param>
    /// <param name="m2">Full matrix subtrahend.</param>
    /// <returns>The difference of the two matrices.</returns>
    public static Matrix operator -(LMatrix lm1, Matrix m2) => (Matrix)lm1 - m2;

    /// <summary>Subtracts a full matrix from an upper-triangular matrix.</summary>
    /// <param name="rm1">Upper-triangular matrix minuend.</param>
    /// <param name="m2">Full matrix subtrahend.</param>
    /// <returns>The difference of the two matrices.</returns>
    public static Matrix operator -(RMatrix rm1, Matrix m2) => (Matrix)rm1 - m2;

    /// <summary>Sums two matrices with the same size.</summary>
    /// <param name="m1">First matrix operand.</param>
    /// <param name="m2">Second matrix operand.</param>
    /// <returns>The sum of the two operands.</returns>
    /// <exception cref="MatrixSizeException">When matrices have non-matching sizes.</exception>
    public static Matrix operator +(Matrix m1, Matrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        if (m1.Rows != m2.Rows ||
            m1.Cols != m2.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m1.Cols);

        double[] result = GC.AllocateUninitializedArray<double>(m1.Rows * m1.Cols);
        ref double a = ref MemoryMarshal.GetArrayDataReference(m1.values);
        ref double b = ref MemoryMarshal.GetArrayDataReference(m2.values);
        ref double c = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref a, i)) + Vector256.LoadUnsafe(ref Add(ref b, i)),
                    ref Add(ref c, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref a, t)) + Vector256.LoadUnsafe(ref Add(ref b, t)),
                ref Add(ref c, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref c, i) = Add(ref a, i) + Add(ref b, i);
        return new(m1.Rows, m1.Cols, result);
    }

    /// <summary>Subtracts two matrices with the same size.</summary>
    /// <param name="m1">First matrix operand.</param>
    /// <param name="m2">Second matrix operand.</param>
    /// <returns>The subtraction of the two operands.</returns>
    /// <exception cref="MatrixSizeException">When matrices have non-matching sizes.</exception>
    public static Matrix operator -(Matrix m1, Matrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        if (m1.Rows != m2.Rows ||
            m1.Cols != m2.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m1.Cols);

        double[] result = GC.AllocateUninitializedArray<double>(m1.Rows * m1.Cols);
        ref double a = ref MemoryMarshal.GetArrayDataReference(m1.values);
        ref double b = ref MemoryMarshal.GetArrayDataReference(m2.values);
        ref double c = ref MemoryMarshal.GetArrayDataReference(result);
        if (Vector256.IsHardwareAccelerated)
        {
            int t = result.Length - Vector256<double>.Count;
            for (int i = 0; i < t; i += Vector256<double>.Count)
                Vector256.StoreUnsafe(
                    Vector256.LoadUnsafe(ref Add(ref a, i)) - Vector256.LoadUnsafe(ref Add(ref b, i)),
                    ref Add(ref c, i));
            Vector256.StoreUnsafe(
                Vector256.LoadUnsafe(ref Add(ref a, t)) - Vector256.LoadUnsafe(ref Add(ref b, t)),
                ref Add(ref c, t));
        }
        else
            for (int i = 0; i < result.Length; i++)
                Add(ref c, i) = Add(ref a, i) - Add(ref b, i);
        return new(m1.Rows, m1.Cols, result);
    }

    /// <summary>Negates a matrix.</summary>
    /// <param name="m">The matrix operand.</param>
    /// <returns>Cell-by-cell negation.</returns>
    public static unsafe Matrix operator -(Matrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);

        double[] result = new double[m.values.Length];
        fixed (double* pA = m.values, pC = result)
        {
            int len = m.values.Length, i = 0;
            if (Avx.IsSupported)
            {
                Vector256<double> z = Vector256<double>.Zero;
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(pC + i, Avx.Subtract(z, Avx.LoadVector256(pA + i)));
            }
            for (; i < len; i++)
                pC[i] = -pA[i];
        }
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Adds a scalar to a matrix.</summary>
    /// <param name="m">The matrix.</param>
    /// <param name="d">A scalar summand.</param>
    /// <returns>The pointwise addition of the scalar.</returns>
    public static unsafe Matrix operator +(Matrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);

        double[] result = new double[m.values.Length];
        fixed (double* pA = m.values, pC = result)
        {
            int len = m.values.Length, i = 0;
            if (Avx.IsSupported)
            {
                Vector256<double> v = Vector256.Create(d);
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(pC + i, Avx.Add(Avx.LoadVector256(pA + i), v));
            }
            for (; i < len; i++)
                pC[i] = pA[i] + d;
        }
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Adds a scalar to a matrix.</summary>
    /// <param name="d">A scalar summand.</param>
    /// <param name="m">The matrix.</param>
    /// <returns>The pointwise addition of the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator +(double d, Matrix m) => m + d;

    /// <summary>Subtracts a scalar from a matrix.</summary>
    /// <param name="m">The matrix.</param>
    /// <param name="d">The scalar.</param>
    /// <returns>The pointwise subtraction of the scalar.</returns>
    public static unsafe Matrix operator -(Matrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);

        double[] result = new double[m.values.Length];
        fixed (double* pA = m.values, pC = result)
        {
            int len = m.values.Length, i = 0;
            if (Avx.IsSupported)
            {
                Vector256<double> v = Vector256.Create(d);
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(pC + i, Avx.Subtract(Avx.LoadVector256(pA + i), v));
            }
            for (; i < len; i++)
                pC[i] = pA[i] - d;
        }
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Subtracts a scalar from a matrix.</summary>
    /// <param name="d">The scalar.</param>
    /// <param name="m">The matrix.</param>
    /// <returns>The pointwise subtraction of the scalar.</returns>
    public static unsafe Matrix operator -(double d, Matrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);

        double[] result = new double[m.values.Length];
        fixed (double* pA = m.values, pC = result)
        {
            int len = m.values.Length, i = 0;
            if (Avx.IsSupported)
            {
                Vector256<double> v = Vector256.Create(d);
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(pC + i, Avx.Subtract(v, Avx.LoadVector256(pA + i)));
            }
            for (; i < len; i++)
                pC[i] = d - pA[i];
        }
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Cell by cell product with a second matrix.</summary>
    /// <param name="m">Second operand.</param>
    /// <returns>The pointwise product.</returns>
    public unsafe Matrix PointwiseMultiply(Matrix m)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(m.IsInitialized);
        if (Rows != m.Rows ||
            Cols != m.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == Cols);

        double[] result = new double[values.Length];
        fixed (double* pA = values, pB = m.values, pC = result)
        {
            int len = values.Length, i = 0;
            if (Avx.IsSupported)
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(pC + i,
                        Avx.Multiply(Avx.LoadVector256(pA + i), Avx.LoadVector256(pB + i)));
            for (; i < len; i++)
                pC[i] = pA[i] * pB[i];
        }
        return new(Rows, Cols, result);
    }

    /// <summary>Cell by cell division with a second matrix.</summary>
    /// <param name="m">Second operand.</param>
    /// <returns>The pointwise quotient.</returns>
    public unsafe Matrix PointwiseDivide(Matrix m)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(m.IsInitialized);
        if (Rows != m.Rows ||
            Cols != m.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == Cols);

        double[] result = new double[values.Length];
        fixed (double* pA = values, pB = m.values, pC = result)
        {
            int len = values.Length, i = 0;
            if (Avx.IsSupported)
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(pC + i,
                        Avx.Divide(Avx.LoadVector256(pA + i), Avx.LoadVector256(pB + i)));
            for (; i < len; i++)
                pC[i] = pA[i] / pB[i];
        }
        return new(Rows, Cols, result);
    }

    /// <summary>Multiplies a matrix by a scalar value.</summary>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <param name="d">A scalar multiplicand.</param>
    /// <returns>The multiplication of the matrix by the scalar.</returns>
    public static unsafe Matrix operator *(Matrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);

        double[] result = new double[m.values.Length];
        fixed (double* pA = m.values, pC = result)
        {
            int len = result.Length, i = 0;
            if (Avx.IsSupported)
            {
                Vector256<double> v = Vector256.Create(d);
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(pC + i, Avx.Multiply(Avx.LoadVector256(pA + i), v));
            }
            for (; i < len; i++)
                pC[i] = pA[i] * d;
        }
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Multiplies a matrix by a scalar value.</summary>
    /// <param name="d">A scalar multiplicand.</param>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <returns>The multiplication of the matrix by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator *(double d, Matrix m) => m * d;

    /// <summary>Divides a matrix by a scalar value.</summary>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <param name="d">A scalar multiplicand.</param>
    /// <returns>The quotient of the matrix by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator /(Matrix m, double d) => m * (1.0 / d);

    /// <summary>Multiplies two compatible matries.</summary>
    /// <param name="m1">First matrix operand.</param>
    /// <param name="m2">Second matrix operand.</param>
    /// <returns>The algebraic multiplication of the two operands.</returns>
    public static unsafe Matrix operator *(Matrix m1, Matrix m2)
    {
        Contract.Requires(m1.IsInitialized);
        Contract.Requires(m2.IsInitialized);
        if (m1.Cols != m2.Rows)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m2.Cols);

        const long MINSIZE = 64L * 64L * 64L * 64L;
        const long MAXSIZE = 1024L * 1024L * 1024L * 1024L;
        long size = (long)m1.values.Length * m2.values.Length;
        int m = m1.Rows;
        int n = m1.Cols;
        int p = m2.Cols;
        double[] result = new double[m * p];
        fixed (double* a = m1.values, b = m2.values, c = result)
            if (size < MINSIZE)
                NonBlocking(m, n, p, a, b, c);
            else if (size < MAXSIZE)
                Blocking128(m, n, p, a, b, c);
            else
                Blocking256(m, n, p, a, b, c);
        return new(m, p, result);

        static void NonBlocking(int m, int n, int p, double* a, double* b, double* c)
        {
            double* pa = a;
            double* pc = c;
            int top = p & Simd.AVX_MASK;
            for (int i = 0; i < m; i++)
            {
                double* pb = b;
                for (int k = 0; k < n; k++)
                {
                    double d = pa[k];
                    int j = 0;
                    if (Avx.IsSupported)
                        for (Vector256<double> vd = Vector256.Create(d); j < top; j += 4)
                            Avx.Store(pc + j,
                                Avx.LoadVector256(pc + j).MultiplyAdd(pb + j, vd));
                    for (; j < p; j++)
                        pc[j] = FusedMultiplyAdd(pb[j], d, pc[j]);
                    pb += p;
                }
                pa += n;
                pc += p;
            }
        }

        static void Blocking128(int m, int n, int p, double* a, double* b, double* c)
        {
            const int BLK_SIZE = 128;
            int pbl = p * BLK_SIZE;
            for (int ii = 0; ii < m; ii += BLK_SIZE)
                for (int kk = 0, pkk = 0; kk < n; kk += BLK_SIZE, pkk += pbl)
                    for (int jj = 0; jj < p; jj += BLK_SIZE)
                    {
                        double* pa = a + n * ii;
                        double* pc = c + p * ii;
                        int topi = Min(m, ii + BLK_SIZE);
                        int topj = Min(p, jj + BLK_SIZE);
                        int top = ((topj - jj) & ~15) + jj;
                        for (int i = ii; i < topi; i++)
                        {
                            double* pb = b + pkk;
                            int topk = Min(n, kk + BLK_SIZE);
                            for (int k = kk; k < topk; k++)
                            {
                                double d = pa[k];
                                int j = jj;
                                if (Avx.IsSupported)
                                    for (var vd = Vector256.Create(d); j < top; j += 16)
                                    {
                                        Avx.Store(pc + j, Avx.LoadVector256(pc + j)
                                            .MultiplyAdd(pb + j, vd));
                                        Avx.Store(pc + j + 4, Avx.LoadVector256(pc + j + 4)
                                            .MultiplyAdd(pb + j + 4, vd));
                                        Avx.Store(pc + j + 8, Avx.LoadVector256(pc + j + 8)
                                            .MultiplyAdd(pb + j + 8, vd));
                                        Avx.Store(pc + j + 12, Avx.LoadVector256(pc + j + 12)
                                            .MultiplyAdd(pb + j + 12, vd));
                                    }
                                for (; j < topj; j++)
                                    pc[j] = FusedMultiplyAdd(d, pb[j], pc[j]);
                                pb += p;
                            }
                            pa += n;
                            pc += p;
                        }
                    }
        }

        static void Blocking256(int m, int n, int p, double* a, double* b, double* c)
        {
            const int BLK_SIZE = 256;
            int pbl = p * BLK_SIZE;
            for (int ii = 0; ii < m; ii += BLK_SIZE)
                for (int kk = 0, pkk = 0; kk < n; kk += BLK_SIZE, pkk += pbl)
                    for (int jj = 0; jj < p; jj += BLK_SIZE)
                    {
                        double* pa = a + n * ii;
                        double* pc = c + p * ii;
                        int topi = Min(m, ii + BLK_SIZE);
                        int topj = Min(p, jj + BLK_SIZE);
                        int top = ((topj - jj) & ~15) + jj;
                        for (int i = ii; i < topi; i++)
                        {
                            double* pb = b + pkk;
                            int topk = Min(n, kk + BLK_SIZE);
                            for (int k = kk; k < topk; k++)
                            {
                                double d = pa[k];
                                int j = jj;
                                if (Avx.IsSupported)
                                    for (var vd = Vector256.Create(d); j < top; j += 16)
                                    {
                                        Avx.Store(pc + j, Avx.LoadVector256(pc + j)
                                            .MultiplyAdd(pb + j, vd));
                                        Avx.Store(pc + j + 4, Avx.LoadVector256(pc + j + 4)
                                            .MultiplyAdd(pb + j + 4, vd));
                                        Avx.Store(pc + j + 8, Avx.LoadVector256(pc + j + 8)
                                            .MultiplyAdd(pb + j + 8, vd));
                                        Avx.Store(pc + j + 12, Avx.LoadVector256(pc + j + 12)
                                            .MultiplyAdd(pb + j + 12, vd));
                                    }
                                for (; j < topj; j++)
                                    pc[j] = FusedMultiplyAdd(d, pb[j], pc[j]);
                                pb += p;
                            }
                            pa += n;
                            pc += p;
                        }
                    }
        }
    }

    /// <summary>Multiplies this matrix by itself.</summary>
    /// <returns>The square of the matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Square() => this * this;

    /// <summary>Multiplies this matrix by the transposed argument.</summary>
    /// <remarks>Calculates <c>this * m'</c>.</remarks>
    /// <param name="m">Second operand.</param>
    /// <returns>The multiplication by the transposed argument.</returns>
    public unsafe Matrix MultiplyTranspose(Matrix m)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(m.IsInitialized);
        if (Cols != m.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Rows);

        int r = Rows, n = Cols, c = m.Rows;
        double[] result = new double[r * c];
        fixed (double* pA = values, pB = m.values, pC = result)
        {
            int top = n & Simd.AVX_MASK;
            double* pAi = pA, pCi = pC;
            for (int i = 0; i < r; i++)
            {
                double* pBj = pB;
                for (int j = 0; j < c; j++)
                {
                    int k = 0;
                    double acc = 0;
                    if (Avx.IsSupported)
                    {
                        Vector256<double> sum = Vector256<double>.Zero;
                        for (; k < top; k += 4)
                            sum = sum.MultiplyAdd(pAi + k, pBj + k);
                        acc = sum.Sum();
                    }
                    for (; k < n; k++)
                        acc = FusedMultiplyAdd(pAi[k], pBj[k], acc);
                    pCi[j] = acc;
                    pBj += n;
                }
                pAi += n;
                pCi += c;
            }
        }
        return new(r, c, result);
    }

    /// <summary>Transform a vector using a matrix.</summary>
    /// <param name="m">The transformation matrix.</param>
    /// <param name="v">Vector to transform.</param>
    /// <returns>The transformed vector.</returns>
    /// <exception cref="MatrixSizeException">
    /// When the matrix and vector have non-matching sizes.
    /// </exception>
    public static Vector operator *(Matrix m, Vector v)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Requires(v.IsInitialized);
        int r = m.Rows, c = m.Cols, top = c & Simd.AVX_MASK;
        if (c != v.Length)
            throw new MatrixSizeException();

        double[] result = GC.AllocateUninitializedArray<double>(r);
        ref double a = ref MemoryMarshal.GetArrayDataReference(m.values);
        ref double x = ref MemoryMarshal.GetArrayDataReference((double[])v);
        ref double b = ref MemoryMarshal.GetArrayDataReference(result);
        for (int i = 0; i < r; i++, a = ref Add(ref a, c), b = ref Add(ref b, 1))
        {
            int j = 0;
            double d = 0;
            if (Vector256.IsHardwareAccelerated)
            {
                Vector256<double> vec = Vector256<double>.Zero;
                for (; j < top; j += Vector256<double>.Count)
                    vec = vec.MultiplyAdd(Vector256.LoadUnsafe(ref Add(ref a, j)),
                        Vector256.LoadUnsafe(ref Add(ref x, j)));
                d = vec.Sum();
            }
            for (; j < c; j++)
                d = FusedMultiplyAdd(Add(ref a, j), Add(ref x, j), d);
            b = d;
        }
        return result;
    }

    /// <summary>Transform a vector using the transposed matrix.</summary>
    /// <remarks>
    /// This operator is equivalent to the method <see cref="TransposeMultiply(Vector)"/>.
    /// </remarks>
    /// <param name="v">Vector to transform.</param>
    /// <param name="m">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator *(Vector v, Matrix m) =>
        m.TransposeMultiply(v);

    /// <summary>Transforms a vector using the transpose of this matrix.</summary>
    /// <remarks>
    /// This method calcutes <c>this' * v</c>, or, equivalently, <c>v * this</c>.
    /// </remarks>
    /// <param name="v">The vector to transform.</param>
    /// <returns>The transformed vector.</returns>
    public unsafe Vector TransposeMultiply(Vector v)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        if (Rows != v.Length)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Vector>().Length == Cols);

        int r = Rows, c = Cols;
        double[] result = new double[r];
        fixed (double* pA = values, pB = (double[])v, pC = result)
        {
            double* pAk = pA;
            for (int k = 0; k < r; k++)
            {
                double d = pB[k];
                int j = 0;
                if (Avx.IsSupported)
                {
                    Vector256<double> vec = Vector256.Create(d);
                    for (int last = c & Simd.AVX_MASK; j < last; j += 4)
                        Avx.Store(pC + j, Avx.LoadVector256(pC + j).MultiplyAdd(pAk + j, vec));
                }
                for (; j < c; j++)
                    pC[j] += pAk[j] * d;
                pAk += c;
            }
        }
        return result;
    }

    /// <summary>Transforms a vector and adds an offset.</summary>
    /// <param name="v">Vector to transform.</param>
    /// <param name="add">Vector to add.</param>
    /// <returns><c>this * v + add</c>.</returns>
    public unsafe Vector MultiplyAdd(Vector v, Vector add)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        int r = Rows, c = Cols, top = c & Simd.AVX_MASK;
        if (c != v.Length)
            throw new MatrixSizeException();

        double[] result = GC.AllocateUninitializedArray<double>(r);
        fixed (double* pA = values, pX = (double[])v, pB = result, pC = (double[])add)
        {
            double* pA1 = pA, pB1 = pB, pC1 = pC;
            for (int i = 0; i < r; i++, pA1 += c)
            {
                int j = 0;
                double d = 0;
                if (Avx.IsSupported)
                {
                    Vector256<double> vec = Vector256<double>.Zero;
                    for (; j < top; j += 4)
                        vec = vec.MultiplyAdd(pA1 + j, pX + j);
                    d = vec.Sum();
                }
                for (; j < c; j++)
                    d = FusedMultiplyAdd(pA1[j], pX[j], d);
                *pB1++ = d + *pC1++;
            }
        }
        return result;
    }

    /// <summary>Transforms a vector and subtracts an offset.</summary>
    /// <param name="v">Vector to transform.</param>
    /// <param name="sub">Vector to subtract.</param>
    /// <returns><c>this * multiplicand - sub</c>.</returns>
    public unsafe Vector MultiplySubtract(Vector v, Vector sub)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        int r = Rows, c = Cols, top = c & Simd.AVX_MASK;
        if (c != v.Length)
            throw new MatrixSizeException();

        double[] result = GC.AllocateUninitializedArray<double>(r);
        fixed (double* pA = values, pX = (double[])v, pB = result, pC = (double[])sub)
        {
            double* pA1 = pA, pB1 = pB, pC1 = pC;
            for (int i = 0; i < r; i++, pA1 += c)
            {
                int j = 0;
                double d = 0;
                if (Avx.IsSupported)
                {
                    Vector256<double> vec = Vector256<double>.Zero;
                    for (; j < top; j += 4)
                        vec = vec.MultiplyAdd(pA1 + j, pX + j);
                    d = vec.Sum();
                }
                for (; j < c; j++)
                    d = FusedMultiplyAdd(pA1[j], pX[j], d);
                *pB1++ = d - *pC1++;
            }
        }
        return result;
    }

    /// <summary>Computes the Cholesky decomposition of this matrix.</summary>
    /// <returns>A Cholesky decomposition.</returns>
    public Cholesky Cholesky() =>
        TryCholesky(out var result)
            ? result
            : throw new NonPositiveDefiniteException();

    /// <summary>Computes the Cholesky decomposition of this matrix.</summary>
    /// <returns>A lower triangular matrix.</returns>
    public LMatrix CholeskyMatrix() =>
        TryCholesky(out var result)
            ? result.L
            : throw new NonPositiveDefiniteException();

    /// <summary>Tentative Cholesky decomposition.</summary>
    /// <param name="cholesky">Contains a full or partial decomposition.</param>
    /// <returns><see langword="true"/> when successful.</returns>
    public bool TryCholesky(out Cholesky cholesky)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(IsSquare);
        Contract.Requires(Rows > 0);

        return Library.Cholesky.TryDecompose(this, out cholesky);
    }

    /// <summary>Performs the LUP decomposition of this matrix.</summary>
    /// <returns>
    /// An upper-triangular and a lower-triangular matrix, packed together
    /// with an integer array with permutations.
    /// </returns>
    public LU LU()
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(IsSquare);

        return new(this);
    }

    /// <summary>Gets the determinant of the matrix.</summary>
    /// <returns>The determinant of the LU decomposition.</returns>
    public double Determinant() => LU().Determinant();

    /// <summary>Calculates the inverse of the matrix.</summary>
    /// <returns>The inverse matrix using LU factorization.</returns>
    public Matrix Inverse() => LU().Solve(Identity(Rows));

    /// <summary>Solves the equation Ax = b for x.</summary>
    /// <param name="v">The right side of the equation.</param>
    /// <returns>The solving vector.</returns>
    public Vector Solve(Vector v) => LU().Solve(v);

    /// <summary>Solves the equation AX = B for the matrix X.</summary>
    /// <param name="m">The right side of the equation.</param>
    /// <returns>The solving matrix.</returns>
    public Matrix Solve(Matrix m) => LU().Solve(m);

    /// <summary>Computes the eigenvalue decomposition.</summary>
    /// <remarks>Use this method when the symmetricity is unknown.</remarks>
    /// <returns>Eigenvectors and eigenvalues.</returns>
    public EVD EVD() => new(this, IsSymmetric());

    /// <summary>Computes the eigenvalue decomposition.</summary>
    /// <param name="isSymmetric">Is this a symmetric real matrix?</param>
    /// <returns>Eigenvectors and eigenvalues.</returns>
    public EVD EVD(bool? isSymmetric = null) => new(this, isSymmetric ?? IsSymmetric());

    /// <summary>Computes the eigenvalue decomposition for a symmetric matrix.</summary>
    /// <returns>Eigenvectors and eigenvalues.</returns>
    public EVD SymEVD() => new(this, true);

    /// <summary>Computes the maximum difference between cells.</summary>
    /// <param name="m">The reference matrix.</param>
    /// <returns>The max-norm of the matrix difference.</returns>
    public unsafe double Distance(Matrix m)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(m.IsInitialized);

        fixed (double* p = values, q = m.values)
            return CommonMatrix.Distance(
                p, q, Min(values.Length, m.values.Length));
    }

    /// <summary>Gets the cell with the maximum absolute value.</summary>
    /// <returns>The max-norm of the matrix.</returns>
    public unsafe double AMax()
    {
        Contract.Requires(IsInitialized);

        fixed (double* p = values)
            return CommonMatrix.AbsoluteMaximum(p, values.Length);
    }

    /// <summary>Gets the cell with the minimum absolute value.</summary>
    /// <returns>The minimum absolute value in the matrix.</returns>
    public unsafe double AMin()
    {
        Contract.Requires(IsInitialized);

        fixed (double* p = values)
            return CommonMatrix.AbsoluteMinimum(p, values.Length);
    }

    /// <summary>Gets the cell with the maximum value.</summary>
    /// <returns>The cell with the maximum value.</returns>
    public unsafe double Maximum()
    {
        Contract.Requires(IsInitialized);

        fixed (double* p = values)
            return CommonMatrix.Maximum(p, values.Length);
    }

    /// <summary>Gets the cell with the minimum value.</summary>
    /// <returns>The cell with the minimum value.</returns>
    public unsafe double Minimum()
    {
        Contract.Requires(IsInitialized);

        fixed (double* p = values)
            return CommonMatrix.Minimum(p, values.Length);
    }

    /// <summary>Applies a function to each cell of the matrix.</summary>
    /// <param name="mapper">The transformation function.</param>
    /// <returns>A new matrix with transformed cells.</returns>
    public unsafe Matrix Map(Func<double, double> mapper)
    {
        double[] newValues = new double[values.Length];
        fixed (double* pA = values, pB = newValues)
        {
            int len = newValues.Length;
            for (int i = 0; i < len; i++)
                pB[i] = mapper(pA[i]);
        }
        return new(Rows, Cols, newValues);
    }

    /// <summary>Checks whether the predicate is satified by all cells.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if all cells satisfy the predicate.</returns>
    public bool All(Func<double, bool> predicate)
    {
        foreach (double value in values)
            if (!predicate(value))
                return false;
        return true;
    }

    /// <summary>Checks whether the predicate is satified by at least one cell.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if there exists a cell satisfying the predicate.</returns>
    public bool Any(Func<double, bool> predicate)
    {
        foreach (double value in values)
            if (predicate(value))
                return true;
        return false;
    }

    /// <summary>Checks if the provided argument is a matrix with the same values.</summary>
    /// <param name="other">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a matrix with the same values.</returns>
    public unsafe bool Equals(Matrix other)
    {
        if (other.Rows != Rows || other.Cols != Cols)
            return false;
        fixed (double* p = values, q = other.values)
        {
            int i = 0, size = values.Length;
            if (Avx.IsSupported)
                for (int top = size & Simd.AVX_MASK; i < top; i += 4)
                    if (Avx.MoveMask(Avx.CompareEqual(
                        Avx.LoadVector256(p + i), Avx.LoadVector256(q + i))) != 0xF)
                        return false;
            for (; i < size; i++)
                if (p[i] != q[i])
                    return false;
        }
        return true;
    }

    /// <summary>Checks if the provided argument is a matrix with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a matrix with the same values.</returns>
    public override bool Equals(object? obj) =>
        obj is Matrix matrix && Equals(matrix);

    /// <summary>Returns the hashcode for this matrix.</summary>
    /// <returns>A hashcode summarizing the content of the matrix.</returns>
    public override int GetHashCode() =>
        ((IStructuralEquatable)values).GetHashCode(EqualityComparer<double>.Default);

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First matrix to compare.</param>
    /// <param name="right">Second matrix to compare.</param>
    /// <returns><see langword="true"/> when all corresponding cells are equals.</returns>
    public static bool operator ==(Matrix left, Matrix right) => left.Equals(right);

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First rectangular matrix to compare.</param>
    /// <param name="right">Second lower-triangular matrix to compare.</param>
    /// <returns><see langword="true"/> when all corresponding cells are equals.</returns>
    public static bool operator ==(Matrix left, LMatrix right) => left.Equals((Matrix)right);

    /// <summary>Checks two matrices for equality.</summary>
    /// <param name="left">First rectangular matrix to compare.</param>
    /// <param name="right">Second upper-triangular matrix to compare.</param>
    /// <returns><see langword="true"/> when all corresponding cells are equals.</returns>
    public static bool operator ==(Matrix left, RMatrix right) => left.Equals((Matrix)right);

    /// <summary>Checks two matrices for inequality.</summary>
    /// <param name="left">First rectangular matrix to compare.</param>
    /// <param name="right">Second rectangular matrix to compare.</param>
    /// <returns><see langword="true"/> if there are cells with different values.</returns>
    public static bool operator !=(Matrix left, Matrix right) => !(left == right);

    /// <summary>Checks two matrices for inequality.</summary>
    /// <param name="left">First rectangular matrix to compare.</param>
    /// <param name="right">Second low-triangular matrix to compare.</param>
    /// <returns><see langword="true"/> if there are cells with different values.</returns>
    public static bool operator !=(Matrix left, LMatrix right) => !(left == right);

    /// <summary>Checks two matrices for inequality.</summary>
    /// <param name="left">First rectangular matrix to compare.</param>
    /// <param name="right">Second upper-triangular matrix to compare.</param>
    /// <returns><see langword="true"/> if there are cells with different values.</returns>
    public static bool operator !=(Matrix left, RMatrix right) => !(left == right);

    /// <summary>Gets a textual representation of this matrix.</summary>
    /// <returns>One line for each row, with space separated columns.</returns>
    public override string ToString() =>
        $"ans ∊ ℝ({Rows}⨯{Cols})" + Environment.NewLine +
        CommonMatrix.ToString(Rows, Cols, values, v => v.ToString("G6"), 0);

    /// <summary>Gets a textual representation of this matrix.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>One line for each row, with space separated columns.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        $"ans ∊ ℝ({Rows}⨯{Cols})" + Environment.NewLine +
        CommonMatrix.ToString(Rows, Cols, values, v => v.ToString(format, provider), 0);
}

/// <summary>JSON converter for rectangular matrices.</summary>
public class MatrixJsonConverter : JsonConverter<Matrix>
{
    /// <summary>Reads and convert JSON to a <see cref="LMatrix"/> instance.</summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type of the object to convert.</param>
    /// <param name="options">JSON options.</param>
    /// <returns>A triangular matrix with the values read from JSON.</returns>
    public unsafe override Matrix Read(
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
        return new(rows, cols, values!);
    }

    /// <summary>Converts a rectangular matrix to JSON.</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The matrix to serialize.</param>
    /// <param name="options">JSON options.</param>    
    public unsafe override void Write(
        Utf8JsonWriter writer,
        Matrix value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(nameof(Matrix.Rows), value.Rows);
        writer.WriteNumber(nameof(Matrix.Cols), value.Cols);
        writer.WriteStartArray("values");
        fixed (double* pV = (double[])value)
        {
            double* pEnd = pV + value.Rows * value.Cols;
            for (double* p = pV; p < pEnd; p++)
                writer.WriteNumberValue(*p);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
