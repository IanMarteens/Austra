namespace Austra.Library;

/// <summary>Implements AVX/AVX2/FMA extensions.</summary>
public static class Simd
{
    /// <summary>Mask for <see cref="Vector256{T}"/> iterations.</summary>
    public const int AVX_MASK = 0x_7FFF_FFFC;
    /// <summary>Mask for Vector512 iterations.</summary>
    public const int AVX512_MASK = 0x_7FFF_FFF8;

    /// <summary>The square root of two.</summary>
    public const double SQRT2 = 1.41421356237309504880;

    /// <summary>Sums all the elements in a vector.</summary>
    /// <param name="v">A intrinsics vector with four doubles.</param>
    /// <returns>The total value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Sum(this Vector256<double> v)
    {
        v = Avx.HorizontalAdd(v, v);
        return v.ToScalar() + v.GetElement(2);
    }

    /// <summary>Multiplies all the elements in a vector.</summary>
    /// <param name="v">A intrinsics vector with four doubles.</param>
    /// <returns>The product of all items.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Product(this Vector256<double> v)
    {
        Vector128<double> x = Sse2.Multiply(v.GetLower(), v.GetUpper());
        return x.ToScalar() * x.GetElement(1);
    }

    /// <summary>Gets the maximum component in a vector.</summary>
    /// <param name="v">A intrinsics vector with four doubles.</param>
    /// <returns>The maximum component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Max(this Vector256<double> v)
    {
        Vector128<double> x = Sse2.Max(v.GetLower(), v.GetUpper());
        return Math.Max(x.ToScalar(), x.GetElement(1));
    }

    /// <summary>Gets the minimum component in a vector.</summary>
    /// <param name="v">A intrinsics vector with four doubles.</param>
    /// <returns>The maximum component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Min(this Vector256<double> v)
    {
        Vector128<double> x = Sse2.Min(v.GetLower(), v.GetUpper());
        return Math.Min(x.ToScalar(), x.GetElement(1));
    }

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and addition.
    /// </summary>
    /// <remarks>Must only be called when <c>Avx.IsSupported</c>.</remarks>
    /// <param name="summand">The summand of the fused operation.</param>
    /// <param name="multiplicand">The operation's multiplicand.</param>
    /// <param name="multiplier">The operations's multiplier.</param>
    /// <returns><c>multiplicand * multiplier + summand</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<double> MultiplyAdd(
        this Vector256<double> summand,
        Vector256<double> multiplicand,
        Vector256<double> multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplyAdd(multiplicand, multiplier, summand)
            : Avx.Add(summand, Avx.Multiply(multiplicand, multiplier));

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and addition.
    /// </summary>
    /// <remarks>This version takes also care of loading the multiplicand.</remarks>
    /// <param name="summand">The summand of the fused operation.</param>
    /// <param name="multiplicand">The address if the multiplicand.</param>
    /// <param name="multiplier">The operations's multiplier.</param>
    /// <returns><c>multiplicand * multiplier + summand</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe static Vector256<double> MultiplyAdd(
        this Vector256<double> summand,
        double* multiplicand,
        Vector256<double> multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplyAdd(Avx.LoadVector256(multiplicand), multiplier, summand)
            : Avx.Add(summand, Avx.Multiply(Avx.LoadVector256(multiplicand), multiplier));

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and addition.
    /// </summary>
    /// <remarks>This version takes also care of loading some of the vectors.</remarks>
    /// <param name="summand">The summand of the fused operation.</param>
    /// <param name="multiplicand">The address of the multiplicand.</param>
    /// <param name="multiplier">The address of the multiplier.</param>
    /// <returns><c>multiplicand * multiplier + summand</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe static Vector256<double> MultiplyAdd(
        this Vector256<double> summand,
        double* multiplicand,
        double* multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplyAdd(
                Avx.LoadVector256(multiplicand), Avx.LoadVector256(multiplier), summand)
            : Avx.Add(summand, Avx.Multiply(
                Avx.LoadVector256(multiplicand), Avx.LoadVector256(multiplier)));

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and subtraction.
    /// </summary>
    /// <remarks>Must only be called when <c>Avx.IsSupported</c>.</remarks>
    /// <param name="subtrahend">The subtrahend of the fused operation.</param>
    /// <param name="multiplicand">The operation's multiplicand.</param>
    /// <param name="multiplier">The operations's multiplier.</param>
    /// <returns><c>multiplicand * multiplier - subtrahend</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<double> MultiplySub(
        this Vector256<double> subtrahend,
        Vector256<double> multiplicand,
        Vector256<double> multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplySubtract(multiplicand, multiplier, subtrahend)
            : Avx.Subtract(Avx.Multiply(multiplicand, multiplier), subtrahend);

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and subtraction.
    /// </summary>
    /// <remarks>This version takes also care of loading the multiplicand.</remarks>
    /// <param name="minuend">The minuend of the fused operation.</param>
    /// <param name="multiplicand">The operation's multiplicand.</param>
    /// <param name="multiplier">The operations's multiplier.</param>
    /// <returns><c>minuend - multiplicand * multiplier</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe static Vector256<double> MultiplyAddNeg(
        this Vector256<double> minuend,
        Vector256<double> multiplicand,
        Vector256<double> multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplyAddNegated(multiplicand, multiplier, minuend)
            : Avx.Subtract(minuend, Avx.Multiply(multiplicand, multiplier));

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and subtraction.
    /// </summary>
    /// <remarks>This version takes also care of loading the multiplicand.</remarks>
    /// <param name="minuend">The minuend of the fused operation.</param>
    /// <param name="multiplicand">The address of the multiplicand.</param>
    /// <param name="multiplier">The operations's multiplier.</param>
    /// <returns><c>minuend - multiplicand * multiplier</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe static Vector256<double> MultiplyAddNeg(
        this Vector256<double> minuend,
        double* multiplicand,
        Vector256<double> multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplyAddNegated(Avx.LoadVector256(multiplicand), multiplier, minuend)
            : Avx.Subtract(minuend, Avx.Multiply(Avx.LoadVector256(multiplicand), multiplier));

    /// <summary>Calculates <c>c₄x⁴+c₃x³+c₂x²+c₁x+c₀</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<double> Poly4(this Vector256<double> x,
        double c0, double c1, double c2, double c3, double c4) =>
        Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(
            Vector256.Create(c4), x,
            Vector256.Create(c3)), x,
            Vector256.Create(c2)), x,
            Vector256.Create(c1)), x,
            Vector256.Create(c0));

    /// <summary>Calculates <c>x⁵+c₄x⁴+c₃x³+c₂x²+c₁x+c₀</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<double> Poly5n(this Vector256<double> x,
        double c0, double c1, double c2, double c3, double c4) =>
        Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(Avx.Add(
            x, Vector256.Create(c4)),
            x, Vector256.Create(c3)),
            x, Vector256.Create(c2)),
            x, Vector256.Create(c1)),
            x, Vector256.Create(c0));

    /// <summary>Calculates <c>c₅x⁵+c₄x⁴+c₃x³+c₂x²+c₁x+c₀</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<double> Poly5(this Vector256<double> x,
        double c0, double c1, double c2, double c3, double c4, double c5) =>
        Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(
            Vector256.Create(c5), x,
            Vector256.Create(c4)), x,
            Vector256.Create(c3)), x,
            Vector256.Create(c2)), x,
            Vector256.Create(c1)), x,
            Vector256.Create(c0));

    /// <summary>Computes four logarithms at once.</summary>
    /// <remarks>Requires AVX/AVX2/FMA support.</remarks>
    /// <param name="x">An AVX vector of doubles.</param>
    /// <returns>A vector with the respective logarithms.</returns>
    public static Vector256<double> Log(this Vector256<double> x)
    {
        const double P0 = 7.70838733755885391666E0;
        const double P1 = 1.79368678507819816313E1;
        const double P2 = 1.44989225341610930846E1;
        const double P3 = 4.70579119878881725854E0;
        const double P4 = 4.97494994976747001425E-1;
        const double P5 = 1.01875663804580931796E-4;
        const double Q0 = 2.31251620126765340583E1;
        const double Q1 = 7.11544750618563894466E1;
        const double Q2 = 8.29875266912776603211E1;
        const double Q3 = 4.52279145837532221105E1;
        const double Q4 = 1.12873587189167450590E1;
        const double ln2_hi = 0.693359375;
        const double ln2_lo = -2.121944400546905827679E-4;
        const double pow2_52 = 4503599627370496.0;   // 2^52
        const double bias = 1023.0;                  // bias in exponent

        // Get the mantissa.
        Vector256<double> m = Avx2.Or(Avx2.And(x.AsUInt64(),
            Vector256.Create(0x000FFFFFFFFFFFFFUL)),
            Vector256.Create(0x3FE0000000000000UL)).AsDouble();
        Vector256<double> e = Avx.Subtract(Avx2.Or(Avx2.ShiftRightLogical(x.AsUInt64(), 52),
            Vector256.Create(pow2_52).AsUInt64()).AsDouble(), Vector256.Create(pow2_52 + bias));
        Vector256<double> blend = Avx.CompareGreaterThan(m, Vector256.Create(SQRT2 * 0.5));
        e = Avx.Add(e, Avx.And(Vector256.Create(1d), blend));
        m = Avx.Subtract(Avx.Add(m, Avx.AndNot(blend, m)), Vector256.Create(1d));
        Vector256<double> x2 = Avx.Multiply(m, m);
        Vector256<double> re = Avx.Divide(
            Avx.Multiply(m.Poly5(P0, P1, P2, P3, P4, P5), Avx.Multiply(m, x2)),
            m.Poly5n(Q0, Q1, Q2, Q3, Q4));
        // Add exponent.
        return Fma.MultiplyAdd(e, Vector256.Create(ln2_hi), Avx.Add(
            Fma.MultiplyAdd(e, Vector256.Create(ln2_lo), re),
            Fma.MultiplyAddNegated(x2, Vector256.Create(0.5), m)));
    }

    /// <summary>Computes four <see cref="Math.Atan2(double, double)"/> at once.</summary>
    /// <remarks>Requires AVX/AVX2/FMA support.</remarks>
    /// <param name="y">AVX vector with ordinates.</param>
    /// <param name="x">AVX vector with abscissas.</param>
    /// <returns>A vector with the respectives tangent inverses.</returns>
    public static Vector256<double> Atan2(this Vector256<double> y, Vector256<double> x)
    {
        const double PI_2 = PI / 2.0;
        const double PI_4 = PI / 4.0;
        const double MOREBITS = 6.123233995736765886130E-17;
        const double MOREBITSO2 = MOREBITS * 0.5;
        const double P4 = -8.750608600031904122785E-1;
        const double P3 = -1.615753718733365076637E1;
        const double P2 = -7.500855792314704667340E1;
        const double P1 = -1.228866684490136173410E2;
        const double P0 = -6.485021904942025371773E1;
        const double Q4 = 2.485846490142306297962E1;
        const double Q3 = 1.650270098316988542046E2;
        const double Q2 = 4.328810604912902668951E2;
        const double Q1 = 4.853903996359136964868E2;
        const double Q0 = 1.945506571482613964425E2;

        Vector256<double> signMask = Vector256.Create(-0.0);
        Vector256<double> minusOne = Vector256.Create(-1d);
        Vector256<double> x1 = Avx.AndNot(signMask, x);
        Vector256<double> y1 = Avx.AndNot(signMask, y);
        Vector256<double> swap = Avx.CompareGreaterThan(y1, x1);
        Vector256<double> x2 = Avx.BlendVariable(x1, y1, swap);
        Vector256<double> y2 = Avx.BlendVariable(y1, x1, swap);
        Vector256<double> bothInfinite = Avx.And(IsInfinite(x), IsInfinite(y));
        if (HorizontalOr(bothInfinite))
        {
            x2 = Avx.BlendVariable(Avx.And(x2, minusOne), x2, bothInfinite);
            y2 = Avx.BlendVariable(Avx.And(y2, minusOne), y2, bothInfinite);
        }
        Vector256<double> t = Avx.Divide(y2, x2);

        Vector256<double> notBig = Avx.CompareLessThanOrEqual(t, Vector256.Create(SQRT2 + 1.0));
        Vector256<double> notSmall = Avx.CompareGreaterThanOrEqual(t, Vector256.Create(0.66));
        Vector256<double> s = Avx.Add(
            Avx.And(notSmall, Avx.BlendVariable(
                Vector256.Create(PI_2), Vector256.Create(PI_4), notBig)),
            Avx.And(notSmall, Avx.BlendVariable(
                Vector256.Create(MOREBITS), Vector256.Create(MOREBITSO2), notBig)));

        Vector256<double> z = Avx.Divide(
            Avx.Add(Avx.And(notBig, t), Avx.And(notSmall, minusOne)), 
            Avx.Add(Avx.And(notBig, Vector256.Create(1d)), Avx.And(notSmall, t)));
        Vector256<double> zz = Avx.Multiply(z, z);

        Vector256<double> px = Avx.Divide(
            zz.Poly4(P0, P1, P2, P3, P4), zz.Poly5n(Q0, Q1, Q2, Q3, Q4));
        Vector256<double> re = Fma.MultiplyAdd(px, Avx.Multiply(z, zz), Avx.Add(z, s));
        re = Avx.BlendVariable(re, Avx.Subtract(Vector256.Create(PI_2), re), swap);
        re = Avx.BlendVariable(re, Vector256<double>.Zero,
            Avx.CompareEqual(Avx.Or(x, y), Vector256<double>.Zero));
        re = Avx.BlendVariable(re, Avx.Subtract(Vector256.Create(PI), re),
            Avx.And(x, signMask));
        return Avx.Xor(re, Avx.And(y, signMask));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<double> IsInfinite(Vector256<double> x) =>
            Avx.Or(
                Avx.CompareEqual(x, Vector256.Create(double.PositiveInfinity)),
                Avx.CompareEqual(x, Vector256.Create(double.NegativeInfinity)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool HorizontalOr(Vector256<double> x) =>
            Avx.MoveMask(Avx.CompareNotEqual(x, Vector256<double>.Zero)) != 0;
    }
}
