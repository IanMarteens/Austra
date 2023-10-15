using System.Globalization;
using System.Windows.Data;

namespace Austra;

public class FormulaConverter : IValueConverter
{
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string formula = (string)value;
        try
        {
            var ans = RootModel.Instance.Environment!.Engine.Eval(formula);
            if (ans.Type == typeof(double) || ans.Type == typeof(int))
                return System.Convert.ToDecimal(ans.Value!);
            return 0M;
        }
        catch
        {
            if (double.TryParse(formula, out double d))
                return (decimal)d;
            return 0M;
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        ((decimal)value).ToString(CultureInfo.InvariantCulture);
}

public class DecConverter : IValueConverter
{
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string v = (string)value;
        if (string.IsNullOrWhiteSpace(v))
            return 0d;
        if (v.EndsWith('.'))
            v += '0';
        if (v.Contains(',') && !v.Contains('.'))
            v = v.Replace(',', '.');
        return double.Parse(v, CultureInfo.InvariantCulture);
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        ((double)value).ToString("F2", CultureInfo.InvariantCulture);
}
