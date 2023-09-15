namespace Austra.Library;

/// <summary>Most common sampling sequences.</summary>
public enum Frequency
{
    /// <summary>Values are published for each day or business day.</summary>
    Daily,
    /// <summary>Values are published every week.</summary>
    Weekly,
    /// <summary>Values are published every two weeks.</summary>
    Biweekly,
    /// <summary>Values are published monthly.</summary>
    Monthly,
    /// <summary>Values are published every two months.</summary>
    Bimonthly,
    /// <summary>Values are published quarterly.</summary>
    Quarterly,
    /// <summary>Values are published every six months.</summary>
    Semestral,
    /// <summary>Values are published for each year.</summary>
    Yearly,
    /// <summary>All other frequencies.</summary>
    Other
}

/// <summary>Represents a time series.</summary>
public sealed class Series : Series<Date>,
    IAdditionOperators<Series, Series, Series>,
    IAdditionOperators<Series, double, Series>,
    ISubtractionOperators<Series, Series, Series>,
    ISubtractionOperators<Series, double, Series>,
    IMultiplyOperators<Series, double, Series>,
    IDivisionOperators<Series, double, Series>,
    IUnaryNegationOperators<Series, Series>,
    IPointwiseMultiply<Series>
{
    /// <summary>Creates a named time series.</summary>
    /// <param name="name">The name of the series.</param>
    /// <param name="ticker">The provider's name for the series.</param>
    /// <param name="args">Arguments.</param>
    /// <param name="values">Values.</param>
    /// <param name="type">Type of the series.</param>
    /// <param name="freq">Sampling frequency.</param>
    [JsonConstructor]
    public Series(string name, string? ticker, Date[] args, double[] values,
        SeriesType type, Frequency freq)
        : base(name, ticker, args, values, type) => Freq = freq;

    /// <summary>Creates a named time series.</summary>
    /// <param name="name">The name of the series.</param>
    /// <param name="ticker">The provider's name for the series.</param>
    /// <param name="args">Arguments.</param>
    /// <param name="values">Values.</param>
    /// <param name="anchor">The source of type and frequency.</param>
    public Series(string name, string? ticker, Date[] args, double[] values, Series anchor)
        : base(name, ticker, args, values, anchor.Type) => Freq = anchor.Freq;

    /// <summary>Creates a named time series.</summary>
    /// <param name="name">The name of the series.</param>
    /// <param name="ticker">The provider's name for the series.</param>
    /// <param name="values">Values.</param>
    /// <param name="anchor">The source of args, type and frequency.</param>
    public Series(string name, string? ticker, double[] values, Series anchor)
        : base(name, ticker, anchor.args, values, anchor.Type) => Freq = anchor.Freq;

    /// <summary>
    /// Transforms a <see cref="Series{T}"/> into a <see cref="Series"/>.
    /// </summary>
    /// <param name="s">Original series to convert.</param>
    /// <returns>The new series.</returns>
    public static Series Adapt(Series<Date> s) =>
        new(s.Name, s.Ticker, s.args, s.values, s.Type, Frequency.Other);

    /// <summary>Gets the sampling frequency.</summary>
    public Frequency Freq { get; }

    /// <summary>A custom tag for the series.</summary>
    [JsonIgnore]
    public char Tag { get; set; } = ' ';

    /// <summary>Clones this series with a new name.</summary>
    /// <param name="newName">The new name.</param>
    /// <returns>A renamed series with shared values.</returns>
    public Series SetName(string newName) =>
        new(newName, Ticker, values, this) { Tag = Tag };

    /// <summary>Creates a new series based in the returns.</summary>
    /// <returns>A derived series with one less point.</returns>
    public unsafe new Series AsReturns()
    {
        double[] newValues = GC.AllocateUninitializedArray<double>(Count - 1);
        fixed (double* p = values, q = newValues)
        {
            int i = 0, size = newValues.Length;
            if (Avx.IsSupported)
            {
                Vector256<double> one = Vector256.Create(1.0);
                for (int top = size & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(q + i, Avx.Subtract(
                        Avx.Divide(Avx.LoadVector256(p + i), Avx.LoadVector256(p + i + 1)),
                        one));
            }
            for (; i < size; i++)
                q[i] = p[i] / p[i + 1] - 1;
        }
        return new(Name + ".RETS", Ticker, args[0..^1], newValues, SeriesType.Rets, Freq);
    }

    /// <summary>Creates a new series based in the logarithmic returns.</summary>
    /// <returns>A derived series with one less point.</returns>
    public unsafe new Series AsLogReturns()
    {
        double[] newValues = GC.AllocateUninitializedArray<double>(Count - 1);
        fixed (double* p = values, q = newValues)
        {
            int i = 0, size = newValues.Length;
            if (Avx2.IsSupported && Fma.IsSupported)
            {
                for (int top = size & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(q + i,
                        Avx.Divide(Avx.LoadVector256(p + i), Avx.LoadVector256(p + i + 1)).Log());
            }
            for (; i < size; i++)
                q[i] = Log(p[i] / p[i + 1]);
        }
        return new(Name + ".LOGS", Ticker, args[0..^1], newValues, SeriesType.Logs, Freq);
    }

    /// <summary>Extracts a slice from the series.</summary>
    /// <param name="range">The range to extract.</param>
    /// <returns>A new copy of the requested data.</returns>
    public new Series this[Range range]
    {
        get
        {
            (int offset, int length) = range.GetOffsetAndLength(values.Length);
            int low = values.Length - offset, high = low - length;
            return new(Name + ".SLICE", Ticker, args[high..low], values[high..low], Type, Freq);
        }
    }

    /// <summary>Takes a slice from a series.</summary>
    /// <param name="lower">Inclusive lower bound for the argument.</param>
    /// <param name="upper">Exclusive upper bound for the argument.</param>
    /// <returns>A slice of the original series.</returns>
    public new Series Slice(Date lower, Date upper)
    {
        (int low, int high) = GetSliceRange(lower, upper);
        return new(Name + ".SLICE", Ticker, args[high..low], values[high..low], this);
    }

    /// <summary>Takes a slice from a series.</summary>
    /// <param name="lower">Inclusive lower bound for the argument.</param>
    /// <param name="upper">Exclusive upper bound for the argument.</param>
    /// <returns>A slice of the original series.</returns>
    public Series Slice(int lower, int upper)
    {
        if (upper > args.Length)
            upper = args.Length;
        int low = args.Length - upper;
        int high = args.Length - lower;
        return new(Name + ".SLICE", Ticker, args[low..high], values[low..high], this);
    }

    /// <summary>Gets statistics on a slice of a date series.</summary>
    /// <param name="d">A date containing the month and year to consider.</param>
    /// <returns>On-the-fly calculated statistics.</returns>
    public Accumulator GetSliceStats(Date d) =>
        GetSliceStats(d.TruncateDay(), d.AddMonths(1).TruncateDay());

    /// <summary>
    /// Generates a normally distributed series using statistics from this series.
    /// </summary>
    /// <returns>A normally distributed time series.</returns>
    public unsafe Series Random()
    {
        NormalRandom rnd = new(Mean(), StandardDeviation());
        double[] newValues = GC.AllocateUninitializedArray<double>(Count);
        fixed (double* p = newValues)
            for (int i = 0; i < newValues.Length; i++)
                p[i] = rnd.NextDouble();
        return new(Name + ".RND", Ticker, newValues, this);
    }

    /// <summary>Creates an interpolator for this series.</summary>
    /// <returns>A set of splines.</returns>
    public DateSpline Spline() => new(this);

    /// <summary>Gets linear coefficients for a fitting line.</summary>
    /// <returns>The [α, β] vector.</returns>
    public Vector Fit()
    {
        double m_x = 0, m_y = 0;
        for (int i = 0; i < values.Length; i++)
        {
            m_x += (uint)args[i];
            m_y += values[i];
        }
        m_x /= values.Length;
        m_y /= values.Length;
        double a = 0, b = 0;
        for (int i = 0; i < values.Length; i++)
        {
            double x = (uint)args[i] - m_x;
            double y = values[i] - m_y;
            a += x * y;
            b += x * x;
        }
        a /= b;
        return new[] { a, m_y - a * m_x };
    }

    /// <summary>Computes the series predicted by a linear fit.</summary>
    /// <returns>A straight line.</returns>
    public Series LinearFit()
    {
        Vector coeffs = Fit();
        double a = coeffs[0], b = coeffs[1];
        double[] newValues = GC.AllocateUninitializedArray<double>(Count);
        for (int i = 0; i < newValues.Length; i++)
            newValues[i] = a * (uint)args[i] + b;
        return new(Name + ".FITS", Ticker, newValues, this);
    }

    /// <summary>Gets the moving return of a one month window.</summary>
    public Series MovingRet()
    {
        if (Type == SeriesType.Mixed || Type == SeriesType.MixedRets)
            throw new Exception("Invalid series type");
        var (low, high) = GetSliceRange(args[0].AddMonths(-1), args[0]);
        int delta = low - high;
        if (delta < 2)
        {
            (low, high) = GetSliceRange(args[0].AddYears(-1), args[0]);
            delta = low - high;
            if (delta < 2)
                throw new EmptySliceException("Not enough samples in window");
            // Check if it's safe to assume we're dealing with monthly samples.
            if (delta >= 11 && delta <= 13)
                delta = 12;
        }
        var newArgs = args[high..^(delta - 1)];
        double[] newValues = new double[newArgs.Length];
        for (int i = 0; i < newValues.Length; i++)
        {
            if (Type == SeriesType.Raw)
                newValues[i] = values[i] / values[i + delta - 1] - 1;
            else if (Type == SeriesType.Rets)
            {
                double acc = 1;
                for (int j = i; j < i + delta; j++)
                    acc *= 1 + values[j];
                newValues[i] = acc - 1;
            }
            else
            {
                double acc = 0;
                for (int j = i; j < i + delta; j++)
                    acc += values[j];
                newValues[i] = acc;
            }
        }
        return new(Name + ".MOVINGRET", Ticker, newArgs, newValues,
            Type == SeriesType.Logs ? Type : SeriesType.Rets, Freq);
    }

    /// <summary>Smooths data using a simple moving average.</summary>
    /// <param name="points">Number of points in the moving window.</param>
    /// <returns>The simple moving average of the original series.</returns>
    public Series MovingAvg(int points)
    {
        if (points <= 1)
            return this;
        double[] newValues = new double[Count];
        int c = Count - 1;
        double acc = newValues[c] = values[c];
        int i = 1;
        for (; i < points; i++)
        {
            acc += values[c - i];
            newValues[c - i] = acc / (i + 1);
        }
        double inv = 1.0 / points;
        for (; i <= c; i++)
        {
            acc += values[c - i] - values[c - i + points];
            newValues[c - i] = acc * inv;
        }
        return new(Name + ".MOVINGAVG", Ticker, newValues, this);
    }

    /// <summary>Smooths data using a simple moving standard deviation.</summary>
    /// <param name="points">Number of points in the moving window.</param>
    /// <returns>The simple moving standard deviation of the original series.</returns>
    public Series MovingStd(int points)
    {
        if (points <= 1)
            return this;
        double[] newValues = new double[Count];
        int c = Count - 1;
        double acc = values[c];
        double acc2 = acc * acc;
        int i = 1;
        for (; i < points; i++)
        {
            double v = values[c - i];
            acc += v;
            acc2 += v * v;
            newValues[c - i] = Sqrt((acc2 - acc * acc / (i + 1)) / i);
        }
        double inv = 1.0 / points;
        double inv1 = 1.0 / (points - 1);
        for (; i <= c; i++)
        {
            double newV = values[c - i];
            double oldV = values[c - i + points];
            acc += newV - oldV;
            // Yes, I'm clever!
            acc2 += (newV + oldV) * (newV - oldV);
            newValues[c - i] = Sqrt((acc2 - acc * acc * inv) * inv1);
        }
        return new(Name + ".MOVINGSTD", Ticker, newValues, this);
    }

    /// <summary>Compress data using a simple moving percentile.</summary>
    /// <param name="points">Number of points in the moving window.</param>
    /// <returns>The simple moving percentile of the original series.</returns>
    public Series MovingNcdf(int points)
    {
        if (points <= 1)
            return this;
        double[] newValues = new double[Count];
        int c = Count - 1;
        double acc = values[c];
        double acc2 = acc * acc;
        newValues[c] = 0.5;
        int i = 1;
        for (; i < points; i++)
        {
            double v = values[c - i];
            acc += v;
            acc2 += v * v;
            double mean = acc / (i + 1);
            double std = Sqrt(2 * (acc2 - acc * mean) / i);
            newValues[c - i] = std == 0.0 ? 0.5 : 0.5 * (1 + F.Erf((v - mean) / std));
        }
        double inv = 1.0 / points;
        double inv1 = 2.0 / (points - 1);
        for (; i <= c; i++)
        {
            double newV = values[c - i];
            double oldV = values[c - i + points];
            acc += newV - oldV;
            // Yes, I'm clever!
            acc2 += (newV + oldV) * (newV - oldV);
            double mean = acc * inv;
            double std = Sqrt((acc2 - acc * mean) * inv1);
            newValues[c - i] = 0.5 * (1 + F.Erf((newV - mean) / std));
        }
        return new(Name + ".MOVINGNCDF", Ticker, newValues, this);
    }

    /// <summary>Smooths data using a exponentially weighted moving average.</summary>
    /// <remarks>
    /// When <paramref name="alpha"/> = 1, the returned series 
    /// is identical to the original. A commonly used value for <paramref name="alpha"/>
    /// is <c>2 / (Count + 1).</c>
    /// </remarks>
    /// <param name="alpha">Smoothing factor.</param>
    /// <returns>The exponentially weighted M.A. of the original series.</returns>
    public Series EWMA(double alpha)
    {
        if (alpha <= 0 || alpha >= 1)
            return this;
        double[] newValues = GC.AllocateUninitializedArray<double>(Count);
        int c = Count - 1;
        double acc = newValues[c] = values[c];
        double beta = 1 - alpha;
        for (int i = 1; i <= c; i++)
        {
            acc = FusedMultiplyAdd(alpha, values[c - i], beta * acc);
            newValues[c - i] = acc;
        }
        return new(Name + ".EWMA", Ticker, newValues, this);
    }

    /// <summary>Creates a new series by adding values from the operands.</summary>
    /// <param name="s1">First operand.</param>
    /// <param name="s2">Second operand.</param>
    /// <returns>A new series.</returns>
    public static Series operator +(Series s1, Series s2)
    {
        if (s1.Freq != s2.Freq)
            throw new Exception("Cannot mix series with different frequencies");
        var (args, vals, type) = Add(s1, s2);
        return new(s1.Name + "+" + s2.Name, null, args, vals, type, s1.Freq);
    }

    /// <summary>Creates a new series by subtracting values from the operands.</summary>
    /// <param name="s1">First operand.</param>
    /// <param name="s2">Second operand.</param>
    /// <returns>A new series.</returns>
    public static Series operator -(Series s1, Series s2)
    {
        if (s1.Freq != s2.Freq)
            throw new Exception("Cannot mix series with different frequencies");
        var (args, vals, type) = Sub(s1, s2);
        return new(s1.Name + "-" + s2.Name, null, args, vals, type, s1.Freq);
    }

    /// <summary>Adds a scalar to a series.</summary>
    /// <param name="s">The series.</param>
    /// <param name="d">A scalar value.</param>
    public static Series operator +(Series s, double d) =>
        new(s.Name, s.Ticker, (double[])(s.GetValues() + d), s);

    /// <summary>Adds a scalar to a series.</summary>
    /// <param name="d">A scalar value.</param>
    /// <param name="s">The series.</param>
    public static Series operator +(double d, Series s) =>
        new(s.Name, s.Ticker, (double[])(s.GetValues() + d), s);

    /// <summary>Subtracts a fixed scalar value from a series.</summary>
    public static Series operator -(Series s, double d) =>
        new(s.Name, s.Ticker, (double[])(s.GetValues() - d), s);

    /// <summary>Subtracts series from a fixed scalar value.</summary>
    public static Series operator -(double d, Series s) =>
        new(s.Name, s.Ticker, (double[])(d - s.GetValues()), s);

    /// <summary>Negates values from a series.</summary>
    /// <param name="s">The series operand.</param>
    /// <returns>Pointwise negation.</returns>
    public static unsafe Series operator -(Series s) =>
        new("-" + s.Name, s.Ticker, (double[])-s.GetValues(), s);

    /// <summary>Scales values from a series.</summary>
    /// <param name="d">Scale factor.</param>
    /// <param name="s">Series to scale.</param>
    /// <returns>A new scaled series.</returns>
    public static Series operator *(double d, Series s) =>
        new(s.Name + "*", s.Ticker, (double[])(s.GetValues() * d), s);

    /// <summary>Scales values from a series.</summary>
    /// <param name="s">Series to scale.</param>
    /// <param name="d">Scale factor.</param>
    /// <returns>A new scaled series.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Series operator *(Series s, double d) => d * s;

    /// <summary>Divides all values from a series.</summary>
    /// <param name="s">Series to scale.</param>
    /// <param name="d">Divisor.</param>
    /// <returns>A new scaled series.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Series operator /(Series s, double d) => (1.0 / d) * s;

    /// <summary>Creates a new series by multiplying values from the operands.</summary>
    /// <remarks>This method corresponds to the .* operator in AUSTRA.</remarks>
    /// <param name="other">Second operand.</param>
    /// <returns>A new series.</returns>
    public unsafe Series PointwiseMultiply(Series other)
    {
        if (Freq != other.Freq)
            throw new Exception("Cannot mix series with different frequencies");
        int len = Min(Count, other.Count);
        Date[] newArgs = Count == len ? args : other.args;
        double[] newValues = new double[len];

        fixed (double* pA = values, pB = other.values, pC = newValues)
        {
            int i = 0;
            if (Avx.IsSupported)
                for (int top = len & Simd.AVX_MASK; i < top; i += 4)
                    Avx.Store(
                        pC + i,
                        Avx.Multiply(Avx.LoadVector256(pA + i), Avx.LoadVector256(pB + i)));
            for (; i < len; i++)
                pC[i] = pA[i] * pB[i];
        }
        return new(
            Name + ".*" + other.Name, Ticker,
            newArgs, newValues, Combine(Type, other.Type), other.Freq);
    }

    /// <summary>Calculates the weighted sum of an array of series.</summary>
    /// <param name="weights">Array of weights.</param>
    /// <param name="series">Array of series.</param>
    /// <returns>The weighted sum of series.</returns>
    public static Series Combine(Vector weights, params Series[] series)
    {
        if (series.Length == 0)
            throw new Exception("Empty list of series");
        Frequency f0 = series[0].Freq;
        for (int i = 1; i < series.Length; i++)
            if (f0 != series[i].Freq)
                throw new Exception("Cannot mix series with different frequencies");
        var (name, args, vals, type) = CombineSeries(weights, series);
        return new(name, null, args, vals, type, f0);
    }

    /// <summary>Creates a linear model from a series and a set of predictors.</summary>
    /// <param name="predictors">Series used to predict this one.</param>
    /// <returns>A full linear model.</returns>
    public LinearSModel FullLinearModel(params Series[] predictors) =>
        new(this, predictors);

    /// <summary>Creates an AR model from a series and a degree.</summary>
    /// <param name="degree">Number of independent variables in the model.</param>
    /// <returns>A full autoregressive model.</returns>
    public ARSModel ARModel(int degree) =>
        new(this, degree);

    /// <summary>
    /// Creates a series retaining the first <paramref name="count"/> items.
    /// </summary>
    /// <param name="count">Number of items to retain.</param>
    /// <returns>A new shorter series.</returns>
    public Series Prune(int count) =>
        Count <= count ? this : new(Name, Ticker, args[..count], values[..count], this);

    /// <summary>
    /// Creates a new series by transforming each item with the given function.
    /// </summary>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new series with transformed content.</returns>
    public Series Map(Func<double, double> mapper)
    {
        double[] newValues = new double[values.Length];
        for (int i = 0; i < newValues.Length; i++)
            newValues[i] = mapper(values[i]);
        return new(Name + ".MAP", Ticker, newValues, this);
    }

    /// <summary>
    /// Creates a new series by transforming each item with the given function.
    /// </summary>
    /// <param name="predicate">A boolean function accepting a series point.</param>
    /// <returns>A new series with the selected points.</returns>
    public Series Filter(Func<Point<Date>, bool> predicate)
    {
        List<Date> dates = new(Count);
        List<double> vs = new(Count);
        for (int i = 0; i < Count; i++)
        {
            Date d = args[i];
            double v = values[i];
            if (predicate(new(d, v)))
            {
                dates.Add(d);
                vs.Add(v);
            }
        }
        return dates.Count == Count
            ? this
            : new(Name + ".FILTER", Ticker, dates.ToArray(), vs.ToArray(), this);
    }

    /// <summary>Combines the common sufix of two time series.</summary>
    /// <param name="other">Second series to combine.</param>
    /// <param name="zipper">The combining function.</param>
    /// <returns>The combining function applied to each pair of items.</returns>
    public unsafe Series Zip(Series other, Func<double, double, double> zipper)
    {
        if (Freq != other.Freq)
            throw new Exception("Cannot mix series with different frequencies");
        int len = Min(Count, other.Count);
        Date[] newArgs = Count == len ? args : other.args;
        double[] newValues = new double[len];
        fixed (double* pA = values, pB = other.values, pC = newValues)
            for (int i = 0; i < len; i++)
                pC[i] = zipper(pA[i], pB[i]);
        return new(
            "ZIP(" + Name + "," + other.Name + ")", Ticker,
            newArgs, newValues, Combine(Type, other.Type), Freq);
    }

    /// <summary>Checks whether the predicate is satified by all items.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if all items satisfy the predicate.</returns>
    public bool All(Func<double, bool> predicate) => GetValues().All(predicate);

    /// <summary>Checks whether the predicate is satified by at least one item.</summary>
    /// <param name="predicate">The predicate to be checked.</param>
    /// <returns><see langword="true"/> if there exists a item satisfying the predicate.</returns>
    public bool Any(Func<double, bool> predicate) => GetValues().Any(predicate);

    /// <summary>Gets a textual representation of the series.</summary>
    /// <returns>A text containing the name and the point count.</returns>
    public override string ToString() => $"{Name}: Series/{Type}/{freq2str[(int)Freq]}[{Count}]";

    private static readonly string[] freq2str =
    {
        "1D", "1W", "2W", "1M", "2M", "3M", "6M", "1Y", ""
    };
}
