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

    private static readonly Dictionary<Type, (string name, string description)[]> members = new()
    {
        [typeof(Series)] = new[]
        {
            ("count", "Gets the number of points"),
            ("min", "Gets the minimum value"),
            ("max", "Gets the maximum value"),
            ("mean", "Gets the mean value"),
            ("var", "Gets the variance"),
            ("varp", "Gets the variance of the population"),
            ("std", "Gets the standard deviation"),
            ("stdp", "Gets the standard deviation of the population"),
            ("skew", "Gets the skewness"),
            ("skewp", "Gets the skewness of the population"),
            ("kurt", "Gets the kurtosis"),
            ("kurtp", "Gets the kurtosis of the population"),
            ("sum", "Gets the sum of all values"),
            ("stats", "Gets all statistics"),
            ("first", "Gets the first point"),
            ("last", "Gets the last point"),
            ("rets", "Gets the linear returns"),
            ("logs", "Gets the logarithmic returns"),
            ("fft", "Performs a Fast Fourier Transform"),
            ("perc", "Gets the percentiles"),
            ("values", "Gets the underlying vector of values"),
            ("random", "Generates a random series"),
            ("movingRet", "Gets the moving monthly/yearly return"),
            ("type", "Gets the type of the series"),
            ("amax", "Gets the maximum absolute value"),
            ("amin", "Gets the minimum absolute value"),
            ("ncdf", "Gets the percentile according to the normal distribution"),
            ("fit", "Gets coefficients for a linear fit"),
            ("linearfit", "Gets a line fitting the original series"),
            ("autocorr(", "Gets the autocorrelation given a lag"),
            ("corr(", "Gets the correlation with another given series"),
            ("cov(", "Gets the covariance with another given series"),
            ("correlogram(", "Gets all autocorrelations up to a given lag"),
            ("linear(", "Gets the regression coefficients given a list of series"),
            ("linearModel(", "Creates a linear model"),
            ("stats(", "Gets monthly statistics for a given date"),
            ("movingAvg(", "Calculates a Simple Moving Average"),
            ("movingStd(", "Calculates a Moving Standard Deviation"),
            ("movingNcdf(", "Calculates a Moving Normal Percentile"),
            ("ewma(", "Calculates an Exponentially Weighted Moving Average"),
            ("map(x => ", "Pointwise transformation of the series"),
            ("any(x => ", "Existential operator"),
            ("all(x => ", "Universal operator"),
            ("zip(", "Combines two series"),
            ("filter(x => ", "Filters points by values or dates"),
            ("indexof(", "Returns the index where a value is stored"),
            ("ar(", "Calculates the autoregression coefficients"),
            ("arModel(", "Creates an AR(p) model"),
            ("spline", "Creates a cubic interpolator"),
            ("acf", "AutoCorrelation Function"),
        },
        [typeof(Series<int>)] = new[]
        {
            ("stats", "Gets all statistics"),
            ("first", "Gets the first point"),
            ("last", "Gets the last point"),
            ("values", "Gets the underlying vector of values"),
        },
        [typeof(Series<double>)] = new[]
        {
            ("stats", "Gets all statistics"),
            ("first", "Gets the first point"),
            ("last", "Gets the last point"),
            ("values", "Gets the underlying vector of values"),
        },
        [typeof(Acc)] = new[]
        {
            ("count", "Gets the number of points"),
            ("min", "Gets the minimum value"),
            ("max", "Gets the maximum value"),
            ("mean", "Gets the mean value"),
            ("var", "Gets the variance"),
            ("varp", "Gets the variance of the population"),
            ("std", "Gets the standard deviation"),
            ("stdp", "Gets the standard deviation of the population"),
            ("skew", "Gets the skewness"),
            ("skewp", "Gets the skewness of the population"),
            ("kurt", "Gets the kurtosis"),
            ("kurtp", "Gets the kurtosis of the population"),
        },
        [typeof(Matrix)] = new[]
        {
            ("rows", "Gets the number of rows"),
            ("cols", "Gets the number of columns"),
            ("det", "Calculates the determinant"),
            ("trace", "Gets the sum of the main diagonal"),
            ("amax", "Gets the maximum absolute value"),
            ("amin", "Gets the minimum absolute value"),
            ("max", "Gets the maximum value"),
            ("min", "Gets the minimum absolute value"),
            ("chol", "Calculates the Cholesky Decomposition"),
            ("evd", "Calculates the EigenValues Decomposition"),
            ("diag", "Extracts the diagonal as a vector"),
            ("inverse", "Calculates the inverse of a square matrix"),
            ("getRow(", "Extracts a row as a vector"),
            ("getCol(", "Extracts a column as a vector"),
            ("map(x => ", "Pointwise transformation of matrix cells"),
            ("any(x => ", "Existential operator"),
            ("all(x => ", "Universal operator"),
        },
        [typeof(LMatrix)] = new[]
        {
            ("rows", "Gets the number of rows"),
            ("cols", "Gets the number of columns"),
            ("det", "Calculates the determinant"),
            ("trace", "Gets the sum of the main diagonal"),
            ("amax", "Gets the maximum absolute value"),
            ("amin", "Gets the minimum absolute value"),
            ("max", "Gets the maximum value"),
            ("min", "Gets the minimum absolute value"),
            ("diag", "Extracts the diagonal as a vector"),
        },
        [typeof(EVD)] = new[]
        {
            ("vectors", "Gets a matrix with eigenvectors as its columns"),
            ("values", "Gets all the eigenvalues"),
            ("d", "Gets a quasi-diagonal real matrix with all eigenvalues")
        },
        [typeof(LinearSModel)] = new[]
        {
            ("original", "Gets the series to be explained"),
            ("prediction", "Gets the predicted series"),
            ("weights", "Gets the regression coefficients"),
            ("r2", "Gets the regression coefficient"),
            ("rss", "Gets the Residual Sum of Squares"),
            ("tss", "Gets the Total Sum of Squares"),
        },
        [typeof(LinearVModel)] = new[]
        {
            ("original", "Gets the vector to be explained"),
            ("prediction", "Gets the predicted vector"),
            ("weights", "Gets the regression coefficients"),
            ("r2", "Gets the regression coefficient"),
            ("rss", "Gets the Residual Sum of Squares"),
            ("tss", "Gets the Total Sum of Squares"),
        },
        [typeof(ARSModel)] = new[]
        {
            ("original", "Gets the series to be explained"),
            ("prediction", "Gets the predicted series"),
            ("coefficients", "Gets the autoregression coefficients"),
            ("r2", "Gets the regression coefficient"),
            ("rss", "Gets the Residual Sum of Squares"),
            ("tss", "Gets the Total Sum of Squares"),
        },
        [typeof(ARVModel)] = new[]
        {
            ("original", "Gets the vector to be explained"),
            ("prediction", "Gets the predicted vector"),
            ("coefficients", "Gets the autoregression coefficients"),
            ("r2", "Gets the regression coefficient"),
            ("rss", "Gets the Residual Sum of Squares"),
            ("tss", "Gets the Total Sum of Squares"),
        },
        [typeof(Vector)] = new[]
        {
            ("length", "Gets the number of items"),
            ("norm", "Gets the norm of the vector"),
            ("sqr", "Gets the squared norm of the vector"),
            ("sum", "Gets the sum of all values"),
            ("prod", "Gets the product of all values"),
            ("mean", "Gets the mean value"),
            ("sqrt", "Pointwise squared root"),
            ("abs", "Pointwise absolute value"),
            ("stats", "Gets all the statistics"),
            ("amax", "Gets the maximum absolute value"),
            ("amin", "Gets the minimum absolute value"),
            ("max", "Gets the maximum  value"),
            ("min", "Gets the minimum value"),
            ("reverse", "Gets a reversed copy"),
            ("distinct", "Gets a new vector with distinct values"),
            ("sort", "Gets a new vector with sorted values"),
            ("first", "Gets the first item from the vector"),
            ("last", "Gets the last item from the vector"),
            ("fft", "Performs a Fast Fourier Transform"),
            ("autocorr(", "Gets the autocorrelation given a lag"),
            ("correlogram(", "Gets all autocorrelations up to a given lag"),
            ("map(x => ", "Pointwise transformation of vector items"),
            ("filter(x => ", "Filters items by value"),
            ("any(x => ", "Existential operator"),
            ("all(x => ", "Universal operator"),
            ("zip(", "Combines two vectors"),
            ("reduce(", "Reduces a vector to a single value"),
            ("indexOf(", "Returns the index where a value is stored"),
            ("linear(", "Gets the regression coefficients given a list of vectors"),
            ("linearModel(", "Creates a linear model"),
            ("ar(", "Calculates the autoregression coefficients"),
            ("arModel(", "Creates an AR(p) model"),
            ("indexof(", "Returns the index where a value is stored"),
            ("linear(", "Gets the regression coefficients given a list of vectors"),
            ("linearModel(", "Creates a linear model"),
            ("ar(", "Calculates the autoregression coefficients"),
            ("arModel(", "Creates an AR(p) model"),
            ("acf", "AutoCorrelation Function"),
        },
        [typeof(ComplexVector)] = new[]
        {
            ("length", "Gets the number of items"),
            ("norm", "Gets the norm of the vector"),
            ("amax", "Gets the maximum absolute value"),
            ("sqr", "Gets the squared norm of the vector"),
            ("sum", "Gets the sum of all values"),
            ("mean", "Gets the mean value"),
            ("first", "Gets the first item from the vector"),
            ("last", "Gets the last item from the vector"),
            ("reverse", "Gets a reversed copy"),
            ("distinct", "Gets a new vector with distinct values"),
            ("fft", "Performs a Fast Fourier Transform"),
            ("magnitudes", "Gets magnitudes as a vector"),
            ("phases", "Gets phases as a vector"),
            ("real", "Gets the real components as a vector"),
            ("imag", "Gets the imaginary components as a vector"),
            ("map(x => ", "Pointwise transformation of complex values"),
            ("mapreal(x => ", "Transforms complex vector into a real one"),
            ("filter(x => ", "Filters items by value"),
            ("any(x => ", "Existential operator"),
            ("all(x => ", "Universal operator"),
            ("zip(", "Combines two complex vectors"),
            ("reduce(", "Reduces a complex vector to a single value"),
            ("indexof(", "Returns the index where a value is stored"),
        },
        [typeof(Library.MVO.MvoModel)] = new[]
        {
            ("length", "Gets the number of corner portfolios"),
            ("first", "Gets the first corner portfolio"),
            ("last", "Gets the last corner portfolio"),
            ("size", "Gets the number of assets in the model"),
        },
        [typeof(Library.MVO.Portfolio)] = new[]
        {
            ("weights", "Gets weights of the portfolio"),
            ("lambda", "Gets the lambda of a corner portfolio"),
            ("ret", "Gets the expected return of the portfolio"),
            ("std", "Gets the standard deviation of the portfolio"),
            ("var", "Gets the variance of the portfolio"),
        },
        [typeof(Point<Date>)] = new[]
        {
            ("value", "Gets the numerical value of the point"),
            ("date", "Gets the date argument"),
        },
        [typeof(Date)] = new[]
        {
            ("day", "Gets the day of the date"),
            ("month", "Gets the month of the date"),
            ("year", "Gets the year of the date"),
            ("dow", "Gets the day of week the date"),
            ("isleap", "Checks if the date belong to a leap year"),
            ("addMonths(", "Adds a number of months to the date"),
            ("addYears(", "Adds a number of years to the date"),
        },
        [typeof(Complex)] = new[]
        {
            ("real", "Gets the real part of the complex number"),
            ("imag", "Gets the imaginary part of the complex number"),
            ("mag", "Gets the magnitude of the complex number"),
            ("phase", "Gets the phase of the complex number"),
        },
        [typeof(FftCModel)] = new[]
        {
            ("amplitudes", "Gets the amplitudes of the FFT"),
            ("phases", "Gets the phases of the FFT"),
            ("length", "Gets the length of the FFT"),
            ("inverse", "Gets the inverse of the transform as a complex vector"),
            ("values", "Gets the full spectrum as a complex vector"),
        },
        [typeof(FftRModel)] = new[]
        {
            ("amplitudes", "Gets the amplitudes of the FFT"),
            ("phases", "Gets the phases of the FFT"),
            ("length", "Gets the length of the FFT"),
            ("inverse", "Gets the inverse of the transform as a real vector"),
            ("values", "Gets the full spectrum as a complex vector"),
        },
        [typeof(Polynomial)] = new[]
        {
            ("eval", "Evaluates the polynomial at a point between 0 and 1"),
            ("derivative", "Gets the derivative at a point between 0 and 1"),
        },
    };

    private static readonly Dictionary<string, (string name, string description)[]> classMembers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["series"] = new[]
            {
                ("new(", "Creates a new series using weights and a list of series"),
            },
            ["spline"] = new[]
            {
                ("new(", "Creates a new interpolator from two vectors"),
                ("grid(", "Approximates a function with a spline"),
            },
            ["model"] = new[]
            {
                ("mvo(", "Creates a model for a Mean Variance Optimizer"),
                ("compare(", "Compares two series or two vectors"),
            },
            ["vector"] = new[]
            {
                ("new(", "Create a vector given an initialization lambda"),
                ("random(", "Creates a random vector given a length"),
                ("nrandom(", "Creates a random vector using a standard normal distribution given a length"),
                ("zero(", "Creates a vector with zeros given a length"),
                ("ones(", "Creates a vector with ones given a length"),
            },
            ["complexvector"] = new[]
            {
                ("new(", "Create a complex vector given an initialization lambda"),
                ("random(", "Creates a random complex vector given a length"),
                ("nrandom(", "Creates a random vector using a standard normal distribution given a length"),
                ("zero(", "Creates a complex vector with zeros given a length"),
                ("from(", "Creates a complex vector from one or two real vectors"),
            },
            ["matrix"] = new[]
            {
                ("new(", "Create a rectangular matrix given an initialization lambda"),
                ("random(", "Creates a random matrix given a size"),
                ("nrandom(", "Creates a random matrix using a standard normal distribution given a size"),
                ("lrandom(", "Creates a random lower triangular matrix given a size"),
                ("lnrandom(", "Creates a random lower triangular matrix with a standard normal distribution"),
                ("zero(", "Creates a matrix with zeros given a size"),
                ("eye(", "Creates an identity matrix given a size"),
                ("diag(", "Creates an diagonal matrix from a vector"),
                ("rows(", "Creates a matrix given its rows as vectors"),
                ("corr(", "Creates a correlation matrix given a list of series"),
                ("cov(", "Creates a covariance matrix given a list of series"),
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
    public static IList<(string member, string description)> GetMembers(
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
        return Array.Empty<(string, string)>();

        static IList<(string member, string description)> ExtractType(IDataSource source, string text)
        {
            Type type = new Parser(source, text).ParseType();
            return members.TryGetValue(type, out (string name, string description)[]? list)
                ? list : Array.Empty<(string, string)>();
        }
    }

    /// <summary>Gets a list of class members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>A list of pairs member name/description.</returns>
    public static IList<(string member, string description)> GetClassMembers(
        string text)
    {
        return classMembers.TryGetValue(ExtractClassName(text),
            out (string name, string description)[]? list)
            ? list
            : (IList<(string member, string description)>)Array.Empty<(string, string)>();

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
