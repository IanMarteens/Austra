namespace Austra.Parser;

/// <summary>Syntactic and lexical analysis for AUSTRA.</summary>
internal sealed partial class Parser
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
                ["stats"] = typeof(Series).Prop(nameof(Series.Stats)),
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
                ["stats"] = typeof(Series<int>).Prop(nameof(Series<int>.Stats)),
                ["first"] = typeof(Series<int>).Get(nameof(Series<int>.First)),
                ["last"] = typeof(Series<int>).Get(nameof(Series<int>.Last)),
                ["values"] = typeof(Series<int>).Get(nameof(Series<int>.GetValues)),
                ["sum"] = typeof(Series<int>).Get(nameof(Series<int>.Sum)),
            },
            [typeof(Series<double>)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["stats"] = typeof(Series<double>).Prop(nameof(Series<double>.Stats)),
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

    private static readonly Dictionary<Type, MemberDescription[]> members = new()
    {
        [typeof(Series)] = new MemberDescription[]
        {
            new("count", "Gets the number of points"),
            new("min", "Gets the minimum value"),
            new("max", "Gets the maximum value"),
            new("mean", "Gets the mean value"),
            new("var", "Gets the variance"),
            new("varp", "Gets the variance of the population"),
            new("std", "Gets the standard deviation"),
            new("stdp", "Gets the standard deviation of the population"),
            new("skew", "Gets the skewness"),
            new("skewp", "Gets the skewness of the population"),
            new("kurt", "Gets the kurtosis"),
            new("kurtp", "Gets the kurtosis of the population"),
            new("sum", "Gets the sum of all values"),
            new("stats", "Gets all statistics"),
            new("first", "Gets the first point"),
            new("last", "Gets the last point"),
            new("rets", "Gets the linear returns"),
            new("logs", "Gets the logarithmic returns"),
            new("fft", "Performs a Fast Fourier Transform"),
            new("perc", "Gets the percentiles"),
            new("values", "Gets the underlying vector of values"),
            new("random", "Generates a random series"),
            new("movingRet", "Gets the moving monthly/yearly return"),
            new("type", "Gets the type of the series"),
            new("amax", "Gets the maximum absolute value"),
            new("amin", "Gets the minimum absolute value"),
            new("ncdf", "Gets the percentile according to the normal distribution"),
            new("fit", "Gets coefficients for a linear fit"),
            new("linearfit", "Gets a line fitting the original series"),
            new("autocorr(", "Gets the autocorrelation given a lag"),
            new("corr(", "Gets the correlation with another given series"),
            new("cov(", "Gets the covariance with another given series"),
            new("correlogram(", "Gets all autocorrelations up to a given lag"),
            new("linear(", "Gets the regression coefficients given a list of series"),
            new("linearModel(", "Creates a linear model"),
            new("stats(", "Gets monthly statistics for a given date"),
            new("movingAvg(", "Calculates a Simple Moving Average"),
            new("movingStd(", "Calculates a Moving Standard Deviation"),
            new("movingNcdf(", "Calculates a Moving Normal Percentile"),
            new("ewma(", "Calculates an Exponentially Weighted Moving Average"),
            new("map(x => ", "Pointwise transformation of the series"),
            new("any(x => ", "Existential operator"),
            new("all(x => ", "Universal operator"),
            new("zip(", "Combines two series"),
            new("filter(x => ", "Filters points by values or dates"),
            new("indexof(", "Returns the index where a value is stored"),
            new("ar(", "Calculates the autoregression coefficients"),
            new("arModel(", "Creates an AR(p) model"),
            new("spline", "Creates a cubic interpolator"),
            new("acf", "AutoCorrelation Function"),
        },
        [typeof(Series<int>)] = new MemberDescription[]
        {
            new("stats", "Gets all statistics"),
            new("first", "Gets the first point"),
            new("last", "Gets the last point"),
            new("values", "Gets the underlying vector of values"),
        },
        [typeof(Series<double>)] = new MemberDescription[]
        {
            new("stats", "Gets all statistics"),
            new("first", "Gets the first point"),
            new("last", "Gets the last point"),
            new("values", "Gets the underlying vector of values"),
        },
        [typeof(Acc)] = new MemberDescription[]
        {
            new("count", "Gets the number of points"),
            new("min", "Gets the minimum value"),
            new("max", "Gets the maximum value"),
            new("mean", "Gets the mean value"),
            new("var", "Gets the variance"),
            new("varp", "Gets the variance of the population"),
            new("std", "Gets the standard deviation"),
            new("stdp", "Gets the standard deviation of the population"),
            new("skew", "Gets the skewness"),
            new("skewp", "Gets the skewness of the population"),
            new("kurt", "Gets the kurtosis"),
            new("kurtp", "Gets the kurtosis of the population"),
        },
        [typeof(Matrix)] = new MemberDescription[]
        {
            new("rows", "Gets the number of rows"),
            new("cols", "Gets the number of columns"),
            new("det", "Calculates the determinant"),
            new("trace", "Gets the sum of the main diagonal"),
            new("amax", "Gets the maximum absolute value"),
            new("amin", "Gets the minimum absolute value"),
            new("max", "Gets the maximum value"),
            new("min", "Gets the minimum absolute value"),
            new("chol", "Calculates the Cholesky Decomposition"),
            new("evd", "Calculates the EigenValues Decomposition"),
            new("diag", "Extracts the diagonal as a vector"),
            new("inverse", "Calculates the inverse of a square matrix"),
            new("getRow(", "Extracts a row as a vector"),
            new("getCol(", "Extracts a column as a vector"),
            new("map(x => ", "Pointwise transformation of matrix cells"),
            new("any(x => ", "Existential operator"),
            new("all(x => ", "Universal operator"),
        },
        [typeof(LMatrix)] = new MemberDescription[]
        {
            new("rows", "Gets the number of rows"),
            new("cols", "Gets the number of columns"),
            new("det", "Calculates the determinant"),
            new("trace", "Gets the sum of the main diagonal"),
            new("amax", "Gets the maximum absolute value"),
            new("amin", "Gets the minimum absolute value"),
            new("max", "Gets the maximum value"),
            new("min", "Gets the minimum absolute value"),
            new("diag", "Extracts the diagonal as a vector"),
        },
        [typeof(EVD)] = new MemberDescription[]
        {
            new("vectors", "Gets a matrix with eigenvectors as its columns"),
            new("values", "Gets all the eigenvalues"),
            new("d", "Gets a quasi-diagonal real matrix with all eigenvalues")
        },
        [typeof(LinearSModel)] = new MemberDescription[]
        {
            new("original", "Gets the series to be explained"),
            new("prediction", "Gets the predicted series"),
            new("weights", "Gets the regression coefficients"),
            new("r2", "Gets the regression coefficient"),
            new("rss", "Gets the Residual Sum of Squares"),
            new("tss", "Gets the Total Sum of Squares"),
        },
        [typeof(LinearVModel)] = new MemberDescription[]
        {
            new("original", "Gets the vector to be explained"),
            new("prediction", "Gets the predicted vector"),
            new("weights", "Gets the regression coefficients"),
            new("r2", "Gets the regression coefficient"),
            new("rss", "Gets the Residual Sum of Squares"),
            new("tss", "Gets the Total Sum of Squares"),
        },
        [typeof(ARSModel)] = new MemberDescription[]
        {
            new("original", "Gets the series to be explained"),
            new("prediction", "Gets the predicted series"),
            new("coefficients", "Gets the autoregression coefficients"),
            new("r2", "Gets the regression coefficient"),
            new("rss", "Gets the Residual Sum of Squares"),
            new("tss", "Gets the Total Sum of Squares"),
        },
        [typeof(ARVModel)] = new MemberDescription[]
        {
            new("original", "Gets the vector to be explained"),
            new("prediction", "Gets the predicted vector"),
            new("coefficients", "Gets the autoregression coefficients"),
            new("r2", "Gets the regression coefficient"),
            new("rss", "Gets the Residual Sum of Squares"),
            new("tss", "Gets the Total Sum of Squares"),
        },
        [typeof(Vector)] = new MemberDescription[]
        {
            new("length", "Gets the number of items"),
            new("norm", "Gets the norm of the vector"),
            new("sqr", "Gets the squared norm of the vector"),
            new("sum", "Gets the sum of all values"),
            new("prod", "Gets the product of all values"),
            new("mean", "Gets the mean value"),
            new("sqrt", "Pointwise squared root"),
            new("abs", "Pointwise absolute value"),
            new("stats", "Gets all the statistics"),
            new("amax", "Gets the maximum absolute value"),
            new("amin", "Gets the minimum absolute value"),
            new("max", "Gets the maximum  value"),
            new("min", "Gets the minimum value"),
            new("reverse", "Gets a reversed copy"),
            new("distinct", "Gets a new vector with distinct values"),
            new("sort", "Gets a new vector with sorted values"),
            new("first", "Gets the first item from the vector"),
            new("last", "Gets the last item from the vector"),
            new("fft", "Performs a Fast Fourier Transform"),
            new("autocorr(", "Gets the autocorrelation given a lag"),
            new("correlogram(", "Gets all autocorrelations up to a given lag"),
            new("map(x => ", "Pointwise transformation of vector items"),
            new("filter(x => ", "Filters items by value"),
            new("any(x => ", "Existential operator"),
            new("all(x => ", "Universal operator"),
            new("zip(", "Combines two vectors"),
            new("reduce(", "Reduces a vector to a single value"),
            new("indexOf(", "Returns the index where a value is stored"),
            new("linear(", "Gets the regression coefficients given a list of vectors"),
            new("linearModel(", "Creates a linear model"),
            new("ar(", "Calculates the autoregression coefficients"),
            new("arModel(", "Creates an AR(p) model"),
            new("indexof(", "Returns the index where a value is stored"),
            new("linear(", "Gets the regression coefficients given a list of vectors"),
            new("linearModel(", "Creates a linear model"),
            new("ar(", "Calculates the autoregression coefficients"),
            new("arModel(", "Creates an AR(p) model"),
            new("acf", "AutoCorrelation Function"),
        },
        [typeof(ComplexVector)] = new MemberDescription[]
        {
            new("length", "Gets the number of items"),
            new("norm", "Gets the norm of the vector"),
            new("amax", "Gets the maximum absolute value"),
            new("sqr", "Gets the squared norm of the vector"),
            new("sum", "Gets the sum of all values"),
            new("mean", "Gets the mean value"),
            new("first", "Gets the first item from the vector"),
            new("last", "Gets the last item from the vector"),
            new("reverse", "Gets a reversed copy"),
            new("distinct", "Gets a new vector with distinct values"),
            new("fft", "Performs a Fast Fourier Transform"),
            new("magnitudes", "Gets magnitudes as a vector"),
            new("phases", "Gets phases as a vector"),
            new("real", "Gets the real components as a vector"),
            new("imag", "Gets the imaginary components as a vector"),
            new("map(x => ", "Pointwise transformation of complex values"),
            new("mapreal(x => ", "Transforms complex vector into a real one"),
            new("filter(x => ", "Filters items by value"),
            new("any(x => ", "Existential operator"),
            new("all(x => ", "Universal operator"),
            new("zip(", "Combines two complex vectors"),
            new("reduce(", "Reduces a complex vector to a single value"),
            new("indexof(", "Returns the index where a value is stored"),
        },
        [typeof(Library.MVO.MvoModel)] = new MemberDescription[]
        {
            new("length", "Gets the number of corner portfolios"),
            new("first", "Gets the first corner portfolio"),
            new("last", "Gets the last corner portfolio"),
            new("size", "Gets the number of assets in the model"),
        },
        [typeof(Library.MVO.Portfolio)] = new MemberDescription[]
        {
            new("weights", "Gets weights of the portfolio"),
            new("lambda", "Gets the lambda of a corner portfolio"),
            new("ret", "Gets the expected return of the portfolio"),
            new("std", "Gets the standard deviation of the portfolio"),
            new("var", "Gets the variance of the portfolio"),
        },
        [typeof(Point<Date>)] = new MemberDescription[]
        {
            new("value", "Gets the numerical value of the point"),
            new("date", "Gets the date argument"),
        },
        [typeof(Date)] = new MemberDescription[]
        {
            new("day", "Gets the day of the date"),
            new("month", "Gets the month of the date"),
            new("year", "Gets the year of the date"),
            new("dow", "Gets the day of week the date"),
            new("isleap", "Checks if the date belong to a leap year"),
            new("addMonths(", "Adds a number of months to the date"),
            new("addYears(", "Adds a number of years to the date"),
        },
        [typeof(Complex)] = new MemberDescription[]
        {
            new("real", "Gets the real part of the complex number"),
            new("imag", "Gets the imaginary part of the complex number"),
            new("mag", "Gets the magnitude of the complex number"),
            new("phase", "Gets the phase of the complex number"),
        },
        [typeof(FftCModel)] = new MemberDescription[]
        {
            new("amplitudes", "Gets the amplitudes of the FFT"),
            new("phases", "Gets the phases of the FFT"),
            new("length", "Gets the length of the FFT"),
            new("inverse", "Gets the inverse of the transform as a complex vector"),
            new("values", "Gets the full spectrum as a complex vector"),
        },
        [typeof(FftRModel)] = new MemberDescription[]
        {
            new("amplitudes", "Gets the amplitudes of the FFT"),
            new("phases", "Gets the phases of the FFT"),
            new("length", "Gets the length of the FFT"),
            new("inverse", "Gets the inverse of the transform as a real vector"),
            new("values", "Gets the full spectrum as a complex vector"),
        },
        [typeof(Polynomial)] = new MemberDescription[]
        {
            new("eval", "Evaluates the polynomial at a point between 0 and 1"),
            new("derivative", "Gets the derivative at a point between 0 and 1"),
        },
    };

    private static readonly Dictionary<string, MemberDescription[]> classMembers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["series"] = new MemberDescription[]
            {
                new("new(", "Creates a new series using weights and a list of series"),
            },
            ["spline"] = new MemberDescription[]
            {
                new("new(", "Creates a new interpolator from two vectors"),
                new("grid(", "Approximates a function with a spline"),
            },
            ["model"] = new MemberDescription[]
            {
                new("mvo(", "Creates a model for a Mean Variance Optimizer"),
                new("compare(", "Compares two series or two vectors"),
            },
            ["vector"] = new MemberDescription[]
            {
                new("new(", "Create a vector given an initialization lambda"),
                new("random(", "Creates a random vector given a length"),
                new("nrandom(", "Creates a random vector using a standard normal distribution given a length"),
                new("zero(", "Creates a vector with zeros given a length"),
                new("ones(", "Creates a vector with ones given a length"),
            },
            ["complexvector"] = new MemberDescription[]
            {
                new("new(", "Create a complex vector given an initialization lambda"),
                new("random(", "Creates a random complex vector given a length"),
                new("nrandom(", "Creates a random vector using a standard normal distribution given a length"),
                new("zero(", "Creates a complex vector with zeros given a length"),
                new("from(", "Creates a complex vector from one or two real vectors"),
            },
            ["matrix"] = new MemberDescription[]
            {
                new("new(", "Create a rectangular matrix given an initialization lambda"),
                new("random(", "Creates a random matrix given a size"),
                new("nrandom(", "Creates a random matrix using a standard normal distribution given a size"),
                new("lrandom(", "Creates a random lower triangular matrix given a size"),
                new("lnrandom(", "Creates a random lower triangular matrix with a standard normal distribution"),
                new("zero(", "Creates a matrix with zeros given a size"),
                new("eye(", "Creates an identity matrix given a size"),
                new("diag(", "Creates an diagonal matrix from a vector"),
                new("rows(", "Creates a matrix given its rows as vectors"),
                new("corr(", "Creates a correlation matrix given a list of series"),
                new("cov(", "Creates a covariance matrix given a list of series"),
            },
        };

    /// <summary>Gets a regex that matches a set statement.</summary>
    [GeneratedRegex("^\\s*(?'header'let\\s+.+\\s+in\\s+)", RegexOptions.IgnoreCase)]
    private static partial Regex LetHeaderRegex();

    /// <summary>Gets a regex that matches a lambda header.</summary>
    [GeneratedRegex(@"^(\w+|\(\s*\w+\s*\,\s*\w+\s*\))\s*\=\>")]
    private static partial Regex IsLambdaHeader();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsLambda() => IsLambdaHeader().IsMatch(text.AsSpan()[start..]);

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="source">A data source.</param>
    /// <param name="text">An expression fragment.</param>
    /// <param name="type">The type of the expression fragment.</param>
    /// <returns>A list of pairs member name/description.</returns>
    public static IList<MemberDescription> GetMembers(
        IDataSource source,
        string text,
        out Type? type)
    {
        string trimmedText = ExtractObjectPath(text).Trim();
        if (!string.IsNullOrEmpty(trimmedText))
            try
            {
                return ExtractType(source, trimmedText);
            }
            catch
            {
                // Give it a second chance, if a let clause was not included.
                Match m = LetHeaderRegex().Match(text);
                if (m.Success && !LetHeaderRegex().IsMatch(trimmedText))
                    try
                    {
                        return ExtractType(source, m.Groups["header"] + trimmedText);
                    }
                    catch { }
            }
        type = null;
        return Array.Empty<MemberDescription>();

        static IList<MemberDescription> ExtractType(IDataSource source, string text)
        {
            Type type = new Parser(source, text).ParseType();
            return members.TryGetValue(type, out MemberDescription[]? list)
                ? list : Array.Empty<MemberDescription>();
        }
    }

    /// <summary>Gets a list of class members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>A list of pairs member name/description.</returns>
    public static IList<MemberDescription> GetClassMembers(
        string text)
    {
        return classMembers.TryGetValue(ExtractClassName(text),
            out MemberDescription[]? list)
            ? list
            : (IList<MemberDescription>)Array.Empty<MemberDescription>();

        static string ExtractClassName(string text)
        {
            int i = text.EndsWith("::") ? text.Length - 2 : text.Length - 1;
            if (i < 0 || text[i--] != ':')
                return "";
            while (i >= 0 && char.IsWhiteSpace(text, i))
                i--;
            if (i < 0)
                return "";
            int end = i + 1;
            while (i >= 0 && char.IsLetter(text, i))
                i--;
            return text[(i + 1)..end].Trim();
        }
    }

    /// <summary>Extracts an object path from an expression fragment.</summary>
    /// <param name="text">A fragment of an expression.</param>
    /// <returns>The final object path.</returns>
    private static string ExtractObjectPath(string text)
    {
        int i = text.Length - 1;
        while (i >= 0)
        {
            char ch = text[i];
            if (char.IsLetterOrDigit(ch) ||
                ch is '_' or '.' or ':' or '=' or '\'' ||
                char.IsWhiteSpace(ch))
                i--;
            else if (ch is '(' or '[')
                return text[(i + 1)..];
            else if (ch == ')')
            {
                int count = 1;
                while (--i >= 0)
                {
                    if (text[i] == ')')
                        count++;
                    else if (text[i] == '(')
                    {
                        if (--count == 0)
                            break;
                    }
                }
                if (count > 0)
                    return "";
                else
                    i--;
            }
            else if (ch == ']')
            {
                int count = 1;
                while (--i >= 0)
                {
                    if (text[i] == ']')
                        count++;
                    else if (text[i] == '[')
                    {
                        if (--count == 0)
                            break;
                    }
                }
                if (count > 0)
                    return "";
                else
                    i--;
            }
            else
                break;
        }
        return text[(i + 1)..];
    }
}
