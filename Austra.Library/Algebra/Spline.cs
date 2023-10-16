using static System.Runtime.CompilerServices.Unsafe;

namespace Austra.Library;

/// <summary>Represents a set of splines for cubic interpolation.</summary>
/// <typeparam name="ARG">The type of the abscissa.</typeparam>
/// <remarks>
/// Splines are implemented in AUSTRA using natural cubic splines.
/// </remarks>
public abstract class Spline<ARG> where ARG : struct
{
    /// <summary>Scale for calculating derivatives.</summary>
    private readonly double scale;
    /// <summary>Keeps arguments for interpolating arbitrary values.</summary>
    protected readonly double[] xs;

    /// <summary>Creates a set of splines for cubic interpolation.</summary>
    /// <param name="xs">Arguments.</param>
    /// <param name="ys">Coordinates.</param>
    protected Spline(double[] xs, double[] ys)
    {
        if (xs is null || ys is null)
            throw new ArgumentException("Null arguments in spline.");
        if (xs.Length != ys.Length)
            throw new ArgumentException("The number of coordinates must match the number of arguments.");
        if (xs.Length <= 2)
            throw new ArgumentException("Three points required at least for cubic splines.");
        this.xs = xs;
        LastCoordinate = ys[^1];
        K = new Polynomial[xs.Length - 1];
        scale = K.Length / (xs[^1] - xs[0]);
        CalculateSplines(xs, ys);
    }

    /// <summary>Creates a spline approximating a function in a given interval.</summary>
    /// <param name="x0">Lower interval bound.</param>
    /// <param name="x1">Upper interval bound.</param>
    /// <param name="segments">Number of segments in the interval.</param>
    /// <param name="f">The function to be approximated.</param>
    /// <param name="ys">Returns the calculated coordinates.</param>
    protected Spline(double x0, double x1, int segments, Func<double, double> f, out double[] ys)
    {
        if (segments < 2)
            throw new ArgumentException("At least two segments required.");
        if (x0 >= x1)
            throw new ArgumentException("The first argument must be lesser than the second.");
        xs = GC.AllocateUninitializedArray<double>(segments + 1);
        ys = GC.AllocateUninitializedArray<double>(segments + 1);
        double dx = (x1 - x0) / segments;
        for (int i = 0; i < segments; i++)
            ys[i] = f(xs[i] = x0 + dx * i);
        xs[^1] = x1;
        LastCoordinate = ys[^1] = f(x1);
        K = new Polynomial[segments];
        scale = K.Length / (xs[^1] - xs[0]);
        CalculateSplines(xs, ys);
    }

    private void CalculateSplines(double[] xs, double[] ys)
    {
        ref double rx = ref MemoryMarshal.GetArrayDataReference(xs);
        ref double ry = ref MemoryMarshal.GetArrayDataReference(ys);
        // Allocate temporal and final arrays.
        int n = xs.Length;
        ref double rc = ref MemoryMarshal.GetArrayDataReference(new double[n]);
        ref double rr = ref MemoryMarshal.GetArrayDataReference(new double[n]);
        // First row.
        rc = 0.5;
        double last = rr = 1.5 * (Add(ref ry, 1) - ry) / (Add(ref rx, 1) - rx);
        // Intermediate rows.
        for (int i = 1; i < n - 1; i++)
        {
            double d = Add(ref rx, i);
            double dx1 = 1 / (d - Add(ref rx, i - 1));
            double dx2 = 1 / (Add(ref rx, i + 1) - d);
            double b = 1.0 / ((dx1 + dx2) * 2 - Add(ref rc, i - 1) * dx1);
            Add(ref rc, i) = dx2 * b;
            Add(ref rr, i) = last = (((Add(ref ry, i) - Add(ref ry, i - 1)) * dx1 * dx1 +
                (Add(ref ry, i + 1) - Add(ref ry, i)) * dx2 * dx2) * 3 - last * dx1) * b;
        }
        // Last row.
        Add(ref rr, n - 1) = last = ((Add(ref ry, n - 1) - Add(ref ry, n - 2))
            / (Add(ref rx, n - 1) - Add(ref rx, n - 2)) * 3 - last) / (2 - Add(ref rc, n - 2));
        // Solve by backward substitution and create final coefficients.
        for (int i = n - 2; i >= 0; i--)
        {
            last = Add(ref rr, i) -= Add(ref rc, i) * last;
            double dx1 = Add(ref rx, i + 1) - Add(ref rx, i);
            double dy1 = Add(ref ry, i + 1) - Add(ref ry, i);
            double a = last * dx1 - dy1;
            double b = -Add(ref rr, i + 1) * dx1 + dy1 - a;
            K[i] = new(Add(ref ry, i), dy1 + a, b - a, -b);
        }
    }

    /// <summary>Cubic piecewise polynomials.</summary>
    public Polynomial[] K { get; }

    /// <summary>Gets a piecewise polynomial by its index.</summary>
    /// <param name="index">The index of the polynomial to be retrieved.</param>
    /// <returns>The polynomial for the given segment.</returns>
    public Polynomial GetPoly(Index index) => K[index.GetOffset(K.Length)];

    /// <summary>Gets the number of piecewise polynomials.</summary>
    /// <remarks>
    /// The number of polynomials is always one less than the number of points.
    /// </remarks>
    public int Length => K.Length;

    /// <summary>Keeps the last coordinate for interpolating arbitrary values.</summary>
    public double LastCoordinate { get; }

    /// <summary>Gets the interpolated value at a given argument.</summary>
    /// <param name="x">The new argument.</param>
    /// <returns>The interpolated value.</returns>
    protected double this[double x]
    {
        get
        {
            ref double rx = ref MemoryMarshal.GetArrayDataReference(xs);
            if (x < rx || x > Add(ref rx, xs.Length - 1))
                throw new ArgumentOutOfRangeException(nameof(x));
            if (x == Add(ref rx, xs.Length - 1))
                return LastCoordinate;
            int i = Array.BinarySearch(xs, x);
            if (i < 0)
            {
                i = ~i - 1;
                double xi = Add(ref rx, i);
                return K[i].Eval((x - xi) / (Add(ref rx, i + 1) - xi));
            }
            else
                return K[i].K0;
        }
    }

    /// <summary>Gets the interpolated derivative at a given argument.</summary>
    /// <param name="x">The new argument.</param>
    /// <returns>The cubic approximation to the derivative.</returns>
    protected double Derivative(double x)
    {
        ref double rx = ref MemoryMarshal.GetArrayDataReference(xs);
        if (x < rx || x > Add(ref rx, xs.Length - 1))
            throw new ArgumentOutOfRangeException(nameof(x));
        int i = Array.BinarySearch(xs, x);
        if (i < 0)
        {
            i = ~i - 1;
            double xi = Add(ref rx, i);
            return K[i].Derivative((x - xi) / (Add(ref rx, i + 1) - xi)) * scale;
        }
        else
            return K[i].K1 * scale;
    }

    /// <summary>Gets the lower bound of a segment.</summary>
    /// <param name="idx">Segment index.</param>
    /// <returns>The initial argument of the segment.</returns>
    public abstract ARG From(int idx);

    /// <summary>Gets the upper bound of a segment.</summary>
    /// <param name="idx">Segment index.</param>
    /// <returns>The final argument of the segment.</returns>
    public abstract ARG To(int idx);

    /// <summary>Gets the segment date nearest to a given argument.</summary>
    /// <param name="arg">A value to be searched.</param>
    /// <returns>The lesser or equal nearest segment.</returns>
    public int NearestArg(double arg)
    {
        int i = Array.BinarySearch(xs, arg);
        if (i < 0)
            i = ~i - 1;
        return i;
    }

    /// <summary>Gets a textual representation of the spline.</summary>
    /// <returns>The list of generated polynomials.</returns>
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder(1024)
            .Append("Spline segments: ").Append(Length).AppendLine()
            .Append("First argument: ").Append(FormatArgument(xs[0]))
            .Append(", last argument: ").Append(FormatArgument(xs[^1]))
            .AppendLine();

        if (Length <= 8)
            for (int i = 0; i < Length; i++)
                sb.Append(FormatArgument(xs[i]))
                    .Append(": ").AppendLine(K[i].ToString());
        else
        {
            for (int i = 0; i < 3; i++)
                sb.Append(FormatArgument(xs[i]))
                    .Append(": ").AppendLine(K[i].ToString());
            sb.AppendLine("...");
            for (int i = Length - 3; i < Length; i++)
                sb.Append(FormatArgument(xs[i]))
                    .Append(": ").AppendLine(K[i].ToString());
        }
        return sb.ToString();
    }

    /// <summary>Creates a string representation of an argument.</summary>
    /// <param name="x">A value from the abscissa of the spline.</param>
    /// <returns>The string representation of the argument.</returns>
    protected abstract string FormatArgument(double x);
}

/// <summary>Interpolates temporal series.</summary>
public sealed class DateSpline : Spline<Date>
{
    /// <summary>Creates an interpolator for a series.</summary>
    /// <param name="series">The temporal series to interpolate.</param>
    public DateSpline(Series series) : base(
        series.Args.Reverse().Take(series.Count).Select(x => (double)x).ToArray(),
        series.Values.Reverse().Take(series.Count).ToArray()) =>
        Original = series;

    /// <summary>Original series.</summary>
    public Series Original { get; }

    /// <summary>Gets the interpolated value at a given date.</summary>
    /// <param name="d">A new date to interpolate its value.</param>
    /// <returns>The interpolated value.</returns>
    public double this[Date d] => this[(double)d];

    /// <summary>Gets the interpolated derivative at a given argument.</summary>
    /// <param name="d">The date argument.</param>
    /// <returns>The cubic approximation to the derivative.</returns>
    public double Derivative(Date d) => Derivative((double)d);

    /// <summary>Gets the lower bound of a segment.</summary>
    /// <param name="idx">Segment index.</param>
    /// <returns>The initial date.</returns>
    public override Date From(int idx) => new((uint)xs[idx]);

    /// <summary>Gets the upper bound of a segment.</summary>
    /// <param name="idx">Segment index.</param>
    /// <returns>The final date.</returns>
    public override Date To(int idx) => new((uint)xs[idx + 1]);

    /// <summary>Creates a string representation of an argument.</summary>
    /// <param name="x">A value from the abscissa of the spline.</param>
    /// <returns>The values formatted as a date.</returns>
    protected override string FormatArgument(double x) => new Date((uint)x).ToString();
}

/// <summary>Interpolates vectors.</summary>
public sealed class VectorSpline : Spline<double>
{
    /// <summary>Creates an interpolator for a series.</summary>
    /// <param name="args">Arguments.</param>
    /// <param name="values">Values.</param>
    public VectorSpline(Vector args, Vector values) :
        base((double[])args, (double[])values) =>
        Original = new Series<double>("Original", null,
            (double[])args.Reverse(), (double[])values.Reverse(), SeriesType.Raw);

    /// <summary>Creates a spline approximating a function in a given interval.</summary>
    /// <param name="x0">Lower interval bound.</param>
    /// <param name="x1">Upper interval bound.</param>
    /// <param name="segments">Number of segments in the interval.</param>
    /// <param name="f">The function to be approximated.</param>
    public VectorSpline(double x0, double x1, int segments, Func<double, double> f) :
        base(x0, x1, segments, f, out double[] values) =>
        Original = new Series<double>("Original", null,
            xs.Reverse().ToArray(), values.Reverse().ToArray(), SeriesType.Raw);

    /// <summary>Original series.</summary>
    public Series<double> Original { get; }

    /// <summary>Gets the interpolated value at a given argument.</summary>
    /// <param name="x">The new argument.</param>
    /// <returns>The interpolated value.</returns>
    public new double this[double x] => base[x];

    /// <summary>Gets the interpolated derivative at a given argument.</summary>
    /// <param name="x">The new argument.</param>
    /// <returns>The cubic approximation to the derivative.</returns>
    public new double Derivative(double x) => base.Derivative(x);

    /// <summary>Gets the lower bound of a segment.</summary>
    /// <param name="idx">Segment index.</param>
    /// <returns>The initial date.</returns>
    public override double From(int idx) => xs[idx];

    /// <summary>Gets the upper bound of a segment.</summary>
    /// <param name="idx">Segment index.</param>
    /// <returns>The final date.</returns>
    public override double To(int idx) => xs[idx + 1];

    /// <summary>Creates a string representation of an argument.</summary>
    /// <param name="x">A value from the abscissa of the spline.</param>
    /// <returns>The values formated as a real number.</returns>
    protected override string FormatArgument(double x) => x.ToString("G6");
}

/// <summary>Represents a cubic polynomial.</summary>
/// <remarks>
/// These polynomials admit arguments in the interval [0, 1].
/// </remarks>
/// <param name="K0">Coefficient for 0th-degree term.</param>
/// <param name="K1">Coefficient for 1st-degree term.</param>
/// <param name="K2">Coefficient for 2nd-degree term.</param>
/// <param name="K3">Coefficient for 3rd-degree term.</param>
public readonly record struct Polynomial(double K0, double K1, double K2, double K3)
{
    /// <summary>Evaluates the polynomial at a given argument.</summary>
    /// <param name="t">Argument, in the interval [0, 1].</param>
    /// <returns>The evaluation of the cubic polynomial.</returns>
    public double Eval(double t) => FusedMultiplyAdd(
        FusedMultiplyAdd(FusedMultiplyAdd(K3, t, K2), t, K1), t, K0);

    /// <summary>Evaluates the derivative of the polynomial at a given argument.</summary>
    /// <param name="t">Argument, in the interval [0, 1].</param>
    /// <returns>The evaluation of the derivative.</returns>
    public double Derivative(double t) =>
        FusedMultiplyAdd(FusedMultiplyAdd(3 * K3, t, K2 + K2), t, K1);

    /// <summary>Gets a textual representation of the cubic polynomial.</summary>
    /// <returns>The formatted equation defining the polynomial.</returns>
    public override string ToString() => $"{K3:G6}x³{Format(K2, "x²")}{Format(K1, "x")}{Format(K0, "")}";

    private static string Format(double k, string suffix) => k == 0 ? "" : $" + {k:G6}{suffix}";
}
