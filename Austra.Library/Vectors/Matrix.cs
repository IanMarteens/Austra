namespace Austra.Library;

/// <summary>Represents a dense rectangular matrix.</summary>
/// <remarks>
/// <para>Values are stored in a one-dimensional array, in row-major order.
/// Compatibility with bidimensional arrays, however, has been preserved.</para>
/// <para>
/// Most methods respect immutability at the cost of extra allocations.
/// Methods like <see cref="MultiplyAdd(DVector, double, DVector)"/>
/// save unneeded allocations. Most methods are hardware accelerated,
/// either by using managed references, SIMD operations or both.
/// Memory pinning has also been reduced to the minimum.
/// </para>
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
    IMultiplyOperators<Matrix, DVector, DVector>,
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
    public Matrix(int rows, int cols, Func<int, int, double> f)
    {
        (Rows, Cols, values) = (rows, cols, GC.AllocateUninitializedArray<double>(rows * cols));
        ref double p = ref MM.GetArrayDataReference(values);
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++, p = ref Add(ref p, 1))
                p = f(i, j);
    }

    /// <summary>Creates a square matrix using a formula to fill its cells.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <param name="f">A function defining cell content.</param>
    public Matrix(int size, Func<int, int, double> f) : this(size, size, f) { }

    /// <summary>Creates a matrix given its rows.</summary>
    /// <param name="rows">The array of rows.</param>
    /// <exception cref="MatrixSizeException">
    /// When there are not enough rows or there is a column size mismatch.
    /// </exception>
    public Matrix(params DVector[] rows)
    {
        if (rows == null || rows.Length == 0)
            throw new MatrixSizeException();
        Rows = rows.Length;
        Cols = rows[0].Length;
        if (Cols == 0)
            throw new MatrixSizeException();
        values = GC.AllocateUninitializedArray<double>(Rows * Cols);
        int offset = 0;
        foreach (DVector row in rows)
        {
            if (row.Length != Cols)
                throw new MatrixSizeException();
            Array.Copy((double[])row, 0, values, offset, Cols);
            offset += Cols;
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
    public Matrix(DVector diagonal) =>
        (Rows, Cols, values) = (diagonal.Length, diagonal.Length, diagonal.CreateDiagonal());

    /// <summary>Creates a diagonal matrix given its diagonal.</summary>
    /// <param name="diagonal">Values in the diagonal.</param>
    public Matrix(params double[] diagonal) =>
        (Rows, Cols, values) = (diagonal.Length, diagonal.Length, CommonMatrix.CreateDiagonal(diagonal));

    /// <summary>Creates a matrix filled with a uniform distribution generator.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random number generator.</param>
    /// <param name="offset">An offset for the random numbers.</param>
    /// <param name="width">Width for the uniform distribution.</param>
    public Matrix(int rows, int cols, Random random, double offset, double width)
    {
        (Rows, Cols, values) = (rows, cols, GC.AllocateUninitializedArray<double>(rows * cols));
        values.AsSpan().CreateRandom(random, offset, width);
    }

    /// <summary>Creates a square matrix filled with a uniform distribution generator.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <param name="random">A random number generator.</param>
    /// <param name="offset">An offset for the random numbers.</param>
    /// <param name="width">Width for the uniform distribution.</param>
    public Matrix(int size, Random random, double offset, double width) :
        this(size, size, random, offset, width)
    { }

    /// <summary>Creates a matrix filled with a uniform distribution generator.</summary>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="random">A random number generator.</param>
    public Matrix(int rows, int cols, Random random)
    {
        (Rows, Cols, values) = (rows, cols, GC.AllocateUninitializedArray<double>(rows * cols));
        values.AsSpan().CreateRandom(random);
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
        int len = rows * cols;
        (Rows, Cols, values) = (rows, cols, GC.AllocateUninitializedArray<double>(len));
        values.AsSpan().CreateRandom(random);
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
    public static Matrix FromColumns(params DVector[] columns) =>
        new Matrix(columns).Transpose();

    /// <summary>Horizontal concatenation of two matrices.</summary>
    /// <param name="left">Left matrix.</param>
    /// <param name="right">Right matrix.</param>
    /// <returns>A new matrix with more columns.</returns>
    public static Matrix HCat(Matrix left, Matrix right)
    {
        if (left.Rows != right.Rows)
            throw new MatrixSizeException();
        int w = left.Rows, c1 = left.Cols, c2 = right.Cols, c = c1 + c2;
        double[] newValues = GC.AllocateUninitializedArray<double>(w * c);
        for (int row = 0, offset = 0; row < w; row++)
        {
            Array.Copy(left.values, row * c1, newValues, offset, c1);
            offset += c1;
            Array.Copy(right.values, row * c2, newValues, offset, c2);
            offset += c2;
        }
        return new(w, c, newValues);
    }

    /// <summary>Horizontal concatenation of an array of matrices.</summary>
    /// <param name="m">Array of matrices.</param>
    /// <returns>A new matrix with more columns.</returns>
    public static Matrix HCat(params Matrix[] m)
    {
        if (m == null || m.Length == 0)
            throw new MatrixSizeException();
        int w = m[0].Rows, c = m[0].Cols;
        for (int i = 1; i < m.Length; i++)
            if (m[i].Rows != w)
                throw new MatrixSizeException();
            else
                c += m[i].Cols;
        double[] newValues = GC.AllocateUninitializedArray<double>(w * c);
        for (int row = 0, offset = 0; row < w; row++)
            for (int i = 0; i < m.Length; i++)
            {
                Array.Copy(m[i].values, row * m[i].Cols, newValues, offset, m[i].Cols);
                offset += m[i].Cols;
            }
        return new(w, c, newValues);
    }

    /// <summary>Horizontal concatenation of a matrix and a new column.</summary>
    /// <param name="left">Left matrix.</param>
    /// <param name="newColumn">New column, as a vector.</param>
    /// <returns>A new matrix with one more column.</returns>
    public static Matrix HCat(Matrix left, DVector newColumn)
    {
        if (left.Rows != newColumn.Length)
            throw new MatrixSizeException();
        int w = left.Rows, c1 = left.Cols, c = c1 + 1;
        double[] newValues = GC.AllocateUninitializedArray<double>(w * c);
        for (int row = 0, offset = 0; row < w; row++)
        {
            Array.Copy(left.values, row * c1, newValues, offset, c1);
            offset += c1;
            newValues[offset++] = newColumn.UnsafeThis(row);
        }
        return new(w, c, newValues);
    }

    /// <summary>Horizontal concatenation of a new column and a matrix.</summary>
    /// <param name="right">Left matrix.</param>
    /// <param name="newColumn">New column, as a vector.</param>
    /// <returns>A new matrix with one more column.</returns>
    public static Matrix HCat(DVector newColumn, Matrix right)
    {
        if (right.Rows != newColumn.Length)
            throw new MatrixSizeException();
        int w = right.Rows, c1 = right.Cols, c = c1 + 1;
        double[] newValues = GC.AllocateUninitializedArray<double>(w * c);
        for (int row = 0, offset = 0; row < w; row++)
        {
            newValues[offset++] = newColumn.UnsafeThis(row);
            Array.Copy(right.values, row * c1, newValues, offset, c1);
            offset += c1;
        }
        return new(w, c, newValues);
    }

    /// <summary>Vertical concatenation of two matrices.</summary>
    /// <param name="upper">Upper matrix.</param>
    /// <param name="lower">Lower matrix.</param>
    /// <returns>A new matrix with more rows.</returns>
    public static Matrix VCat(Matrix upper, Matrix lower)
    {
        if (upper.Cols != lower.Cols)
            throw new MatrixSizeException();
        int w1 = upper.Rows, c = upper.Cols, w2 = lower.Rows, w = w1 + w2;
        double[] newValues = GC.AllocateUninitializedArray<double>(w * c);
        Array.Copy(upper.values, newValues, upper.values.Length);
        Array.Copy(lower.values, 0, newValues, upper.values.Length, lower.values.Length);
        return new(w, c, newValues);
    }

    /// <summary>Vertical concatenation of an array of matrices.</summary>
    /// <param name="m">Array of matrices.</param>
    /// <returns>A new matrix with more rows.</returns>
    public static Matrix VCat(params Matrix[] m)
    {
        if (m == null || m.Length == 0)
            throw new MatrixSizeException();
        int w = m[0].Rows, c = m[0].Cols;
        for (int i = 1; i < m.Length; i++)
            if (m[i].Cols != c)
                throw new MatrixSizeException();
            else
                w += m[i].Rows;
        double[] newValues = GC.AllocateUninitializedArray<double>(w * c);
        for (int i = 0, offset = 0; i < m.Length; i++)
        {
            int len = m[i].values.Length;
            Array.Copy(m[i].values, 0, newValues, offset, len);
            offset += len;
        }
        return new(w, c, newValues);
    }

    /// <summary>Vertical concatenation of a matrix and a row vector.</summary>
    /// <param name="upper">Upper matrix.</param>
    /// <param name="lower">Lower row vector.</param>
    /// <returns>A new matrix with one more row.</returns>
    public static Matrix VCat(Matrix upper, DVector lower)
    {
        if (upper.Cols != lower.Length)
            throw new MatrixSizeException();
        int w1 = upper.Rows, c = upper.Cols, w = w1 + 1;
        double[] newValues = GC.AllocateUninitializedArray<double>(w * c);
        Array.Copy(upper.values, newValues, upper.values.Length);
        Array.Copy((double[])lower, 0, newValues, upper.values.Length, lower.Length);
        return new(w, c, newValues);
    }

    /// <summary>Vertical concatenation of a row vector and a matrix.</summary>
    /// <param name="upper">Upper row vector.</param>
    /// <param name="lower">Lower matrix.</param>
    /// <returns>A new matrix with one more row.</returns>
    public static Matrix VCat(DVector upper, Matrix lower)
    {
        if (upper.Length != lower.Cols)
            throw new MatrixSizeException();
        int w2 = lower.Rows, c = lower.Cols, w = w2 + 1;
        double[] newValues = GC.AllocateUninitializedArray<double>(w * c);
        Array.Copy((double[])upper, newValues, upper.Length);
        Array.Copy(lower.values, 0, newValues, upper.Length, lower.values.Length);
        return new(w, c, newValues);
    }

    /// <summary>Creates an identical rectangular matrix.</summary>
    /// <returns>A deep clone of the instance.</returns>
    public Matrix Clone() => new(Rows, Cols, (double[])values.Clone());

    /// <summary>Explicit conversion from a matrix to a 2D-array.</summary>
    /// <remarks>
    /// The returned array is a copy of the original matrix storage.
    /// </remarks>
    /// <param name="m">The original matrix.</param>
    /// <returns>A 2D-array representing the underlying storage.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator double[,](Matrix m)
    {
        double[,] result = new double[m.Rows, m.Cols];
        ref byte source = ref As<double, byte>(ref MM.GetArrayDataReference(m.values));
        ref byte target = ref As<double, byte>(ref result[0, 0]);
        CopyBlockUnaligned(ref target, ref source, (uint)(m.values.Length * sizeof(double)));
        return result;
    }

    /// <summary>Explicit conversion from a matrix to a 1D-array.</summary>
    /// <remarks>Use carefully: it returns the underlying 1D-array.</remarks>
    /// <param name="m">The original matrix.</param>
    /// <returns>The underlying bidimensional array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator double[](Matrix m) => m.values;

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
        {
            double* q = p;
            for (int row = 0; row < size; row++, q += size)
            {
                double* r = q + size + row;
                for (int col = row + 1; col < Cols; col++, r += size)
                    if (q[col] != *r)
                        return false;
            }
        }
        return true;
    }

    /// <summary>Gets the main diagonal.</summary>
    /// <returns>A vector containing values in the main diagonal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector Diagonal() => values.Diagonal(Rows, Cols);

    /// <summary>Calculates the trace of a matrix.</summary>
    /// <returns>The sum of the cells in the main diagonal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Trace() => values.Trace(Rows, Cols);

    /// <summary>Gets or sets the value of a single cell.</summary>
    /// <param name="row">The row number, between 0 and <see cref="Rows"/> - 1.</param>
    /// <param name="column">The column number, between 0 and <see cref="Cols"/> - 1.</param>
    /// <returns>The value at the given cell.</returns>
    public double this[int row, int column]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => values[row * Cols + column];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => values[row * Cols + column] = value;
    }

    /// <summary>Gets the value of a single cell using <see cref="Index"/>.</summary>
    /// <param name="row">The row number, between 0 and <see cref="Rows"/> - 1.</param>
    /// <param name="column">The column number, between 0 and <see cref="Cols"/> - 1.</param>
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
    public Matrix this[Range rowRange, Range columnRange]
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
                    double[] newValues = GC.AllocateUninitializedArray<double>(rLen * Cols);
                    Array.Copy(values, rOff * Cols, newValues, 0, rLen * Cols);
                    return new(rLen, Cols, newValues);
                }
            }
            if (cLen > 0 && rLen > 0)
            {
                double[] newValues = new double[rLen * cLen];
                int offset = rOff * Cols + cOff;
                for (int row = 0; row < rLen; row++, offset += Cols)
                    Array.Copy(values, offset, newValues, row * cLen, cLen);
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

    /// <summary>Creates a new matrix with different dimensions.</summary>
    /// <param name="rows">New number of rows.</param>
    /// <param name="columns">New number of columns.</param>
    /// <returns>A new matrix, or the same one, when no resizing is needed.</returns>
    public Matrix Redim(int rows, int columns)
    {
        if (Rows == rows && Cols == columns)
            return this;
        double[] newValues = rows <= Rows && Cols <= columns
            ? GC.AllocateUninitializedArray<double>(rows * columns)
            : new double[rows * columns];
        int maxRow = Min(Rows, rows), maxCol = Min(Cols, columns);
        for (int r = 0; r < maxRow; r++)
            Array.Copy(values, r * Cols, newValues, r * columns, maxCol);
        return new(rows, columns, newValues);
    }

    /// <summary>Creates a new matrix with different dimensions.</summary>
    /// <param name="size">New number of rows and columns.</param>
    /// <returns>A new matrix, or the same one, when no resizing is needed.</returns>
    public Matrix Redim(int size) => Redim(size, size);

    /// <summary>Copies the content of this matrix into an existing one.</summary>
    /// <param name="dest">Destination matrix.</param>
    internal void CopyTo(Matrix dest)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(dest.IsInitialized);
        Contract.Requires(Rows == dest.Rows);
        Contract.Requires(Cols == dest.Cols);
        Array.Copy(values, dest.values, values.Length);
    }

    /// <summary>Gets a copy of a row as a vector.</summary>
    /// <param name="row">Row number, counting from 0.</param>
    /// <returns>A copy of the row.</returns>
    public DVector GetRow(int row)
    {
        double[] v = GC.AllocateUninitializedArray<double>(Cols);
        Array.Copy(values, row * Cols, v, 0, Cols);
        return v;
    }

    /// <summary>Gets a copy of a row as a vector, using <see cref="Index"/>.</summary>
    /// <param name="row">Row number, counting from 0.</param>
    /// <returns>A copy of the row.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DVector GetRow(Index row) => GetRow(row.GetOffset(Rows));

    /// <summary>Gets a copy of a column as a vector.</summary>
    /// <param name="col">Column number, counting from 0.</param>
    /// <returns>A copy of the column.</returns>
    public DVector GetColumn(int col)
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
    public DVector GetColumn(Index col) => GetColumn(col.GetOffset(Cols));

    /// <summary>Transposes the matrix.</summary>
    /// <returns>A new matrix with swapped rows and cells.</returns>
    public Matrix Transpose()
    {
        Contract.Requires(IsInitialized);
        int r = Rows, c = Cols;
        nuint r1 = (nuint)r, r2 = r1 + r1, r3 = r2 + r1, r4 = r2 + r2, r8 = r4 + r4;
        nuint c1 = (nuint)c, c2 = c1 + c1, c3 = c2 + c1, c4 = c2 + c2, c8 = c4 + c4;
        double[] result = GC.AllocateUninitializedArray<double>(c * r);
        ref double a = ref MM.GetArrayDataReference(values);
        ref double b = ref MM.GetArrayDataReference(result);
        if (Avx512F.IsSupported)
        {
            V8L i0 = V8.Create(00L, 01, 08, 09, 02, 03, 10, 11);
            V8L i1 = V8.Create(04L, 05, 12, 13, 06, 07, 14, 15);
            V8L i2 = V8.Create(00L, 01, 02, 03, 12, 13, 14, 15);
            V8L i3 = V8.Create(06L, 07, 14, 15, 04, 05, 06, 07);
            V8L i4 = V8.Create(04L, 05, 12, 13, 04, 05, 06, 07);
            V8L i5 = V8.Create(02L, 03, 10, 11, 04, 05, 06, 07);
            V8L i6 = V8.Create(00L, 01, 08, 09, 04, 05, 06, 07);
            V8L i7 = V8.Create(00L, 01, 02, 03, 08, 09, 10, 11);
            // Blocks are multiple of eight.
            int rm = r & Simd.MASK8, cm = c & Simd.MASK8;
            ref double pA = ref a;
            for (int row = 0; row < rm; row += V8d.Count)
            {
                ref double q = ref Add(ref b, row);
                for (int col = 0; col < cm; col += V8d.Count)
                {
                    ref double pp = ref Add(ref pA, col);
                    V8d row0 = V8.LoadUnsafe(ref pp);
                    V8d row1 = V8.LoadUnsafe(ref pp, c1);
                    V8d row2 = V8.LoadUnsafe(ref pp, c2);
                    V8d row3 = V8.LoadUnsafe(ref pp, c3);
                    V8d row4 = V8.LoadUnsafe(ref pp, c4);
                    V8d row5 = V8.LoadUnsafe(ref pp, c4 + c1);
                    V8d row6 = V8.LoadUnsafe(ref pp, c4 + c2);
                    V8d row7 = V8.LoadUnsafe(ref pp, c4 + c3);

                    V8d t0 = Avx512F.UnpackLow(row0, row1);
                    V8d t1 = Avx512F.UnpackHigh(row0, row1);
                    V8d t2 = Avx512F.UnpackLow(row2, row3);
                    V8d t3 = Avx512F.UnpackHigh(row2, row3);
                    V8d t4 = Avx512F.UnpackLow(row4, row5);
                    V8d t5 = Avx512F.UnpackHigh(row4, row5);
                    V8d t6 = Avx512F.UnpackLow(row6, row7);
                    V8d t7 = Avx512F.UnpackHigh(row6, row7);

                    V8d t8 = Avx512F.PermuteVar8x64x2(t4, i0, t6);
                    t6 = Avx512F.PermuteVar8x64x2(t4, i1, t6);
                    t4 = Avx512F.PermuteVar8x64x2(t5, i0, t7);
                    t5 = Avx512F.PermuteVar8x64x2(t5, i1, t7);

                    V8.StoreUnsafe(Avx512F.PermuteVar8x64x2(
                        Avx512F.PermuteVar8x64x2(t0, i6, t2), i7, t8), ref q);
                    V8.StoreUnsafe(Avx512F.PermuteVar8x64x2(
                        Avx512F.PermuteVar8x64x2(t1, i6, t3), i7, t4), ref q, r1);
                    V8.StoreUnsafe(Avx512F.PermuteVar8x64x2(
                        Avx512F.PermuteVar8x64x2(t0, i5, t2), i2, t8), ref q, r2);
                    V8.StoreUnsafe(Avx512F.PermuteVar8x64x2(
                        Avx512F.PermuteVar8x64x2(t1, i5, t3), i2, t4), ref q, r3);
                    V8.StoreUnsafe(Avx512F.PermuteVar8x64x2(
                        Avx512F.PermuteVar8x64x2(t0, i4, t2), i7, t6), ref q, r4);
                    V8.StoreUnsafe(Avx512F.PermuteVar8x64x2(
                        Avx512F.PermuteVar8x64x2(t1, i4, t3), i7, t5), ref q, r4 + r1);
                    V8.StoreUnsafe(Avx512F.PermuteVar8x64x2(
                        Avx512F.PermuteVar8x64x2(t0, i3, t2), i2, t6), ref q, r4 + r2);
                    V8.StoreUnsafe(Avx512F.PermuteVar8x64x2(
                        Avx512F.PermuteVar8x64x2(t1, i3, t3), i2, t5), ref q, r4 + r3);
                    q = ref Add(ref q, r8);
                }
                pA = ref Add(ref pA, c8);
            }
            for (int row = rm; row < r; row++)
            {
                ref double src = ref Add(ref a, row * c);
                for (int col = 0, cr = row; col < c; col++, cr += r)
                    Add(ref b, cr) = Add(ref src, col);
            }
            for (int col = cm; col < c; col++)
            {
                ref double dst = ref Add(ref b, col * r);
                for (int row = 0, rc = col; row < rm; row++, rc += c)
                    Add(ref dst, row) = Add(ref a, rc);
            }
        }
        else if (Avx.IsSupported)
        {
            // Blocks are multiple of four.
            int rm = r & Simd.MASK4, cm = c & Simd.MASK4;
            ref double pA = ref a;
            for (int row = 0; row < rm; row += V4d.Count)
            {
                ref double q = ref Add(ref b, row);
                for (int col = 0; col < cm; col += V4d.Count)
                {
                    ref double pp = ref Add(ref pA, col);
                    var row1 = V4.LoadUnsafe(ref pp);
                    var row2 = V4.LoadUnsafe(ref pp, c1);
                    var row3 = V4.LoadUnsafe(ref pp, c2);
                    var row4 = V4.LoadUnsafe(ref pp, c3);
                    var t1 = Avx.Shuffle(row1, row2, 0b_0000);
                    var t2 = Avx.Shuffle(row1, row2, 0b_1111);
                    var t3 = Avx.Shuffle(row3, row4, 0b_0000);
                    var t4 = Avx.Shuffle(row3, row4, 0b_1111);
                    V4.StoreUnsafe(Avx.Permute2x128(t1, t3, 0b_0010_0000), ref q);
                    V4.StoreUnsafe(Avx.Permute2x128(t2, t4, 0b_0010_0000), ref q, r1);
                    V4.StoreUnsafe(Avx.Permute2x128(t1, t3, 0b_0011_0001), ref q, r2);
                    V4.StoreUnsafe(Avx.Permute2x128(t2, t4, 0b_0011_0001), ref q, r3);
                    q = ref Add(ref q, r4);
                }
                pA = ref Add(ref pA, c4);
            }
            for (int row = rm; row < r; row++)
            {
                ref double src = ref Add(ref a, row * c);
                for (int col = 0; col < c; col++)
                    Add(ref b, col * r + row) = Add(ref src, col);
            }
            for (int col = cm; col < c; col++)
            {
                ref double dst = ref Add(ref b, col * r);
                for (int row = 0; row < rm; row++)
                    Add(ref dst, row) = Add(ref a, row * c + col);
            }
        }
        else
            for (int row = 0; row < r; row++)
                for (int col = 0, cr = row; col < c; col++, cr += r)
                    Add(ref b, cr) = Add(ref a, row * c + col);
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
        if (m1.Rows != m2.Rows || m1.Cols != m2.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m1.Cols);
        double[] result = GC.AllocateUninitializedArray<double>(m1.values.Length);
        m1.values.AsSpan().Add(m2.values, result);
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
        if (m1.Rows != m2.Rows || m1.Cols != m2.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == m1.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m1.Cols);
        double[] result = GC.AllocateUninitializedArray<double>(m1.values.Length);
        m1.values.AsSpan().Sub(m2.values, result);
        return new(m1.Rows, m1.Cols, result);
    }

    /// <summary>Negates a matrix.</summary>
    /// <param name="m">The matrix operand.</param>
    /// <returns>Cell-by-cell negation.</returns>
    public static Matrix operator -(Matrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);
        double[] result = GC.AllocateUninitializedArray<double>(m.values.Length);
        m.values.AsSpan().Neg(result);
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Adds a scalar to a matrix.</summary>
    /// <param name="m">The matrix.</param>
    /// <param name="d">A scalar summand.</param>
    /// <returns>The pointwise addition of the scalar.</returns>
    public static Matrix operator +(Matrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);
        double[] result = GC.AllocateUninitializedArray<double>(m.values.Length);
        m.values.AsSpan().Add(d, result);
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Adds a scalar to a matrix.</summary>
    /// <param name="d">A scalar summand.</param>
    /// <param name="m">The matrix.</param>
    /// <returns>The pointwise addition of the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator +(double d, Matrix m) => m + d;

    /// <summary>Subtracts a scalar from a matrix.</summary>
    /// <param name="m">The matrix minuend.</param>
    /// <param name="d">The scalar subtrahend.</param>
    /// <returns>The pointwise subtraction of the scalar.</returns>
    public static Matrix operator -(Matrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);
        double[] result = GC.AllocateUninitializedArray<double>(m.values.Length);
        m.values.AsSpan().Sub(d, result);
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Subtracts a matrix from a scalar.</summary>
    /// <param name="d">The scalar minuend.</param>
    /// <param name="m">The matrix subtrahend.</param>
    /// <returns>The pointwise subtraction of the scalar.</returns>
    public static Matrix operator -(double d, Matrix m)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);
        double[] result = GC.AllocateUninitializedArray<double>(m.values.Length);
        CommonMatrix.Sub(d, m.values, result);
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Cell by cell product with a second matrix.</summary>
    /// <param name="m">Second operand.</param>
    /// <returns>The pointwise product.</returns>
    public Matrix PointwiseMultiply(Matrix m)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(m.IsInitialized);
        if (Rows != m.Rows || Cols != m.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == Cols);
        return new(Rows, Cols, values.AsSpan().Mul(m.values));
    }

    /// <summary>Cell by cell division with a second matrix.</summary>
    /// <param name="m">The matrix divisor.</param>
    /// <returns>The pointwise quotient.</returns>
    public Matrix PointwiseDivide(Matrix m)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(m.IsInitialized);
        if (Rows != m.Rows || Cols != m.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == Cols);
        return new(Rows, Cols, values.AsSpan().Div(m.values));
    }

    /// <summary>Multiplies a matrix by a scalar value.</summary>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <param name="d">A scalar multiplicand.</param>
    /// <returns>The multiplication of the matrix by the scalar.</returns>
    public static Matrix operator *(Matrix m, double d)
    {
        Contract.Requires(m.IsInitialized);
        Contract.Ensures(Contract.Result<Matrix>().Rows == m.Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Cols);
        double[] result = GC.AllocateUninitializedArray<double>(m.values.Length);
        m.values.AsSpan().Mul(d, result);
        return new(m.Rows, m.Cols, result);
    }

    /// <summary>Multiplies a matrix by a scalar value.</summary>
    /// <param name="d">A scalar multiplicand.</param>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <returns>The multiplication of the matrix by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator *(double d, Matrix m) => m * d;

    /// <summary>Solves the equation m2*x = m1 for the matrix x.</summary>
    /// <param name="m1">The matrix at the right side.</param>
    /// <param name="m2">The matrix at the left side.</param>
    /// <returns>The solving matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator /(Matrix m1, Matrix m2) => m2.Solve(m1);

    /// <summary>Solves the equation m*x = v for the vector x.</summary>
    /// <param name="v">The vector at the right side.</param>
    /// <param name="m">The matrix at the left side.</param>
    /// <returns>The solving vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DVector operator /(DVector v, Matrix m) => m.Solve(v);

    /// <summary>Divides a matrix by a scalar value.</summary>
    /// <param name="m">Matrix to be multiplied.</param>
    /// <param name="d">A scalar multiplicand.</param>
    /// <returns>The quotient of the matrix by the scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator /(Matrix m, double d) => m * (1.0 / d);

    /// <summary>Multiplies two compatible matrices.</summary>
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
        int m = m1.Rows, n = m1.Cols, p = m2.Cols;
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
            double* pa = a, pc = c;
            for (int i = 0, top = p & Simd.MASK4; i < m; i++)
            {
                double* pb = b;
                for (int k = 0; k < n; k++)
                {
                    double d = pa[k];
                    int j = 0;
                    if (Avx.IsSupported)
                        for (V4d vd = V4.Create(d); j < top; j += V4d.Count)
                            Avx.Store(pc + j, Avx.LoadVector256(pc + j).MultiplyAdd(pb + j, vd));
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
                                if (Avx512F.IsSupported)
                                    for (V8d vd = V8.Create(d); j < top; j += 16)
                                    {
                                        V8d op1 = Avx512F.LoadVector512(pb + j);
                                        V8d op2 = Avx512F.LoadVector512(pb + j + 8);
                                        Avx512F.Store(pc + j, Avx512F.FusedMultiplyAdd(
                                            op1, vd, Avx512F.LoadVector512(pc + j)));
                                        Avx512F.Store(pc + j + 8, Avx512F.FusedMultiplyAdd(
                                            op2, vd, Avx512F.LoadVector512(pc + j + 8)));
                                    }
                                else if (Avx.IsSupported)
                                    for (V4d vd = V4.Create(d); j < top; j += 16)
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
                                    for (var vd = V4.Create(d); j < top; j += 16)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix MultiplyTranspose(Matrix m) =>
        MultiplyTranspose(m, GC.AllocateUninitializedArray<double>(Rows * m.Rows));

    /// <summary>Multiplies this matrix by the transposed argument.</summary>
    /// <remarks>
    /// <para>Calculates <c>this * m'</c>.</para>
    /// <para>This method allows preallocating a buffer for the result.
    /// This can be useful when the same operation is performed multiple times.
    /// The buffer size must be <c>this.Rows * m.Rows</c>.</para>
    /// </remarks>
    /// <param name="m">Second operand.</param>
    /// <param name="result">Preallocated buffer for the result.</param>
    /// <returns>The multiplication by the transposed argument.</returns>
    public Matrix MultiplyTranspose(Matrix m, double[] result)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(m.IsInitialized);
        if (Cols != m.Cols)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<Matrix>().Rows == Rows);
        Contract.Ensures(Contract.Result<Matrix>().Cols == m.Rows);

        int r = Rows, n = Cols, c = m.Rows;
        ref double a = ref MM.GetArrayDataReference(values);
        ref double b = ref MM.GetArrayDataReference(m.values);
        ref double t = ref MM.GetArrayDataReference(result);
        for (int i = 0; i < r; i++, a = ref Add(ref a, n), t = ref Add(ref t, c))
        {
            ref double bj = ref b;
            for (int j = 0; j < c; j++, bj = ref Add(ref bj, n))
                Add(ref t, j) = MM.CreateSpan(ref a, n).Dot(MM.CreateSpan(ref bj, n));
        }
        return new(r, c, result);
    }

    /// <summary>Transform a vector using a matrix.</summary>
    /// <param name="m">The transformation matrix.</param>
    /// <param name="v">Vector to transform.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DVector operator *(Matrix m, DVector v) =>
        m.Transform(v, GC.AllocateUninitializedArray<double>(m.Rows));

    /// <summary>Transform a complex vector using a matrix.</summary>
    /// <param name="m">The transformation matrix.</param>
    /// <param name="v">Complex vector to transform.</param>
    /// <returns>The transformed complex vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CVector operator *(Matrix m, CVector v) =>
        new(m * v.Real, m * v.Imaginary);

    /// <summary>Transforms a vector using a matrix and a preallocated buffer.</summary>
    /// <remarks>The buffer must have <see cref="Matrix.Rows"/> items.</remarks>
    /// <param name="v">Vector to transform.</param>
    /// <param name="result">Preallocated buffer for the result.</param>
    /// <returns>The transformed vector.</returns>
    /// <exception cref="MatrixSizeException">
    /// When the matrix and vector have non-matching sizes.
    /// </exception>
    public DVector Transform(DVector v, double[] result)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        int r = Rows, c = Cols;
        if (c != v.Length)
            throw new MatrixSizeException();

        ref double b = ref MM.GetArrayDataReference(result);
        for (int i = 0, offset = 0; i < r; i++, offset += c)
            Add(ref b, i) = values.AsSpan(offset, c).Dot((double[])v);
        return result;
    }

    /// <summary>Transform a vector using the transposed matrix.</summary>
    /// <remarks>
    /// This operator is equivalent to the method <see cref="TransposeMultiply(DVector)"/>.
    /// </remarks>
    /// <param name="v">Vector to transform.</param>
    /// <param name="m">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DVector operator *(DVector v, Matrix m) => m.TransposeMultiply(v);

    /// <summary>Transform a complex vector using the transposed matrix.</summary>
    /// <remarks>
    /// This operator is equivalent to the method <see cref="TransposeMultiply(DVector)"/>,
    /// but operating on complex vectors.
    /// </remarks>
    /// <param name="v">Complex vector to transform.</param>
    /// <param name="m">The transformation matrix.</param>
    /// <returns>The transformed complex vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CVector operator *(CVector v, Matrix m) =>
        new(v.Real * m, v.Imaginary * m);

    /// <summary>Transforms a vector using the transpose of this matrix.</summary>
    /// <remarks>
    /// This method calcutes <c>this' * v</c>, or, equivalently, <c>v * this</c>.
    /// </remarks>
    /// <param name="v">The vector to transform.</param>
    /// <returns>The transformed vector.</returns>
    public DVector TransposeMultiply(DVector v)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        if (Rows != v.Length)
            throw new MatrixSizeException();
        Contract.Ensures(Contract.Result<DVector>().Length == Cols);

        int r = Rows, c = Cols;
        double[] result = new double[c];
        ref double p = ref MM.GetArrayDataReference(values);
        ref double q = ref MM.GetArrayDataReference((double[])v);
        Span<double> target = MM.CreateSpan(ref MM.GetArrayDataReference(result), c);
        for (int k = 0; k < r; k++, p = ref Add(ref p, c))
            MM.CreateSpan(ref p, c).MulAddStore(Add(ref q, k), target);
        return result;
    }

    /// <summary>Transforms a vector and adds an offset.</summary>
    /// <remarks>
    /// This method avoids allocating a temporary vector for the multiplication.
    /// </remarks>
    /// <param name="v">Vector to transform.</param>
    /// <param name="add">Vector to add.</param>
    /// <returns><c>this * v + add</c>.</returns>
    public DVector MultiplyAdd(DVector v, DVector add)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        int r = Rows, c = Cols;
        if (c != v.Length)
            throw new MatrixSizeException();

        double[] result = GC.AllocateUninitializedArray<double>(r);
        double[] source = (double[])v;
        ref double ad = ref MM.GetArrayDataReference((double[])add);
        ref double b = ref MM.GetArrayDataReference(result);
        for (int i = 0, offset = 0; i < r; i++, offset += c, ad = ref Add(ref ad, 1))
            Add(ref b, i) = values.AsSpan(offset, c).Dot(source) + ad;
        return result;
    }

    /// <summary>Transforms a complex vector and adds an offset.</summary>
    /// <remarks>
    /// This method avoids allocating a temporary vector for the multiplication.
    /// </remarks>
    /// <param name="v">Complex vector to transform.</param>
    /// <param name="add">Complex ector to add.</param>
    /// <returns><c>this * v + add</c>.</returns>
    public CVector MultiplyAdd(CVector v, CVector add) =>
        new(MultiplyAdd(v.Real, add.Real), MultiplyAdd(v.Imaginary, add.Imaginary));

    /// <summary>Transforms a vector and adds an offset.</summary>
    /// <remarks>
    /// This method avoids allocating two temporary vectors.
    /// </remarks>
    /// <param name="v">Vector to transform.</param>
    /// <param name="scale">A scale factor for the vector to add.</param>
    /// <param name="add">Vector to add.</param>
    /// <returns><c>this * v + scale * add</c>.</returns>
    public DVector MultiplyAdd(DVector v, double scale, DVector add)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        int r = Rows, c = Cols;
        if (c != v.Length)
            throw new MatrixSizeException();

        double[] result = GC.AllocateUninitializedArray<double>(r);
        double[] source = (double[])v;
        ref double ad = ref MM.GetArrayDataReference((double[])add);
        ref double b = ref MM.GetArrayDataReference(result);
        for (int i = 0, offset = 0; i < r; i++, offset += c, ad = ref Add(ref ad, 1))
            Add(ref b, i) = values.AsSpan(offset, c).Dot(source) + scale * ad;
        return result;
    }

    /// <summary>Transforms a vector and subtracts an offset.</summary>
    /// <remarks>
    /// This method avoids allocating a temporary vector for the multiplication.
    /// </remarks>
    /// <param name="v">Vector to transform.</param>
    /// <param name="sub">Vector to subtract.</param>
    /// <returns><c>this * multiplicand - sub</c>.</returns>
    public DVector MultiplySubtract(DVector v, DVector sub)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        int r = Rows, c = Cols;
        if (c != v.Length)
            throw new MatrixSizeException();

        double[] result = GC.AllocateUninitializedArray<double>(r);
        double[] source = (double[])v;
        ref double sb = ref MM.GetArrayDataReference((double[])sub);
        ref double b = ref MM.GetArrayDataReference(result);
        for (int i = 0, offset = 0; i < r; i++, offset += c, sb = ref Add(ref sb, 1))
            Add(ref b, i) = values.AsSpan(offset, c).Dot(source) - sb;
        return result;
    }

    /// <summary>Optimized subtraction of transformed vector.</summary>
    /// <remarks>
    /// <para>The second vector is the minuend.</para>
    /// <para>This operation is hardware-accelerated when possible.</para>
    /// </remarks>
    /// <param name="v">The vector to be transformed.</param>
    /// <param name="minuend">The vector acting as minuend.</param>
    /// <returns><code>minuend - this v</code>.</returns>
    public DVector SubtractMultiply(DVector v, DVector minuend)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(v.IsInitialized);
        int r = Rows, c = Cols;
        if (c != v.Length)
            throw new MatrixSizeException();

        double[] result = GC.AllocateUninitializedArray<double>(r);
        double[] source = (double[])v;
        ref double sb = ref MM.GetArrayDataReference((double[])minuend);
        ref double b = ref MM.GetArrayDataReference(result);
        for (int i = 0, offset = 0; i < r; i++, offset += c, sb = ref Add(ref sb, 1))
            Add(ref b, i) = sb - values.AsSpan(offset, c).Dot(source);
        return result;
    }

    /// <summary>Optimized subtraction of transformed complex vector.</summary>
    /// <remarks>
    /// <para>The second vector is the minuend.</para>
    /// <para>This operation is hardware-accelerated when possible.</para>
    /// </remarks>
    /// <param name="v">The vector to be transformed.</param>
    /// <param name="minuend">The vector acting as minuend.</param>
    /// <returns><code>minuend - this v</code>.</returns>
    public CVector SubtractMultiply(CVector v, CVector minuend) =>
        new(SubtractMultiply(v.Real, minuend.Real), SubtractMultiply(v.Imaginary, minuend.Imaginary));

    /// <summary>Transforms a complex vector and subtracts an offset.</summary>
    /// <remarks>
    /// This method avoids allocating a temporary vector for the multiplication.
    /// </remarks>
    /// <param name="v">Complex vector to transform.</param>
    /// <param name="sub">Complex vector to subtract.</param>
    /// <returns><c>this * multiplicand - sub</c>.</returns>
    public CVector MultiplySubtract(CVector v, CVector sub) =>
        new(MultiplySubtract(v.Real, sub.Real), MultiplySubtract(v.Imaginary, sub.Imaginary));

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
    public DVector Solve(DVector v) => LU().Solve(v);

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

    /// <summary>Gets statistics on the matrix cells.</summary>
    /// <returns>Matrix statistics.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Accumulator Stats() => new(values);

    /// <summary>Checks if the matrix contains the given value.</summary>
    /// <param name="value">Value to locate.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(double value)
    {
        Contract.Requires(IsInitialized);
        return new ReadOnlySpan<double>(values).IndexOf(value) != -1;
    }

    /// <summary>Computes the maximum difference between cells.</summary>
    /// <param name="m">The reference matrix.</param>
    /// <returns>The max-norm of the matrix difference.</returns>
    public double Distance(Matrix m)
    {
        Contract.Requires(IsInitialized);
        Contract.Requires(m.IsInitialized);
        return values.Distance(m.values);
    }

    /// <summary>Gets the cell with the maximum absolute value.</summary>
    /// <returns>The max-norm of the matrix.</returns>
    public double AMax()
    {
        Contract.Requires(IsInitialized);
        return values.AsSpan().AMax();
    }

    /// <summary>Gets the cell with the minimum absolute value.</summary>
    /// <returns>The minimum absolute value in the matrix.</returns>
    public double AMin()
    {
        Contract.Requires(IsInitialized);
        return values.AsSpan().AMin();
    }

    /// <summary>Gets the cell with the maximum value.</summary>
    /// <returns>The cell with the maximum value.</returns>
    public double Maximum()
    {
        Contract.Requires(IsInitialized);
        return values.AsSpan().Max();
    }

    /// <summary>Gets the cell with the minimum value.</summary>
    /// <returns>The cell with the minimum value.</returns>
    public double Minimum()
    {
        Contract.Requires(IsInitialized);
        return values.AsSpan().Min();
    }

    /// <summary>Applies a function to each cell of the matrix.</summary>
    /// <param name="mapper">The transformation function.</param>
    /// <returns>A new matrix with transformed cells.</returns>
    public Matrix Map(Func<double, double> mapper)
    {
        double[] newValues = GC.AllocateUninitializedArray<double>(values.Length);
        ref double p = ref MM.GetArrayDataReference(values);
        ref double q = ref MM.GetArrayDataReference(newValues);
        for (int i = 0; i < newValues.Length; i++)
            Add(ref q, i) = mapper(Add(ref p, i));
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
    /// <param name="other">The matrix to be compared.</param>
    /// <returns><see langword="true"/> if the second matrix has the same values.</returns>
    public bool Equals(Matrix other) =>
        Rows == other.Rows && Cols == other.Cols && values.Eqs(other.values);

    /// <summary>Checks if the provided argument is a matrix with the same values.</summary>
    /// <param name="obj">The object to be compared.</param>
    /// <returns><see langword="true"/> if the argument is a matrix with the same values.</returns>
    public override bool Equals(object? obj) => obj is Matrix matrix && Equals(matrix);

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
        values.ToString(Rows, Cols, v => v.ToString("G6"), 0);

    /// <summary>Gets a textual representation of this matrix.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>One line for each row, with space separated columns.</returns>
    public string ToString(string? format, IFormatProvider? provider = null) =>
        $"ans ∊ ℝ({Rows}⨯{Cols})" + Environment.NewLine +
        values.ToString(Rows, Cols, v => v.ToString(format, provider), 0);
}

/// <summary>JSON converter for rectangular matrices.</summary>
public class MatrixJsonConverter : JsonConverter<Matrix>
{
    /// <summary>Reads and convert JSON to a <see cref="Matrix"/> instance.</summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type of the object to convert.</param>
    /// <param name="options">JSON options.</param>
    /// <returns>A triangular matrix with the values read from JSON.</returns>
    public override Matrix Read(
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
                ref double p = ref MM.GetArrayDataReference(values);
                int total = rows * cols;
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

    /// <summary>Converts a rectangular matrix to JSON.</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The matrix to serialize.</param>
    /// <param name="options">JSON options.</param>    
    public override void Write(
        Utf8JsonWriter writer,
        Matrix value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(nameof(Matrix.Rows), value.Rows);
        writer.WriteNumber(nameof(Matrix.Cols), value.Cols);
        writer.WriteStartArray("values");
        foreach (double v in (double[])value)
            writer.WriteNumberValue(v);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
