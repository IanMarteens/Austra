namespace Austra.Library.Stats;

/// <summary>Contains statistical and other useful math functions.</summary>
public static partial class Functions
{
    /// <summary>
    /// Compute the inverse of the standard normal cumulative distribution function.
    /// </summary>
    /// <param name="p">A probability value, belonging to interval [0,1].</param>
    /// <returns>
    /// An approximation to the x value satisfying p = Pr{Z &lt;= x} where Z is a 
    /// random variable following a standard normal distribution law.
    /// </returns>
    public static double Probit(double p)
    {
        // Checks
        if (p <= 0.0 || p >= 1.0)
        {
            return
                p == 0.0
                ? double.NegativeInfinity
                : p == 1.0
                ? double.PositiveInfinity :
                throw new ArgumentException($"Invalid argument: {p}");
        }

        double q = p - 0.5;
        if (Abs(q) <= 0.425)
        {
            // P CLOSE TO 1/2
            double s = 0.180625 - q * q;
            double num = FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(s, 2.5090809287301226727E3, 3.3430575583588128105E4), s, 6.7265770927008700853E4), s , 4.5921953931549871457E4), s, 1.3731693765509461125E4), s, 1.9715909503065514427E3), s, 1.3314166789178437745E2), s, 3.3871328727963666080E0);
            double den = FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(s, 5.2264952788528545610E3, 2.8729085735721942674E4), s, 3.9307895800092710610E4), s, 2.1213794301586595867E4), s, 5.3941960214247511077E3), s, 6.8718700749205790830E2), s, 4.2313330701600911252E1), s, 1.0);
            return q * num / den;
        }

        double r = Sqrt(-Log(q < 0.0 ? p : 1.0 - p));
        double ppnd16;
        if (r <= 5)
        {
            // P NEITHER CLOSE TO 1/2 NOR 0 OR 1
            r -= 1.6;
            double num = FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(r, 7.74545014278341407640E-4, 2.27238449892691845833E-2), r, 2.41780725177450611770E-1), r, 1.27045825245236838258E0), r, 3.64784832476320460504E0), r, 5.76949722146069140550E0), r, 4.63033784615654529590E0), r, 1.42343711074968357734E0);
            double den = FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(r, 1.05075007164441684324E-9, 5.47593808499534494600E-4), r, 1.51986665636164571966E-2), r, 1.48103976427480074590E-1), r, 6.89767334985100004550E-1), r, 1.67638483018380384940E0), r, 2.05319162663775882187E0), r, 1.0);
            ppnd16 = num / den;
        }
        else
        {
            // COEFFICIENTS FOR P NEAR 0 OR 1
            r -= 5.0;
            double num = FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(r, 2.01033439929228813265E-7, 2.71155556874348757815E-5), r, 1.24266094738807843860E-3), r, 2.65321895265761230930E-2), r, 2.96560571828504891230E-1), r, 1.78482653991729133580E0), r, 5.46378491116411436990E0), r, 6.65790464350110377720E0);
            double den = FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(r, 2.04426310338993978564E-15, 1.42151175831644588870E-7), r, 1.84631831751005468180E-5), r, 7.86869131145613259100E-4), r, 1.48753612908506148525E-2), r, 1.36929880922735805310E-1), r, 5.99832206555887937690E-1), r, 1.0);
            ppnd16 = num / den;
        }
        return q < 0.0 ? -ppnd16 : ppnd16;
    }

    /// <summary>Numerically stable hypotenuse of a right angle triangle.</summary>
    /// <param name="a">The length of side a of the triangle.</param>
    /// <param name="b">The length of side b of the triangle.</param>
    /// <returns>Returns sqrt(a^2 + b^2) without underflow/overflow.</returns>
    public static double Hypotenuse(double a, double b)
    {
        a = Abs(a); b = Abs(b);
        if (a > b)
        {
            double r = b / a;
            return a * Sqrt(FusedMultiplyAdd(r, r, 1d));
        }
        if (b != 0.0)
        {
            double r = a / b;
            return b * Sqrt(FusedMultiplyAdd(r, r, 1d));
        }
        return 0.0;
    }

    /// <summary>Numerically stable hypotenuse of a right angle triangle.</summary>
    /// <param name="a">The length of side a of the triangle.</param>
    /// <returns>Returns sqrt(1 + a^2) without underflow/overflow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Hypotenuse(double a)
    {
        a = Abs(a);
        if (a > 1)
        {
            double r = 1 / a;
            return a * Sqrt(FusedMultiplyAdd(r, r, 1d));
        }
        return Sqrt(FusedMultiplyAdd(a, a, 1d));
    }

    /// <summary>The order of the <see cref="GammaLn"/> approximation.</summary>
    private const int GammaN = 10;

    /// <summary>
    /// Auxiliary variable when evaluating the <see cref="GammaLn"/> function.
    /// </summary>
    private const double GammaR = 10.900511;

    /// <summary>The number 2 * sqrt(e / pi)</summary>
    private const double TwoSqrtEOverPi = 1.8603827342052657173362492472666631120594218414085755;

    /// <summary>The number log[e](pi)</summary>
    private const double LnPi = 1.1447298858494001741434273513530587116472948129153d;

    /// <summary>The number log(2 * sqrt(e / pi))</summary>
    private const double LogTwoSqrtEOverPi = 0.6207822376352452223455184457816472122518527279025978;

    /// <summary>
    /// Polynomial coefficients for the <see cref="GammaLn"/> approximation.
    /// </summary>
    private static ReadOnlySpan<double> GammaDk => new double[]
    {
        2.48574089138753565546e-5,
        1.05142378581721974210,
        -3.45687097222016235469,
        4.51227709466894823700,
        -2.98285225323576655721,
        1.05639711577126713077,
        -1.95428773191645869583e-1,
        1.70970543404441224307e-2,
        -5.71926117404305781283e-4,
        4.63399473359905636708e-6,
        -2.71994908488607703910e-9
    };

    /// <summary>Computes the logarithm of the Gamma function.</summary>
    /// <param name="z">The argument of the gamma function.</param>
    /// <returns>The logarithm of the gamma function.</returns>
    public static double GammaLn(double z)
    {
        double s = GammaDk[0];
        if (z < 0.5)
        {
            for (int i = 1; i <= GammaN; i++)
            {
                s += GammaDk[i] / (i - z);
            }
            return LnPi - Log(Sin(PI * z)) - Log(s)
                - LogTwoSqrtEOverPi - ((0.5 - z) * Log((0.5 - z + GammaR) / E));
        }
        else
        {
            for (int i = 1; i <= GammaN; i++)
                s += GammaDk[i] / (z + i - 1.0);
            return Log(s) + LogTwoSqrtEOverPi
                + ((z - 0.5) * Log((z - 0.5 + GammaR) / E));
        }
    }

    /// <summary>Computes the Gamma function.</summary>
    /// <param name="z">The argument of the gamma function.</param>
    /// <returns>The gamma function.</returns>
    public static double Gamma(double z)
    {
        ref double rd = ref MemoryMarshal.GetReference(GammaDk);
        double s = rd;
        if (z < 0.5)
        {
            for (int i = 1; i <= GammaN; i++)
                s += Unsafe.Add(ref rd, i) / (i - z);
            return PI / (Sin(PI * z) * s
                * TwoSqrtEOverPi * Pow((0.5 - z + GammaR) / E, 0.5 - z));
        }
        else
        {
            for (int i = 1; i <= GammaN; i++)
                s += Unsafe.Add(ref rd, i) / (z + i - 1.0);
            return s * TwoSqrtEOverPi * Pow((z - 0.5 + GammaR) / E, z - 0.5);
        }
    }

    /// <summary>Computes the Beta function.</summary>
    /// <param name="p">The first argument of the beta function.</param>
    /// <param name="q">The second argument of the beta function.</param>
    /// <returns>
    /// The value of the Euler's Beta, computed from the <see cref="Gamma"/> function.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Beta(double p, double q) =>
        Gamma(p) * Gamma(q) / Gamma(p + q);

    /// <summary>
    /// Generates a random number from a uniform distribution in the interval [0, 1).
    /// </summary>
    /// <returns>A random number from a uniform distribution in the interval [0, 1).</returns>
    public static double Random() => System.Random.Shared.NextDouble();

    /// <summary>
    /// Generates a random number from a normal distribution with mean 0 and standard deviation 1.
    /// </summary>
    /// <returns>A random number, that can be either positive or negative.</returns>
    public static double NRandom() => NormalRandom.Shared.NextDouble();
}
