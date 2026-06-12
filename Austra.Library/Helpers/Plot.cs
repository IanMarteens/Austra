namespace Austra.Library;

/// <summary>Contains dataset information for plots.</summary>
/// <typeparam name="T">The type of the contained datasets.</typeparam>
public class Plot<T> : IFormattable where T: IFormattable
{
    /// <summary>First element to be plotted.</summary>
    public T First { get;}
    /// <summary>Optional second element to be plotted.</summary>
    public T? Second { get; }
    /// <summary>Has the second dataset been assigned?</summary>
    public bool HasSecond { get; }

    /// <summary>Creates a plot for one dataset.</summary>
    /// <param name="first">Dataset to be plotted.</param>
    public Plot(T first) => First = first;

    /// <summary>Creates a plot for comparing two datasets.</summary>
    /// <param name="first">First dataset.</param>
    /// <param name="second">Second dataset.</param>
    public Plot(T first, T second) => (First, Second, HasSecond) = (first, second, true);

    /// <summary>Gets a textual representation of the datasets in this plot.</summary>
    /// <returns>The combined </returns>
    public override string ToString()
    {
        string s = (Second is null
            ? First!.ToString()
            : First!.ToString() + Environment.NewLine + Second!.ToString())!;
        return typeof(T) == typeof(Complex) || typeof(T) == typeof(CVector)
            ? s.Replace('<', '(').Replace('>', ')') : s;
    }

    /// <summary>Gets a textual representation of the datasets in this plot.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider)
    {
        string s = (Second is null
            ? First!.ToString(format, provider)
            : First!.ToString(format, provider) + Environment.NewLine + Second!.ToString(format, provider))!;
        return typeof(T) == typeof(Complex) || typeof(T) == typeof(CVector)
            ? s.Replace('<', '(').Replace('>', ')') : s;
    }
}

/// <summary>
/// Contains the information necessary to plot a function, including the range of x values,
/// the resolution, and the function itself.
/// </summary>
/// <param name="lowX">Low X bound.</param>
/// <param name="highX">High X bound.</param>
/// <param name="resolution">Number of points to plot.</param>
/// <param name="function">The function to plot.</param>
public class ChartSource(double lowX, double highX, int resolution, Func<double, double> function)
{
    /// <summary>Creates a new <see cref="ChartSource"/> with a predefined resolution.</summary>
    /// <param name="lowX">Low X bound.</param>
    /// <param name="highX">High X bound.</param>
    /// <param name="function">The function to plot.</param>
    public ChartSource(double lowX, double highX, Func<double, double> function) :
        this(lowX, highX, 512, function)
    { }

    /// <summary>Low X bound.</summary>
    public double LowX { get; } = lowX;
    /// <summary>High X bound.</summary>
    public double HighX { get; } = highX;
    /// <summary>Number of points to plot.</summary>
    public int Resolution { get; } = resolution;
    /// <summary>The function to plot.</summary>
    public Func<double, double> Function { get; } = function;

    /// <summary>
    /// Creates a spline interpolation of the function defined by this <see cref="ChartSource"/>.
    /// </summary>
    /// <returns>A <see cref="VectorSpline"/> representing the interpolated function.</returns>
    public VectorSpline GetSpline() => new(LowX, HighX, Resolution, Function);
}
