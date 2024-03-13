namespace Austra.Parser;

/// <summary>A symbol table for predefined classes and methods.</summary>
/// <remarks>
/// This class is instantiated from <see cref="AustraEngine"/> and acts
/// like a singleton for all the lifetime of the session.
/// </remarks>
internal sealed class Bindings
{
    /// <summary>The argument is a complex.</summary>
    private static readonly Type[] CArg = [typeof(Complex)];
    /// <summary>The argument is a double.</summary>
    private static readonly Type[] DArg = [typeof(double)];
    /// <summary>Two double arguments.</summary>
    private static readonly Type[] DDArg = [typeof(double), typeof(double)];
    /// <summary>A double and a vector argument.</summary>
    private static readonly Type[] DVArg = [typeof(double), typeof(DVector)];
    /// <summary>The argument is an integer.</summary>
    private static readonly Type[] NArg = [typeof(int)];
    /// <summary>An integer followed by a double argument.</summary>
    private static readonly Type[] NDArg = [typeof(int), typeof(double)];
    /// <summary>Two integer arguments.</summary>
    private static readonly Type[] NNArg = [typeof(int), typeof(int)];
    /// <summary>The argument is a vector.</summary>
    private static readonly Type[] VArg = [typeof(DVector)];
    /// <summary>Two vector arguments.</summary>
    private static readonly Type[] VVArg = [typeof(DVector), typeof(DVector)];

    private static readonly MethodList MatrixEye = new(
        typeof(Matrix).MD(nameof(Matrix.Identity), NArg));
    private static readonly MethodList MatrixCovariance = new(
        typeof(Series<Date>).MD(nameof(Series.CovarianceMatrix), typeof(Series[])));
    private static readonly MethodList MatrixCorrelation = new(
        typeof(Series<Date>).MD(nameof(Series.CorrelationMatrix), typeof(Series[])));
    private static readonly MethodList ModelPlot = new(
        typeof(Plot<DVector>).MD(VVArg),
        typeof(Plot<DVector>).MD(VArg),
        typeof(Plot<CVector>).MD(typeof(CVector), typeof(CVector)),
        typeof(Plot<CVector>).MD(typeof(CVector)),
        typeof(Plot<Series>).MD(typeof(Series), typeof(Series)),
        typeof(Plot<Series>).MD(typeof(Series)));
    private static readonly MethodList PolyDerivative = new(
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), DVArg),
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), typeof(double), typeof(double[])),
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), typeof(Complex), typeof(DVector)),
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), typeof(Complex), typeof(double[])));

    private readonly IReadOnlyList<string> emptyParameters = new List<string>(0).AsReadOnly();

    /// <summary>Reusable lambda block for parsing lambda expressions.</summary>
    public LambdaBlock LambdaBlock { get; } = new();

    private readonly FrozenDictionary<string, Type> typeNames =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["int"] = typeof(int),
            ["real"] = typeof(double),
            ["double"] = typeof(double),
            ["complex"] = typeof(Complex),
            ["series"] = typeof(Series),
            ["bool"] = typeof(bool),
            ["vec"] = typeof(DVector),
            ["cvec"] = typeof(CVector),
            ["ivec"] = typeof(NVector),
            ["seq"] = typeof(DSequence),
            ["cseq"] = typeof(CSequence),
            ["iseq"] = typeof(NSequence),
            ["matrix"] = typeof(Matrix),
            ["date"] = typeof(Date),
            ["string"] = typeof(string),
        }.ToFrozenDictionary();

    /// <summary>Code completion descriptors for root classes.</summary>
    private readonly Member[] rootClasses =
    [
        new("cseq::", "Allows access to complex sequence constructors"),
        new("cvec::", "Allows access to complex vector constructors"),
        new("iseq::", "Allows access to integer sequence constructors"),
        new("ivec::", "Allows access to integer vector constructors"),
        new("math::", "Allows access to mathematical functions"),
        new("matrix::", "Allows access to matrix constructors"),
        new("model::", "Allows access to model constructors"),
        new("seq::", "Allows access to sequence constructors"),
        new("series::", "Allows access to series constructors"),
        new("spline::", "Allows access to spline constructors"),
        new("vec::", "Allows access to vector constructors"),
    ];

    /// <summary>Code completion descriptors for class methods or constructors.</summary>
    private readonly FrozenDictionary<string, Member[]> classMembers =
        new Dictionary<string, Member[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["cseq"] = [
                new("new(", "Creates a complex sequence from a complex vector"),
                new("nrandom(", "Creates a complex sequence from normal random numbers"),
                new("random(", "Creates a complex sequence from random numbers"),
                new("unfold", "Creates a complex sequence from a seed and a lambda"),
            ],
            ["cvec"] = [
                new("new(", "Create a complex vector given a size and an optional lambda"),
                new("nrandom(", "Creates a random vector using a standard normal distribution given a length"),
                new("random(", "Creates a random complex vector given a length"),
            ],
            ["iseq"] = [
                new("new(", "Creates an integer sequence either from a range, a range and a step, or a vector"),
                new("random(", "Creates an integer sequence with random numbers"),
                new("unfold", "Creates an integer sequence from a seed and a lambda"),
            ],
            ["ivec"] = [
                new("new(", "Creates an integer vector given a size and an optional lambda"),
                new("ones(", "Creates an integer vector with ones given a length"),
                new("random(", "Creates a random integer vector given a length and an optional range"),
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
                new("maxInt", "Maximum value for an integer"),
                new("maxReal", "Maximum value for a real"),
                new("min(", "Minimum function"),
                new("minInt", "Minimum value for an integer"),
                new("minReal", "Minimum value for a real"),
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
            ["model"] = [
                new("mvo(", "Creates a model for a Mean Variance Optimizer"),
                new("simplex(", "Creates a model for a Linear Programming problem"),
                new("simplexMin(", "Creates a model for a Linear Programming problem minimizing its objective function"),
                new("plot(", "Plots vectors, series and sequences"),
            ],
            ["seq"] = [
                new("ar(", "Creates an autoregressive (AR) sequence"),
                new("ma(", "Creates an moving average (MA) sequence"),
                new("new(", "Creates a sequence from a range, a grid or a vector"),
                new("nrandom(", "Creates a sequence from normal random numbers"),
                new("random(", "Creates a sequence from random numbers"),
                new("repeat(", "Creates a sequence with a repeated value"),
                new("unfold", "Creates a sequence from a seed and a lambda"),
            ],
            ["series"] = [
                new("new(", "Creates a new series using weights and a list of series"),
            ],
            ["spline"] = [
                new("new(", "Creates a new interpolator either from two vectors, a series, or from a function"),
            ],
            ["vec"] = [
                new("new(", "Create a vector given a length and an optional lambda"),
                new("ones(", "Creates a vector with ones given a length"),
                new("nrandom(", "Creates a random vector using a standard normal distribution given a length"),
                new("random(", "Creates a random vector given a length"),
            ],
        }.ToFrozenDictionary();

    /// <summary>Code completion descriptors for properties and methods.</summary>
    private readonly FrozenDictionary<Type, Member[]> members =
        new Dictionary<Type, Member[]>()
        {
            [typeof(Acc)] = [
                new("count", "Gets the number of points"),
                new("kurt", "Gets the kurtosis"),
                new("kurtp", "Gets the kurtosis of the population"),
                new("max", "Gets the maximum value"),
                new("mean", "Gets the mean value"),
                new("min", "Gets the minimum value"),
                new("skew", "Gets the skewness"),
                new("skewp", "Gets the skewness of the population"),
                new("std", "Gets the standard deviation"),
                new("stdp", "Gets the standard deviation of the population"),
                new("var", "Gets the variance"),
                new("varp", "Gets the variance of the population"),
            ],
            [typeof(ARSModel)] = [
                new("coefficients", "Gets the autoregression coefficients"),
                new("original", "Gets the series to be explained"),
                new("prediction", "Gets the predicted series"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(ARVModel)] = [
                new("coefficients", "Gets the autoregression coefficients"),
                new("original", "Gets the vector to be explained"),
                new("prediction", "Gets the predicted vector"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(Cholesky)] = [
                new("lower", "Gets the lower-triangular matrix from the decomposition"),
                new("solve(", "Solves a linear system involving the original matrix"),
            ],
            [typeof(Complex)] = [
                new("imag", "Gets the imaginary part of the complex number"),
                new("mag", "Gets the magnitude of the complex number"),
                new("phase", "Gets the phase of the complex number"),
                new("real", "Gets the real part of the complex number"),
            ],
            [typeof(CSequence)] = [
                new("distinct", "Get the unique values in the sequence"),
                new("fft", "Performs a Fast Fourier Transform"),
                new("first", "Gets the first value in the sequence"),
                new("last", "Gets the last value in the sequence"),
                new("length", "Gets the number of values in the sequence"),
                new("plot", "Plots this sequence"),
                new("prod", "Gets the product of all values in the sequence"),
                new("sum", "Gets the sum of all values in the sequence"),
                new("toVector", "Converts the sequence to a complex vector"),
                new("all(x => ", "Universal operator"),
                new("any(x => ", "Existential operator"),
                new("filter(x => ", "Filters the sequence according to a predicate"),
                new("map(x => ", "Transforms the sequence according to a mapping function"),
                new("mapReal(x => ", "Transforms the sequence into a sequence of doubles"),
                new("reduce(", "Combines all values in the sequence into a single value"),
                new("zip(", "Combines two sequence using a lambda function"),
            ],
            [typeof(CVector)] = [
                new("amax", "Gets the maximum absolute value"),
                new("amin", "Gets the minimum absolute value"),
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
                new("prod", "Gets the product of all values in the vector"),
                new("real", "Gets the real components as a vector"),
                new("reverse", "Gets a reversed copy"),
                new("sqr", "Gets the squared norm of the vector"),
                new("sum", "Gets the sum of all values"),
                new("all(x => ", "Universal operator"),
                new("any(x => ", "Existential operator"),
                new("filter(x => ", "Filters items by value"),
                new("find(", "Finds the indexes of all ocurrences of a value"),
                new("indexof(", "Returns the index where a value is stored"),
                new("map(x => ", "Pointwise transformation of complex values"),
                new("mapreal(x => ", "Transforms complex vector into a real one"),
                new("reduce(", "Reduces a complex vector to a single value"),
                new("until(x => ", "Gets a subsequence until a predicate is satisfied"),
                new("while(x => ", "Gets a subsequence while a predicate is satisfied"),
                new("zip(", "Combines two complex vectors"),
            ],
            [typeof(Date)] = [
                new("day", "Gets the day of the date"),
                new("dow", "Gets the day of week the date"),
                new("isleap", "Checks if the date belong to a leap year"),
                new("month", "Gets the month of the date"),
                new("year", "Gets the year of the date"),
                new("toInt", "Converts the date to an integer"),
                new("addMonths(", "Adds a number of months to the date"),
                new("addYears(", "Adds a number of years to the date"),
            ],
            [typeof(DateSpline)] = [
                new("first", "Gets the lower bound of the spline's interval"),
                new("last", "Gets the upper bound of the spline's interval"),
                new("length", "Gets the number of segments in the spline"),
                new("derivative(", "Gets the derivative of the spline at the given date"),
                new("poly(", "Retrieve the cubic polynomial at the given index"),
            ],
            [typeof(double)] = [
                new("toInt", "Converts this double value to integer"),
            ],
            [typeof(DSequence)] = [
                new("acf", "Gets the autocorrelation function"),
                new("distinct", "Gets the unique values in the sequence"),
                new("fft", "Performs a Fast Fourier Transform"),
                new("first", "Gets the first value in the sequence"),
                new("last", "Gets the last value in the sequence"),
                new("length", "Gets the number of values in the sequence"),
                new("max", "Gets the maximum value from the sequence"),
                new("min", "Gets the minimum value from the sequence"),
                new("pacf", "Gets the partial autocorrelation function"),
                new("plot", "Plots this sequence"),
                new("prod", "Gets the product of all values in the sequence"),
                new("sort", "Sorts the sequence in ascending order"),
                new("sortDesc", "Sorts the sequence in descending order"),
                new("stats", "Gets the common statistics of the sequence"),
                new("sum", "Gets the sum of all values in the sequence"),
                new("toVector", "Converts the sequence to a vector"),
                new("all(x => ", "Universal operator"),
                new("any(x => ", "Existential operator"),
                new("ar(", "Calculates the autoregression coefficients"),
                new("arModel(", "Creates an AR(p) model"),
                new("filter(x => ", "Filters the sequence according to a predicate"),
                new("ma(", "Calculates coefficients for a Moving Average model"),
                new("maModel(", "Creates an MA(q) model"),
                new("map(x => ", "Transforms the sequence according to a mapping function"),
                new("reduce(", "Combines all values in the sequence into a single value"),
                new("until(x => ", "Gets a subsequence until a predicate is satisfied"),
                new("while(x => ", "Gets a subsequence while a predicate is satisfied"),
                new("zip(", "Combines two sequence using a lambda function"),
            ],
            [typeof(DVector)] = [
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
                new("pacf", "Partial AutoCorrelation Function"),
                new("plot", "Plots this vector"),
                new("prod", "Gets the product of all values"),
                new("reverse", "Gets a reversed copy"),
                new("sort", "Gets a new vector with sorted values"),
                new("sortDesc", "Gets a new vector with sorted values in descending order"),
                new("sqr", "Gets the squared norm of the vector"),
                new("sqrt", "Pointwise squared root"),
                new("stats", "Gets all the statistics"),
                new("sum", "Gets the sum of all values"),
                new("all(x => ", "Universal operator"),
                new("any(x => ", "Existential operator"),
                new("ar(", "Calculates the autoregression coefficients"),
                new("arModel(", "Creates an AR(p) model"),
                new("autocorr(", "Gets the autocorrelation given a lag"),
                new("correlogram(", "Gets all autocorrelations up to a given lag"),
                new("filter(x => ", "Filters items by value"),
                new("find(", "Finds the indexes of all ocurrences of a value"),
                new("indexof(", "Returns the index where a value is stored"),
                new("linear(", "Gets the regression coefficients given a list of vectors"),
                new("linearModel(", "Creates a linear model"),
                new("ma(", "Calculates coefficients for a Moving Average model"),
                new("maModel(", "Creates an MA(q) model"),
                new("map(x => ", "Pointwise transformation of vector items"),
                new("reduce(", "Reduces a vector to a single value"),
                new("zip(", "Combines two vectors"),
            ],
            [typeof(EVD)] = [
                new("d", "Gets a quasi-diagonal real matrix with all eigenvalues"),
                new("rank", "Gets the rank of the original matrix"),
                new("values", "Gets all the eigenvalues"),
                new("vectors", "Gets a matrix with eigenvectors as its columns"),
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
            [typeof(int)] = [
                new("even", "Checks if the integer is an even number"),
                new("odd", "Checks if the integer is an odd number"),
            ],
            [typeof(LinearSModel)] = [
                new("original", "Gets the series to be explained"),
                new("prediction", "Gets the predicted series"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
                new("weights", "Gets the regression coefficients"),
            ],
            [typeof(LinearVModel)] = [
                new("original", "Gets the vector to be explained"),
                new("prediction", "Gets the predicted vector"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
                new("weights", "Gets the regression coefficients"),
            ],
            [typeof(LMatrix)] = [
                new("amax", "Gets the maximum absolute value"),
                new("amin", "Gets the minimum absolute value"),
                new("cols", "Gets the number of columns"),
                new("det", "Calculates the determinant"),
                new("diag", "Extracts the diagonal as a vector"),
                new("inverse", "Calculates the inverse of a square triangular matrix"),
                new("max", "Gets the maximum value"),
                new("min", "Gets the minimum absolute value"),
                new("rows", "Gets the number of rows"),
                new("stats", "Calculates statistics on the cells"),
                new("trace", "Gets the sum of the main diagonal"),
                new("redim(", "Creates a new matrix with a different size"),
            ],
            [typeof(LU)] = [
                new("det", "Gets the determinant of the decomposed matrix"),
                new("lower", "Gets the lower triangular matrix of the LU decomposition"),
                new("perm", "Gets the permutation vector"),
                new("size", "Dimensions of the LU decomposition"),
                new("upper", "Gets the upper triangular matrix of the LU decomposition"),
                new("solve(", "Solves a linear equation involving a vector or a matrix"),
            ],
            [typeof(MASModel)] = [
                new("coefficients", "Gets the autoregression coefficients"),
                new("mean", "Gets the independent term of the model"),
                new("original", "Gets the series to be explained"),
                new("prediction", "Gets the predicted series"),
                new("r2", "Gets the regression coefficient"),
                new("residuals", "Gets the estimated residuals"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(Matrix)] = [
                new("amax", "Gets the maximum absolute value"),
                new("amin", "Gets the minimum absolute value"),
                new("chol", "Gets the lower-triangular matrix of a Cholesky Decomposition"),
                new("cholesky", "Calculates the Cholesky Decomposition"),
                new("cols", "Gets the number of columns"),
                new("det", "Calculates the determinant"),
                new("diag", "Extracts the diagonal as a vector"),
                new("evd", "Calculates the EigenValues Decomposition"),
                new("inverse", "Calculates the inverse of a square matrix"),
                new("isSymmetric", "Checks if a matrix is symmetric"),
                new("lu", "Calculates the LU Decomposition of a square matrix"),
                new("max", "Gets the maximum value"),
                new("min", "Gets the minimum absolute value"),
                new("rows", "Gets the number of rows"),
                new("stats", "Calculates statistics on the cells"),
                new("trace", "Gets the sum of the main diagonal"),
                new("all(x => ", "Universal operator"),
                new("any(x => ", "Existential operator"),
                new("getCol(", "Extracts a column as a vector"),
                new("getRow(", "Extracts a row as a vector"),
                new("map(x => ", "Pointwise transformation of matrix cells"),
                new("redim(", "Creates a new matrix with a different size"),
            ],
            [typeof(MAVModel)] = [
                new("coefficients", "Gets the autoregression coefficients"),
                new("mean", "Gets the independent term of the model"),
                new("original", "Gets the vector to be explained"),
                new("prediction", "Gets the predicted vector"),
                new("r2", "Gets the regression coefficient"),
                new("residuals", "Gets the estimated residuals"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(MvoModel)] = [
                new("first", "Gets the first corner portfolio"),
                new("last", "Gets the last corner portfolio"),
                new("length", "Gets the number of corner portfolios"),
                new("size", "Gets the number of assets in the model"),
                new("setConstraints(", "Adds constraints to the model and recalculates the frontier"),
            ],
            [typeof(NSequence)] = [
                new("distinct", "Get the unique values in the sequence"),
                new("first", "Gets the first value in the sequence"),
                new("last", "Gets the last value in the sequence"),
                new("length", "Gets the number of values in the sequence"),
                new("max", "Gets the maximum value from the sequence"),
                new("min", "Gets the minimum value from the sequence"),
                new("plot", "Plots this sequence"),
                new("prod", "Gets the product of all values in the sequence"),
                new("sort", "Sorts the sequence in ascending order"),
                new("sortDesc", "Sorts the sequence in descending order"),
                new("stats", "Gets the common statistics of the sequence"),
                new("sum", "Gets the sum of all values in the sequence"),
                new("toVector", "Converts the sequence to a vector"),
                new("all(x => ", "Universal operator"),
                new("any(x => ", "Existential operator"),
                new("filter(x => ", "Filters the sequence according to a predicate"),
                new("map(x => ", "Transforms the sequence according to a mapping function"),
                new("mapReal(x => ", "Transforms the sequence into a sequence of doubles"),
                new("reduce(", "Combines all values in the sequence into a single value"),
                new("until(x => ", "Gets a subsequence until a predicate is satisfied"),
                new("while(x => ", "Gets a subsequence while a predicate is satisfied"),
                new("zip(", "Combines two sequence using a lambda function"),
            ],
            [typeof(NVector)] = [
                new("abs", "Pointwise absolute value"),
                new("distinct", "Gets a new vector with distinct values"),
                new("first", "Gets the first item from the vector"),
                new("last", "Gets the last item from the vector"),
                new("length", "Gets the number of items"),
                new("max", "Gets the maximum  value"),
                new("mean", "Gets the mean value"),
                new("min", "Gets the minimum value"),
                new("plot", "Plots this vector"),
                new("prod", "Gets the product of all values"),
                new("reverse", "Gets a reversed copy"),
                new("stats", "Gets all the statistics"),
                new("sort", "Gets a new vector with sorted values"),
                new("sortDesc", "Gets a new vector with sorted values in descending order"),
                new("sum", "Gets the sum of all values"),
                new("toVector", "Converts to a vector of reals"),
                new("all(x => ", "Universal operator"),
                new("any(x => ", "Existential operator"),
                new("filter(x => ", "Filters items by value"),
                new("indexOf(", "Returns the index where a value is stored"),
                new("map(x => ", "Pointwise transformation of vector items"),
                new("mapReal(x => ", "Pointwise transformation from integers to reals"),
                new("reduce(", "Reduces a vector to a single value"),
                new("zip(", "Combines two integer vectors"),
            ],
            [typeof(Point<Date>)] = [
                new("date", "Gets the date argument"),
                new("value", "Gets the numerical value of the point"),
            ],
            [typeof(Polynomial)] = [
                new("area", "Gets the definite integral of the polynomial over the interval [0, 1]"),
                new("derivative", "Gets the derivative at a point between 0 and 1"),
                new("eval", "Evaluates the polynomial at a point between 0 and 1"),
            ],
            [typeof(Portfolio)] = [
                new("lambda", "Gets the lambda of a corner portfolio"),
                new("ret", "Gets the expected return of the portfolio"),
                new("std", "Gets the standard deviation of the portfolio"),
                new("var", "Gets the variance of the portfolio"),
                new("weights", "Gets weights of the portfolio"),
            ],
            [typeof(RMatrix)] = [
                new("amax", "Gets the maximum absolute value"),
                new("amin", "Gets the minimum absolute value"),
                new("cols", "Gets the number of columns"),
                new("det", "Calculates the determinant"),
                new("diag", "Extracts the diagonal as a vector"),
                new("max", "Gets the maximum value"),
                new("min", "Gets the minimum absolute value"),
                new("rows", "Gets the number of rows"),
                new("stats", "Calculates statistics on the cells"),
                new("trace", "Gets the sum of the main diagonal"),
                new("redim(", "Creates a new matrix with a different size"),
            ],
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
                new("pacf", "Partial AutoCorrelation Function"),
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
                new("values", "Gets the underlying vector of values"),
                new("var", "Gets the variance"),
                new("varp", "Gets the variance of the population"),
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
                new("ma(", "Calculates coefficients for a Moving Average model"),
                new("maModel(", "Creates an MA(q) model"),
                new("map(x => ", "Pointwise transformation of the series"),
                new("movingAvg(", "Calculates a Simple Moving Average"),
                new("movingNcdf(", "Calculates a Moving Normal Percentile"),
                new("movingStd(", "Calculates a Moving Standard Deviation"),
                new("stats(", "Gets monthly statistics for a given date"),
                new("zip(", "Combines two series"),
            ],
            [typeof(Series<double>)] = [
                new("stats", "Gets all statistics"),
                new("first", "Gets the first point"),
                new("last", "Gets the last point"),
                new("values", "Gets the underlying vector of values"),
            ],
            [typeof(Series<int>)] = [
                new("stats", "Gets all statistics"),
                new("first", "Gets the first point"),
                new("last", "Gets the last point"),
                new("values", "Gets the underlying vector of values"),
            ],
            [typeof(SimplexModel)] = [
                new("objective", "Gets the coefficients of the objective function"),
                new("value", "Gets the value of the objective function at the optimal solution"),
                new("weights", "Gets the weights of the optimal solution"),
            ],
            [typeof(VectorSpline)] = [
                new("area", "Gets the approximate area below the spline"),
                new("first", "Gets the lower bound of the spline's interval"),
                new("last", "Gets the upper bound of the spline's interval"),
                new("length", "Gets the number of segments in the spline"),
                new("derivative(", "Gets the derivative of the spline at the given point"),
                new("poly(", "Retrieve the cubic polynomial at the given index"),
            ],
        }.ToFrozenDictionary();

    /// <summary>Information for class methods.</summary>
    /// <remarks>
    /// An AUSTRA class method may be implemented either by a static method or by a constructor.
    /// </remarks>
    private readonly FrozenDictionary<string, MethodList> classMethods =
        new Dictionary<string, MethodList>()
        {
            ["cseq.new"] = new(
                typeof(CSequence).MD(nameof(CSequence.Create),
                    typeof(Complex), typeof(Complex), typeof(int)),
                typeof(CSequence).MD(nameof(CSequence.Create), typeof(CVector))),
            ["cseq.random"] = new(
                typeof(CSequence).MD(nameof(CSequence.Random), typeof(int))),
            ["cseq.nrandom"] = new(
                typeof(CSequence).MD(nameof(CSequence.NormalRandom), typeof(int)),
                typeof(CSequence).MD(nameof(CSequence.NormalRandom), NDArg)),
            ["cseq.unfold"] = new(
                typeof(CSequence).MD(nameof(CSequence.Unfold),
                    typeof(int), typeof(Complex), typeof(Func<Complex, Complex>)),
                typeof(CSequence).MD(nameof(CSequence.Unfold),
                    typeof(int), typeof(Complex), typeof(Func<int, Complex, Complex>)),
                typeof(CSequence).MD(nameof(CSequence.Unfold),
                    typeof(int), typeof(Complex), typeof(Complex),
                    typeof(Func<Complex, Complex, Complex>))),
            ["cvec.new"] = new(
                typeof(CVector).MD(VArg),
                typeof(CVector).MD(VVArg),
                typeof(CVector).MD(NArg),
                typeof(CVector).MD(typeof(int), typeof(Func<int, Complex>)),
                typeof(CVector).MD(typeof(int), typeof(Func<int, CVector, Complex>))),
            ["cvec.nrandom"] = new(
                typeof(CVector).MD(typeof(int), typeof(NormalRandom))),
            ["cvec.random"] = new(
                typeof(CVector).MD(typeof(int), typeof(Random))),
            ["iseq.new"] = new(
                typeof(NSequence).MD(nameof(NSequence.Create), NNArg),
                typeof(NSequence).MD(nameof(NSequence.Create), [.. NNArg, typeof(int)]),
                typeof(NSequence).MD(nameof(NSequence.Create), typeof(NVector))),
            ["iseq.random"] = new(
                typeof(NSequence).MD(nameof(NSequence.Random), NArg),
                typeof(NSequence).MD(nameof(NSequence.Random), NNArg),
                typeof(NSequence).MD(nameof(NSequence.Random), [.. NNArg, typeof(int)])),
            ["iseq.unfold"] = new(
                typeof(NSequence).MD(nameof(NSequence.Unfold),
                    typeof(int), typeof(int), typeof(Func<int, int>)),
                typeof(NSequence).MD(nameof(NSequence.Unfold),
                    typeof(int), typeof(int), typeof(Func<int, int, int>)),
                typeof(NSequence).MD(nameof(NSequence.Unfold),
                    typeof(int), typeof(int), typeof(int), typeof(Func<int, int, int>))),
            ["ivec.new"] = new(
                typeof(NVector).MD(NArg),
                typeof(NVector).MD(typeof(int), typeof(Func<int, int>)),
                typeof(NVector).MD(typeof(int), typeof(Func<int, NVector, int>))),
            ["ivec.ones"] = new(
                typeof(NVector).MD(typeof(int), typeof(One))),
            ["ivec.random"] = new(
                typeof(NVector).MD(typeof(int), typeof(Random)),
                typeof(NVector).MD(typeof(int), typeof(int), typeof(Random)),
                typeof(NVector).MD(typeof(int), typeof(int), typeof(int), typeof(Random))),
            ["math.abs"] = new(
                typeof(Math).MD(nameof(Math.Abs), NArg),
                typeof(Math).MD(nameof(Math.Abs), DArg),
                typeof(Complex).MD(nameof(Complex.Abs), CArg)),
            ["math.acos"] = new(
                typeof(Math).MD(nameof(Math.Acos), DArg),
                typeof(Complex).MD(nameof(Complex.Acos), CArg)),
            ["math.asin"] = new(
                typeof(Math).MD(nameof(Math.Asin), DArg),
                typeof(Complex).MD(nameof(Complex.Asin), CArg)),
            ["math.atan"] = new(
                typeof(Math).MD(nameof(Math.Atan), DArg),
                typeof(Math).MD(nameof(Math.Atan2), DDArg),
                typeof(Complex).MD(nameof(Complex.Atan), CArg)),
            ["math.beta"] = new(
                typeof(Functions).MD(nameof(Functions.Beta), DDArg)),
            ["math.cbrt"] = new(
                typeof(Math).MD(nameof(Math.Cbrt), DArg)),
            ["math.complex"] = new(
                typeof(Complex).MD(DDArg),
                typeof(Complex).MD(typeof(double), typeof(Zero))),
            ["math.cos"] = new(
                typeof(Math).MD(nameof(Math.Cos), DArg),
                typeof(Complex).MD(nameof(Complex.Cos), CArg)),
            ["math.cosh"] = new(
                typeof(Math).MD(nameof(Math.Cosh), DArg),
                typeof(Complex).MD(nameof(Complex.Cosh), CArg)),
            ["math.erf"] = new(
                typeof(Functions).MD(nameof(Functions.Erf), DArg)),
            ["math.exp"] = new(
                typeof(Math).MD(nameof(Math.Exp), DArg),
                typeof(Complex).MD(nameof(Complex.Exp), CArg)),
            ["math.gamma"] = new(
                typeof(Functions).MD(nameof(Functions.Gamma), DArg)),
            ["math.lngamma"] = new(
                typeof(Functions).MD(nameof(Functions.GammaLn), DArg)),
            ["math.log"] = new(
                typeof(Math).MD(nameof(Math.Log), DArg),
                typeof(Complex).MD(nameof(Complex.Log), CArg)),
            ["math.log10"] = new(
                typeof(Math).MD(nameof(Math.Log10), DArg),
                typeof(Complex).MD(nameof(Complex.Log10), CArg)),
            ["math.min"] = new(
                typeof(Date).MD(nameof(Date.Min), typeof(Date), typeof(Date)),
                typeof(Math).MD(nameof(Math.Min), NNArg),
               typeof(Math).MD(nameof(Math.Min), DDArg)),
            ["math.max"] = new(
                typeof(Date).MD(nameof(Date.Max), typeof(Date), typeof(Date)),
                typeof(Math).MD(nameof(Math.Max), NNArg),
                typeof(Math).MD(nameof(Math.Max), DDArg)),
            ["math.ncdf"] = new(
                typeof(Functions).MD(nameof(Functions.NCdf), DArg)),
            ["math.plot"] = ModelPlot,
            ["math.polar"] = new(
                typeof(Complex).MD(nameof(Complex.FromPolarCoordinates), DDArg),
                typeof(Complex).MD(nameof(Complex.FromPolarCoordinates), typeof(double), typeof(Zero))),
            ["math.polyderiv"] = PolyDerivative,
            ["math.polyderivative"] = PolyDerivative,
            ["math.polyeval"] = new(
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), DVArg),
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), typeof(double), typeof(double[])),
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), typeof(Complex), typeof(DVector)),
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), typeof(Complex), typeof(double[]))),
            ["math.polysolve"] = new(
                typeof(Polynomials).MD(nameof(Polynomials.PolySolve), VArg),
                typeof(Polynomials).MD(nameof(Polynomials.PolySolve), typeof(double[]))),
            ["math.probit"] = new(
                typeof(Functions).MD(nameof(Functions.Probit), DArg)),
            ["math.sign"] = new(
                typeof(Math).MD(nameof(Math.Sign), NArg),
                typeof(Math).MD(nameof(Math.Sign), DArg)),
            ["math.sin"] = new(
                typeof(Math).MD(nameof(Math.Sin), DArg),
                typeof(Complex).MD(nameof(Complex.Sin), CArg)),
            ["math.sinh"] = new(
                typeof(Math).MD(nameof(Math.Sinh), DArg),
                typeof(Complex).MD(nameof(Complex.Sinh), CArg)),
            ["math.solve"] = new(
                typeof(Solver).MD(nameof(Solver.Solve),
                    typeof(Func<double, double>), typeof(Func<double, double>), typeof(double)),
                typeof(Solver).MD(nameof(Solver.Solve),
                    typeof(Func<double, double>), typeof(Func<double, double>), typeof(double),
                    typeof(double)),
                typeof(Solver).MD(nameof(Solver.Solve),
                    typeof(Func<double, double>), typeof(Func<double, double>), typeof(double),
                    typeof(double), typeof(int))),
            ["math.tan"] = new(
                typeof(Math).MD(nameof(Math.Tan), DArg),
                typeof(Complex).MD(nameof(Complex.Tan), CArg)),
            ["math.tanh"] = new(
                typeof(Math).MD(nameof(Math.Tanh), DArg),
                typeof(Complex).MD(nameof(Complex.Tanh), CArg)),
            ["math.sqrt"] = new(
                typeof(Math).MD(nameof(Math.Sqrt), DArg),
                typeof(Complex).MD(nameof(Complex.Sqrt), CArg)),
            ["math.trunc"] = new(
                typeof(Math).MD(nameof(Math.Truncate), DArg)),
            ["math.round"] = new(
                typeof(Math).MD(nameof(Math.Round), DArg),
                typeof(Math).MD(nameof(Math.Round), typeof(double), typeof(int))),
            ["matrix.new"] = new(
                typeof(Matrix).MD(NArg),
                typeof(Matrix).MD(NNArg),
                typeof(Matrix).MD(typeof(int), typeof(Func<int, int, double>)),
                typeof(Matrix).MD(typeof(int), typeof(int), typeof(Func<int, int, double>))),
            ["matrix.rows"] = new(
                typeof(Matrix).MD(typeof(DVector[]))),
            ["matrix.cols"] = new(
                typeof(Matrix).MD(nameof(Matrix.FromColumns), typeof(DVector[]))),
            ["matrix.diag"] = new(
                typeof(Matrix).MD(VArg),
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
            ["model.plot"] = ModelPlot,
            ["model.mvo"] = new(
                typeof(MvoModel).MD(typeof(DVector), typeof(Matrix)),
                typeof(MvoModel).MD(typeof(DVector), typeof(Matrix), typeof(DVector), typeof(DVector)),
                typeof(MvoModel).MD(typeof(DVector), typeof(Matrix), typeof(Series[])),
                typeof(MvoModel).MD(typeof(DVector), typeof(Series[])),
                typeof(MvoModel).MD(typeof(DVector), typeof(Matrix),
                    typeof(DVector), typeof(DVector), typeof(Series[])),
                typeof(MvoModel).MD(typeof(DVector), typeof(Matrix), typeof(string[])),
                typeof(MvoModel).MD(typeof(DVector), typeof(Matrix),
                    typeof(DVector), typeof(DVector), typeof(string[]))),
            ["model.simplex"] = new(
                typeof(SimplexModel).MD(typeof(DVector), typeof(Matrix), typeof(DVector), typeof(NVector), typeof(string[])),
                typeof(SimplexModel).MD(typeof(DVector), typeof(Matrix), typeof(DVector), typeof(NVector)),
                typeof(SimplexModel).MD(typeof(DVector), typeof(Matrix), typeof(DVector), typeof(int)),
                typeof(SimplexModel).MD(typeof(DVector), typeof(Matrix), typeof(DVector))),
            ["model.simplexmin"] = new(
                typeof(SimplexModel).MD(nameof(SimplexModel.Minimize),
                    typeof(DVector), typeof(Matrix), typeof(DVector), typeof(NVector), typeof(string[])),
                typeof(SimplexModel).MD(nameof(SimplexModel.Minimize),
                    typeof(DVector), typeof(Matrix), typeof(DVector), typeof(NVector)),
                typeof(SimplexModel).MD(nameof(SimplexModel.Minimize),
                    typeof(DVector), typeof(Matrix), typeof(DVector), typeof(int)),
                typeof(SimplexModel).MD(nameof(SimplexModel.Minimize),
                    typeof(DVector), typeof(Matrix), typeof(DVector))),
            ["seq.ar"] = new(
                typeof(DSequence).MD(nameof(DSequence.AR),
                    typeof(int), typeof(double), typeof(DVector))),
            ["seq.ma"] = new(
                typeof(DSequence).MD(nameof(DSequence.MA),
                    typeof(int), typeof(double), typeof(DVector))),
            ["seq.new"] = new(
                typeof(DSequence).MD(nameof(DSequence.Create), NNArg),
                typeof(DSequence).MD(nameof(DSequence.Create), DDArg),
                typeof(DSequence).MD(nameof(DSequence.Create),
                    typeof(double), typeof(int), typeof(double)),
                typeof(DSequence).MD(nameof(DSequence.Create), typeof(DVector)),
                typeof(DSequence).MD(nameof(DSequence.Create), typeof(Matrix)),
                typeof(DSequence).MD(nameof(DSequence.Create), typeof(Series))),
            ["seq.nrandom"] = new(
                typeof(DSequence).MD(nameof(DSequence.NormalRandom), typeof(int)),
                typeof(DSequence).MD(nameof(DSequence.NormalRandom), NDArg)),
            ["seq.random"] = new(
                typeof(DSequence).MD(nameof(DSequence.Random), typeof(int))),
            ["seq.repeat"] = new(
                typeof(DSequence).MD(nameof(DSequence.Repeat), typeof(int), typeof(double))),
            ["seq.unfold"] = new(
                typeof(DSequence).MD(nameof(DSequence.Unfold),
                    typeof(int), typeof(double), typeof(Func<double, double>)),
                typeof(DSequence).MD(nameof(DSequence.Unfold),
                    typeof(int), typeof(double), typeof(Func<int, double, double>)),
                typeof(DSequence).MD(nameof(DSequence.Unfold),
                    typeof(int), typeof(double), typeof(double),
                    typeof(Func<double, double, double>))),
            ["series.new"] = new(
                typeof(Series).MD(nameof(Series.Combine), typeof(DVector), typeof(Series[]))),
            ["spline.new"] = new(
                typeof(DateSpline).MD(typeof(Series)),
                typeof(VectorSpline).MD(VVArg),
                typeof(VectorSpline).MD(
                    typeof(double), typeof(double), typeof(int), typeof(Func<double, double>))),
            ["vec.new"] = new(
                typeof(DVector).MD(NArg),
                typeof(DVector).MD(nameof(DVector.Combine), typeof(DVector), typeof(DVector[])),
                typeof(DVector).MD(typeof(int), typeof(Func<int, double>)),
                typeof(DVector).MD(typeof(int), typeof(Func<int, DVector, double>))),
            ["vec.nrandom"] = new(
                typeof(DVector).MD(typeof(int), typeof(NormalRandom))),
            ["vec.random"] = new(
                typeof(DVector).MD(typeof(int), typeof(Random))),
            ["vec.ones"] = new(
                typeof(DVector).MD(typeof(int), typeof(One))),
        }.ToFrozenDictionary();

    /// <summary>Allowed properties and their implementations.</summary>
    private readonly FrozenDictionary<TypeId, MethodInfo> allProps =
        new Dictionary<TypeId, MethodInfo>()
        {
            [new(typeof(Acc), "count")] = typeof(Acc).Prop(nameof(Acc.Count)),
            [new(typeof(Acc), "kurt")] = typeof(Acc).Prop(nameof(Acc.Kurtosis)),
            [new(typeof(Acc), "kurtp")] = typeof(Acc).Prop(nameof(Acc.PopulationKurtosis)),
            [new(typeof(Acc), "max")] = typeof(Acc).Prop(nameof(Acc.Maximum)),
            [new(typeof(Acc), "mean")] = typeof(Acc).Prop(nameof(Acc.Mean)),
            [new(typeof(Acc), "min")] = typeof(Acc).Prop(nameof(Acc.Minimum)),
            [new(typeof(Acc), "skew")] = typeof(Acc).Prop(nameof(Acc.Skewness)),
            [new(typeof(Acc), "skewp")] = typeof(Acc).Prop(nameof(Acc.PopulationSkewness)),
            [new(typeof(Acc), "std")] = typeof(Acc).Prop(nameof(Acc.StandardDeviation)),
            [new(typeof(Acc), "stdp")] = typeof(Acc).Prop(nameof(Acc.PopulationStandardDeviation)),
            [new(typeof(Acc), "var")] = typeof(Acc).Prop(nameof(Acc.Variance)),
            [new(typeof(Acc), "varp")] = typeof(Acc).Prop(nameof(Acc.PopulationVariance)),

            [new(typeof(ARSModel), "coeff")] = typeof(ARSModel).Prop(nameof(ARSModel.Coefficients)),
            [new(typeof(ARSModel), "coefficients")] = typeof(ARSModel).Prop(nameof(ARSModel.Coefficients)),
            [new(typeof(ARSModel), "original")] = typeof(ARSModel).Prop(nameof(ARSModel.Original)),
            [new(typeof(ARSModel), "prediction")] = typeof(ARSModel).Prop(nameof(ARSModel.Prediction)),
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

            [new(typeof(Cholesky), "l")] = typeof(Cholesky).Prop(nameof(Cholesky.L)),
            [new(typeof(Cholesky), "lower")] = typeof(Cholesky).Prop(nameof(Cholesky.L)),

            [new(typeof(Complex), "real")] = typeof(Complex).Prop(nameof(Complex.Real)),
            [new(typeof(Complex), "re")] = typeof(Complex).Prop(nameof(Complex.Real)),
            [new(typeof(Complex), "imaginary")] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
            [new(typeof(Complex), "imag")] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
            [new(typeof(Complex), "im")] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
            [new(typeof(Complex), "magnitude")] = typeof(Complex).Prop(nameof(Complex.Magnitude)),
            [new(typeof(Complex), "mag")] = typeof(Complex).Prop(nameof(Complex.Magnitude)),
            [new(typeof(Complex), "phase")] = typeof(Complex).Prop(nameof(Complex.Phase)),

            [new(typeof(CSequence), "distinct")] = typeof(CSequence).Get(nameof(CSequence.Distinct)),
            [new(typeof(CSequence), "first")] = typeof(CSequence).Get(nameof(CSequence.First)),
            [new(typeof(CSequence), "fft")] = typeof(CSequence).Get(nameof(CSequence.Fft)),
            [new(typeof(CSequence), "last")] = typeof(CSequence).Get(nameof(CSequence.Last)),
            [new(typeof(CSequence), "length")] = typeof(CSequence).Get(nameof(CSequence.Length)),
            [new(typeof(CSequence), "plot")] = typeof(CSequence).Get(nameof(CSequence.Plot)),
            [new(typeof(CSequence), "prod")] = typeof(CSequence).Get(nameof(CSequence.Product)),
            [new(typeof(CSequence), "product")] = typeof(CSequence).Get(nameof(CSequence.Product)),
            [new(typeof(CSequence), "sum")] = typeof(CSequence).Get(nameof(CSequence.Sum)),
            [new(typeof(CSequence), "tovector")] = typeof(CSequence).GetMethod(
                nameof(CSequence.ToVector), Type.EmptyTypes)!,

            [new(typeof(CVector), "amax")] = typeof(CVector).Get(nameof(CVector.AbsMax)),
            [new(typeof(CVector), "amin")] = typeof(CVector).Get(nameof(CVector.AbsMin)),
            [new(typeof(CVector), "amplitudes")] = typeof(CVector).Get(nameof(CVector.Magnitudes)),
            [new(typeof(CVector), "distinct")] = typeof(CVector).Get(nameof(CVector.Distinct)),
            [new(typeof(CVector), "fft")] = typeof(CVector).Get(nameof(CVector.Fft)),
            [new(typeof(CVector), "first")] = typeof(CVector).Prop(nameof(CVector.First)),
            [new(typeof(CVector), "im")] = typeof(CVector).Prop(nameof(CVector.Imaginary)),
            [new(typeof(CVector), "imag")] = typeof(CVector).Prop(nameof(CVector.Imaginary)),
            [new(typeof(CVector), "imaginary")] = typeof(CVector).Prop(nameof(CVector.Imaginary)),
            [new(typeof(CVector), "last")] = typeof(CVector).Prop(nameof(CVector.Last)),
            [new(typeof(CVector), "length")] = typeof(CVector).Prop(nameof(CVector.Length)),
            [new(typeof(CVector), "mag")] = typeof(CVector).Get(nameof(CVector.Magnitudes)),
            [new(typeof(CVector), "mags")] = typeof(CVector).Get(nameof(CVector.Magnitudes)),
            [new(typeof(CVector), "magnitudes")] = typeof(CVector).Get(nameof(CVector.Magnitudes)),
            [new(typeof(CVector), "mean")] = typeof(CVector).Get(nameof(CVector.Mean)),
            [new(typeof(CVector), "norm")] = typeof(CVector).Get(nameof(CVector.Norm)),
            [new(typeof(CVector), "phases")] = typeof(CVector).Get(nameof(CVector.Phases)),
            [new(typeof(CVector), "plot")] = typeof(CVector).Get(nameof(CVector.Plot)),
            [new(typeof(CVector), "prod")] = typeof(CVector).Get(nameof(CVector.Product)),
            [new(typeof(CVector), "product")] = typeof(CVector).Get(nameof(CVector.Product)),
            [new(typeof(CVector), "re")] = typeof(CVector).Prop(nameof(CVector.Real)),
            [new(typeof(CVector), "real")] = typeof(CVector).Prop(nameof(CVector.Real)),
            [new(typeof(CVector), "reverse")] = typeof(CVector).Get(nameof(CVector.Reverse)),
            [new(typeof(CVector), "sqr")] = typeof(CVector).Get(nameof(CVector.Squared)),
            [new(typeof(CVector), "sum")] = typeof(CVector).Get(nameof(CVector.Sum)),

            [new(typeof(Date), "day")] = typeof(Date).Prop(nameof(Date.Day)),
            [new(typeof(Date), "dow")] = typeof(Date).Prop(nameof(Date.DayOfWeek)),
            [new(typeof(Date), "isleap")] = typeof(Date).Get(nameof(Date.IsLeap)),
            [new(typeof(Date), "month")] = typeof(Date).Prop(nameof(Date.Month)),
            [new(typeof(Date), "toint")] = typeof(Date).Prop(nameof(Date.ToInt)),
            [new(typeof(Date), "year")] = typeof(Date).Prop(nameof(Date.Year)),

            [new(typeof(DateSpline), "area")] = typeof(DateSpline).Prop(nameof(DateSpline.Area)),
            [new(typeof(DateSpline), "first")] = typeof(DateSpline).Prop(nameof(DateSpline.First)),
            [new(typeof(DateSpline), "last")] = typeof(DateSpline).Prop(nameof(DateSpline.Last)),
            [new(typeof(DateSpline), "length")] = typeof(DateSpline).Prop(nameof(DateSpline.Length)),

            [new(typeof(DSequence), "acf")] = typeof(DSequence).Get(nameof(DSequence.ACF)),
            [new(typeof(DSequence), "distinct")] = typeof(DSequence).Get(nameof(DSequence.Distinct)),
            [new(typeof(DSequence), "first")] = typeof(DSequence).Get(nameof(DSequence.First)),
            [new(typeof(DSequence), "fft")] = typeof(DSequence).Get(nameof(DSequence.Fft)),
            [new(typeof(DSequence), "last")] = typeof(DSequence).Get(nameof(DSequence.Last)),
            [new(typeof(DSequence), "length")] = typeof(DSequence).Get(nameof(DSequence.Length)),
            [new(typeof(DSequence), "min")] = typeof(DSequence).Get(nameof(DSequence.Min)),
            [new(typeof(DSequence), "max")] = typeof(DSequence).Get(nameof(DSequence.Max)),
            [new(typeof(DSequence), "pacf")] = typeof(DSequence).Get(nameof(DSequence.PACF)),
            [new(typeof(DSequence), "plot")] = typeof(DSequence).Get(nameof(DSequence.Plot)),
            [new(typeof(DSequence), "prod")] = typeof(DSequence).Get(nameof(DSequence.Product)),
            [new(typeof(DSequence), "product")] = typeof(DSequence).Get(nameof(DSequence.Product)),
            [new(typeof(DSequence), "sort")] = typeof(DSequence).Get(nameof(DSequence.Sort)),
            [new(typeof(DSequence), "sortasc")] = typeof(DSequence).Get(nameof(DSequence.Sort)),
            [new(typeof(DSequence), "sortdesc")] = typeof(DSequence).Get(nameof(DSequence.SortDescending)),
            [new(typeof(DSequence), "stats")] = typeof(DSequence).Get(nameof(DSequence.Stats)),
            [new(typeof(DSequence), "sum")] = typeof(DSequence).Get(nameof(DSequence.Sum)),
            [new(typeof(DSequence), "tovector")] = typeof(DSequence).Get(nameof(DSequence.ToVector)),

            [new(typeof(DVector), "abs")] = typeof(DVector).Get(nameof(DVector.Abs)),
            [new(typeof(DVector), "acf")] = typeof(DVector).Get(nameof(DVector.ACF)),
            [new(typeof(DVector), "amax")] = typeof(DVector).Get(nameof(DVector.AMax)),
            [new(typeof(DVector), "amin")] = typeof(DVector).Get(nameof(DVector.AMin)),
            [new(typeof(DVector), "distinct")] = typeof(DVector).Get(nameof(DVector.Distinct)),
            [new(typeof(DVector), "fft")] = typeof(DVector).Get(nameof(DVector.Fft)),
            [new(typeof(DVector), "first")] = typeof(DVector).Prop(nameof(DVector.First)),
            [new(typeof(DVector), "last")] = typeof(DVector).Prop(nameof(DVector.Last)),
            [new(typeof(DVector), "length")] = typeof(DVector).Prop(nameof(DVector.Length)),
            [new(typeof(DVector), "max")] = typeof(DVector).Get(nameof(DVector.Maximum)),
            [new(typeof(DVector), "mean")] = typeof(DVector).Get(nameof(DVector.Mean)),
            [new(typeof(DVector), "min")] = typeof(DVector).Get(nameof(DVector.Minimum)),
            [new(typeof(DVector), "norm")] = typeof(DVector).Get(nameof(DVector.Norm)),
            [new(typeof(DVector), "pacf")] = typeof(DVector).Get(nameof(DVector.PACF)),
            [new(typeof(DVector), "plot")] = typeof(DVector).Get(nameof(DVector.Plot)),
            [new(typeof(DVector), "prod")] = typeof(DVector).Get(nameof(DVector.Product)),
            [new(typeof(DVector), "product")] = typeof(DVector).Get(nameof(DVector.Product)),
            [new(typeof(DVector), "reverse")] = typeof(DVector).Get(nameof(DVector.Reverse)),
            [new(typeof(DVector), "sort")] = typeof(DVector).Get(nameof(DVector.Sort)),
            [new(typeof(DVector), "sortdesc")] = typeof(DVector).Get(nameof(DVector.SortDescending)),
            [new(typeof(DVector), "sortdescending")] = typeof(DVector).Get(nameof(DVector.SortDescending)),
            [new(typeof(DVector), "sqr")] = typeof(DVector).Get(nameof(DVector.Squared)),
            [new(typeof(DVector), "sqrt")] = typeof(DVector).Get(nameof(DVector.Sqrt)),
            [new(typeof(DVector), "stats")] = typeof(DVector).Get(nameof(DVector.Stats)),
            [new(typeof(DVector), "sum")] = typeof(DVector).Get(nameof(DVector.Sum)),

            [new(typeof(EVD), "d")] = typeof(EVD).Prop(nameof(EVD.D)),
            [new(typeof(EVD), "det")] = typeof(EVD).Get(nameof(EVD.Determinant)),
            [new(typeof(EVD), "rank")] = typeof(EVD).Get(nameof(EVD.Rank)),
            [new(typeof(EVD), "values")] = typeof(EVD).Prop(nameof(EVD.Values)),
            [new(typeof(EVD), "vectors")] = typeof(EVD).Prop(nameof(EVD.Vectors)),

            [new(typeof(FftCModel), "amplitudes")] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
            [new(typeof(FftCModel), "inverse")] = typeof(FftCModel).Get(nameof(FftCModel.Inverse)),
            [new(typeof(FftCModel), "length")] = typeof(FftModel).Prop(nameof(FftModel.Length)),
            [new(typeof(FftCModel), "magnitudes")] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
            [new(typeof(FftCModel), "phases")] = typeof(FftModel).Prop(nameof(FftModel.Phases)),
            [new(typeof(FftCModel), "values")] = typeof(FftModel).Prop(nameof(FftModel.Spectrum)),

            [new(typeof(FftRModel), "amplitudes")] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
            [new(typeof(FftRModel), "inverse")] = typeof(FftRModel).Get(nameof(FftRModel.Inverse)),
            [new(typeof(FftRModel), "length")] = typeof(FftModel).Prop(nameof(FftModel.Length)),
            [new(typeof(FftRModel), "magnitudes")] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
            [new(typeof(FftRModel), "phases")] = typeof(FftModel).Prop(nameof(FftModel.Phases)),
            [new(typeof(FftRModel), "values")] = typeof(FftModel).Prop(nameof(FftModel.Spectrum)),

            [new(typeof(LinearSModel), "original")] = typeof(LinearSModel).Prop(nameof(LinearSModel.Original)),
            [new(typeof(LinearSModel), "prediction")] = typeof(LinearSModel).Prop(nameof(LinearSModel.Prediction)),
            [new(typeof(LinearSModel), "r2")] = typeof(LinearSModel).Prop(nameof(LinearSModel.R2)),
            [new(typeof(LinearSModel), "rss")] = typeof(LinearSModel).Prop(nameof(LinearSModel.ResidualSumSquares)),
            [new(typeof(LinearSModel), "tss")] = typeof(LinearSModel).Prop(nameof(LinearSModel.TotalSumSquares)),
            [new(typeof(LinearSModel), "weights")] = typeof(LinearSModel).Prop(nameof(LinearSModel.Weights)),

            [new(typeof(LinearVModel), "original")] = typeof(LinearVModel).Prop(nameof(LinearVModel.Original)),
            [new(typeof(LinearVModel), "prediction")] = typeof(LinearVModel).Prop(nameof(LinearVModel.Prediction)),
            [new(typeof(LinearVModel), "r2")] = typeof(LinearVModel).Prop(nameof(LinearVModel.R2)),
            [new(typeof(LinearVModel), "rss")] = typeof(LinearVModel).Prop(nameof(LinearVModel.ResidualSumSquares)),
            [new(typeof(LinearVModel), "tss")] = typeof(LinearVModel).Prop(nameof(LinearVModel.TotalSumSquares)),
            [new(typeof(LinearVModel), "weights")] = typeof(LinearVModel).Prop(nameof(LinearVModel.Weights)),

            [new(typeof(LMatrix), "amax")] = typeof(LMatrix).Get(nameof(LMatrix.AMax)),
            [new(typeof(LMatrix), "amin")] = typeof(LMatrix).Get(nameof(LMatrix.AMin)),
            [new(typeof(LMatrix), "cols")] = typeof(LMatrix).Prop(nameof(LMatrix.Cols)),
            [new(typeof(LMatrix), "det")] = typeof(LMatrix).Get(nameof(LMatrix.Determinant)),
            [new(typeof(LMatrix), "diag")] = typeof(LMatrix).Get(nameof(LMatrix.Diagonal)),
            [new(typeof(LMatrix), "inverse")] = typeof(LMatrix).Get(nameof(LMatrix.Inverse)),
            [new(typeof(LMatrix), "max")] = typeof(LMatrix).Get(nameof(LMatrix.Maximum)),
            [new(typeof(LMatrix), "min")] = typeof(LMatrix).Get(nameof(LMatrix.Minimum)),
            [new(typeof(LMatrix), "rows")] = typeof(LMatrix).Prop(nameof(LMatrix.Rows)),
            [new(typeof(LMatrix), "trace")] = typeof(LMatrix).Get(nameof(LMatrix.Trace)),
            [new(typeof(LMatrix), "stats")] = typeof(LMatrix).Get(nameof(LMatrix.Stats)),

            [new(typeof(LU), "det")] = typeof(LU).Get(nameof(LU.Determinant)),
            [new(typeof(LU), "l")] = typeof(LU).Prop(nameof(LU.L)),
            [new(typeof(LU), "lower")] = typeof(LU).Prop(nameof(LU.L)),
            [new(typeof(LU), "perm")] = typeof(LU).Prop(nameof(LU.Perm)),
            [new(typeof(LU), "size")] = typeof(LU).Prop(nameof(LU.Size)),
            [new(typeof(LU), "u")] = typeof(LU).Prop(nameof(LU.U)),
            [new(typeof(LU), "upper")] = typeof(LU).Prop(nameof(LU.U)),

            [new(typeof(MASModel), "coeff")] = typeof(MASModel).Prop(nameof(MASModel.Coefficients)),
            [new(typeof(MASModel), "coefficients")] = typeof(MASModel).Prop(nameof(MASModel.Coefficients)),
            [new(typeof(MASModel), "mean")] = typeof(MASModel).Prop(nameof(MASModel.Mean)),
            [new(typeof(MASModel), "original")] = typeof(MASModel).Prop(nameof(MASModel.Original)),
            [new(typeof(MASModel), "prediction")] = typeof(MASModel).Prop(nameof(MASModel.Prediction)),
            [new(typeof(MASModel), "r2")] = typeof(MASModel).Prop(nameof(MASModel.R2)),
            [new(typeof(MASModel), "residuals")] = typeof(MASModel).Prop(nameof(MASModel.Residuals)),
            [new(typeof(MASModel), "rss")] = typeof(MASModel).Prop(nameof(MASModel.ResidualSumSquares)),
            [new(typeof(MASModel), "tss")] = typeof(MASModel).Prop(nameof(MASModel.TotalSumSquares)),

            [new(typeof(Matrix), "amax")] = typeof(Matrix).Get(nameof(Matrix.AMax)),
            [new(typeof(Matrix), "amin")] = typeof(Matrix).Get(nameof(Matrix.AMin)),
            [new(typeof(Matrix), "chol")] = typeof(Matrix).Get(nameof(Matrix.CholeskyMatrix)),
            [new(typeof(Matrix), "cholesky")] = typeof(Matrix).Get(nameof(Matrix.Cholesky)),
            [new(typeof(Matrix), "cols")] = typeof(Matrix).Prop(nameof(Matrix.Cols)),
            [new(typeof(Matrix), "det")] = typeof(Matrix).Get(nameof(Matrix.Determinant)),
            [new(typeof(Matrix), "diag")] = typeof(Matrix).Get(nameof(Matrix.Diagonal)),
            [new(typeof(Matrix), "evd")] = typeof(Matrix).GetMethod(nameof(Matrix.EVD), Type.EmptyTypes)!,
            [new(typeof(Matrix), "inverse")] = typeof(Matrix).Get(nameof(Matrix.Inverse)),
            [new(typeof(Matrix), "issym")] = typeof(Matrix).Get(nameof(Matrix.IsSymmetric)),
            [new(typeof(Matrix), "issymmetric")] = typeof(Matrix).Get(nameof(Matrix.IsSymmetric)),
            [new(typeof(Matrix), "lu")] = typeof(Matrix).Get(nameof(Matrix.LU)),
            [new(typeof(Matrix), "max")] = typeof(Matrix).Get(nameof(Matrix.Maximum)),
            [new(typeof(Matrix), "min")] = typeof(Matrix).Get(nameof(Matrix.Minimum)),
            [new(typeof(Matrix), "rows")] = typeof(Matrix).Prop(nameof(Matrix.Rows)),
            [new(typeof(Matrix), "trace")] = typeof(Matrix).Get(nameof(Matrix.Trace)),
            [new(typeof(Matrix), "stats")] = typeof(Matrix).Get(nameof(Matrix.Stats)),
            [new(typeof(Matrix), "sym")] = typeof(Matrix).Get(nameof(Matrix.IsSymmetric)),

            [new(typeof(MAVModel), "coeff")] = typeof(MAVModel).Prop(nameof(MAVModel.Coefficients)),
            [new(typeof(MAVModel), "coefficients")] = typeof(MAVModel).Prop(nameof(MAVModel.Coefficients)),
            [new(typeof(MAVModel), "mean")] = typeof(MAVModel).Prop(nameof(MAVModel.Mean)),
            [new(typeof(MAVModel), "original")] = typeof(MAVModel).Prop(nameof(MAVModel.Original)),
            [new(typeof(MAVModel), "prediction")] = typeof(MAVModel).Prop(nameof(MAVModel.Prediction)),
            [new(typeof(MAVModel), "r2")] = typeof(MAVModel).Prop(nameof(MAVModel.R2)),
            [new(typeof(MAVModel), "residuals")] = typeof(MAVModel).Prop(nameof(MAVModel.Residuals)),
            [new(typeof(MAVModel), "rss")] = typeof(MAVModel).Prop(nameof(MAVModel.ResidualSumSquares)),
            [new(typeof(MAVModel), "tss")] = typeof(MAVModel).Prop(nameof(MAVModel.TotalSumSquares)),

            [new(typeof(MvoModel), "first")] = typeof(MvoModel).Prop(nameof(MvoModel.First)),
            [new(typeof(MvoModel), "last")] = typeof(MvoModel).Prop(nameof(MvoModel.Last)),
            [new(typeof(MvoModel), "length")] = typeof(MvoModel).Prop(nameof(MvoModel.Length)),
            [new(typeof(MvoModel), "size")] = typeof(MvoModel).Prop(nameof(MvoModel.Size)),

            [new(typeof(NSequence), "distinct")] = typeof(NSequence).Get(nameof(NSequence.Distinct)),
            [new(typeof(NSequence), "first")] = typeof(NSequence).Get(nameof(NSequence.First)),
            [new(typeof(NSequence), "last")] = typeof(NSequence).Get(nameof(NSequence.Last)),
            [new(typeof(NSequence), "length")] = typeof(NSequence).Get(nameof(NSequence.Length)),
            [new(typeof(NSequence), "max")] = typeof(NSequence).Get(nameof(NSequence.Max)),
            [new(typeof(NSequence), "min")] = typeof(NSequence).Get(nameof(NSequence.Min)),
            [new(typeof(NSequence), "plot")] = typeof(NSequence).Get(nameof(NSequence.Plot)),
            [new(typeof(NSequence), "prod")] = typeof(NSequence).Get(nameof(NSequence.Product)),
            [new(typeof(NSequence), "product")] = typeof(NSequence).Get(nameof(NSequence.Product)),
            [new(typeof(NSequence), "sort")] = typeof(NSequence).Get(nameof(NSequence.Sort)),
            [new(typeof(NSequence), "sortasc")] = typeof(NSequence).Get(nameof(NSequence.Sort)),
            [new(typeof(NSequence), "sortdesc")] = typeof(NSequence).Get(nameof(NSequence.SortDescending)),
            [new(typeof(NSequence), "stats")] = typeof(NSequence).Get(nameof(NSequence.Stats)),
            [new(typeof(NSequence), "sum")] = typeof(NSequence).Get(nameof(NSequence.Sum)),
            [new(typeof(NSequence), "tovector")] = typeof(NSequence).Get(nameof(NSequence.ToVector)),

            [new(typeof(NVector), "abs")] = typeof(NVector).Get(nameof(NVector.Abs)),
            [new(typeof(NVector), "distinct")] = typeof(NVector).Get(nameof(NVector.Distinct)),
            [new(typeof(NVector), "first")] = typeof(NVector).Prop(nameof(NVector.First)),
            [new(typeof(NVector), "last")] = typeof(NVector).Prop(nameof(NVector.Last)),
            [new(typeof(NVector), "length")] = typeof(NVector).Prop(nameof(NVector.Length)),
            [new(typeof(NVector), "max")] = typeof(NVector).Get(nameof(NVector.Maximum)),
            [new(typeof(NVector), "min")] = typeof(NVector).Get(nameof(NVector.Minimum)),
            [new(typeof(NVector), "prod")] = typeof(NVector).Get(nameof(NVector.Product)),
            [new(typeof(NVector), "product")] = typeof(NVector).Get(nameof(NVector.Product)),
            [new(typeof(NVector), "reverse")] = typeof(NVector).Get(nameof(NVector.Reverse)),
            [new(typeof(NVector), "sort")] = typeof(NVector).Get(nameof(NVector.Sort)),
            [new(typeof(NVector), "sortdesc")] = typeof(NVector).Get(nameof(NVector.SortDescending)),
            [new(typeof(NVector), "sortdescending")] = typeof(NVector).Get(nameof(NVector.SortDescending)),
            [new(typeof(NVector), "stats")] = typeof(NVector).Get(nameof(NVector.Stats)),
            [new(typeof(NVector), "sum")] = typeof(NVector).Get(nameof(NVector.Sum)),
            [new(typeof(NVector), "tovector")] = typeof(NVector).Get(nameof(NVector.ToVector)),

            [new(typeof(Polynomial), "area")] = typeof(Polynomial).Prop(nameof(Polynomial.Area)),

            [new(typeof(Point<Date>), "date")] = typeof(Point<Date>).Prop(nameof(Point<Date>.Arg)),
            [new(typeof(Point<Date>), "value")] = typeof(Point<Date>).Prop(nameof(Point<Date>.Value)),

            [new(typeof(Portfolio), "lambda")] = typeof(Portfolio).Prop(nameof(Portfolio.Lambda)),
            [new(typeof(Portfolio), "ret")] = typeof(Portfolio).Prop(nameof(Portfolio.Mean)),
            [new(typeof(Portfolio), "std")] = typeof(Portfolio).Prop(nameof(Portfolio.StdDev)),
            [new(typeof(Portfolio), "var")] = typeof(Portfolio).Prop(nameof(Portfolio.Variance)),
            [new(typeof(Portfolio), "weights")] = typeof(Portfolio).Prop(nameof(Portfolio.Weights)),

            [new(typeof(RMatrix), "amax")] = typeof(RMatrix).Get(nameof(RMatrix.AMax)),
            [new(typeof(RMatrix), "amin")] = typeof(RMatrix).Get(nameof(RMatrix.AMin)),
            [new(typeof(RMatrix), "cols")] = typeof(RMatrix).Prop(nameof(RMatrix.Cols)),
            [new(typeof(RMatrix), "det")] = typeof(RMatrix).Get(nameof(RMatrix.Determinant)),
            [new(typeof(RMatrix), "diag")] = typeof(RMatrix).Get(nameof(RMatrix.Diagonal)),
            [new(typeof(RMatrix), "max")] = typeof(RMatrix).Get(nameof(RMatrix.Maximum)),
            [new(typeof(RMatrix), "min")] = typeof(RMatrix).Get(nameof(RMatrix.Minimum)),
            [new(typeof(RMatrix), "rows")] = typeof(RMatrix).Prop(nameof(RMatrix.Rows)),
            [new(typeof(RMatrix), "stats")] = typeof(RMatrix).Get(nameof(RMatrix.Stats)),
            [new(typeof(RMatrix), "trace")] = typeof(RMatrix).Get(nameof(RMatrix.Trace)),

            [new(typeof(Series), "acf")] = typeof(Series).Get(nameof(Series.ACF)),
            [new(typeof(Series), "amax")] = typeof(Series).Get(nameof(Series.AbsMax)),
            [new(typeof(Series), "amin")] = typeof(Series).Get(nameof(Series.AbsMin)),
            [new(typeof(Series), "count")] = typeof(Series).Prop(nameof(Series.Count)),
            [new(typeof(Series), "fft")] = typeof(Series).Get(nameof(Series.Fft)),
            [new(typeof(Series), "first")] = typeof(Series).Prop(nameof(Series.First)),
            [new(typeof(Series), "fit")] = typeof(Series).Get(nameof(Series.Fit)),
            [new(typeof(Series), "kurt")] = typeof(Series).Prop(nameof(Series.Kurtosis)),
            [new(typeof(Series), "kurtp")] = typeof(Series).Prop(nameof(Series.PopulationKurtosis)),
            [new(typeof(Series), "last")] = typeof(Series).Prop(nameof(Series.Last)),
            [new(typeof(Series), "length")] = typeof(Series).Prop(nameof(Series.Count)),
            [new(typeof(Series), "linearfit")] = typeof(Series).Get(nameof(Series.LinearFit)),
            [new(typeof(Series), "logs")] = typeof(Series).Get(nameof(Series.AsLogReturns)),
            [new(typeof(Series), "max")] = typeof(Series).Prop(nameof(Series.Maximum)),
            [new(typeof(Series), "mean")] = typeof(Series).Prop(nameof(Series.Mean)),
            [new(typeof(Series), "min")] = typeof(Series).Prop(nameof(Series.Minimum)),
            [new(typeof(Series), "movingret")] = typeof(Series).Get(nameof(Series.MovingRet)),
            [new(typeof(Series), "ncdf")] = typeof(Series).GetMethod(nameof(Series.NCdf), Type.EmptyTypes)!,
            [new(typeof(Series), "pacf")] = typeof(Series).Get(nameof(Series.PACF)),
            [new(typeof(Series), "perc")] = typeof(Series).Get(nameof(Series.Percentiles)),
            [new(typeof(Series), "random")] = typeof(Series).Get(nameof(Series.Random)),
            [new(typeof(Series), "rets")] = typeof(Series).Get(nameof(Series.AsReturns)),
            [new(typeof(Series), "skew")] = typeof(Series).Prop(nameof(Series.Skewness)),
            [new(typeof(Series), "skewp")] = typeof(Series).Prop(nameof(Series.PopulationSkewness)),
            [new(typeof(Series), "stats")] = typeof(Series).Prop(nameof(Series.Stats)),
            [new(typeof(Series), "std")] = typeof(Series).Prop(nameof(Series.StandardDeviation)),
            [new(typeof(Series), "stdp")] = typeof(Series).Prop(nameof(Series.PopulationStandardDeviation)),
            [new(typeof(Series), "sum")] = typeof(Series).Get(nameof(Series.Sum)),
            [new(typeof(Series), "type")] = typeof(Series).Prop(nameof(Series.Type)),
            [new(typeof(Series), "values")] = typeof(Series).Prop(nameof(Series.Values)),
            [new(typeof(Series), "var")] = typeof(Series).Prop(nameof(Series.Variance)),
            [new(typeof(Series), "varp")] = typeof(Series).Prop(nameof(Series.PopulationVariance)),

            [new(typeof(Series<double>), "stats")] = typeof(Series<double>).Prop(nameof(Series<double>.Stats)),
            [new(typeof(Series<double>), "first")] = typeof(Series<double>).Prop(nameof(Series<double>.First)),
            [new(typeof(Series<double>), "last")] = typeof(Series<double>).Prop(nameof(Series<double>.Last)),
            [new(typeof(Series<double>), "values")] = typeof(Series<double>).Prop(nameof(Series<double>.Values)),
            [new(typeof(Series<double>), "sum")] = typeof(Series<double>).Get(nameof(Series<double>.Sum)),

            [new(typeof(Series<int>), "stats")] = typeof(Series<int>).Prop(nameof(Series<int>.Stats)),
            [new(typeof(Series<int>), "first")] = typeof(Series<int>).Prop(nameof(Series<int>.First)),
            [new(typeof(Series<int>), "last")] = typeof(Series<int>).Prop(nameof(Series<int>.Last)),
            [new(typeof(Series<int>), "values")] = typeof(Series<int>).Prop(nameof(Series<int>.Values)),
            [new(typeof(Series<int>), "sum")] = typeof(Series<int>).Get(nameof(Series<int>.Sum)),

            [new(typeof(SimplexModel), "objective")] = typeof(SimplexModel).Prop(nameof(SimplexModel.Objective)),
            [new(typeof(SimplexModel), "value")] = typeof(SimplexModel).Prop(nameof(SimplexModel.Value)),
            [new(typeof(SimplexModel), "weights")] = typeof(SimplexModel).Prop(nameof(SimplexModel.Weights)),

            [new(typeof(VectorSpline), "area")] = typeof(VectorSpline).Prop(nameof(VectorSpline.Area)),
            [new(typeof(VectorSpline), "first")] = typeof(VectorSpline).Prop(nameof(VectorSpline.First)),
            [new(typeof(VectorSpline), "last")] = typeof(VectorSpline).Prop(nameof(VectorSpline.Last)),
            [new(typeof(VectorSpline), "length")] = typeof(VectorSpline).Prop(nameof(VectorSpline.Length)),
        }.ToFrozenDictionary();

    /// <summary>Allowed instance methods.</summary>
    private readonly FrozenDictionary<TypeId, MethodInfo> methods =
        new Dictionary<TypeId, MethodInfo>()
        {
            [new(typeof(CSequence), "all")] = typeof(CSequence).Get(nameof(CSequence.All)),
            [new(typeof(CSequence), "any")] = typeof(CSequence).Get(nameof(CSequence.Any)),
            [new(typeof(CSequence), "filter")] = typeof(CSequence).Get(nameof(CSequence.Filter)),
            [new(typeof(CSequence), "map")] = typeof(CSequence).Get(nameof(CSequence.Map)),
            [new(typeof(CSequence), "mapr")] = typeof(CSequence).Get(nameof(CSequence.MapReal)),
            [new(typeof(CSequence), "mapreal")] = typeof(CSequence).Get(nameof(CSequence.MapReal)),
            [new(typeof(CSequence), "reduce")] = typeof(CSequence).Get(nameof(CSequence.Reduce)),
            [new(typeof(CSequence), "while")] = typeof(CSequence).Get(nameof(CSequence.While)),
            [new(typeof(CSequence), "zip")] = typeof(CSequence).Get(nameof(CSequence.Zip)),

            [new(typeof(CVector), "all")] = typeof(CVector).Get(nameof(CVector.All)),
            [new(typeof(CVector), "any")] = typeof(CVector).Get(nameof(CVector.Any)),
            [new(typeof(CVector), "filter")] = typeof(CVector).Get(nameof(CVector.Filter)),
            [new(typeof(CVector), "map")] = typeof(CVector).Get(nameof(CVector.Map)),
            [new(typeof(CVector), "mapr")] = typeof(CVector).Get(nameof(CVector.MapReal)),
            [new(typeof(CVector), "mapreal")] = typeof(CVector).Get(nameof(CVector.MapReal)),
            [new(typeof(CVector), "reduce")] = typeof(CVector).Get(nameof(CVector.Reduce)),
            [new(typeof(CVector), "zip")] = typeof(CVector).Get(nameof(CVector.Zip)),

            [new(typeof(Date), "addmonths")] = typeof(Date).GetMethod(nameof(Date.AddMonths), NArg)!,
            [new(typeof(Date), "addyears")] = typeof(Date).Get(nameof(Date.AddYears)),

            [new(typeof(DateSpline), "poly")] = typeof(DateSpline).Get(nameof(DateSpline.GetPoly)),
            [new(typeof(DateSpline), "derivative")] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),
            [new(typeof(DateSpline), "deriv")] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),
            [new(typeof(DateSpline), "der")] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),

            [new(typeof(DSequence), "all")] = typeof(DSequence).Get(nameof(DSequence.All)),
            [new(typeof(DSequence), "any")] = typeof(DSequence).Get(nameof(DSequence.Any)),
            [new(typeof(DSequence), "ar")] = typeof(DSequence).Get(nameof(DSequence.AutoRegression)),
            [new(typeof(DSequence), "armodel")] = typeof(DSequence).Get(nameof(DSequence.ARModel)),
            [new(typeof(DSequence), "filter")] = typeof(DSequence).Get(nameof(DSequence.Filter)),
            [new(typeof(DSequence), "ma")] = typeof(DSequence).Get(nameof(DSequence.MovingAverage)),
            [new(typeof(DSequence), "mamodel")] = typeof(DSequence).Get(nameof(DSequence.MAModel)),
            [new(typeof(DSequence), "map")] = typeof(DSequence).Get(nameof(DSequence.Map)),
            [new(typeof(DSequence), "reduce")] = typeof(DSequence).Get(nameof(DSequence.Reduce)),
            [new(typeof(DSequence), "while")] = typeof(DSequence).Get(nameof(DSequence.While)),
            [new(typeof(DSequence), "zip")] = typeof(DSequence).Get(nameof(DSequence.Zip)),

            [new(typeof(DVector), "all")] = typeof(DVector).Get(nameof(DVector.All)),
            [new(typeof(DVector), "any")] = typeof(DVector).Get(nameof(DVector.Any)),
            [new(typeof(DVector), "ar")] = typeof(DVector).Get(nameof(DVector.AutoRegression)),
            [new(typeof(DVector), "armodel")] = typeof(DVector).Get(nameof(DVector.ARModel)),
            [new(typeof(DVector), "autocorr")] = typeof(DVector).Get(nameof(DVector.AutoCorrelation)),
            [new(typeof(DVector), "correlogram")] = typeof(DVector).Get(nameof(DVector.Correlogram)),
            [new(typeof(DVector), "filter")] = typeof(DVector).Get(nameof(DVector.Filter)),
            [new(typeof(DVector), "linear")] = typeof(DVector).Get(nameof(DVector.LinearModel)),
            [new(typeof(DVector), "linearmodel")] = typeof(DVector).Get(nameof(DVector.FullLinearModel)),
            [new(typeof(DVector), "ma")] = typeof(DVector).Get(nameof(DVector.MovingAverage)),
            [new(typeof(DVector), "mamodel")] = typeof(DVector).Get(nameof(DVector.MAModel)),
            [new(typeof(DVector), "map")] = typeof(DVector).Get(nameof(DVector.Map)),
            [new(typeof(DVector), "reduce")] = typeof(DVector).Get(nameof(DVector.Reduce)),
            [new(typeof(DVector), "zip")] = typeof(DVector).Get(nameof(DVector.Zip)),

            [new(typeof(Matrix), "all")] = typeof(Matrix).Get(nameof(Matrix.All)),
            [new(typeof(Matrix), "any")] = typeof(Matrix).Get(nameof(Matrix.Any)),
            [new(typeof(Matrix), "getcol")] = typeof(Matrix).GetMethod(nameof(Matrix.GetColumn), NArg)!,
            [new(typeof(Matrix), "getrow")] = typeof(Matrix).GetMethod(nameof(Matrix.GetRow), NArg)!,
            [new(typeof(Matrix), "map")] = typeof(Matrix).Get(nameof(Matrix.Map)),

            [new(typeof(NSequence), "all")] = typeof(NSequence).Get(nameof(NSequence.All)),
            [new(typeof(NSequence), "any")] = typeof(NSequence).Get(nameof(NSequence.Any)),
            [new(typeof(NSequence), "filter")] = typeof(NSequence).Get(nameof(NSequence.Filter)),
            [new(typeof(NSequence), "map")] = typeof(NSequence).Get(nameof(NSequence.Map)),
            [new(typeof(NSequence), "mapr")] = typeof(NSequence).Get(nameof(NSequence.MapReal)),
            [new(typeof(NSequence), "mapreal")] = typeof(NSequence).Get(nameof(NSequence.MapReal)),
            [new(typeof(NSequence), "reduce")] = typeof(NSequence).Get(nameof(NSequence.Reduce)),
            [new(typeof(NSequence), "while")] = typeof(NSequence).Get(nameof(NSequence.While)),
            [new(typeof(NSequence), "zip")] = typeof(NSequence).Get(nameof(NSequence.Zip)),

            [new(typeof(NVector), "all")] = typeof(NVector).Get(nameof(NVector.All)),
            [new(typeof(NVector), "any")] = typeof(NVector).Get(nameof(NVector.Any)),
            [new(typeof(NVector), "filter")] = typeof(NVector).Get(nameof(NVector.Filter)),
            [new(typeof(NVector), "map")] = typeof(NVector).Get(nameof(NVector.Map)),
            [new(typeof(NVector), "mapr")] = typeof(NVector).Get(nameof(NVector.MapReal)),
            [new(typeof(NVector), "mapreal")] = typeof(NVector).Get(nameof(NVector.MapReal)),
            [new(typeof(NVector), "reduce")] = typeof(NVector).Get(nameof(NVector.Reduce)),
            [new(typeof(NVector), "zip")] = typeof(NVector).Get(nameof(NVector.Zip)),

            [new(typeof(Polynomial), "eval")] = typeof(Polynomial).Get(nameof(Polynomial.Eval)),
            [new(typeof(Polynomial), "derivative")] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),
            [new(typeof(Polynomial), "deriv")] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),
            [new(typeof(Polynomial), "der")] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),

            [new(typeof(Series), "all")] = typeof(Series).Get(nameof(Series.All)),
            [new(typeof(Series), "any")] = typeof(Series).Get(nameof(Series.Any)),
            [new(typeof(Series), "ar")] = typeof(Series).Get(nameof(Series.AutoRegression)),
            [new(typeof(Series), "armodel")] = typeof(Series).Get(nameof(Series.ARModel)),
            [new(typeof(Series), "autocorr")] = typeof(Series).Get(nameof(Series.AutoCorrelation)),
            [new(typeof(Series), "corr")] = typeof(Series).Get(nameof(Series.Correlation)),
            [new(typeof(Series), "correlogram")] = typeof(Series).Get(nameof(Series.Correlogram)),
            [new(typeof(Series), "cov")] = typeof(Series).Get(nameof(Series.Covariance)),
            [new(typeof(Series), "ewma")] = typeof(Series).Get(nameof(Series.EWMA)),
            [new(typeof(Series), "indexof")] = typeof(Series).GetMethod(nameof(Series.IndexOf), DArg)!,
            [new(typeof(Series), "filter")] = typeof(Series).Get(nameof(Series.Filter)),
            [new(typeof(Series), "linear")] = typeof(Series).Get(nameof(Series.LinearModel)),
            [new(typeof(Series), "linearmodel")] = typeof(Series).Get(nameof(Series.FullLinearModel)),
            [new(typeof(Series), "ma")] = typeof(Series).Get(nameof(Series.MovingAverage)),
            [new(typeof(Series), "mamodel")] = typeof(Series).Get(nameof(Series.MAModel)),
            [new(typeof(Series), "map")] = typeof(Series).Get(nameof(Series.Map)),
            [new(typeof(Series), "movingavg")] = typeof(Series).Get(nameof(Series.MovingAvg)),
            [new(typeof(Series), "movingncdf")] = typeof(Series).Get(nameof(Series.MovingNcdf)),
            [new(typeof(Series), "movingstd")] = typeof(Series).GetMethod(nameof(Series.MovingStd), NArg)!,
            [new(typeof(Series), "ncdf")] = typeof(Series).GetMethod(nameof(Series.NCdf), DArg)!,
            [new(typeof(Series), "stats")] = typeof(Series).GetMethod(nameof(Series.GetSliceStats), [typeof(Date)])!,
            [new(typeof(Series), "zip")] = typeof(Series).Get(nameof(Series.Zip)),

            [new(typeof(VectorSpline), "poly")] = typeof(VectorSpline).Get(nameof(VectorSpline.GetPoly)),
            [new(typeof(VectorSpline), "derivative")] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),
            [new(typeof(VectorSpline), "deriv")] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),
            [new(typeof(VectorSpline), "der")] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),
        }.ToFrozenDictionary();

    /// <summary>Overloaded instance methods.</summary>
    private readonly FrozenDictionary<TypeId, MethodList> methodOverloads =
        new Dictionary<TypeId, MethodList>()
        {
            [new(typeof(Cholesky), "solve")] = new(
                typeof(Cholesky).MD(nameof(Cholesky.Solve), typeof(DVector)),
                typeof(Cholesky).MD(nameof(Cholesky.Solve), typeof(Matrix))),
            [new(typeof(CSequence), "until")] = new(
                typeof(CSequence).MD(nameof(CSequence.Until), typeof(Func<Complex, bool>)),
                typeof(CSequence).MD(nameof(CSequence.Until), CArg)),
            [new(typeof(CVector), "find")] = new(
                typeof(CVector).MD(nameof(CVector.Find), CArg),
                typeof(CVector).MD(nameof(CVector.Find), typeof(Func<Complex, bool>))),
            [new(typeof(CVector), "indexof")] = new(
                typeof(CVector).MD(nameof(CVector.IndexOf), CArg),
                typeof(CVector).MD(nameof(CVector.IndexOf), typeof(Complex), typeof(int))),
            [new(typeof(DSequence), "until")] = new(
                typeof(DSequence).MD(nameof(DSequence.Until), typeof(Func<double, bool>)),
                typeof(DSequence).MD(nameof(DSequence.Until), DArg)),
            [new(typeof(DVector), "find")] = new(
                typeof(DVector).MD(nameof(DVector.Find), DArg),
                typeof(DVector).MD(nameof(DVector.Find), typeof(Func<double, bool>))),
            [new(typeof(DVector), "indexof")] = new(
                typeof(DVector).MD(nameof(DVector.IndexOf), DArg),
                typeof(DVector).MD(nameof(DVector.IndexOf), typeof(double), typeof(int))),
            [new(typeof(LMatrix), "redim")] = new(
                typeof(LMatrix).MD(nameof(LMatrix.Redim), NArg),
                typeof(LMatrix).MD(nameof(LMatrix.Redim), NNArg)),
            [new(typeof(LU), "solve")] = new(
                typeof(LU).MD(nameof(LU.Solve), typeof(DVector)),
                typeof(LU).MD(nameof(LU.Solve), typeof(Matrix))),
            [new(typeof(Matrix), "redim")] = new(
                typeof(Matrix).MD(nameof(Matrix.Redim), NArg),
                typeof(Matrix).MD(nameof(Matrix.Redim), NNArg)),
            [new(typeof(MvoModel), "setconstraints")] = new(
                typeof(MvoModel).MD(nameof(MvoModel.SetConstraints),
                    typeof(Matrix), typeof(DVector), typeof(NVector)),
                typeof(MvoModel).MD(nameof(MvoModel.SetConstraints),
                    typeof(Matrix), typeof(DVector), typeof(int)),
                typeof(MvoModel).MD(nameof(MvoModel.SetConstraints),
                    typeof(Matrix), typeof(DVector))),
            [new(typeof(NSequence), "until")] = new(
                typeof(NSequence).MD(nameof(NSequence.Until), typeof(Func<int, bool>)),
                typeof(NSequence).MD(nameof(NSequence.Until), NArg)),
            [new(typeof(NVector), "indexof")] = new(
                typeof(NVector).MD(nameof(NVector.IndexOf), NArg),
                typeof(NVector).MD(nameof(NVector.IndexOf), NNArg)),
            [new(typeof(RMatrix), "redim")] = new(
                typeof(RMatrix).MD(nameof(RMatrix.Redim), NArg),
                typeof(RMatrix).MD(nameof(RMatrix.Redim), NNArg)),
        }.ToFrozenDictionary();

    private readonly FrozenSet<string> optimizableCalls =
        new HashSet<string>
        {
            nameof(DVector.InplaceAdd), nameof(DVector.InplaceSub),
            nameof(DVector.MultiplyAdd), nameof(DVector.MultiplySubtract),
            nameof(DVector.SubtractMultiply),
            nameof(DVector.Combine2), nameof(DVector.Combine)
        }.ToFrozenSet();

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

    public bool IsOptimizableCall(string identifier) => optimizableCalls.Contains(identifier);

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="source">A data source.</param>
    /// <param name="text">An expression fragment.</param>
    /// <param name="type">The type of the expression fragment.</param>
    /// <returns>A list of pairs member name/description.</returns>
    public IList<Member> GetMembers(IDataSource source, string text, out Type? type)
    {
        // Creating a scanner is a light task: we can afford it as many times as required.
        Scanner scanner = new(text);
        // Divide the whole text according to semicolons.
        List<int> semicolons = new(8);
        int lastIn = -1;
        for (; scanner.Kind is not Token.Error and not Token.Eof; scanner.Move())
        {
            if (scanner.Kind is Token.Semicolon)
                semicolons.Add(scanner.Start);
            else if (scanner.Kind is Token.In)
                lastIn = scanner.Start;
        }
        // Save fragments that are set statements or script-scoped variables.
        StringBuilder newText = new(text.Length);
        for (int i = 0; i < semicolons.Count; i++)
        {
            int from = i == 0 ? 0 : semicolons[i - 1] + 1;
            scanner.LexCursor = from;
            scanner.Move();
            if (scanner.Kind == Token.Let)
            {
                // Only script-scoped variables are allowed to survive.
                while (scanner.Kind != Token.Semicolon && scanner.Kind != Token.In)
                    scanner.Move();
                if (scanner.Kind == Token.In)
                    continue;
                newText.Append(text[from..(semicolons[i] + 1)]);
            }
            else if (scanner.Kind == Token.Set)
            {
                // Set statements are allowed to survive.
                newText.Append(text[from..(semicolons[i] + 1)]);
            }
        }
        // Now, we have to deal with the last fragment.
        scanner.LexCursor = semicolons.Count == 0 ? 0 : semicolons[^1] + 1;
        text = text[scanner.LexCursor..];
        scanner.Move();
        if (scanner.Kind is Token.Let or Token.Set)
            if (lastIn > scanner.Start)
            {
                newText.Append(text[..(lastIn + 2)]).Append(' ');
                text = text[(lastIn + 2)..];
            }
            else
            {
                // Locate the last sequence ","-identifier-"=".
                // We will use a four-state automaton to do that.
                int state = 0, saveUpTo = -1, confirmedSaveUpTo = -1;
                scanner = new(text);
                for (; scanner.Kind is not Token.Eof and not Token.Error; scanner.Move())
                {
                    if (scanner.Kind == Token.Comma)
                        saveUpTo = scanner.Start;
                    switch (state)
                    {
                        case 0:
                            if (scanner.Kind == Token.Comma)
                                state = 1;
                            break;
                        case 1:
                            if (scanner.Kind == Token.Id)
                                state = 2;
                            else if (scanner.Kind != Token.Comma)
                                state = 0;
                            break;
                        case 2:
                            if (scanner.Kind == Token.Comma)
                                state = 1;
                            else
                            {
                                if (scanner.Kind == Token.Eq)
                                    confirmedSaveUpTo = saveUpTo;
                                state = 0;
                            }
                            break;
                    }
                }
                if (confirmedSaveUpTo >= 0)
                {
                    newText.Append(text[..confirmedSaveUpTo]).Append(';');
                    text = text[(confirmedSaveUpTo + 1)..];
                }
            }

        ReadOnlySpan<char> trimmedText = newText.Length == 0
            ? ExtractObjectPath(text)
            : (newText.ToString() + ExtractObjectPath(text).ToString());
        if (!trimmedText.IsEmpty)
            try
            {
                return ExtractType(trimmedText.ToString(), out type);
            }
            catch
            {
                if (text.Contains("=>"))
                {
                    string id = trimmedText.ToString();
                    ParameterExpression? pe = new Parser(this, source, text)
                        .ParseLambdaContext(text.Length)
                        .FirstOrDefault(p => id.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
                    if (pe is not null && members.TryGetValue(pe.Type, out Member[]? list))
                    {
                        type = pe.Type;
                        return list;
                    }
                }
            }
        type = null;
        return [];

        // Finds the type of an object path.
        IList<Member> ExtractType(string text, out Type? type)
        {
            using Parser parser = new(this, source, text);
            // The abort position of the parser is set to the end of the text, so that
            // any error results in an AbortException instead of the regular AstException.
            if (parser.ParseType(text.Length + 1) is Type[] types
                && types.Length > 0 && types[^1] is not null)
            {
                type = types[^1];
                return members.TryGetValue(types[^1], out Member[]? list) ? list : [];
            }
            else
            {
                type = null;
                return [];
            }
        }

        // Extracts an object path from an expression fragment.
        static ReadOnlySpan<char> ExtractObjectPath(string text)
        {
            int i = text.Length - 1;
            for (ref char c = ref Unsafe.As<Str>(text).FirstChar; i >= 0; i--)
            {
                char ch = Unsafe.Add(ref c, i);
                if (ch is '(' or '[')
                    return text.AsSpan()[(i + 1)..];
                else if (ch == ')')
                {
                    int count = 1;
                    while (--i >= 0)
                        if ((ch = Unsafe.Add(ref c, i)) == ')')
                            count++;
                        else if (ch == '(' && --count == 0)
                            break;
                    if (count > 0)
                        return [];
                }
                else if (ch == ']')
                {
                    int count = 1;
                    while (--i >= 0)
                        if ((ch = Unsafe.Add(ref c, i)) == ']')
                            count++;
                        else if (ch == '[' && --count == 0)
                            break;
                    if (count > 0)
                        return [];
                }
                else if (!char.IsLetterOrDigit(ch)
                    && ch is not '_' and not '.' and not ':' and not '\''
                    && !char.IsWhiteSpace(ch))
                    break;
            }
            return text.AsSpan()[(i + 1)..].Trim();
        }
    }

    /// <summary>Gets a list of class members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>A list of pairs member name/description.</returns>
    public IList<Member> GetClassMembers(string text)
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

    /// <summary>Checks if there is parameter information for a given method.</summary>
    /// <param name="text">Text up to the method call.</param>
    /// <returns>The list of method overload signatures.</returns>
    public IReadOnlyList<string> GetParamInfo(string text)
    {
        // Extract a class method call.
        int i = text.Length - 1;
        for (ref char c = ref Unsafe.As<Str>(text).FirstChar; i >= 0; i--)
        {
            char ch = Unsafe.Add(ref c, i);
            if (!char.IsLetterOrDigit(ch) && ch is not '_' and not ':'
                && !char.IsWhiteSpace(ch))
                break;
        }
        string method = text[(i + 1)..].Trim();
        if (method.Contains("::"))
            text = method.Replace("::", ".");
        else if (IsClassName(method))
            text = method + ".new";
        else
            return emptyParameters;
        if (classMethods.TryGetValue(text, out MethodList list)
            && list.Methods != null)
        {
            List<string> result = new(list.Methods.Length);
            foreach (MethodData m in list.Methods)
                result.Add(method + m.DescribeArguments());
            return result.AsReadOnly();
        }
        return emptyParameters;
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

    /// <summary>Gets a list of instance method overloads for a given type and identifier.</summary>
    /// <param name="type">Implementing type.</param>
    /// <param name="identifier">Method name.</param>
    /// <param name="info">The method overload list, on success.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    public bool TryGetOverloads(Type type, string identifier, [MaybeNullWhen(false)] out MethodList info) =>
        methodOverloads.TryGetValue(new TypeId(type, identifier.ToLower()), out info);

    /// <summary>Gets an class method given the class and method names.</summary>
    /// <param name="identifier">Prefixed method name.</param>
    /// <param name="info">The method info, on success.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    public bool TryGetClassMethod(string identifier, out MethodList info) =>
        classMethods.TryGetValue(identifier, out info);

    /// <summary>Translate a type name to a <see cref="Type"/> object.</summary>
    /// <param name="identifier">The name of the type.</param>
    /// <param name="type">The corresponding type, when successful.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    public bool TryGetTypeName(string identifier, [MaybeNullWhen(false)] out Type type) =>
        typeNames.TryGetValue(identifier, out type);

    /// <summary>Checks if a class method exists.</summary>
    /// <param name="identifier">Prefixed method name.</param>
    /// <returns><see langword="true"/> if successful.</returns>
    public bool ContainsClassMethod(string identifier) =>
        classMethods.ContainsKey(identifier);

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

    public bool IsMatch(Type inputType, Type returnType) =>
        Args.Length == 1
        && (Args[0] == inputType || Args[0] == typeof(double) && inputType == typeof(int))
        && mInfo is MethodInfo m && m.ReturnType == returnType;

    public LambdaExpression GetAsLambda(Type inputType)
    {
        ParameterExpression x = Expression.Parameter(inputType, "x");
        Expression arg = Args[0] == x.Type ? x : Expression.Convert(x, typeof(double));
        return Expression.Lambda(Expression.Call((MethodInfo)mInfo, arg), x);
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

    /// <summary>Creates an expression that calls an instance method on a target.</summary>
    /// <param name="instance">The target for the instance method.</param>
    /// <param name="actualArguments">Actual arguments.</param>
    /// <returns>A expression node for calling an instance method.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Expression GetExpression(Expression instance, List<Expression> actualArguments) =>
        Expression.Call(instance, (MethodInfo)mInfo, actualArguments);

    public string DescribeArguments()
    {
        ParameterInfo[] parameters = mInfo.GetParameters();
        StringBuilder sb = new(Args.Length * 16);
        sb.Append('(');
        for (int i = 0; i < Args.Length; i++)
        {
            Type arg = Args[i];
            string typeName = DescribeType(arg);
            if (typeName != "")
            {
                if (sb.Length > 1)
                    sb.Append(", ");
                if (i < parameters.Length)
                    sb.Append(parameters[i].Name).Append(": ");
                sb.Append(typeName);
            }
        }
        return sb.Append(')').ToString();
    }

    private readonly static FrozenDictionary<Type, string> types =
        new Dictionary<Type, string>
        {
            [typeof(bool)] = "bool",
            [typeof(int)] = "int",
            [typeof(long)] = "long",
            [typeof(double)] = "real",
            [typeof(string)] = "string",
            [typeof(Date)] = "date",
            [typeof(Complex)] = "Complex",
            [typeof(Series)] = "series",
            [typeof(Matrix)] = "matrix",
            [typeof(DVector)] = "vec",
            [typeof(CVector)] = "cvec",
            [typeof(NVector)] = "ivec",
            [typeof(DSequence)] = "seq",
            [typeof(CSequence)] = "cseq",
            [typeof(NSequence)] = "iseq",
        }.ToFrozenDictionary();

    public static string DescribeType(Type type) =>
        type.IsArray
        ? DescribeType(type.GetElementType()!) + "[]"
        : type.IsAssignableTo(typeof(Delegate))
        ? DescribeDelegate(type)
        : types.TryGetValue(type, out string? name) ? name : "";

    private static string DescribeDelegate(Type type) =>
        string.Join("=>", type.GenericTypeArguments.Select(DescribeType));
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
