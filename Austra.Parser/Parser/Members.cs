namespace Austra.Parser;

/// <summary>Syntactic analysis for AUSTRA.</summary>
internal static partial class Parser
{
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
            Type type = ParseType(new(source, text));
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
            int i = text.Length - 1;
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