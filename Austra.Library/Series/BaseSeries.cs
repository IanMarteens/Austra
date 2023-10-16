namespace Austra.Library;

/// <summary>Simple taxonomy for series.</summary>
public enum SeriesType
{
    /// <summary>Raw values, such as prices or volumes.</summary>
    Raw,
    /// <summary>Simple returns.</summary>
    Rets,
    /// <summary>Logarithmic returns.</summary>
    Logs,
    /// <summary>Simple and logarithmic returns mixed.</summary>
    MixedRets,
    /// <summary>Cannot assert the type of the series.</summary>
    Mixed
}

/// <summary>Represents a point in a series.</summary>
/// <typeparam name="T">Represents the type of the abscissa.</typeparam>
public readonly struct Point<T> where T : struct, IComparable<T>
{
    /// <summary>Gets the argument from the point.</summary>
    public T Arg { get; }
    /// <summary>Gets the value at the point's date.</summary>
    public double Value { get; }

    /// <summary>Creates a point in a series.</summary>
    /// <param name="arg">Argument.</param>
    /// <param name="value">Point's value.</param>
    public Point(T arg, double value) =>
        (Arg, Value) = (arg, value);

    /// <summary>Checks if the argument is a point with the same values.</summary>
    /// <param name="obj">Object to compare.</param>
    /// <returns><see langword="true"/> if the argument is a point with the same values.</returns>
    public override bool Equals(object? obj) => obj is Point<T> point && this == point;

    /// <summary>Returns a hashcode for the series.</summary>
    /// <returns>A hashcode combining hashcodes from argument and value.</returns>
    public override int GetHashCode() => Arg.GetHashCode() ^ Value.GetHashCode();

    /// <summary>Checks two points for equality.</summary>
    /// <param name="left">First point to compare.</param>
    /// <param name="right">Second point to compare.</param>
    /// <returns><see langword="true"/> when both points has the same components.</returns>
    public static bool operator ==(Point<T> left, Point<T> right) =>
        left.Arg.Equals(right.Arg) && left.Value.Equals(right.Value);

    /// <summary>Checks two points for inequality.</summary>
    /// <param name="left">First point to compare.</param>
    /// <param name="right">Second point to compare.</param>
    /// <returns><see langword="true"/> when both points has diffent components.</returns>
    public static bool operator !=(Point<T> left, Point<T> right) =>
        !left.Arg.Equals(right.Arg) || !left.Value.Equals(right.Value);

    /// <summary>Gets a text representation of the point.</summary>
    /// <returns>A string containing the argument and its associated value.</returns>
    public override string ToString() =>
        $"[{Arg}: {Value}]";
}

/// <summary>Represents a named series.</summary>
/// <typeparam name="T">Type of the abscissa.</typeparam>
public class Series<T> : ISafeIndexed where T : struct, IComparable<T>
{
    /// <summary>Stores the arguments for the series.</summary>
    protected internal readonly T[] args;
    /// <summary>Stores the values for the series.</summary>
    protected internal readonly double[] values;

    /// <summary>Creates a named series.</summary>
    /// <param name="name">The name of the series.</param>
    /// <param name="ticker">Externally provided name for the series.</param>
    /// <param name="args">Arguments.</param>
    /// <param name="values">Values.</param>
    /// <param name="type">Type of the series.</param>
    public Series(string name, string? ticker, T[] args, double[] values, SeriesType type) =>
        (Name, Ticker, this.args, this.values, Type, Stats)
            = (name, ticker, args, values, type, new(values));

    /// <summary>Creates a series with integer arguments given its values.</summary>
    /// <param name="name">The name of the new series.</param>
    /// <param name="ticker">Externally provided name for the series.</param>
    /// <param name="values">Values array.</param>
    /// <param name="type">Type of the series.</param>
    /// <returns>The new series.</returns>
    public static Series<int> Create(string name, string? ticker,
        double[] values, SeriesType type = SeriesType.Raw) =>
        new(name, ticker, Enumerable.Range(0, values.Length).ToArray(), values, type);

    /// <summary>Gets the name of the series.</summary>
    public string Name { get; protected set; }

    /// <summary>Gets the ticker of the series.</summary>
    public string? Ticker { get; protected set; }

    /// <summary>Is this a raw (Prices) series or a derived one?</summary>
    public SeriesType Type { get; }

    /// <summary>Gets the list of arguments from the series.</summary>
    public IEnumerable<T> Args => args;

    /// <summary>Gets the list of values from the series.</summary>
    public IEnumerable<double> Values => values;

    /// <summary>Gets the values array as a vector.</summary>
    /// <returns>The values array as a vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector GetValues() => values;

    /// <summary>Gets the number of points in the series.</summary>
    [JsonIgnore]
    public int Count => values.Length;

    /// <summary>Gets the sorted list of points.</summary>
    /// <remarks>The order is inverted on purpose.</remarks>
    [JsonIgnore]
    public IEnumerable<Point<T>> Points
    {
        get
        {
            for (int i = values.Length - 1; i >= 0; i--)
                yield return new(args[i], values[i]);
        }
    }

    /// <summary>Gets a point given its index.</summary>
    /// <param name="index">The index of the point to retrieve.</param>
    /// <returns>The specified point.</returns>
    public Point<T> this[int index] =>
        new(args[args.Length - index - 1], values[values.Length - index - 1]);

    /// <summary>Gets a point given its index.</summary>
    /// <param name="index">The index of the point to retrieve.</param>
    /// <returns>The specified point.</returns>
    public Point<T> this[Index index]
    {
        get
        {
            int i = index.IsFromEnd ? values.Length - index.Value : index.Value;
            return new(args[args.Length - i - 1], values[values.Length - i - 1]);
        }
    }

    /// <summary>Extracts a slice from the series.</summary>
    /// <param name="range">The range to extract.</param>
    /// <returns>A new copy of the requested data.</returns>
    public Series<T> this[Range range]
    {
        get
        {
            (int offset, int length) = range.GetOffsetAndLength(values.Length);
            int low = values.Length - offset, high = low - length;
            return new(Name + ".SLICE", Ticker, args[high..low], values[high..low], Type);
        }
    }

    /// <summary>
    /// Safe access to the series' points. If the index is out of range, a zero is returned.
    /// </summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The value at the given index, or zero, if index is out of range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point<T> SafeThis(int index) =>
        (uint)index >= values.Length
        ? default
        : new(
            Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(args), values.Length - index - 1),
            Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(values), values.Length - index - 1));

    /// <summary>Gets statistics on the series.</summary>
    /// <returns>The current calculated statistics.</returns>
    [JsonIgnore]
    public Accumulator Stats { get; }

    /// <summary>Returns the minimum value from the series.</summary>
    [JsonIgnore]
    public double Minimum => Stats.Minimum;

    /// <summary>Returns the maximum value from the series.</summary>
    [JsonIgnore]
    public double Maximum => Stats.Maximum;

    /// <summary>Gets the mean value from the series.</summary>
    [JsonIgnore]
    public double Mean => Stats.Mean;

    /// <summary>Gets the unbiased variance.</summary>
    [JsonIgnore]
    public double Variance => Stats.Variance;

    /// <summary>Gets the variance from the full population.</summary>
    [JsonIgnore]
    public double PopulationVariance => Stats.PopulationVariance;

    /// <summary>Gets the unbiased standard deviation.</summary>
    [JsonIgnore]
    public double StandardDeviation => Stats.StandardDeviation;

    /// <summary>Gets the standard deviation from the full population.</summary>
    [JsonIgnore]
    public double PopulationStandardDeviation => Stats.PopulationStandardDeviation;

    /// <summary>Gets the unbiased population skewness.</summary>
    [JsonIgnore]
    public double Skewness => Stats.Skewness;

    /// <summary>Get the skewness from the full population.</summary>
    [JsonIgnore]
    public double PopulationSkewness => Stats.PopulationSkewness;

    /// <summary>Gets the unbiased population kurtosis.</summary>
    [JsonIgnore]
    public double Kurtosis => Stats.Kurtosis;

    /// <summary>Gets the kurtosis from the full population.</summary>
    [JsonIgnore]
    public double PopulationKurtosis => Stats.PopulationKurtosis;

    /// <summary>Gets the first point in the series.</summary>
    [JsonIgnore]
    public Point<T> First => this[0];

    /// <summary>Gets the last point in the series.</summary>
    [JsonIgnore]
    public Point<T> Last => this[^1];

    /// <summary>Creates a new series based in the returns.</summary>
    /// <returns>A derived series with one less point.</returns>
    public Series<T> AsReturns()
    {
        var newArgs = new T[Count - 1];
        Array.Copy(args, newArgs, newArgs.Length);
        var newValues = new double[Count - 1];
        for (int i = 0; i < newValues.Length; i++)
            newValues[i] = values[i] / values[i + 1] - 1;
        return new(Name + ".RETS", Ticker, newArgs, newValues, SeriesType.Rets);
    }

    /// <summary>Creates a new series based in the logarithmic returns.</summary>
    /// <returns>A derived series with one less point.</returns>
    public Series<T> AsLogReturns()
    {
        var newArgs = new T[Count - 1];
        Array.Copy(args, newArgs, newArgs.Length);
        var newValues = new double[Count - 1];
        for (int i = 0; i < newValues.Length; i++)
            newValues[i] = Log(values[i] / values[i + 1]);
        return new(Name + ".LOGS", Ticker, newArgs, newValues, SeriesType.Logs);
    }

    /// <summary>Gets a textual representation of the series.</summary>
    /// <returns>A text containing the name and the point count.</returns>
    public override string ToString() => $"Series/{Type}[{Count}]: {Name}";

    /// <summary>Calculate indexes given a range of arguments.</summary>
    /// <param name="lower">Inclusive lower bound for the argument.</param>
    /// <param name="upper">Exclusive upper bound for the argument.</param>
    /// <returns>Indexes for the requested slice.</returns>
    protected (int low, int high) GetSliceRange(T lower, T upper)
    {
        int low = Array.BinarySearch(args, lower, comparer);
        if (low < 0)
            low = ~low;
        else
            low++;
        int high = Array.BinarySearch(args, upper, comparer);
        if (high < 0)
            high = ~high;
        else
            high++;
        if (high > low)
            throw new EmptySliceException();
        return (low, high);
    }

    /// <summary>Takes a slice from a series.</summary>
    /// <param name="lower">Inclusive lower bound for the argument.</param>
    /// <param name="upper">Exclusive upper bound for the argument.</param>
    /// <returns>A slice of the original series.</returns>
    public Series<T> Slice(T lower, T upper)
    {
        (int low, int high) = GetSliceRange(lower, upper);
        return new(Name + ".SLICE", Ticker, args[high..low], values[high..low], Type);
    }

    /// <summary>Gets statistics on a slice of the series.</summary>
    /// <param name="lower">Inclusive lower bound for the argument.</param>
    /// <param name="upper">Exclusive upper bound for the argument.</param>
    /// <returns>On-the-fly calculated statistics.</returns>
    public unsafe Accumulator GetSliceStats(T lower, T upper)
    {
        (int low, int high) = GetSliceRange(lower, upper);
        fixed (double* p = values)
        {
            Accumulator result = new();
            result.Add(p + high, low - high);
            return result;
        }
    }

    /// <summary>Returns the zero-based index of the first occurrence of a value.</summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>Index of the first ocurrence, if found; <c>-1</c>, otherwise.</returns>
    public int IndexOf(double value)
    {
        int idx = new Vector(values).IndexOf(value);
        return idx < 0 ? idx : Count - idx - 1;
    }

    /// <summary>Gets the maximum absolute value.</summary>
    /// <returns>The max-norm of the values vector.</returns>
    public double AbsMax() => new Vector(values).AMax();

    /// <summary>Gets the minimum absolute value.</summary>
    /// <returns>The minimum absolute of the values vector.</returns>
    public double AbsMin() => new Vector(values).AMin();

    /// <summary>Multilinear regression based in Ordinary Least Squares.</summary>
    /// <param name="predictors">Predicting series.</param>
    /// <returns>Regression coefficients.</returns>
    public Vector LinearModel(params Series[] predictors)
    {
        int size = predictors.Select(s => s.Count).Min();
        Vector[] rows = new Vector[predictors.Length + 1];
        rows[0] = new(size, 1.0);
        for (int i = 0; i < predictors.Length; i++)
        {
            double[] row = predictors[i].values;
            rows[i + 1] = new(row.Length == size ? row : row[0..size]);
        }
        Matrix x = new(rows);
        Vector y = new(Count == size ? values : values[0..size]);
        return x.MultiplyTranspose(x).Cholesky().Solve(x * y);
    }

    /// <summary>Computes the covariance between two series.</summary>
    /// <param name="other">Second series.</param>
    /// <returns>The covariance of the two operands.</returns>
    public unsafe double Covariance(Series<T> other)
    {
        if (this == other)
            return Variance;
        int count = Min(Count, other.Count);
        double x0 = Mean;
        double y0 = other.Mean;
        double ex = 0, ey = 0, exy = 0;
        fixed (double* pA = values, pB = other.values)
        {
            int i = 0;
            if (Avx.IsSupported)
            {
                var meanx = Vector256.Create(x0);
                var meany = Vector256.Create(y0);
                var vex = Vector256<double>.Zero;
                var vey = Vector256<double>.Zero;
                var vexy = Vector256<double>.Zero;
                for (int top = count & Simd.AVX_MASK; i < top; i += 4)
                {
                    var x = Avx.Subtract(Avx.LoadVector256(pA + i), meanx);
                    var y = Avx.Subtract(Avx.LoadVector256(pB + i), meany);
                    vex = Avx.Add(vex, x);
                    vey = Avx.Add(vey, y);
                    vexy = vexy.MultiplyAdd(x, y);
                }
                ex = vex.Sum();
                ey = vey.Sum();
                exy = vexy.Sum();
            }
            for (; i < count; i++)
            {
                double x = pA[i] - x0;
                double y = pB[i] - y0;
                ex += x;
                ey += y;
                exy += x * y;
            }
        }
        return (exy - ex * ey / count) / (count - 1);
    }

    /// <summary>Computes the Pearson correlation between two series.</summary>
    /// <param name="other">Second series.</param>
    /// <returns>The covariance divided by the standard deviations.</returns>
    public double Correlation(Series<T> other) =>
        this == other
        ? 1.0
        : Covariance(other) / (StandardDeviation * other.StandardDeviation);

    /// <summary>Computes the covariance matrix for a group of series.</summary>
    /// <param name="series">A list of series.</param>
    /// <returns>A symmetric real matrix.</returns>
    public static Matrix CovarianceMatrix(params Series<T>[] series)
    {
        double[,] result = new double[series.Length, series.Length];
        for (int row = 0; row < series.Length; row++)
        {
            result[row, row] = series[row].Variance;
            for (int col = row + 1; col < series.Length; col++)
            {
                double cov = series[row].Covariance(series[col]);
                result[row, col] = result[col, row] = cov;
            }
        }
        return result;
    }

    /// <summary>Computes the correlation matrix for a group of series.</summary>
    /// <param name="series">A list of series.</param>
    /// <returns>A symmetric real matrix.</returns>
    public static Matrix CorrelationMatrix(params Series<T>[] series)
    {
        double[,] result = new double[series.Length, series.Length];
        for (int row = 0; row < series.Length; row++)
        {
            result[row, row] = 1.0;
            for (int col = row + 1; col < series.Length; col++)
            {
                double cov = series[row].Correlation(series[col]);
                result[row, col] = result[col, row] = cov;
            }
        }
        return result;
    }

    /// <summary>Computes the autocorrelation for a fixed lag.</summary>
    /// <param name="lag">Lag number in samples.</param>
    /// <returns>The autocorrelation factor.</returns>
    public double AutoCorrelation(int lag) =>
        lag < 0
        ? throw new ArgumentOutOfRangeException(nameof(lag), "Lag cannot be negative")
        : lag == 0
        ? 1
        : Count - lag <= 1
        ? throw new ArgumentOutOfRangeException(nameof(lag), "Lag too large")
        : new Vector(values).AutoCorrelation(lag, Stats.Mean);

    /// <summary>Computes autocorrelation for a range of lags.</summary>
    /// <param name="size">Number of lags to compute.</param>
    /// <returns>Pairs lags/autocorrelation.</returns>
    public Series<int> Correlogram(int size) =>
        Create("CORR(" + Name + ")", Ticker, new Vector(values).CorrelogramRaw(size), Type);

    /// <summary>Computes autocorrelation for all lags.</summary>
    /// <returns>Pairs lags/autocorrelation.</returns>
    public Series<int> ACF() => Correlogram(Count - 2);

    /// <summary>Computes the real discrete Fourier transform.</summary>
    /// <returns>The spectrum.</returns>
    public FftRModel Fft() => new(FFT.Transform(values));

    /// <summary>Returns ascendenly sorted values.</summary>
    /// <returns>A series mapping percentiles into values.</returns>
    public Series<double> Percentiles()
    {
        double[] newValues = (double[])values.Clone();
        Array.Sort(newValues);
        double[] args = new double[newValues.Length];
        for (int i = 0; i < args.Length; i++)
            args[i] = (double)(i + 1) / args.Length;
        return new(Name + ".PERCENTILES", Ticker, args, newValues, Type);
    }

    /// <summary>The normal cumulative distribution function.</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>NCDF of the value according to the estimated mean and variance.</returns>
    public double NCdf(double value) =>
        0.5 * (1 + F.Erf((value - Mean) / (StandardDeviation * Simd.SQRT2)));

    /// <summary>The normal cumulative distribution function of the most recent value.</summary>
    /// <returns>NCDF of the value according to the estimated mean and variance.</returns>
    public double NCdf() => NCdf(Last.Value);

    /// <summary>Combines two series types.</summary>
    /// <param name="s1">First type.</param>
    /// <param name="s2">Second type.</param>
    /// <returns>The type resulting from the mixing.</returns>
    protected static SeriesType Combine(SeriesType s1, SeriesType s2) =>
        s1 == s2
        ? s1
        : s1 == SeriesType.Mixed || s2 == SeriesType.Mixed
        ? SeriesType.Mixed
        : s1 == SeriesType.Raw || s2 == SeriesType.Raw
        ? SeriesType.Mixed
        : SeriesType.MixedRets;

    /// <summary>Low level method to add two series.</summary>
    /// <param name="s1">First series to add.</param>
    /// <param name="s2">Second series to add.</param>
    /// <returns>A tuple with the arguments, values and type of the new series.</returns>
    protected static unsafe (T[], double[], SeriesType) Add(Series<T> s1, Series<T> s2)
    {
        int len = Min(s1.Count, s2.Count);
        T[] newArgs = s1.Count == len ? s1.args : s2.args;
        double[] newValues = GC.AllocateUninitializedArray<double>(len);

        fixed (double* pA = s1.values, pB = s2.values, pC = newValues)
        {
            int i = 0;
            if (Avx.IsSupported)
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(pC + i,
                        Avx.Add(Avx.LoadVector256(pA + i), Avx.LoadVector256(pB + i)));
            for (; i < len; i++)
                pC[i] = pA[i] + pB[i];
        }
        return (newArgs, newValues, Combine(s1.Type, s2.Type));
    }

    /// <summary>Creates a new series by adding values from the operands.</summary>
    /// <param name="s1">First operand.</param>
    /// <param name="s2">Second operand.</param>
    /// <returns>A new series.</returns>
    public static Series<T> operator +(Series<T> s1, Series<T> s2)
    {
        var (args, vals, type) = Add(s1, s2);
        return new(s1.Name + "+" + s2.Name, s1.Ticker, args, vals, type);
    }

    /// <summary>Low level method to perform series subtraction.</summary>
    /// <param name="s1">The minuend.</param>
    /// <param name="s2">The subtrahend.</param>
    /// <returns>A tuple with the arguments, values and type of the new series.</returns>
    protected static unsafe (T[], double[], SeriesType) Sub(Series<T> s1, Series<T> s2)
    {
        int len = Min(s1.Count, s2.Count);
        T[] newArgs = s1.Count == len ? s1.args : s2.args;
        double[] newValues = GC.AllocateUninitializedArray<double>(len);

        fixed (double* pA = s1.values, pB = s2.values, pC = newValues)
        {
            int i = 0;
            if (Avx.IsSupported)
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(pC + i,
                        Avx.Subtract(Avx.LoadVector256(pA + i), Avx.LoadVector256(pB + i)));
            for (; i < len; i++)
                pC[i] = pA[i] - pB[i];
        }
        return (newArgs, newValues, Combine(s1.Type, s2.Type));
    }

    /// <summary>Creates a new series by subtracting values from the operands.</summary>
    /// <param name="s1">First operand.</param>
    /// <param name="s2">Second operand.</param>
    /// <returns>A new series.</returns>
    public static Series<T> operator -(Series<T> s1, Series<T> s2)
    {
        var (args, vals, type) = Add(s1, s2);
        return new(s1.Name + "-" + s2.Name, s1.Ticker, args, vals, type);
    }

    /// <summary>Scales values from a series.</summary>
    /// <param name="d">Scale factor.</param>
    /// <param name="s">Series to scale.</param>
    /// <returns>A new scaled series.</returns>
    public static Series<T> operator *(double d, Series<T> s) =>
        new(s.Name + "*", s.Ticker, s.args, (double[])(new Vector(s.values) * d), s.Type);

    /// <summary>Scales values from a series.</summary>
    /// <param name="s">Series to scale.</param>
    /// <param name="d">Scale factor.</param>
    /// <returns>A new scaled series.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Series<T> operator *(Series<T> s, double d) => d * s;

    /// <summary>Low-level method to linearly combine series with weights.</summary>
    /// <param name="weights">The weights of the linear combination.</param>
    /// <param name="series">Series to combine.</param>
    /// <returns>The linear combination of the series.</returns>
    protected static unsafe (string, T[], double[], SeriesType) CombineSeries(
        Vector weights, params Series<T>[] series)
    {
        if (weights.Length == 0 ||
            weights.Length != series.Length &&
            weights.Length != series.Length + 1)
            throw new ArgumentException("Weights and series don't match");
        Series<T> s0 = series[0];
        int size = s0.Count;
        SeriesType type = s0.Type;
        T[] args = s0.args;
        int firstW = weights.Length == series.Length ? 0 : 1;
        StringBuilder sb = new();
        if (firstW > 0)
            sb.Append("α+");
        sb.Append(s0.Name);
        for (int i = 1; i < series.Length; i++)
        {
            Series<T> s = series[i];
            if (s.Count < size)
            {
                size = s.Count;
                args = s.args;
            }
            type = Combine(type, s.Type);
            sb.Append('+').Append(s.Name);
        }
        double[] values = new double[size];
        if (firstW > 0)
            Array.Fill(values, weights[0]);
        fixed (double* p = values)
        {
            for (int i = 0; i < series.Length; i++)
            {
                fixed (double* pa = series[i].values)
                {
                    int j = 0;
                    double w = weights[firstW + i];
                    if (Avx.IsSupported)
                    {
                        var vec = Vector256.Create(w);
                        for (int top = size & Simd.AVX_MASK; j < top; j += 4)
                            Avx.Store(p + j, Avx.LoadVector256(p + j).MultiplyAdd(pa + j, vec));
                    }
                    for (; j < size; j++)
                        p[j] += pa[j] * w;
                }
            }
        }
        return (sb.ToString(), args, values, type);
    }

    /// <summary>Calculates the weighted sum of an array of series.</summary>
    /// <param name="weights">Array of weights.</param>
    /// <param name="series">Array of series.</param>
    /// <returns>The weighted sum of series.</returns>
    public static Series<T> Combine(Vector weights, Series<T>[] series)
    {
        var (name, args, vals, type) = CombineSeries(weights, series);
        return new Series<T>(name, null, args, vals, type);
    }

    /// <summary>Finds the coefficients for an autoregressive model.</summary>
    /// <param name="degree">Number of coefficients in the model.</param>
    /// <returns>The coefficients of the AR(degree) model.</returns>
    public Vector AutoRegression(int degree) => AutoRegression(degree, out _, out _);

    /// <summary>Finds the coefficients for an autoregressive model.</summary>
    /// <param name="degree">Number of coefficients in the model.</param>
    /// <param name="matrix">The correlation matrix.</param>
    /// <param name="correlations">The correlations.</param>
    /// <returns>The coefficients of the AR(degree) model.</returns>
    internal Vector AutoRegression(int degree, out Matrix matrix, out Vector correlations) =>
        GetValues().Reverse().AutoRegression(degree, out matrix, out correlations);

    /// <summary>Calculates the sum of the series values.</summary>
    /// <returns>The sum of all series values.</returns>
    public double Sum() => new Vector(values).Sum();

    /// <summary>Comparer for reverse order.</summary>
    private sealed class ReverseComparer : IComparer<T>
    {
        public int Compare(T x, T y) => y.CompareTo(x);
    }

    /// <summary>Instance for the default arguments comparer.</summary>
    private static readonly IComparer<T> comparer = new ReverseComparer();
}
