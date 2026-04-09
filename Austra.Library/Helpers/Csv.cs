using System;
using System.IO;

namespace Austra.Library;

/// <summary>Allows configuration and reading from a CSV file.</summary>
public class Csv
{
    /// <summary>Full path to the CSV file.</summary>
    private readonly string filename;
    /// <summary>Format provider used for parsing numeric and date values.</summary>
    private IFormatProvider formatProvider = CultureInfo.CurrentCulture;
    /// <summary>
    /// The separator for this CSV file. By default, this is a comma, but it can be changed to support other formats.
    /// </summary>
    private string separator = ",";
    /// <summary>
    /// Does this CSV file have a header line? If true, the first line will be ignored when reading.
    /// </summary>
    private bool hasHeader;
    /// <summary>Either the name of the column to filter by, or a null string.</summary>
    private string? filterColumn;
    /// <summary>
    /// Either the index of the column to filter by, or -1 if no index-based filtering is configured.
    /// </summary>
    private int filterIndex = -1;
    /// <summary>
    /// The value used for filtering, or a null string if no filtering is configured.
    /// </summary>
    private string? filterValue;

    /// <summary>Creates a new CSV reader for the specified file.</summary>
    /// <param name="fileName">Path to the CSV file.</param>
    public Csv(string fileName)
    {
        if (!Path.IsPathFullyQualified(fileName))
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string f = Path.Combine(documents, fileName);
            if (File.Exists(f))
                fileName = f;
            else
            {
                f = Path.Combine(documents, "austra", fileName);
                if (File.Exists(f))
                    fileName = f;
                else
                {
                    f = Path.Combine(Environment.CurrentDirectory, fileName);
                    if (File.Exists(f))
                        fileName = f;
                }
            }
        }
        filename = fileName;
    }

    /// <summary>
    /// Mark this CSV file as having a header line. The first line will be ignored when reading.
    /// </summary>
    /// <returns>A new instance of this class.</returns>
    public Csv WithHeader()
    {
        var result = MemberwiseClone() as Csv;
        result!.hasHeader = true;
        return result;
    }

    /// <summary>
    /// Change the separator character for this CSV file.
    /// </summary>
    /// <param name="separator">The new separator character.</param>
    /// <returns>A new instance of this class.</returns>
    public Csv WithSeparator(string separator)
    {
        var result = MemberwiseClone() as Csv;
        result!.separator = separator;
        return result;
    }

    /// <summary>
    /// Change the format provider for this CSV file. This is used when parsing numeric values.
    /// </summary>
    /// <param name="cultureId">The ID of the new format provider.</param>
    /// <returns>A new instance of this class.</returns>
    public Csv WithFormat(string cultureId)
    {
        var result = MemberwiseClone() as Csv;
        result!.formatProvider = new CultureInfo(cultureId);
        return result;
    }

    /// <summary>
    /// Instructs the reader to only return lines where the value in the specified column matches the provided value.
    /// This overload implies that the CSV file has a header.
    /// </summary>
    /// <param name="columnName">The name of the column,</param>
    /// <param name="value">The value to match.</param>
    /// <returns>A new instance of this class.</returns>
    public Csv WithFilter(string columnName, string value)
    {
        var result = MemberwiseClone() as Csv;
        result!.hasHeader = true;
        result!.filterColumn = columnName;
        result!.filterIndex = -1;
        result!.filterValue = value;
        return result;
    }

    /// <summary>
    /// Instructs the reader to only return lines where the value in the specified column matches the provided value.
    /// </summary>
    /// <param name="columnIndex">The index of the filtering column.</param>
    /// <param name="value">The value to match.</param>
    /// <returns>A new instance of this class.</returns>
    public Csv WithFilter(int columnIndex, string value)
    {
        var result = MemberwiseClone() as Csv;
        result!.filterColumn = null;
        result!.filterIndex = columnIndex;
        result!.filterValue = value;
        return result;
    }

    /// <summary>Gets the start and end indices of the value in the specified column.</summary>
    /// <param name="from">The index to start searching from.</param>
    /// <param name="line">The line to search.</param>
    /// <param name="columnIndex">The index of the column to search for.</param>
    /// <returns>The start and end positions.</returns>
    private (int from, int to) GetColumnBounds(int from, string line, int columnIndex)
    {
        while (columnIndex-- > 0 && from >= 0)
            from = line.IndexOf(separator, from + 1);
        if (from < 0)
            return (-1, -1);
        from++;
        int to = line.IndexOf(separator, from);
        return (from, to < 0 ? line.Length : to);
    }

    /// <summary>
    /// Filters and project a substring and converts it to a provided type.
    /// </summary>
    /// <typeparam name="T">The conversion target type.</typeparam>
    /// <param name="s">The line being analyzed.</param>
    /// <param name="columnIndex">Index of the column to extract.</param>
    /// <param name="filtering">Do we have a filter?</param>
    /// <param name="value">The result value, when successful.</param>
    /// <returns>True when succeeds.</returns>
    private bool TryParse<T>(string s, int columnIndex, bool filtering, out T value)
        where T : struct, ISpanParsable<T>
    {
        value = default;
        if (filtering)
        {
            var (from, to) = GetColumnBounds(0, s, filterIndex);
            if (from >= 0 && s.AsSpan(from, to - from)
                .Equals(filterValue, StringComparison.OrdinalIgnoreCase))
            {
                (from, to) = GetColumnBounds(0, s, columnIndex);
                return from >= 0
                    && T.TryParse(s.AsSpan(from, to - from), formatProvider, out value);
            }
            return false;
        }
        else
        {
            var (from, to) = GetColumnBounds(0, s, columnIndex);
            return from >= 0
                && T.TryParse(s.AsSpan(from, to - from), formatProvider, out value);
        }
    }

    /// <summary>Reads all lines from the configured CSV file.</summary>
    /// <typeparam name="T">The type to convert the column values to.</typeparam>
    /// <param name="columnName">The name of a numeric column to return.</param>
    /// <returns>A sequence of possibly filtered lines.</returns>
    public IEnumerable<T> ReadColumn<T>(string columnName)
        where T : struct, ISpanParsable<T>
    {
        bool filtering = filterValue is not null && (filterIndex >= 0 || filterColumn is not null);
        // Assume that we have a header.
        bool headerRead = false;
        int selectedIndex = -1;
        foreach (string s in File.ReadLines(filename))
        {
            if (!headerRead)
            {
                string[] headers = s.Split(separator);
                if (filtering && filterColumn is not null)
                {
                    filterIndex = Array.IndexOf(headers, filterColumn);
                    if (filterIndex < 0)
                    {
                        filtering = false;
                    }
                }
                selectedIndex = Array.IndexOf(headers, columnName);
                if (selectedIndex < 0)
                {
                    yield break;
                }
                // Skip the header line.
                headerRead = true;
                continue;
            }
            if (TryParse(s, selectedIndex, filtering, out T value))
                yield return value;
        }
    }

    /// <summary>Reads all lines from the configured CSV file.</summary>
    /// <typeparam name="T">The type to convert the column values to.</typeparam>
    /// <param name="columnIndex">The index of a numeric column to return.</param>
    /// <returns>A sequence of possibly filtered lines.</returns>
    public IEnumerable<T> ReadColumn<T>(int columnIndex)
        where T : struct, ISpanParsable<T>
    {
        bool filtering = filterValue is not null && (filterIndex >= 0 || filterColumn is not null);
        // Assume that we have a header.
        bool headerRead = !hasHeader;
        if (columnIndex < 0)
            yield break;
        foreach (string s in File.ReadLines(filename))
        {
            if (!headerRead)
            {
                string[] headers = s.Split(separator);
                if (filtering && filterColumn is not null)
                {
                    filterIndex = Array.IndexOf(headers, filterColumn);
                    if (filterIndex < 0)
                    {
                        filtering = false;
                    }
                }
                // Skip the header line.
                headerRead = true;
                continue;
            }
            if (TryParse(s, columnIndex, filtering, out T value))
                yield return value;
        }
    }

    /// <summary>
    /// Reads all rows from the configured CSV file and returns all values as an array of doubles,
    /// along with the number of columns per row. Applies any configured filtering and handles headers as specified.
    /// </summary>
    /// <returns>
    /// A tuple containing an array of all double values in the file and the number of columns per row.
    /// </returns>
    public (double[] items, int columns) ReadAll()
    {
        bool filtering = filterValue is not null && (filterIndex >= 0 || filterColumn is not null);
        bool headerRead = !hasHeader;
        List<double> result = [];
        int columns = -1;
        foreach (string s in File.ReadLines(filename))
        {
            if (!headerRead)
            {
                string[] headers = s.Split(separator);
                if (filtering && filterColumn is not null)
                {
                    filterIndex = Array.IndexOf(headers, filterColumn);
                    if (filterIndex < 0)
                    {
                        filtering = false;
                    }
                }
                // Skip the header line.
                headerRead = true;
                columns = headers.Length;
                continue;
            }
            // From now on, we expect a data row.
            if (columns < 0)
                columns = s.Split(separator).Length;
            int saveLength = result.Count;
            for (int c = 0, from = 0; c < columns; c++)
            {
                int to = s.IndexOf(separator, from);
                if (to < 0)
                    to = s.Length;
                ReadOnlySpan<char> span = s.AsSpan(from, to - from);
                if (filtering && filterIndex == c
                    && !span.Equals(filterValue, StringComparison.OrdinalIgnoreCase))
                {
                    result.RemoveRange(saveLength, result.Count - saveLength);
                    break;
                }
                if (double.TryParse(span, formatProvider, out double d))
                    result.Add(d);
                else
                    result.Add(0.0);
                from = to + 1;
            }
        }
        return (result.ToArray(), columns);
    }

    /// <summary>
    /// Reads all rows from the configured CSV file and returns all values as an array of doubles,
    /// along with the number of columns per row. Applies any configured filtering and handles headers as specified.
    /// </summary>
    /// <param name="columnIndexes">The indexes of the columns to parse and return.</param>
    /// <returns>
    /// A tuple containing an array of all double values in the file and the number of columns per row.
    /// </returns>
    public (double[] items, int columns) ReadAll(params int[] columnIndexes)
    {
        bool filtering = filterValue is not null && (filterIndex >= 0 || filterColumn is not null);
        bool headerRead = !hasHeader;
        List<double> result = [];
        int[]? map = null;
        double[] buffer = new double[columnIndexes.Length];
        int columns = -1;
        foreach (string s in File.ReadLines(filename))
        {
            if (!headerRead)
            {
                string[] headers = s.Split(separator);
                if (filtering && filterColumn is not null)
                {
                    filterIndex = Array.IndexOf(headers, filterColumn);
                    if (filterIndex < 0)
                        filtering = false;
                }
                // Skip the header line.
                headerRead = true;
                columns = headers.Length;
                continue;
            }
            // From now on, we expect a data row.
            if (columns < 0)
                columns = s.Split(separator).Length;
            if (map == null)
            {
                map = new int[columns];
                for (int i = 0, m = 1; i < columnIndexes.Length; i++, m <<= 1)
                {
                    int idx = columnIndexes[i];
                    if ((uint)idx >= map.Length)
                        throw new ArgumentOutOfRangeException(
                            nameof(columnIndexes),
                            $"Column index {idx} is out of range for a file with {columns} columns.");
                    map[idx] |= m;
                }
            }
            Array.Clear(buffer, 0, buffer.Length);
            bool assigned = false;
            for (int c = 0, from = 0; c < columns; c++)
            {
                int to = s.IndexOf(separator, from);
                if (to < 0)
                    to = s.Length;
                ReadOnlySpan<char> span = s.AsSpan(from, to - from);
                if (filtering && filterIndex == c
                    && !span.Equals(filterValue, StringComparison.OrdinalIgnoreCase))
                {
                    assigned = false;
                    break;
                }
                if (map[c] != 0)
                {
                    assigned = true;
                    _ = double.TryParse(span, formatProvider, out double d);
                    for (int i = 0, m = 1; i < buffer.Length; i++, m <<= 1)
                        if ((map[c] & m) != 0)
                            buffer[i] = d;
                }
                from = to + 1;
            }
            if (assigned)
                result.AddRange(buffer);
        }
        return (result.ToArray(), columnIndexes.Length);
    }

    /// <summary>Read points for a series from the configured CSV file.</summary>
    /// <param name="dateIndex">Index of column containing the dates.</param>
    /// <param name="valueIndex">Index of column containing the values.</param>
    /// <returns>The list of points for the series.</returns>
    public Point<Date>[] ReadSeries(int dateIndex, int valueIndex)
    {
        bool filtering = filterValue is not null && (filterIndex >= 0 || filterColumn is not null);
        bool headerRead = !hasHeader;
        int columns = -1;
        List<Point<Date>> result = [];
        foreach (string s in File.ReadLines(filename))
        {
            if (!headerRead)
            {
                string[] headers = s.Split(separator);
                if (filtering && filterColumn is not null)
                {
                    filterIndex = Array.IndexOf(headers, filterColumn);
                    if (filterIndex < 0)
                        filtering = false;
                }
                // Skip the header line.
                headerRead = true;
                columns = headers.Length;
                continue;
            }
            if (columns < 0)
                columns = s.Split(separator).Length;
            Date? date = null;
            double? value = null;
            for (int c = 0, from = 0; c < columns; c++)
            {
                int to = s.IndexOf(separator, from);
                if (to < 0)
                    to = s.Length;
                ReadOnlySpan<char> span = s.AsSpan(from, to - from);
                if (filtering && filterIndex == c
                    && !span.Equals(filterValue, StringComparison.OrdinalIgnoreCase))
                {
                    date = null;
                    break;
                }
                if (c == dateIndex)
                {
                    if (!Date.TryParse(span, formatProvider, out Date d))
                        break;
                    date = d;
                }
                else if (c == valueIndex)
                {
                    if (!double.TryParse(span, formatProvider, out double d))
                        break;
                    value = d;
                }
                from = to + 1;
            }
            if (date is not null && value is not null)
                result.Add(new Point<Date>(date.Value, value.Value));
        }
        return [.. result];
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        string result = filename;
        if (hasHeader)
            result += ", with headers";
        result += $", separator: '{separator}'";
        if (formatProvider is CultureInfo info)
            result += $", format: {info.DisplayName}";
        return result;
    }
}