namespace Austra.Parser;

/// <summary>A symbol table for predefined classes and methods.</summary>
/// <remarks>
/// This class is instantiated from <see cref="AustraEngine"/> and acts
/// like a singleton for all the lifetime of the session.
/// </remarks>
internal sealed partial class ParserBindings
{
    /// <summary>Most common argument list in functions.</summary>
    private static readonly Type[] DoubleArg = [typeof(double)];
    /// <summary>Second common argument list in functions.</summary>
    private static readonly Type[] IntArg = [typeof(int)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] VectorArg = [typeof(Vector)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] ComplexArg = [typeof(Complex)];
    /// <summary>Another common argument list in functions: two integer parameters.</summary>
    private static readonly Type[] IntIntArg = [typeof(int), typeof(int)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] DoubleDoubleArg = [typeof(double), typeof(double)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] VectorVectorArg = [typeof(Vector), typeof(Vector)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] DoubleVectorArg = [typeof(double), typeof(Vector)];

    /// <summary>Code completion descriptors for root classes.</summary>
    private readonly Member[] rootClasses =
    [
        new("cvec::", "Allows access to complex vector constructors"),
        new("matrix::", "Allows access to matrix constructors"),
        new("model::", "Allows access to model constructors"),
        new("series::", "Allows access to series constructors"),
        new("spline::", "Allows access to spline constructors"),
        new("vec::", "Allows access to vector constructors"),
        new("seq::", "Allows access to sequence constructors"),
        new("math::", "Allows access to mathematical functions"),
    ];

    /// <summary>Code completion descriptors for class methods or constructors.</summary>
    private readonly FrozenDictionary<string, Member[]> classMembers =
        new Dictionary<string, Member[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["series"] = [
                new("new(", "Creates a new series using weights and a list of series"),
            ],
            ["seq"] = [
                new("new(", "Creates a sequence from a range, a grid or a vector"),
                new("nrandom(", "Creates a sequence from normal random numbers"),
                new("random(", "Creates a sequence from random numbers"),
            ],
            ["spline"] = [
                new("new(", "Creates a new interpolator either from two vectors, a series, or from a function"),
            ],
            ["model"] = [
                new("mvo(", "Creates a model for a Mean Variance Optimizer"),
                new("plot(", "Plots vectors, series and sequences"),
            ],
            ["vec"] = [
                new("new(", "Create a vector given a length and an optional lambda"),
                new("ones(", "Creates a vector with ones given a length"),
                new("nrandom(", "Creates a random vector using a standard normal distribution given a length"),
                new("random(", "Creates a random vector given a length"),
            ],
            ["cvec"] = [
                new("new(", "Create a complex vector given a size and an optional lambda"),
                new("nrandom(", "Creates a random vector using a standard normal distribution given a length"),
                new("random(", "Creates a random complex vector given a length"),
            ],
            ["matrix"] = [
                new("cols(", "Creates a matrix given its columns as vectors"),
                new("corr(", "Creates a correlation matrix given a list of series"),
                new("cov(", "Creates a covariance matrix given a list of series"),
                new("diag(", "Creates an diagonal matrix from a vector"),
                new("eye(", "Creates an identity matrix given a size"),
                new("lrandom(", "Creates a random lower triangular matrix given a size"),
                new("lnrandom(", "Creates a random lower triangular matrix with a standard normal distribution"),
                new("new(", "Create a rectangular matrix given a size and an optional lambda"),
                new("nrandom(", "Creates a random matrix using a standard normal distribution given a size"),
                new("random(", "Creates a random matrix given a size"),
                new("rows(", "Creates a matrix given its rows as vectors"),
            ],
            ["math"] = [
                new("abs(", "Absolute value"),
                new("acos(", "Arccosine function"),
                new("asin(", "Arcsine function"),
                new("atan(", "Arctangent function"),
                new("beta(", "The Beta function"),
                new("cbrt(", "Cubic root"),
                new("complex(", "Creates a complex number from its real and imaginary components"),
                new("cos(", "Cosine function"),
                new("erf(", "Error function"),
                new("e", "Euler's constant"),
                new("exp(", "Exponential function"),
                new("gamma(", "The Gamma function"),
                new("i", "The imaginary unit"),
                new("log(", "Natural logarithm"),
                new("log10(", "Base 10 logarithm"),
                new("max(", "Maximum function"),
                new("min(", "Minimum function"),
                new("ncdf(", "Normal cummulative function"),
                new("nrandom", "Generate a random number from a normal standard distribution"),
                new("pearl", "Try me!"),
                new("pi", "Don't be irrational: be trascendent!"),
                new("polar(", "Creates a complex number from its magnitude and phase components"),
                new("polyDerivative(", "Evaluates the derivative of a polynomial"),
                new("polyEval(", "Evaluates a polynomial"),
                new("polySolve(", "Solves a polynomial equation"),
                new("probit(", "Probit function"),
                new("random", "Generate a random number from a uniform distribution"),
                new("round(", "Rounds a real value"),
                new("sin(", "Sine function"),
                new("solve(", "Newton-Raphson solver"),
                new("sqrt(", "Squared root"),
                new("tau", "Twice π"),
                new("tan(", "Tangent function"),
                new("today", "Gets the current date"),
            ]
        }.ToFrozenDictionary();

    /// <summary>Code completion descriptors for properties and methods.</summary>
    private readonly FrozenDictionary<Type, Member[]> members =
        new Dictionary<Type, Member[]>()
        {
            [typeof(Series)] = [
                new("acf", "AutoCorrelation Function"),
                new("amax", "Gets the maximum absolute value"),
                new("amin", "Gets the minimum absolute value"),
                new("count", "Gets the number of points"),
                new("fft", "Performs a Fast Fourier Transform"),
                new("first", "Gets the first point"),
                new("fit", "Gets coefficients for a linear fit"),
                new("kurt", "Gets the kurtosis"),
                new("kurtp", "Gets the kurtosis of the population"),
                new("last", "Gets the last point"),
                new("linearfit", "Gets a line fitting the original series"),
                new("logs", "Gets the logarithmic returns"),
                new("max", "Gets the maximum value"),
                new("mean", "Gets the mean value"),
                new("min", "Gets the minimum value"),
                new("movingRet", "Gets the moving monthly/yearly return"),
                new("ncdf", "Gets the percentile according to the normal distribution"),
                new("perc", "Gets the percentiles"),
                new("random", "Generates a random series"),
                new("rets", "Gets the linear returns"),
                new("skew", "Gets the skewness"),
                new("skewp", "Gets the skewness of the population"),
                new("stats", "Gets all statistics"),
                new("std", "Gets the standard deviation"),
                new("stdp", "Gets the standard deviation of the population"),
                new("sum", "Gets the sum of all values"),
                new("type", "Gets the type of the series"),
                new("var", "Gets the variance"),
                new("varp", "Gets the variance of the population"),
                new("values", "Gets the underlying vector of values"),
                new("all(x => ", "Universal operator"),
                new("any(x => ", "Existential operator"),
                new("ar(", "Calculates the autoregression coefficients"),
                new("arModel(", "Creates an AR(p) model"),
                new("autocorr(", "Gets the autocorrelation given a lag"),
                new("corr(", "Gets the correlation with another given series"),
                new("correlogram(", "Gets all autocorrelations up to a given lag"),
                new("cov(", "Gets the covariance with another given series"),
                new("ewma(", "Calculates an Exponentially Weighted Moving Average"),
                new("filter(x => ", "Filters points by values or dates"),
                new("indexof(", "Returns the index where a value is stored"),
                new("linear(", "Gets the regression coefficients given a list of series"),
                new("linearModel(", "Creates a linear model"),
                new("map(x => ", "Pointwise transformation of the series"),
                new("movingAvg(", "Calculates a Simple Moving Average"),
                new("movingNcdf(", "Calculates a Moving Normal Percentile"),
                new("movingStd(", "Calculates a Moving Standard Deviation"),
                new("stats(", "Gets monthly statistics for a given date"),
                new("zip(", "Combines two series"),
            ],
            [typeof(Series<int>)] = [
                new("stats", "Gets all statistics"),
                new("first", "Gets the first point"),
                new("last", "Gets the last point"),
                new("values", "Gets the underlying vector of values"),
            ],
            [typeof(Series<double>)] = [
                new("stats", "Gets all statistics"),
                new("first", "Gets the first point"),
                new("last", "Gets the last point"),
                new("values", "Gets the underlying vector of values"),
            ],
            [typeof(Acc)] = [
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
            ],
            [typeof(Matrix)] = [
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
                new("isSymmetric", "Checks if a matrix is symmetric"),
                new("stats", "Calculates statistics on the cells"),
                new("map(x => ", "Pointwise transformation of matrix cells"),
                new("any(x => ", "Existential operator"),
                new("all(x => ", "Universal operator"),
            ],
            [typeof(LMatrix)] = [
                new("rows", "Gets the number of rows"),
                new("cols", "Gets the number of columns"),
                new("det", "Calculates the determinant"),
                new("trace", "Gets the sum of the main diagonal"),
                new("amax", "Gets the maximum absolute value"),
                new("amin", "Gets the minimum absolute value"),
                new("max", "Gets the maximum value"),
                new("min", "Gets the minimum absolute value"),
                new("diag", "Extracts the diagonal as a vector"),
            ],
            [typeof(RMatrix)] = [
                new("rows", "Gets the number of rows"),
                new("cols", "Gets the number of columns"),
                new("det", "Calculates the determinant"),
                new("trace", "Gets the sum of the main diagonal"),
                //new("amax", "Gets the maximum absolute value"),
                //new("amin", "Gets the minimum absolute value"),
                //new("max", "Gets the maximum value"),
                //new("min", "Gets the minimum absolute value"),
                new("diag", "Extracts the diagonal as a vector"),
            ],
            [typeof(EVD)] = [
                new("vectors", "Gets a matrix with eigenvectors as its columns"),
                new("values", "Gets all the eigenvalues"),
                new("d", "Gets a quasi-diagonal real matrix with all eigenvalues"),
                new("rank", "Gets the rank of the original matrix"),
            ],
            [typeof(LinearSModel)] = [
                new("original", "Gets the series to be explained"),
                new("prediction", "Gets the predicted series"),
                new("weights", "Gets the regression coefficients"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(LinearVModel)] = [
                new("original", "Gets the vector to be explained"),
                new("prediction", "Gets the predicted vector"),
                new("weights", "Gets the regression coefficients"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(ARSModel)] = [
                new("original", "Gets the series to be explained"),
                new("prediction", "Gets the predicted series"),
                new("coefficients", "Gets the autoregression coefficients"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(ARVModel)] = [
                new("original", "Gets the vector to be explained"),
                new("prediction", "Gets the predicted vector"),
                new("coefficients", "Gets the autoregression coefficients"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(Vector)] = [
                new("abs", "Pointwise absolute value"),
                new("acf", "AutoCorrelation Function"),
                new("amax", "Gets the maximum absolute value"),
                new("amin", "Gets the minimum absolute value"),
                new("distinct", "Gets a new vector with distinct values"),
                new("fft", "Performs a Fast Fourier Transform"),
                new("first", "Gets the first item from the vector"),
                new("last", "Gets the last item from the vector"),
                new("length", "Gets the number of items"),
                new("max", "Gets the maximum  value"),
                new("mean", "Gets the mean value"),
                new("min", "Gets the minimum value"),
                new("norm", "Gets the norm of the vector"),
                new("plot", "Plots this vector"),
                new("prod", "Gets the product of all values"),
                new("reverse", "Gets a reversed copy"),
                new("sqr", "Gets the squared norm of the vector"),
                new("sum", "Gets the sum of all values"),
                new("sqrt", "Pointwise squared root"),
                new("stats", "Gets all the statistics"),
                new("sort", "Gets a new vector with sorted values"),
                new("all(x => ", "Universal operator"),
                new("any(x => ", "Existential operator"),
                new("ar(", "Calculates the autoregression coefficients"),
                new("arModel(", "Creates an AR(p) model"),
                new("autocorr(", "Gets the autocorrelation given a lag"),
                new("correlogram(", "Gets all autocorrelations up to a given lag"),
                new("filter(x => ", "Filters items by value"),
                new("indexof(", "Returns the index where a value is stored"),
                new("linear(", "Gets the regression coefficients given a list of vectors"),
                new("linearModel(", "Creates a linear model"),
                new("map(x => ", "Pointwise transformation of vector items"),
                new("reduce(", "Reduces a vector to a single value"),
                new("zip(", "Combines two vectors"),
            ],
            [typeof(ComplexVector)] = [
                new("amax", "Gets the maximum absolute value"),
                new("distinct", "Gets a new vector with distinct values"),
                new("fft", "Performs a Fast Fourier Transform"),
                new("first", "Gets the first item from the vector"),
                new("imag", "Gets the imaginary components as a vector"),
                new("last", "Gets the last item from the vector"),
                new("length", "Gets the number of items"),
                new("magnitudes", "Gets magnitudes as a vector"),
                new("mean", "Gets the mean value"),
                new("norm", "Gets the norm of the vector"),
                new("phases", "Gets phases as a vector"),
                new("plot", "Plots this complex vector"),
                new("real", "Gets the real components as a vector"),
                new("reverse", "Gets a reversed copy"),
                new("sqr", "Gets the squared norm of the vector"),
                new("sum", "Gets the sum of all values"),
                new("all(x => ", "Universal operator"),
                new("any(x => ", "Existential operator"),
                new("filter(x => ", "Filters items by value"),
                new("indexof(", "Returns the index where a value is stored"),
                new("map(x => ", "Pointwise transformation of complex values"),
                new("mapreal(x => ", "Transforms complex vector into a real one"),
                new("reduce(", "Reduces a complex vector to a single value"),
                new("zip(", "Combines two complex vectors"),
            ],
            [typeof(MvoModel)] = [
                new("first", "Gets the first corner portfolio"),
                new("last", "Gets the last corner portfolio"),
                new("length", "Gets the number of corner portfolios"),
                new("size", "Gets the number of assets in the model"),
            ],
            [typeof(Portfolio)] = [
                new("lambda", "Gets the lambda of a corner portfolio"),
                new("ret", "Gets the expected return of the portfolio"),
                new("std", "Gets the standard deviation of the portfolio"),
                new("var", "Gets the variance of the portfolio"),
                new("weights", "Gets weights of the portfolio"),
            ],
            [typeof(Point<Date>)] = [
                new("date", "Gets the date argument"),
                new("value", "Gets the numerical value of the point"),
            ],
            [typeof(Date)] = [
                new("day", "Gets the day of the date"),
                new("month", "Gets the month of the date"),
                new("year", "Gets the year of the date"),
                new("dow", "Gets the day of week the date"),
                new("isleap", "Checks if the date belong to a leap year"),
                new("addMonths(", "Adds a number of months to the date"),
                new("addYears(", "Adds a number of years to the date"),
            ],
            [typeof(Complex)] = [
                new("imag", "Gets the imaginary part of the complex number"),
                new("mag", "Gets the magnitude of the complex number"),
                new("phase", "Gets the phase of the complex number"),
                new("real", "Gets the real part of the complex number"),
            ],
            [typeof(FftCModel)] = [
                new("amplitudes", "Gets the amplitudes of the FFT"),
                new("inverse", "Gets the inverse of the transform as a complex vector"),
                new("length", "Gets the length of the FFT"),
                new("phases", "Gets the phases of the FFT"),
                new("values", "Gets the full spectrum as a complex vector"),
            ],
            [typeof(FftRModel)] = [
                new("amplitudes", "Gets the amplitudes of the FFT"),
                new("inverse", "Gets the inverse of the transform as a real vector"),
                new("length", "Gets the length of the FFT"),
                new("phases", "Gets the phases of the FFT"),
                new("values", "Gets the full spectrum as a complex vector"),
            ],
            [typeof(Polynomial)] = [
                new("derivative", "Gets the derivative at a point between 0 and 1"),
                new("eval", "Evaluates the polynomial at a point between 0 and 1"),
            ],
            [typeof(DoubleSequence)] = [
                new("all(x => ", "Universal operator"),
                new("any(x => ", "Existential operator"),
                new("arModel(", "Creates an AR(p) model"),
                new("distinct", "Get the unique values in the sequence"),
                new("fft", "Performs a Fast Fourier Transform"),
                new("filter(x => ", "Filters the sequence according to a predicate"),
                new("first", "Gets the first value in the sequence"),
                new("last", "Gets the last value in the sequence"),
                new("length", "Gets the number of values in the sequence"),
                new("map(x => ", "Transforms the sequence according to a mapping function"),
                new("max", "Gets the maximum value from the sequence"),
                new("min", "Gets the minimum value from the sequence"),
                new("plot", "Plots this sequence"),
                new("prod", "Gets the product of all values in the sequence"),
                new("reduce(", "Combines all values in the sequence into a single value"),
                new("sort", "Sorts the sequence in ascending order"),
                new("sortDesc", "Sorts the sequence in descending order"),
                new("stats", "Gets the common statistics of the sequence"),
                new("sum", "Gets the sum of all values in the sequence"),
                new("toVector", "Converts the sequence to a vector"),
                new("zip(", "Combines two sequence using a lambda function"),
            ],
        }.ToFrozenDictionary();

    /// <summary>Allowed properties and their implementations.</summary>
    private static readonly FrozenDictionary<TypeId, MethodInfo> allProps =
        new Dictionary<TypeId, MethodInfo>()
        {
            [new(typeof(Complex), "real")] = typeof(Complex).Prop(nameof(Complex.Real)),
            [new(typeof(Complex), "re")] = typeof(Complex).Prop(nameof(Complex.Real)),
            [new(typeof(Complex), "imaginary")] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
            [new(typeof(Complex), "imag")] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
            [new(typeof(Complex), "im")] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
            [new(typeof(Complex), "magnitude")] = typeof(Complex).Prop(nameof(Complex.Magnitude)),
            [new(typeof(Complex), "mag")] = typeof(Complex).Prop(nameof(Complex.Magnitude)),
            [new(typeof(Complex), "phase")] = typeof(Complex).Prop(nameof(Complex.Phase)),
            [new(typeof(FftRModel), "amplitudes")] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
            [new(typeof(FftRModel), "magnitudes")] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
            [new(typeof(FftRModel), "phases")] = typeof(FftModel).Prop(nameof(FftModel.Phases)),
            [new(typeof(FftRModel), "length")] = typeof(FftModel).Prop(nameof(FftModel.Length)),
            [new(typeof(FftRModel), "values")] = typeof(FftModel).Prop(nameof(FftModel.Spectrum)),
            [new(typeof(FftRModel), "inverse")] = typeof(FftRModel).Get(nameof(FftRModel.Inverse)),
            [new(typeof(FftCModel), "amplitudes")] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
            [new(typeof(FftCModel), "magnitudes")] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
            [new(typeof(FftCModel), "phases")] = typeof(FftModel).Prop(nameof(FftModel.Phases)),
            [new(typeof(FftCModel), "length")] = typeof(FftModel).Prop(nameof(FftModel.Length)),
            [new(typeof(FftCModel), "values")] = typeof(FftModel).Prop(nameof(FftModel.Spectrum)),
            [new(typeof(FftCModel), "inverse")] = typeof(FftCModel).Get(nameof(FftCModel.Inverse)),

            [new(typeof(Series), "count")] = typeof(Series).Prop(nameof(Series.Count)),
            [new(typeof(Series), "length")] = typeof(Series).Prop(nameof(Series.Count)),
            [new(typeof(Series), "min")] = typeof(Series).Prop(nameof(Series.Minimum)),
            [new(typeof(Series), "max")] = typeof(Series).Prop(nameof(Series.Maximum)),
            [new(typeof(Series), "mean")] = typeof(Series).Prop(nameof(Series.Mean)),
            [new(typeof(Series), "var")] = typeof(Series).Prop(nameof(Series.Variance)),
            [new(typeof(Series), "varp")] = typeof(Series).Prop(nameof(Series.PopulationVariance)),
            [new(typeof(Series), "std")] = typeof(Series).Prop(nameof(Series.StandardDeviation)),
            [new(typeof(Series), "stdp")] = typeof(Series).Prop(nameof(Series.PopulationStandardDeviation)),
            [new(typeof(Series), "skew")] = typeof(Series).Prop(nameof(Series.Skewness)),
            [new(typeof(Series), "skewp")] = typeof(Series).Prop(nameof(Series.PopulationSkewness)),
            [new(typeof(Series), "kurt")] = typeof(Series).Prop(nameof(Series.Kurtosis)),
            [new(typeof(Series), "kurtp")] = typeof(Series).Prop(nameof(Series.PopulationKurtosis)),
            [new(typeof(Series), "stats")] = typeof(Series).Prop(nameof(Series.Stats)),
            [new(typeof(Series), "first")] = typeof(Series).Prop(nameof(Series.First)),
            [new(typeof(Series), "last")] = typeof(Series).Prop(nameof(Series.Last)),
            [new(typeof(Series), "rets")] = typeof(Series).Get(nameof(Series.AsReturns)),
            [new(typeof(Series), "logs")] = typeof(Series).Get(nameof(Series.AsLogReturns)),
            [new(typeof(Series), "fft")] = typeof(Series).Get(nameof(Series.Fft)),
            [new(typeof(Series), "perc")] = typeof(Series).Get(nameof(Series.Percentiles)),
            [new(typeof(Series), "values")] = typeof(Series).Get(nameof(Series.GetValues)),
            [new(typeof(Series), "random")] = typeof(Series).Get(nameof(Series.Random)),
            [new(typeof(Series), "movingret")] = typeof(Series).Get(nameof(Series.MovingRet)),
            [new(typeof(Series), "sum")] = typeof(Series).Get(nameof(Series.Sum)),
            [new(typeof(Series), "type")] = typeof(Series).Prop(nameof(Series.Type)),
            [new(typeof(Series), "amax")] = typeof(Series).Get(nameof(Series.AbsMax)),
            [new(typeof(Series), "amin")] = typeof(Series).Get(nameof(Series.AbsMin)),
            [new(typeof(Series), "ncdf")] = typeof(Series).GetMethod(nameof(Series.NCdf), Type.EmptyTypes)!,
            [new(typeof(Series), "fit")] = typeof(Series).Get(nameof(Series.Fit)),
            [new(typeof(Series), "linearfit")] = typeof(Series).Get(nameof(Series.LinearFit)),
            [new(typeof(Series), "acf")] = typeof(Series).Get(nameof(Series.ACF)),

            [new(typeof(Vector), "abs")] = typeof(Vector).Get(nameof(Vector.Abs)),
            [new(typeof(Vector), "acf")] = typeof(Vector).Get(nameof(Vector.ACF)),
            [new(typeof(Vector), "amax")] = typeof(Vector).Get(nameof(Vector.AMax)),
            [new(typeof(Vector), "amin")] = typeof(Vector).Get(nameof(Vector.AMin)),
            [new(typeof(Vector), "distinct")] = typeof(Vector).Get(nameof(Vector.Distinct)),
            [new(typeof(Vector), "fft")] = typeof(Vector).Get(nameof(Vector.Fft)),
            [new(typeof(Vector), "first")] = typeof(Vector).Prop(nameof(Vector.First)),
            [new(typeof(Vector), "last")] = typeof(Vector).Prop(nameof(Vector.Last)),
            [new(typeof(Vector), "length")] = typeof(Vector).Prop(nameof(Vector.Length)),
            [new(typeof(Vector), "max")] = typeof(Vector).Get(nameof(Vector.Maximum)),
            [new(typeof(Vector), "mean")] = typeof(Vector).Get(nameof(Vector.Mean)),
            [new(typeof(Vector), "min")] = typeof(Vector).Get(nameof(Vector.Minimum)),
            [new(typeof(Vector), "norm")] = typeof(Vector).Get(nameof(Vector.Norm)),
            [new(typeof(Vector), "plot")] = typeof(Vector).Get(nameof(Vector.Plot)),
            [new(typeof(Vector), "prod")] = typeof(Vector).Get(nameof(Vector.Product)),
            [new(typeof(Vector), "product")] = typeof(Vector).Get(nameof(Vector.Product)),
            [new(typeof(Vector), "reverse")] = typeof(Vector).Get(nameof(Vector.Reverse)),
            [new(typeof(Vector), "sort")] = typeof(Vector).Get(nameof(Vector.Sort)),
            [new(typeof(Vector), "sqr")] = typeof(Vector).Get(nameof(Vector.Squared)),
            [new(typeof(Vector), "sqrt")] = typeof(Vector).Get(nameof(Vector.Sqrt)),
            [new(typeof(Vector), "stats")] = typeof(Vector).Get(nameof(Vector.Stats)),
            [new(typeof(Vector), "sum")] = typeof(Vector).Get(nameof(Vector.Sum)),

            [new(typeof(Acc), "count")] = typeof(Acc).Prop(nameof(Acc.Count)),
            [new(typeof(Acc), "min")] = typeof(Acc).Prop(nameof(Acc.Minimum)),
            [new(typeof(Acc), "max")] = typeof(Acc).Prop(nameof(Acc.Maximum)),
            [new(typeof(Acc), "mean")] = typeof(Acc).Prop(nameof(Acc.Mean)),
            [new(typeof(Acc), "var")] = typeof(Acc).Prop(nameof(Acc.Variance)),
            [new(typeof(Acc), "varp")] = typeof(Acc).Prop(nameof(Acc.PopulationVariance)),
            [new(typeof(Acc), "std")] = typeof(Acc).Prop(nameof(Acc.StandardDeviation)),
            [new(typeof(Acc), "stdp")] = typeof(Acc).Prop(nameof(Acc.PopulationStandardDeviation)),
            [new(typeof(Acc), "skew")] = typeof(Acc).Prop(nameof(Acc.Skewness)),
            [new(typeof(Acc), "skewp")] = typeof(Acc).Prop(nameof(Acc.PopulationSkewness)),
            [new(typeof(Acc), "kurt")] = typeof(Acc).Prop(nameof(Acc.Kurtosis)),
            [new(typeof(Acc), "kurtp")] = typeof(Acc).Prop(nameof(Acc.PopulationKurtosis)),
            [new(typeof(Matrix), "det")] = typeof(Matrix).Get(nameof(Matrix.Determinant)),
            [new(typeof(Matrix), "chol")] = typeof(Matrix).Get(nameof(Matrix.CholeskyMatrix)),
            [new(typeof(Matrix), "evd")] = typeof(Matrix).GetMethod(nameof(Matrix.EVD), Type.EmptyTypes)!,
            [new(typeof(Matrix), "trace")] = typeof(Matrix).Get(nameof(Matrix.Trace)),
            [new(typeof(Matrix), "rows")] = typeof(Matrix).Prop(nameof(Matrix.Rows)),
            [new(typeof(Matrix), "cols")] = typeof(Matrix).Prop(nameof(Matrix.Cols)),
            [new(typeof(Matrix), "amax")] = typeof(Matrix).Get(nameof(Matrix.AMax)),
            [new(typeof(Matrix), "amin")] = typeof(Matrix).Get(nameof(Matrix.AMin)),
            [new(typeof(Matrix), "max")] = typeof(Matrix).Get(nameof(Matrix.Maximum)),
            [new(typeof(Matrix), "min")] = typeof(Matrix).Get(nameof(Matrix.Minimum)),
            [new(typeof(Matrix), "diag")] = typeof(Matrix).Get(nameof(Matrix.Diagonal)),
            [new(typeof(Matrix), "inverse")] = typeof(Matrix).Get(nameof(Matrix.Inverse)),
            [new(typeof(Matrix), "issym")] = typeof(Matrix).Get(nameof(Matrix.IsSymmetric)),
            [new(typeof(Matrix), "sym")] = typeof(Matrix).Get(nameof(Matrix.IsSymmetric)),
            [new(typeof(Matrix), "issymmetric")] = typeof(Matrix).Get(nameof(Matrix.IsSymmetric)),
            [new(typeof(Matrix), "stats")] = typeof(Matrix).Get(nameof(Matrix.Stats)),

            [new(typeof(ComplexVector), "amax")] = typeof(ComplexVector).Get(nameof(ComplexVector.AbsMax)),
            [new(typeof(ComplexVector), "amplitudes")] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
            [new(typeof(ComplexVector), "distinct")] = typeof(ComplexVector).Get(nameof(ComplexVector.Distinct)),
            [new(typeof(ComplexVector), "fft")] = typeof(ComplexVector).Get(nameof(ComplexVector.Fft)),
            [new(typeof(ComplexVector), "first")] = typeof(ComplexVector).Prop(nameof(ComplexVector.First)),
            [new(typeof(ComplexVector), "im")] = typeof(ComplexVector).Prop(nameof(ComplexVector.Imaginary)),
            [new(typeof(ComplexVector), "imag")] = typeof(ComplexVector).Prop(nameof(ComplexVector.Imaginary)),
            [new(typeof(ComplexVector), "imaginary")] = typeof(ComplexVector).Prop(nameof(ComplexVector.Imaginary)),
            [new(typeof(ComplexVector), "last")] = typeof(ComplexVector).Prop(nameof(ComplexVector.Last)),
            [new(typeof(ComplexVector), "length")] = typeof(ComplexVector).Prop(nameof(ComplexVector.Length)),
            [new(typeof(ComplexVector), "mag")] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
            [new(typeof(ComplexVector), "mags")] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
            [new(typeof(ComplexVector), "magnitudes")] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
            [new(typeof(ComplexVector), "mean")] = typeof(ComplexVector).Get(nameof(ComplexVector.Mean)),
            [new(typeof(ComplexVector), "norm")] = typeof(ComplexVector).Get(nameof(ComplexVector.Norm)),
            [new(typeof(ComplexVector), "phases")] = typeof(ComplexVector).Get(nameof(ComplexVector.Phases)),
            [new(typeof(ComplexVector), "plot")] = typeof(ComplexVector).Get(nameof(ComplexVector.Plot)),
            [new(typeof(ComplexVector), "re")] = typeof(ComplexVector).Prop(nameof(ComplexVector.Real)),
            [new(typeof(ComplexVector), "real")] = typeof(ComplexVector).Prop(nameof(ComplexVector.Real)),
            [new(typeof(ComplexVector), "reverse")] = typeof(ComplexVector).Get(nameof(ComplexVector.Reverse)),
            [new(typeof(ComplexVector), "sqr")] = typeof(ComplexVector).Get(nameof(ComplexVector.Squared)),
            [new(typeof(ComplexVector), "sum")] = typeof(ComplexVector).Get(nameof(ComplexVector.Sum)),

            [new(typeof(DoubleSequence), "acf")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.ACF)),
            [new(typeof(DoubleSequence), "distinct")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Distinct)),
            [new(typeof(DoubleSequence), "first")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.First)),
            [new(typeof(DoubleSequence), "fft")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Fft)),
            [new(typeof(DoubleSequence), "last")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Last)),
            [new(typeof(DoubleSequence), "length")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Length)),
            [new(typeof(DoubleSequence), "min")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Min)),
            [new(typeof(DoubleSequence), "max")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Max)),
            [new(typeof(DoubleSequence), "plot")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Plot)),
            [new(typeof(DoubleSequence), "prod")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Product)),
            [new(typeof(DoubleSequence), "product")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Product)),
            [new(typeof(DoubleSequence), "sort")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Sort)),
            [new(typeof(DoubleSequence), "sortasc")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Sort)),
            [new(typeof(DoubleSequence), "sortdesc")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.SortDescending)),
            [new(typeof(DoubleSequence), "stats")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Stats)),
            [new(typeof(DoubleSequence), "sum")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Sum)),
            [new(typeof(DoubleSequence), "tovector")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.ToVector)),

            [new(typeof(LMatrix), "det")] = typeof(LMatrix).Get(nameof(LMatrix.Determinant)),
            [new(typeof(LMatrix), "trace")] = typeof(LMatrix).Get(nameof(LMatrix.Trace)),
            [new(typeof(LMatrix), "rows")] = typeof(LMatrix).Prop(nameof(LMatrix.Rows)),
            [new(typeof(LMatrix), "cols")] = typeof(LMatrix).Prop(nameof(LMatrix.Cols)),
            [new(typeof(LMatrix), "amax")] = typeof(LMatrix).Get(nameof(LMatrix.AMax)),
            [new(typeof(LMatrix), "amin")] = typeof(LMatrix).Get(nameof(LMatrix.AMin)),
            [new(typeof(LMatrix), "max")] = typeof(LMatrix).Get(nameof(LMatrix.Maximum)),
            [new(typeof(LMatrix), "min")] = typeof(LMatrix).Get(nameof(LMatrix.Minimum)),
            [new(typeof(LMatrix), "diag")] = typeof(LMatrix).Get(nameof(LMatrix.Diagonal)),
            [new(typeof(RMatrix), "det")] = typeof(RMatrix).Get(nameof(RMatrix.Determinant)),
            [new(typeof(RMatrix), "trace")] = typeof(RMatrix).Get(nameof(RMatrix.Trace)),
            [new(typeof(RMatrix), "rows")] = typeof(RMatrix).Prop(nameof(RMatrix.Rows)),
            [new(typeof(RMatrix), "cols")] = typeof(RMatrix).Prop(nameof(RMatrix.Cols)),
            //[new(typeof(RMatrix),"amax")] = typeof(RMatrix).Get(nameof(RMatrix.AMax)),
            //[new(typeof(RMatrix),"amin")] = typeof(RMatrix).Get(nameof(RMatrix.AMin)),
            //[new(typeof(RMatrix),"max")] = typeof(RMatrix).Get(nameof(RMatrix.Maximum)),
            //[new(typeof(RMatrix),"min")] = typeof(RMatrix).Get(nameof(RMatrix.Minimum)),
            [new(typeof(RMatrix), "diag")] = typeof(RMatrix).Get(nameof(RMatrix.Diagonal)),
            [new(typeof(EVD), "vectors")] = typeof(EVD).Prop(nameof(EVD.Vectors)),
            [new(typeof(EVD), "values")] = typeof(EVD).Prop(nameof(EVD.Values)),
            [new(typeof(EVD), "d")] = typeof(EVD).Prop(nameof(EVD.D)),
            [new(typeof(EVD), "rank")] = typeof(EVD).Get(nameof(EVD.Rank)),
            [new(typeof(EVD), "det")] = typeof(EVD).Get(nameof(EVD.Determinant)),
            [new(typeof(LinearSModel), "original")] = typeof(LinearSModel).Prop(nameof(LinearSModel.Original)),
            [new(typeof(LinearSModel), "prediction")] = typeof(LinearSModel).Prop(nameof(LinearSModel.Prediction)),
            [new(typeof(LinearSModel), "weights")] = typeof(LinearSModel).Prop(nameof(LinearSModel.Weights)),
            [new(typeof(LinearSModel), "r2")] = typeof(LinearSModel).Prop(nameof(LinearSModel.R2)),
            [new(typeof(LinearSModel), "rss")] = typeof(LinearSModel).Prop(nameof(LinearSModel.ResidualSumSquares)),
            [new(typeof(LinearSModel), "tss")] = typeof(LinearSModel).Prop(nameof(LinearSModel.TotalSumSquares)),
            [new(typeof(LinearVModel), "original")] = typeof(LinearVModel).Prop(nameof(LinearVModel.Original)),
            [new(typeof(LinearVModel), "prediction")] = typeof(LinearVModel).Prop(nameof(LinearVModel.Prediction)),
            [new(typeof(LinearVModel), "weights")] = typeof(LinearVModel).Prop(nameof(LinearVModel.Weights)),
            [new(typeof(LinearVModel), "r2")] = typeof(LinearVModel).Prop(nameof(LinearVModel.R2)),
            [new(typeof(LinearVModel), "rss")] = typeof(LinearVModel).Prop(nameof(LinearVModel.ResidualSumSquares)),
            [new(typeof(LinearVModel), "tss")] = typeof(LinearVModel).Prop(nameof(LinearVModel.TotalSumSquares)),
            [new(typeof(Series<int>), "stats")] = typeof(Series<int>).Prop(nameof(Series<int>.Stats)),
            [new(typeof(Series<int>), "first")] = typeof(Series<int>).Prop(nameof(Series<int>.First)),
            [new(typeof(Series<int>), "last")] = typeof(Series<int>).Prop(nameof(Series<int>.Last)),
            [new(typeof(Series<int>), "values")] = typeof(Series<int>).Get(nameof(Series<int>.GetValues)),
            [new(typeof(Series<int>), "sum")] = typeof(Series<int>).Get(nameof(Series<int>.Sum)),
            [new(typeof(Series<double>), "stats")] = typeof(Series<double>).Prop(nameof(Series<double>.Stats)),
            [new(typeof(Series<double>), "first")] = typeof(Series<double>).Prop(nameof(Series<double>.First)),
            [new(typeof(Series<double>), "last")] = typeof(Series<double>).Prop(nameof(Series<double>.Last)),
            [new(typeof(Series<double>), "values")] = typeof(Series<double>).Get(nameof(Series<double>.GetValues)),
            [new(typeof(Series<double>), "sum")] = typeof(Series<double>).Get(nameof(Series<double>.Sum)),
            [new(typeof(MvoModel), "length")] = typeof(MvoModel).Prop(nameof(MvoModel.Length)),
            [new(typeof(MvoModel), "first")] = typeof(MvoModel).Prop(nameof(MvoModel.First)),
            [new(typeof(MvoModel), "last")] = typeof(MvoModel).Prop(nameof(MvoModel.Last)),
            [new(typeof(MvoModel), "size")] = typeof(MvoModel).Prop(nameof(MvoModel.Size)),
            [new(typeof(Portfolio), "weights")] = typeof(Portfolio).Prop(nameof(Portfolio.Weights)),
            [new(typeof(Portfolio), "lambda")] = typeof(Portfolio).Prop(nameof(Portfolio.Lambda)),
            [new(typeof(Portfolio), "ret")] = typeof(Portfolio).Prop(nameof(Portfolio.Mean)),
            [new(typeof(Portfolio), "var")] = typeof(Portfolio).Prop(nameof(Portfolio.Variance)),
            [new(typeof(Portfolio), "std")] = typeof(Portfolio).Prop(nameof(Portfolio.StdDev)),
            [new(typeof(ARSModel), "original")] = typeof(ARSModel).Prop(nameof(ARSModel.Original)),
            [new(typeof(ARSModel), "prediction")] = typeof(ARSModel).Prop(nameof(ARSModel.Prediction)),
            [new(typeof(ARSModel), "coefficients")] = typeof(ARSModel).Prop(nameof(ARSModel.Coefficients)),
            [new(typeof(ARSModel), "coeff")] = typeof(ARSModel).Prop(nameof(ARSModel.Coefficients)),
            [new(typeof(ARSModel), "r2")] = typeof(ARSModel).Prop(nameof(ARSModel.R2)),
            [new(typeof(ARSModel), "rss")] = typeof(ARSModel).Prop(nameof(ARSModel.ResidualSumSquares)),
            [new(typeof(ARSModel), "tss")] = typeof(ARSModel).Prop(nameof(ARSModel.TotalSumSquares)),
            [new(typeof(ARVModel), "original")] = typeof(ARVModel).Prop(nameof(ARVModel.Original)),
            [new(typeof(ARVModel), "prediction")] = typeof(ARVModel).Prop(nameof(ARVModel.Prediction)),
            [new(typeof(ARVModel), "coefficients")] = typeof(ARVModel).Prop(nameof(ARVModel.Coefficients)),
            [new(typeof(ARVModel), "coeff")] = typeof(ARVModel).Prop(nameof(ARVModel.Coefficients)),
            [new(typeof(ARVModel), "r2")] = typeof(ARVModel).Prop(nameof(ARVModel.R2)),
            [new(typeof(ARVModel), "rss")] = typeof(ARVModel).Prop(nameof(ARVModel.ResidualSumSquares)),
            [new(typeof(ARVModel), "tss")] = typeof(ARVModel).Prop(nameof(ARVModel.TotalSumSquares)),
            [new(typeof(Point<Date>), "value")] = typeof(Point<Date>).Prop(nameof(Point<Date>.Value)),
            [new(typeof(Point<Date>), "date")] = typeof(Point<Date>).Prop(nameof(Point<Date>.Arg)),
            [new(typeof(Date), "day")] = typeof(Date).Prop(nameof(Date.Day)),
            [new(typeof(Date), "month")] = typeof(Date).Prop(nameof(Date.Month)),
            [new(typeof(Date), "year")] = typeof(Date).Prop(nameof(Date.Year)),
            [new(typeof(Date), "dow")] = typeof(Date).Prop(nameof(Date.DayOfWeek)),
            [new(typeof(Date), "isleap")] = typeof(Date).Get(nameof(Date.IsLeap)),
            [new(typeof(DateSpline), "length")] = typeof(DateSpline).Prop(nameof(DateSpline.Length)),
            [new(typeof(VectorSpline), "length")] = typeof(DateSpline).Prop(nameof(DateSpline.Length)),
        }.ToFrozenDictionary();

    /// <summary>Allowed instance methods.</summary>
    private static readonly FrozenDictionary<TypeId, MethodInfo> methods =
        new Dictionary<TypeId, MethodInfo>()
        {
            [new(typeof(Series), "autocorr")] = typeof(Series).Get(nameof(Series.AutoCorrelation)),
            [new(typeof(Series), "corr")] = typeof(Series).Get(nameof(Series.Correlation)),
            [new(typeof(Series), "correlogram")] = typeof(Series).Get(nameof(Series.Correlogram)),
            [new(typeof(Series), "cov")] = typeof(Series).Get(nameof(Series.Covariance)),
            [new(typeof(Series), "stats")] = typeof(Series).GetMethod(nameof(Series.GetSliceStats), [typeof(Date)])!,
            [new(typeof(Series), "ncdf")] = typeof(Series).GetMethod(nameof(Series.NCdf), DoubleArg)!,
            [new(typeof(Series), "movingavg")] = typeof(Series).Get(nameof(Series.MovingAvg)),
            [new(typeof(Series), "movingstd")] = typeof(Series).GetMethod(nameof(Series.MovingStd), IntArg)!,
            [new(typeof(Series), "movingncdf")] = typeof(Series).Get(nameof(Series.MovingNcdf)),
            [new(typeof(Series), "ewma")] = typeof(Series).Get(nameof(Series.EWMA)),
            [new(typeof(Series), "map")] = typeof(Series).Get(nameof(Series.Map)),
            [new(typeof(Series), "filter")] = typeof(Series).Get(nameof(Series.Filter)),
            [new(typeof(Series), "any")] = typeof(Series).Get(nameof(Series.Any)),
            [new(typeof(Series), "all")] = typeof(Series).Get(nameof(Series.All)),
            [new(typeof(Series), "zip")] = typeof(Series).Get(nameof(Series.Zip)),
            [new(typeof(Series), "indexof")] = typeof(Series).GetMethod(nameof(Series.IndexOf), DoubleArg)!,
            [new(typeof(Series), "linear")] = typeof(Series).Get(nameof(Series.LinearModel)),
            [new(typeof(Series), "linearmodel")] = typeof(Series).Get(nameof(Series.FullLinearModel)),
            [new(typeof(Series), "ar")] = typeof(Series).Get(nameof(Series.AutoRegression)),
            [new(typeof(Series), "armodel")] = typeof(Series).Get(nameof(Series.ARModel)),
            [new(typeof(DateSpline), "poly")] = typeof(DateSpline).Get(nameof(DateSpline.GetPoly)),
            [new(typeof(DateSpline), "derivative")] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),
            [new(typeof(DateSpline), "deriv")] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),
            [new(typeof(DateSpline), "der")] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),
            [new(typeof(VectorSpline), "poly")] = typeof(VectorSpline).Get(nameof(VectorSpline.GetPoly)),
            [new(typeof(VectorSpline), "derivative")] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),
            [new(typeof(VectorSpline), "deriv")] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),
            [new(typeof(VectorSpline), "der")] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),

            [new(typeof(Vector), "all")] = typeof(Vector).Get(nameof(Vector.All)),
            [new(typeof(Vector), "any")] = typeof(Vector).Get(nameof(Vector.Any)),
            [new(typeof(Vector), "ar")] = typeof(Vector).Get(nameof(Vector.AutoRegression)),
            [new(typeof(Vector), "armodel")] = typeof(Vector).Get(nameof(Vector.ARModel)),
            [new(typeof(Vector), "autocorr")] = typeof(Vector).Get(nameof(Vector.AutoCorrelation)),
            [new(typeof(Vector), "correlogram")] = typeof(Vector).Get(nameof(Vector.Correlogram)),
            [new(typeof(Vector), "filter")] = typeof(Vector).Get(nameof(Vector.Filter)),
            [new(typeof(Vector), "indexof")] = typeof(Vector).GetMethod(nameof(Vector.IndexOf), DoubleArg)!,
            [new(typeof(Vector), "linear")] = typeof(Vector).Get(nameof(Vector.LinearModel)),
            [new(typeof(Vector), "linearmodel")] = typeof(Vector).Get(nameof(Vector.FullLinearModel)),
            [new(typeof(Vector), "map")] = typeof(Vector).Get(nameof(Vector.Map)),
            [new(typeof(Vector), "reduce")] = typeof(Vector).Get(nameof(Vector.Reduce)),
            [new(typeof(Vector), "zip")] = typeof(Vector).Get(nameof(Vector.Zip)),

            [new(typeof(ComplexVector), "all")] = typeof(ComplexVector).Get(nameof(ComplexVector.All)),
            [new(typeof(ComplexVector), "any")] = typeof(ComplexVector).Get(nameof(ComplexVector.Any)),
            [new(typeof(ComplexVector), "filter")] = typeof(ComplexVector).Get(nameof(ComplexVector.Filter)),
            [new(typeof(ComplexVector), "indexof")] = typeof(ComplexVector).GetMethod(nameof(ComplexVector.IndexOf),
                    ComplexArg)!,
            [new(typeof(ComplexVector), "map")] = typeof(ComplexVector).Get(nameof(ComplexVector.Map)),
            [new(typeof(ComplexVector), "mapreal")] = typeof(ComplexVector).Get(nameof(ComplexVector.MapReal)),
            [new(typeof(ComplexVector), "mapr")] = typeof(ComplexVector).Get(nameof(ComplexVector.MapReal)),
            [new(typeof(ComplexVector), "reduce")] = typeof(ComplexVector).Get(nameof(ComplexVector.Reduce)),
            [new(typeof(ComplexVector), "zip")] = typeof(ComplexVector).Get(nameof(ComplexVector.Zip)),

            [new(typeof(Date), "addmonths")] = typeof(Date).GetMethod(nameof(Date.AddMonths), IntArg)!,
            [new(typeof(Date), "addyears")] = typeof(Date).Get(nameof(Date.AddYears)),
            [new(typeof(Matrix), "getcol")] = typeof(Matrix).GetMethod(nameof(Matrix.GetColumn), IntArg)!,
            [new(typeof(Matrix), "getrow")] = typeof(Matrix).GetMethod(nameof(Matrix.GetRow), IntArg)!,
            [new(typeof(Matrix), "map")] = typeof(Matrix).Get(nameof(Matrix.Map)),
            [new(typeof(Matrix), "any")] = typeof(Matrix).Get(nameof(Matrix.Any)),
            [new(typeof(Matrix), "all")] = typeof(Matrix).Get(nameof(Matrix.All)),
            [new(typeof(Polynomial), "eval")] = typeof(Polynomial).Get(nameof(Polynomial.Eval)),
            [new(typeof(Polynomial), "derivative")] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),
            [new(typeof(Polynomial), "deriv")] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),
            [new(typeof(Polynomial), "der")] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),
            [new(typeof(DoubleSequence), "filter")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Filter)),
            [new(typeof(DoubleSequence), "map")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Map)),
            [new(typeof(DoubleSequence), "zip")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Zip)),
            [new(typeof(DoubleSequence), "reduce")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Reduce)),
            [new(typeof(DoubleSequence), "any")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Any)),
            [new(typeof(DoubleSequence), "all")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.All)),
            [new(typeof(DoubleSequence), "armodel")] = typeof(DoubleSequence).Get(nameof(DoubleSequence.ARModel)),
        }.ToFrozenDictionary();

    private static readonly MethodList MatrixEye = new(
        typeof(Matrix).MD(nameof(Matrix.Identity), IntArg));
    private static readonly MethodList MatrixCovariance = new(
        typeof(Series<Date>).MD(nameof(Series.CovarianceMatrix), typeof(Series[])));
    private static readonly MethodList MatrixCorrelation = new(
        typeof(Series<Date>).MD(nameof(Series.CorrelationMatrix), typeof(Series[])));
    private static readonly MethodList ModelCompare = new(
        typeof(Plot<Vector>).MD(VectorVectorArg),
        typeof(Plot<Vector>).MD(VectorArg),
        typeof(Plot<ComplexVector>).MD(typeof(ComplexVector), typeof(ComplexVector)),
        typeof(Plot<ComplexVector>).MD(typeof(ComplexVector)),
        typeof(Plot<Series>).MD(typeof(Series), typeof(Series)),
        typeof(Plot<Series>).MD(typeof(Series), typeof(Series)));
    private static readonly MethodList PolyDerivative = new(
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), DoubleVectorArg),
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), typeof(double), typeof(double[])),
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), typeof(Complex), typeof(Vector)),
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), typeof(Complex), typeof(double[])));

    /// <summary>Information for class methods.</summary>
    /// <remarks>
    /// An AUSTRA class method may be implemented either by a static method or by a constructor.
    /// </remarks>
    private readonly FrozenDictionary<string, MethodList> classMethods =
        new Dictionary<string, MethodList>()
        {
            ["series.new"] = new(
                typeof(Series).MD(nameof(Series.Combine), typeof(Vector), typeof(Series[]))),
            ["spline.new"] = new(
                typeof(DateSpline).MD(typeof(Series)),
                typeof(VectorSpline).MD(VectorVectorArg),
                typeof(VectorSpline).MD(
                    typeof(double), typeof(double), typeof(int), typeof(Func<double, double>))),
            ["vec.new"] = new(
                typeof(Vector).MD(IntArg),
                typeof(Vector).MD(nameof(Vector.Combine), typeof(Vector), typeof(Vector[])),
                typeof(Vector).MD(typeof(int), typeof(Func<int, double>)),
                typeof(Vector).MD(typeof(int), typeof(Func<int, Vector, double>))),
            ["vec.nrandom"] = new(
                typeof(Vector).MD(typeof(int), typeof(NormalRandom))),
            ["vec.random"] = new(
                typeof(Vector).MD(typeof(int), typeof(Random))),
            ["vec.ones"] = new(
                typeof(Vector).MD(typeof(int), typeof(One))),
            ["cvec.new"] = new(
                typeof(ComplexVector).MD(VectorArg),
                typeof(ComplexVector).MD(VectorVectorArg),
                typeof(ComplexVector).MD(IntArg),
                typeof(ComplexVector).MD(typeof(int), typeof(Func<int, Complex>)),
                typeof(ComplexVector).MD(typeof(int), typeof(Func<int, ComplexVector, Complex>))),
            ["cvec.nrandom"] = new(
                typeof(ComplexVector).MD(typeof(int), typeof(NormalRandom))),
            ["cvec.random"] = new(
                typeof(ComplexVector).MD(typeof(int), typeof(Random))),
            ["matrix.new"] = new(
                typeof(Matrix).MD(IntArg),
                typeof(Matrix).MD(IntIntArg),
                typeof(Matrix).MD(typeof(int), typeof(Func<int, int, double>)),
                typeof(Matrix).MD(typeof(int), typeof(int), typeof(Func<int, int, double>))),
            ["matrix.rows"] = new(
                typeof(Matrix).MD(typeof(Vector[]))),
            ["matrix.cols"] = new(
                typeof(Matrix).MD(nameof(Matrix.FromColumns), typeof(Vector[]))),
            ["matrix.diag"] = new(
                typeof(Matrix).MD(VectorArg),
                typeof(Matrix).MD(typeof(double[]))),
            ["matrix.i"] = MatrixEye,
            ["matrix.eye"] = MatrixEye,
            ["matrix.random"] = new(
                typeof(Matrix).MD(typeof(int), typeof(Random)),
                typeof(Matrix).MD(typeof(int), typeof(int), typeof(Random))),
            ["matrix.nrandom"] = new(
                typeof(Matrix).MD(typeof(int), typeof(NormalRandom)),
                typeof(Matrix).MD(typeof(int), typeof(int), typeof(NormalRandom))),
            ["matrix.lrandom"] = new(
                typeof(LMatrix).MD(typeof(int), typeof(Random)),
                typeof(LMatrix).MD(typeof(int), typeof(int), typeof(Random))),
            ["matrix.lnrandom"] = new(
                typeof(LMatrix).MD(typeof(int), typeof(NormalRandom)),
                typeof(LMatrix).MD(typeof(int), typeof(int), typeof(NormalRandom))),
            ["matrix.cov"] = MatrixCovariance,
            ["matrix.covariance"] = MatrixCovariance,
            ["matrix.corr"] = MatrixCorrelation,
            ["matrix.correlation"] = MatrixCorrelation,
            ["model.plot"] = ModelCompare,
            ["model.mvo"] = new(
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix)),
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix), typeof(Vector), typeof(Vector)),
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix), typeof(Series[])),
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix),
                    typeof(Vector), typeof(Vector), typeof(Series[])),
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix), typeof(string[])),
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix),
                    typeof(Vector), typeof(Vector), typeof(string[]))),
            ["math.polysolve"] = new(
                typeof(Polynomials).MD(nameof(Polynomials.PolySolve), VectorArg),
                typeof(Polynomials).MD(nameof(Polynomials.PolySolve), typeof(double[]))),
            ["math.polyeval"] = new(
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), DoubleVectorArg),
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), typeof(double), typeof(double[])),
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), typeof(Complex), typeof(Vector)),
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), typeof(Complex), typeof(double[]))),
            ["math.polyderivative"] = PolyDerivative,
            ["math.polyderiv"] = PolyDerivative,
            ["math.abs"] = new(
                typeof(Math).MD(nameof(Math.Abs), IntArg),
                typeof(Math).MD(nameof(Math.Abs), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Abs), ComplexArg)),
            ["math.acos"] = new(
                typeof(Math).MD(nameof(Math.Acos), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Acos), ComplexArg)),
            ["math.asin"] = new(
                typeof(Math).MD(nameof(Math.Asin), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Asin), ComplexArg)),
            ["math.atan"] = new(
                typeof(Math).MD(nameof(Math.Atan), DoubleArg),
                typeof(Math).MD(nameof(Math.Atan2), DoubleDoubleArg),
                typeof(Complex).MD(nameof(Complex.Atan), ComplexArg)),
            ["math.beta"] = new(
                typeof(Functions).MD(nameof(Functions.Beta), DoubleDoubleArg)),
            ["math.cbrt"] = new(
                typeof(Math).MD(nameof(Math.Cbrt), DoubleArg)),
            ["math.cos"] = new(
                typeof(Math).MD(nameof(Math.Cos), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Cos), ComplexArg)),
            ["math.cosh"] = new(
                typeof(Math).MD(nameof(Math.Cosh), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Cosh), ComplexArg)),
            ["math.erf"] = new(
                typeof(Functions).MD(nameof(Functions.Erf), DoubleArg)),
            ["math.exp"] = new(
                typeof(Math).MD(nameof(Math.Exp), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Exp), ComplexArg)),
            ["math.gamma"] = new(
                typeof(Functions).MD(nameof(Functions.Gamma), DoubleArg)),
            ["math.lngamma"] = new(
                typeof(Functions).MD(nameof(Functions.GammaLn), DoubleArg)),
            ["math.log"] = new(
                typeof(Math).MD(nameof(Math.Log), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Log), ComplexArg)),
            ["math.log10"] = new(
                typeof(Math).MD(nameof(Math.Log10), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Log10), ComplexArg)),
            ["math.ncdf"] = new(
                typeof(Functions).MD(nameof(Functions.NCdf), DoubleArg)),
            ["math.probit"] = new(
                typeof(Functions).MD(nameof(Functions.Probit), DoubleArg)),
            ["math.sign"] = new(
                typeof(Math).MD(nameof(Math.Sign), IntArg),
                typeof(Math).MD(nameof(Math.Sign), DoubleArg)),
            ["math.sin"] = new(
                typeof(Math).MD(nameof(Math.Sin), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Sin), ComplexArg)),
            ["math.sinh"] = new(
                typeof(Math).MD(nameof(Math.Sinh), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Sinh), ComplexArg)),
            ["math.tan"] = new(
                typeof(Math).MD(nameof(Math.Tan), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Tan), ComplexArg)),
            ["math.tanh"] = new(
                typeof(Math).MD(nameof(Math.Tanh), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Tanh), ComplexArg)),
            ["math.sqrt"] = new(
                typeof(Math).MD(nameof(Math.Sqrt), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Sqrt), ComplexArg)),
            ["math.trunc"] = new(
                typeof(Math).MD(nameof(Math.Truncate), DoubleArg)),
            ["math.round"] = new(
                typeof(Math).MD(nameof(Math.Round), DoubleArg),
                typeof(Math).MD(nameof(Math.Round), typeof(double), typeof(int))),
            ["math.plot"] = ModelCompare,
            ["math.complex"] = new(
                typeof(Complex).MD(DoubleDoubleArg),
                typeof(Complex).MD(typeof(double), typeof(Zero))),
            ["math.polar"] = new(
                typeof(Complex).MD(nameof(Complex.FromPolarCoordinates), DoubleDoubleArg),
                typeof(Complex).MD(nameof(Complex.FromPolarCoordinates), typeof(double), typeof(Zero))),
            ["math.min"] = new(
                typeof(Date).MD(nameof(Date.Min), typeof(Date), typeof(Date)),
                typeof(Math).MD(nameof(Math.Min), IntIntArg),
               typeof(Math).MD(nameof(Math.Min), DoubleDoubleArg)),
            ["math.max"] = new(
                typeof(Date).MD(nameof(Date.Max), typeof(Date), typeof(Date)),
                typeof(Math).MD(nameof(Math.Max), IntIntArg),
                typeof(Math).MD(nameof(Math.Max), DoubleDoubleArg)),
            ["math.solve"] = new(
                typeof(Solver).MD(nameof(Solver.Solve),
                    typeof(Func<double, double>), typeof(Func<double, double>), typeof(double)),
                typeof(Solver).MD(nameof(Solver.Solve),
                    typeof(Func<double, double>), typeof(Func<double, double>), typeof(double),
                    typeof(double)),
                typeof(Solver).MD(nameof(Solver.Solve),
                    typeof(Func<double, double>), typeof(Func<double, double>), typeof(double),
                    typeof(double), typeof(int))),
            ["seq.new"] = new(
                typeof(DoubleSequence).MD(nameof(DoubleSequence.Create), IntIntArg),
                typeof(DoubleSequence).MD(nameof(DoubleSequence.Create),
                    typeof(double), typeof(double), typeof(int)),
                typeof(DoubleSequence).MD(nameof(DoubleSequence.Create), typeof(Vector)),
                typeof(DoubleSequence).MD(nameof(DoubleSequence.Create), typeof(Series))),
            ["seq.random"] = new(
                typeof(DoubleSequence).MD(nameof(DoubleSequence.Random), typeof(int))),
            ["seq.nrandom"] = new(
                typeof(DoubleSequence).MD(nameof(DoubleSequence.NormalRandom), typeof(int)),
                typeof(DoubleSequence).MD(nameof(DoubleSequence.NormalRandom),
                    typeof(int), typeof(double)),
                typeof(DoubleSequence).MD(nameof(DoubleSequence.NormalRandom),
                    typeof(int), typeof(double), typeof(Vector)),
                typeof(DoubleSequence).MD(nameof(DoubleSequence.NormalRandom),
                    typeof(int), typeof(double),  typeof(double), typeof(Vector))),
        }.ToFrozenDictionary();

    /// <summary>Get root expressions for code completion.</summary>
    /// <returns>Class names, global methods and a couple of statement prefixes.</returns>
    public Member[] GetGlobalRoots() =>
        [.. rootClasses,
        .. classMembers["math"],
        new("set", "Assigns a value to a variable"),
        new("let", "Declares local variables")];

    /// <summary>Checks if the identifier is a root class.</summary>
    /// <param name="identifier">A potential class name.</param>
    /// <returns><see langword="true"/> when the identifier is a root class name.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsClassName(string identifier) => classMembers.ContainsKey(identifier);

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="source">A data source.</param>
    /// <param name="text">An expression fragment.</param>
    /// <param name="type">The type of the expression fragment.</param>
    /// <returns>A list of pairs member name/description.</returns>
    public IList<Member> GetMembers(IDataSource source, string text, out Type? type)
    {
        ReadOnlySpan<char> trimmedText = ExtractObjectPath(text);
        if (!trimmedText.IsEmpty)
            try
            {
                return ExtractType(trimmedText.ToString());
            }
            catch
            {
                // Give it a second chance, if a let clause was not included.
                Match m = LetHeaderRegex().Match(text);
                if (m.Success && !LetHeaderRegex().IsMatch(trimmedText))
                    try
                    {
                        return ExtractType(m.Groups["header"] + trimmedText.ToString());
                    }
                    catch { }
            }
        type = null;
        return [];

        IList<Member> ExtractType(string text)
        {
            using Parser parser = new(this, source, text);
            return parser.ParseType() is var types
                && types is not null && types.Length > 0 && types[0] is not null
                && members.TryGetValue(types[0], out Member[]? list)
                ? list
                : [];
        }
    }

    /// <summary>Gets a list of class members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>A list of pairs member name/description.</returns>
    public IList<Member> GetClassMembers(
        string text)
    {
        return classMembers.TryGetValue(ExtractClassName(text),
            out Member[]? list) ? list : [];

        static string ExtractClassName(string text)
        {
            ref char c = ref Unsafe.As<Str>(text).FirstChar;
            int i = Unsafe.Add(ref c, text.Length - 1) == ':'
                && Unsafe.Add(ref c, text.Length - 2) == ':' ? text.Length - 2 : text.Length - 1;
            if (i < 0 || Unsafe.Add(ref c, i--) != ':')
                return "";
            while (i >= 0 && char.IsWhiteSpace(Unsafe.Add(ref c, i)))
                i--;
            if (i < 0)
                return "";
            int end = i + 1;
            while (i >= 0 && char.IsLetter(Unsafe.Add(ref c, i)))
                i--;
            return text[(i + 1)..end].Trim();
        }
    }

    /// <summary>Extracts an object path from an expression fragment.</summary>
    /// <param name="text">A fragment of an expression.</param>
    /// <returns>The final object path.</returns>
    private ReadOnlySpan<char> ExtractObjectPath(string text)
    {
        ref char c = ref Unsafe.As<Str>(text).FirstChar;
        int i = text.Length - 1;
        while (i >= 0)
        {
            char ch = Unsafe.Add(ref c, i);
            if (char.IsLetterOrDigit(ch) ||
                ch is '_' or '.' or ':' or '=' or '\'' ||
                char.IsWhiteSpace(ch))
                i--;
            else if (ch is '(' or '[')
                return text.AsSpan()[(i + 1)..];
            else if (ch == ')')
            {
                int count = 1;
                while (--i >= 0)
                {
                    ch = Unsafe.Add(ref c, i);
                    if (ch == ')')
                        count++;
                    else if (ch == '(')
                    {
                        if (--count == 0)
                            break;
                    }
                }
                if (count > 0)
                    return [];
                i--;
            }
            else if (ch == ']')
            {
                int count = 1;
                while (--i >= 0)
                {
                    ch = Unsafe.Add(ref c, i);
                    if (ch == ']')
                        count++;
                    else if (ch == '[')
                    {
                        if (--count == 0)
                            break;
                    }
                }
                if (count > 0)
                    return [];
                i--;
            }
            else
                break;
        }
        return text.AsSpan()[(i + 1)..].Trim();
    }

    /// <summary>Gets a property method for a given type and identifier.</summary>
    /// <param name="type">Implementing type.</param>
    /// <param name="identifier">Property name.</param>
    /// <param name="info">The method info, on success.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    public bool TryGetProperty(Type type, string identifier, [MaybeNullWhen(false)] out MethodInfo info) =>
        allProps.TryGetValue(new TypeId(type, identifier.ToLower()), out info);

    /// <summary>Gets an instance method for a given type and identifier.</summary>
    /// <param name="type">Implementing type.</param>
    /// <param name="identifier">Method name.</param>
    /// <param name="info">The method info, on success.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    public bool TryGetMethod(Type type, string identifier, [MaybeNullWhen(false)] out MethodInfo info) =>
        methods.TryGetValue(new TypeId(type, identifier.ToLower()), out info);

    /// <summary>Gets an class method given the class and method names.</summary>
    /// <param name="identifier">Prefixed method name.</param>
    /// <param name="info">The method info, on success.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    public bool TryGetClassMethod(string identifier, out MethodList info) =>
        classMethods.TryGetValue(identifier, out info);

    /// <summary>Gets a regex that matches a set statement.</summary>
    [GeneratedRegex("^\\s*(?'header'let\\s+.+\\s+in\\s+)", RegexOptions.IgnoreCase)]
    private static partial Regex LetHeaderRegex();

    /// <summary>Represents a dictionary key with a type and a string identifier.</summary>
    /// <param name="Type">The type.</param>
    /// <param name="Id">Name of a member of the type.</param>
    private readonly record struct TypeId(Type Type, string Id);
}

/// <summary>Represents a single method overload.</summary>
internal readonly struct MethodData
{
    public const uint Mλ1 = 1u, Mλ2 = 2u;

    /// <summary>Either the constructor or the static method for this overload.</summary>
    private readonly MethodBase mInfo;
    /// <summary>Bit mask for marking arguments that are lambda expressions.</summary>
    private readonly uint typeMask;
    /// <summary>Formal parameters for this method overload.</summary>
    public Type[] Args { get; }
    public int ExpectedArgs { get; }

    public MethodData(Type implementor, string? memberName, params Type[] args)
    {
        Args = args;
        for (int i = 0, m = 0; i < args.Length; i++, m += 2)
            if (args[i] is var t1 && t1.IsAssignableTo(typeof(Delegate)))
                typeMask |= (t1.GetGenericArguments().Length == 2 ? Mλ1 : Mλ2) << m;
        Type t = Args[^1];
        ExpectedArgs = t.IsArray
            ? int.MaxValue
            : t == typeof(Random) || t == typeof(NormalRandom) || t == typeof(One) || t == typeof(Zero)
            ? Args.Length - 1
            : Args.Length;
        Args[^1] = t == typeof(Zero) || t == typeof(One) ? typeof(double) : t;
        mInfo = memberName != null
            ? implementor.GetMethod(memberName, Args)!
            : implementor.GetConstructor(Args)!;
        Args[^1] = t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetMask(int typeId) => (typeMask >> (typeId * 2)) & 3u;

    /// <summary>Creates an expression that calls the method.</summary>
    /// <param name="actualArguments">Actual arguments.</param>
    /// <returns>A expression node for calling either a static method or a constructor.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Expression GetExpression(List<Expression> actualArguments) =>
        mInfo.IsConstructor
        ? Expression.New((ConstructorInfo)mInfo, actualArguments)
        : Expression.Call((MethodInfo)mInfo, actualArguments);
}

/// <summary>Represents a set of overloaded methods.</summary>
internal readonly struct MethodList
{
    public MethodData[] Methods { get; }
    public bool[] IsLambda { get; }

    public MethodList(params MethodData[] methods)
    {
        Methods = methods;
        int maxArgs = methods.Max(m => m.Args.Length);
        IsLambda = new bool[maxArgs];
        for (int i = 0; i < IsLambda.Length; i++)
            foreach (MethodData method in methods)
                if (method.Args.Length > i
                    && method.Args[i].IsAssignableTo(typeof(Delegate)))
                {
                    IsLambda[i] = true;
                    break;
                }
    }
}

/// <summary>
/// Tells the compiler to add a constant <c>0d</c> argument to a method call.
/// </summary>
internal sealed class Zero { }

/// <summary>
/// Tells the compiler to add a constant <c>1d</c> argument to a method call.
/// </summary>
internal sealed class One { }

/// <summary>Internal stub for accessing string internals.</summary>
internal sealed class Str
{
    /// <summary>The length of the string.</summary>
    public int Length = 0;
    /// <summary>The first character in the string.</summary>
    public char FirstChar;
}
