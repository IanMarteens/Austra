namespace Austra.Library;

/// <summary>Allowed tolerances for floating numbers.</summary>
internal static class Tolerance
{
    /// <summary>
    /// Maximum relative precision of IEEE 754 double-precision numbers (64 bit).
    /// </summary>
    public static readonly double DoublePrecision = Pow(2, -53);

    /// <summary>
    /// Value representing 10 * 2^(-53) = 1.11022302462516E-15
    /// </summary>
    public static readonly double DefaultDoubleAccuracy = DoublePrecision * 10;

    /// <summary>Checks whether a complex is almost zero.</summary>
    /// <param name="a">Number to check.</param>
    /// <returns>True if value is no greater than 10 * 2^(-52); false otherwise.</returns>
    public static bool AlmostZero(this Complex a)
    {
        double norm = (a.Real * a.Real) + (a.Imaginary * a.Imaginary);
        if (double.IsInfinity(norm) || double.IsNaN(norm))
        {
            return false;
        }
        return Abs(norm) < DefaultDoubleAccuracy;
    }
}
