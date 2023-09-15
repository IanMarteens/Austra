namespace Austra.Parser;

/// <summary>Syntactic analysis for AUSTRA.</summary>
internal static partial class Parser
{
    /// <summary>Most common argument list in functions.</summary>
    private static readonly Type[] doubleArg = new[] { typeof(double) };
    /// <summary>Second common argument list in functions.</summary>
    private static readonly Type[] intArg = new[] { typeof(int) };
    /// <summary>Constructor for <see cref="Index"/>.</summary>
    private static readonly ConstructorInfo indexCtor =
        typeof(Index).GetConstructor(new[] { typeof(int), typeof(bool) })!;
    /// <summary>Constructor for <see cref="Range"/>.</summary>
    private static readonly ConstructorInfo rangeCtor =
        typeof(Range).GetConstructor(new[] { typeof(Index), typeof(Index) })!;
    /// <summary>The <see cref="Expression"/> for <see langword="false"/>.</summary>
    private static readonly ConstantExpression falseExpr = Expression.Constant(false);
    /// <summary>The <see cref="Expression"/> for <see langword="true"/>.</summary>
    private static readonly ConstantExpression trueExpr = Expression.Constant(true);

    /// <summary>Allowed global functions and their implementations.</summary>
    private static readonly Dictionary<string, MethodInfo> functions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Monadic functions.
            ["abs"] = typeof(Math).GetMethod(nameof(Math.Abs), doubleArg)!,
            ["exp"] = typeof(Math).Get(nameof(Math.Exp)),
            ["log"] = typeof(Math).GetMethod(nameof(Math.Log), doubleArg)!,
            ["cbrt"] = typeof(Math).Get(nameof(Math.Cbrt)),
            ["sin"] = typeof(Math).Get(nameof(Math.Sin)),
            ["cos"] = typeof(Math).Get(nameof(Math.Cos)),
            ["tan"] = typeof(Math).Get(nameof(Math.Tan)),
            ["asin"] = typeof(Math).Get(nameof(Math.Asin)),
            ["acos"] = typeof(Math).Get(nameof(Math.Acos)),
            ["atan"] = typeof(Math).Get(nameof(Math.Atan)),
            ["sqrt"] = typeof(Math).Get(nameof(Math.Sqrt)),
            ["sign"] = typeof(Math).GetMethod(nameof(Math.Sign), doubleArg)!,
            ["round"] = typeof(Math).GetMethod(nameof(Math.Round), doubleArg)!,
            ["trunc"] = typeof(Math).GetMethod(nameof(Math.Truncate), doubleArg)!,
            ["probit"] = typeof(F).Get(nameof(F.Probit)),
            ["gamma"] = typeof(F).Get(nameof(F.Gamma)),
            ["lngamma"] = typeof(F).Get(nameof(F.GammaLn)),
            ["erf"] = typeof(F).Get(nameof(F.Erf)),
            ["ncdf"] = typeof(F).Get(nameof(F.NCdf)),
        };

    /// <summary>Allowed series methods.</summary>
    private static readonly Dictionary<Type, Dictionary<string, MethodInfo>> methods =
        new()
        {
            [typeof(Series)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["autocorr"] = typeof(Series).Get(nameof(Series.AutoCorrelation)),
                ["corr"] = typeof(Series).Get(nameof(Series.Correlation)),
                ["correlogram"] = typeof(Series).Get(nameof(Series.Correlogram)),
                ["cov"] = typeof(Series).Get(nameof(Series.Covariance)),
                ["stats"] = typeof(Series).GetMethod(nameof(Series.GetSliceStats),
                    new[] { typeof(Date) })!,
                ["ncdf"] = typeof(Series).GetMethod(nameof(Series.NCdf), doubleArg)!,
                ["movingAvg"] = typeof(Series).Get(nameof(Series.MovingAvg)),
                ["movingStd"] = typeof(Series).GetMethod(nameof(Series.MovingStd), intArg)!,
                ["movingNcdf"] = typeof(Series).Get(nameof(Series.MovingNcdf)),
                ["ewma"] = typeof(Series).Get(nameof(Series.EWMA)),
                ["map"] = typeof(Series).Get(nameof(Series.Map)),
                ["filter"] = typeof(Series).Get(nameof(Series.Filter)),
                ["any"] = typeof(Series).Get(nameof(Series.Any)),
                ["all"] = typeof(Series).Get(nameof(Series.All)),
                ["zip"] = typeof(Series).Get(nameof(Series.Zip)),
                ["indexof"] = typeof(Series).GetMethod(nameof(Series.IndexOf), doubleArg)!,
                ["linear"] = typeof(Series).Get(nameof(Series.LinearModel)),
                ["linearModel"] = typeof(Series).Get(nameof(Series.FullLinearModel)),
                ["ar"] = typeof(Series).Get(nameof(Series.AutoRegression)),
                ["arModel"] = typeof(Series).Get(nameof(Series.ARModel)),
            },
            [typeof(DateSpline)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["poly"] = typeof(DateSpline).Get(nameof(DateSpline.GetPoly)),
                ["derivative"] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),
                ["deriv"] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),
                ["der"] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),
            },
            [typeof(VectorSpline)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["poly"] = typeof(VectorSpline).Get(nameof(VectorSpline.GetPoly)),
                ["derivative"] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),
                ["deriv"] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),
                ["der"] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),
            },
            [typeof(Vector)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["autocorr"] = typeof(Vector).Get(nameof(Vector.AutoCorrelation)),
                ["correlogram"] = typeof(Vector).Get(nameof(Vector.Correlogram)),
                ["map"] = typeof(Vector).Get(nameof(Vector.Map)),
                ["any"] = typeof(Vector).Get(nameof(Vector.Any)),
                ["all"] = typeof(Vector).Get(nameof(Vector.All)),
                ["reduce"] = typeof(Vector).Get(nameof(Vector.Reduce)),
                ["zip"] = typeof(Vector).Get(nameof(Vector.Zip)),
                ["filter"] = typeof(Vector).Get(nameof(Vector.Filter)),
                ["indexof"] = typeof(Vector).GetMethod(nameof(Vector.IndexOf), doubleArg)!,
                ["linear"] = typeof(Vector).Get(nameof(Vector.LinearModel)),
                ["linearModel"] = typeof(Vector).Get(nameof(Vector.FullLinearModel)),
                ["ar"] = typeof(Vector).Get(nameof(Vector.AutoRegression)),
                ["arModel"] = typeof(Vector).Get(nameof(Vector.ARModel)),
            },
            [typeof(ComplexVector)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["map"] = typeof(ComplexVector).Get(nameof(ComplexVector.Map)),
                ["mapreal"] = typeof(ComplexVector).Get(nameof(ComplexVector.MapReal)),
                ["mapr"] = typeof(ComplexVector).Get(nameof(ComplexVector.MapReal)),
                ["any"] = typeof(ComplexVector).Get(nameof(ComplexVector.Any)),
                ["all"] = typeof(ComplexVector).Get(nameof(ComplexVector.All)),
                ["reduce"] = typeof(ComplexVector).Get(nameof(ComplexVector.Reduce)),
                ["zip"] = typeof(ComplexVector).Get(nameof(ComplexVector.Zip)),
                ["filter"] = typeof(ComplexVector).Get(nameof(ComplexVector.Filter)),
                ["indexof"] = typeof(ComplexVector).GetMethod(nameof(ComplexVector.IndexOf),
                    new[] { typeof(Complex) })!,
            },
            [typeof(Date)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["addmonths"] = typeof(Date).GetMethod(nameof(Date.AddMonths), intArg)!,
                ["addyears"] = typeof(Date).Get(nameof(Date.AddYears)),
            },
            [typeof(Matrix)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["getcol"] = typeof(Matrix).GetMethod(nameof(Matrix.GetColumn), intArg)!,
                ["getrow"] = typeof(Matrix).GetMethod(nameof(Matrix.GetRow), intArg)!,
                ["map"] = typeof(Matrix).Get(nameof(Matrix.Map)),
                ["any"] = typeof(Matrix).Get(nameof(Matrix.Any)),
                ["all"] = typeof(Matrix).Get(nameof(Matrix.All)),
            },
            [typeof(Polynomial)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["eval"] = typeof(Polynomial).Get(nameof(Polynomial.Eval)),
                ["derivative"] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),
                ["deriv"] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),
                ["der"] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),
            },
        };

    /// <summary>Allowed properties and their implementations.</summary>
    private static readonly Dictionary<Type, Dictionary<string, MethodInfo>> allProps =
        new()
        {
            [typeof(Complex)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["real"] = typeof(Complex).Prop(nameof(Complex.Real)),
                ["re"] = typeof(Complex).Prop(nameof(Complex.Real)),
                ["imaginary"] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
                ["imag"] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
                ["im"] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
                ["magnitude"] = typeof(Complex).Prop(nameof(Complex.Magnitude)),
                ["mag"] = typeof(Complex).Prop(nameof(Complex.Magnitude)),
                ["phase"] = typeof(Complex).Prop(nameof(Complex.Phase)),
            },
            [typeof(FftRModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["amplitudes"] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
                ["magnitudes"] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
                ["phases"] = typeof(FftModel).Prop(nameof(FftModel.Phases)),
                ["length"] = typeof(FftModel).Prop(nameof(FftModel.Length)),
                ["values"] = typeof(FftModel).Prop(nameof(FftModel.Spectrum)),
                ["inverse"] = typeof(FftRModel).Get(nameof(FftRModel.Inverse)),
            },
            [typeof(FftCModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["amplitudes"] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
                ["magnitudes"] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
                ["phases"] = typeof(FftModel).Prop(nameof(FftModel.Phases)),
                ["length"] = typeof(FftModel).Prop(nameof(FftModel.Length)),
                ["values"] = typeof(FftModel).Prop(nameof(FftModel.Spectrum)),
                ["inverse"] = typeof(FftCModel).Get(nameof(FftCModel.Inverse)),
            },

            [typeof(Series)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["count"] = typeof(Series).Prop(nameof(Series.Count)),
                ["length"] = typeof(Series).Prop(nameof(Series.Count)),
                ["min"] = typeof(Series).Get(nameof(Series.Minimum)),
                ["max"] = typeof(Series).Get(nameof(Series.Maximum)),
                ["mean"] = typeof(Series).Get(nameof(Series.Mean)),
                ["var"] = typeof(Series).Get(nameof(Series.Variance)),
                ["varp"] = typeof(Series).Get(nameof(Series.PopulationVariance)),
                ["std"] = typeof(Series).Get(nameof(Series.StandardDeviation)),
                ["stdp"] = typeof(Series).Get(nameof(Series.PopulationStandardDeviation)),
                ["skew"] = typeof(Series).Get(nameof(Series.Skewness)),
                ["skewp"] = typeof(Series).Get(nameof(Series.PopulationSkewness)),
                ["kurt"] = typeof(Series).Get(nameof(Series.Kurtosis)),
                ["kurtp"] = typeof(Series).Get(nameof(Series.PopulationKurtosis)),
                ["stats"] = typeof(Series).Get(nameof(Series.Stats)),
                ["first"] = typeof(Series).Get(nameof(Series.First)),
                ["last"] = typeof(Series).Get(nameof(Series.Last)),
                ["rets"] = typeof(Series).Get(nameof(Series.AsReturns)),
                ["logs"] = typeof(Series).Get(nameof(Series.AsLogReturns)),
                ["fft"] = typeof(Series).Get(nameof(Series.Fft)),
                ["perc"] = typeof(Series).Get(nameof(Series.Percentiles)),
                ["values"] = typeof(Series).Get(nameof(Series.GetValues)),
                ["random"] = typeof(Series).Get(nameof(Series.Random)),
                ["movingret"] = typeof(Series).Get(nameof(Series.MovingRet)),
                ["sum"] = typeof(Series).Get(nameof(Series.Sum)),
                ["type"] = typeof(Series).Prop(nameof(Series.Type)),
                ["amax"] = typeof(Series).Get(nameof(Series.AbsMax)),
                ["amin"] = typeof(Series).Get(nameof(Series.AbsMin)),
                ["ncdf"] = typeof(Series).GetMethod(nameof(Series.NCdf), Array.Empty<Type>())!,
                ["fit"] = typeof(Series).Get(nameof(Series.Fit)),
                ["linearfit"] = typeof(Series).Get(nameof(Series.LinearFit)),
                ["spline"] = typeof(Series).Get(nameof(Series.Spline)),
                ["acf"] = typeof(Series).Get(nameof(Series.ACF)),
            },
            [typeof(Series<int>)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["stats"] = typeof(Series<int>).Get(nameof(Series<int>.Stats)),
                ["first"] = typeof(Series<int>).Get(nameof(Series<int>.First)),
                ["last"] = typeof(Series<int>).Get(nameof(Series<int>.Last)),
                ["values"] = typeof(Series<int>).Get(nameof(Series<int>.GetValues)),
                ["sum"] = typeof(Series<int>).Get(nameof(Series<int>.Sum)),
            },
            [typeof(Series<double>)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["stats"] = typeof(Series<double>).Get(nameof(Series<double>.Stats)),
                ["first"] = typeof(Series<double>).Get(nameof(Series<double>.First)),
                ["last"] = typeof(Series<double>).Get(nameof(Series<double>.Last)),
                ["values"] = typeof(Series<double>).Get(nameof(Series<double>.GetValues)),
                ["sum"] = typeof(Series<double>).Get(nameof(Series<double>.Sum)),
            },
            [typeof(Acc)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["count"] = typeof(Acc).Prop(nameof(Acc.Count)),
                ["min"] = typeof(Acc).Prop(nameof(Acc.Minimum)),
                ["max"] = typeof(Acc).Prop(nameof(Acc.Maximum)),
                ["mean"] = typeof(Acc).Prop(nameof(Acc.Mean)),
                ["var"] = typeof(Acc).Prop(nameof(Acc.Variance)),
                ["varp"] = typeof(Acc).Prop(nameof(Acc.PopulationVariance)),
                ["std"] = typeof(Acc).Prop(nameof(Acc.StandardDeviation)),
                ["stdp"] = typeof(Acc).Prop(nameof(Acc.PopulationStandardDeviation)),
                ["skew"] = typeof(Acc).Prop(nameof(Acc.Skewness)),
                ["skewp"] = typeof(Acc).Prop(nameof(Acc.PopulationSkewness)),
                ["kurt"] = typeof(Acc).Prop(nameof(Acc.Kurtosis)),
                ["kurtp"] = typeof(Acc).Prop(nameof(Acc.PopulationKurtosis)),
            },
            [typeof(Matrix)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["det"] = typeof(Matrix).Get(nameof(Matrix.Determinant)),
                ["chol"] = typeof(Matrix).Get(nameof(Matrix.CholeskyMatrix)),
                ["evd"] = typeof(Matrix).GetMethod(nameof(Matrix.EVD), Array.Empty<Type>())!,
                ["trace"] = typeof(Matrix).Get(nameof(Matrix.Trace)),
                ["rows"] = typeof(Matrix).Prop(nameof(Matrix.Rows)),
                ["cols"] = typeof(Matrix).Prop(nameof(Matrix.Cols)),
                ["amax"] = typeof(Matrix).Get(nameof(Matrix.AMax)),
                ["amin"] = typeof(Matrix).Get(nameof(Matrix.AMin)),
                ["max"] = typeof(Matrix).Get(nameof(Matrix.Maximum)),
                ["min"] = typeof(Matrix).Get(nameof(Matrix.Minimum)),
                ["diag"] = typeof(Matrix).Get(nameof(Matrix.Diagonal)),
                ["inverse"] = typeof(Matrix).Get(nameof(Matrix.Inverse)),
            },
            [typeof(LMatrix)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["det"] = typeof(LMatrix).Get(nameof(LMatrix.Determinant)),
                ["trace"] = typeof(LMatrix).Get(nameof(LMatrix.Trace)),
                ["rows"] = typeof(LMatrix).Prop(nameof(LMatrix.Rows)),
                ["cols"] = typeof(LMatrix).Prop(nameof(LMatrix.Cols)),
                ["amax"] = typeof(LMatrix).Get(nameof(LMatrix.AMax)),
                ["amin"] = typeof(LMatrix).Get(nameof(LMatrix.AMin)),
                ["max"] = typeof(LMatrix).Get(nameof(LMatrix.Maximum)),
                ["min"] = typeof(LMatrix).Get(nameof(LMatrix.Minimum)),
                ["diag"] = typeof(LMatrix).Get(nameof(LMatrix.Diagonal))
            },
            [typeof(EVD)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["vectors"] = typeof(EVD).Prop(nameof(EVD.Vectors)),
                ["values"] = typeof(EVD).Prop(nameof(EVD.Values)),
                ["d"] = typeof(EVD).Prop(nameof(EVD.D)),
                ["rank"] = typeof(EVD).Get(nameof(EVD.Rank)),
                ["det"] = typeof(EVD).Get(nameof(EVD.Determinant)),
            },
            [typeof(LinearSModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["original"] = typeof(LinearSModel).Prop(nameof(LinearSModel.Original)),
                ["prediction"] = typeof(LinearSModel).Prop(nameof(LinearSModel.Prediction)),
                ["weights"] = typeof(LinearSModel).Prop(nameof(LinearSModel.Weights)),
                ["r2"] = typeof(LinearSModel).Prop(nameof(LinearSModel.R2)),
                ["rss"] = typeof(LinearSModel).Prop(nameof(LinearSModel.ResidualSumSquares)),
                ["tss"] = typeof(LinearSModel).Prop(nameof(LinearSModel.TotalSumSquares)),
            },
            [typeof(LinearVModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["original"] = typeof(LinearVModel).Prop(nameof(LinearVModel.Original)),
                ["prediction"] = typeof(LinearVModel).Prop(nameof(LinearVModel.Prediction)),
                ["weights"] = typeof(LinearVModel).Prop(nameof(LinearVModel.Weights)),
                ["r2"] = typeof(LinearVModel).Prop(nameof(LinearVModel.R2)),
                ["rss"] = typeof(LinearVModel).Prop(nameof(LinearVModel.ResidualSumSquares)),
                ["tss"] = typeof(LinearVModel).Prop(nameof(LinearVModel.TotalSumSquares)),
            },
            [typeof(Vector)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["norm"] = typeof(Vector).Get(nameof(Vector.Norm)),
                ["length"] = typeof(Vector).Prop(nameof(Vector.Length)),
                ["abs"] = typeof(Vector).Get(nameof(Vector.Abs)),
                ["sqr"] = typeof(Vector).Get(nameof(Vector.Squared)),
                ["stats"] = typeof(Vector).Get(nameof(Vector.Stats)),
                ["sum"] = typeof(Vector).Get(nameof(Vector.Sum)),
                ["prod"] = typeof(Vector).Get(nameof(Vector.Product)),
                ["product"] = typeof(Vector).Get(nameof(Vector.Product)),
                ["sqrt"] = typeof(Vector).Get(nameof(Vector.Sqrt)),
                ["amax"] = typeof(Vector).Get(nameof(Vector.AMax)),
                ["amin"] = typeof(Vector).Get(nameof(Vector.AMin)),
                ["max"] = typeof(Vector).Get(nameof(Vector.Maximum)),
                ["min"] = typeof(Vector).Get(nameof(Vector.Minimum)),
                ["mean"] = typeof(Vector).Get(nameof(Vector.Mean)),
                ["reverse"] = typeof(Vector).Get(nameof(Vector.Reverse)),
                ["distinct"] = typeof(Vector).Get(nameof(Vector.Distinct)),
                ["sort"] = typeof(Vector).Get(nameof(Vector.Sort)),
                ["fft"] = typeof(Vector).Get(nameof(Vector.Fft)),
                ["first"] = typeof(Vector).Prop(nameof(Vector.First)),
                ["last"] = typeof(Vector).Prop(nameof(Vector.Last)),
                ["acf"] = typeof(Vector).Get(nameof(Vector.ACF)),
            },
            [typeof(ComplexVector)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["length"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Length)),
                ["norm"] = typeof(ComplexVector).Get(nameof(ComplexVector.Norm)),
                ["amax"] = typeof(ComplexVector).Get(nameof(ComplexVector.AbsMax)),
                ["sqr"] = typeof(ComplexVector).Get(nameof(ComplexVector.Squared)),
                ["first"] = typeof(ComplexVector).Prop(nameof(ComplexVector.First)),
                ["last"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Last)),
                ["fft"] = typeof(ComplexVector).Get(nameof(ComplexVector.Fft)),
                ["sum"] = typeof(ComplexVector).Get(nameof(ComplexVector.Sum)),
                ["mean"] = typeof(ComplexVector).Get(nameof(ComplexVector.Mean)),
                ["reverse"] = typeof(ComplexVector).Get(nameof(ComplexVector.Reverse)),
                ["distinct"] = typeof(ComplexVector).Get(nameof(ComplexVector.Distinct)),
                ["magnitudes"] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
                ["amplitudes"] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
                ["mags"] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
                ["mag"] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
                ["phases"] = typeof(ComplexVector).Get(nameof(ComplexVector.Phases)),
                ["real"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Real)),
                ["re"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Real)),
                ["imaginary"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Imaginary)),
                ["imag"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Imaginary)),
                ["im"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Imaginary)),
            },
            [typeof(Library.MVO.MvoModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["length"] = typeof(Library.MVO.MvoModel).Prop(nameof(Library.MVO.MvoModel.Length)),
                ["first"] = typeof(Library.MVO.MvoModel).Prop(nameof(Library.MVO.MvoModel.First)),
                ["last"] = typeof(Library.MVO.MvoModel).Prop(nameof(Library.MVO.MvoModel.Last)),
                ["size"] = typeof(Library.MVO.MvoModel).Prop(nameof(Library.MVO.MvoModel.Size)),
            },
            [typeof(Library.MVO.Portfolio)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["weights"] = typeof(Library.MVO.Portfolio).Prop(nameof(Library.MVO.Portfolio.Weights)),
                ["lambda"] = typeof(Library.MVO.Portfolio).Prop(nameof(Library.MVO.Portfolio.Lambda)),
                ["ret"] = typeof(Library.MVO.Portfolio).Prop(nameof(Library.MVO.Portfolio.Mean)),
                ["var"] = typeof(Library.MVO.Portfolio).Prop(nameof(Library.MVO.Portfolio.Variance)),
                ["std"] = typeof(Library.MVO.Portfolio).Prop(nameof(Library.MVO.Portfolio.StdDev)),
            },
            [typeof(ARSModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["original"] = typeof(ARSModel).Prop(nameof(ARSModel.Original)),
                ["prediction"] = typeof(ARSModel).Prop(nameof(ARSModel.Prediction)),
                ["coefficients"] = typeof(ARSModel).Prop(nameof(ARSModel.Coefficients)),
                ["coeff"] = typeof(ARSModel).Prop(nameof(ARSModel.Coefficients)),
                ["r2"] = typeof(ARSModel).Prop(nameof(ARSModel.R2)),
                ["rss"] = typeof(ARSModel).Prop(nameof(ARSModel.ResidualSumSquares)),
                ["tss"] = typeof(ARSModel).Prop(nameof(ARSModel.TotalSumSquares)),
            },
            [typeof(ARVModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["original"] = typeof(ARVModel).Prop(nameof(ARVModel.Original)),
                ["prediction"] = typeof(ARVModel).Prop(nameof(ARVModel.Prediction)),
                ["coefficients"] = typeof(ARVModel).Prop(nameof(ARVModel.Coefficients)),
                ["coeff"] = typeof(ARVModel).Prop(nameof(ARVModel.Coefficients)),
                ["r2"] = typeof(ARVModel).Prop(nameof(ARVModel.R2)),
                ["rss"] = typeof(ARVModel).Prop(nameof(ARVModel.ResidualSumSquares)),
                ["tss"] = typeof(ARVModel).Prop(nameof(ARVModel.TotalSumSquares)),
            },
            [typeof(Point<Date>)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["value"] = typeof(Point<Date>).Prop(nameof(Point<Date>.Value)),
                ["date"] = typeof(Point<Date>).Prop(nameof(Point<Date>.Arg)),
            },
            [typeof(DateSpline)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["length"] = typeof(DateSpline).Prop(nameof(DateSpline.Length)),
            },
            [typeof(VectorSpline)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["length"] = typeof(DateSpline).Prop(nameof(DateSpline.Length)),
            },
            [typeof(Date)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["day"] = typeof(Date).Prop(nameof(Date.Day)),
                ["month"] = typeof(Date).Prop(nameof(Date.Month)),
                ["year"] = typeof(Date).Prop(nameof(Date.Year)),
                ["dow"] = typeof(Date).Prop(nameof(Date.DayOfWeek)),
                ["isleap"] = typeof(Date).Get(nameof(Date.IsLeap)),
            },
        };

    /// <summary>Avoids repeating the bang operator (!) in the code.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static MethodInfo Get(this Type type, string method) =>
         type.GetMethod(method)!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static MethodInfo Prop(this Type type, string property) =>
         type.GetProperty(property)!.GetGetMethod()!;

    private static NewExpression New(this Type type, params Expression[] args) =>
        Expression.New(type.GetConstructor(args.Select(a => a.Type).ToArray())!, args);

    private static NewExpression New(this Type type, List<Expression> args) =>
        Expression.New(type.GetConstructor(args.Select(a => a.Type).ToArray())!, args);

    private static NewArrayExpression Make(this Type type, IEnumerable<Expression> args) =>
        Expression.NewArrayInit(type, args);

    private static MethodCallExpression Call(this Type type,
        string method, Expression a1, Expression a2) =>
        Expression.Call(type.GetMethod(method, new[] { a1.Type, a2.Type })!, a1, a2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsArithmetic(Expression e1) =>
    e1.Type == typeof(int) || e1.Type == typeof(double);

    private static bool AreArithmeticTypes(Expression e1, Expression e2) =>
        IsArithmetic(e1) && IsArithmetic(e2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsVectorMatrix(Expression e1) =>
        e1.Type == typeof(Vector)
        || e1.Type == typeof(Matrix) || e1.Type == typeof(LMatrix);

    private static Expression ToDouble(Expression e) =>
        e.Type != typeof(int)
        ? e
        : e is ConstantExpression constExpr
        ? Expression.Constant((double)(int)constExpr.Value!, typeof(double))
        : Expression.Convert(e, typeof(double));

    private static bool DifferentTypes(ref Expression e1, ref Expression e2)
    {
        if (e1.Type != e2.Type)
        {
            if (e1.Type == typeof(Complex) && IsArithmetic(e2))
                e2 = Expression.Convert(e2, typeof(Complex));
            else if (e2.Type == typeof(Complex) && IsArithmetic(e1))
                e1 = Expression.Convert(e1, typeof(Complex));
            else
            {
                if (!AreArithmeticTypes(e1, e2))
                    return true;
                (e1, e2) = (ToDouble(e1), ToDouble(e2));
            }
        }
        return false;
    }

    private static List<Expression> AddExp(this List<Expression> args, Expression exp)
    {
        args.Add(exp);
        return args;
    }

    private static List<Expression> AddRandom(this List<Expression> args) =>
        args.AddExp(Expression.New(typeof(Random).GetConstructor(Array.Empty<Type>())!));

    private static List<Expression> AddNormalRandom(this List<Expression> args) =>
        args.AddExp(Expression.New(typeof(NormalRandom).GetConstructor(Array.Empty<Type>())!));

    private static AstException Error(string message, int position) =>
        new(message, position);

    private static AstException Error(string message, AstContext ctx) =>
        new(message, ctx.Lex.Current.Position);

    private static AstException Error(string message, in Lexeme lexeme) =>
        new(message, lexeme.Position);

    /// <summary>Gets a regex that matches a set statement</summary>
    [GeneratedRegex("^\\s*(?'header'let\\s+.+\\s+in\\s+)", RegexOptions.IgnoreCase, "es-ES")]
    private static partial Regex LetHeaderRegex();
}
