namespace Austra.Library;

/// <summary>Common matrix operations.</summary>
public static class CommonMatrix
{
    /// <summary>Deconstruct a complex number into its real and imaginary parts.</summary>
    /// <param name="complex">The value to be deconstructed.</param>
    /// <param name="real">The real part.</param>
    /// <param name="imaginary">The imaginary part.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deconstruct(this Complex complex, out double real, out double imaginary) =>
        (real, imaginary) = (complex.Real, complex.Imaginary);

    /// <summary>Creates an identity matrix given its size.</summary>
    /// <param name="size">Number of rows and columns.</param>
    /// <returns>An identity matrix with the requested size.</returns>
    public unsafe static double[,] CreateIdentity(int size)
    {
        double[,] values = new double[size, size];
        int r = size + 1;
        fixed (double* pA = values)
            for (double* p = pA; size-- > 0; p += r)
                *p = 1.0;
        return values;
    }

    /// <summary>Creates a diagonal matrix given its diagonal.</summary>
    /// <param name="diagonal">Values in the diagonal.</param>
    /// <returns>A 2D-array with its main diagonal initialized.</returns>
    public unsafe static double[,] CreateDiagonal(Vector diagonal)
    {
        int size = diagonal.Length, r = size + 1; ;
        double[,] values = new double[size, size];
        fixed (double* pA = values, pB = (double[])diagonal)
            for (double* p = pA, q = pB; size-- > 0; p += r)
                *p = *q++;
        return values;
    }

    /// <summary>Gets the main diagonal of a 2D-array.</summary>
    /// <param name="values">A 2D-array.</param>
    /// <returns>A vector containing values in the main diagonal.</returns>
    public unsafe static Vector Diagonal(double[,] values)
    {
        int rows = values.GetLength(0), cells = values.GetLength(1);
        int r = cells + 1, size = Min(rows, cells);
        double[] result = new double[size];
        fixed (double* pA = values, pB = result)
            for (double* p = pA, q = pB; size-- > 0; p += r)
                *q++ = *p;
        return result;
    }

    /// <summary>Calculates the trace of a 2D-array.</summary>
    /// <param name="values">A 2D-array.</param>
    /// <returns>The sum of the cells in the main diagonal.</returns>
    public unsafe static double Trace(double[,] values)
    {
        double trace = 0;
        int rows = values.GetLength(0), cells = values.GetLength(1);
        int r = cells + 1, size = Min(rows, cells);
        if (size <= 4)
            for (int s = size; s-- > 0;)
                trace += values[s, s];
        else
            fixed (double* pA = values)
                for (double* p = pA; size-- > 0; p += r)
                    trace += *p;
        return trace;
    }

    /// <summary>Gets the product of the cells in the main diagonal.</summary>
    /// <param name="values">A 2D-array.</param>
    /// <returns>The product of the main diagonal.</returns>
    public unsafe static double DiagonalProduct(double[,] values)
    {
        int rows = values.GetLength(0), cells = values.GetLength(1);
        int r = cells + 1, size = Min(rows, cells);
        double product = 1.0;
        fixed (double* pA = values)
            for (double* p = pA; size-- > 0; p += r)
                product *= *p;
        return product;
    }

    /// <summary>Computes the dot product of two double arrays.</summary>
    /// <param name="p">Pointer to the first array.</param>
    /// <param name="q">Pointer to the second array.</param>
    /// <param name="size">Number of items in each array.</param>
    /// <returns>A sum of products.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static double DotProduct(double* p, double* q, int size)
    {
        double sum = 0;
        int i = 0;
        if (Avx.IsSupported)
        {
            Vector256<double> acc = Vector256<double>.Zero;
            for (int top = size & Simd.AVX_MASK; i < top; i += 4)
                acc = acc.MultiplyAdd(p + i, q + i);
            sum = acc.Sum();
        }
        for (; i < size; i++)
            sum += p[i] * q[i];
        return sum;
    }

    /// <summary>Gets the item in an array with the minimum absolute value.</summary>
    /// <param name="p">Pointer to the array.</param>
    /// <param name="size">Number of items in the array.</param>
    /// <returns>The minimum absolute value in the samples.</returns>
    public unsafe static double AbsoluteMaximum(double* p, int size)
    {
        double max = 0;
        int i = 0;
        if (Avx.IsSupported && size >= 8)
        {
            Vector256<double> z = Vector256<double>.Zero, vm = z;
            for (int top = size & Simd.AVX_MASK; i < top; i += 4)
            {
                Vector256<double> v = Avx.LoadVector256(p + i);
                vm = Avx.Max(Avx.Max(v, Avx.Subtract(z, v)), vm);
            }
            max = vm.Max();
        }
        for (; i < size; i++)
        {
            double v = Abs(p[i]);
            if (v > max)
                max = v;
        }
        return max;
    }

    /// <summary>Gets the item in an array with the maximum absolute value.</summary>
    /// <param name="p">Pointer to the array.</param>
    /// <param name="size">Number of items in the array.</param>
    /// <returns>The max-norm of the array segment.</returns>
    public unsafe static double AbsoluteMinimum(double* p, int size)
    {
        double min = Abs(*p);
        int i = 0;
        if (Avx.IsSupported && size >= 8)
        {
            Vector256<double> z = Vector256<double>.Zero;
            Vector256<double> vm = Vector256.Create(min);
            for (int top = size & Simd.AVX_MASK; i < top; i += 4)
            {
                Vector256<double> v = Avx.LoadVector256(p + i);
                vm = Avx.Min(Avx.Max(v, Avx.Subtract(z, v)), vm);
            }
            min = vm.Min();
        }
        for (; i < size; i++)
        {
            double v = Abs(p[i]);
            if (v < min)
                min = v;
        }
        return min;
    }

    /// <summary>Gets the item with the maximum value in the array.</summary>
    /// <param name="p">Pointer to the array.</param>
    /// <param name="size">Number of items in the array.</param>
    /// <returns>The item with the minimum value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static double Maximum(double* p, int size)
    {
        double max = double.MinValue;
        int i = 0;
        if (Avx.IsSupported && size >= 8)
        {
            Vector256<double> vm = Avx.LoadVector256(p);
            i = 4;
            for (int top = size & Simd.AVX_MASK; i < top; i += 4)
                vm = Avx.Max(Avx.LoadVector256(p + i), vm);
            max = vm.Max();
        }
        for (; i < size; i++)
        {
            double v = p[i];
            if (v > max)
                max = v;
        }
        return max;
    }

    /// <summary>Gets the item with the minimum value in the array.</summary>
    /// <param name="p">Pointer to the array.</param>
    /// <param name="size">Number of items in the array.</param>
    /// <returns>The item with the minimum value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static double Minimum(double* p, int size)
    {
        double min = double.MaxValue;
        int i = 0;
        if (Avx.IsSupported && size >= 8)
        {
            Vector256<double> vm = Avx.LoadVector256(p);
            i = 4;
            for (int top = size & Simd.AVX_MASK; i < top; i += 4)
                vm = Avx.Min(Avx.LoadVector256(p + i), vm);
            min = vm.Min();
        }
        for (; i < size; i++)
        {
            double v = p[i];
            if (v < min)
                min = v;
        }
        return min;
    }

    /// <summary>In place transposition of a square matrix.</summary>
    /// <param name="data">A 2D-array with data.</param>
    public unsafe static void Transpose(double[,] data)
    {
        Contract.Requires(data.GetLength(0) == data.GetLength(1));
        fixed (double* a = data)
            Transpose(a, data.GetLength(0));
    }

    /// <summary>In place transposition of a square matrix.</summary>
    /// <param name="a">Pointer to raw data.</param>
    /// <param name="size">The size of the matrix.</param>
    internal unsafe static void Transpose(double* a, int size)
    {
        if (Avx.IsSupported)
        {
            int s1 = size & Simd.AVX_MASK;
            int s2 = size + size;
            for (int r = 0; r < s1; r += 4)
            {
                for (int c = 0; c < r; c += 4)
                {
                    double* pp = a + (r * size + c);
                    double* qq = a + (c * size + r);
                    var row1 = Avx.LoadVector256(pp);
                    var row2 = Avx.LoadVector256(pp + size);
                    var row3 = Avx.LoadVector256(pp + s2);
                    var row4 = Avx.LoadVector256(pp + s2 + size);
                    var t1 = Avx.Shuffle(row1, row2, 0b0000);
                    var t2 = Avx.Shuffle(row1, row2, 0b1111);
                    var t3 = Avx.Shuffle(row3, row4, 0b0000);
                    var t4 = Avx.Shuffle(row3, row4, 0b1111);
                    row1 = Avx.LoadVector256(qq);
                    row2 = Avx.LoadVector256(qq + size);
                    row3 = Avx.LoadVector256(qq + s2);
                    row4 = Avx.LoadVector256(qq + s2 + size);
                    Avx.Store(qq, Avx.Permute2x128(t1, t3, 0b00100000));
                    Avx.Store(qq + size, Avx.Permute2x128(t2, t4, 0b00100000));
                    Avx.Store(qq + s2, Avx.Permute2x128(t1, t3, 0b00110001));
                    Avx.Store(qq + s2 + size, Avx.Permute2x128(t2, t4, 0b00110001));
                    t1 = Avx.Shuffle(row1, row2, 0b0000);
                    t2 = Avx.Shuffle(row1, row2, 0b1111);
                    t3 = Avx.Shuffle(row3, row4, 0b0000);
                    t4 = Avx.Shuffle(row3, row4, 0b1111);
                    Avx.Store(pp, Avx.Permute2x128(t1, t3, 0b00100000));
                    Avx.Store(pp + size, Avx.Permute2x128(t2, t4, 0b00100000));
                    Avx.Store(pp + s2, Avx.Permute2x128(t1, t3, 0b00110001));
                    Avx.Store(pp + s2 + size, Avx.Permute2x128(t2, t4, 0b00110001));
                }
                // Transpose a diagonal block.
                {
                    double* pp = a + (r * size + r);
                    var row1 = Avx.LoadVector256(pp);
                    var row2 = Avx.LoadVector256(pp + size);
                    var row3 = Avx.LoadVector256(pp + s2);
                    var row4 = Avx.LoadVector256(pp + s2 + size);
                    var t1 = Avx.Shuffle(row1, row2, 0b0000);
                    var t2 = Avx.Shuffle(row1, row2, 0b1111);
                    var t3 = Avx.Shuffle(row3, row4, 0b0000);
                    var t4 = Avx.Shuffle(row3, row4, 0b1111);
                    Avx.Store(pp, Avx.Permute2x128(t1, t3, 0b00100000));
                    Avx.Store(pp + size, Avx.Permute2x128(t2, t4, 0b00100000));
                    Avx.Store(pp + s2, Avx.Permute2x128(t1, t3, 0b00110001));
                    Avx.Store(pp + s2 + size, Avx.Permute2x128(t2, t4, 0b00110001));
                }
            }
            for (int r = s1; r < size; r++)
            {
                double* src = a + r * size;
                double* dst = a + r;
                for (int c = 0; c < s1; c++)
                    (src[c], dst[c * size]) = (dst[c * size], src[c]);
            }
            for (int r = s1; r < size; r++)
                for (int c = s1; c < r; c++)
                    (a[r * size + c], a[c * size + r]) = (a[c * size + r], a[r * size + c]);
        }
        else
        {
            double* b = a;
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < row; col++)
                    (a[col * size + row], b[col]) = (b[col], a[col * size + row]);
                b += size;
            }
        }
    }

    /// <summary>Computes the maximum difference between two arrays.</summary>
    /// <param name="p">First array.</param>
    /// <param name="q">Second array.</param>
    /// <param name="length">Common length of the arrays.</param>
    /// <returns>The max-norm of the vector difference.</returns>
    public unsafe static double Distance(double* p, double* q, int length)
    {
        double max = 0;
        int i = 0;
        if (Avx.IsSupported)
        {
            Vector256<double> z = Vector256<double>.Zero, vm = z;
            for (int top = length & Simd.AVX_MASK; i < top; i += 4)
            {
                Vector256<double> v = Avx.LoadVector256(p + i);
                vm = Avx.Max(Avx.Max(v, Avx.Subtract(z, v)), vm);
            }
            max = vm.Max();
        }
        for (; i < length; i++)
        {
            double v = Abs(p[i] - q[i]);
            if (v > max)
                max = v;
        }
        return max;
    }

    /// <summary>Gets a text representation of an array.</summary>
    /// <param name="data">An array from a vector.</param>
    /// <param name="formatter">A formatter for items.</param>
    /// <returns>A text representation of the vector.</returns>
    public static string ToString<T>(T[] data, Func<T, string> formatter)
        where T : struct
    {
        if (data.Length == 0)
            return "";
        string[] cells = data.Select(formatter).ToArray();
        int width = Max(3, cells.Max(c => c.Length));
        int cols = (80 + 2) / (width + 2);
        StringBuilder sb = new(Min(data.Length/ cols, 12) * 82);
        int offset = 0;
        for (int row = 0; row < 11 && offset < data.Length; row++)
        {
            for (int col = 0; col < cols && offset < data.Length; col++, offset++)
            {
                sb.Append(cells[offset].PadLeft(width));
                if (col < cols - 1)
                    sb.Append("  ");
            }
            sb.AppendLine();
        }
        if (offset < data.Length)
        {
            if (data.Length - offset <= cols)
                for (int col = 0; col < cols && offset < data.Length; col++, offset++)
                {
                    sb.Append(cells[offset].PadLeft(width));
                    if (col < cols - 1)
                        sb.Append("  ");
                }
            else
            {
                for (int col = 0; col < cols - 2; col++, offset++)
                {
                    sb.Append(cells[offset].PadLeft(width));
                    if (col < cols - 1)
                        sb.Append("  ");
                }
                sb.Append("...".PadLeft(width))
                    .Append("  ")
                    .Append(cells[^1].PadLeft(width));
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>Gets a text representation of a 2D-array.</summary>
    /// <param name="data">A 2D-array from a matrix.</param>
    /// <param name="formatter">Converts items to text.</param>
    /// <returns>A text representation of the matrix.</returns>
    public static string ToString(double[,] data, Func<double, string> formatter)
    {
        const int upperRows = 8, lowerRows = 4, minLeftColumns = 5, rightColumns = 2;
        int rowCount = data.GetLength(0), colCount = data.GetLength(1);

        int upper = rowCount <= upperRows ? rowCount : upperRows;
        int lower = rowCount <= upperRows
            ? 0
            : rowCount <= upperRows + lowerRows
            ? rowCount - upperRows
            : lowerRows;
        bool rowEllipsis = rowCount > upper + lower;
        int rows = rowEllipsis ? upper + lower + 1 : upper + lower;

        int left = colCount <= minLeftColumns ? colCount : minLeftColumns;
        int right = colCount <= minLeftColumns
            ? 0
            : colCount <= minLeftColumns + rightColumns
            ? colCount - minLeftColumns
            : rightColumns;

        List<(int, string[])> columnsLeft = new(left);
        for (int j = 0; j < left; j++)
            columnsLeft.Add(FormatColumn(j, rows, upper, lower));

        List<(int, string[])> columnsRight = new(right);
        for (int j = 0; j < right; j++)
            columnsRight.Add(FormatColumn(colCount - right + j, rows, upper, lower));

        int chars = columnsLeft.Sum(t => t.Item1 + 2) + columnsRight.Sum(t => t.Item1 + 2);
        for (int j = left; j < colCount - right; j++)
        {
            (int, string[]) candidate = FormatColumn(j, rows, upper, lower);
            chars += candidate.Item1 + 2;
            if (chars > 76)
                break;
            columnsLeft.Add(candidate);
        }

        int cols = columnsLeft.Count + columnsRight.Count;
        bool colEllipsis = colCount > cols;
        if (colEllipsis)
            cols++;

        string[,] array = new string[rows, cols];
        int colIndex = 0;
        foreach ((int, string[]) column in columnsLeft)
        {
            for (int i = 0; i < column.Item2.Length; i++)
                array[i, colIndex] = column.Item2[i];
            colIndex++;
        }
        if (colEllipsis)
        {
            int rowIndex = 0;
            for (int row = 0; row < upper; row++)
                array[rowIndex++, colIndex] = "..";
            if (rowEllipsis)
                array[rowIndex++, colIndex] = "..";
            for (int row = rowCount - lower; row < rowCount; row++)
                array[rowIndex++, colIndex] = "..";
            colIndex++;
        }
        foreach ((int, string[]) column in columnsRight)
        {
            for (int i = 0; i < column.Item2.Length; i++)
                array[i, colIndex] = column.Item2[i];
            colIndex++;
        }
        return FormatStringArrayToString(array);

        (int, string[]) FormatColumn(int column, int height, int upper, int lower)
        {
            string[] c = new string[height];
            int index = 0;
            for (int row = 0; row < upper; row++)
                c[index++] = formatter(data[row, column]);
            if (rowEllipsis)
                c[index++] = "";
            int rowCount = data.GetLength(0);
            for (int row = rowCount - lower; row < rowCount; row++)
                c[index++] = formatter(data[row, column]);
            int w = c.Max(x => x.Length);
            if (rowEllipsis)
                c[upper] = "..";
            return (w, c);
        }

        static string FormatStringArrayToString(string[,] data)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1);
            Span<int> widths = stackalloc int[cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    widths[j] = Max(widths[j], data[i, j].Length);
            StringBuilder sb = new();
            for (int i = 0; i < rows; i++)
            {
                sb.Append(data[i, 0].PadLeft(widths[0]));
                for (int j = 1; j < cols; j++)
                    sb.Append("  ").Append(data[i, j].PadLeft(widths[j]));
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
