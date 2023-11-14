namespace Austra.Library;

/// <summary>Contains dataset information for plots.</summary>
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
    public override string ToString() => (Second is null
        ? First!.ToString()
        : First!.ToString() + Environment.NewLine + Second!.ToString())!;

    /// <summary>Gets a textual representation of the datasets in this plot.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Space-separated components.</returns>
    public string ToString(string? format, IFormatProvider? provider) => (Second is null
        ? First!.ToString(format, provider)
        : First!.ToString(format, provider) + Environment.NewLine + Second!.ToString(format, provider))!;
}
