namespace Austra.Library;

/// <summary>Provides methods for working with polynomials.</summary>
public static class Polynomials
{
    /// <summary>Evaluates a polynomial with real coefficients.</summary>
    /// <param name="value">Value to substitute.</param>
    /// <param name="coefficients">Polynomial coefficients.</param>
    /// <returns>The evaluation of the polynomial.</returns>
    public static Complex PolyEval(Complex value, Vector coefficients)
    {
        Complex res = Complex.Zero;
        foreach (double c in coefficients)
            res = new(
                FusedMultiplyAdd(res.Real, value.Real, c - res.Imaginary * value.Imaginary),
                FusedMultiplyAdd(res.Real, value.Imaginary, res.Imaginary * value.Real));
        return res;
    }

    /// <summary>Evaluates a polynomial with real coefficients.</summary>
    /// <param name="value">Value to substitute.</param>
    /// <param name="coefficients">Polynomial coefficients.</param>
    /// <returns>The evaluation of the polynomial.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Complex PolyEval(Complex value, double[] coefficients) =>
        PolyEval(value, new Vector(coefficients));

    /// <summary>Evaluates a polynomial with real coefficients.</summary>
    /// <param name="value">Value to substitute.</param>
    /// <param name="coefficients">Polynomial coefficients.</param>
    /// <returns>The evaluation of the polynomial.</returns>
    public static double PolyEval(double value, Vector coefficients)
    {
        double result = 0.0;
        foreach (double c in coefficients)
            result = FusedMultiplyAdd(result, value, c);
        return result;
    }

    /// <summary>Evaluates a polynomial with real coefficients.</summary>
    /// <param name="value">Value to substitute.</param>
    /// <param name="coefficients">Polynomial coefficients.</param>
    /// <returns>The evaluation of the polynomial.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double PolyEval(double value, double[] coefficients) =>
        PolyEval(value, new Vector(coefficients));

    /// <summary>Evaluates the derivative of a polynomial with real coefficients.</summary>
    /// <param name="value">Value to substitute.</param>
    /// <param name="coefficients">Original polynomial coefficients.</param>
    /// <returns>The evaluation of the derivate of the polynomial.</returns>
    public static Complex PolyDerivative(Complex value, Vector coefficients)
    {
        Complex res = Complex.Zero;
        int k = coefficients.Length - 1;
        for (int i = 0; i < coefficients.Length - 1; i++)
        {
            double c = coefficients[i] * k--;
            res = new(
                FusedMultiplyAdd(res.Real, value.Real, c - res.Imaginary * value.Imaginary),
                FusedMultiplyAdd(res.Real, value.Imaginary, res.Imaginary * value.Real));
        }
        return res;
    }

    /// <summary>Evaluates the derivative of a polynomial with real coefficients.</summary>
    /// <param name="value">Value to substitute.</param>
    /// <param name="coefficients">Original polynomial coefficients.</param>
    /// <returns>The evaluation of the derivate of the polynomial.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Complex PolyDerivative(Complex value, double[] coefficients) =>
        PolyDerivative(value, new Vector(coefficients));


    /// <summary>Evaluates the derivative of a polynomial with real coefficients.</summary>
    /// <param name="value">Value to substitute.</param>
    /// <param name="coefficients">Original polynomial coefficients.</param>
    /// <returns>The evaluation of the derivate of the polynomial.</returns>
    public static double PolyDerivative(double value, Vector coefficients)
    {
        double result = 0.0;
        int k = coefficients.Length - 1;
        for (int i = 0; i < coefficients.Length - 1; i++)
            result = FusedMultiplyAdd(result, value, k-- * coefficients[i]);
        return result;
    }

    /// <summary>Evaluates the derivative of a polynomial with real coefficients.</summary>
    /// <param name="value">Value to substitute.</param>
    /// <param name="coefficients">Original polynomial coefficients.</param>
    /// <returns>The evaluation of the derivate of the polynomial.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double PolyDerivative(double value, double[] coefficients) =>
        PolyDerivative(value, new Vector(coefficients));

    /// <summary>Solves a polynomial equation with real coefficients.</summary>
    /// <param name="coefficients">
    /// Real coefficients of the polynomial.
    /// The first element is the coefficient of the lowest degree term.
    /// </param>
    /// <returns>The array of complex or real roots.</returns>
    public static ComplexVector PolySolve(Vector coefficients) =>
        SpanSolve((double[])coefficients);

    /// <summary>Solves a polynomial equation with real coefficients.</summary>
    /// <param name="coefficients">
    /// Real coefficients of the polynomial.
    /// The first element is the coefficient of the lowest degree term.
    /// </param>
    /// <returns>The array of complex or real roots.</returns>
    public static ComplexVector PolySolve(params double[] coefficients) =>
        SpanSolve(coefficients);

    private static ComplexVector SpanSolve(ReadOnlySpan<double> c) => c.Length switch
    {
        0 => new ComplexVector(0),
        1 => SolveConstant(c),
        2 => SolveLineal(c),
        3 => SolveQuadratic(c),
        4 => SolveCubic(c),
        _ => SolveGeneral(c),
    };

    private static ComplexVector SolveConstant(ReadOnlySpan<double> c) =>
        c[0] == 0
        ? new ComplexVector(0)
        : throw new PolynomialRootsException();

    private static ComplexVector SolveLineal(ReadOnlySpan<double> c) =>
        c[0] == 0
        ? SolveConstant(c[1..])
        : new ComplexVector(-c[1] / c[0]);

    private static ComplexVector SolveQuadratic(ReadOnlySpan<double> c)
    {
        if (c[0] == 0)
            return SolveLineal(c[1..]);
        double discr = c[1] * c[1] - 4 * c[0] * c[2];
        if (discr >= 0)
        {
            double sqrt = Sqrt(discr), den = 0.5 / c[0];
            double first = c[1] >= 0
                ? (-c[1] - sqrt) * den
                : (-c[1] + sqrt) * den;
            double second = c[2] / (c[0] * first);
            return new(first, second);
        }
        else
        {
            double sqrt = Sqrt(-discr), den = 0.5 / c[0];
            return new(
                new Complex(-c[1] * den, sqrt * den),
                new Complex(-c[1] * den, -sqrt * den));
        }
    }

    private static (Complex, Complex, Complex) CubicRoots(this Complex complex)
    {
        double r = Pow(complex.Magnitude, 1d / 3d);
        double theta = complex.Phase / 3;
        return (Complex.FromPolarCoordinates(r, theta),
            Complex.FromPolarCoordinates(r, theta + Tau / 3d),
            Complex.FromPolarCoordinates(r, theta - Tau / 3d));
    }

    private static (Complex, Complex, Complex) CubicRoots(this double real)
    {
        double r = Pow(real, 1d / 3d);
        return (new Complex(r, 0),
            Complex.FromPolarCoordinates(r, Tau / 3d),
            Complex.FromPolarCoordinates(r, -Tau / 3d));
    }

    private static ComplexVector SolveCubic(ReadOnlySpan<double> k)
    {
        if (k[0] == 0)
            return SolveQuadratic(k[1..]);

        ref double rk = ref MemoryMarshal.GetReference(k);
        double a = rk, b = Unsafe.Add(ref rk, 1);
        double c = Unsafe.Add(ref rk, 2), d = Unsafe.Add(ref rk, 3);
        double bb = b * b, ac = a * c, bc = b * c;
        double A = bb - 3 * ac;
        double B = (2 * bb - 9 * ac) * b + 27 * a * a * d;
        double s = 1 / (-3 * a);

        double D = (B * B - 4 * A * A * A) / (-27 * a * a);
        if (D == 0d)
        {
            if (A == 0d)
            {
                Complex u = new(s * b, 0d);
                return new(u, u, u);
            }

            Complex v = new((9 * a * d - bc) / (2 * A), 0d);
            Complex w = new(((4 * bc - 9 * a * d) * a - bb * b) / (a * A), 0d);
            return new(v, v, w);
        }

        (Complex c1, Complex c2, Complex c3) = A == 0
            ? B.CubicRoots()
            : ((B + Complex.Sqrt(B * B - 4 * A * A * A)) / 2).CubicRoots();
        return new(s * (b + c1 + A / c1), s * (b + c2 + A / c2), s * (b + c3 + A / c3));
    }

    private static ComplexVector SolveGeneral(ReadOnlySpan<double> c)
    {
        int n = c.Length - 1;
        double[] companion = new double[n * n];
        double c0 = c[0];
        companion[c.Length - 2] = -c[^1] / c0;
        for (int i = 1; i < c.Length - 1; i++)
        {
            companion[i * n + i - 1] = 1.0;
            companion[i * n + c.Length - 2] = -c[^(i + 1)] / c0;
        }
        return new Matrix(n, n, companion).EVD(false).Values;
    }
}
