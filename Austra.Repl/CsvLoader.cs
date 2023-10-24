using Austra.Library;
using Austra.Library.Dates;

namespace Austra.Parser;

internal static class CsvLoader
{
    public static IEnumerable<Series> Load(string file)
    {
        bool first = true;
        List<SeriesHeader> headers = new();
        List<Date> dates = new();
        int row = 0;
        foreach (string line in File.ReadLines(file))
        {
            string[] parts = line.Split(',');
            if (first)
            {
                // Create series headers.
                foreach (string text in parts)
                    if (!text.Equals("Date", StringComparison.OrdinalIgnoreCase))
                    {
                        string name = text.Replace(' ', '_').Replace('-', '_');
                        if (char.IsDigit(name, 0))
                            name = "S" + name;
                        headers.Add(new SeriesHeader { Name = name });
                    }
                if (headers.Count == 0)
                    return Enumerable.Empty<Series>();
                first = false;
            }
            else
            {
                // Parse date.
                if (!DateTime.TryParseExact(parts[0], "dd/MM/yyyy", null,
                    System.Globalization.DateTimeStyles.None, out DateTime d))
                    break;
                dates.Add((Date)d);
                int incomplete = -1;
                for (int i = 0; i < headers.Count; i++)
                {
                    if (double.TryParse(parts[i + 1],
                        System.Globalization.CultureInfo.InvariantCulture, out double value))
                    {
                        var hdr = headers[i];
                        if (hdr.Values.Count == 0)
                            hdr.Offset = row;
                        hdr.Values.Add(value);
                    }
                    else if (headers[i].Values.Count > 0)
                    {
                        incomplete = i;
                        break;
                    }
                }
                if (incomplete >= 0)
                {
                    dates.RemoveAt(dates.Count - 1);
                    for (int i = 0; i < incomplete; i++)
                    {
                        var list = headers[i].Values;
                        if (list.Count > 0)
                            list.RemoveAt(list.Count - 1);
                    }
                }
            }
        }
        if (dates.Count == 0)
            return Enumerable.Empty<Series>();
        dates.Reverse();
        var args = dates.ToArray();
        var series = new List<Series>();
        Frequency frequency = GetFrequency(dates);
        for (int i = 0; i < headers.Count; i++)
        {
            var hdr = headers[i];
            if (hdr.Values.Count == 0)
                continue;
            hdr.Values.Reverse();
            Date[] arg = hdr.Offset == 0 ? args : args[..^hdr.Offset];
            series.Add(new Series(hdr.Name, null, arg, hdr.Values.ToArray(),
                SeriesType.Raw, frequency));
        }
        return series;
    }

    private static Frequency GetFrequency(List<Date> dates) => dates.Count == 1
        ? Frequency.Other
        : ((double)Math.Abs(dates[0] - dates[^1]) / (dates.Count - 1)) switch
        {
            < 3 => Frequency.Daily,
            < 10 => Frequency.Weekly,
            < 17 => Frequency.Biweekly,
            < 45 => Frequency.Monthly,
            < 75 => Frequency.Bimonthly,
            < 135 => Frequency.Quarterly,
            < 273 => Frequency.Semestral,
            < 540 => Frequency.Yearly,
            _ => Frequency.Other,
        };

    private sealed class SeriesHeader
    {
        public string Name { get; init; } = "";
        public int Offset { get; set; }
        public List<double> Values { get; } = new();
    }
}
