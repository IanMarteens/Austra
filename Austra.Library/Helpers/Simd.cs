namespace Austra.Library.Helpers;

/// <summary>Implements AVX/AVX2/FMA extensions.</summary>
public static class Simd
{
    /// <summary>Mask for <see cref="Vector256{T}"/> iterations.</summary>
    public const int MASK4 = 0x_7FFF_FFFC;
    /// <summary>Mask for <see cref="Vector512{T}"/> iterations.</summary>
    public const int MASK8 = 0x_7FFF_FFF8;
    /// <summary>Mask for <see cref="V8i"/> iterations.</summary>
    public const int MASK16 = 0x_7FFF_FFF0;

    /// <summary>The square root of two.</summary>
    public const double SQRT2 = 1.41421356237309504880;

    /// <summary>PI over two.</summary>
    private const double PI_2 = PI / 2.0;
    /// <summary>PI over four.</summary>
    private const double PI_4 = PI / 4.0;

    /// <summary>Sums all the elements in a vector.</summary>
    /// <param name="v">A intrinsics vector with four doubles.</param>
    /// <returns>The total value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Sum(this V4d v)
    {
        v = Avx.HorizontalAdd(v, v);
        return v.ToScalar() + v.GetElement(2);
    }

    /// <summary>Multiplies all the elements in a vector.</summary>
    /// <param name="v">A intrinsics vector with four doubles.</param>
    /// <returns>The product of all items.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Product(this V4d v)
    {
        Vector128<double> x = Sse2.Multiply(v.GetLower(), v.GetUpper());
        return x.ToScalar() * x.GetElement(1);
    }

    /// <summary>Multiplies all the elements in a vector.</summary>
    /// <param name="v">A intrinsics vector with eight integers.</param>
    /// <returns>The product of all items.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Product(this V4i v)
    {
        Vector128<int> x = v.GetLower() * v.GetUpper();
        Vector64<int> y = x.GetLower() * x.GetUpper();
        return y.ToScalar() * y.GetElement(1);
    }

    /// <summary>Gets the maximum component in a vector.</summary>
    /// <param name="v">A intrinsics vector with eight doubles.</param>
    /// <returns>The maximum component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Max(this V8d v) =>
        Math.Max(v.GetLower().Max(), v.GetUpper().Max());

    /// <summary>Gets the maximum component in a vector.</summary>
    /// <param name="v">A intrinsics vector with sixteen doubles.</param>
    /// <returns>The maximum component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Max(this V8i v) =>
        Math.Max(v.GetLower().Max(), v.GetUpper().Max());

    /// <summary>Gets the maximum component in a vector.</summary>
    /// <param name="v">A intrinsics vector with four doubles.</param>
    /// <returns>The maximum component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Max(this V4d v)
    {
        Vector128<double> x = Sse2.Max(v.GetLower(), v.GetUpper());
        return Math.Max(x.ToScalar(), x.GetElement(1));
    }

    /// <summary>Gets the maximum component in a vector.</summary>
    /// <param name="v">A intrinsics vector with eight integers.</param>
    /// <returns>The maximum component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Max(this V4i v)
    {
        Vector128<int> x = Sse41.Max(v.GetLower(), v.GetUpper());
        Vector64<int> y = Vector64.Max(x.GetLower(), x.GetUpper());
        return Math.Max(y.ToScalar(), y.GetElement(1));
    }

    /// <summary>Gets the maximum component in a vector.</summary>
    /// <param name="v">A intrinsics vector with eight doubles.</param>
    /// <returns>The maximum component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Min(this V8d v) =>
        Math.Min(v.GetLower().Min(), v.GetUpper().Min());

    /// <summary>Gets the maximum component in a vector.</summary>
    /// <param name="v">A intrinsics vector with sixteen integers.</param>
    /// <returns>The maximum component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Min(this V8i v) =>
        Math.Min(v.GetLower().Min(), v.GetUpper().Min());

    /// <summary>Gets the minimum component in a vector.</summary>
    /// <param name="v">A intrinsics vector with four doubles.</param>
    /// <returns>The maximum component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Min(this V4d v)
    {
        Vector128<double> x = Sse2.Min(v.GetLower(), v.GetUpper());
        return Math.Min(x.ToScalar(), x.GetElement(1));
    }

    /// <summary>Gets the minimum component in a vector.</summary>
    /// <param name="v">A intrinsics vector with eight integers.</param>
    /// <returns>The maximum component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Min(this V4i v)
    {
        Vector128<int> x = Sse41.Min(v.GetLower(), v.GetUpper());
        Vector64<int> y = Vector64.Min(x.GetLower(), x.GetUpper());
        return Math.Min(y.ToScalar(), y.GetElement(1));
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
    internal static V4d MultiplyAdd(
        this V4d summand,
        V4d multiplicand,
        V4d multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplyAdd(multiplicand, multiplier, summand)
            : multiplicand * multiplier + summand;

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and addition.
    /// </summary>
    /// <remarks>This version takes also care of loading the multiplicand.</remarks>
    /// <param name="summand">The summand of the fused operation.</param>
    /// <param name="multiplicand">The address if the multiplicand.</param>
    /// <param name="multiplier">The operations's multiplier.</param>
    /// <returns><c>multiplicand * multiplier + summand</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe static V8d MultiplyAdd(
        this V8d summand,
        double* multiplicand,
        V8d multiplier) =>
        Avx512F.FusedMultiplyAdd(Avx512F.LoadVector512(multiplicand), multiplier, summand);

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and addition.
    /// </summary>
    /// <remarks>This version takes also care of loading the multiplicand.</remarks>
    /// <param name="summand">The summand of the fused operation.</param>
    /// <param name="multiplicand">The address if the multiplicand.</param>
    /// <param name="multiplier">The operations's multiplier.</param>
    /// <returns><c>multiplicand * multiplier + summand</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe static V4d MultiplyAdd(
        this V4d summand,
        double* multiplicand,
        V4d multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplyAdd(Avx.LoadVector256(multiplicand), multiplier, summand)
            : Avx.LoadVector256(multiplicand) * multiplier + summand;

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and addition.
    /// </summary>
    /// <remarks>This version takes also care of loading some of the vectors.</remarks>
    /// <param name="summand">The summand of the fused operation.</param>
    /// <param name="multiplicand">The address of the multiplicand.</param>
    /// <param name="multiplier">The address of the multiplier.</param>
    /// <returns><c>multiplicand * multiplier + summand</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe static V4d MultiplyAdd(
        this V4d summand,
        double* multiplicand,
        double* multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplyAdd(
                Avx.LoadVector256(multiplicand), Avx.LoadVector256(multiplier), summand)
            : Avx.LoadVector256(multiplicand) * Avx.LoadVector256(multiplier) + summand;

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and subtraction.
    /// </summary>
    /// <remarks>Must only be called when <c>Avx.IsSupported</c>.</remarks>
    /// <param name="subtrahend">The subtrahend of the fused operation.</param>
    /// <param name="multiplicand">The operation's multiplicand.</param>
    /// <param name="multiplier">The operations's multiplier.</param>
    /// <returns><c>multiplicand * multiplier - subtrahend</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static V4d MultiplySub(
        this V4d subtrahend,
        V4d multiplicand,
        V4d multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplySubtract(multiplicand, multiplier, subtrahend)
            : multiplicand * multiplier - subtrahend;

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and subtraction.
    /// </summary>
    /// <remarks>This version takes also care of loading the multiplicand.</remarks>
    /// <param name="minuend">The minuend of the fused operation.</param>
    /// <param name="multiplicand">The operation's multiplicand.</param>
    /// <param name="multiplier">The operations's multiplier.</param>
    /// <returns><c>minuend - multiplicand * multiplier</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe static V4d MultiplyAddNeg(
        this V4d minuend,
        V4d multiplicand,
        V4d multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplyAddNegated(multiplicand, multiplier, minuend)
            : minuend - multiplicand * multiplier;

    /// <summary>
    /// Execute the best available version of a SIMD multiplication and subtraction.
    /// </summary>
    /// <remarks>This version takes also care of loading the multiplicand.</remarks>
    /// <param name="minuend">The minuend of the fused operation.</param>
    /// <param name="multiplicand">The address of the multiplicand.</param>
    /// <param name="multiplier">The operations's multiplier.</param>
    /// <returns><c>minuend - multiplicand * multiplier</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe static V4d MultiplyAddNeg(
        this V4d minuend,
        double* multiplicand,
        V4d multiplier) =>
        Fma.IsSupported
            ? Fma.MultiplyAddNegated(Avx.LoadVector256(multiplicand), multiplier, minuend)
            : minuend - Avx.LoadVector256(multiplicand) * multiplier;

    /// <summary>Calculates <c>c₄x⁴+c₃x³+c₂x²+c₁x+c₀</c>.</summary>
    /// <param name="x">The real variable used for evaluation.</param>
    /// <param name="c0">The constant term.</param>
    /// <param name="c1">The linear term.</param>
    /// <param name="c2">The quadratic term.</param>
    /// <param name="c3">The cubic term.</param>
    /// <param name="c4">The quartic term.</param>
    /// <returns>The evaluation of the polynomial at the given point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V4d Poly4(this V4d x,
        double c0, double c1, double c2, double c3, double c4) =>
        Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(
            V4.Create(c4), x,
            V4.Create(c3)), x,
            V4.Create(c2)), x,
            V4.Create(c1)), x,
            V4.Create(c0));

    /// <summary>Calculates <c>c₄x⁴+c₃x³+c₂x²+c₁x+c₀</c>.</summary>
    /// <param name="x">The real variable used for evaluation.</param>
    /// <param name="c0">The constant term.</param>
    /// <param name="c1">The linear term.</param>
    /// <param name="c2">The quadratic term.</param>
    /// <param name="c3">The cubic term.</param>
    /// <param name="c4">The quartic term.</param>
    /// <returns>The evaluation of the polynomial at the given point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V8d Poly4(this V8d x,
        double c0, double c1, double c2, double c3, double c4) =>
        Avx512F.FusedMultiplyAdd(Avx512F.FusedMultiplyAdd(
            Avx512F.FusedMultiplyAdd(Avx512F.FusedMultiplyAdd(
            V8.Create(c4), x,
            V8.Create(c3)), x,
            V8.Create(c2)), x,
            V8.Create(c1)), x,
            V8.Create(c0));

    /// <summary>Calculates <c>x⁵+c₄x⁴+c₃x³+c₂x²+c₁x+c₀</c>.</summary>
    /// <param name="x">The real variable used for evaluation.</param>
    /// <param name="c0">The constant term.</param>
    /// <param name="c1">The linear term.</param>
    /// <param name="c2">The quadratic term.</param>
    /// <param name="c3">The cubic term.</param>
    /// <param name="c4">The quartic term.</param>
    /// <remarks>It is assumed that the quintic term is one.</remarks>
    /// <returns>The evaluation of the polynomial at the given point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V4d Poly5n(this V4d x,
        double c0, double c1, double c2, double c3, double c4) =>
        Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(
            x + V4.Create(c4),
            x, V4.Create(c3)),
            x, V4.Create(c2)),
            x, V4.Create(c1)),
            x, V4.Create(c0));

    /// <summary>Calculates <c>x⁵+c₄x⁴+c₃x³+c₂x²+c₁x+c₀</c>.</summary>
    /// <param name="x">The real variable used for evaluation.</param>
    /// <param name="c0">The constant term.</param>
    /// <param name="c1">The linear term.</param>
    /// <param name="c2">The quadratic term.</param>
    /// <param name="c3">The cubic term.</param>
    /// <param name="c4">The quartic term.</param>
    /// <remarks>It is assumed that the quintic term is one.</remarks>
    /// <returns>The evaluation of the polynomial at the given point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V8d Poly5n(this V8d x,
        double c0, double c1, double c2, double c3, double c4) =>
        Avx512F.FusedMultiplyAdd(Avx512F.FusedMultiplyAdd(
            Avx512F.FusedMultiplyAdd(Avx512F.FusedMultiplyAdd(
            x + V8.Create(c4),
            x, V8.Create(c3)),
            x, V8.Create(c2)),
            x, V8.Create(c1)),
            x, V8.Create(c0));

    /// <summary>Calculates <c>c₅x⁵+c₄x⁴+c₃x³+c₂x²+c₁x+c₀</c>.</summary>
    /// <param name="x">The real variable used for evaluation.</param>
    /// <param name="c0">The constant term.</param>
    /// <param name="c1">The linear term.</param>
    /// <param name="c2">The quadratic term.</param>
    /// <param name="c3">The cubic term.</param>
    /// <param name="c4">The quartic term.</param>
    /// <param name="c5">The quintic term.</param>
    /// <returns>The evaluation of the polynomial at the given point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V4d Poly5(this V4d x,
        double c0, double c1, double c2, double c3, double c4, double c5) =>
        Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(Fma.MultiplyAdd(
            V4.Create(c5), x,
            V4.Create(c4)), x,
            V4.Create(c3)), x,
            V4.Create(c2)), x,
            V4.Create(c1)), x,
            V4.Create(c0));

    /// <summary>Calculates <c>c₅x⁵+c₄x⁴+c₃x³+c₂x²+c₁x+c₀</c>.</summary>
    /// <param name="x">The real variable used for evaluation.</param>
    /// <param name="c0">The constant term.</param>
    /// <param name="c1">The linear term.</param>
    /// <param name="c2">The quadratic term.</param>
    /// <param name="c3">The cubic term.</param>
    /// <param name="c4">The quartic term.</param>
    /// <param name="c5">The quintic term.</param>
    /// <returns>The evaluation of the polynomial at the given point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V8d Poly5(this V8d x,
        double c0, double c1, double c2, double c3, double c4, double c5) =>
        Avx512F.FusedMultiplyAdd(Avx512F.FusedMultiplyAdd(Avx512F.FusedMultiplyAdd(
            Avx512F.FusedMultiplyAdd(Avx512F.FusedMultiplyAdd(
            V8.Create(c5), x,
            V8.Create(c4)), x,
            V8.Create(c3)), x,
            V8.Create(c2)), x,
            V8.Create(c1)), x,
            V8.Create(c0));

    /// <summary>Computes four logarithms at once.</summary>
    /// <remarks>Requires AVX/AVX2/FMA support.</remarks>
    /// <param name="x">An AVX vector of doubles.</param>
    /// <returns>A vector with the respective logarithms.</returns>
    public static V4d Log(this V4d x)
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

        // Get the mantissa and the exponent.
        V4d m = Avx2.Or(Avx2.And(x.AsUInt64(),
            V4.Create(0x000FFFFFFFFFFFFFUL)),
            V4.Create(0x3FE0000000000000UL)).AsDouble();
        V4d e = Avx2.Or(Avx2.ShiftRightLogical(x.AsUInt64(), 52),
            V4.Create(pow2_52).AsUInt64()).AsDouble() - V4.Create(pow2_52 + bias);
        V4d blend = Avx.CompareGreaterThan(m, V4.Create(SQRT2 * 0.5));
        e += V4d.One & blend;
        m += Avx.AndNot(blend, m) - V4d.One;
        V4d x2 = m * m;
        V4d re = m.Poly5(P0, P1, P2, P3, P4, P5) * m * x2
            / m.Poly5n(Q0, Q1, Q2, Q3, Q4);
        // Add exponent.
        return Fma.MultiplyAdd(e, V4.Create(ln2_hi), 
            Fma.MultiplyAdd(e, V4.Create(ln2_lo), re) +
            Fma.MultiplyAddNegated(x2, V4.Create(0.5), m));
    }

    /// <summary>Computes eight logarithms at once.</summary>
    /// <remarks>Requires AVX512F support.</remarks>
    /// <param name="x">An AVX vector of doubles.</param>
    /// <returns>A vector with the respective logarithms.</returns>
    public static V8d Log(this V8d x)
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

        // Get the mantissa and the exponent.
        V8d m = Avx512F.GetMantissa(x, 2);
        V8d e = Avx512F.GetExponent(x);
        V8d blend = Avx512F.CompareGreaterThan(m, V8.Create(SQRT2 * 0.5));
        e += V8d.One & blend;
        m += AndNot(blend, m) - V8d.One;
        V8d x2 = m * m;
        V8d re = m.Poly5(P0, P1, P2, P3, P4, P5) * m * x2
            / m.Poly5n(Q0, Q1, Q2, Q3, Q4);
        // Add exponent.
        return Avx512F.FusedMultiplyAdd(e, V8.Create(ln2_hi),
            Avx512F.FusedMultiplyAdd(e, V8.Create(ln2_lo), re) +
            Avx512F.FusedMultiplyAddNegated(x2, V8.Create(0.5), m));
    }

    /// <summary>Computes four <see cref="Math.Atan2(double, double)"/> at once.</summary>
    /// <remarks>Requires AVX/AVX2/FMA support.</remarks>
    /// <param name="y">AVX vector with ordinates.</param>
    /// <param name="x">AVX vector with abscissas.</param>
    /// <returns>A vector with the respective tangent inverses.</returns>
    public static V4d Atan2(this V4d y, V4d x)
    {
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

        V4d signMask = V4.Create(-0.0);
        V4d minusOne = V4.Create(-1d);
        V4d x1 = Avx.AndNot(signMask, x);
        V4d y1 = Avx.AndNot(signMask, y);
        V4d swap = Avx.CompareGreaterThan(y1, x1);
        V4d x2 = Avx.BlendVariable(x1, y1, swap);
        V4d y2 = Avx.BlendVariable(y1, x1, swap);
        V4d bothInfinite = IsInfinite(x) & IsInfinite(y);
        if (Avx.MoveMask(Avx.CompareNotEqual(bothInfinite, V4d.Zero)) != 0)
        {
            x2 = Avx.BlendVariable(x2 & minusOne, x2, bothInfinite);
            y2 = Avx.BlendVariable(y2 & minusOne, y2, bothInfinite);
        }
        V4d t = y2 / x2;
        V4d notBig = Avx.CompareLessThanOrEqual(t, V4.Create(SQRT2 + 1.0));
        V4d notSmall = Avx.CompareGreaterThanOrEqual(t, V4.Create(0.66));
        V4d s = (notSmall & Avx.BlendVariable(V4.Create(PI_2), V4.Create(PI_4), notBig)) +
            (notSmall & Avx.BlendVariable(V4.Create(MOREBITS), V4.Create(MOREBITSO2), notBig));

        V4d z = ((notBig & t) + (notSmall & minusOne)) / ((notBig & V4d.One) + (notSmall & t));
        V4d zz = z * z;
        V4d px = zz.Poly4(P0, P1, P2, P3, P4) / zz.Poly5n(Q0, Q1, Q2, Q3, Q4);
        V4d re = Fma.MultiplyAdd(px, z * zz, z + s);
        re = Avx.BlendVariable(re, V4.Create(PI_2) - re, swap);
        re = Avx.BlendVariable(re, V4d.Zero, Avx.CompareEqual(x | y, V4d.Zero));
        re = Avx.BlendVariable(re, V4.Create(PI) - re, x & signMask);
        return re ^ (y & signMask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static V4d IsInfinite(V4d x) =>
            Avx.CompareEqual(x, V4.Create(double.PositiveInfinity)) |
            Avx.CompareEqual(x, V4.Create(double.NegativeInfinity));
    }

    /// <summary>Computes eight <see cref="Math.Atan2(double, double)"/> at once.</summary>
    /// <remarks>Requires AVX512F support.</remarks>
    /// <param name="y">AVX vector with ordinates.</param>
    /// <param name="x">AVX vector with abscissas.</param>
    /// <returns>A vector with the respective tangent inverses.</returns>
    public static V8d Atan2(this V8d y, V8d x)
    {
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

        V8d signMask = V8.Create(-0.0);
        V8d minusOne = V8.Create(-1d);
        V8d x1 = AndNot(signMask, x);
        V8d y1 = AndNot(signMask, y);
        V8d swap = Avx512F.CompareGreaterThan(y1, x1);
        V8d x2 = Avx512F.BlendVariable(x1, y1, swap);
        V8d y2 = Avx512F.BlendVariable(y1, x1, swap);
        V8d bothInfinite = IsInfinite(x) & IsInfinite(y);
        if (!V8.EqualsAll(bothInfinite, V8d.Zero))
        {
            x2 = Avx512F.BlendVariable(x2 & minusOne, x2, bothInfinite);
            y2 = Avx512F.BlendVariable(y2 & minusOne, y2, bothInfinite);
        }
        V8d t = y2 / x2;
        V8d notBig = Avx512F.CompareLessThanOrEqual(t, V8.Create(SQRT2 + 1.0));
        V8d notSmall = Avx512F.CompareGreaterThanOrEqual(t, V8.Create(0.66));
        V8d s =
            (notSmall & Avx512F.BlendVariable(V8.Create(PI_2), V8.Create(PI_4), notBig)) +
            (notSmall & Avx512F.BlendVariable(V8.Create(MOREBITS), V8.Create(MOREBITSO2), notBig));

        V8d z = ((notBig & t) + (notSmall & minusOne)) / ((notBig & V8d.One) + (notSmall & t));
        V8d zz = z * z;
        V8d px = zz.Poly4(P0, P1, P2, P3, P4) / zz.Poly5n(Q0, Q1, Q2, Q3, Q4);
        V8d re = Avx512F.FusedMultiplyAdd(px, z * zz, z + s);
        re = Avx512F.BlendVariable(re, V8.Create(PI_2) - re, swap);
        re = Avx512F.BlendVariable(re, V8d.Zero, Avx512F.CompareEqual(x | y, V8d.Zero));
        re = Avx512F.BlendVariable(re, V8.Create(PI) - re, x & signMask);
        return re ^ (y & signMask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static V8d IsInfinite(V8d x) =>
            Avx512F.CompareEqual(x, V8.Create(double.PositiveInfinity)) |
            Avx512F.CompareEqual(x, V8.Create(double.NegativeInfinity));
    }

    /// <summary>Computes the sine and cosine of a vector of doubles.</summary>
    /// <param name="x">AVX512 argument.</param>
    /// <returns>A tuple with the sine and the cosine.</returns>
    public static (V8d Sin, V8d Cos) SinCos(this V8d x)
    {
        const double P0sin = -1.66666666666666307295E-1;
        const double P1sin = 8.33333333332211858878E-3;
        const double P2sin = -1.98412698295895385996E-4;
        const double P3sin = 2.75573136213857245213E-6;
        const double P4sin = -2.50507477628578072866E-8;
        const double P5sin = 1.58962301576546568060E-10;

        const double P0cos = 4.16666666666665929218E-2;
        const double P1cos = -1.38888888888730564116E-3;
        const double P2cos = 2.48015872888517045348E-5;
        const double P3cos = -2.75573141792967388112E-7;
        const double P4cos = 2.08757008419747316778E-9;
        const double P5cos = -1.13585365213876817300E-11;

        const double DP1 = 7.853981554508209228515625E-1 * 2;
        const double DP2 = 7.94662735614792836714E-9 * 2;
        const double DP3 = 3.06161699786838294307E-17 * 2;

        V8d absX = V8.Abs(x);
        absX = V8.ConditionalSelect(
            Avx512F.CompareGreaterThan(absX, V8.Create(1E17d)),
            V8d.Zero,
            absX);
        V8d y = absX * V8.Create(2.0 / PI);
        y = V8.Create(Avx.RoundToNearestInteger(y.GetLower()), Avx.RoundToNearestInteger(y.GetUpper()));
        V8d xx = Avx512F.FusedMultiplyAddNegated(y, V8.Create(DP3),
            Avx512F.FusedMultiplyAddNegated(y, V8.Create(DP2 + DP1), absX));
        V8d x2 = xx * xx;
        V8d s = x2.Poly5(P0sin, P1sin, P2sin, P3sin, P4sin, P5sin);
        V8d c = x2.Poly5(P0cos, P1cos, P2cos, P3cos, P4cos, P5cos);
        // s = x + (x * z2) * s;
        s = Avx512F.FusedMultiplyAdd(xx * x2, s, xx);
        // c = 1.0 - z2 * 0.5 + (z2 * z2) * c;
        c = Avx512F.FusedMultiplyAdd(x2 * x2, c,
            Avx512F.FusedMultiplyAddNegated(x2, V8.Create(0.5), V8d.One));
        Vector512<ulong> q = V8.ConvertToUInt64(y);
        V8d swap = V8.Equals(q & Vector512<ulong>.One, Vector512<ulong>.One).AsDouble();
        V8d sin1 = V8.ConditionalSelect(swap, c, s);
        V8d cos1 = V8.ConditionalSelect(swap, s, c);
        sin1 = V8.ConditionalSelect((q << 62).AsDouble(), -sin1, sin1);
        sin1 = V8.ConditionalSelect(x & V8.Create(-0.0), -sin1, sin1);
        cos1 = V8.ConditionalSelect(
            (((q + Vector512<ulong>.One) & V8.Create(2UL)) << 62).AsDouble(), -cos1, cos1);
        return (sin1, cos1);
    }

    /// <summary>Computes the sine and cosine of a vector of doubles.</summary>
    /// <remarks>
    /// This method assume that each argument is in the range [0, Math.Tau].
    /// That is the range of values supplied by the standard distribution generator.
    /// </remarks>
    /// <param name="x">AVX512 argument. Each component must be in the range [0, 2π].</param>
    /// <returns>A tuple with the sine and the cosine.</returns>
    public static (V8d Sin, V8d Cos) SinCosNormal(this V8d x)
    {
        const double P0sin = -1.66666666666666307295E-1;
        const double P1sin = 8.33333333332211858878E-3;
        const double P2sin = -1.98412698295895385996E-4;
        const double P3sin = 2.75573136213857245213E-6;
        const double P4sin = -2.50507477628578072866E-8;
        const double P5sin = 1.58962301576546568060E-10;

        const double P0cos = 4.16666666666665929218E-2;
        const double P1cos = -1.38888888888730564116E-3;
        const double P2cos = 2.48015872888517045348E-5;
        const double P3cos = -2.75573141792967388112E-7;
        const double P4cos = 2.08757008419747316778E-9;
        const double P5cos = -1.13585365213876817300E-11;

        const double DP1 = 7.853981554508209228515625E-1 * 2;
        const double DP2 = 7.94662735614792836714E-9 * 2;
        const double DP3 = 3.06161699786838294307E-17 * 2;

        V8d y = x * V8.Create(2.0 / PI);
        y = V8.Create(Avx.RoundToNearestInteger(y.GetLower()), Avx.RoundToNearestInteger(y.GetUpper()));
        V8d z = Avx512F.FusedMultiplyAddNegated(y, V8.Create(DP3),
            Avx512F.FusedMultiplyAddNegated(y, V8.Create(DP2 + DP1), x));
        V8d z2 = z * z;
        // s = z + (z * z2) * s;
        V8d s = Avx512F.FusedMultiplyAdd(z * z2, z2.Poly5(P0sin, P1sin, P2sin, P3sin, P4sin, P5sin), z);
        // c = 1.0 - z2 * 0.5 + (z2 * z2) * c;
        V8d c = Avx512F.FusedMultiplyAdd(z2 * z2, z2.Poly5(P0cos, P1cos, P2cos, P3cos, P4cos, P5cos),
            Avx512F.FusedMultiplyAddNegated(z2, V8.Create(0.5), V8d.One));
        Vector512<ulong> q = V8.ConvertToUInt64(y);
        V8d swap = V8.Equals(q & Vector512<ulong>.One, Vector512<ulong>.One).AsDouble();
        V8d sin1 = V8.ConditionalSelect(swap, c, s);
        V8d cos1 = V8.ConditionalSelect(swap, s, c);
        return (V8.ConditionalSelect((q << 62).AsDouble(), -sin1, sin1), V8.ConditionalSelect(
            (((q + Vector512<ulong>.One) & V8.Create(2UL)) << 62).AsDouble(), -cos1, cos1));
    }

    /// <summary>Computes the sine and cosine of a vector of doubles.</summary>
    /// <remarks>
    /// This method assume that each argument is in the range [0, Math.Tau].
    /// That is the range of values supplied by the standard distribution generator.
    /// </remarks>
    /// <param name="x">AVX256 argument. Each component must be in the range [0, 2π].</param>
    /// <returns>A tuple with the sine and the cosine.</returns>
    public static (V4d Sin, V4d Cos) SinCosNormal(this V4d x)
    {
        const double P0sin = -1.66666666666666307295E-1;
        const double P1sin = 8.33333333332211858878E-3;
        const double P2sin = -1.98412698295895385996E-4;
        const double P3sin = 2.75573136213857245213E-6;
        const double P4sin = -2.50507477628578072866E-8;
        const double P5sin = 1.58962301576546568060E-10;

        const double P0cos = 4.16666666666665929218E-2;
        const double P1cos = -1.38888888888730564116E-3;
        const double P2cos = 2.48015872888517045348E-5;
        const double P3cos = -2.75573141792967388112E-7;
        const double P4cos = 2.08757008419747316778E-9;
        const double P5cos = -1.13585365213876817300E-11;

        const double DP1 = 7.853981554508209228515625E-1 * 2;
        const double DP2 = 7.94662735614792836714E-9 * 2;
        const double DP3 = 3.06161699786838294307E-17 * 2;

        V4d y = x * V4.Create(2.0 / PI);
        y = Avx.RoundToNearestInteger(y);
        V4d xx = Fma.MultiplyAddNegated(y, V4.Create(DP3),
            Fma.MultiplyAddNegated(y, V4.Create(DP2 + DP1), x));
        V4d x2 = xx * xx;
        V4d s = Poly5(x2, P0sin, P1sin, P2sin, P3sin, P4sin, P5sin);
        V4d c = Poly5(x2, P0cos, P1cos, P2cos, P3cos, P4cos, P5cos);
        // s = x + (x * x2) * s;
        s = Fma.MultiplyAdd(xx * x2, s, xx);
        // c = 1.0 - x2 * 0.5 + (x2 * x2) * c;
        c = Fma.MultiplyAdd(x2 * x2, c,
            Fma.MultiplyAddNegated(x2, V4.Create(0.5), V4d.One));
        Vector256<ulong> q = V4.ConvertToUInt64(y);
        V4d swap = V4.Equals(q & Vector256<ulong>.One, Vector256<ulong>.One).AsDouble();
        V4d sin1 = V4.ConditionalSelect(swap, c, s);
        V4d cos1 = V4.ConditionalSelect(swap, s, c);
        sin1 = V4.ConditionalSelect((q << 62).AsDouble(), -sin1, sin1);
        cos1 = V4.ConditionalSelect((((q + Vector256<ulong>.One) & V4.Create(2UL)) << 62).AsDouble(), -cos1, cos1);
        return (sin1, cos1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static V8d AndNot(V8d x, V8d y) =>
        V8.Create(
            Avx.AndNot(x.GetLower(), y.GetLower()),
            Avx.AndNot(x.GetUpper(), y.GetUpper()));
}
