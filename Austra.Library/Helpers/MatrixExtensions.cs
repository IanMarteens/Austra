namespace Austra.Library.Helpers;

/// <summary>Implements public common matrix and vector operations.</summary>
public static class MatrixExtensions
{
    /// <summary>Number of characters in a line.</summary>
    public static int TERMINAL_COLUMNS { get; set; } = 80;

    /// <summary>Gets a text representation of a matrix.</summary>
    /// <param name="values">The values to be formatted.</param>
    /// <param name="rowCount">Number of rows.</param>
    /// <param name="colCount">Number of columns.</param>
    /// <param name="formatter">Converts items to text.</param>
    /// <param name="triangularity">Which part of the matrix is significative.</param>
    /// <returns>A text representation of the matrix.</returns>
    public static string ToString(
        this Span<double> values,
        int rowCount, int colCount,
        Func<double, string> formatter, sbyte triangularity)
    {
        const int upperRows = 8, lowerRows = 4, minLeftColumns = 5, rightColumns = 2;

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
            columnsLeft.Add(FormatColumn(values, j, rows, upper, lower));

        List<(int, string[])> columnsRight = new(right);
        for (int j = 0; j < right; j++)
            columnsRight.Add(FormatColumn(values, colCount - right + j, rows, upper, lower));

        int chars = columnsLeft.Sum(t => t.Item1 + 2) + columnsRight.Sum(t => t.Item1 + 2);
        for (int j = left; j < colCount - right; j++)
        {
            (int, string[]) candidate = FormatColumn(values, j, rows, upper, lower);
            chars += candidate.Item1 + 2;
            if (chars > TERMINAL_COLUMNS - 4)
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
        int saveCol = colEllipsis ? colIndex++ : colIndex;
        foreach ((int, string[]) column in columnsRight)
        {
            for (int i = 0; i < column.Item2.Length; i++)
                array[i, colIndex] = column.Item2[i];
            colIndex++;
        }
        if (colEllipsis)
        {
            colIndex = saveCol;
            int rowIndex = 0;
            if (triangularity == 0)
            {
                for (int row = 0; row < upper; row++)
                    array[rowIndex++, colIndex] = "..";
                if (rowEllipsis)
                    array[rowIndex++, colIndex] = "..";
                for (int row = rowCount - lower; row < rowCount; row++)
                    array[rowIndex++, colIndex] = "..";
            }
            else
            {
                (_, string[] refCol) = triangularity < 0 ? columnsLeft[^1] : columnsRight[0];
                for (int row = 0; row < upper; row++, rowIndex++)
                    if (refCol[rowIndex] != "")
                        array[rowIndex, colIndex] = "..";
                if (rowEllipsis)
                    if (triangularity < 0)
                        rowIndex++;
                    else
                        array[rowIndex++, colIndex] = "..";
                for (int row = rowCount - lower; row < rowCount; row++, rowIndex++)
                    if (refCol[rowIndex] != "")
                        array[rowIndex, colIndex] = "..";
            }
        }
        return FormatStringArrayToString(array);

        string Filter(double value, int row, int column) =>
            triangularity switch
            {
                1 => row > column ? "" : formatter(value),
                -1 => row < column ? "" : formatter(value),
                _ => formatter(value)
            };

        (int, string[]) FormatColumn(Span<double> values, int column, int height, int upper, int lower)
        {
            string[] c = new string[height];
            int index = 0;
            for (int row = 0; row < upper; row++)
                c[index++] = Filter(values[row * colCount + column], row, column);
            if (rowEllipsis)
                c[index++] = "";
            for (int row = rowCount - lower; row < rowCount; row++)
                c[index++] = Filter(values[row * colCount + column], row, column);
            int w = c.Max(x => x.Length);
            if (rowEllipsis)
                if (triangularity == 0 ||
                    triangularity < 0 && column <= upper ||
                    triangularity > 0 && column >= upper)
                    c[upper] = "..";
                else
                    c[upper] = "";
            return (w, c);
        }

        static string FormatStringArrayToString(string[,] data)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1);
            Span<int> widths = stackalloc int[cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    widths[j] = Math.Max(widths[j], data[i, j]?.Length ?? 0);
            StringBuilder sb = new();
            for (int i = 0; i < rows; i++)
            {
                sb.Append(data[i, 0].PadLeft(widths[0]));
                for (int j = 1; j < cols; j++)
                    sb.Append("  ").Append((data[i, j] ?? "").PadLeft(widths[j]));
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
